import argparse
import re
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CLI = ROOT / "src" / "SpellFire.WowRuntime.Cli" / "bin" / "Debug" / "net48" / "SpellFire.WowRuntime.Cli.exe"


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


def run_nearest(pid, kind, radius):
    command = [
        str(CLI),
        "--command",
        "object-nearest",
        "--kind",
        kind,
        "--radius",
        str(radius),
        "--pid",
        str(pid),
    ]
    completed = subprocess.run(
        command,
        cwd=str(ROOT),
        text=True,
        encoding="utf-8",
        errors="replace",
        capture_output=True,
    )
    return completed.returncode, (completed.stdout + completed.stderr).strip()


def assert_name(pid, kind, radius, required):
    exit_code, output = run_nearest(pid, kind, radius)
    print(f"START wowruntime-object-name-case Kind={kind} Required={required} Pid={pid} Radius={radius:g}")
    print(output)

    if exit_code != 0:
        if required:
            print(f"FAIL wowruntime-object-name-case Kind={kind} Reason=\"CommandFailed\" Exit={exit_code}")
            return False

        print(f"OK wowruntime-object-name-case Kind={kind} Skipped=True Exit={exit_code}")
        return True

    match = re.search(r'Name="([^"]*)"', output)
    name = match.group(1) if match else ""
    if not name:
        if required:
            print(f"FAIL wowruntime-object-name-case Kind={kind} Reason=\"NameEmpty\"")
            return False

        print(f"OK wowruntime-object-name-case Kind={kind} NamePresent=False Required=False")
        return True

    print(f"OK wowruntime-object-name-case Kind={kind} NamePresent=True Name=\"{name}\"")
    return True


def main():
    parser = argparse.ArgumentParser(description="Verify WowRuntime can read nearby object names.")
    parser.add_argument("--pid", type=int, default=0)
    parser.add_argument("--radius", type=float, default=120.0)
    parser.add_argument("--require-unit", action="store_true")
    args = parser.parse_args()

    if not CLI.exists():
        print(f"FAIL wowruntime-object-name-matrix Reason=\"CliMissing\" Path=\"{CLI}\"")
        return 2

    pid = args.pid or find_wow_pid()
    if pid <= 0:
        print('FAIL wowruntime-object-name-matrix Reason="NoWowProcess"')
        return 2

    print(f"START wowruntime-object-name-matrix Pid={pid} Radius={args.radius:g} RequireUnit={args.require_unit}")
    passed = True
    passed = assert_name(pid, "GameObject", args.radius, True) and passed
    passed = assert_name(pid, "Unit", args.radius, args.require_unit) and passed

    if not passed:
        print("FAIL wowruntime-object-name-matrix")
        return 1

    print("OK wowruntime-object-name-matrix")
    return 0


if __name__ == "__main__":
    sys.exit(main())
