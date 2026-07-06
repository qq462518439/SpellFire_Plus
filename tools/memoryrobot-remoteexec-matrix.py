import pathlib
import subprocess
import sys


ROOT = pathlib.Path(__file__).resolve().parents[1]
CLI = ROOT / "src" / "SpellFire.MemoryRobot.Cli" / "bin" / "Debug" / "net48" / "SpellFire.MemoryRobot.Cli.exe"


def run_case(name, *args, expect_exit=0, expect_substrings=()):
    print(f"START memoryrobot-remoteexec-case Name={name}")
    completed = subprocess.run([str(CLI), *args], capture_output=True, text=True, encoding="utf-8", errors="replace")
    output = (completed.stdout or "").strip()
    if output:
        print(output)
    ok = completed.returncode == expect_exit and all(token in output for token in expect_substrings)
    if ok:
        print(f"OK memoryrobot-remoteexec-case Name={name}")
        return True
    print(f"FAIL memoryrobot-remoteexec-case Name={name} Exit={completed.returncode} Output={output!r}")
    return False


def main():
    passed = True
    passed &= run_case("remote-thread-invalid-start", "remote-thread-invalid-start", expect_substrings=["Exception=\"ArgumentException\""])
    passed &= run_case("self-remote-thread-get-current-process-id", "self-remote-thread-get-current-process-id", expect_substrings=["ExitCode=", "Expected="])
    passed &= run_case("load-library-missing-file", "load-library-missing-file", expect_substrings=["Exception=\"FileNotFoundException\""])
    passed &= run_case("self-load-library-known-system-dll", "self-load-library-known-system-dll", expect_substrings=["Reason=\"LoadLibrarySucceeded\"", "ModuleHandle=0x"])
    passed &= run_case("self-free-library-known-system-dll", "self-free-library-known-system-dll", expect_substrings=["FreeReason=\"FreeLibrarySucceeded\""])
    passed &= run_case("self-load-then-free-library-known-system-dll", "self-load-then-free-library-known-system-dll", expect_substrings=["LoadReason=\"LoadLibrarySucceeded\"", "FreeReason=\"FreeLibrarySucceeded\""])
    if passed:
        print("OK memoryrobot-remoteexec-matrix")
        return 0
    print("FAIL memoryrobot-remoteexec-matrix")
    return 1


if __name__ == "__main__":
    sys.exit(main())
