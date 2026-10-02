param(
    [string]$BattleNetPath = 'D:\Program Files (x86)\Battle.net',
    [string]$ReportPath = (Join-Path $PSScriptRoot '..\artifacts\battle-net-agent-probe.json')
)

# Independent read-only probe. No POST/PUT/DELETE requests and no update tasks.
$ErrorActionPreference = 'Stop'
$taskStartedAgent = $null
$taskReport = [ordered]@{ Status = 'Checking'; BattleNetVersion = $null; AgentVersion = $null; Port = $null; Authenticated = $false; Games = @(); Failure = $null }

function Get-AgentJson([int]$taskPort, [string]$taskEndpoint, $taskHeaders) {
    Invoke-RestMethod -Method Get -Uri ('http://127.0.0.1:{0}/{1}' -f $taskPort, $taskEndpoint) -Headers $taskHeaders -TimeoutSec 5
}

try {
    $taskBattleExe = Join-Path $BattleNetPath 'Battle.net.exe'
    if (!(Test-Path -LiteralPath $taskBattleExe)) { throw 'Battle.net.exe was not found in the supplied directory.' }
    $taskReport.BattleNetVersion = (Get-Item -LiteralPath $taskBattleExe).VersionInfo.FileVersion

    $taskAgentRoot = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'Battle.net\Agent'
    $taskAgentBootstrap = Join-Path $taskAgentRoot 'Agent.exe'
    if (!(Test-Path -LiteralPath $taskAgentBootstrap)) { throw 'The installed Battle.net Agent was not found.' }
    $taskVersionInfo = (Get-Item -LiteralPath $taskAgentBootstrap).VersionInfo
    $taskReport.AgentVersion = $taskVersionInfo.FileVersion
    $taskAgentExe = Join-Path $taskAgentRoot ('Agent.{0}\Agent.exe' -f $taskVersionInfo.ProductPrivatePart)
    if (!(Test-Path -LiteralPath $taskAgentExe)) { throw 'The versioned Agent executable was not found.' }

    $taskAgents = @(Get-Process -Name Agent -ErrorAction SilentlyContinue | Where-Object {
        $_.Path -and $_.Path.StartsWith($taskAgentRoot + '\', [StringComparison]::OrdinalIgnoreCase)
    })
    if ($taskAgents.Count -eq 0) {
        $taskStartedAgent = Start-Process -FilePath $taskAgentExe -ArgumentList '--internalclienttools' -WindowStyle Hidden -PassThru
        $taskAgents = @($taskStartedAgent)
    }

    $taskDeadline = [DateTime]::UtcNow.AddSeconds(20)
    $taskListener = $null
    do {
        $taskAgentIds = @($taskAgents | Where-Object { !$_.HasExited } | ForEach-Object { $_.Id })
        if ($taskAgentIds.Count -eq 0) { throw 'Agent exited before opening its local API.' }
        # netstat avoids the CIM permissions required by Get-NetTCPConnection.
        # Match the owning PID; never assume port 1120 belongs to Battle.net.
        $taskListener = $null
        foreach ($taskTcpLine in (& netstat -ano -p tcp)) {
            if ($taskTcpLine -match '^\s*TCP\s+(\S+):(\d+)\s+\S+\s+LISTENING\s+(\d+)\s*$') {
                $taskAddress = $Matches[1].Trim('[', ']')
                $taskPort = [int]$Matches[2]
                $taskOwner = [int]$Matches[3]
                if ($taskAgentIds -contains $taskOwner -and $taskAddress -in @('127.0.0.1', '0.0.0.0', '::', '::1')) {
                    $taskListener = [pscustomobject]@{ LocalPort = $taskPort }
                    break
                }
            }
        }
        if (!$taskListener) { Start-Sleep -Milliseconds 500 }
    } while (!$taskListener -and [DateTime]::UtcNow -lt $taskDeadline)
    if (!$taskListener) { throw 'Agent did not expose a local listening port within 20 seconds.' }

    $taskReport.Port = $taskListener.LocalPort
    $taskHeaders = @{ 'User-Agent' = 'phoenix-agent/1.0' }
    $taskAgentState = $null
    $taskApiDeadline = [DateTime]::UtcNow.AddSeconds(10)
    do {
        try {
            $taskAgentState = Get-AgentJson $taskReport.Port 'agent' $taskHeaders
        } catch {
            if ([DateTime]::UtcNow -ge $taskApiDeadline) { throw }
            Start-Sleep -Milliseconds 500
        }
    } while (!$taskAgentState -and [DateTime]::UtcNow -lt $taskApiDeadline)
    if (!$taskAgentState) { throw 'Agent returned no state within 10 seconds.' }
    if (!$taskAgentState.authorization) { throw 'Agent did not return an authorization token. Sign in to Battle.net first.' }
    # Keep the token in memory only; never print it or include it in the report.
    $taskHeaders.Authorization = [string]$taskAgentState.authorization
    $taskReport.Authenticated = $true

    $taskGames = Get-AgentJson $taskReport.Port 'game' $taskHeaders
    foreach ($taskGameProperty in $taskGames.PSObject.Properties) {
        # Only inspect Hearthstone entries, avoiding unrelated games/account data.
        if ($taskGameProperty.Name -notmatch '^hs(?:_|$)|^hearthstone$') { continue }
        $taskLink = [string]$taskGameProperty.Value.link
        if ($taskLink -notmatch '^/game/[A-Za-z0-9_-]+$') { continue }
        $taskGame = Get-AgentJson $taskReport.Port $taskLink.TrimStart('/') $taskHeaders
        $taskCnState = $null
        if ($taskGame.regional_version_info) { $taskCnState = $taskGame.regional_version_info.cn }
        $taskReport.Games += [ordered]@{
            Uid = $taskGameProperty.Name
            Product = $taskGame.product
            InstallDirectory = $taskGame.install_dir
            Installed = $taskGame.installed
            Region = $taskGame.region
            LocalVersion = $taskGame.local_version
            CurrentVersion = $taskGame.current_version
            DownloadComplete = $taskGame.download_complete
            PatchApplicationComplete = $taskGame.patch_application_complete
            Playable = $taskGame.playable
            UpdateProgress = $taskGame.update_progress
            ChinaVersion = $(if ($taskCnState) { $taskCnState.display_version } else { $null })
            ChinaSelected = $(if ($taskCnState) { $taskCnState.selected } else { $null })
        }
    }
    if ($taskReport.Games.Count -eq 0) {
        $taskReport.Status = 'ConnectedButHearthstoneNotFound'
        $taskReport.Failure = 'The API is reachable, but no Hearthstone installation was returned.'
    } else {
        $taskReport.Status = 'ReadOnlyProbePassed'
    }
}
catch {
    $taskReport.Status = 'ProbeFailed'
    # Do not include response bodies: they may contain authorization/account data.
    if ($_.Exception -is [System.Net.WebException] -or $_.Exception.GetType().Name -match 'HttpResponse') {
        $taskReport.Failure = 'Local Agent HTTP request failed: ' + $_.Exception.GetType().Name
        if ($_.Exception -is [System.Net.WebException]) {
            $taskReport.Failure += ' (' + $_.Exception.Status + ')'
            if ($_.Exception.Response) { $taskReport.Failure += '; HTTP ' + [int]$_.Exception.Response.StatusCode }
        }
    } else {
        $taskReport.Failure = $_.Exception.Message
    }
}
finally {
    # Only stop the helper process this probe created. Existing Agents are left running.
    if ($taskStartedAgent) {
        $taskStartedAgent.Refresh()
        if (!$taskStartedAgent.HasExited) { Stop-Process -Id $taskStartedAgent.Id -ErrorAction SilentlyContinue }
        $taskStartedAgent.Dispose()
    }
    $taskReportDirectory = Split-Path -Parent ([IO.Path]::GetFullPath($ReportPath))
    New-Item -ItemType Directory -Path $taskReportDirectory -Force | Out-Null
    $taskReportJson = $taskReport | ConvertTo-Json -Depth 6
    [IO.File]::WriteAllText([IO.Path]::GetFullPath($ReportPath), $taskReportJson, [Text.UTF8Encoding]::new($false))
    Write-Output $taskReportJson
}
if ($taskReport.Status -ne 'ReadOnlyProbePassed') { exit 1 }
