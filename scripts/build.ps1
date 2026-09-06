param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$locator = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (!(Test-Path -LiteralPath $locator)) { throw '请安装 Visual Studio 或 Build Tools（.NET 桌面开发与 .NET Framework 4.8 targeting pack）。' }
$builder = & $locator -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (!$builder) { throw '未找到 MSBuild。' }
$outputPath = Join-Path $projectRoot ('artifacts\' + $Configuration.ToLowerInvariant())
$intermediatePath = Join-Path $projectRoot ('artifacts\obj-' + $Configuration.ToLowerInvariant())
& $builder (Join-Path $projectRoot 'HSCentric.sln') /t:Build "/p:Configuration=$Configuration" '/p:PostBuildEvent=' "/p:OutputPath=$outputPath\" "/p:IntermediateOutputPath=$intermediatePath\" /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw "构建失败：$LASTEXITCODE" }
Write-Output "完整运行目录：$outputPath"
Write-Output "启动：$outputPath\HSCentric.exe"
