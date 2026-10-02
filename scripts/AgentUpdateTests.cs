// Compile the production updater unchanged. Only scheduler/logging dependencies
// are stubbed, so the tests cannot launch games or edit account configuration.
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using HSCentric;

namespace HSCentric
{
    internal static class Out
    {
        internal static readonly List<string> Messages = new List<string>();
        internal static void Info(string message) { Messages.Add(message); }
        internal static void Debug(string message) { Messages.Add(message); }
        internal static void Error(string message) { Messages.Add(message); }
    }
    internal class HSUnit { internal bool Enable = false; internal string HSPath = ""; }
    internal class HSUnitManager
    {
        internal static HSUnitManager Get() { return new HSUnitManager(); }
        internal T Access<T>(Func<List<HSUnit>, T> action) { return action(new List<HSUnit>()); }
    }
}

internal static class AgentUpdateTests
{
    private static readonly CancellationToken None = CancellationToken.None;
    private static int assertions;
    private static void Require(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        assertions++;
        Console.WriteLine("PASS " + description);
    }
    private static async Task Reject(Func<Task> action, string description)
    {
        try { await action(); }
        catch (BattleNetAgentException) { Require(true, description); return; }
        throw new Exception(description + " was accepted");
    }
    private static JObject Game(string directory)
    {
        return new JObject
        {
            ["product"] = "hsb", ["install_dir"] = directory, ["region"] = "cn",
            ["installed"] = true, ["download_complete"] = true,
            ["patch_application_complete"] = true, ["playable"] = true,
            ["active_config_key"] = "current-cn", ["local_version"] = "36.6.3",
            ["regional_version_info"] = new JObject
            {
                ["cn"] = new JObject { ["config_key"] = "current-cn" },
                ["us"] = new JObject { ["config_key"] = "different-us" }
            }
        };
    }
    private static JObject Update()
    {
        return new JObject { ["state"] = 1004, ["progress"] = 1,
            ["download_complete"] = true, ["patch_application_complete"] = true, ["playable"] = true };
    }

    private sealed class QueueHandler : HttpMessageHandler
    {
        internal readonly Queue<Func<HttpRequestMessage, Task<HttpResponseMessage>>> Steps =
            new Queue<Func<HttpRequestMessage, Task<HttpResponseMessage>>>();
        internal void Expect(string method, string path, JObject response,
            Action<HttpRequestMessage> inspect = null, HttpStatusCode status = HttpStatusCode.OK)
        {
            Steps.Enqueue(async request =>
            {
                if (request.Method.Method != method || request.RequestUri.AbsolutePath != path)
                    throw new Exception("Unexpected request: " + request.Method + " " + request.RequestUri);
                if (request.Content != null) await request.Content.LoadIntoBufferAsync();
                inspect?.Invoke(request);
                return new HttpResponseMessage(status) { Content = new StringContent(response.ToString()) };
            });
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (Steps.Count == 0) throw new Exception("Unexpected extra Agent request");
            return Steps.Dequeue()(request);
        }
        internal BattleNetAgentClient Client()
        {
            return new BattleNetAgentClient(new HttpClient(this) { BaseAddress = new Uri("http://127.0.0.1:12345/") });
        }
    }

    private static async Task ProtocolTests(string directory)
    {
        var handler = new QueueHandler();
        handler.Expect("GET", "/agent", new JObject { ["authorization"] = "test-secret" });
        handler.Expect("GET", "/game", new JObject { ["hs_beta"] = new JObject { ["link"] = "/game/hs_beta" } },
            request => Require(string.Join("", request.Headers.GetValues("Authorization")) == "test-secret",
                "Agent authorization is sent after handshake"));
        handler.Expect("GET", "/game/hs_beta", Game(directory));
        handler.Expect("POST", "/version", new JObject { ["response_uri"] = "/version/hs_beta" }, request =>
            Require(JObject.Parse(request.Content.ReadAsStringAsync().Result).Value<string>("uid") == "hs_beta",
                "version refresh targets registered Hearthstone"));
        handler.Expect("GET", "/version/hs_beta", new JObject { ["state"] = 1004 });
        handler.Expect("POST", "/update", new JObject { ["response_uri"] = "/update/hs_beta" }, request =>
        {
            JObject body = JObject.Parse(request.Content.ReadAsStringAsync().Result);
            Require(body.Value<string>("uid") == "hs_beta" && body.Count == 2 &&
                body["priority"].Value<int>("value") == 699 && body["priority"].Value<bool>("insert_at_head") == false,
                "update payload preserves install path, region and language");
        });
        handler.Expect("GET", "/update/hs_beta", Update());
        handler.Expect("GET", "/game/hs_beta", Game(directory));
        using (var client = handler.Client())
        {
            await client.AuthenticateAsync(None);
            var installation = await client.FindHearthstoneAsync(new[] { directory, directory.ToUpperInvariant() + "\\" }, None);
            Require(installation.Region == "cn" && installation.Product == "hsb", "matching shared directory selects China installation");
            string operation = await client.BeginUpdateAsync(installation, None);
            Require((await client.GetUpdateStateAsync(installation, operation, None)).IsComplete, "confirmed patch restores eligible completion");
            Require(handler.Steps.Count == 0, "protocol uses only expected version and update endpoints");
        }
        Require(!Out.Messages.Exists(m => m.Contains("test-secret")), "authorization token is never logged");

        handler = new QueueHandler();
        using (var client = handler.Client())
            await Reject(() => client.FindHearthstoneAsync(new[] { directory, directory + "-other" }, None),
                "different account directories are rejected before any Agent request");

        handler = new QueueHandler();
        handler.Expect("GET", "/game", new JObject { ["hs_beta"] = new JObject { ["link"] = "/game/hs_beta" } });
        handler.Expect("GET", "/game/hs_beta", Game(directory + "-other"));
        using (var client = handler.Client())
            await Reject(() => client.FindHearthstoneAsync(new[] { directory }, None), "wrong registered directory cannot be updated");

        handler = new QueueHandler();
        handler.Expect("POST", "/version", new JObject { ["response_uri"] = "http://external.invalid/version/hs_beta" });
        using (var client = handler.Client())
            await Reject(() => client.BeginUpdateAsync(new HearthstoneInstallation { Uid = "hs_beta" }, None),
                "external operation address is rejected before update submission");

        handler = new QueueHandler();
        handler.Expect("GET", "/agent", new JObject { ["authorization"] = "secret-in-error" }, status: HttpStatusCode.Unauthorized);
        using (var client = handler.Client())
        {
            try { await client.AuthenticateAsync(None); throw new Exception("401 accepted"); }
            catch (BattleNetAgentException ex)
            {
                Require(!ex.Retryable && !ex.Message.Contains("secret-in-error"), "authentication refusal is actionable and hides response secrets");
            }
        }
        handler = new QueueHandler();
        handler.Expect("GET", "/agent", new JObject { ["error"] = 2, ["authorization"] = "unused" });
        using (var client = handler.Client())
            await Reject(() => client.AuthenticateAsync(None), "Agent application errors cannot be treated as success");

        handler = new QueueHandler();
        handler.Expect("GET", "/update/hs_beta", Update());
        JObject changed = Game(directory); changed["region"] = "us";
        handler.Expect("GET", "/game/hs_beta", changed);
        using (var client = handler.Client())
            await Reject(() => client.GetUpdateStateAsync(new HearthstoneInstallation
                { Uid = "hs_beta", Directory = directory, Product = "hsb", Region = "cn" }, "/update/hs_beta", None),
                "region changes during update prevent automatic resume");
    }

    private static void CompletionTests(string directory)
    {
        foreach (string flag in new[] { "download_complete", "patch_application_complete", "playable" })
        {
            JObject update = Update(); update[flag] = false;
            Require(!BattleNetAgentClient.IsUpdateComplete(update, Game(directory), "cn"), "100 percent with update " + flag + " false stays paused");
            JObject game = Game(directory); game[flag] = false;
            Require(!BattleNetAgentClient.IsUpdateComplete(Update(), game, "cn"), "100 percent with game " + flag + " false stays paused");
        }
        JObject incomplete = Update(); incomplete["state"] = 1003;
        Require(!BattleNetAgentClient.IsUpdateComplete(incomplete, Game(directory), "cn"), "in-progress task cannot restore scheduling");
        JObject old = Game(directory); old["active_config_key"] = "old-cn";
        Require(!BattleNetAgentClient.IsUpdateComplete(Update(), old, "cn"), "old active configuration prevents false success");
        JObject missing = Game(directory); missing.Remove("regional_version_info");
        Require(!BattleNetAgentClient.IsUpdateComplete(Update(), missing, "cn"), "missing regional version data is not success");
        Require(BattleNetAgentClient.IsUpdateComplete(Update(), Game(directory), "cn"), "China target configuration confirms completion independently of US version");
    }

    private sealed class FakeAgent : IBattleNetAgent
    {
        internal int Submissions, Polls, Disposals;
        internal Func<CancellationToken, Task<string>> Submit;
        internal Func<CancellationToken, Task<AgentUpdateState>> Poll;
        public Task<HearthstoneInstallation> FindHearthstoneAsync(string[] directories, CancellationToken token)
        { return Task.FromResult(new HearthstoneInstallation { Uid = "hs_beta", Directory = directories[0], Region = "cn", Product = "hsb" }); }
        public Task<string> BeginUpdateAsync(HearthstoneInstallation installation, CancellationToken token)
        { Submissions++; return Submit == null ? Task.FromResult("/update/hs_beta") : Submit(token); }
        public Task<AgentUpdateState> GetUpdateStateAsync(HearthstoneInstallation installation, string operation, CancellationToken token)
        { Polls++; return Poll == null ? Task.FromResult(new AgentUpdateState { Progress = 1, Fingerprint = "waiting" }) : Poll(token); }
        public void Dispose() { Disposals++; }
    }
    private static UpdateManger Manager(FakeAgent agent, Action completed, Func<bool> running = null)
    {
        var manager = new UpdateManger((path, token) => Task.FromResult<IBattleNetAgent>(agent),
            () => new[] { "C:\\test-game" }, running ?? (() => false));
        manager.Init(completed, "");
        manager.Start();
        return manager;
    }
    private static void SetTime(UpdateManger manager, string field, DateTime value)
    { typeof(UpdateManger).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(manager, value); }

    private static async Task LifecycleTests()
    {
        int resumed = 0;
        var agent = new FakeAgent();
        var manager = Manager(agent, () => { Require(agent.Disposals == 1, "helper cleanup precedes scheduler resume"); resumed++; });
        try
        {
            manager.Start(); await manager.CheckAsync(); await manager.CheckAsync();
            Require(agent.Submissions == 1 && resumed == 0, "duplicate triggers and 100 percent progress neither resubmit nor resume");
            agent.Poll = token => Task.FromResult(new AgentUpdateState { IsComplete = true, LocalVersion = "new", Fingerprint = "done" });
            await manager.CheckAsync(); await manager.CheckAsync();
            Require(resumed == 1, "successful update invokes existing resume callback exactly once");
        }
        finally { manager.Stop(); }

        agent = new FakeAgent(); manager = Manager(agent, () => resumed++, () => true);
        try { await manager.CheckAsync(); Require(agent.Submissions == 0, "running Hearthstone prevents update submission"); }
        finally { manager.Stop(); }

        agent = new FakeAgent { Poll = token => { throw new HttpRequestException("connection interrupted"); } };
        manager = Manager(agent, () => resumed++);
        try
        {
            int before = resumed;
            await manager.CheckAsync(); await manager.CheckAsync(); await manager.CheckAsync(); await manager.CheckAsync();
            Require(agent.Submissions == 1 && agent.Polls == 3 && agent.Disposals == 1 && resumed == before,
                "three failed polls pause and clean up without resubmission or resume");
        }
        finally { manager.Stop(); }

        agent = new FakeAgent { Submit = token => { throw new HttpRequestException("response lost after POST"); } };
        manager = Manager(agent, () => resumed++);
        try
        {
            await manager.CheckAsync(); await manager.CheckAsync();
            Require(agent.Submissions == 1 && agent.Polls == 0 && agent.Disposals == 1,
                "ambiguous submission is never automatically repeated");
        }
        finally { manager.Stop(); }

        var entered = new TaskCompletionSource<bool>();
        agent = new FakeAgent { Poll = async token => { entered.SetResult(true); await Task.Delay(Timeout.Infinite, token); return null; } };
        manager = Manager(agent, () => resumed++);
        int prior = resumed;
        Task checking = manager.CheckAsync(); await entered.Task;
        await manager.CheckAsync();
        Require(agent.Polls == 1, "overlapping timer polls are serialized");
        manager.Stop();
        Require(agent.Disposals == 1, "backend stop waits for cancelled helper cleanup before returning");
        await checking;
        Require(agent.Disposals == 1 && resumed == prior, "stopping during HTTP wait cancels and never resumes games");

        agent = new FakeAgent(); manager = Manager(agent, () => resumed++);
        try
        {
            await manager.CheckAsync(); SetTime(manager, "m_lastProgressUtc", DateTime.UtcNow.AddMinutes(-16));
            await manager.CheckAsync();
            Require(agent.Disposals == 1 && resumed == prior, "stalled update stays paused after inactivity timeout");
        }
        finally { manager.Stop(); }

        agent = new FakeAgent(); manager = Manager(agent, () => resumed++);
        try
        {
            SetTime(manager, "m_startedUtc", DateTime.UtcNow.AddHours(-3)); await manager.CheckAsync();
            Require(agent.Submissions == 0 && resumed == prior, "overall timeout cannot submit or resume an update");
        }
        finally { manager.Stop(); }
    }

    private static async Task Run(string[] args)
    {
        if (args.Length == 3 && args[0] == "--read-only")
        {
            using (var agent = await BattleNetAgentClient.ConnectAsync(args[1], None))
            {
                var installation = await agent.FindHearthstoneAsync(new[] { args[2] }, None);
                Require(installation != null, "production C# client connects and reads the actual installed game (no POST)");
                Console.WriteLine("UID=" + installation.Uid + "; product=" + installation.Product +
                    "; region=" + installation.Region + "; directory=" + installation.Directory);
                using (var second = await BattleNetAgentClient.ConnectAsync(args[1], None))
                    Require(await second.FindHearthstoneAsync(new[] { args[2] }, None) != null,
                        "a second helper can coexist with an existing Agent");
                Require(await agent.FindHearthstoneAsync(new[] { args[2] }, None) != null,
                    "disposing the second helper leaves the existing Agent usable");
            }
            return;
        }
        string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "game-fixture");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "Hearthstone.exe"), "test fixture, not an executable");
        CompletionTests(directory);
        await ProtocolTests(directory);
        await LifecycleTests();
        Console.WriteLine("Passed " + assertions + " assertions; no real update tasks or games started.");
    }
    private static int Main(string[] args)
    {
        try { Run(args).GetAwaiter().GetResult(); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
