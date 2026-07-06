import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]


FILES = {
    "interface": ROOT / "src" / "SpellFire.WowRuntime" / "Movement" / "IMovementService.cs",
    "service": ROOT / "src" / "SpellFire.WowRuntime" / "Infrastructure" / "ScriptMovementService.cs",
    "cli": ROOT / "src" / "SpellFire.WowRuntime.Cli" / "Program.cs",
    "matrix": ROOT / "tools" / "wowruntime-movement-matrix.py",
    "contract": ROOT / "Documentation" / "待办事项" / "WowRuntime_Movement最小动作层成品契约.md",
}


REQUIRED = {
    "interface": [
        "StartMoveForward()",
        "StartMoveBackward()",
        "StartStrafeLeft()",
        "StartStrafeRight()",
        "StartTurnLeft()",
        "StartTurnRight()",
        "StopTurn()",
        "StopMove()",
        "StopMoveTo()",
        "FaceTo(Vector3 point)",
        "FaceObject(ulong guid)",
        "Go(IReadOnlyList<Vector3> points)",
    ],
    "service": [
        "MoveForwardStart();",
        "SPELLFIRE_MOVE_FORWARD_START_OK",
        "MoveBackwardStart();",
        "SPELLFIRE_MOVE_BACKWARD_START_OK",
        "StrafeLeftStart();",
        "SPELLFIRE_MOVE_STRAFE_LEFT_START_OK",
        "StrafeRightStart();",
        "SPELLFIRE_MOVE_STRAFE_RIGHT_START_OK",
        "TurnLeftStart();",
        "SPELLFIRE_MOVE_TURN_LEFT_START_OK",
        "TurnRightStart();",
        "SPELLFIRE_MOVE_TURN_RIGHT_START_OK",
        "MoveForwardStop(); MoveBackwardStop(); StrafeLeftStop(); StrafeRightStop(); AscendStop(); TurnLeftStop(); TurnRightStop();",
        "ReadOnly=True CtmWriteKnown=False",
        "Precise facing is not stable enough",
        "Path movement is not implemented in the minimal movement layer.",
    ],
    "cli": [
        'case "movement-forward-start"',
        'case "movement-backward-start"',
        'case "movement-strafe-left-start"',
        'case "movement-strafe-right-start"',
        'case "movement-speed-sample"',
        'case "forward"',
        'case "backward"',
        'case "strafe-left"',
        'case "strafe-right"',
        "Unsupported movement speed sample action.",
        'return "Forward";',
        'return "Backward";',
        'return "Strafe-left";',
        'return "Strafe-right";',
        "movement produced measurable displacement speed evidence.",
        "bool moved = hasPositionSamples && distance > 0.3 && computedSpeed > 0.3;",
    ],
    "matrix": [
        'run_movement_speed_sample(wow_pid, "forward", "forward-start", "Forward movement produced measurable displacement speed evidence.")',
        'run_movement_speed_sample(wow_pid, "backward", "backward-start", "Backward movement produced measurable displacement speed evidence.")',
        'run_movement_speed_sample(wow_pid, "strafe-left", "strafe-left-start", "Strafe-left movement produced measurable displacement speed evidence.")',
        'run_movement_speed_sample(wow_pid, "strafe-right", "strafe-right-start", "Strafe-right movement produced measurable displacement speed evidence.")',
        "Moved=True",
        "distance > 0.3",
        "computed_speed > 0.3",
    ],
    "contract": [
        "`Movement` 当前成品边界是可验证的最小动作层",
        "明确后置",
        "FaceTo(Vector3)",
        "FaceObject(ulong guid)",
        "Go(IReadOnlyList<Vector3>)",
        "CTM 写入",
        "禁止误判",
    ],
}


FORBIDDEN_SERVICE_SNIPPETS = [
    'return ExecuteAction("face-to"',
    'return ExecuteAction("face-object"',
    'return ExecuteAction("go"',
    "CtmWriteKnown=True",
]


def read_text(name):
    path = FILES[name]
    if not path.exists():
        return None
    return path.read_text(encoding="utf-8", errors="replace")


def main():
    violations = []
    texts = {}
    for name in FILES:
        text = read_text(name)
        if text is None:
            violations.append(f"MissingFile:{FILES[name]}")
            continue
        texts[name] = text

    for name, snippets in REQUIRED.items():
        text = texts.get(name, "")
        for snippet in snippets:
            if snippet not in text:
                violations.append(f"MissingSnippet:{name}:{snippet}")

    service = texts.get("service", "")
    for snippet in FORBIDDEN_SERVICE_SNIPPETS:
        if snippet in service:
            violations.append(f"ForbiddenSnippet:service:{snippet}")

    if "WowRuntimeStatus.FeatureUnavailable" not in service:
        violations.append("MissingSnippet:service:WowRuntimeStatus.FeatureUnavailable")

    if violations:
        print(f"FAIL wowruntime-movement-contract-guard Violations={len(violations)}")
        for item in violations:
            print(item)
        return 1

    scanned = ",".join(str(FILES[name].relative_to(ROOT)) for name in FILES)
    print(f"OK wowruntime-movement-contract-guard Files={len(FILES)} Scanned=\"{scanned}\"")
    return 0


if __name__ == "__main__":
    sys.exit(main())
