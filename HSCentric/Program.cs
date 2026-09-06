using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace HSCentric
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => MiniDump.Write();
            try
            {
                int port = 17321;
                string option = args.FirstOrDefault(a => a.StartsWith("--port="));
                if (option != null && (!int.TryParse(option.Substring(7), out port) || port < 1024 || port > 65535))
                    throw new ArgumentException("端口应在 1024–65535 之间。");
                string hostOption = args.FirstOrDefault(a => a.StartsWith("--host="));
                string host = hostOption == null ? "127.0.0.1" : hostOption.Substring(7);
                using (var runtime = new BackendRuntime(args.Contains("--safe-mode")))
                using (var server = new WebUiServer(runtime, port, host))
                {
                    server.Start();
                    runtime.Start();
                    Console.WriteLine("HSCentric 中控监听：" + server.ListenAddress);
                    Console.WriteLine("浏览器访问：" + server.Address);
                    Console.WriteLine("关闭浏览器后调度继续运行。按 Ctrl+C 停止后端。");
                    Console.CancelKeyPress += (sender, e) => { e.Cancel = true; runtime.RequestStop(); };
                    if (!args.Contains("--no-browser"))
                    {
                        try { Process.Start(new ProcessStartInfo(server.Address) { UseShellExecute = true }); }
                        catch (Exception) { Console.WriteLine("请在浏览器打开上述地址。"); }
                    }
                    runtime.WaitForStop();
                }
                return 0;
            }
            catch (Exception ex)
            {
                Out.Error("后端启动失败：" + ex.Message);
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }
    }
}
