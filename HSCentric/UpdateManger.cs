using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HSCentric
{
    public class UpdateManger
    {
        private static readonly UpdateManger s_instance = new UpdateManger();
        private readonly object m_lifecycleLock = new object();
        private readonly SemaphoreSlim m_checkLock = new SemaphoreSlim(1, 1);
        private readonly Func<string, CancellationToken, Task<IBattleNetAgent>> m_connect;
        private readonly Func<string[]> m_gameDirectories;
        private readonly Func<bool> m_gamesRunning;
        private Action m_callbackUpdateComplete;
        private string m_path = "";
        private Timer m_timer;
        private CancellationTokenSource m_session;
        private IBattleNetAgent m_agent;
        private HearthstoneInstallation m_installation;
        private string m_operation;
        private string m_progress;
        private DateTime m_startedUtc;
        private DateTime m_lastProgressUtc;
        private int m_failures;
        private bool m_submitting;
        private bool m_active;

        public UpdateManger() : this(BattleNetAgentClient.ConnectAsync,
            () => HSUnitManager.Get().Access(units => units.Where(u => u.Enable)
                .Select(u => Path.GetDirectoryName(u.HSPath)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()),
            AreGamesRunning) { }

        internal UpdateManger(Func<string, CancellationToken, Task<IBattleNetAgent>> connect,
            Func<string[]> gameDirectories, Func<bool> gamesRunning)
        {
            m_connect = connect;
            m_gameDirectories = gameDirectories;
            m_gamesRunning = gamesRunning;
        }

        public static UpdateManger Get() { return s_instance; }

        public void Init(Action callbackUpdateComplete, string path)
        {
            lock (m_lifecycleLock)
            {
                m_callbackUpdateComplete = callbackUpdateComplete;
                m_path = path;
            }
        }

        public void Start()
        {
            lock (m_lifecycleLock)
            {
                // Multiple accounts can report the same update. Keep one session.
                if (m_active || m_session != null) return;
                m_session = new CancellationTokenSource();
                m_active = true;
                m_startedUtc = m_lastProgressUtc = DateTime.UtcNow;
                m_failures = 0;
                m_submitting = false;
                Out.Info("[升级]5分钟后通过战网Agent更新炉石");
                if (m_timer == null) m_timer = new Timer(Check, null, Timeout.Infinite, Timeout.Infinite);
                m_timer.Change(5 * 60 * 1000, 30 * 1000);
            }
        }

        public void Stop()
        {
            lock (m_lifecycleLock)
            {
                m_active = false;
                m_timer?.Change(Timeout.Infinite, Timeout.Infinite);
                m_session?.Cancel();
            }
            // Let cancelled HTTP calls unwind before the backend process exits,
            // otherwise its privately started Agent could be left behind.
            if (m_checkLock.Wait(TimeSpan.FromSeconds(10)))
            {
                try { Cleanup(); }
                finally { m_checkLock.Release(); }
            }
            else Out.Error("[升级]等待取消超时，正在进行的检查将自行清理Agent");
        }

        private async void Check(object state) { await CheckAsync(); }

        internal async Task CheckAsync()
        {
            if (!m_checkLock.Wait(0)) return;
            bool completed = false;
            CancellationTokenSource session = null;
            try
            {
                string path;
                lock (m_lifecycleLock)
                {
                    if (!m_active) return;
                    session = m_session;
                    path = m_path;
                }
                CancellationToken token = session.Token;
                token.ThrowIfCancellationRequested();
                if (DateTime.UtcNow - m_startedUtc > TimeSpan.FromHours(2))
                    throw new InvalidOperationException("升级超过2小时，保持暂停；请检查战网后重启中控重试");
                if (m_gamesRunning())
                {
                    Out.Debug("[升级]等待炉石客户端全部退出");
                    return;
                }

                if (m_agent == null) m_agent = await m_connect(path, token);
                if (m_installation == null)
                {
                    m_installation = await m_agent.FindHearthstoneAsync(m_gameDirectories(), token);
                    Out.Info("[升级]已识别 " + m_installation.Uid + "，目录：" + m_installation.Directory);
                }
                if (m_operation == null)
                {
                    // A lost submission response may still mean the operation was accepted.
                    m_submitting = true;
                    m_operation = await m_agent.BeginUpdateAsync(m_installation, token);
                    m_submitting = false;
                    m_lastProgressUtc = DateTime.UtcNow;
                    Out.Info("[升级]炉石更新任务已提交");
                }
                AgentUpdateState update = await m_agent.GetUpdateStateAsync(m_installation, m_operation, token);
                token.ThrowIfCancellationRequested();
                if (update.Fingerprint != m_progress)
                {
                    m_progress = update.Fingerprint;
                    m_lastProgressUtc = DateTime.UtcNow;
                }
                m_failures = 0;
                if (update.IsComplete)
                {
                    completed = true;
                    lock (m_lifecycleLock)
                    {
                        m_active = false;
                        m_timer.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                    Out.Info("[升级]Agent确认炉石更新完成，版本：" + update.LocalVersion);
                }
                else
                {
                    Out.Debug("[升级]炉石更新进度 " + update.Progress.ToString("P0") + "，等待补丁应用及版本确认");
                    if (DateTime.UtcNow - m_lastProgressUtc > TimeSpan.FromMinutes(15))
                        throw new InvalidOperationException("更新15分钟没有进展，保持暂停；请检查战网后重启中控重试");
                }
            }
            catch (OperationCanceledException) when (session != null && session.IsCancellationRequested) { }
            catch (Exception ex)
            {
                bool retryable = ex is HttpRequestException || ex is OperationCanceledException ||
                    (ex is BattleNetAgentException && ((BattleNetAgentException)ex).Retryable);
                if (retryable && !m_submitting && ++m_failures < 3)
                    Out.Error("[升级]Agent查询失败，30秒后重试：" + ex.Message);
                else
                {
                    lock (m_lifecycleLock)
                    {
                        m_active = false;
                        m_timer?.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                    Out.Error("[升级]" + (m_submitting ? "提交结果不明确，停止自动重试。" : "自动更新失败。") +
                        ex.Message + "；炉石保持暂停，请处理后重启中控");
                }
            }
            finally
            {
                try
                {
                    lock (m_lifecycleLock)
                    {
                        if (!m_active)
                        {
                            completed = completed && session != null && !session.IsCancellationRequested;
                            Cleanup();
                            if (completed) m_callbackUpdateComplete?.Invoke();
                        }
                    }
                }
                catch (Exception ex) { Out.Error("[升级]结束更新失败：" + ex.Message); }
                finally { m_checkLock.Release(); }
            }
        }

        private void Cleanup()
        {
            lock (m_lifecycleLock)
            {
                m_agent?.Dispose();
                m_agent = null;
                m_installation = null;
                m_operation = m_progress = null;
                m_session?.Dispose();
                m_session = null;
            }
        }

        private static bool AreGamesRunning()
        {
            Process[] processes = Process.GetProcessesByName("Hearthstone");
            try { return processes.Length != 0; }
            finally { foreach (Process process in processes) process.Dispose(); }
        }
    }
}
