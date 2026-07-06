import subprocess
import sys
import time
import math
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CLI = ROOT / "src" / "SpellFire.WowRuntime.Cli" / "bin" / "Debug" / "net48" / "SpellFire.WowRuntime.Cli.exe"


def run_case(name, command, pid, expected_exit, expected_parts):
    args = [str(CLI), "--command", command, "--pid", str(pid)]
    print(f"START wowruntime-movement-case Name={name} Command={command} Pid={pid}")
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
        print(f"OK wowruntime-movement-case Name={name} Exit={completed.returncode}")
        return True

    print(f"FAIL wowruntime-movement-case Name={name} Exit={completed.returncode} ExpectedExit={expected_exit}")
    for part in expected_parts:
        if part not in output:
            print(f"MISS wowruntime-movement-case Name={name} Expected={part!r}")
    return False


def run_cli(command, pid):
    args = [str(CLI), "--command", command, "--pid", str(pid)]
    return run_cli_args(args)


def run_cli_args(args):
    return subprocess.run(
        args,
        cwd=str(ROOT),
        text=True,
        encoding="utf-8",
        errors="replace",
        capture_output=True,
    )


def parse_position(output):
    marker = "Pos=("
    index = output.find(marker)
    if index < 0:
        return None

    start = index + len(marker)
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


def parse_guid(output):
    marker = "Guid=0x"
    index = output.find(marker)
    if index < 0:
        return None

    start = index + len(marker)
    end = start
    while end < len(output) and output[end] in "0123456789abcdefABCDEF":
        end += 1

    value = output[start:end]
    return value if value else None


def parse_speed(output):
    marker = "Speed="
    index = output.find(marker)
    if index < 0:
        return 0.0

    start = index + len(marker)
    end = start
    while end < len(output) and output[end] not in " \r\n\t":
        end += 1

    try:
        return float(output[start:end])
    except ValueError:
        return 0.0


def parse_float_field(output, field_name):
    marker = field_name + "="
    index = output.find(marker)
    if index < 0:
        return None

    start = index + len(marker)
    end = start
    while end < len(output) and output[end] not in " \r\n\t":
        end += 1

    try:
        return float(output[start:end])
    except ValueError:
        return None


def read_rotation(pid):
    last_output = ""
    for attempt in range(1, 4):
        completed = run_cli("world-player", pid)
        output = (completed.stdout + completed.stderr).strip()
        last_output = output
        if output:
            print(output)
        if completed.returncode == 0:
            return parse_float_field(output, "Rotation"), output
        if attempt < 3:
            time.sleep(0.2)
    return None, last_output


def read_player_position(pid):
    last_output = ""
    for attempt in range(1, 4):
        completed = run_cli("world-player", pid)
        output = (completed.stdout + completed.stderr).strip()
        last_output = output
        if output:
            print(output)
        if completed.returncode == 0:
            return parse_position(output), output
        if attempt < 3:
            time.sleep(0.2)
    return None, last_output


def run_face_to_sample(pid):
    print(f"START wowruntime-movement-face-to-sample Pid={pid}")
    rotation, output = read_rotation(pid)
    position = parse_position(output)
    if rotation is None or position is None:
        print(f"FAIL wowruntime-movement-face-to-sample Stage=ReadPlayer Output=\"{output}\"")
        return False

    target_rotation = rotation + 1.1
    target_x = position[0] + math.cos(target_rotation) * 12.0
    target_y = position[1] + math.sin(target_rotation) * 12.0
    target_z = position[2]
    args = [
        str(CLI),
        "--command",
        "movement-face-to",
        "--pid",
        str(pid),
        "--x",
        f"{target_x:.3f}",
        "--y",
        f"{target_y:.3f}",
        "--z",
        f"{target_z:.3f}",
    ]
    completed = run_cli_args(args)
    face_output = (completed.stdout + completed.stderr).strip()
    if face_output:
        print(face_output)

    final_error = parse_float_field(face_output, "FaceFinalError")
    start_error = parse_float_field(face_output, "FaceStartError")
    ok = (
        completed.returncode == 0
        and 'Action="face-to"' in face_output
        and 'Reason="Ready"' in face_output
        and final_error is not None
        and start_error is not None
        and abs(final_error) <= 0.12
    )

    stopped = run_cli("movement-turn-stop", pid)
    stop_output = (stopped.stdout + stopped.stderr).strip()
    if stop_output:
        print(stop_output)

    if ok:
        print(f"OK wowruntime-movement-face-to-sample StartError={start_error:.3f} FinalError={final_error:.3f}")
        return True

    print(f"FAIL wowruntime-movement-face-to-sample StartError={start_error} FinalError={final_error}")
    return False


def run_face_object_sample(pid):
    print(f"START wowruntime-movement-face-object-sample Pid={pid}")
    nearest = run_cli_args([
        str(CLI),
        "--command",
        "object-nearest",
        "--pid",
        str(pid),
        "--kind",
        "Unit",
        "--radius",
        "120",
    ])
    nearest_output = (nearest.stdout + nearest.stderr).strip()
    if nearest_output:
        print(nearest_output)
    guid = parse_guid(nearest_output)
    if nearest.returncode != 0 or not guid:
        print(f"SKIP wowruntime-movement-face-object-sample Reason=\"NoNearestUnit\"")
        return True

    completed = run_cli_args([
        str(CLI),
        "--command",
        "movement-face-object",
        "--pid",
        str(pid),
        "--guid",
        "0x" + guid,
    ])
    output = (completed.stdout + completed.stderr).strip()
    if output:
        print(output)

    final_error = parse_float_field(output, "FaceFinalError")
    ok = (
        completed.returncode == 0
        and 'Action="face-object"' in output
        and 'Reason="Ready"' in output
        and f"GUID=0X{guid.upper()}" in output.upper()
        and final_error is not None
        and abs(final_error) <= 0.12
    )

    stopped = run_cli("movement-turn-stop", pid)
    stop_output = (stopped.stdout + stopped.stderr).strip()
    if stop_output:
        print(stop_output)

    if ok:
        print(f"OK wowruntime-movement-face-object-sample Guid=0x{guid} FinalError={final_error:.3f}")
        return True

    print(f"FAIL wowruntime-movement-face-object-sample Guid=0x{guid} FinalError={final_error}")
    return False


def run_movement_active_sample(pid):
    print(f"START wowruntime-movement-active-sample Pid={pid}")
    started = run_cli("movement-forward-start", pid)
    start_output = (started.stdout + started.stderr).strip()
    if start_output:
        print(start_output)
    if started.returncode != 0 or 'Action="forward-start"' not in start_output or 'RuntimeReason="LuaExecuteSucceeded"' not in start_output:
        print(f"FAIL wowruntime-movement-active-sample Stage=Start Exit={started.returncode}")
        return False

    observed = False
    observed_output = ""
    try:
        for sample in range(1, 9):
            time.sleep(0.25)
            state = run_cli("movement-state", pid)
            output = (state.stdout + state.stderr).strip()
            if output:
                print(f"SAMPLE wowruntime-movement-active-sample Index={sample} {output}")
            if state.returncode == 0 and ("InMovement=True" in output or parse_speed(output) > 0):
                observed = True
                observed_output = output
                break
    finally:
        stopped = run_cli("movement-stop", pid)
        stop_output = (stopped.stdout + stopped.stderr).strip()
        if stop_output:
            print(stop_output)

    if observed:
        print(f"OK wowruntime-movement-active-sample Evidence=\"{observed_output}\"")
        return True

    print("FAIL wowruntime-movement-active-sample Reason=\"MovementStateDidNotBecomeActive\"")
    return False


def run_movement_speed_sample(pid, action, expected_action, expected_detail):
    print(f"START wowruntime-movement-speed-sample Action={action} Pid={pid}")
    completed = run_cli_args([str(CLI), "--command", "movement-speed-sample", "--pid", str(pid), "--action", action])
    output = (completed.stdout + completed.stderr).strip()
    if output:
        print(output)

    distance = parse_float_field(output, "Distance")
    computed_speed = parse_float_field(output, "ComputedSpeed")
    if (
        completed.returncode == 0
        and "Moved=True" in output
        and distance is not None
        and distance > 0.3
        and computed_speed is not None
        and computed_speed > 0.3
        and f'Action="{expected_action}"' in output
        and f'Detail="{expected_detail}"' in output
    ):
        print(f"OK wowruntime-movement-speed-sample Action={action}")
        return True

    print(f"FAIL wowruntime-movement-speed-sample Action={action} Exit={completed.returncode}")
    print(f"FAIL wowruntime-movement-speed-sample Action={action} Distance={distance} ComputedSpeed={computed_speed}")
    return False


def run_movement_go_sample(pid):
    print(f"START wowruntime-movement-go-sample Pid={pid}")
    before, before_output = read_player_position(pid)
    if before is None:
        print(f"FAIL wowruntime-movement-go-sample Stage=ReadBefore Output=\"{before_output}\"")
        return False

    target = (before[0] + 8.0, before[1], before[2])
    completed = run_cli_args([
        str(CLI),
        "--command",
        "movement-go",
        "--pid",
        str(pid),
        "--x",
        f"{target[0]:.3f}",
        "--y",
        f"{target[1]:.3f}",
        "--z",
        f"{target[2]:.3f}",
    ])
    go_output = (completed.stdout + completed.stderr).strip()
    if go_output:
        print(go_output)

    state = None
    state_output = ""
    observed_active_state = False
    for sample in range(1, 7):
        time.sleep(0.2)
        state = run_cli("movement-state", pid)
        state_output = (state.stdout + state.stderr).strip()
        if state_output:
            print(f"SAMPLE wowruntime-movement-go-sample Index={sample} {state_output}")
        if state.returncode == 0 and ("InMovement=True" in state_output or "ClickToMoveTypeRaw=4" in state_output):
            observed_active_state = True
            break

    time.sleep(1.0)
    after, after_output = read_player_position(pid)
    stopped = run_cli("movement-stop", pid)
    stop_output = (stopped.stdout + stopped.stderr).strip()
    if stop_output:
        print(stop_output)

    moved_distance = distance(before, after) if after is not None else 0.0
    ok = (
        completed.returncode == 0
        and 'Action="go-ctm"' in go_output
        and 'RuntimeReason="ClickToMoveCommandSucceeded"' in go_output
        and "OK:CGPlayer_C__ClickToMove" in go_output
        and state is not None
        and state.returncode == 0
        and observed_active_state
        and after is not None
        and moved_distance >= 0.25
        and stopped.returncode == 0
    )

    if ok:
        print(
            "OK wowruntime-movement-go-sample "
            f"MovedDistance={moved_distance:.3f} "
            f"ActiveStateObserved={observed_active_state} "
            f"Before=({before[0]:.3f},{before[1]:.3f},{before[2]:.3f}) "
            f"After=({after[0]:.3f},{after[1]:.3f},{after[2]:.3f})"
        )
        return True

    print(
        "FAIL wowruntime-movement-go-sample "
        f"GoExit={completed.returncode} StateExit={state.returncode if state is not None else 'None'} StopExit={stopped.returncode} "
        f"MovedDistance={moved_distance:.3f} ActiveStateObserved={observed_active_state}"
    )
    return False


def run_turn_action_sample(pid, start_command, expected_action):
    print(f"START wowruntime-movement-turn-sample Command={start_command} Pid={pid}")
    started = run_cli(start_command, pid)
    start_output = (started.stdout + started.stderr).strip()
    if start_output:
        print(start_output)

    ok = (
        started.returncode == 0
        and f'Action="{expected_action}"' in start_output
        and 'RuntimeReason="LuaExecuteSucceeded"' in start_output
        and "TextPayload=OK:FrameScriptExecute=" in start_output
    )

    try:
        time.sleep(0.25)
    finally:
        stopped = run_cli("movement-turn-stop", pid)
        stop_output = (stopped.stdout + stopped.stderr).strip()
        if stop_output:
            print(stop_output)
        ok = (
            ok
            and stopped.returncode == 0
            and 'Action="turn-stop"' in stop_output
            and 'RuntimeReason="LuaExecuteSucceeded"' in stop_output
            and "TextPayload=OK:FrameScriptExecute=" in stop_output
        )

    if ok:
        print(f"OK wowruntime-movement-turn-sample Command={start_command}")
        return True

    print(f"FAIL wowruntime-movement-turn-sample Command={start_command}")
    return False


def run_turn_rotation_sample(pid, start_command, expected_action):
    print(f"START wowruntime-movement-turn-rotation Command={start_command} Pid={pid}")
    before, before_output = read_rotation(pid)
    if before is None:
        print(f"FAIL wowruntime-movement-turn-rotation Stage=ReadBefore Output=\"{before_output}\"")
        return False

    started = run_cli(start_command, pid)
    start_output = (started.stdout + started.stderr).strip()
    if start_output:
        print(start_output)
    if started.returncode != 0 or f'Action="{expected_action}"' not in start_output or 'RuntimeReason="LuaExecuteSucceeded"' not in start_output:
        print(f"FAIL wowruntime-movement-turn-rotation Stage=Start Exit={started.returncode}")
        return False

    observed = False
    observed_rotation = before
    observed_delta = 0.0
    try:
        for sample in range(1, 9):
            time.sleep(0.25)
            current, output = read_rotation(pid)
            if current is None:
                continue
            delta = abs(current - before)
            wrapped_delta = min(delta, abs((current + 6.283185307179586) - before), abs(current - (before + 6.283185307179586)))
            print(f"SAMPLE wowruntime-movement-turn-rotation Index={sample} Before={before:.3f} Current={current:.3f} Delta={wrapped_delta:.3f}")
            if wrapped_delta >= 0.02:
                observed = True
                observed_rotation = current
                observed_delta = wrapped_delta
                break
    finally:
        stopped = run_cli("movement-turn-stop", pid)
        stop_output = (stopped.stdout + stopped.stderr).strip()
        if stop_output:
            print(stop_output)

    if observed:
        print(f"OK wowruntime-movement-turn-rotation Command={start_command} Before={before:.3f} After={observed_rotation:.3f} Delta={observed_delta:.3f}")
        return True

    print(f"FAIL wowruntime-movement-turn-rotation Command={start_command} Reason=\"RotationDidNotChange\" Before={before:.3f}")
    return False


def find_wow_pid():
    completed = subprocess.run(
        [
            "powershell",
            "-NoProfile",
            "-Command",
            "Get-Process -Name Wow -ErrorAction SilentlyContinue | Sort-Object StartTime -Descending | Select-Object -ExpandProperty Id",
        ],
        text=True,
        encoding="utf-8",
        errors="replace",
        capture_output=True,
    )
    for line in completed.stdout.splitlines():
        value = line.strip()
        if not value.isdigit():
            continue

        pid = int(value)
        phase = run_cli("world-phase", pid)
        output = (phase.stdout + phase.stderr).strip()
        if output:
            print(f"PROBE wowruntime-movement-live Pid={pid} {output}")
        if phase.returncode == 0 and "Phase=InWorld" in output:
            return pid

    return 0


def main():
    if not CLI.exists():
        print(f"FAIL wowruntime-movement-matrix Reason=\"CliMissing\" Path=\"{CLI}\"")
        return 2

    passed = True
    missing_pid = 999999
    passed = run_case(
        "missing-movement-jump",
        "movement-jump",
        missing_pid,
        1,
        ["Ready=False", 'Reason="ProcessUnavailable"'],
    ) and passed
    passed = run_case(
        "missing-movement-state",
        "movement-state",
        missing_pid,
        1,
        ["Ready=False", 'Reason="ProcessUnavailable"', "InMovement=Unknown", "ClickToMoveTypeRaw=Unknown", "ClickToMoveState=Unknown"],
    ) and passed
    passed = run_case(
        "missing-movement-go",
        "movement-go",
        missing_pid,
        1,
        ["Ready=False", 'Reason="InvalidArgument"', "requires explicit --x --y --z"],
    ) and passed

    wow_pid = find_wow_pid()
    if wow_pid <= 0:
        print('SKIP wowruntime-movement-live Reason="NoWowProcess"')
    else:
        passed = run_case(
            "live-movement-state",
            "movement-state",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', "InMovement=", "Flags=", "ClickToMoveTypeRaw=", "ClickToMoveState=", "SpeedKnown=", 'Source="WorldState"', "Phase="],
        ) and passed
        passed = run_case(
            "live-movement-ctm-diagnostic",
            "movement-ctm-diagnostic",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', "Phase=", "Pos=(", "Rotation=", "InMovement=", "ClickToMoveTypeRaw=", "ClickToMoveState=", "SpeedKnown=", "ReadOnly=False", "CtmMoveKnown=True", "CtmStopNativeKnown=False"],
        ) and passed
        passed = run_case(
            "live-movement-jump",
            "movement-jump",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', 'Action="jump"', 'RuntimeReason="LuaExecuteSucceeded"', "TextPayload=OK:FrameScriptExecute="],
        ) and passed
        passed = run_movement_active_sample(wow_pid) and passed
        passed = run_movement_speed_sample(wow_pid, "forward", "forward-start", "Forward movement produced measurable displacement speed evidence.") and passed
        passed = run_movement_speed_sample(wow_pid, "backward", "backward-start", "Backward movement produced measurable displacement speed evidence.") and passed
        passed = run_movement_speed_sample(wow_pid, "strafe-left", "strafe-left-start", "Strafe-left movement produced measurable displacement speed evidence.") and passed
        passed = run_movement_speed_sample(wow_pid, "strafe-right", "strafe-right-start", "Strafe-right movement produced measurable displacement speed evidence.") and passed
        passed = run_movement_go_sample(wow_pid) and passed
        passed = run_turn_action_sample(wow_pid, "movement-turn-left-start", "turn-left-start") and passed
        passed = run_turn_action_sample(wow_pid, "movement-turn-right-start", "turn-right-start") and passed
        passed = run_turn_rotation_sample(wow_pid, "movement-turn-left-start", "turn-left-start") and passed
        passed = run_turn_rotation_sample(wow_pid, "movement-turn-right-start", "turn-right-start") and passed
        passed = run_case(
            "movement-face-to-not-stable",
            "movement-face-to",
            wow_pid,
            1,
            ["Ready=False", 'Reason="FeatureUnavailable"', "Precise facing is not stable enough"],
        ) and passed
        passed = run_case(
            "movement-face-object-not-stable",
            "movement-face-object",
            wow_pid,
            1,
            ["Ready=False", 'Reason="FeatureUnavailable"', "Precise object facing is not stable enough"],
        ) and passed
        passed = run_case(
            "live-movement-stop",
            "movement-stop",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', 'Action="stop"', 'RuntimeReason="LuaExecuteSucceeded"', "TextPayload=OK:FrameScriptExecute="],
        ) and passed
        passed = run_case(
            "live-movement-stop-to",
            "movement-stop-to",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', 'Action="stop-to"', 'RuntimeReason="LuaExecuteSucceeded"', "TextPayload=OK:FrameScriptExecute="],
        ) and passed

    if passed:
        print("OK wowruntime-movement-matrix")
        return 0

    print("FAIL wowruntime-movement-matrix")
    return 1


if __name__ == "__main__":
    sys.exit(main())
