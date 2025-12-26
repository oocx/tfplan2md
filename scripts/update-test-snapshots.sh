#!/usr/bin/env bash
# Update Test Snapshots Script
#
# Purpose: Regenerate all test snapshot files when intentional markdown changes are made.
# This script deletes existing snapshots and re-runs tests to capture the new expected output.
#
# Usage: scripts/update-test-snapshots.sh

set -euo pipefail

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

log_info() { echo -e "${GREEN}[INFO]${NC} $*"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $*"; }
log_error() { echo -e "${RED}[ERROR]${NC} $*" >&2; }

# Navigate to repo root
cd "$(git rev-parse --show-toplevel)"

SNAPSHOTS_DIR="tests/Oocx.TfPlan2Md.Tests/TestData/Snapshots"

if [[ ! -d "$SNAPSHOTS_DIR" ]]; then
  log_error "Snapshots directory not found: $SNAPSHOTS_DIR"
  exit 1
fi

log_info "Deleting existing snapshot files..."
rm -f "$SNAPSHOTS_DIR"/*.md
log_info "✓ Deleted $(find "$SNAPSHOTS_DIR" -maxdepth 1 -type f -name '*.md' | wc -l) snapshot files"

log_info "Running snapshot tests to regenerate files..."
log_info "(Tests will fail on first run, but will create new snapshots)"

# Run only the snapshot tests (MarkdownSnapshotTests class)
dotnet test tests/Oocx.TfPlan2Md.Tests/Oocx.TfPlan2Md.Tests.csproj \
  --filter "FullyQualifiedName~MarkdownSnapshotTests" \
  --verbosity minimal || true

# Count generated snapshots
SNAPSHOT_COUNT=$(find "$SNAPSHOTS_DIR" -maxdepth 1 -type f -name '*.md' | wc -l)

if [[ $SNAPSHOT_COUNT -eq 0 ]]; then
  log_error "No snapshot files were generated. Tests may have crashed."
  exit 1
fi

log_info "✓ Generated $SNAPSHOT_COUNT new snapshot files"

log_info "Running snapshot tests again to verify..."
if dotnet test tests/Oocx.TfPlan2Md.Tests/Oocx.TfPlan2Md.Tests.csproj \
  --filter "FullyQualifiedName~MarkdownSnapshotTests" \
  --verbosity minimal; then
  log_info "✅ All snapshot tests pass!"
  log_info ""
  log_info "Snapshots updated successfully. Review changes with:"
  log_info "  git diff $SNAPSHOTS_DIR"
else
  log_error "Snapshot tests still failing after regeneration."
  log_error "This may indicate a non-deterministic issue in the code."
  exit 1
fi
