import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CLI = ROOT / "src" / "SpellFire.WowRuntime.Cli" / "bin" / "Debug" / "net48" / "SpellFire.WowRuntime.Cli.exe"


def run_case(name, pid, expected_exit, expected_parts):
    args = [str(CLI), "--command", "navigation-capability", "--pid", str(pid)]
    print(f"START wowruntime-navigation-capability-case Name={name} Pid={pid}")
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
        print(f"OK wowruntime-navigation-capability-case Name={name} Exit={completed.returncode}")
        return True

    print(f"FAIL wowruntime-navigation-capability-case Name={name} Exit={completed.returncode} ExpectedExit={expected_exit}")
    for part in expected_parts:
        if part not in output:
            print(f"MISS wowruntime-navigation-capability-case Name={name} Expected={part!r}")
    return False


def main():
    if not CLI.exists():
        print(f"FAIL wowruntime-navigation-capability-matrix Reason=\"CliMissing\" Path=\"{CLI}\"")
        return 2

    passed = run_case(
        "unavailable-navigation-contract",
        999999,
        0,
        [
            "Ready=True",
            'Reason="Ready"',
            "CanFindPath=False",
            "CanExecutePath=False",
            "CanFindZ=False",
            "SupportsPathQueue=False",
            "SupportsArrivalCheck=False",
            "SupportsStuckDetection=False",
            "Movement.Go is only a single-point CTM primitive",
            "native CTM stop is not proven",
        ],
    )

    if passed:
        print("OK wowruntime-navigation-capability-matrix")
        return 0

    print("FAIL wowruntime-navigation-capability-matrix")
    return 1


if __name__ == "__main__":
    sys.exit(main())
