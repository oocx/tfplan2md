#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage: scripts/website-verify.sh [--all] [--base <ref>]

Runs a lightweight verification suite for website changes:
  1) Lint website HTML/CSS/JS via scripts/website-lint.sh
  2) Basic static checks for broken local links and asset references in HTML

Options:
  --all           Verify all website files (can be slower)
  --base <ref>    Git ref to diff against (default: origin/main if present)
EOF
}

lint_all=false
base_ref=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --all)
      lint_all=true
      shift
      ;;
    --base)
      base_ref="${2:-}"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage
      exit 2
      ;;
  esac
done

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

if [[ ! -x scripts/website-lint.sh ]]; then
  echo "Error: scripts/website-lint.sh not found or not executable." >&2
  exit 1
fi

# 1) Lint first (hard gate)
args=()
if [[ "$lint_all" == "true" ]]; then
  args+=("--all")
fi
if [[ -n "$base_ref" ]]; then
  args+=("--base" "$base_ref")
fi

scripts/website-lint.sh "${args[@]}"

# 2) Static link/asset existence checks (HTML only)
if ! command -v python3 >/dev/null 2>&1; then
  echo "Warning: python3 not found; skipping link/asset verification." >&2
  echo "Website verify OK (lint-only)."
  exit 0
fi

if [[ -z "$base_ref" ]]; then
  if git show-ref --verify --quiet refs/remotes/origin/main; then
    base_ref="origin/main"
  else
    base_ref="HEAD~1"
  fi
fi

html_files=()
if [[ "$lint_all" == "true" ]]; then
  while IFS= read -r file; do html_files+=("$file"); done < <(
    find website \
      -type f -name '*.html' \
      ! -path 'website/prototypes/*' \
      ! -path 'website/assets/icons/*.html' \
      -print
  )
else
  while IFS= read -r file; do html_files+=("$file"); done < <(
    (
      git diff --name-only --diff-filter=ACMR "${base_ref}...HEAD" -- website || true
      git diff --name-only --diff-filter=ACMR --cached -- website || true
      git diff --name-only --diff-filter=ACMR -- website || true
    ) \
      | grep -E '\.html$' \
      | grep -vE '^website/prototypes/' \
      | grep -vE '^website/assets/icons/.*\.html$' \
      | sort -u || true
  )
fi

if [[ ${#html_files[@]} -eq 0 ]]; then
  echo "No website HTML files to verify."
  echo "Website verify OK."
  exit 0
fi

python3 - <<'PY' "${html_files[@]}"
import os
import re
import sys

html_files = sys.argv[1:]
website_root = os.path.abspath("website")

# Basic attribute extraction. This is intentionally lightweight (not a full HTML parser).
ATTR_RE = re.compile(r"\b(?:href|src)=([\"'])(.+?)\1", re.IGNORECASE)

IGNORE_PREFIXES = (
    "http://",
    "https://",
    "mailto:",
    "tel:",
    "data:",
    "javascript:",
    "//",
)

missing = []

def normalize_ref(value: str) -> str:
    # Strip fragments and query strings.
    value = value.split("#", 1)[0]
    value = value.split("?", 1)[0]
    return value.strip()


def resolve_path(html_path: str, ref: str) -> str | None:
    ref = normalize_ref(ref)
    if not ref or ref == "/" or ref.startswith("#"):
        return None
    if ref.startswith(IGNORE_PREFIXES):
        return None

    # Root-relative paths map to website root.
    if ref.startswith("/"):
        candidate = os.path.join(website_root, ref.lstrip("/"))
    else:
        candidate = os.path.normpath(os.path.join(os.path.dirname(html_path), ref))

    candidate_abs = os.path.abspath(candidate)

    # Require refs to stay within website/.
    if not candidate_abs.startswith(website_root + os.sep) and candidate_abs != website_root:
        return None

    return candidate_abs


def exists_for_ref(candidate_abs: str, original_ref: str) -> bool:
    # If ref ends with '/', treat it as a directory link.
    if original_ref.rstrip().endswith("/"):
        return os.path.isdir(candidate_abs) and os.path.isfile(os.path.join(candidate_abs, "index.html"))

    return os.path.exists(candidate_abs)


for html_file in html_files:
    html_abs = os.path.abspath(html_file)
    try:
        with open(html_abs, "r", encoding="utf-8") as f:
            content = f.read()
    except OSError as e:
        missing.append((html_file, f"(cannot read file: {e})"))
        continue

    for _, raw in ATTR_RE.findall(content):
        ref = raw.strip()
        candidate_abs = resolve_path(html_abs, ref)
        if candidate_abs is None:
            continue

        if not exists_for_ref(candidate_abs, ref):
            # Show paths relative to repo root for readability.
            rel_candidate = os.path.relpath(candidate_abs, os.path.abspath("."))
            missing.append((html_file, f"{ref} -> {rel_candidate}"))

if missing:
    print("Broken local links/assets detected:")
    for html_file, detail in missing:
        print(f"- {html_file}: {detail}")
    sys.exit(1)

print(f"Static link/asset check OK ({len(html_files)} HTML file(s)).")
PY

echo "Website verify OK."