using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HSCentric
{
    internal interface IBattleNetAgent : IDisposable
    {
        Task<HearthstoneInstallation> FindHearthstoneAsync(string[] directories, CancellationToken token);
        Task<string> BeginUpdateAsync(HearthstoneInstallation installation, CancellationToken token);
        Task<AgentUpdateState> GetUpdateStateAsync(HearthstoneInstallation installation, string operation, CancellationToken token);
    }

    internal sealed class HearthstoneInstallation
    {
        internal string Uid;
        internal string Product;
        internal string Directory;
        internal string Region;
    }

    internal sealed class AgentUpdateState
    {
        internal bool IsComplete;
        internal double Progress;
        internal string Fingerprint;
        internal string LocalVersion;
    }

    internal sealed class BattleNetAgentException : Exception
    {
        internal bool Retryable { get; private set; }
        internal BattleNetAgentException(string message, bool retryable = false) : base(message) { Retryable = retryable; }
    }

    // Uses the installed Agent's private local API, not the Battle.net window.
    // Only registered Hearthstone installations may be updated; no install/repair requests.
    internal sealed class BattleNetAgentClient : IBattleNetAgent
    {
        private readonly HttpClient http;
        private readonly Process ownedAgent;

        internal BattleNetAgentClient(HttpClient http, Process ownedAgent = null)
        {
            this.http = http;
            this.ownedAgent = ownedAgent;
            http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "phoenix-agent/1.0");
        }

        internal static async Task<IBattleNetAgent> ConnectAsync(string battleNetPath, CancellationToken token)
        {
            if (!string.IsNullOrWhiteSpace(battleNetPath) && !File.Exists(battleNetPath))
                throw new BattleNetAgentException("战网启动程序路径无效，请检查运行设置");
            string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Battle.net", "Agent");
            string bootstrap = Path.Combine(root, "Agent.exe");
            if (!File.Exists(bootstrap)) throw new BattleNetAgentException("未找到战网Agent，请安装战网并登录一次");
            int version = FileVersionInfo.GetVersionInfo(bootstrap).ProductPrivatePart;
            string executable = Path.Combine(root, "Agent." + version, "Agent.exe");
            if (!File.Exists(executable)) throw new BattleNetAgentException("未找到当前版本的Agent，请更新战网并登录一次");

            Process process = null;
            BattleNetAgentClient client = null;
            try
            {
                token.ThrowIfCancellationRequested();
                process = Process.Start(new ProcessStartInfo(executable, "--internalclienttools")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(executable)
                });
                Stopwatch wait = Stopwatch.StartNew();
                int port = 0;
                while (port == 0 && wait.Elapsed < TimeSpan.FromSeconds(20))
                {
                    token.ThrowIfCancellationRequested();
                    if (process.HasExited) throw new BattleNetAgentException("Agent启动后退出，请打开战网并重新登录");
                    port = FindListeningPort(process.Id);
                    if (port == 0) await Task.Delay(250, token);
                }
                if (port == 0) throw new BattleNetAgentException("等待Agent本地接口超时", true);
                HttpClient http = new HttpClient(new HttpClientHandler { UseProxy = false, AllowAutoRedirect = false })
                {
                    BaseAddress = new Uri("http://127.0.0.1:" + port + "/"),
                    Timeout = TimeSpan.FromSeconds(10)
                };
                client = new BattleNetAgentClient(http, process);
                await client.AuthenticateAsync(token);
                Out.Info("[升级]已连接战网Agent，本地端口 " + port);
                return client;
            }
            catch
            {
                if (client != null) client.Dispose();
                else CloseOwnedAgent(process);
                throw;
            }
        }

        internal async Task AuthenticateAsync(CancellationToken token)
        {
            JObject state = await RequestAsync(HttpMethod.Get, "/agent", null, token);
            string authorization = (string)state["authorization"];
            if (string.IsNullOrWhiteSpace(authorization)) throw new BattleNetAgentException("Agent认证失败，请打开战网并登录");
            // Never write the authorization token or raw response to logs.
            http.DefaultRequestHeaders.Remove("Authorization");
            http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorization);
        }

        public async Task<HearthstoneInstallation> FindHearthstoneAsync(string[] directories, CancellationToken token)
        {
            string[] targets = (directories ?? new string[0]).Select(NormalizeDirectory)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (targets.Length == 0) throw new BattleNetAgentException("没有启用账号的炉石目录，无法确定升级目标");
            if (targets.Length != 1) throw new BattleNetAgentException("启用账号使用了多个炉石目录，当前自动更新仅支持共同使用一个战网安装目录");
            JObject games = await RequestAsync(HttpMethod.Get, "/game", null, token);
            HearthstoneInstallation selected = null;
            foreach (JProperty game in games.Properties().Where(p => p.Name == "hs_beta" || p.Name == "hs"))
            {
                string endpoint = ValidateEndpoint((string)game.Value["link"], "/game/" + game.Name);
                JObject state = await RequestAsync(HttpMethod.Get, endpoint, null, token);
                string directory = (string)state["install_dir"];
                if (state.Value<bool?>("installed") != true || string.IsNullOrWhiteSpace(directory) ||
                    !string.Equals(targets[0], NormalizeDirectory(directory), StringComparison.OrdinalIgnoreCase)) continue;
                string product = (string)state["product"];
                if (product != "hsb" && product != "hs") throw new BattleNetAgentException("Agent返回了未知的炉石产品代码，停止自动更新");
                if (!File.Exists(Path.Combine(directory, "Hearthstone.exe")))
                    throw new BattleNetAgentException("Agent记录的炉石程序不存在，请先在战网定位游戏");
                if (selected != null) throw new BattleNetAgentException("同一目录存在多个炉石产品记录，请先在战网确认安装版本");
                selected = new HearthstoneInstallation
                {
                    Uid = game.Name, Product = product, Directory = directory,
                    Region = (string)state["region"]
                };
            }
            if (selected == null) throw new BattleNetAgentException("中控炉石目录与战网安装记录不一致，请在战网定位该目录后重试");
            if (string.IsNullOrWhiteSpace(selected.Region)) throw new BattleNetAgentException("Agent未返回炉石地区，停止自动更新");
            return selected;
        }

        public async Task<string> BeginUpdateAsync(HearthstoneInstallation installation, CancellationToken token)
        {
            JObject refreshed = await RequestAsync(HttpMethod.Post, "/version", new JObject { ["uid"] = installation.Uid }, token);
            string versionEndpoint = ValidateEndpoint((string)refreshed["response_uri"], "/version/" + installation.Uid);
            Stopwatch wait = Stopwatch.StartNew();
            // State 1004 is the completed version/update state in this Agent's logs.
            while ((await RequestAsync(HttpMethod.Get, versionEndpoint, null, token)).Value<int?>("state") != 1004)
            {
                if (wait.Elapsed > TimeSpan.FromSeconds(60)) throw new BattleNetAgentException("Agent检查最新版本超时，请检查战网网络连接");
                await Task.Delay(1000, token);
            }
            JObject response = await RequestAsync(HttpMethod.Post, "/update", new JObject
            {
                ["uid"] = installation.Uid,
                ["priority"] = new JObject { ["insert_at_head"] = false, ["value"] = 699 }
            }, token);
            return ValidateEndpoint((string)response["response_uri"], "/update/" + installation.Uid);
        }

        public async Task<AgentUpdateState> GetUpdateStateAsync(HearthstoneInstallation installation, string operation, CancellationToken token)
        {
            ValidateEndpoint(operation, "/update/" + installation.Uid);
            JObject update = await RequestAsync(HttpMethod.Get, operation, null, token);
            JObject game = await RequestAsync(HttpMethod.Get, "/game/" + installation.Uid, null, token);
            if (!string.Equals(NormalizeDirectory((string)game["install_dir"]), NormalizeDirectory(installation.Directory), StringComparison.OrdinalIgnoreCase) ||
                (string)game["region"] != installation.Region || (string)game["product"] != installation.Product)
                throw new BattleNetAgentException("更新期间炉石安装目录、地区或产品发生变化，停止自动恢复");
            double progress = Math.Max(0, Math.Min(1, update.Value<double?>("progress") ?? 0));
            return new AgentUpdateState
            {
                Progress = progress,
                IsComplete = IsUpdateComplete(update, game, installation.Region),
                LocalVersion = (string)game["local_version"],
                Fingerprint = string.Join("|", new[] { update["state"], update["progress"],
                    update["download_remaining"], update["extended_status"]?["current"],
                    update["patch_application_complete"], game["active_config_key"] }.Select(t => t?.ToString(Formatting.None) ?? ""))
            };
        }

        internal static bool IsUpdateComplete(JObject update, JObject game, string region)
        {
            string active = (string)game["active_config_key"];
            string target = (string)game["regional_version_info"]?[region]?["config_key"];
            return update.Value<int?>("state") == 1004 && update.Value<bool?>("download_complete") == true &&
                update.Value<bool?>("patch_application_complete") == true && update.Value<bool?>("playable") == true &&
                game.Value<bool?>("installed") == true && game.Value<bool?>("download_complete") == true &&
                game.Value<bool?>("patch_application_complete") == true && game.Value<bool?>("playable") == true &&
                !string.IsNullOrWhiteSpace(active) && !string.IsNullOrWhiteSpace(target) && active == target;
        }

        private async Task<JObject> RequestAsync(HttpMethod method, string endpoint, JObject payload, CancellationToken token)
        {
            if (!endpoint.StartsWith("/", StringComparison.Ordinal) || endpoint.StartsWith("//", StringComparison.Ordinal))
                throw new BattleNetAgentException("Agent返回了无效的本地任务地址");
            using (HttpRequestMessage request = new HttpRequestMessage(method, endpoint))
            {
                if (payload != null) request.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");
                using (HttpResponseMessage response = await http.SendAsync(request, token))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                            throw new BattleNetAgentException("Agent认证被拒绝，请打开战网重新登录");
                        throw new BattleNetAgentException("Agent接口 " + endpoint + " 返回HTTP " + (int)response.StatusCode,
                            (int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.RequestTimeout);
                    }
                    JObject result;
                    try { result = JObject.Parse(await response.Content.ReadAsStringAsync()); }
                    catch (JsonException) { throw new BattleNetAgentException("Agent返回了无效的状态数据"); }
                    double error = result.Value<double?>("error") ?? 0;
                    if (error > 0) throw new BattleNetAgentException("Agent接口 " + endpoint + " 返回错误码 " + error);
                    return result;
                }
            }
        }

        internal static string ValidateEndpoint(string actual, string expected)
        {
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new BattleNetAgentException("Agent返回的任务地址与炉石目标不一致");
            return actual;
        }

        internal static string NormalizeDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new BattleNetAgentException("炉石安装目录为空");
            return Path.GetFullPath(directory.Replace('/', Path.DirectorySeparatorChar)).TrimEnd(Path.DirectorySeparatorChar);
        }

        private static int FindListeningPort(int processId)
        {
            int size = 0;
            GetTcpTable2(IntPtr.Zero, ref size, false);
            if (size <= 0) return 0;
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                if (GetTcpTable2(buffer, ref size, false) != 0) return 0;
                int count = Marshal.ReadInt32(buffer);
                int rowSize = Marshal.SizeOf(typeof(TcpRow));
                for (int i = 0; i < count; i++)
                {
                    TcpRow row = (TcpRow)Marshal.PtrToStructure(IntPtr.Add(buffer, 4 + i * rowSize), typeof(TcpRow));
                    if (row.ProcessId == processId && row.State == 2 &&
                        (row.LocalAddress == 0 || new IPAddress((uint)row.LocalAddress).Equals(IPAddress.Loopback)))
                        return ((row.LocalPort & 255) << 8) | ((row.LocalPort >> 8) & 255);
                }
            }
            finally { Marshal.FreeHGlobal(buffer); }
            return 0;
        }

        [DllImport("iphlpapi.dll")]
        private static extern uint GetTcpTable2(IntPtr table, ref int size, [MarshalAs(UnmanagedType.Bool)] bool ordered);

        [StructLayout(LayoutKind.Sequential)]
        private struct TcpRow
        {
            internal int State, LocalAddress, LocalPort, RemoteAddress, RemotePort, ProcessId, OffloadState;
        }

        private static void CloseOwnedAgent(Process process)
        {
            if (process == null) return;
            try { if (!process.HasExited) process.Kill(); }
            catch (Exception ex) { Out.Error("[升级]清理本次启动的Agent失败：" + ex.Message); }
            finally { process.Dispose(); }
        }

        public void Dispose()
        {
            http.Dispose();
            CloseOwnedAgent(ownedAgent);
        }
    }
}
