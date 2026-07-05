param(
    [int]$ProcessId = 0,
    [switch]$SkipBuild,
    [switch]$RequireFresh,
    [switch]$Shutdown
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$vcvars = "C:\Program Files\Microsoft Visual Studio\2026\Community\VC\Auxiliary\Build\vcvars32.bat"
$hookProject = Join-Path $repoRoot "src\SpellFire.Hook\SpellFire.Hook.vcxproj"
$runtimeHostProject = Join-Path $repoRoot "src\SpellFire.RuntimeHost\SpellFire.RuntimeHost.csproj"
$runtimeHostCliProject = Join-Path $repoRoot "src\SpellFire.RuntimeHost.Cli\SpellFire.RuntimeHost.Cli.csproj"
$runtimeHostCliExe = Join-Path $repoRoot "src\SpellFire.RuntimeHost.Cli\bin\Debug\net48\SpellFire.RuntimeHost.Cli.exe"

if (-not $SkipBuild) {
    if (-not (Test-Path $vcvars)) {
        throw "vcvars32.bat not found: $vcvars"
    }

    cmd /c "call `"$vcvars`" && msbuild `"$hookProject`" /p:Configuration=Debug /p:Platform=Win32 /m /nologo /v:minimal"
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    dotnet build $runtimeHostProject -c Debug
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
        Write-Output "FAIL runtimehost-smoke Reason=`"NoWowProcess`""
        exit 2
    }

    $ProcessId = $wow.Id
}

Write-Output "START runtimehost-cleanup-before"
& $runtimeHostCliExe cleanup

Write-Output "START runtimehost-memory-probe pid=$ProcessId"
$memoryProbeOutput = & $runtimeHostCliExe memory-probe $ProcessId
$memoryProbeOutput
$memoryProbeExit = $LASTEXITCODE

Write-Output "START runtimehost-preflight pid=$ProcessId"
$preflightOutput = & $runtimeHostCliExe preflight $ProcessId
$preflightOutput
$preflightExit = $LASTEXITCODE

Write-Output "START runtimehost-attach pid=$ProcessId"
$attachOutput = & $runtimeHostCliExe attach $ProcessId
$attachOutput
$attachExit = $LASTEXITCODE

if ($RequireFresh -and ($attachOutput -match "HookLoadedButReadySignalMissing")) {
    Write-Output "FAIL runtimehost-smoke pid=$ProcessId Reason=`"ExistingStaleHookPayload`""
    exit 3
}

Write-Output "START runtimehost-repeat-attach pid=$ProcessId"
$repeatOutput = & $runtimeHostCliExe attach $ProcessId
$repeatOutput
$repeatAttachExit = $LASTEXITCODE

Write-Output "START runtimehost-status pid=$ProcessId"
$statusOutput = & $runtimeHostCliExe status $ProcessId
$statusOutput
$statusExit = $LASTEXITCODE

Write-Output "START runtimehost-command-ping pid=$ProcessId"
$commandPingOutput = & $runtimeHostCliExe command-ping $ProcessId
$commandPingOutput
$commandPingExit = $LASTEXITCODE

Write-Output "START runtimehost-hook-info pid=$ProcessId"
$hookInfoOutput = & $runtimeHostCliExe hook-info $ProcessId
$hookInfoOutput
$hookInfoExit = $LASTEXITCODE

Write-Output "START runtimehost-read-self-module pid=$ProcessId"
$readSelfModuleOutput = & $runtimeHostCliExe read-self-module $ProcessId
$readSelfModuleOutput
$readSelfModuleExit = $LASTEXITCODE

$shutdownExit = 0
if ($Shutdown) {
    Write-Output "START runtimehost-shutdown pid=$ProcessId"
    $shutdownOutput = & $runtimeHostCliExe shutdown $ProcessId
    $shutdownOutput
    $shutdownExit = $LASTEXITCODE
}

if ($memoryProbeExit -eq 0 -and $preflightExit -eq 0 -and $attachExit -eq 0 -and $repeatAttachExit -eq 0 -and $statusExit -eq 0 -and $commandPingExit -eq 0 -and $hookInfoExit -eq 0 -and $readSelfModuleExit -eq 0 -and $shutdownExit -eq 0) {
    Write-Output "START runtimehost-cleanup-after"
    & $runtimeHostCliExe cleanup
    Write-Output "OK runtimehost-smoke pid=$ProcessId"
    exit 0
}

Write-Output "START runtimehost-cleanup-after"
& $runtimeHostCliExe cleanup
Write-Output "FAIL runtimehost-smoke pid=$ProcessId MemoryProbeExit=$memoryProbeExit PreflightExit=$preflightExit AttachExit=$attachExit RepeatAttachExit=$repeatAttachExit StatusExit=$statusExit CommandPingExit=$commandPingExit HookInfoExit=$hookInfoExit ReadSelfModuleExit=$readSelfModuleExit ShutdownExit=$shutdownExit"
exit 1
