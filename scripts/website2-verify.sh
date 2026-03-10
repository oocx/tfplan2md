#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage: scripts/website2-verify.sh [--all]

Runs verification for Website2:
  1) scripts/website2-lint.sh
  2) Static local link and asset checks against website2/dist
EOF
}

if [[ ${1:-} == "-h" || ${1:-} == "--help" ]]; then
  usage
  exit 0
fi

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

if [[ ! -x scripts/website2-lint.sh ]]; then
  echo "Error: scripts/website2-lint.sh not found or not executable." >&2
  exit 1
fi

scripts/website2-lint.sh "$@"

if ! command -v python3 >/dev/null 2>&1; then
  echo "Warning: python3 not found; skipping Website2 link verification." >&2
  echo "Website2 verify OK (lint-only)."
  exit 0
fi

python3 - <<'PY'
import os
import sys
from html.parser import HTMLParser

dist_root = os.path.abspath("website2/dist")
ignore_prefixes = (
    "http://",
    "https://",
    "mailto:",
    "tel:",
    "data:",
    "javascript:",
    "//",
)

missing = []


class AssetReferenceParser(HTMLParser):
    def __init__(self):
        super().__init__()
        self.references = []

    def handle_starttag(self, tag, attrs):
        self._collect(attrs)

    def handle_startendtag(self, tag, attrs):
        self._collect(attrs)

    def _collect(self, attrs):
        for name, value in attrs:
            if value is None:
                continue
            if name.lower() in {"href", "src", "data"}:
                self.references.append(value)

html_files = []
for root, _, files in os.walk(dist_root):
    for name in files:
        if name.endswith(".html"):
            html_files.append(os.path.join(root, name))

def normalize_ref(value: str) -> str:
    value = value.split("#", 1)[0]
    value = value.split("?", 1)[0]
    return value.strip()

for html_file in html_files:
    with open(html_file, "r", encoding="utf-8") as handle:
        content = handle.read()

    parser = AssetReferenceParser()
    parser.feed(content)

    for raw in parser.references:
        ref = normalize_ref(raw)
        if not ref or ref.startswith("#") or ref.startswith(ignore_prefixes):
            continue

        if ref.startswith("/"):
            candidate = os.path.join(dist_root, ref.lstrip("/"))
        else:
            candidate = os.path.normpath(os.path.join(os.path.dirname(html_file), ref))

        candidate = os.path.abspath(candidate)
        if not candidate.startswith(dist_root + os.sep) and candidate != dist_root:
            continue

        if raw.rstrip().endswith("/"):
            exists = os.path.isdir(candidate) and os.path.isfile(os.path.join(candidate, "index.html"))
        else:
            exists = os.path.exists(candidate)

        if not exists:
            missing.append((os.path.relpath(html_file, os.path.abspath(".")), ref, os.path.relpath(candidate, os.path.abspath("."))))

if missing:
    print("Broken Website2 local links/assets detected:")
    for html_file, ref, candidate in missing:
        print(f"- {html_file}: {ref} -> {candidate}")
    sys.exit(1)

print(f"Website2 static link/asset check OK ({len(html_files)} HTML file(s)).")
PY

echo "Website2 verify OK."