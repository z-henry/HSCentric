param([switch]$Integration)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $projectRoot 'artifacts\startup-tests'
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$locator = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$compiler = & $locator -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\Roslyn\csc.exe' | Select-Object -First 1
if (!$compiler) { throw '未找到 C# 编译器，请安装 Visual Studio / Build Tools。' }

# Compile the production implementation with an isolated task name. Integration
# checks never change the user's startup task and never execute a scheduled action.
$taskName = 'HSCentric-Startup-Test-' + [Guid]::NewGuid().ToString('N')
$source = Get-Content -LiteralPath (Join-Path $projectRoot 'HSCentric\LoginStartup.cs') -Raw
$taskDeclaration = 'private const string TaskName = "HSCentric-WebUI-17321";'
if (!$source.Contains($taskDeclaration)) { throw '启动任务名称已变化，请更新测试隔离逻辑。' }
$source.Replace($taskDeclaration, ('private const string TaskName = "' + $taskName + '";')) |
    Set-Content -LiteralPath (Join-Path $outputPath 'LoginStartup.cs') -Encoding UTF8
@'
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using HSCentric;

public sealed class MissingTaskFolder
{
    public Exception Failure;
    public object GetTask(string name) { throw Failure; }
}

internal static class StartupTests
{
    private static void CheckFailure(Exception failure, bool missing)
    {
        var owner = typeof(LoginStartup);
        var objectsType = owner.GetNestedType("ComObjects", BindingFlags.NonPublic);
        using (var objects = (IDisposable)Activator.CreateInstance(objectsType, true))
        {
            var method = owner.GetMethod("FindTask", BindingFlags.Static | BindingFlags.NonPublic);
            try
            {
                var result = method.Invoke(null, new object[] { new MissingTaskFolder { Failure = failure }, objects });
                if (!missing || result != null) throw new Exception("Unexpected missing-task result.");
            }
            catch (TargetInvocationException ex)
            {
                if (missing || !ReferenceEquals(ex.InnerException, failure)) throw;
            }
        }
        Console.WriteLine("PASS " + failure.GetType().Name + " 0x" + failure.HResult.ToString("X8"));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Console.WriteLine("PASS " + message);
    }

    private static int Main(string[] args)
    {
        try
        {
            CheckFailure(new FileNotFoundException(), true);
            CheckFailure(new DirectoryNotFoundException(), true);
            CheckFailure(new COMException("missing file", unchecked((int)0x80070002)), true);
            CheckFailure(new COMException("missing path", unchecked((int)0x80070003)), true);
            CheckFailure(new UnauthorizedAccessException(), false);
            CheckFailure(new COMException("service unavailable", unchecked((int)0x800706BA)), false);
            CheckFailure(new InvalidOperationException("unexpected failure"), false);
            if (!LoginStartup.IsAdministrator())
            {
                foreach (bool enabled in new[] { true, false })
                {
                    try
                    {
                        LoginStartup.SetEnabled(enabled);
                        throw new Exception("A non-administrator must not change startup tasks.");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Require(ex.Message == LoginStartup.AdministratorRequiredMessage,
                            "non-administrator receives actionable instructions (enabled=" + enabled + ")");
                    }
                }
            }
            if (args.Length > 0 && args[0] == "--integration")
            {
                Require(LoginStartup.IsAdministrator(), "integration test requires an elevated process");
                Require(!LoginStartup.IsEnabled(), "absent task reads as disabled");
                try
                {
                    LoginStartup.SetEnabled(false);
                    Require(!LoginStartup.IsEnabled(), "disabling absent task is harmless");
                    LoginStartup.SetEnabled(true);
                    Require(LoginStartup.IsEnabled(), "registered task reads as enabled");
                    LoginStartup.SetEnabled(true);
                    Require(LoginStartup.IsEnabled(), "enabling twice remains enabled");
                }
                finally { LoginStartup.SetEnabled(false); }
                Require(!LoginStartup.IsEnabled(), "removed task reads as disabled");
            }
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
'@ | Set-Content -LiteralPath (Join-Path $outputPath 'StartupTests.cs') -Encoding UTF8

$executable = Join-Path $outputPath 'StartupTests.exe'
& $compiler /nologo /target:exe /r:Microsoft.CSharp.dll "/out:$executable" (Join-Path $outputPath 'LoginStartup.cs') (Join-Path $outputPath 'StartupTests.cs')
if ($LASTEXITCODE -ne 0) { throw '开机启动测试编译失败。' }
if ($Integration) { & $executable --integration } else { & $executable }
if ($LASTEXITCODE -ne 0) { throw '开机启动测试失败。' }
