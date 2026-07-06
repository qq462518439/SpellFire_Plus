param()

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$sourceRoots = @(
    (Join-Path $repoRoot "src"),
    (Join-Path $repoRoot "tools")
)

$forbiddenPatterns = @(
    "SendInput",
    "keybd_event",
    "VK_SPACE",
    "KEYEVENTF",
    "/run print",
    "JumpAndChatPing",
    "HookLuaSmokeOk",
    "InputSent=True"
)

foreach ($root in $sourceRoots) {
    if (-not (Test-Path $root)) {
        continue
    }

    $files = Get-ChildItem -Path $root -Recurse -File -Include *.cs,*.cpp,*.h,*.ps1
    foreach ($file in $files) {
        if ([string]::Equals($file.FullName, $PSCommandPath, [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        $content = Get-Content $file.FullName -Raw
        foreach ($pattern in $forbiddenPatterns) {
            if ($content.Contains($pattern)) {
                Write-Output "FAIL runtimehost-lua-gate Reason=`"ForbiddenFakeLuaSmoke`" Pattern=`"$pattern`" Path=`"$($file.FullName)`""
                exit 1
            }
        }
    }
}

$hookSource = Join-Path $repoRoot "src\SpellFire.Hook\src\dllmain.cpp"
if (-not (Test-Path $hookSource)) {
    Write-Output "FAIL runtimehost-lua-gate Reason=`"HookSourceMissing`""
    exit 1
}

$hookContent = Get-Content $hookSource -Raw
$hasFrameScriptOffset = $hookContent.Contains("0x819210") -or $hookContent.Contains("FrameScript__Execute")
$hasMainThreadQueue = $hookContent.Contains("EndScene") -or $hookContent.Contains("MainThread")

if ($hasFrameScriptOffset -and -not $hasMainThreadQueue) {
    Write-Output "FAIL runtimehost-lua-gate Reason=`"FrameScriptWithoutMainThreadGate`""
    exit 1
}

Write-Output "OK runtimehost-lua-gate ForbiddenFakeLuaSmoke=False FrameScriptRequiresMainThread=True"
exit 0
