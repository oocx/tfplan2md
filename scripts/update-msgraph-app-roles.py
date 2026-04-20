#!/usr/bin/env python3
"""
Microsoft Graph Application Permissions (App Roles) Mapping Generator

Regenerates ``src/Oocx.TfPlan2Md/Platforms/Azure/MicrosoftGraphAppRoles.json``
from the canonical Microsoft Graph permissions reference.

The Microsoft Learn page
<https://learn.microsoft.com/en-us/graph/permissions-reference> is rendered
from the markdown source in the public GitHub repository
``microsoftgraph/microsoft-graph-docs-contrib`` — that markdown source is the
authoritative, machine-parseable upstream of the rendered Learn page. This
script fetches that markdown source by default because it is significantly
more robust to parse than the rendered HTML and contains the exact same data.
A custom URL or local file can be supplied via ``--source`` for offline /
audit runs.

Only **application** permission GUIDs are extracted (``app_role_id`` values
used by ``azuread_app_role_assignment``). Delegated permission GUIDs
(``oauth2PermissionScopes``) are intentionally NOT included — see
``docs/issues/120-msgraph-permissions-mapping-coverage/analysis.md``.

The output JSON shape mirrors the existing file: a flat dictionary mapping
the permission GUID (lower-case) to the short permission ``value`` such as
``Policy.ReadWrite.Authorization``. Entries are sorted by GUID for
deterministic, diff-friendly output, matching the existing convention.

Usage::

    scripts/update-msgraph-app-roles.py
    scripts/update-msgraph-app-roles.py --source /path/to/permissions-reference.md
    scripts/update-msgraph-app-roles.py --output some/other/path.json --dry-run

Related issue: docs/issues/120-msgraph-permissions-mapping-coverage/analysis.md
Related feature: docs/features/116-azuread-app-role-assignment/specification.md
"""

from __future__ import annotations

import argparse
import json
import re
import sys
import urllib.error
import urllib.request
from pathlib import Path
from typing import Dict, Iterable, Tuple

# Upstream source: the markdown file that is rendered as
# https://learn.microsoft.com/en-us/graph/permissions-reference
DEFAULT_SOURCE_URL = (
    "https://raw.githubusercontent.com/microsoftgraph/"
    "microsoft-graph-docs-contrib/main/concepts/permissions-reference.md"
)
LEARN_PAGE_URL = "https://learn.microsoft.com/en-us/graph/permissions-reference"
DEFAULT_OUTPUT = (
    "src/Oocx.TfPlan2Md/Platforms/Azure/MicrosoftGraphAppRoles.json"
)
USER_AGENT = "tfplan2md-msgraph-app-role-generator/1.0"

# Match a permission section header. Examples:
#   ### Policy.ReadWrite.Authorization
#   ### User.Read.All
# Names are dot-separated identifiers; we accept the chars Microsoft uses.
_HEADER_RE = re.compile(r"^###\s+([A-Za-z0-9._-]+)\s*$")

# Match the Identifier table row. Both columns may contain a GUID or a "-".
# Example:
#   | Identifier | fb221be6-99f2-473f-bd32-01c6a0e9ca3b | edd3c878-... |
_GUID = r"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}"
_IDENTIFIER_RE = re.compile(
    r"^\|\s*Identifier\s*\|\s*(" + _GUID + r"|-)\s*\|\s*(" + _GUID + r"|-)\s*\|",
)


def fetch_source(source: str) -> str:
    """Fetch the permissions reference markdown.

    ``source`` is either an HTTP/HTTPS URL or a local file path. Local paths
    are useful for offline reruns and for unit-testing the parser.
    """
    if source.startswith(("http://", "https://")):
        req = urllib.request.Request(source, headers={"User-Agent": USER_AGENT})
        try:
            with urllib.request.urlopen(req, timeout=60) as resp:  # noqa: S310 (trusted host)
                return resp.read().decode("utf-8")
        except urllib.error.URLError as exc:
            raise SystemExit(f"error: failed to fetch {source}: {exc}") from exc
    path = Path(source)
    if not path.is_file():
        raise SystemExit(f"error: source file not found: {source}")
    return path.read_text(encoding="utf-8")


def parse_app_permissions(markdown: str) -> Dict[str, str]:
    """Extract ``{guid: permission_value}`` for every application permission.

    Walks the markdown line-by-line. Each permission section starts with an
    ``### Permission.Name`` header. The next ``| Identifier | ... | ... |``
    table row holds the application GUID (column 2) and the delegated GUID
    (column 3). A literal ``-`` means "not available for that grant type".

    Only entries with a real application GUID are returned. The most recent
    header wins, matching the document structure.
    """
    mappings: Dict[str, str] = {}
    current_name: str | None = None
    # Track which headers we've already consumed an identifier row for, so
    # repeated identifier rows under the same header (shouldn't happen, but
    # defensive) don't overwrite with stale data.
    consumed_for_header = False

    for raw_line in markdown.splitlines():
        line = raw_line.rstrip()
        header_match = _HEADER_RE.match(line)
        if header_match:
            current_name = header_match.group(1)
            consumed_for_header = False
            continue

        if current_name is None or consumed_for_header:
            continue

        ident_match = _IDENTIFIER_RE.match(line)
        if not ident_match:
            continue

        app_guid, _delegated_guid = ident_match.group(1), ident_match.group(2)
        consumed_for_header = True
        if app_guid == "-":
            # Delegated-only permission; skip — we only map app roles.
            continue

        guid_lower = app_guid.lower()
        # If two headers point at the same GUID (legacy aliases), keep the
        # first one encountered — the document orders newest names later.
        mappings.setdefault(guid_lower, current_name)

    return mappings


def merge_with_existing(
    new_mappings: Dict[str, str], existing_path: Path
) -> Tuple[Dict[str, str], Dict[str, str], Dict[str, str]]:
    """Compute (final, added, removed) relative to the existing JSON file.

    The final dict is just ``new_mappings`` — we treat the upstream as the
    source of truth — but we surface added/removed for the run summary.
    """
    if existing_path.is_file():
        existing = json.loads(existing_path.read_text(encoding="utf-8"))
        if not isinstance(existing, dict):
            raise SystemExit(
                f"error: {existing_path} is not a flat JSON object"
            )
        existing = {str(k).lower(): str(v) for k, v in existing.items()}
    else:
        existing = {}

    added = {k: v for k, v in new_mappings.items() if k not in existing}
    removed = {k: v for k, v in existing.items() if k not in new_mappings}
    return new_mappings, added, removed


def render_json(mappings: Dict[str, str]) -> str:
    """Render mappings sorted by GUID with two-space indentation.

    Matches the existing file's formatting so diffs stay minimal.
    """
    sorted_items: Iterable[Tuple[str, str]] = sorted(
        mappings.items(), key=lambda kv: kv[0]
    )
    body = ",\n".join(
        f'  {json.dumps(k)}: {json.dumps(v)}' for k, v in sorted_items
    )
    return "{\n" + body + "\n}\n"


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description=(
            "Regenerate MicrosoftGraphAppRoles.json from the Microsoft Graph "
            "permissions reference."
        ),
        epilog=(
            f"Default source: {DEFAULT_SOURCE_URL}\n"
            f"Rendered as:    {LEARN_PAGE_URL}"
        ),
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument(
        "--source",
        default=DEFAULT_SOURCE_URL,
        help=(
            "Source URL or local file path of the permissions-reference "
            "markdown. Defaults to the upstream raw markdown on GitHub."
        ),
    )
    parser.add_argument(
        "--output",
        default=DEFAULT_OUTPUT,
        help="Path to the JSON file to write.",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Print the diff summary without writing the output file.",
    )
    args = parser.parse_args(argv)

    print(f"fetching: {args.source}", file=sys.stderr)
    markdown = fetch_source(args.source)

    print("parsing application permissions...", file=sys.stderr)
    mappings = parse_app_permissions(markdown)
    if not mappings:
        raise SystemExit(
            "error: no application permissions parsed — source format may "
            "have changed; inspect the markdown structure and update the "
            "regular expressions in this script."
        )

    output_path = Path(args.output)
    final, added, removed = merge_with_existing(mappings, output_path)

    print(
        f"summary: total={len(final)} added={len(added)} removed={len(removed)}",
        file=sys.stderr,
    )
    if added:
        print("added entries (first 10):", file=sys.stderr)
        for guid, name in list(sorted(added.items()))[:10]:
            print(f"  + {guid}  {name}", file=sys.stderr)
    if removed:
        print("removed entries (first 10):", file=sys.stderr)
        for guid, name in list(sorted(removed.items()))[:10]:
            print(f"  - {guid}  {name}", file=sys.stderr)

    rendered = render_json(final)
    if args.dry_run:
        print("(dry run — not writing output)", file=sys.stderr)
        return 0

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(rendered, encoding="utf-8")
    print(f"wrote: {output_path}", file=sys.stderr)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
