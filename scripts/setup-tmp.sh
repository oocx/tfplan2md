#!/usr/bin/env bash
set -euo pipefail

# Ensure a workspace-local temp directory exists.
# Agents should prefer this over writing to /tmp or the user's home directory.

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
tmp_dir="$repo_root/.tmp"

mkdir -p "$tmp_dir"

# Keep the directory present in git (the file is ignored for runtime usage).
if [ ! -f "$tmp_dir/.gitkeep" ]; then
  : > "$tmp_dir/.gitkeep"
fi

echo "Workspace temp dir: $tmp_dir"
