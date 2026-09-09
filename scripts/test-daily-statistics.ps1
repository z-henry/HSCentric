$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$runtimePath = Join-Path $projectRoot ('artifacts\daily-tests-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory -Path $runtimePath | Out-Null
Copy-Item -Path (Join-Path $projectRoot 'artifacts\release\*') -Destination $runtimePath -Recurse
Copy-Item -LiteralPath (Join-Path $projectRoot 'HSCentric\App.config') -Destination (Join-Path $runtimePath 'DailyStatisticsTests.exe.config')
$locator = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$compiler = & $locator -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\Roslyn\csc.exe' | Select-Object -First 1
& $compiler /nologo /target:exe "/out:$runtimePath\DailyStatisticsTests.exe" "/reference:$runtimePath\HSCentric.exe" "/reference:$runtimePath\Newtonsoft.Json.dll" /reference:System.Configuration.dll (Join-Path $PSScriptRoot 'DailyStatisticsTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Daily statistics test compilation failed.' }
Push-Location $runtimePath
try { & .\DailyStatisticsTests.exe; if ($LASTEXITCODE -ne 0) { throw 'Daily statistics test failed.' } } finally { Pop-Location }
Set-Content -LiteralPath (Join-Path $projectRoot 'artifacts\daily-test-runtime.txt') -Value $runtimePath
Write-Output "Isolated test runtime: $runtimePath"
