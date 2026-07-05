param(
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$runtimeHostCliProject = Join-Path $repoRoot "src\SpellFire.RuntimeHost.Cli\SpellFire.RuntimeHost.Cli.csproj"
$runtimeHostCliExe = Join-Path $repoRoot "src\SpellFire.RuntimeHost.Cli\bin\Debug\net48\SpellFire.RuntimeHost.Cli.exe"

if (-not $SkipBuild) {
    dotnet build $runtimeHostCliProject -c Debug
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

function Invoke-MemoryProbeCase {
    param(
        [string]$Name,
        [int]$ProcessId,
        [string]$ExpectedReason
    )

    [Console]::WriteLine("START memory-probe-case Name=$Name Pid=$ProcessId ExpectedReason=$ExpectedReason")
    $output = & $runtimeHostCliExe memory-probe $ProcessId
    $exitCode = $LASTEXITCODE
    foreach ($line in $output) {
        [Console]::WriteLine($line)
    }

    $matched = $output -match "Reason=`"$ExpectedReason`""
    if ($matched) {
        [Console]::WriteLine("OK memory-probe-case Name=$Name Pid=$ProcessId Exit=$exitCode Reason=$ExpectedReason")
        return $true
    }

    [Console]::WriteLine("FAIL memory-probe-case Name=$Name Pid=$ProcessId Exit=$exitCode ExpectedReason=$ExpectedReason")
    return $false
}

$passed = $true

$passed = (Invoke-MemoryProbeCase -Name "missing-process" -ProcessId 999999 -ExpectedReason "ProcessUnavailable") -and $passed

$system = Get-Process -Id 4 -ErrorAction SilentlyContinue
if ($null -ne $system) {
    $passed = (Invoke-MemoryProbeCase -Name "system-access" -ProcessId $system.Id -ExpectedReason "AccessDenied") -and $passed
}
else {
    Write-Output "SKIP memory-probe-case Name=system-access Reason=`"SystemProcessUnavailable`""
}

$explorer = Get-Process -Name explorer -ErrorAction SilentlyContinue | Sort-Object StartTime -Descending | Select-Object -First 1
if ($null -ne $explorer) {
    $passed = (Invoke-MemoryProbeCase -Name "explorer-bitness" -ProcessId $explorer.Id -ExpectedReason "TargetNot32Bit") -and $passed
}
else {
    Write-Output "SKIP memory-probe-case Name=explorer-bitness Reason=`"ExplorerUnavailable`""
}

if ($passed) {
    Write-Output "OK memory-probe-matrix"
    exit 0
}

Write-Output "FAIL memory-probe-matrix"
exit 1
