param(
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

$passed = $true

function Invoke-ProbeExpect {
    param(
        [string]$Name,
        [int]$ProcessId,
        [string]$ExpectedReason
    )

    Write-Output "START memoryrobot-failure-case Name=$Name Pid=$ProcessId ExpectedReason=$ExpectedReason"
    $output = & $memoryRobotCliExe probe-expect $ProcessId $ExpectedReason
    $exitCode = $LASTEXITCODE
    foreach ($line in $output) {
        Write-Output $line
    }

    if ($exitCode -eq 0) {
        Write-Output "OK memoryrobot-failure-case Name=$Name Pid=$ProcessId Reason=$ExpectedReason"
        return
    }

    Write-Output "FAIL memoryrobot-failure-case Name=$Name Pid=$ProcessId Exit=$exitCode ExpectedReason=$ExpectedReason"
    $script:passed = $false
}

Invoke-ProbeExpect -Name "missing-process" -ProcessId 999999 -ExpectedReason "ProcessUnavailable"

$system = Get-Process -Id 4 -ErrorAction SilentlyContinue
if ($null -ne $system) {
    Invoke-ProbeExpect -Name "system-access" -ProcessId $system.Id -ExpectedReason "AccessDenied"
}
else {
    Write-Output "SKIP memoryrobot-failure-case Name=system-access Reason=`"SystemProcessUnavailable`""
}

$explorer = Get-Process -Name explorer -ErrorAction SilentlyContinue | Sort-Object StartTime -Descending | Select-Object -First 1
if ($null -ne $explorer) {
    Invoke-ProbeExpect -Name "explorer-bitness" -ProcessId $explorer.Id -ExpectedReason "TargetNot32Bit"
}
else {
    Write-Output "SKIP memoryrobot-failure-case Name=explorer-bitness Reason=`"ExplorerUnavailable`""
}

if ($passed) {
    Write-Output "OK memoryrobot-failure-matrix"
    exit 0
}

Write-Output "FAIL memoryrobot-failure-matrix"
exit 1
