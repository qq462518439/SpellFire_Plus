import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CASES = [
    ROOT / "tools" / "wowruntime-worldphase-matrix.py",
    ROOT / "tools" / "wowruntime-objectmanager-matrix.py",
    ROOT / "tools" / "wowruntime-object-name-matrix.py",
    ROOT / "tools" / "wowruntime-scripting-matrix.py",
    ROOT / "tools" / "wowruntime-movement-contract-guard.py",
    ROOT / "tools" / "wowruntime-movement-matrix.py",
]
MAX_SUCCESS_OUTPUT_CHARS = 12000


def safe_print(value):
    encoding = getattr(sys.stdout, "encoding", None) or "utf-8"
    print(value.encode(encoding, errors="replace").decode(encoding, errors="replace"))


def main():
    extra_args = sys.argv[1:]
    passed = True
    for script in CASES:
        print(f"START wowruntime-matrix Script={script.name}")
        completed = subprocess.run(
            [sys.executable, str(script)] + extra_args,
            cwd=str(ROOT),
            text=True,
            encoding="utf-8",
            errors="replace",
            capture_output=True,
        )
        output = (completed.stdout + completed.stderr).strip()
        if completed.returncode == 0:
            if output:
                if len(output) > MAX_SUCCESS_OUTPUT_CHARS:
                    safe_print(output[:MAX_SUCCESS_OUTPUT_CHARS])
                    print(f"... truncated successful output, original chars={len(output)}")
                else:
                    safe_print(output)
            print(f"OK wowruntime-matrix Script={script.name} Exit=0")
        else:
            if output:
                safe_print(output)
            print(f"FAIL wowruntime-matrix Script={script.name} Exit={completed.returncode}")
            passed = False

    if passed:
        print("OK wowruntime-matrix")
        return 0

    print("FAIL wowruntime-matrix")
    return 1


if __name__ == "__main__":
    sys.exit(main())
