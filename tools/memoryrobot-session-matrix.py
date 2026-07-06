import pathlib
import subprocess
import sys


ROOT = pathlib.Path(__file__).resolve().parents[1]
CLI = ROOT / "src" / "SpellFire.MemoryRobot.Cli" / "bin" / "Debug" / "net48" / "SpellFire.MemoryRobot.Cli.exe"


def run_case(name, *args, expect_exit=0, expect_substrings=()):
    print(f"START memoryrobot-session-case Name={name}")
    completed = subprocess.run([str(CLI), *args], capture_output=True, text=True, encoding="utf-8", errors="replace")
    output = (completed.stdout or "").strip()
    if output:
        print(output)
    ok = completed.returncode == expect_exit and all(token in output for token in expect_substrings)
    if ok:
        print(f"OK memoryrobot-session-case Name={name}")
        return True
    print(f"FAIL memoryrobot-session-case Name={name} Exit={completed.returncode} Output={output!r}")
    return False


def main():
    passed = True
    passed &= run_case("missing-process-probe", "probe-expect", "999999", "ProcessUnavailable", expect_substrings=["ActualReason=\"ProcessUnavailable\""])
    passed &= run_case("session-open-close", "session-open-close", expect_substrings=["CloseResult=True", "HasClosedSnapshot=False"])
    passed &= run_case("close-then-reopen", "close-then-reopen", expect_substrings=["CloseResult=True", "SecondOpen=True"])
    passed &= run_case("snapshot-after-close", "snapshot-after-close", expect_substrings=["CloseResult=True", "HasSnapshot=False"])
    passed &= run_case("session-close-all", "session-close-all", expect_substrings=["HasBefore=True", "HasAfter=False"])
    passed &= run_case("process-exit-after-open", "process-exit-after-open", expect_substrings=["AcquireFailed=True", "HasAfterClose=False"])
    if passed:
        print("OK memoryrobot-session-matrix")
        return 0
    print("FAIL memoryrobot-session-matrix")
    return 1


if __name__ == "__main__":
    sys.exit(main())
