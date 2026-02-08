#!/bin/bash
# Wrapper for markdownlint-cli2 Docker container
# Uses project's .markdownlint.json configuration

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

DOCKER_IMAGE="davidanson/markdownlint-cli2:v0.20.0"
CONFIG_FILE=".markdownlint.json"

# Show usage if no arguments provided
if [ $# -eq 0 ]; then
  echo "Usage: $0 <file-or-directory> [file-or-directory...]"
  echo ""
  echo "Examples:"
  echo "  $0 README.md                    # Lint single file"
  echo "  $0 artifacts/comprehensive-demo.md  # Lint demo artifact"
  echo "  $0 docs/                        # Lint directory"
  echo "  $0 --stdin < file.md            # Lint from stdin"
  echo ""
  echo "Uses .markdownlint.json configuration from repository root"
  exit 1
fi

# Handle stdin mode
if [ "$1" = "--stdin" ]; then
  cd "$REPO_ROOT"
  docker run --rm -i -v "$REPO_ROOT:/workdir" -w /workdir "$DOCKER_IMAGE" --config "$CONFIG_FILE" --stdin
  exit $?
fi

# Build file arguments - convert to absolute paths relative to repo root
FILE_ARGS=()
for arg in "$@"; do
  # If path is relative, make it relative to current directory
  if [[ "$arg" != /* ]]; then
    arg="$(realpath --relative-to="$REPO_ROOT" "$PWD/$arg" 2>/dev/null || echo "$arg")"
  else
    arg="$(realpath --relative-to="$REPO_ROOT" "$arg" 2>/dev/null || echo "$arg")"
  fi
  FILE_ARGS+=("$arg")
done

# Run markdownlint with file arguments
cd "$REPO_ROOT"
docker run --rm -v "$REPO_ROOT:/workdir" -w /workdir "$DOCKER_IMAGE" --config "$CONFIG_FILE" "${FILE_ARGS[@]}"
