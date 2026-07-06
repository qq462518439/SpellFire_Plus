import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CLI = ROOT / "src" / "SpellFire.WowRuntime.Cli" / "bin" / "Debug" / "net48" / "SpellFire.WowRuntime.Cli.exe"


def run_case(name, command, pid, expected_exit, expected_parts, extra=None):
    args = [str(CLI), "--command", command, "--pid", str(pid)]
    if extra:
        args.extend(extra)

    print(f"START wowruntime-objectmanager-case Name={name} Command={command} Pid={pid}")
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
        print(f"OK wowruntime-objectmanager-case Name={name} Exit={completed.returncode}")
        return True

    print(f"FAIL wowruntime-objectmanager-case Name={name} Exit={completed.returncode} ExpectedExit={expected_exit}")
    for part in expected_parts:
        if part not in output:
            print(f"MISS wowruntime-objectmanager-case Name={name} Expected={part!r}")
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
        print(f"FAIL wowruntime-objectmanager-matrix Reason=\"CliMissing\" Path=\"{CLI}\"")
        return 2

    passed = True
    missing_pid = 999999
    passed = run_case(
        "missing-object-snapshot",
        "object-snapshot",
        missing_pid,
        1,
        ["Ready=False", 'Reason="ProcessUnavailable"'],
    ) and passed
    passed = run_case(
        "invalid-guid",
        "object-by-guid",
        missing_pid,
        1,
        ["Ready=False", 'Reason="InvalidArgument"'],
        ["--guid", "0"],
    ) and passed

    wow_pid = find_wow_pid()
    if wow_pid <= 0:
        print('SKIP wowruntime-objectmanager-live Reason="NoWowProcess"')
    else:
        passed = run_case(
            "live-object-snapshot",
            "object-snapshot",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', "ObjectCount=", "Me=Guid=0x"],
            ["--limit", "512"],
        ) and passed
        passed = run_case(
            "live-object-me",
            "object-me",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', "Object=Guid=0x"],
        ) and passed
        passed = run_case(
            "live-object-by-local-guid",
            "object-by-guid",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', "Object=Guid=0x10"],
            ["--guid", "16"],
        ) and passed
        passed = run_case(
            "live-object-target-empty-or-ready",
            "object-snapshot",
            wow_pid,
            0,
            ["Ready=True", "TargetGuid=0x"],
            ["--limit", "512"],
        ) and passed
        passed = run_case(
            "live-object-kind-unit",
            "object-snapshot",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', "ObjectCount="],
            ["--limit", "512", "--kind", "Unit"],
        ) and passed
        passed = run_case(
            "live-object-list-gameobject",
            "object-list",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', "ItemIndex="],
            ["--limit", "512", "--kind", "GameObject"],
        ) and passed
        passed = run_case(
            "live-object-nearby-unit",
            "object-nearby",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', "ObjectCount="],
            ["--limit", "512", "--radius", "80", "--kind", "Unit"],
        ) and passed
        passed = run_case(
            "live-object-nearby-list-gameobject",
            "object-nearby-list",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', "ItemIndex=", "Dist="],
            ["--limit", "512", "--radius", "80", "--kind", "GameObject"],
        ) and passed
        passed = run_case(
            "live-object-nearest-gameobject",
            "object-nearest",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', "Object=Guid=0x", "Kind=GameObject", "Dist="],
            ["--radius", "80", "--kind", "GameObject"],
        ) and passed

    if passed:
        print("OK wowruntime-objectmanager-matrix")
        return 0

    print("FAIL wowruntime-objectmanager-matrix")
    return 1


if __name__ == "__main__":
    sys.exit(main())
