import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CLI = ROOT / "src" / "SpellFire.WowRuntime.Cli" / "bin" / "Debug" / "net48" / "SpellFire.WowRuntime.Cli.exe"


def run_case(name, pid, args, expected_exit, expected_parts):
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

    pid = 999999
    passed = run_case(
        "rdmanaged-provider-shell-contract",
        pid,
        [str(CLI), "--command", "navigation-capability", "--pid", str(pid)],
        0,
        [
            "Ready=True",
            'Reason="Ready"',
            "CanFindPath=True",
            "CanExecutePath=False",
            "CanFindZ=True",
            "RdManagedAssemblyPresent=True",
            "RdManagedSessionReady=True",
            "TileProviderReady=True",
            "SupportsPathQueue=False",
            "SupportsArrivalCheck=False",
            "SupportsStuckDetection=False",
            "RDManaged session is ready",
            "TileSourcePresent=True",
            "TileProviderReady=True",
            "TileDecodeReady",
            "native CTM stop is not proven",
        ],
    )

    passed = run_case(
        "rdmanaged-find-path-northrend-sample",
        pid,
        [
            str(CLI),
            "--command",
            "navigation-find-path",
            "--pid",
            str(pid),
            "--map",
            "571",
            "--from-x",
            "5924.576",
            "--from-y",
            "644.421",
            "--from-z",
            "645.511",
            "--to-x",
            "5934.576",
            "--to-y",
            "644.421",
            "--to-z",
            "645.511",
        ],
        0,
        [
            "Ready=True",
            'Reason="Success"',
            "PathStatus=Success",
            "RDManaged FindPath succeeded",
            "PointCount=5",
        ],
    ) and passed

    passed = run_case(
        "rdmanaged-find-z-northrend-sample",
        pid,
        [
            str(CLI),
            "--command",
            "navigation-find-z",
            "--pid",
            str(pid),
            "--map",
            "571",
            "--x",
            "5924.576",
            "--y",
            "644.421",
            "--z",
            "645.511",
        ],
        0,
        [
            "Ready=True",
            'Reason="Success"',
            "MapId=571",
            "Z=",
        ],
    ) and passed

    passed = run_case(
        "navigation-execute-requires-explicit-target",
        pid,
        [
            str(CLI),
            "--command",
            "navigation-execute-to",
            "--pid",
            str(pid),
        ],
        1,
        [
            "Ready=False",
            'Reason="InvalidArgument"',
            "requires explicit --x --y --z",
        ],
    ) and passed

    if passed:
        print("OK wowruntime-navigation-capability-matrix")
        return 0

    print("FAIL wowruntime-navigation-capability-matrix")
    return 1


if __name__ == "__main__":
    sys.exit(main())
