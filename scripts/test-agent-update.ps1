param(
    [switch]$LiveReadOnly,
    [string]$BattleNetExe = 'D:\Program Files (x86)\Battle.net\Battle.net.exe',
    [string]$GameDirectory = 'E:\Hearthstone'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $projectRoot 'artifacts\agent-tests'
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$locator = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$compiler = & $locator -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\Roslyn\csc.exe' | Select-Object -First 1
if (!$compiler) { throw 'C# compiler not found. Install Visual Studio / Build Tools.' }
$json = Join-Path $projectRoot 'packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll'
Copy-Item -LiteralPath $json -Destination $outputPath -Force
$executable = Join-Path $outputPath 'AgentUpdateTests.exe'
& $compiler /nologo /target:exe /r:System.Net.Http.dll "/r:$json" "/out:$executable" `
    (Join-Path $projectRoot 'HSCentric\BattleNetAgentClient.cs') `
    (Join-Path $projectRoot 'HSCentric\UpdateManger.cs') `
    (Join-Path $PSScriptRoot 'AgentUpdateTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Agent update tests failed to compile.' }
if ($LiveReadOnly) { & $executable --read-only $BattleNetExe $GameDirectory } else { & $executable }
if ($LASTEXITCODE -ne 0) { throw 'Agent update tests failed.' }
