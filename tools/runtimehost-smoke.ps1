param(
    [int]$ProcessId = 0,
    [switch]$SkipBuild,
    [switch]$Shutdown
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$hookProject = Join-Path $repoRoot "src\SpellFire.Hook\SpellFire.Hook.vcxproj"
$runtimeHostProject = Join-Path $repoRoot "src\SpellFire.RuntimeHost\SpellFire.RuntimeHost.csproj"
$runtimeHostCliProject = Join-Path $repoRoot "src\SpellFire.RuntimeHost.Cli\SpellFire.RuntimeHost.Cli.csproj"
$runtimeHostCliExe = Join-Path $repoRoot "src\SpellFire.RuntimeHost.Cli\bin\Debug\net48\SpellFire.RuntimeHost.Cli.exe"

function Resolve-MSBuildPath {
    $vswhere = "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $paths = & $vswhere -products * -requires Microsoft.Component.MSBuild -find "MSBuild\Current\Bin\MSBuild.exe"
        foreach ($path in $paths) {
            if (-not [string]::IsNullOrWhiteSpace($path) -and (Test-Path $path) -and (Test-CppTargetsAvailable -MSBuildPath $path)) {
                return $path
            }
        }

        foreach ($path in $paths) {
            if (-not [string]::IsNullOrWhiteSpace($path) -and (Test-Path $path)) {
                return $path
            }
        }
    }

    $command = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($null -ne $command -and (Test-Path $command.Source)) {
        return $command.Source
    }

    $fallback = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
    if (Test-Path $fallback) {
        return $fallback
    }

    return $null
}

function Test-CppTargetsAvailable {
    param([string]$MSBuildPath)

    if ([string]::IsNullOrWhiteSpace($MSBuildPath)) {
        return $false
    }

    $vsRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $MSBuildPath)))
    $props = Get-ChildItem (Join-Path $vsRoot "MSBuild\Microsoft\VC") -Recurse -Filter "Microsoft.Cpp.Default.props" -ErrorAction SilentlyContinue | Select-Object -First 1
    return $null -ne $props
}

if (-not $SkipBuild) {
    $msbuild = Resolve-MSBuildPath
    if ([string]::IsNullOrWhiteSpace($msbuild)) {
        Write-Output "FAIL runtimehost-smoke Reason=`"MSBuildUnavailable`""
        exit 4
    }

    if (-not (Test-CppTargetsAvailable -MSBuildPath $msbuild)) {
        Write-Output "FAIL runtimehost-smoke Reason=`"CppTargetsMissing`" MSBuild=`"$msbuild`" Hint=`"Install Visual Studio Desktop development with C++ workload, or run with -SkipBuild when SpellFire.Hook.dll already exists.`""
        exit 4
    }

    & $msbuild $hookProject /p:Configuration=Debug /p:Platform=Win32 /m /nologo /v:minimal
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

if ($attachOutput -match "HookLoadedButReadySignalMissing") {
    Write-Output "START runtimehost-cleanup-after"
    & $runtimeHostCliExe cleanup
    Write-Output "FAIL runtimehost-smoke pid=$ProcessId Reason=`"ExistingStaleHookPayload`" Hint=`"The target process already contains SpellFire.Hook.dll but ready/heartbeat is not alive. Restart the target process before a full smoke run.`""
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
