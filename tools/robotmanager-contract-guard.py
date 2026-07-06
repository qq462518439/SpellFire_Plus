import sys
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]

SCAN_DIRS = [
    ROOT / "src" / "SpellFire.WowRuntime" / "Bot",
    ROOT / "src" / "SpellFire.RobotManager",
]

FORBIDDEN_PATTERNS = [
    "SpellFire.MemoryRobot",
    "SpellFire.Hook",
    "SpellFire.RuntimeHost",
    "SpellFire.Runtime;",
    "SpellFire.Runtime.",
    "IRuntimeFacade",
    "RuntimeCompositionRoot",
    "RemoteThread",
    "LoadLibrary",
    "FreeLibrary",
    "HookRuntime",
    "HookCommand",
]

PRODUCT_CONTEXT = ROOT / "src" / "SpellFire.RobotManager" / "Core" / "ProductContext.cs"
ALLOWED_PRODUCT_CONTEXT_MEMBERS = {
    "ProductContext",
    "ProcessId",
    "World",
    "WorldSnapshots",
    "ObjectManager",
    "Movement",
    "Scripts",
    "Snapshot",
}
FORBIDDEN_PRODUCT_CONTEXT_PATTERNS = [
    "public IWowRuntime",
    "public INavigationService",
    "public IRuntimeFacade",
    "public IMemoryRobot",
    "public RuntimeHost",
    "public Hook",
]


def iter_files():
    for directory in SCAN_DIRS:
        if not directory.exists():
            continue
        for path in directory.rglob("*"):
            if any(part in ("bin", "obj") for part in path.parts):
                continue
            if path.suffix.lower() in (".cs", ".csproj"):
                yield path


def main():
    files = list(iter_files())
    if not files:
        print("FAIL robotmanager-contract-guard Reason=\"NoContractFilesFound\"")
        return 1

    violations = []
    for path in files:
        text = path.read_text(encoding="utf-8", errors="replace")
        for pattern in FORBIDDEN_PATTERNS:
            if pattern in text:
                relative = path.relative_to(ROOT)
                violations.append((str(relative), pattern))

    if PRODUCT_CONTEXT.exists():
        text = PRODUCT_CONTEXT.read_text(encoding="utf-8", errors="replace")
        for pattern in FORBIDDEN_PRODUCT_CONTEXT_PATTERNS:
            if pattern in text:
                violations.append((str(PRODUCT_CONTEXT.relative_to(ROOT)), pattern))

        member_pattern = re.compile(r"\bpublic\s+(?:[\w<>\[\].]+\s+)+(?P<name>\w+)\s*(?:\(|\{)")
        for match in member_pattern.finditer(text):
            name = match.group("name")
            if name not in ALLOWED_PRODUCT_CONTEXT_MEMBERS:
                violations.append((str(PRODUCT_CONTEXT.relative_to(ROOT)), "ProductContextPublicMember:" + name))
    else:
        violations.append((str(PRODUCT_CONTEXT.relative_to(ROOT)), "MissingProductContext"))

    if violations:
        print(f"FAIL robotmanager-contract-guard Violations={len(violations)}")
        for path, pattern in violations:
            print(f"VIOLATION Path=\"{path}\" Pattern=\"{pattern}\"")
        return 1

    scanned = ";".join(str(path.relative_to(ROOT)) for path in files)
    print(f"OK robotmanager-contract-guard Files={len(files)} Scanned=\"{scanned}\"")
    return 0


if __name__ == "__main__":
    sys.exit(main())
