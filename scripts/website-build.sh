#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage: scripts/website-build.sh [--all] [--base <ref>]

Builds the Eleventy website into `website/dist`.

Options:
  --all           Accepted for compatibility; the website is always fully built
  --base <ref>    Accepted for compatibility; currently ignored
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --all)
      shift
      ;;
    --base)
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

if ! command -v node >/dev/null 2>&1; then
  echo "Error: node is required for website builds." >&2
  exit 1
fi

if ! command -v npm >/dev/null 2>&1; then
  echo "Error: npm is required for website builds." >&2
  exit 1
fi

if [[ ! -d website/node_modules ]]; then
  echo "Installing website dependencies..."
  (cd website && npm ci)
fi

echo "Building website..."
(cd website && npm run clean && npm run build)

echo "Website build OK."