import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CLI = ROOT / "src" / "SpellFire.WowRuntime.Cli" / "bin" / "Debug" / "net48" / "SpellFire.WowRuntime.Cli.exe"


def run_case(name, command, pid, expected_exit, expected_parts):
    args = [str(CLI), "--command", command, "--pid", str(pid)]
    print(f"START wowruntime-movement-case Name={name} Command={command} Pid={pid}")
    completed = subprocess.run(
        args,
        cwd=str(ROOT),
        text=True,
        encoding="utf-8",
        errors="replace",
        capture_output=True,
    )
    output = (completed.stdout + completed.stderr).strip()
    if output:
        print(output)

    ok = completed.returncode == expected_exit and all(part in output for part in expected_parts)
    if ok:
        print(f"OK wowruntime-movement-case Name={name} Exit={completed.returncode}")
        return True

    print(f"FAIL wowruntime-movement-case Name={name} Exit={completed.returncode} ExpectedExit={expected_exit}")
    for part in expected_parts:
        if part not in output:
            print(f"MISS wowruntime-movement-case Name={name} Expected={part!r}")
    return False


def find_wow_pid():
    completed = subprocess.run(
        [
            "powershell",
            "-NoProfile",
            "-Command",
            "Get-Process -Name Wow -ErrorAction SilentlyContinue | Sort-Object StartTime -Descending | Select-Object -First 1 -ExpandProperty Id",
        ],
        text=True,
        encoding="utf-8",
        errors="replace",
        capture_output=True,
    )
    value = completed.stdout.strip()
    return int(value) if value.isdigit() else 0


def main():
    if not CLI.exists():
        print(f"FAIL wowruntime-movement-matrix Reason=\"CliMissing\" Path=\"{CLI}\"")
        return 2

    passed = True
    missing_pid = 999999
    passed = run_case(
        "missing-movement-jump",
        "movement-jump",
        missing_pid,
        1,
        ["Ready=False", 'Reason="ProcessUnavailable"'],
    ) and passed
    passed = run_case(
        "movement-go-not-implemented",
        "movement-go",
        missing_pid,
        1,
        ["Ready=False", 'Reason="FeatureUnavailable"', "Path movement is not implemented"],
    ) and passed

    wow_pid = find_wow_pid()
    if wow_pid <= 0:
        print('SKIP wowruntime-movement-live Reason="NoWowProcess"')
    else:
        passed = run_case(
            "live-movement-jump",
            "movement-jump",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', 'Action="jump"', 'RuntimeReason="LuaExecuteSucceeded"', "TextPayload=OK:FrameScriptExecute="],
        ) and passed
        passed = run_case(
            "live-movement-stop",
            "movement-stop",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', 'Action="stop"', 'RuntimeReason="LuaExecuteSucceeded"', "TextPayload=OK:FrameScriptExecute="],
        ) and passed
        passed = run_case(
            "live-movement-stop-to",
            "movement-stop-to",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', 'Action="stop-to"', 'RuntimeReason="LuaExecuteSucceeded"', "TextPayload=OK:FrameScriptExecute="],
        ) and passed

    if passed:
        print("OK wowruntime-movement-matrix")
        return 0

    print("FAIL wowruntime-movement-matrix")
    return 1


if __name__ == "__main__":
    sys.exit(main())
