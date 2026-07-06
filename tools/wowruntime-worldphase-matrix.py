import argparse
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
WATCH = ROOT / "tools" / "wowruntime-worldphase-watch.py"


def run_case(name, args, expected_tokens):
    command = [sys.executable, str(WATCH)] + args
    print(f"START wowruntime-worldphase-case Name={name} Command=\"{' '.join(command)}\"")
    completed = subprocess.run(
        command,
        cwd=str(ROOT),
        text=True,
        encoding="utf-8",
        errors="replace",
        capture_output=True,
    )
    output = (completed.stdout + completed.stderr).strip()
    if output:
        print(output)

    missing = [token for token in expected_tokens if token not in output]
    if completed.returncode != 0 or missing:
        print(
            f"FAIL wowruntime-worldphase-case Name={name} Exit={completed.returncode} "
            f"Missing=\"{'|'.join(missing)}\""
        )
        return False

    print(f"OK wowruntime-worldphase-case Name={name} Exit={completed.returncode}")
    return True


def main():
    parser = argparse.ArgumentParser(description="WowRuntime world phase regression matrix.")
    parser.add_argument("--pid", type=int, default=0)
    parser.add_argument("--samples", type=int, default=3)
    parser.add_argument("--interval", type=float, default=0.5)
    parser.add_argument("--send-enter-when-not-inworld", action="store_true")
    parser.add_argument("--enter-interval", type=float, default=2.0)
    parser.add_argument("--max-enters", type=int, default=2)
    args = parser.parse_args()

    if not WATCH.exists():
        print(f"FAIL wowruntime-worldphase-matrix Reason=\"WatchMissing\" Path=\"{WATCH}\"")
        return 2

    base_args = [
        "--samples",
        str(max(1, args.samples)),
        "--interval",
        str(max(0.0, args.interval)),
    ]
    if args.pid > 0:
        base_args += ["--pid", str(args.pid)]

    if args.send_enter_when_not_inworld:
        base_args += [
            "--send-enter-when-not-inworld",
            "--enter-interval",
            str(max(0.0, args.enter_interval)),
            "--max-enters",
            str(max(0, args.max_enters)),
        ]

    passed = True
    passed &= run_case(
        "phase-watch",
        base_args,
        ["START wowruntime-worldphase-watch", "Sample=1", "Phase=", "SUMMARY wowruntime-worldphase-watch", "FirstPhase=", "LastPhase=", "PhaseCounts=", "OK wowruntime-worldphase-watch"],
    )
    passed &= run_case(
        "missing-process",
        ["--pid", "999999", "--samples", "1", "--interval", "0"],
        ["Result=Fail", "Reason=\"ProcessUnavailable\"", "SUMMARY wowruntime-worldphase-watch", "PhaseCounts=\"Unknown:1\"", "OK wowruntime-worldphase-watch"],
    )

    if passed:
        print("OK wowruntime-worldphase-matrix")
        return 0

    print("FAIL wowruntime-worldphase-matrix")
    return 1


if __name__ == "__main__":
    sys.exit(main())
