param(
    [Parameter(Mandatory = $true)]
    [string]$Command,

    [int]$ProcessId = 0,

    [string]$Script = ''
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$runtimeDll = Join-Path $repoRoot "src\SpellFire.Runtime\bin\Debug\net48\SpellFire.Runtime.dll"
$runtimeHostExe = Join-Path $repoRoot "src\SpellFire.RuntimeHost\bin\Debug\net48\SpellFire.RuntimeHost.exe"
$memoryRobotDll = Join-Path $repoRoot "src\SpellFire.MemoryRobot\bin\Debug\net48\SpellFire.MemoryRobot.dll"

function Write-Operation {
    param(
        [string]$Name,
        [object]$Result
    )

    if ($null -eq $Result) {
        Write-Output "FAIL runtime-facade Command=`"$Name`" Ready=False Reason=`"ResultNull`""
        exit 1
    }

    $componentsCount = 0
    if ($null -ne $Result.Components) {
        $componentsCount = $Result.Components.Count
    }

    $prefix = if ($Result.Ready) { "OK" } else { "FAIL" }
    Write-Output ($prefix +
        " runtime-facade Command=`"$Name`"" +
        " ProcessId=" + $Result.ProcessId +
        " Operation=`"" + $Result.Operation + "`"" +
        " Ready=" + $Result.Ready +
        " Reason=`"" + $Result.Reason + "`"" +
        " HostState=`"" + $Result.HostState + "`"" +
        " Components=" + $componentsCount +
        " Detail=`"" + (($Result.Detail -as [string]) -replace "`r|`n", " ") + "`"")
    exit $(if ($Result.Ready) { 0 } else { 1 })
}

if (-not (Test-Path $runtimeDll) -or -not (Test-Path $runtimeHostExe) -or -not (Test-Path $memoryRobotDll)) {
    Write-Output "FAIL runtime-facade Command=`"$Command`" Ready=False Reason=`"BuildOutputMissing`""
    exit 2
}

[System.Reflection.Assembly]::LoadFrom($memoryRobotDll) | Out-Null
[System.Reflection.Assembly]::LoadFrom($runtimeHostExe) | Out-Null
[System.Reflection.Assembly]::LoadFrom($runtimeDll) | Out-Null

$facade = New-Object SpellFire.Runtime.RuntimeFacade
$normalized = $Command.Trim().ToLowerInvariant()

switch ($normalized) {
    "probe-memory" {
        $result = $facade.ProbeMemory($ProcessId)
        $prefix = if ($result.Ready) { "OK" } else { "FAIL" }
        Write-Output ($prefix +
            " runtime-facade Command=`"probe-memory`"" +
            " ProcessId=" + $result.ProcessId +
            " Ready=" + $result.Ready +
            " Reason=`"" + $result.Reason + "`"" +
            " ProcessFound=" + $result.ProcessFound +
            " ProcessName=`"" + $result.ProcessName + "`"" +
            " TargetWow64Known=" + $result.TargetWow64Known +
            " TargetWow64=" + $result.TargetWow64 +
            " Win32Error=" + $result.Win32Error +
            " Win32Message=`"" + $result.Win32Message + "`"")
        exit $(if ($result.Ready) { 0 } else { 1 })
    }
    "attach" {
        $result = $facade.Attach($ProcessId)
        $componentsCount = if ($null -eq $result.Components) { 0 } else { $result.Components.Count }
        Write-Output ("OK runtime-facade Command=`"attach`" ProcessId=" + $result.ProcessId + " HostState=`"" + $result.HostState + "`" Components=" + $componentsCount)
        exit 0
    }
    "preflight" { Write-Operation "preflight" $facade.Preflight($ProcessId) }
    "attach-hook" { Write-Operation "attach-hook" $facade.AttachHook($ProcessId) }
    "hook-status" { Write-Operation "hook-status" $facade.GetHookStatus($ProcessId) }
    "ping-hook" { Write-Operation "ping-hook" $facade.PingHook($ProcessId) }
    "hook-info" { Write-Operation "hook-info" $facade.GetHookInfo($ProcessId) }
    "read-self-module" { Write-Operation "read-self-module" $facade.ReadHookSelfModule($ProcessId) }
    "lua-smoke" { Write-Operation "lua-smoke" $facade.LuaSmoke($ProcessId) }
    "lua-exec" { Write-Operation "lua-exec" $facade.ExecuteLua($ProcessId, $Script) }
    "shutdown-hook" { Write-Operation "shutdown-hook" $facade.ShutdownHook($ProcessId) }
    default {
        Write-Output "FAIL runtime-facade Command=`"$Command`" Ready=False Reason=`"UnknownCommand`""
        exit 2
    }
}
