import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CLI = ROOT / "src" / "SpellFire.WowRuntime.Cli" / "bin" / "Debug" / "net48" / "SpellFire.WowRuntime.Cli.exe"


def run_case(name, command, pid, expected_exit, expected_parts, extra=None):
    args = [str(CLI), "--command", command, "--pid", str(pid)]
    if extra:
        args.extend(extra)

    print(f"START wowruntime-scripting-case Name={name} Command={command} Pid={pid}")
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
        print(f"OK wowruntime-scripting-case Name={name} Exit={completed.returncode}")
        return True

    print(f"FAIL wowruntime-scripting-case Name={name} Exit={completed.returncode} ExpectedExit={expected_exit}")
    for part in expected_parts:
        if part not in output:
            print(f"MISS wowruntime-scripting-case Name={name} Expected={part!r}")
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
        print(f"FAIL wowruntime-scripting-matrix Reason=\"CliMissing\" Path=\"{CLI}\"")
        return 2

    passed = True
    missing_pid = 999999
    passed = run_case(
        "missing-script-smoke",
        "script-smoke",
        missing_pid,
        1,
        ["Ready=False", 'Reason="ProcessUnavailable"'],
    ) and passed
    passed = run_case(
        "empty-script-exec",
        "script-exec",
        missing_pid,
        1,
        ["Ready=False", 'Reason="InvalidArgument"', "Script must not be empty"],
        ["--script", ""],
    ) and passed

    wow_pid = find_wow_pid()
    if wow_pid <= 0:
        print('SKIP wowruntime-scripting-live Reason="NoWowProcess"')
    else:
        passed = run_case(
            "live-script-smoke",
            "script-smoke",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', 'RuntimeReason="LuaSmokeExecuted"', "LuaBridgeReady=True"],
        ) and passed
        passed = run_case(
            "live-script-exec",
            "script-exec",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', 'RuntimeReason="LuaExecuteSucceeded"', "TextPayload=OK:FrameScriptExecute="],
            ["--script", 'DEFAULT_CHAT_FRAME:AddMessage("SPELLFIRE_WOWRUNTIME_SCRIPT_OK");'],
        ) and passed

    if passed:
        print("OK wowruntime-scripting-matrix")
        return 0

    print("FAIL wowruntime-scripting-matrix")
    return 1


if __name__ == "__main__":
    sys.exit(main())
