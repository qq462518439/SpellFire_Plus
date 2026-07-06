param(
    [int]$ProcessId = 0,
    [int]$Limit = 512,
    [int]$Radius = 40,
    [string]$LogPath = "",
    [switch]$Watch,
    [int]$IntervalSeconds = 1,
    [int]$Iterations = 0,
    [switch]$NoBuild,
    [switch]$NoPause
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$cliProject = Join-Path $root "src\SpellFire.WowRuntime.Cli\SpellFire.WowRuntime.Cli.csproj"
$cli = Join-Path $root "src\SpellFire.WowRuntime.Cli\bin\Debug\net48\SpellFire.WowRuntime.Cli.exe"

if ([string]::IsNullOrWhiteSpace($LogPath)) {
    $logDir = Join-Path $root "artifacts\logs"
    New-Item -ItemType Directory -Force -Path $logDir | Out-Null
    $LogPath = Join-Path $logDir ("wowruntime-object-workbench-{0:yyyyMMdd-HHmmss}.log" -f (Get-Date))
}
else {
    $logParent = Split-Path -Parent $LogPath
    if (-not [string]::IsNullOrWhiteSpace($logParent)) {
        New-Item -ItemType Directory -Force -Path $logParent | Out-Null
    }
}

function Write-Step {
    param([string]$Message)
    $line = "[{0:HH:mm:ss.fff}] {1}" -f (Get-Date), $Message
    Write-Host $line
    Add-Content -Path $LogPath -Value $line -Encoding UTF8
}

function Invoke-ObjectCommand {
    param(
        [string]$Name,
        [string[]]$ExtraArgs = @()
    )

    $args = @("--command", $Name, "--pid", [string]$ProcessId) + $ExtraArgs
    Write-Step ("START {0} {1}" -f $Name, ($args -join " "))
    $output = & $cli @args 2>&1
    $exitCode = $LASTEXITCODE
    foreach ($line in $output) {
        Write-Step ("OUT {0}" -f $line)
    }
    Write-Step ("END {0} Exit={1}" -f $Name, $exitCode)
    return [pscustomobject]@{
        Name = $Name
        ExitCode = $exitCode
        Output = ($output -join "`n")
    }
}

try {
    Write-Step "Ready. WowRuntime object workbench started. LogPath=$LogPath"

    if (-not $NoBuild) {
        Write-Step "BUILD $cliProject"
        dotnet build $cliProject -c Debug -p:UseSharedCompilation=false | ForEach-Object { Write-Step "BUILD_OUT $_" }
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed. Exit=$LASTEXITCODE"
        }
    }

    if ($ProcessId -le 0) {
        $wow = Get-Process -Name Wow -ErrorAction SilentlyContinue |
            Sort-Object StartTime -Descending |
            Select-Object -First 1
        if ($null -eq $wow) {
            throw "No Wow process found. Start Wow.exe or pass -ProcessId."
        }
        $ProcessId = [int]$wow.Id
        Write-Step "AutoSelected ProcessId=$ProcessId ProcessName=$($wow.ProcessName)"
    }

    if (-not (Test-Path $cli)) {
        throw "CLI not found: $cli"
    }

    $runIndex = 0
    do {
        $runIndex++
        Write-Step "ROUND $runIndex"
        $snapshot = Invoke-ObjectCommand "object-snapshot" @("--limit", [string]$Limit)
        Invoke-ObjectCommand "object-me" | Out-Null
        Invoke-ObjectCommand "object-by-guid" @("--guid", "16") | Out-Null
        Invoke-ObjectCommand "object-target" | Out-Null
        Invoke-ObjectCommand "object-nearby" @("--limit", [string]$Limit, "--radius", [string]$Radius) | Out-Null
        Invoke-ObjectCommand "object-list" @("--limit", "20") | Out-Null
        Invoke-ObjectCommand "object-list" @("--limit", [string]$Limit, "--kind", "GameObject") | Out-Null
        Invoke-ObjectCommand "object-nearby" @("--limit", [string]$Limit, "--radius", [string]$Radius, "--kind", "Unit") | Out-Null

        if ($Watch -and ($Iterations -le 0 -or $runIndex -lt $Iterations)) {
            Start-Sleep -Seconds $IntervalSeconds
        }
    } while ($Watch -and ($Iterations -le 0 -or $runIndex -lt $Iterations))

    Write-Step "SUMMARY SnapshotExit=$($snapshot.ExitCode) LogPath=$LogPath"
}
catch {
    Write-Step ("FAIL " + $_.Exception.Message)
    if (-not $NoPause) {
        Write-Host ""
        Read-Host "Press Enter to close"
    }
    exit 1
}

if (-not $NoPause) {
    Write-Host ""
    Read-Host "Press Enter to close"
}
