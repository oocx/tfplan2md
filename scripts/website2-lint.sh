#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage: scripts/website2-lint.sh [args]

Deprecated compatibility wrapper. Use scripts/website-lint.sh instead.
EOF
}

if [[ ${1:-} == "-h" || ${1:-} == "--help" ]]; then
  usage
  exit 0
fi

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

echo "Warning: scripts/website2-lint.sh is deprecated; forwarding to scripts/website-lint.sh." >&2
exec scripts/website-lint.sh "$@"