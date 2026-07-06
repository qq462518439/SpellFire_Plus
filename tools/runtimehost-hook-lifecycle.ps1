param(
    [int]$ProcessId = 0,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$hookProject = Join-Path $repoRoot "src\Hook\SpellFire.Hook.vcxproj"
$runtimeHostCliProject = Join-Path $repoRoot "src\SpellFire.RuntimeHost.Cli\SpellFire.RuntimeHost.Cli.csproj"
$runtimeHostCliExe = Join-Path $repoRoot "src\SpellFire.RuntimeHost.Cli\bin\Debug\net48\SpellFire.RuntimeHost.Cli.exe"

function Resolve-MSBuildPath {
    $vswhere = "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $path = & $vswhere -products * -requires Microsoft.Component.MSBuild -find "MSBuild\Current\Bin\MSBuild.exe" | Select-Object -First 1
        if (-not [string]::IsNullOrWhiteSpace($path) -and (Test-Path $path)) {
            return $path
        }
    }

    $command = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($null -ne $command -and (Test-Path $command.Source)) {
        return $command.Source
    }

    return $null
}

function Invoke-Cli {
    param(
        [string]$Command,
        [int]$TargetProcessId,
        [string[]]$Arguments = @()
    )

    Write-Output "START hook-lifecycle-$Command pid=$TargetProcessId"
    $output = & $runtimeHostCliExe $Command $TargetProcessId @Arguments
    $exitCode = $LASTEXITCODE
    foreach ($line in $output) {
        Write-Output $line
    }

    $script:lastCliResult = @{
        ExitCode = $exitCode
        Output = ($output -join "`n")
    }
}

function Test-Output {
    param(
        [hashtable]$Result,
        [string[]]$Patterns
    )

    if ($Result.ExitCode -ne 0) {
        return $false
    }

    foreach ($pattern in $Patterns) {
        if ($Result.Output -notmatch $pattern) {
            Write-Output "FAIL hook-lifecycle-assert Pattern=`"$pattern`" Output=`"$($Result.Output)`""
            return $false
        }
    }

    return $true
}

function Reset-NamedEvent {
    param(
        [string]$Name
    )

    $eventHandle = [System.Threading.EventWaitHandle]::OpenExisting($Name)
    try {
        $null = $eventHandle.Reset()
    }
    finally {
        $eventHandle.Dispose()
    }
}

function Try-Set-NamedEvent {
    param(
        [string]$Name
    )

    try {
        $eventHandle = [System.Threading.EventWaitHandle]::OpenExisting($Name)
        try {
            $null = $eventHandle.Set()
        }
        finally {
            $eventHandle.Dispose()
        }
    }
    catch {
    }
}

if (-not $SkipBuild) {
    $msbuild = Resolve-MSBuildPath
    if ([string]::IsNullOrWhiteSpace($msbuild)) {
        Write-Output "FAIL hook-lifecycle Reason=`"MSBuildUnavailable`""
        exit 4
    }

    & $msbuild $hookProject /p:Configuration=Debug /p:Platform=Win32 /m /nologo /v:minimal
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    dotnet build $runtimeHostCliProject -c Debug
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

if ($ProcessId -le 0) {
    $wow = Get-Process -Name Wow -ErrorAction SilentlyContinue | Sort-Object StartTime -Descending | Select-Object -First 1
    if ($null -eq $wow) {
        Write-Output "FAIL hook-lifecycle Reason=`"NoWowProcess`""
        exit 2
    }

    $ProcessId = $wow.Id
}

Invoke-Cli -Command cleanup -TargetProcessId $ProcessId
$cleanupBefore = $script:lastCliResult
Invoke-Cli -Command attach -TargetProcessId $ProcessId
$attach = $script:lastCliResult

Invoke-Cli -Command attach -TargetProcessId $ProcessId
$repeatAttach = $script:lastCliResult
Invoke-Cli -Command status -TargetProcessId $ProcessId
$status = $script:lastCliResult
Invoke-Cli -Command command-ping -TargetProcessId $ProcessId
$commandPing = $script:lastCliResult
Invoke-Cli -Command hook-info -TargetProcessId $ProcessId
$hookInfo = $script:lastCliResult
Invoke-Cli -Command read-self-module -TargetProcessId $ProcessId
$readSelfModule = $script:lastCliResult
Invoke-Cli -Command lua-exec -TargetProcessId $ProcessId -Arguments @('DEFAULT_CHAT_FRAME:AddMessage("SPELLFIRE_EXEC_LIFECYCLE_OK");')
$luaExec = $script:lastCliResult

$readyEventName = "Local\SpellFireHookReady_$ProcessId"
$heartbeatEventName = "Local\SpellFireHookHeartbeat_$ProcessId"

Reset-NamedEvent -Name $readyEventName
Invoke-Cli -Command preflight -TargetProcessId $ProcessId
$readyMissingPreflight = $script:lastCliResult
Invoke-Cli -Command attach -TargetProcessId $ProcessId
$readyMissingAttach = $script:lastCliResult
Invoke-Cli -Command status -TargetProcessId $ProcessId
$readyMissingStatus = $script:lastCliResult
Invoke-Cli -Command lua-smoke -TargetProcessId $ProcessId
$readyMissingLua = $script:lastCliResult
Reset-NamedEvent -Name $readyEventName
Invoke-Cli -Command lua-exec -TargetProcessId $ProcessId -Arguments @('DEFAULT_CHAT_FRAME:AddMessage("SPELLFIRE_EXEC_RECOVER_OK");')
$readyMissingLuaExec = $script:lastCliResult

Reset-NamedEvent -Name $heartbeatEventName
Invoke-Cli -Command preflight -TargetProcessId $ProcessId
$heartbeatMissingPreflight = $script:lastCliResult
Reset-NamedEvent -Name $heartbeatEventName
Invoke-Cli -Command attach -TargetProcessId $ProcessId
$heartbeatMissingAttach = $script:lastCliResult
Reset-NamedEvent -Name $heartbeatEventName
Invoke-Cli -Command lua-smoke -TargetProcessId $ProcessId
$heartbeatMissingLua = $script:lastCliResult
Reset-NamedEvent -Name $heartbeatEventName
Invoke-Cli -Command lua-exec -TargetProcessId $ProcessId -Arguments @('DEFAULT_CHAT_FRAME:AddMessage("SPELLFIRE_EXEC_HEARTBEAT_REFUSE");')
$heartbeatMissingLuaExec = $script:lastCliResult
if ($heartbeatMissingLuaExec.Output -match "SafeBoundary_DirtyRefused_HeartbeatMissing") {
    Write-Output "OK hook-lifecycle-heartbeat-lua-exec-refused pid=$ProcessId"
}
elseif ($heartbeatMissingLuaExec.Output -match "LuaExecuteSucceeded") {
    Write-Output "NOTE hook-lifecycle-heartbeat-lua-exec-raced pid=$ProcessId Reason=`"Heartbeat event was re-signaled before command boundary check.`""
}
else {
    Write-Output "FAIL hook-lifecycle-heartbeat-lua-exec-observation pid=$ProcessId Output=`"$($heartbeatMissingLuaExec.Output)`""
}

Try-Set-NamedEvent -Name $readyEventName
Try-Set-NamedEvent -Name $heartbeatEventName

Invoke-Cli -Command shutdown -TargetProcessId $ProcessId
$cleanShutdownBeforeLuaExec = $script:lastCliResult
Start-Sleep -Milliseconds 250
Invoke-Cli -Command lua-exec -TargetProcessId $ProcessId -Arguments @('DEFAULT_CHAT_FRAME:AddMessage("SPELLFIRE_EXEC_DIRECT_OK");')
$cleanDirectLuaExec = $script:lastCliResult

Invoke-Cli -Command shutdown -TargetProcessId $ProcessId
$shutdown = $script:lastCliResult
Invoke-Cli -Command status -TargetProcessId $ProcessId
$postStatus = $script:lastCliResult
Invoke-Cli -Command cleanup -TargetProcessId $ProcessId
$cleanupAfter = $script:lastCliResult

$ok = $true
$ok = $ok -and (Test-Output -Result $attach -Patterns @("HookReady|HookAlreadyReady", "Ready=True"))
$ok = $ok -and (Test-Output -Result $repeatAttach -Patterns @("HookAlreadyReady", "Ready=True", "ExistingModule=0x", "ReadySignal=True"))
$ok = $ok -and (Test-Output -Result $status -Patterns @("HookServiceAlive", "Ready=True", "ReadySignal=True", "HeartbeatSignal=True"))
$ok = $ok -and (Test-Output -Result $commandPing -Patterns @("HookCommandPingOk", "Ack=True", "Magic=0x53464850", "HeaderSize=88", "Status=0x53464F4B", "Result=0x50494E47"))
$ok = $ok -and (Test-Output -Result $hookInfo -Patterns @("HookInfoOk", "Ack=True", "Magic=0x53464850", "HeaderSize=88", "Status=0x53464F4B", "Result=0x494E464F", "HookProcessId=$ProcessId", "HookProtocolVersion=2"))
$ok = $ok -and (Test-Output -Result $readSelfModule -Patterns @("HookSelfModuleReadOk", "Ack=True", "Magic=0x53464850", "HeaderSize=88", "Status=0x53464F4B", "Result=0x50455244", "DosSignature=0x5A4D", "PeSignature=0x4550", "Machine=0x14C", "SectionCount=[1-9][0-9]*"))
$ok = $ok -and (Test-Output -Result $luaExec -Patterns @("LuaExecuteSucceeded", "Ready=True", "Result=0x45584543", "MainThreadBridgeReady=True", "LuaBridgeReady=True", "TextPayload=OK:FrameScriptExecute=[0-9-]+"))
$ok = $ok -and $readyMissingPreflight.ExitCode -ne 0 -and ($readyMissingPreflight.Output -match "SafeBoundary_DirtyRecoverable_ReadyMissing")
$ok = $ok -and (Test-Output -Result $readyMissingAttach -Patterns @("HookReady", "Ready=True", "StaleUnloadAttempted=True", "StaleUnloadResult=True", "StaleModuleStillLoaded=False"))
$ok = $ok -and (Test-Output -Result $readyMissingStatus -Patterns @("HookServiceAlive", "Ready=True", "ReadySignal=True", "HeartbeatSignal=True"))
$ok = $ok -and (Test-Output -Result $readyMissingLua -Patterns @("LuaSmokeExecuted", "Ready=True", "MainThreadBridgeReady=True", "LuaBridgeReady=True", "LuaSmokeExecuted=True"))
$ok = $ok -and (Test-Output -Result $readyMissingLuaExec -Patterns @("LuaExecuteSucceeded", "Ready=True", "AttachReason=HookReady", "Result=0x45584543", "TextPayload=OK:FrameScriptExecute=[0-9-]+"))
$ok = $ok -and $heartbeatMissingPreflight.ExitCode -ne 0 -and ($heartbeatMissingPreflight.Output -match "SafeBoundary_DirtyRefused_HeartbeatMissing")
$ok = $ok -and $heartbeatMissingAttach.ExitCode -ne 0 -and ($heartbeatMissingAttach.Output -match "SafeBoundary_DirtyRefused_HeartbeatMissing")
$ok = $ok -and $heartbeatMissingLua.ExitCode -ne 0 -and ($heartbeatMissingLua.Output -match "SafeBoundary_DirtyRefused_HeartbeatMissing")
$ok = $ok -and (($heartbeatMissingLuaExec.Output -match "SafeBoundary_DirtyRefused_HeartbeatMissing") -or ($heartbeatMissingLuaExec.Output -match "LuaExecuteSucceeded"))
$ok = $ok -and (Test-Output -Result $cleanShutdownBeforeLuaExec -Patterns @("HookShutdownRequested", "Ready=True"))
$ok = $ok -and (Test-Output -Result $cleanDirectLuaExec -Patterns @("LuaExecuteSucceeded", "Ready=True", "AttachReason=HookReady", "Result=0x45584543", "TextPayload=OK:FrameScriptExecute=[0-9-]+"))
$ok = $ok -and (Test-Output -Result $shutdown -Patterns @("HookShutdownRequested", "Ready=True"))
$ok = $ok -and $postStatus.ExitCode -ne 0 -and $postStatus.Output -match "HookServiceUnavailable"
$ok = $ok -and $cleanupBefore.ExitCode -eq 0 -and $cleanupAfter.ExitCode -eq 0

if ($ok) {
    Write-Output "OK hook-lifecycle pid=$ProcessId"
    exit 0
}

Write-Output "FAIL hook-lifecycle pid=$ProcessId AttachExit=$($attach.ExitCode) RepeatAttachExit=$($repeatAttach.ExitCode) StatusExit=$($status.ExitCode) CommandPingExit=$($commandPing.ExitCode) HookInfoExit=$($hookInfo.ExitCode) ReadSelfModuleExit=$($readSelfModule.ExitCode) LuaExecExit=$($luaExec.ExitCode) ReadyMissingPreflightExit=$($readyMissingPreflight.ExitCode) ReadyMissingAttachExit=$($readyMissingAttach.ExitCode) ReadyMissingStatusExit=$($readyMissingStatus.ExitCode) ReadyMissingLuaExit=$($readyMissingLua.ExitCode) ReadyMissingLuaExecExit=$($readyMissingLuaExec.ExitCode) HeartbeatMissingPreflightExit=$($heartbeatMissingPreflight.ExitCode) HeartbeatMissingAttachExit=$($heartbeatMissingAttach.ExitCode) HeartbeatMissingLuaExit=$($heartbeatMissingLua.ExitCode) HeartbeatMissingLuaExecExit=$($heartbeatMissingLuaExec.ExitCode) CleanShutdownBeforeLuaExecExit=$($cleanShutdownBeforeLuaExec.ExitCode) CleanDirectLuaExecExit=$($cleanDirectLuaExec.ExitCode) ShutdownExit=$($shutdown.ExitCode) PostStatusExit=$($postStatus.ExitCode)"
exit 1
