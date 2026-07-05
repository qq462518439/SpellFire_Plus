param(
    [int]$ProcessId = 0,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$hookProject = Join-Path $repoRoot "src\SpellFire.Hook\SpellFire.Hook.vcxproj"
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
        [int]$Pid
    )

    Write-Output "START hook-lifecycle-$Command pid=$Pid"
    $output = & $runtimeHostCliExe $Command $Pid
    $exitCode = $LASTEXITCODE
    foreach ($line in $output) {
        Write-Output $line
    }

    return @{
        ExitCode = $exitCode
        Output = ($output -join "`n")
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

$cleanupBefore = Invoke-Cli -Command cleanup -Pid $ProcessId
$attach = Invoke-Cli -Command attach -Pid $ProcessId
$repeatAttach = Invoke-Cli -Command attach -Pid $ProcessId
$status = Invoke-Cli -Command status -Pid $ProcessId
$commandPing = Invoke-Cli -Command command-ping -Pid $ProcessId
$hookInfo = Invoke-Cli -Command hook-info -Pid $ProcessId
$readSelfModule = Invoke-Cli -Command read-self-module -Pid $ProcessId
$shutdown = Invoke-Cli -Command shutdown -Pid $ProcessId
$postStatus = Invoke-Cli -Command status -Pid $ProcessId
$cleanupAfter = Invoke-Cli -Command cleanup -Pid $ProcessId

$ok = $true
$ok = $ok -and $attach.ExitCode -eq 0 -and $attach.Output -match "HookReady|HookAlreadyReady"
$ok = $ok -and $repeatAttach.ExitCode -eq 0 -and $repeatAttach.Output -match "HookAlreadyReady"
$ok = $ok -and $status.ExitCode -eq 0 -and $status.Output -match "HookServiceAlive"
$ok = $ok -and $commandPing.ExitCode -eq 0 -and $commandPing.Output -match "HookCommandPingOk"
$ok = $ok -and $hookInfo.ExitCode -eq 0 -and $hookInfo.Output -match "HookInfoOk"
$ok = $ok -and $readSelfModule.ExitCode -eq 0 -and $readSelfModule.Output -match "HookSelfModuleReadOk"
$ok = $ok -and $shutdown.ExitCode -eq 0 -and $shutdown.Output -match "HookShutdownRequested"
$ok = $ok -and $postStatus.ExitCode -ne 0 -and $postStatus.Output -match "HookServiceUnavailable"
$ok = $ok -and $cleanupBefore.ExitCode -eq 0 -and $cleanupAfter.ExitCode -eq 0

if ($ok) {
    Write-Output "OK hook-lifecycle pid=$ProcessId"
    exit 0
}

Write-Output "FAIL hook-lifecycle pid=$ProcessId AttachExit=$($attach.ExitCode) RepeatAttachExit=$($repeatAttach.ExitCode) StatusExit=$($status.ExitCode) CommandPingExit=$($commandPing.ExitCode) HookInfoExit=$($hookInfo.ExitCode) ReadSelfModuleExit=$($readSelfModule.ExitCode) ShutdownExit=$($shutdown.ExitCode) PostStatusExit=$($postStatus.ExitCode)"
exit 1
