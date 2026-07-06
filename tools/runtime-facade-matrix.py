import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "tools" / "runtime-facade-command.ps1"
PS32 = Path(r"C:\Windows\SysWOW64\WindowsPowerShell\v1.0\powershell.exe")


def run_command(name, process_id=0, script=""):
    args = [
        str(PS32),
        "-ExecutionPolicy",
        "Bypass",
        "-File",
        str(SCRIPT),
        "-Command",
        name,
        "-ProcessId",
        str(process_id),
    ]
    if script:
        args.extend(["-Script", script])

    completed = subprocess.run(
        args,
        cwd=str(ROOT),
        text=True,
        encoding="utf-8",
        errors="replace",
        capture_output=True,
    )
    output = (completed.stdout + completed.stderr).strip()
    return completed.returncode, output


def assert_case(case_name, command, process_id, expected_exit, expected_parts, script=""):
    print(f"START runtime-facade-case Name={case_name} Command={command} Pid={process_id}")
    exit_code, output = run_command(command, process_id, script)
    if output:
        print(output)

    ok = exit_code == expected_exit and all(part in output for part in expected_parts)
    if ok:
        print(f"OK runtime-facade-case Name={case_name} Exit={exit_code}")
        return True

    print(f"FAIL runtime-facade-case Name={case_name} Exit={exit_code} ExpectedExit={expected_exit}")
    for part in expected_parts:
        if part not in output:
            print(f"MISS runtime-facade-case Name={case_name} Expected={part!r}")
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
    if not PS32.exists():
        print(f"FAIL runtime-facade-matrix Reason=\"PowerShell32Missing\" Path=\"{PS32}\"")
        return 2

    passed = True
    missing_pid = 999999

    invalid_cases = [
        ("missing-probe-memory", "probe-memory", 1, ['Reason="ProcessUnavailable"']),
        ("missing-preflight", "preflight", 1, ['Reason="ProcessUnavailable"', 'Ready=False']),
        ("missing-attach-hook", "attach-hook", 1, ['Reason="ProcessUnavailable"', 'Ready=False']),
        ("missing-hook-status", "hook-status", 1, ['Reason="ProcessUnavailable"', 'Ready=False']),
        ("missing-lua-smoke", "lua-smoke", 1, ['Reason="ProcessUnavailable"', 'Ready=False']),
        ("missing-shutdown-hook", "shutdown-hook", 1, ['Reason="ProcessUnavailable"', 'Ready=False']),
    ]

    for case_name, command, expected_exit, parts in invalid_cases:
        passed = assert_case(case_name, command, missing_pid, expected_exit, parts) and passed

    wow_pid = find_wow_pid()
    if wow_pid <= 0:
        print('SKIP runtime-facade-live-cases Reason="NoWowProcess"')
    else:
        live_cases = [
            ("live-probe-memory", "probe-memory", 0, ['Reason="SessionOpened"', "Ready=True"]),
            ("live-attach-hook", "attach-hook", 0, ['Reason="HookReady"', "Ready=True"]),
            ("live-hook-status", "hook-status", 0, ['Reason="HookServiceAlive"', "Ready=True"]),
            ("live-ping-hook", "ping-hook", 0, ['Reason="HookCommandPingOk"', "Ready=True"]),
            ("live-hook-info", "hook-info", 0, ['Reason="HookInfoOk"', "Ready=True"]),
            ("live-read-self-module", "read-self-module", 0, ['Reason="HookSelfModuleReadOk"', "Ready=True"]),
            ("live-lua-smoke", "lua-smoke", 0, ['Reason="LuaSmokeExecuted"', "Ready=True", "LuaBridgeReady=True"]),
            (
                "live-lua-exec",
                "lua-exec",
                0,
                ['Reason="LuaExecuteSucceeded"', "Ready=True", "TextPayload=OK:FrameScriptExecute="],
            ),
            ("live-shutdown-hook", "shutdown-hook", 0, ['Reason="HookShutdownRequested"', "Ready=True"]),
        ]

        for case_name, command, expected_exit, parts in live_cases:
            script = 'DEFAULT_CHAT_FRAME:AddMessage("SPELLFIRE_RUNTIME_FACADE_OK");' if command == "lua-exec" else ""
            passed = assert_case(case_name, command, wow_pid, expected_exit, parts, script=script) and passed

    if passed:
        print("OK runtime-facade-matrix")
        return 0

    print("FAIL runtime-facade-matrix")
    return 1


if __name__ == "__main__":
    sys.exit(main())
