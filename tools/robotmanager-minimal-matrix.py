import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / "src" / "SpellFire.RobotManager" / "SpellFire.RobotManager.csproj"
CLI_PROJECT = ROOT / "src" / "SpellFire.RobotManager.Cli" / "SpellFire.RobotManager.Cli.csproj"
CLI = ROOT / "src" / "SpellFire.RobotManager.Cli" / "bin" / "Debug" / "net48" / "SpellFire.RobotManager.Cli.exe"


def run(args):
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
    return completed.returncode == 0


def find_wow_pid():
    completed = subprocess.run(
        [
            "powershell",
            "-NoProfile",
            "-Command",
            "Get-Process Wow -ErrorAction SilentlyContinue | Sort-Object StartTime -Descending | Select-Object -First 1 -ExpandProperty Id",
        ],
        cwd=str(ROOT),
        text=True,
        encoding="utf-8",
        errors="replace",
        capture_output=True,
    )
    output = (completed.stdout + completed.stderr).strip()
    if completed.returncode == 0 and output:
        return output.splitlines()[0].strip()
    return None


def run_cli_case(name, wow_pid, command, expected_parts):
    print(f"START robotmanager-minimal-case Name={name}")
    completed = subprocess.run(
        [str(CLI), "--command", command, "--pid", str(wow_pid)],
        cwd=str(ROOT),
        text=True,
        encoding="utf-8",
        errors="replace",
        capture_output=True,
    )
    output = (completed.stdout + completed.stderr).strip()
    if output:
        print(output)
    ok = completed.returncode == 0 and all(part in output for part in expected_parts)
    if ok:
        print(f"OK robotmanager-minimal-case Name={name}")
        return True

    print(f"FAIL robotmanager-minimal-case Name={name} Exit={completed.returncode}")
    for part in expected_parts:
        if part not in output:
            print(f"MISS robotmanager-minimal-case Name={name} Expected={part!r}")
    return False


def assert_project_reference():
    print("START robotmanager-minimal-case Name=project-reference")
    tree = ET.parse(PROJECT)
    refs = [
        item.attrib.get("Include", "")
        for item in tree.findall(".//ProjectReference")
    ]
    expected = ["..\\SpellFire.WowRuntime\\SpellFire.WowRuntime.csproj"]
    if refs == expected:
        print("OK robotmanager-minimal-case Name=project-reference")
        return True

    print(f"FAIL robotmanager-minimal-case Name=project-reference Refs={refs!r}")
    return False


def assert_cli_project_reference():
    print("START robotmanager-minimal-case Name=cli-project-reference")
    tree = ET.parse(CLI_PROJECT)
    refs = [
        item.attrib.get("Include", "")
        for item in tree.findall(".//ProjectReference")
    ]
    expected = [
        "..\\SpellFire.RobotManager\\SpellFire.RobotManager.csproj",
        "..\\SpellFire.WowRuntime\\SpellFire.WowRuntime.csproj",
    ]
    if refs == expected:
        print("OK robotmanager-minimal-case Name=cli-project-reference")
        return True

    print(f"FAIL robotmanager-minimal-case Name=cli-project-reference Refs={refs!r}")
    return False


def assert_required_files():
    print("START robotmanager-minimal-case Name=required-files")
    required = [
        "Core/IRobotManager.cs",
        "Core/IProduct.cs",
        "Core/ProductContext.cs",
        "Core/ProductState.cs",
        "Core/RobotManager.cs",
        "Core/RobotManagerResult.cs",
        "Core/RobotManagerStatus.cs",
        "Core/RobotPulseLoop.cs",
        "Core/RobotPulseLoopResult.cs",
        "Testing/ContextProbeProduct.cs",
        "Testing/NoopProduct.cs",
        "Testing/ThrowingProduct.cs",
    ]

    missing = [
        relative for relative in required
        if not (PROJECT.parent / relative).exists()
    ]
    if not missing:
        print("OK robotmanager-minimal-case Name=required-files")
        return True

    print(f"FAIL robotmanager-minimal-case Name=required-files Missing={missing!r}")
    return False


def main():
    passed = True
    passed = assert_project_reference() and passed
    passed = assert_cli_project_reference() and passed
    passed = assert_required_files() and passed

    print("START robotmanager-minimal-case Name=contract-guard")
    passed = run([sys.executable, str(ROOT / "tools" / "robotmanager-contract-guard.py")]) and passed

    print("START robotmanager-minimal-case Name=build")
    passed = run([
        "dotnet",
        "build",
        str(PROJECT),
        "-c",
        "Debug",
        "-p:UseSharedCompilation=false",
    ]) and passed

    print("START robotmanager-minimal-case Name=cli-build")
    passed = run([
        "dotnet",
        "build",
        str(CLI_PROJECT),
        "-c",
        "Debug",
        "-p:UseSharedCompilation=false",
    ]) and passed

    wow_pid = None
    if "--pid" in sys.argv:
        index = sys.argv.index("--pid")
        if index + 1 < len(sys.argv):
            wow_pid = sys.argv[index + 1]
    if not wow_pid:
        wow_pid = find_wow_pid()
        if wow_pid:
            print(f'INFO robotmanager-minimal-matrix AutoPid={wow_pid}')

    if wow_pid:
        passed = run_cli_case("noop-lifecycle", wow_pid, "noop-lifecycle", [
            "Result=OK",
            'Reason="LifecycleVerified"',
            "ProductState=Stopped",
            "PulseCount=2",
            "PausedPulse=False:Paused",
            "AfterStopPulse=False:NoProduct",
        ]) and passed
        passed = run_cli_case("state-machine-hardening", wow_pid, "state-machine-hardening", [
            "Result=OK",
            'Reason="StateMachineHardened"',
            "DuplicateStart=False:ProductAlreadyRunning",
            "DuplicateStop=False:NoProduct",
            "FaultPulse=False:ProductError",
            "FaultedPulse=False:Faulted",
            "FaultedPause=False:Faulted",
            "FaultedResume=False:Faulted",
            "FaultedStop=True:Ready",
            "AfterFaultStopPulse=False:NoProduct",
        ]) and passed
        passed = run_cli_case("timed-pulse-loop", wow_pid, "timed-pulse-loop", [
            "Result=OK",
            'Reason="TimedPulseLoopVerified"',
            'Product="TimedPulseProduct"',
            "ProductState=Stopped",
            "LoopRunning=False",
            "StartLoop=True:Ready",
            "StopLoop=True:Ready",
            "StopProduct=True:Ready",
            "ProductPulseCount=",
        ]) and passed
        passed = run_cli_case("context-probe-product", wow_pid, "context-probe-product", [
            "Result=OK",
            'Reason="ContextProbeVerified"',
            'Product="ContextProbeProduct"',
            "ProductState=Stopped",
            "PulseCount=1",
            "ScriptExecuted=True",
            'ScriptReason="LuaExecuteSucceeded"',
            "SnapshotAttempted=True",
            "Start=True:Ready",
            "Pulse=True:Ready",
            "Stop=True:Ready",
        ]) and passed
    else:
        print('SKIP robotmanager-minimal-case Name=noop-lifecycle Reason="NoPid"')
        print('SKIP robotmanager-minimal-case Name=state-machine-hardening Reason="NoPid"')
        print('SKIP robotmanager-minimal-case Name=timed-pulse-loop Reason="NoPid"')
        print('SKIP robotmanager-minimal-case Name=context-probe-product Reason="NoPid"')

    if passed:
        print("OK robotmanager-minimal-matrix")
        return 0

    print("FAIL robotmanager-minimal-matrix")
    return 1


if __name__ == "__main__":
    sys.exit(main())
