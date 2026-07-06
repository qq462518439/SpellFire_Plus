import math
import subprocess
import sys
import time
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
RUNTIME_CLI = ROOT / "src" / "SpellFire.RuntimeHost.Cli" / "bin" / "Debug" / "net48" / "SpellFire.RuntimeHost.Cli.exe"
WOW_CLI = ROOT / "src" / "SpellFire.WowRuntime.Cli" / "bin" / "Debug" / "net48" / "SpellFire.WowRuntime.Cli.exe"


def run(args):
    completed = subprocess.run(
        [str(arg) for arg in args],
        cwd=str(ROOT),
        text=True,
        encoding="utf-8",
        errors="replace",
        capture_output=True,
    )
    output = (completed.stdout + completed.stderr).strip()
    if output:
        print(output)
    return completed.returncode, output


def parse_position(output):
    marker = "Pos=("
    start = output.find(marker)
    if start < 0:
        return None
    start += len(marker)
    end = output.find(")", start)
    if end < 0:
        return None
    parts = output[start:end].split(",")
    if len(parts) < 3:
        return None
    try:
        return float(parts[0]), float(parts[1]), float(parts[2])
    except ValueError:
        return None


def distance(a, b):
    return math.sqrt((a[0] - b[0]) ** 2 + (a[1] - b[1]) ** 2 + (a[2] - b[2]) ** 2)


def main():
    if len(sys.argv) < 2:
        print("FAIL wowruntime-ctm-matrix Reason=\"MissingPid\" Usage=\"python tools\\wowruntime-ctm-matrix.py <pid>\"")
        return 2

    pid = sys.argv[1]
    print(f"START wowruntime-ctm-matrix Pid={pid}")

    checks = []
    checks.append(run([RUNTIME_CLI, "attach", pid]))
    checks.append(run([RUNTIME_CLI, "lua-smoke", pid]))

    before_exit, before_output = run([WOW_CLI, "--command", "world-player", "--pid", pid])
    before = parse_position(before_output)
    if before_exit != 0 or before is None:
        print("FAIL wowruntime-ctm-matrix Stage=\"ReadBefore\"")
        return 1

    target = (before[0] + 8.0, before[1], before[2])
    ctm_exit, ctm_output = run([
        RUNTIME_CLI,
        "ctm-move",
        pid,
        f"{target[0]:.3f}",
        f"{target[1]:.3f}",
        f"{target[2]:.3f}",
        "0",
        "4",
        "0.5",
    ])

    state_exit = 1
    state_output = ""
    observed_active_state = False
    for sample in range(1, 7):
        time.sleep(0.2)
        state_exit, state_output = run([WOW_CLI, "--command", "movement-state", "--pid", pid])
        if state_exit == 0 and ("InMovement=True" in state_output or "ClickToMoveTypeRaw=4" in state_output):
            observed_active_state = True
            break
    time.sleep(1.0)
    after_exit, after_output = run([WOW_CLI, "--command", "world-player", "--pid", pid])
    stop_exit, stop_output = run([WOW_CLI, "--command", "movement-stop", "--pid", pid])

    after = parse_position(after_output)
    moved_distance = distance(before, after) if after is not None else 0.0
    ok = (
        all(code == 0 for code, _ in checks)
        and ctm_exit == 0
        and "ClickToMoveCommandSucceeded" in ctm_output
        and "OK:CGPlayer_C__ClickToMove" in ctm_output
        and state_exit == 0
        and observed_active_state
        and after_exit == 0
        and after is not None
        and moved_distance >= 0.25
        and stop_exit == 0
    )

    if ok:
        print(
            "OK wowruntime-ctm-matrix "
            f"MovedDistance={moved_distance:.3f} "
            f"ActiveStateObserved={observed_active_state} "
            f"Before=({before[0]:.3f},{before[1]:.3f},{before[2]:.3f}) "
            f"After=({after[0]:.3f},{after[1]:.3f},{after[2]:.3f})"
        )
        return 0

    print(
        "FAIL wowruntime-ctm-matrix "
        f"CtmExit={ctm_exit} StateExit={state_exit} AfterExit={after_exit} StopExit={stop_exit} "
        f"MovedDistance={moved_distance:.3f} ActiveStateObserved={observed_active_state}"
    )
    return 1


if __name__ == "__main__":
    sys.exit(main())
