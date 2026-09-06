using HSCentric.Const;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HSCentric
{
    internal sealed class WebUiServer : IDisposable
    {
        private readonly HttpListener listener = new HttpListener();
        private readonly BackendRuntime runtime;
        private readonly string csrf = Guid.NewGuid().ToString("N");
        private readonly string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebUI");
        private readonly JsonSerializerSettings json = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver(), DateTimeZoneHandling = DateTimeZoneHandling.Local };
        private readonly string authority;
        private readonly int port;
        internal string Address { get; }
        internal string ListenAddress { get; }

        internal WebUiServer(BackendRuntime runtime, int port, string host = "127.0.0.1")
        {
            this.runtime = runtime;
            this.port = port;
            if (port < 1024 || port > 65535) throw new ArgumentException("端口应在 1024–65535 之间。");
            if (string.IsNullOrWhiteSpace(host) || host != host.Trim())
                throw new ArgumentException("监听地址不能为空或包含首尾空格。");
            bool wildcard = host == "*" || host == "0.0.0.0";
            if (!wildcard && Uri.CheckHostName(host) == UriHostNameType.Unknown)
                throw new ArgumentException("监听地址应为 *、IP 地址或主机名，不包含协议、端口或路径。");
            Address = new UriBuilder(Uri.UriSchemeHttp, wildcard ? "127.0.0.1" : host, port).Uri.AbsoluteUri;
            ListenAddress = wildcard ? "http://*:" + port + "/" : Address;
            authority = wildcard ? null : new Uri(Address).Authority;
            listener.Prefixes.Add(ListenAddress);
        }
        internal void Start()
        {
            if (!File.Exists(Path.Combine(root, "index.html"))) throw new FileNotFoundException("缺少 WebUI/index.html，请完整复制发布目录。");
            listener.Start();
            Task.Run((Func<Task>)Accept);
        }
        private async Task Accept()
        {
            while (listener.IsListening)
            {
                try { var context = await listener.GetContextAsync(); _ = Task.Run(() => Handle(context)); }
                catch (HttpListenerException) { break; }
                catch (ObjectDisposedException) { break; }
            }
        }
        private void Handle(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;
                response.Headers["Cache-Control"] = "no-store";
                response.Headers["X-Content-Type-Options"] = "nosniff";
                response.Headers["Referrer-Policy"] = "no-referrer";
                response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
                ValidateOrigin(request);
                string path = request.Url.AbsolutePath;
                if (!path.StartsWith("/api/")) { ServeFile(context, path); return; }
                if (!runtime.Ready) throw new ApiException(503, "后端正在加载配置，请稍候重试。");
                if (request.HttpMethod != "GET" && request.Headers["X-HSCentric-Token"] != csrf) throw new ApiException(403, "页面会话已失效，请刷新后重试。");
                object result = HSUnitManager.Get().Access(units => Route(request, path, units));
                WriteJson(response, result);
            }
            catch (ApiException ex) { WriteError(context.Response, ex.Status, ex.Message); }
            catch (JsonException) { WriteError(context.Response, 400, "请求内容格式错误，请重新填写。"); }
            catch (Exception ex)
            {
                Out.Error("Web 请求处理失败：" + ex.GetType().Name);
                WriteError(context.Response, 500, "操作未完成，请检查文件权限和后端日志后重试。");
            }
            finally { try { context.Response.Close(); } catch { } }
        }

        private void ValidateOrigin(HttpListenerRequest request)
        {
            Uri requestedOrigin;
            if (!TryOrigin("http://" + request.UserHostName, out requestedOrigin) || requestedOrigin.Port != port ||
                !string.Equals(request.Url.Authority, requestedOrigin.Authority, StringComparison.OrdinalIgnoreCase) ||
                (authority != null && !string.Equals(requestedOrigin.Authority, authority, StringComparison.OrdinalIgnoreCase)))
                throw new ApiException(403, "请通过已配置的中控地址访问。");

            // A wildcard is a listener prefix, never a browser origin. Compare against the
            // actual request authority so LAN addresses work without allowing cross-site writes.
            string originHeader = request.Headers["Origin"];
            Uri origin;
            if (originHeader != null && (!TryOrigin(originHeader, out origin) ||
                !string.Equals(origin.GetLeftPart(UriPartial.Authority), requestedOrigin.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)))
                throw new ApiException(403, "不允许跨站请求。");
        }

        private static bool TryOrigin(string value, out Uri origin)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out origin) && origin.Scheme == Uri.UriSchemeHttp &&
                origin.Host.Length > 0 && origin.UserInfo.Length == 0 && origin.AbsolutePath == "/" &&
                origin.Query.Length == 0 && origin.Fragment.Length == 0;
        }

        private object Route(HttpListenerRequest request, string path, List<HSUnit> units)
        {
            string method = request.HttpMethod;
            if (path == "/api/state" && method == "GET")
                return new { sessionId = runtime.SessionId, now = DateTime.Now, nextCheck = runtime.NextCheck, checking = runtime.Checking, safeMode = runtime.SafeMode,
                    accounts = units.Select(Summary).ToArray() };
            if (path == "/api/meta" && method == "GET")
                return new { sessionId = runtime.SessionId, version = typeof(WebUiServer).Assembly.GetName().Version.ToString(), csrfToken = csrf, modes = Enum.GetNames(typeof(TASK_MODE)), strategies = Enum.GetNames(typeof(BEHAVIOR_MODE)), battleNetPath = runtime.BattleNetPath };
            if (path == "/api/startup" && method == "GET")
            {
                bool isAdministrator = LoginStartup.IsAdministrator();
                string disabledReason = !isAdministrator ? LoginStartup.AdministratorRequiredMessage :
                    runtime.SafeMode ? "安全模式不修改开机启动设置。" : null;
                try { return new { enabled = LoginStartup.IsEnabled(), canChange = disabledReason == null, disabledReason }; }
                catch (Exception ex)
                {
                    Out.Error("读取开机启动设置失败：" + ex);
                    if (!isAdministrator) throw new ApiException(403, LoginStartup.AdministratorRequiredMessage + " 当前启动状态未能读取。");
                    throw new ApiException(500, "无法读取开机启动设置，请查看后端日志，并检查运行权限和 Windows 任务计划程序服务。");
                }
            }
            if (path == "/api/startup" && method == "PUT")
            {
                if (runtime.SafeMode) throw new ApiException(409, "安全模式不修改开机启动设置。");
                if (!LoginStartup.IsAdministrator()) throw new ApiException(403, LoginStartup.AdministratorRequiredMessage);
                JToken enabled = Read<JObject>(request)["enabled"];
                if (enabled == null || enabled.Type != JTokenType.Boolean) throw new ApiException(400, "开机启动选项必须为布尔值。");
                try { LoginStartup.SetEnabled(enabled.Value<bool>()); }
                catch (Exception ex)
                {
                    Out.Error("保存开机启动设置失败：" + ex);
                    throw new ApiException(500, "开机启动设置未保存，请以管理员身份运行中控，并查看后端日志检查具体原因。");
                }
                return new { enabled = enabled.Value<bool>(), message = enabled.Value<bool>() ? "已添加开机启动，下次登录 Windows 后自动运行。" : "已取消开机启动。" };
            }
            if (path == "/api/logs" && method == "GET")
            {
                long after; long.TryParse(request.QueryString["after"], out after);
                return runtime.GetLogs(after).Select(log => new { log.Id, log.Time, log.Level, message = Redact(log.Message, units) }).ToArray();
            }
            if (path == "/api/settings" && method == "PUT")
            {
                string value = Read<JObject>(request).Value<string>("battleNetPath") ?? "";
                if (value.Length > 2048 || value.Any(char.IsControl)) throw new ApiException(400, "战网路径格式错误。");
                runtime.SaveSettings(value.Trim());
                return new { message = "战网路径已保存。" };
            }
            if (path == "/api/shutdown" && method == "POST")
            {
                // Allow the response to complete before the listener is disposed.
                Task.Run(async () => { await Task.Delay(500); runtime.RequestStop(); });
                return new { message = "正在停止后端并保存配置。" };
            }
            if (path == "/api/accounts" && method == "POST")
            {
                var input = Read<AccountInput>(request);
                var unit = input.Validate(null);
                if (units.Any(u => u.ID == unit.ID)) throw new ApiException(409, "账号 ID 已存在。");
                if (units.Any(u => u.HSModPort == unit.HSModPort)) throw new ApiException(409, "HSMod 端口已被其他账号使用。");
                units.Add(unit);
                try { HSUnitManager.Get().Release(); } catch { units.Remove(unit); throw; }
                Out.Info("[" + unit.ID + "] 已添加账号。");
                return AccountInput.From(unit);
            }
            string[] parts = path.Split('/');
            if (parts.Length >= 4 && parts[2] == "accounts")
            {
                string id = Uri.UnescapeDataString(parts[3]);
                int index = units.FindIndex(u => u.ID == id);
                if (index < 0) throw new ApiException(404, "账号不存在，可能已被删除，请刷新列表。");
                HSUnit existing = units[index];
                if (parts.Length == 4 && method == "GET") return AccountInput.From(existing);
                if (parts.Length == 4 && method == "PUT")
                {
                    var input = Read<AccountInput>(request);
                    if (input.Revision != AccountInput.Fingerprint(existing)) throw new ApiException(409, "账号配置已在其他窗口改变，请保留草稿内容并重新打开最新配置。");
                    var unit = input.Validate(existing);
                    if (units.Any(u => u.ID != id && u.HSModPort == unit.HSModPort)) throw new ApiException(409, "HSMod 端口已被其他账号使用。");
                    units[index] = unit;
                    try { HSUnitManager.Get().Release(); } catch { units[index] = existing; throw; }
                    Out.Info("[" + id + "] 配置已保存。");
                    return AccountInput.From(unit);
                }
                if (parts.Length == 4 && method == "DELETE")
                {
                    units.RemoveAt(index);
                    try { HSUnitManager.Get().Release(); } catch { units.Insert(index, existing); throw; }
                    ScheduledTaskManager.Instance.RemoveTask(id + "_pause");
                    Out.Info("[" + id + "] 已移除中控配置。");
                    return new { message = "账号已移除；已启动的游戏进程保持当前状态。" };
                }
                if (parts.Length == 5 && parts[4] == "actions" && method == "POST")
                    return Action(Read<JObject>(request), existing, index);
            }
            throw new ApiException(404, "接口不存在或请求方法不支持。");
        }

        private object Action(JObject input, HSUnit unit, int index)
        {
            string name = input.Value<string>("action");
            if (runtime.SafeMode && new[] { "start", "deploy", "disable-plugin" }.Contains(name))
                throw new ApiException(409, "安全模式不执行游戏进程或插件配置操作。");
            var manager = HSUnitManager.Get();
            switch (name)
            {
                case "enable": manager.SetEnable(unit.ID, true); break;
                case "disable": manager.SetEnable(unit.ID, false); break;
                case "pause": manager.SetPause(unit.ID); break;
                case "start": manager.StartOnce(index); break;
                case "deploy": manager.InitConfig(index); break;
                case "disable-plugin": manager.ReleaseConfig(index); break;
                case "reset-xp": manager.ResetXPRate(index); break;
                case "backup": return new { message = Backup(unit) };
                default: throw new ApiException(400, "不支持的账号操作。");
            }
            manager.Release();
            Out.Info("[" + unit.ID + "] 操作完成：" + name);
            return new { message = "操作已完成。" };
        }

        private static string Backup(HSUnit unit)
        {
            string source = Path.Combine(Path.GetDirectoryName(unit.HSPath) ?? "", "BepInEx", "config");
            if (!Directory.Exists(source)) throw new ApiException(400, "未找到炉石 BepInEx/config 目录，请核对炉石路径。");
            string[] files = Directory.GetFiles(source, "*.cfg");
            if (files.Length == 0) throw new ApiException(400, "插件目录没有可备份的 cfg 文件。");
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups", DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6));
            Directory.CreateDirectory(folder);
            foreach (string file in files) File.Copy(file, Path.Combine(folder, Path.GetFileName(file)));
            Out.Info("[" + unit.ID + "] 插件配置已备份：" + folder);
            return "已备份 " + files.Length + " 个插件配置至 " + folder;
        }

        private object Summary(HSUnit unit)
        {
            bool configured = unit.Tasks.GetTasks().Count > 0;
            TaskUnit task = configured ? unit.CurrentTask : null;
            bool running = !runtime.SafeMode && unit.IsProcessAlive();
            string status = !configured ? "未配置时段" : !unit.Enable ? "已停用" : running ? "运行中" : unit.IsActive() ? "等待启动" : "等待时段";
            return new { id = unit.ID, enable = unit.Enable, status, running, level = unit.XP.Level, xp = unit.XP.ProgressXP, totalXp = unit.XP.TotalXP,
                xpRate = unit.XPRate, pvpRate = unit.MercPvpRate, classicRate = unit.ClassicRate, currentTask = TaskInput.From(task),
                tasks = unit.Tasks.GetTasks().Select(TaskInput.From).ToArray(), switchTask = unit.Tasks.SwitchTask,
                wakeTime = configured && !runtime.SafeMode ? (DateTime?)unit.BasicConfigValue.mercCacheConfig.awakeTime : null };
        }

        private static string Redact(string text, List<HSUnit> units)
        {
            foreach (var unit in units) if (!string.IsNullOrEmpty(unit.Token)) text = text.Replace(unit.Token, "[已隐藏]");
            return text;
        }
        private T Read<T>(HttpListenerRequest request)
        {
            if (!(request.ContentType ?? "").StartsWith("application/json", StringComparison.OrdinalIgnoreCase)) throw new ApiException(415, "请求必须为 JSON。");
            if (request.ContentLength64 > 262144) throw new ApiException(413, "配置内容过大。");
            using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
            {
                char[] buffer = new char[262145];
                int count = 0, read;
                while (count < buffer.Length && (read = reader.Read(buffer, count, buffer.Length - count)) > 0) count += read;
                if (count == buffer.Length) throw new ApiException(413, "配置内容过大。");
                var value = JsonConvert.DeserializeObject<T>(new string(buffer, 0, count));
                if (value == null) throw new ApiException(400, "请求内容不能为空。");
                return value;
            }
        }
        private void ServeFile(HttpListenerContext context, string path)
        {
            if (context.Request.HttpMethod != "GET") throw new ApiException(405, "静态文件仅支持 GET。");
            // Explicit allowlist prevents exposing config, source, tokens, or traversal paths.
            string name = path == "/" ? "index.html" : path.TrimStart('/');
            var types = new Dictionary<string, string> { { "index.html", "text/html" }, { "styles.css", "text/css" }, { "app.js", "text/javascript" },
                { "fonts/noto-sans-sc-display.woff2", "font/woff2" }, { "icon.svg", "image/svg+xml" }, { "editor.js", "text/javascript" }, { "view.js", "text/javascript" }, { "api.js", "text/javascript" } };
            string type;
            if (!types.TryGetValue(name, out type)) throw new ApiException(404, "文件不存在。");
            byte[] data = File.ReadAllBytes(Path.Combine(root, name));
            context.Response.ContentType = type + "; charset=utf-8";
            context.Response.ContentLength64 = data.Length;
            context.Response.OutputStream.Write(data, 0, data.Length);
        }
        private void WriteJson(HttpListenerResponse response, object value)
        {
            byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value, json));
            response.ContentType = "application/json; charset=utf-8";
            response.ContentLength64 = data.Length;
            response.OutputStream.Write(data, 0, data.Length);
        }
        private void WriteError(HttpListenerResponse response, int status, string message)
        {
            try { response.StatusCode = status; WriteJson(response, new { error = message }); } catch { }
        }
        public void Dispose() { listener.Close(); }
    }
}
