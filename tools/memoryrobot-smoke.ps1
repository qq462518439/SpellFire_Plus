param(
    [int]$ProcessId = 0,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$memoryRobotCliProject = Join-Path $repoRoot "src\SpellFire.MemoryRobot.Cli\SpellFire.MemoryRobot.Cli.csproj"
$memoryRobotCliExe = Join-Path $repoRoot "src\SpellFire.MemoryRobot.Cli\bin\Debug\net48\SpellFire.MemoryRobot.Cli.exe"

if (-not $SkipBuild) {
    dotnet build $memoryRobotCliProject -c Debug
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

if ($ProcessId -le 0) {
    $wow = Get-Process -Name Wow -ErrorAction SilentlyContinue | Sort-Object StartTime -Descending | Select-Object -First 1
    if ($null -eq $wow) {
        Write-Output "FAIL memoryrobot-smoke Reason=`"NoWowProcess`""
        exit 2
    }

    $ProcessId = $wow.Id
}

$passed = $true

function Invoke-MemoryRobotCommand {
    param([string]$Command)

    Write-Output "START memoryrobot-$Command pid=$ProcessId"
    $output = & $memoryRobotCliExe $Command $ProcessId
    $exitCode = $LASTEXITCODE
    foreach ($line in $output) {
        Write-Output $line
    }

    if ($exitCode -eq 0) {
        return
    }

    Write-Output "FAIL memoryrobot-$Command pid=$ProcessId Exit=$exitCode"
    $script:passed = $false
}

$commands = @(
    "probe",
    "session-open-close",
    "close-then-reopen",
    "snapshot-after-close",
    "session-close-all",
    "process-exit-after-open",
    "module-snapshot",
    "memory-region",
    "remote-alloc-free",
    "write-remote-allocation",
    "remote-thread-invalid-start",
    "load-library-missing-file",
    "self-remote-thread-get-current-process-id",
    "self-load-library-known-system-dll",
    "try-read-invalid"
)

foreach ($command in $commands) {
    Invoke-MemoryRobotCommand -Command $command
}

if ($passed) {
    Write-Output "OK memoryrobot-smoke pid=$ProcessId"
    exit 0
}

Write-Output "FAIL memoryrobot-smoke pid=$ProcessId"
exit 1
