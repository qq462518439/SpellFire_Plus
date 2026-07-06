import pathlib
import subprocess
import sys


ROOT = pathlib.Path(__file__).resolve().parent
SCRIPTS = [
    "memoryrobot-session-matrix.py",
    "memoryrobot-memory-matrix.py",
    "memoryrobot-remoteexec-matrix.py",
]


def main():
    passed = True
    for script in SCRIPTS:
        path = ROOT / script
        print(f"START memoryrobot-matrix Script={script}")
        completed = subprocess.run([sys.executable, str(path)], text=True, encoding="utf-8", errors="replace")
        if completed.returncode == 0:
            print(f"OK memoryrobot-matrix Script={script}")
        else:
            print(f"FAIL memoryrobot-matrix Script={script} Exit={completed.returncode}")
            passed = False
            break

    if passed:
        print("OK memoryrobot-matrix")
        return 0

    print("FAIL memoryrobot-matrix")
    return 1


if __name__ == "__main__":
    sys.exit(main())
