import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CASES = [
    ROOT / "tools" / "wowruntime-objectmanager-matrix.py",
    ROOT / "tools" / "wowruntime-scripting-matrix.py",
    ROOT / "tools" / "wowruntime-movement-matrix.py",
]


def main():
    passed = True
    for script in CASES:
        print(f"START wowruntime-matrix Script={script.name}")
        completed = subprocess.run(
            [sys.executable, str(script)],
            cwd=str(ROOT),
            text=True,
            encoding="utf-8",
            errors="replace",
            capture_output=True,
        )
        output = (completed.stdout + completed.stderr).strip()
        if output:
            print(output)

        if completed.returncode == 0:
            print(f"OK wowruntime-matrix Script={script.name} Exit=0")
        else:
            print(f"FAIL wowruntime-matrix Script={script.name} Exit={completed.returncode}")
            passed = False

    if passed:
        print("OK wowruntime-matrix")
        return 0

    print("FAIL wowruntime-matrix")
    return 1


if __name__ == "__main__":
    sys.exit(main())
