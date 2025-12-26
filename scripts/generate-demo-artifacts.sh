#!/usr/bin/env bash
# Generate Demo Artifacts Script
#
# Purpose: Regenerate all demo markdown artifacts from the current codebase.
# This ensures UAT tests validate the actual behavior of the tool, not stale output.
#
# Usage: scripts/generate-demo-artifacts.sh

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

log_info "Building project (Release configuration)..."
dotnet build src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -c Release

# ============================================================================
# Part 1: Generate /artifacts/*.md (used for UAT)
# ============================================================================

log_info "Generating artifacts/comprehensive-demo.md (inline-diff, for Azure DevOps UAT)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --principal-mapping examples/comprehensive-demo/demo-principals.json \
  --output artifacts/comprehensive-demo.md \
  examples/comprehensive-demo/plan.json

if [[ ! -s artifacts/comprehensive-demo.md ]]; then
  log_error "Generated artifact is empty or missing: artifacts/comprehensive-demo.md"
  exit 1
fi

if ! head -1 artifacts/comprehensive-demo.md | grep -q '^#'; then
  log_error "Generated artifact does not appear to be valid markdown."
  exit 1
fi

log_info "✓ artifacts/comprehensive-demo.md generated successfully (inline-diff)"

log_info "Generating artifacts/comprehensive-demo-standard-diff.md (for GitHub UAT)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --principal-mapping examples/comprehensive-demo/demo-principals.json \
  --large-value-format standard-diff \
  --output artifacts/comprehensive-demo-standard-diff.md \
  examples/comprehensive-demo/plan.json

if [[ ! -s artifacts/comprehensive-demo-standard-diff.md ]]; then
  log_error "Generated artifact is empty or missing: artifacts/comprehensive-demo-standard-diff.md"
  exit 1
fi

log_info "✓ artifacts/comprehensive-demo-standard-diff.md generated successfully"

log_info "Generating artifacts/role.md (role assignments with principal mapping)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --principal-mapping examples/comprehensive-demo/demo-principals.json \
  --output artifacts/role.md \
  tests/Oocx.TfPlan2Md.Tests/TestData/role-assignments.json

if [[ -s artifacts/role.md ]]; then
  log_info "✓ artifacts/role.md generated successfully"
else
  log_error "Failed to generate artifacts/role.md"
  exit 1
fi

log_info "Generating artifacts/role-default.md (role assignments without principal mapping)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --output artifacts/role-default.md \
  tests/Oocx.TfPlan2Md.Tests/TestData/role-assignments.json

if [[ -s artifacts/role-default.md ]]; then
  log_info "✓ artifacts/role-default.md generated successfully"
else
  log_error "Failed to generate artifacts/role-default.md"
  exit 1
fi

# Note: uat-minimal.md is a static handcrafted file, not generated

# ============================================================================
# Part 2: Generate examples/comprehensive-demo/*.md (documentation samples)
# ============================================================================

log_info "Generating examples/comprehensive-demo/report.md (default template)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --principal-mapping examples/comprehensive-demo/demo-principals.json \
  --output examples/comprehensive-demo/report.md \
  examples/comprehensive-demo/plan.json

if [[ -s examples/comprehensive-demo/report.md ]]; then
  log_info "✓ examples/comprehensive-demo/report.md generated successfully"
else
  log_error "Failed to generate examples/comprehensive-demo/report.md"
  exit 1
fi

log_info "Generating examples/comprehensive-demo/report-with-sensitive.md (with --show-sensitive)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --principal-mapping examples/comprehensive-demo/demo-principals.json \
  --show-sensitive \
  --output examples/comprehensive-demo/report-with-sensitive.md \
  examples/comprehensive-demo/plan.json

if [[ -s examples/comprehensive-demo/report-with-sensitive.md ]]; then
  log_info "✓ examples/comprehensive-demo/report-with-sensitive.md generated successfully"
else
  log_error "Failed to generate examples/comprehensive-demo/report-with-sensitive.md"
  exit 1
fi

log_info "Generating examples/comprehensive-demo/report-summary.md (summary template)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --template summary \
  --output examples/comprehensive-demo/report-summary.md \
  examples/comprehensive-demo/plan.json

if [[ -s examples/comprehensive-demo/report-summary.md ]]; then
  log_info "✓ examples/comprehensive-demo/report-summary.md generated successfully"
else
  log_error "Failed to generate examples/comprehensive-demo/report-summary.md"
  exit 1
fi

log_info "All demo artifacts generated successfully"
