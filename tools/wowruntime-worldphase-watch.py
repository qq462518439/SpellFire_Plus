import argparse
import subprocess
import sys
import time
from collections import OrderedDict
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CLI = ROOT / "src" / "SpellFire.WowRuntime.Cli" / "bin" / "Debug" / "net48" / "SpellFire.WowRuntime.Cli.exe"


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


def run_phase(pid):
    completed = subprocess.run(
        [str(CLI), "--command", "world-phase", "--pid", str(pid)],
        cwd=str(ROOT),
        text=True,
        encoding="utf-8",
        errors="replace",
        capture_output=True,
    )
    output = (completed.stdout + completed.stderr).strip()
    return completed.returncode, output


def extract_phase(output):
    marker = "Phase="
    index = output.find(marker)
    if index < 0:
        return ""

    start = index + len(marker)
    end = output.find(" ", start)
    if end < 0:
        end = len(output)
    return output[start:end].strip().strip('"')


def send_enter(pid):
    script = rf"""
Add-Type -AssemblyName System.Windows.Forms
$process = Get-Process -Id {pid} -ErrorAction SilentlyContinue
if ($null -eq $process) {{
    Write-Output 'Result=Fail Reason="ProcessUnavailable"'
    exit 1
}}
$hwnd = $process.MainWindowHandle
if ($hwnd -eq [IntPtr]::Zero) {{
    Write-Output 'Result=Fail Reason="MainWindowHandleUnavailable"'
    exit 1
}}
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class Win32WindowFocus {{
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
}}
'@
[Win32WindowFocus]::SetForegroundWindow($hwnd) | Out-Null
Start-Sleep -Milliseconds 120
[System.Windows.Forms.SendKeys]::SendWait('{{ENTER}}')
Write-Output 'Result=OK Reason="EnterSent"'
"""
    completed = subprocess.run(
        ["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", script],
        text=True,
        encoding="utf-8",
        errors="replace",
        capture_output=True,
    )
    output = (completed.stdout + completed.stderr).strip()
    return completed.returncode, output


def main():
    parser = argparse.ArgumentParser(description="Poll WowRuntime world-phase without Hook/Lua/ObjectManager side effects.")
    parser.add_argument("--pid", type=int, default=0)
    parser.add_argument("--expect-phase", default="")
    parser.add_argument("--samples", type=int, default=10)
    parser.add_argument("--interval", type=float, default=1.0)
    parser.add_argument("--send-enter-when-not-inworld", action="store_true")
    parser.add_argument("--enter-interval", type=float, default=2.0)
    parser.add_argument("--max-enters", type=int, default=2)
    args = parser.parse_args()

    if not CLI.exists():
        print(f"FAIL wowruntime-worldphase-watch Reason=\"CliMissing\" Path=\"{CLI}\"")
        return 2

    pid = args.pid or find_wow_pid()
    if pid <= 0:
        print('FAIL wowruntime-worldphase-watch Reason="NoWowProcess"')
        return 2

    expected = args.expect_phase.strip()
    matched = False
    last_output = ""
    sample_count = max(1, args.samples)
    interval = max(0.0, args.interval)
    enter_interval = max(0.0, args.enter_interval)
    max_enters = max(0, args.max_enters)
    enter_count = 0
    last_enter_at = 0.0
    phase_counts = OrderedDict()
    first_phase = ""
    last_phase = ""

    print(f"START wowruntime-worldphase-watch Pid={pid} Samples={sample_count} Interval={interval:g} ExpectPhase=\"{expected}\" SendEnterWhenNotInWorld={args.send_enter_when_not_inworld} EnterInterval={enter_interval:g} MaxEnters={max_enters}")
    for index in range(sample_count):
        exit_code, output = run_phase(pid)
        last_output = output
        phase = extract_phase(output)
        if not phase:
            phase = "Unavailable"
        if not first_phase:
            first_phase = phase
        last_phase = phase
        phase_counts[phase] = phase_counts.get(phase, 0) + 1
        print(f"Sample={index + 1} Exit={exit_code} ObservedPhase=\"{phase}\" {output}")

        if expected and f"Phase={expected}" in output:
            matched = True
            break

        if args.send_enter_when_not_inworld and phase and phase != "InWorld" and enter_count < max_enters:
            now = time.monotonic()
            if last_enter_at <= 0 or now - last_enter_at >= enter_interval:
                enter_exit, enter_output = send_enter(pid)
                enter_count += 1
                last_enter_at = now
                print(f"Action=SendEnter Index={enter_count} Exit={enter_exit} Phase=\"{phase}\" {enter_output}")

        if index + 1 < sample_count and interval > 0:
            time.sleep(interval)

    if expected and not matched:
        print(f"FAIL wowruntime-worldphase-watch Reason=\"ExpectedPhaseNotObserved\" ExpectedPhase=\"{expected}\" Last=\"{last_output}\"")
        return 1

    summary = ",".join(f"{phase}:{count}" for phase, count in phase_counts.items())
    print(
        f"SUMMARY wowruntime-worldphase-watch Pid={pid} Samples={sample_count} "
        f"FirstPhase=\"{first_phase}\" LastPhase=\"{last_phase}\" PhaseCounts=\"{summary}\" "
        f"Matched={matched} EnterCount={enter_count}"
    )
    print(f"OK wowruntime-worldphase-watch Pid={pid} Matched={matched} EnterCount={enter_count} LastPhase=\"{last_phase}\"")
    return 0


if __name__ == "__main__":
    sys.exit(main())
