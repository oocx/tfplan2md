#!/usr/bin/env python3
"""Validate canonical role definitions under .agents/roles/.

Checks that each role file is well-formed, declares a known tier, stays within
the line budget, has the required sections, and does not link to files that do
not exist. Also verifies the generated .claude/ adapter is in sync.

Usage:
    scripts/validate-agents.py
"""

from __future__ import annotations

import json
import re
import subprocess
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
ROLES = REPO / ".agents" / "roles"
TIERS = REPO / ".agents" / "tiers.json"

# A role that outgrows this is doing more than one job, or is repeating something
# that belongs in AGENTS.md or the agent-runtime skill.
LINE_BUDGET = 160
DESCRIPTION_BUDGET = 100

REQUIRED_SECTIONS = ["## Goal", "## Boundaries", "## Definition of Done"]

# Markdown links to repo files, e.g. [text](../../docs/spec.md) — anchors and
# URLs are ignored.
LINK = re.compile(r"\[[^\]]+\]\((?!https?:|#)([^)#]+)(?:#[^)]*)?\)")

errors: list[str] = []
warnings: list[str] = []


def error(path: Path, msg: str) -> None:
    errors.append(f"{path.relative_to(REPO)}: {msg}")


def warn(path: Path, msg: str) -> None:
    warnings.append(f"{path.relative_to(REPO)}: {msg}")


def parse_frontmatter(text: str, path: Path) -> dict[str, str] | None:
    if not text.startswith("---\n"):
        error(path, "missing YAML frontmatter")
        return None
    end = text.find("\n---\n", 3)
    if end == -1:
        error(path, "unterminated YAML frontmatter")
        return None
    meta: dict[str, str] = {}
    for line in text[4:end].splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        if ":" not in line:
            error(path, f"frontmatter line is not `key: value`: {line!r}")
            continue
        key, value = line.split(":", 1)
        meta[key.strip()] = value.strip().strip("\"'")
    return meta


def validate_role(path: Path, valid_tiers: set[str], names: dict[str, Path]) -> None:
    text = path.read_text(encoding="utf-8")
    meta = parse_frontmatter(text, path)
    if meta is None:
        return

    for key in ("name", "description", "tier"):
        if key not in meta:
            error(path, f"frontmatter is missing `{key}`")

    if "model" in meta:
        error(
            path,
            "declares `model` — roles declare `tier`; the mapping lives in .agents/tiers.json",
        )
    for obsolete in ("target", "tools", "handoffs"):
        if obsolete in meta:
            error(path, f"declares `{obsolete}`, which no longer exists in this workflow")

    tier = meta.get("tier")
    if tier and tier not in valid_tiers:
        error(path, f"unknown tier {tier!r} (expected one of {', '.join(sorted(valid_tiers))})")

    desc = meta.get("description", "")
    if len(desc) > DESCRIPTION_BUDGET:
        error(path, f"description is {len(desc)} chars, budget is {DESCRIPTION_BUDGET}")

    name = meta.get("name")
    if name:
        if name in names:
            error(path, f"duplicate role name {name!r} (also in {names[name].name})")
        names[name] = path

    lines = text.count("\n") + 1
    if lines > LINE_BUDGET:
        error(path, f"{lines} lines, budget is {LINE_BUDGET}")
    elif lines > LINE_BUDGET * 0.9:
        warn(path, f"{lines} lines, approaching the {LINE_BUDGET}-line budget")

    for section in REQUIRED_SECTIONS:
        if section not in text:
            error(path, f"missing required section `{section}`")

    # Every role must point at the shared conventions, or it will silently drift
    # back into repeating them.
    if "AGENTS.md" not in text:
        error(path, "does not reference AGENTS.md")

    for target in LINK.findall(text):
        resolved = (path.parent / target).resolve()
        if not resolved.exists():
            error(path, f"broken link: {target}")


def check_adapter_in_sync() -> None:
    result = subprocess.run(
        [str(REPO / "scripts" / "sync-agent-config.sh"), "--check"],
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        errors.append(
            ".claude/ is out of sync with .agents/ — run scripts/sync-agent-config.sh\n"
            + "\n".join(f"    {line}" for line in result.stdout.strip().splitlines())
        )


def main() -> int:
    if not ROLES.is_dir():
        print(f"error: {ROLES} does not exist", file=sys.stderr)
        return 1

    valid_tiers = set(json.loads(TIERS.read_text(encoding="utf-8"))["tiers"])
    role_files = sorted(ROLES.glob("*.md"))
    if not role_files:
        print(f"error: no role files in {ROLES}", file=sys.stderr)
        return 1

    names: dict[str, Path] = {}
    for role in role_files:
        validate_role(role, valid_tiers, names)

    check_adapter_in_sync()

    for w in warnings:
        print(f"warn:  {w}")
    for e in errors:
        print(f"ERROR: {e}")

    total = sum(f.read_text(encoding="utf-8").count("\n") + 1 for f in role_files)
    print(
        f"\n{len(role_files)} role(s), {total} lines total, "
        f"{len(errors)} error(s), {len(warnings)} warning(s)"
    )
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
