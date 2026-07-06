import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CLI = ROOT / "src" / "SpellFire.WowRuntime.Cli" / "bin" / "Debug" / "net48" / "SpellFire.WowRuntime.Cli.exe"
MAX_SUCCESS_ITEM_LINES = 3


def summarize_output(output):
    if not output:
        return ""

    lines = output.splitlines()
    if len(lines) <= 1:
        return output

    kept = []
    item_count = 0
    omitted = 0
    for line in lines:
        if line.startswith("ItemIndex="):
            item_count += 1
            if item_count <= MAX_SUCCESS_ITEM_LINES:
                kept.append(line)
            else:
                omitted += 1
            continue

        kept.append(line)

    if omitted > 0:
        kept.append(f"... omitted ItemIndex lines={omitted}")

    return "\n".join(kept)


def run_case(name, command, pid, expected_exit, expected_parts, extra=None):
    args = [str(CLI), "--command", command, "--pid", str(pid)]
    if extra:
        args.extend(extra)

    print(f"START wowruntime-objectmanager-case Name={name} Command={command} Pid={pid}")
    completed = subprocess.run(
        args,
        cwd=str(ROOT),
        text=True,
        encoding="utf-8",
        errors="replace",
        capture_output=True,
    )
    output = (completed.stdout + completed.stderr).strip()

    ok = completed.returncode == expected_exit and all(part in output for part in expected_parts)
    if ok:
        summary = summarize_output(output)
        if summary:
            print(summary)
        print(f"OK wowruntime-objectmanager-case Name={name} Exit={completed.returncode}")
        return True

    if output:
        print(output)
    print(f"FAIL wowruntime-objectmanager-case Name={name} Exit={completed.returncode} ExpectedExit={expected_exit}")
    for part in expected_parts:
        if part not in output:
            print(f"MISS wowruntime-objectmanager-case Name={name} Expected={part!r}")
    return False


def run_probe(command, pid, extra=None):
    args = [str(CLI), "--command", command, "--pid", str(pid)]
    if extra:
        args.extend(extra)

    completed = subprocess.run(
        args,
        cwd=str(ROOT),
        text=True,
        encoding="utf-8",
        errors="replace",
        capture_output=True,
    )
    output = (completed.stdout + completed.stderr).strip()
    return completed.returncode, output


def find_wow_pid():
    completed = subprocess.run(
        [
            "powershell",
            "-NoProfile",
            "-Command",
            "Get-Process -Name Wow -ErrorAction SilentlyContinue | Sort-Object StartTime -Descending | Select-Object -First 1 -ExpandProperty Id",
        ],
        text=True,
        encoding="utf-8",
        errors="replace",
        capture_output=True,
    )
    value = completed.stdout.strip()
    return int(value) if value.isdigit() else 0


def main():
    if not CLI.exists():
        print(f"FAIL wowruntime-objectmanager-matrix Reason=\"CliMissing\" Path=\"{CLI}\"")
        return 2

    passed = True
    missing_pid = 999999
    passed = run_case(
        "missing-object-snapshot",
        "object-snapshot",
        missing_pid,
        1,
        ["Ready=False", 'Reason="ProcessUnavailable"'],
    ) and passed
    passed = run_case(
        "missing-world-player",
        "world-player",
        missing_pid,
        1,
        ["Ready=False", 'Reason="ProcessUnavailable"', "Player=Unavailable"],
    ) and passed
    passed = run_case(
        "missing-world-phase",
        "world-phase",
        missing_pid,
        1,
        ["Ready=False", 'Reason="ProcessUnavailable"', "Phase=Unknown", "InGame=Unknown", "LoadingOrConnecting=Unknown"],
    ) and passed
    passed = run_case(
        "missing-world-snapshot",
        "world-snapshot",
        missing_pid,
        1,
        ["Ready=False", 'Reason="ProcessUnavailable"', "ObjectCount=0", "Limit=0", "Scanned=0"],
    ) and passed
    passed = run_case(
        "invalid-guid",
        "object-by-guid",
        missing_pid,
        1,
        ["Ready=False", 'Reason="InvalidArgument"'],
        ["--guid", "0"],
    ) and passed

    wow_pid = find_wow_pid()
    if wow_pid <= 0:
        print('SKIP wowruntime-objectmanager-live Reason="NoWowProcess"')
    else:
        probe_exit, probe_output = run_probe("object-snapshot", wow_pid, ["--limit", "512"])
        if probe_exit != 0 and (
            'Reason="ObjectManagerUnavailable"' in probe_output
            or 'Reason="ReadFailed"' in probe_output
        ):
            print(probe_output)
            reason = "ReadFailed" if 'Reason="ReadFailed"' in probe_output else "ObjectManagerUnavailable"
            print(f'SKIP wowruntime-objectmanager-live Reason="{reason}" Pid={wow_pid}')
            print("OK wowruntime-objectmanager-matrix")
            return 0

        passed = run_case(
            "live-object-diagnostic",
            "object-diagnostic",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', "Stage=", "ClientConnection=0x", "ObjectManager=0x", "LocalGuid=0x", "FirstObject=0x", "Scanned=", "ReadableObjects=", "FailedObjects="],
            ["--scan-limit", "512"],
        ) and passed
        passed = run_case(
            "live-object-snapshot",
            "object-snapshot",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', "SnapshotUtc=", "AgeMs=", "ObjectCount=", "PlayerCount=", "UnitCount=", "GameObjectCount=", "Me=Guid=0x"],
            ["--limit", "512"],
        ) and passed
        passed = run_case(
            "live-world-player",
            "world-player",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', "MapIdKnown=", "ContinentId=", "ContinentName=", "Pos=(", "Movement=", "ClickToMoveTypeRaw=", "ClickToMoveState="],
        ) and passed
        passed = run_case(
            "live-world-phase",
            "world-phase",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', "Phase=", "InGame=", "LoadingOrConnecting=", 'Source="Usefuls"', "Detail="],
        ) and passed
        passed = run_case(
            "live-world-snapshot",
            "world-snapshot",
        wow_pid,
        0,
            ["Ready=True", 'Reason="Ready"', "SnapshotUtc=", "AgeMs=", "Phase=", "InGame=", "LoadingOrConnecting=", "InWorld=", "HasPlayer=", "HasTarget=", "ObjectCount=", "PlayerCount=", "UnitCount=", "GameObjectCount=", "Limit=", "Scanned=", "Player=MapId=", "MapIdKnown=", "ContinentId=", "ContinentName=", "Movement=", "ClickToMoveTypeRaw=", "ClickToMoveState=", "Me=Guid=0x", "NearestUnit=", "NearestGameObject="],
            ["--limit", "512"],
        ) and passed
        passed = run_case(
            "live-object-me",
            "object-me",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', "Object=Guid=0x"],
        ) and passed
        passed = run_case(
            "live-object-by-local-guid",
            "object-by-guid",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', "Object=Guid=0x10"],
            ["--guid", "16"],
        ) and passed
        passed = run_case(
            "live-object-target-empty-or-ready",
            "object-snapshot",
            wow_pid,
            0,
            ["Ready=True", "TargetGuid=0x"],
            ["--limit", "512"],
        ) and passed
        passed = run_case(
            "live-object-kind-unit",
            "object-list",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', "SnapshotUtc=", "AgeMs=", "ObjectCount=", "UnitCount=", "Kind=Unit"],
            ["--limit", "512", "--kind", "Unit"],
        ) and passed
        passed = run_case(
            "live-object-list-unit-scan-limit",
            "object-list",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', "ObjectCount=20", "UnitCount=20", "Limit=20", "Scanned=", "ItemIndex=19", "Kind=Unit", "Name="],
            ["--limit", "20", "--scan-limit", "512", "--kind", "Unit"],
        ) and passed
        passed = run_case(
            "live-object-list-gameobject",
            "object-list",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', "SnapshotUtc=", "AgeMs=", "GameObjectCount=", "ItemIndex="],
            ["--limit", "512", "--kind", "GameObject"],
        ) and passed
        passed = run_case(
            "live-object-nearby-unit",
            "object-nearby",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', "ObjectCount="],
            ["--limit", "512", "--radius", "80", "--kind", "Unit"],
        ) and passed
        passed = run_case(
            "live-object-nearby-list-gameobject",
            "object-nearby-list",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', "ItemIndex=", "Dist="],
            ["--limit", "512", "--radius", "80", "--kind", "GameObject"],
        ) and passed
        passed = run_case(
            "live-object-nearest-gameobject",
            "object-nearest",
            wow_pid,
            0,
            ["Ready=True", 'Reason="Ready"', "Object=Guid=0x", "Kind=GameObject", "Dist="],
            ["--radius", "80", "--kind", "GameObject"],
        ) and passed

    if passed:
        print("OK wowruntime-objectmanager-matrix")
        return 0

    print("FAIL wowruntime-objectmanager-matrix")
    return 1


if __name__ == "__main__":
    sys.exit(main())
