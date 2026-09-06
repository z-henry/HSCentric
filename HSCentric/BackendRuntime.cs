using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;

namespace HSCentric
{
    internal sealed class BackendRuntime : IDisposable
    {
        private readonly ManualResetEvent stop = new ManualResetEvent(false);
        private readonly object logLock = new object();
        private readonly Queue<LogEntry> logs = new Queue<LogEntry>();
        private Thread worker;
        private long sequence;
        internal bool SafeMode { get; }
        internal string SessionId { get; } = Guid.NewGuid().ToString("N");
        internal string BattleNetPath { get; private set; }
        internal DateTime? NextCheck { get; private set; }
        internal bool Checking { get; private set; }
        internal bool Ready { get; private set; }

        internal BackendRuntime(bool safeMode)
        {
            SafeMode = safeMode;
            BattleNetPath = ConfigurationManager.AppSettings["bnet_path"] ?? "";
            Out.Logged += OnLog;
        }
        internal void Start()
        {
            UpdateManger.Get().Init(HSUnitManager.Get().RecoverAfterUpdated, BattleNetPath);
            HSUnitManager.Get().Init(UpdateManger.Get().Start, !SafeMode);
            if (!SafeMode)
            {
                MyRestFul.Init(ConfigurationManager.AppSettings["rest_url"]);
                worker = new Thread(Run) { IsBackground = true, Name = "HSCentric scheduler" };
                worker.Start();
            }
            Ready = true;
            Out.Info(SafeMode ? "安全模式：调度和游戏进程操作已停用。" : "Web 中控后端已启动。");
        }
        private void Run()
        {
            DateTime nextLog = DateTime.Now;
            while (!stop.WaitOne(0))
            {
                Checking = true;
                try
                {
                    HSUnitManager.Get().Check();
                    if (DateTime.Now >= nextLog)
                    {
                        HSUnitManager.Get().CheckLog();
                        nextLog = DateTime.Now.AddMinutes(1);
                    }
                }
                catch (Exception ex) { Out.Error("检测失败：" + ex.Message); }
                finally { Checking = false; NextCheck = DateTime.Now.AddSeconds(10); }
                if (stop.WaitOne(10000)) break;
            }
        }
        internal void SaveSettings(string path)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings["bnet_path"] == null) config.AppSettings.Settings.Add("bnet_path", path);
            else config.AppSettings.Settings["bnet_path"].Value = path;
            config.Save();
            BattleNetPath = path;
            UpdateManger.Get().Init(HSUnitManager.Get().RecoverAfterUpdated, path);
        }
        private void OnLog(string level, string message)
        {
            lock (logLock)
            {
                logs.Enqueue(new LogEntry { Id = ++sequence, Time = DateTime.Now, Level = level, Message = message });
                while (logs.Count > 500) logs.Dequeue();
            }
        }
        internal LogEntry[] GetLogs(long after)
        {
            lock (logLock) return logs.Where(l => l.Id > after).ToArray();
        }
        internal void RequestStop() { stop.Set(); }
        internal void WaitForStop() { stop.WaitOne(); }
        public void Dispose()
        {
            stop.Set();
            if (worker != null) worker.Join();
            MyRestFul.Rlease();
            if (Ready) HSUnitManager.Get().Release();
            Out.Logged -= OnLog;
            stop.Dispose();
        }
    }
    internal sealed class LogEntry
    {
        public long Id { get; set; }
        public DateTime Time { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
    }
}
