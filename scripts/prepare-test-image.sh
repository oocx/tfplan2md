#!/usr/bin/env bash
set -euo pipefail

# Builds the tfplan2md-test:latest Docker image for integration tests.
# This script should be run before executing integration tests to ensure
# the image is available and cached, reducing test execution time.
#
# Usage:
#   scripts/prepare-test-image.sh [--force]
#
# Options:
#   --force    Force rebuild even if image already exists

IMAGE_NAME="tfplan2md-test"
IMAGE_TAG="latest"
FULL_IMAGE_NAME="${IMAGE_NAME}:${IMAGE_TAG}"
FORCE_BUILD=false

usage() {
  cat <<'USAGE'
Usage:
  scripts/prepare-test-image.sh [--force]

Builds the tfplan2md-test:latest Docker image for integration tests.

Options:
  --force    Force rebuild even if image already exists
  -h, --help Show this help message

Examples:
  scripts/prepare-test-image.sh
  scripts/prepare-test-image.sh --force
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --force)
      FORCE_BUILD=true
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "ERROR: Unknown option: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

# Check if Docker is available
if ! command -v docker >/dev/null 2>&1; then
  echo "ERROR: docker command not found. Please install Docker." >&2
  exit 1
fi

# Check if Docker daemon is running
if ! docker version >/dev/null 2>&1; then
  echo "ERROR: Docker daemon is not running. Please start Docker." >&2
  exit 1
fi

# Find repository root (directory containing Dockerfile)
REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
if [ ! -f "$REPO_ROOT/Dockerfile" ]; then
  echo "ERROR: Dockerfile not found at $REPO_ROOT/Dockerfile" >&2
  exit 1
fi

# Check if image already exists
IMAGE_EXISTS=false
if docker image inspect "$FULL_IMAGE_NAME" >/dev/null 2>&1; then
  IMAGE_EXISTS=true
fi

# Decide whether to build
if [ "$IMAGE_EXISTS" = true ] && [ "$FORCE_BUILD" = false ]; then
  echo "✓ Docker image $FULL_IMAGE_NAME already exists (use --force to rebuild)"
  exit 0
fi

# Build the image
echo "Building Docker image $FULL_IMAGE_NAME from $REPO_ROOT..."
cd "$REPO_ROOT"

if docker build -t "$FULL_IMAGE_NAME" .; then
  echo "✓ Docker image $FULL_IMAGE_NAME built successfully"
  exit 0
else
  echo "ERROR: Failed to build Docker image" >&2
  exit 1
fi
