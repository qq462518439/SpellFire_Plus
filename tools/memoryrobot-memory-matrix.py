import pathlib
import subprocess
import sys


ROOT = pathlib.Path(__file__).resolve().parents[1]
CLI = ROOT / "src" / "SpellFire.MemoryRobot.Cli" / "bin" / "Debug" / "net48" / "SpellFire.MemoryRobot.Cli.exe"


def run_case(name, *args, expect_exit=0, expect_substrings=()):
    print(f"START memoryrobot-memory-case Name={name}")
    completed = subprocess.run([str(CLI), *args], capture_output=True, text=True, encoding="utf-8", errors="replace")
    output = (completed.stdout or "").strip()
    if output:
        print(output)
    ok = completed.returncode == expect_exit and all(token in output for token in expect_substrings)
    if ok:
        print(f"OK memoryrobot-memory-case Name={name}")
        return True
    print(f"FAIL memoryrobot-memory-case Name={name} Exit={completed.returncode} Output={output!r}")
    return False


def main():
    passed = True
    passed &= run_case("module-snapshot", "module-snapshot", expect_substrings=["Count=", "First=Name="])
    passed &= run_case("memory-region", "memory-region", expect_substrings=["Reason=\"MemoryRegionReady\"", "Region=Base=0x"])
    passed &= run_case("remote-alloc-free", "remote-alloc-free", expect_substrings=["Freed=True"])
    passed &= run_case("write-remote-allocation", "write-remote-allocation", expect_substrings=["WriteSuccess=True", "PayloadMatches=True", "Freed=True"])
    passed &= run_case("try-read-invalid", "try-read-invalid", expect_substrings=["Success=False", "RequestedBytes=4"])
    if passed:
        print("OK memoryrobot-memory-matrix")
        return 0
    print("FAIL memoryrobot-memory-matrix")
    return 1


if __name__ == "__main__":
    sys.exit(main())
