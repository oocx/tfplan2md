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
  --code-analysis-results "examples/static-analysis/*.sarif" \
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

log_info "Generating artifacts/comprehensive-demo-simple-diff.md (for GitHub UAT)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --principal-mapping examples/comprehensive-demo/demo-principals.json \
  --code-analysis-results "examples/static-analysis/*.sarif" \
  --render-target github \
  --output artifacts/comprehensive-demo-simple-diff.md \
  examples/comprehensive-demo/plan.json

if [[ ! -s artifacts/comprehensive-demo-simple-diff.md ]]; then
  log_error "Generated artifact is empty or missing: artifacts/comprehensive-demo-simple-diff.md"
  exit 1
fi

log_info "✓ artifacts/comprehensive-demo-simple-diff.md generated successfully"

log_info "Generating artifacts/role.md (role assignments with principal mapping)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --principal-mapping examples/comprehensive-demo/demo-principals.json \
  --output artifacts/role.md \
  src/tests/Oocx.TfPlan2Md.TUnit/TestData/role-assignments.json

if [[ -s artifacts/role.md ]]; then
  log_info "✓ artifacts/role.md generated successfully"
else
  log_error "Failed to generate artifacts/role.md"
  exit 1
fi

log_info "Generating artifacts/role-default.md (role assignments with demo principal mapping)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --principal-mapping examples/role-assignments-principals.json \
  --output artifacts/role-default.md \
  src/tests/Oocx.TfPlan2Md.TUnit/TestData/role-assignments.json

if [[ -s artifacts/role-default.md ]]; then
  log_info "✓ artifacts/role-default.md generated successfully"
else
  log_error "Failed to generate artifacts/role-default.md"
  exit 1
fi

log_info "Generating artifacts/apim-display-enhancements-demo.md (APIM display enhancements demo)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --output artifacts/apim-display-enhancements-demo.md \
  examples/apim-display-enhancements.json

if [[ -s artifacts/apim-display-enhancements-demo.md ]]; then
  log_info "✓ artifacts/apim-display-enhancements-demo.md generated successfully"
else
  log_error "Failed to generate artifacts/apim-display-enhancements-demo.md"
  exit 1
fi

log_info "Generating artifacts/refactoring-demo.md (inline-diff, for Azure DevOps UAT)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --output artifacts/refactoring-demo.md \
  examples/refactoring-demo.json

if [[ -s artifacts/refactoring-demo.md ]]; then
  log_info "✓ artifacts/refactoring-demo.md generated successfully"
else
  log_error "Failed to generate artifacts/refactoring-demo.md"
  exit 1
fi

log_info "Generating artifacts/refactoring-demo-simple-diff.md (for GitHub UAT)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --render-target github \
  --output artifacts/refactoring-demo-simple-diff.md \
  examples/refactoring-demo.json

if [[ -s artifacts/refactoring-demo-simple-diff.md ]]; then
  log_info "✓ artifacts/refactoring-demo-simple-diff.md generated successfully"
else
  log_error "Failed to generate artifacts/refactoring-demo-simple-diff.md"
  exit 1
fi

log_info "Generating artifacts/static-analysis-comprehensive-demo.md (comprehensive demo with code analysis)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --code-analysis-results "examples/static-analysis/*.sarif" \
  --output artifacts/static-analysis-comprehensive-demo.md \
  examples/comprehensive-demo/plan.json

if [[ -s artifacts/static-analysis-comprehensive-demo.md ]]; then
  log_info "✓ artifacts/static-analysis-comprehensive-demo.md generated successfully"
else
  log_error "Failed to generate artifacts/static-analysis-comprehensive-demo.md"
  exit 1
fi

# Note: uat-minimal.md is a static handcrafted file, not generated

log_info "Generating artifacts/azapi-create-demo.md (AzAPI create demo)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --output artifacts/azapi-create-demo.md \
  examples/azapi-create.json

if [[ -s artifacts/azapi-create-demo.md ]]; then
  log_info "✓ artifacts/azapi-create-demo.md generated successfully"
else
  log_error "Failed to generate artifacts/azapi-create-demo.md"
  exit 1
fi

log_info "Generating artifacts/azapi-update-demo.md (AzAPI update demo)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --output artifacts/azapi-update-demo.md \
  examples/azapi-update.json

if [[ -s artifacts/azapi-update-demo.md ]]; then
  log_info "✓ artifacts/azapi-update-demo.md generated successfully"
else
  log_error "Failed to generate artifacts/azapi-update-demo.md"
  exit 1
fi

log_info "Generating artifacts/azapi-complex-demo.md (AzAPI complex demo)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --output artifacts/azapi-complex-demo.md \
  examples/azapi-complex.json

if [[ -s artifacts/azapi-complex-demo.md ]]; then
  log_info "✓ artifacts/azapi-complex-demo.md generated successfully"
else
  log_error "Failed to generate artifacts/azapi-complex-demo.md"
  exit 1
fi

log_info "Generating artifacts/azuread-enhancements-demo.md (AzureAD enhancements demo)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --principal-mapping examples/principal-mapping-azuread.json \
  --output artifacts/azuread-enhancements-demo.md \
  examples/azuread-resources-demo.json

if [[ -s artifacts/azuread-enhancements-demo.md ]]; then
  log_info "✓ artifacts/azuread-enhancements-demo.md generated successfully"
else
  log_error "Failed to generate artifacts/azuread-enhancements-demo.md"
  exit 1
fi

log_info "Generating artifacts/azure-display-enhancements-demo.md (Azure display enhancements demo)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --output artifacts/azure-display-enhancements-demo.md \
  examples/azure-display-enhancements.json

if [[ -s artifacts/azure-display-enhancements-demo.md ]]; then
  log_info "✓ artifacts/azure-display-enhancements-demo.md generated successfully"
else
  log_error "Failed to generate artifacts/azure-display-enhancements-demo.md"
  exit 1
fi

log_info "Generating artifacts/azure-display-enhancements-demo-simple-diff.md (GitHub render target)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --render-target github \
  --output artifacts/azure-display-enhancements-demo-simple-diff.md \
  examples/azure-display-enhancements.json

if [[ -s artifacts/azure-display-enhancements-demo-simple-diff.md ]]; then
  log_info "✓ artifacts/azure-display-enhancements-demo-simple-diff.md generated successfully"
else
  log_error "Failed to generate artifacts/azure-display-enhancements-demo-simple-diff.md"
  exit 1
fi

# ============================================================================
# Part 2: Generate examples/comprehensive-demo/*.md (documentation samples)
# ============================================================================

log_info "Generating examples/comprehensive-demo/report.md (default template)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --principal-mapping examples/comprehensive-demo/demo-principals.json \
  --code-analysis-results "examples/static-analysis/*.sarif" \
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
  --code-analysis-results "examples/static-analysis/*.sarif" \
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

# ============================================================================
# Part 3: Generate additional artifacts (restored and examples)
# ============================================================================

log_info "Generating artifacts/azapi-nested-grouping-demo.md (AzAPI nested grouping demo)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --output artifacts/azapi-nested-grouping-demo.md \
  src/tests/Oocx.TfPlan2Md.TUnit/TestData/azapi-complex-nested-plan.json

if [[ -s artifacts/azapi-nested-grouping-demo.md ]]; then
  log_info "✓ artifacts/azapi-nested-grouping-demo.md generated successfully"
else
  log_error "Failed to generate artifacts/azapi-nested-grouping-demo.md"
  exit 1
fi

log_info "Generating artifacts/azapi-uat-combined.md (AzAPI UAT combined demo)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --output artifacts/azapi-uat-combined.md \
  src/tests/Oocx.TfPlan2Md.TUnit/TestData/azapi-complex-nested-plan.json

if [[ -s artifacts/azapi-uat-combined.md ]]; then
  log_info "✓ artifacts/azapi-uat-combined.md generated successfully"
else
  log_error "Failed to generate artifacts/azapi-uat-combined.md"
  exit 1
fi

log_info "Generating artifacts/comprehensive-demo-nested.md (comprehensive demo with nested principals)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --principal-mapping examples/comprehensive-demo/demo-principals-nested.json \
  --code-analysis-results "examples/static-analysis/*.sarif" \
  --output artifacts/comprehensive-demo-nested.md \
  examples/comprehensive-demo/plan.json

if [[ -s artifacts/comprehensive-demo-nested.md ]]; then
  log_info "✓ artifacts/comprehensive-demo-nested.md generated successfully"
else
  log_error "Failed to generate artifacts/comprehensive-demo-nested.md"
  exit 1
fi

log_info "Generating examples/code-analysis/report.md (code analysis example)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --code-analysis-results "examples/code-analysis/analysis.sarif" \
  --output examples/code-analysis/report.md \
  examples/code-analysis/plan.json

if [[ -s examples/code-analysis/report.md ]]; then
  log_info "✓ examples/code-analysis/report.md generated successfully"
else
  log_error "Failed to generate examples/code-analysis/report.md"
  exit 1
fi

log_info "Generating examples/firewall-with-static-analysis/report.md (firewall with static analysis example)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --code-analysis-results "examples/firewall-with-static-analysis/analysis.sarif" \
  --output examples/firewall-with-static-analysis/report.md \
  examples/firewall-with-static-analysis/plan.json

if [[ -s examples/firewall-with-static-analysis/report.md ]]; then
  log_info "✓ examples/firewall-with-static-analysis/report.md generated successfully"
else
  log_error "Failed to generate examples/firewall-with-static-analysis/report.md"
  exit 1
fi

# ============================================================================
# Part 4: Generate old UAT artifacts (from test data)
# ============================================================================

log_info "Generating artifacts/azure-rm-batch-2-feature-test.md (Azure RM batch 2 feature test)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --output artifacts/azure-rm-batch-2-feature-test.md \
  src/tests/Oocx.TfPlan2Md.TUnit/TestData/azure-rm-batch-2-feature-test-plan.json

if [[ -s artifacts/azure-rm-batch-2-feature-test.md ]]; then
  log_info "✓ artifacts/azure-rm-batch-2-feature-test.md generated successfully"
else
  log_error "Failed to generate artifacts/azure-rm-batch-2-feature-test.md"
  exit 1
fi

log_info "Generating artifacts/azure-rm-batch-2-feature-test-simple-diff.md (GitHub render target)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --render-target github \
  --output artifacts/azure-rm-batch-2-feature-test-simple-diff.md \
  src/tests/Oocx.TfPlan2Md.TUnit/TestData/azure-rm-batch-2-feature-test-plan.json

if [[ -s artifacts/azure-rm-batch-2-feature-test-simple-diff.md ]]; then
  log_info "✓ artifacts/azure-rm-batch-2-feature-test-simple-diff.md generated successfully"
else
  log_error "Failed to generate artifacts/azure-rm-batch-2-feature-test-simple-diff.md"
  exit 1
fi

log_info "Generating artifacts/parent-child-resource-grouping-uat.md (parent-child grouping UAT)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --output artifacts/parent-child-resource-grouping-uat.md \
  src/tests/Oocx.TfPlan2Md.TUnit/TestData/parent-child-resource-grouping-uat-plan.json

if [[ -s artifacts/parent-child-resource-grouping-uat.md ]]; then
  log_info "✓ artifacts/parent-child-resource-grouping-uat.md generated successfully"
else
  log_error "Failed to generate artifacts/parent-child-resource-grouping-uat.md"
  exit 1
fi

log_info "Generating artifacts/azure-rm-parent-child-demo.md (Azure RM parent-child demo)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --output artifacts/azure-rm-parent-child-demo.md \
  src/tests/Oocx.TfPlan2Md.TUnit/TestData/multiple-parents-same-type.json

if [[ -s artifacts/azure-rm-parent-child-demo.md ]]; then
  log_info "✓ artifacts/azure-rm-parent-child-demo.md generated successfully"
else
  log_error "Failed to generate artifacts/azure-rm-parent-child-demo.md"
  exit 1
fi

log_info "Generating artifacts/test-vnet-separate.md (VNet separate subnets test)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --output artifacts/test-vnet-separate.md \
  src/tests/Oocx.TfPlan2Md.TUnit/TestData/azurerm-vnet-separate-subnets-plan.json

if [[ -s artifacts/test-vnet-separate.md ]]; then
  log_info "✓ artifacts/test-vnet-separate.md generated successfully"
else
  log_error "Failed to generate artifacts/test-vnet-separate.md"
  exit 1
fi

log_info "Generating artifacts/firewall-application-rules-uat.md (firewall application rules UAT)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --output artifacts/firewall-application-rules-uat.md \
  examples/firewall-application-rules-demo/plan.json

if [[ -s artifacts/firewall-application-rules-uat.md ]]; then
  log_info "✓ artifacts/firewall-application-rules-uat.md generated successfully"
else
  log_error "Failed to generate artifacts/firewall-application-rules-uat.md"
  exit 1
fi

log_info "Generating examples/firewall-rules-demo/firewall-rules.md (firewall rules example)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --principal-mapping examples/firewall-rules-demo/principals.json \
  --output examples/firewall-rules-demo/firewall-rules.md \
  examples/firewall-rules-demo/plan.json

if [[ -s examples/firewall-rules-demo/firewall-rules.md ]]; then
  log_info "✓ examples/firewall-rules-demo/firewall-rules.md generated successfully"
else
  log_error "Failed to generate examples/firewall-rules-demo/firewall-rules.md"
  exit 1
fi

log_info "Generating examples/api-management-policy-demo/output.md (API management policy example)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --output examples/api-management-policy-demo/output.md \
  examples/api-management-policy-demo/plan.json

if [[ -s examples/api-management-policy-demo/output.md ]]; then
  log_info "✓ examples/api-management-policy-demo/output.md generated successfully"
else
  log_error "Failed to generate examples/api-management-policy-demo/output.md"
  exit 1
fi

log_info "Generating artifacts/azuredevops-feature-096.md (Azure DevOps repo mapping and icons - feature 096 UAT)..."
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj --no-build -c Release -- \
  --principal-mapping examples/comprehensive-demo/demo-principals.json \
  --output artifacts/azuredevops-feature-096.md \
  examples/azuredevops/terraform_plan.json

if [[ -s artifacts/azuredevops-feature-096.md ]]; then
  log_info "✓ artifacts/azuredevops-feature-096.md generated successfully"
else
  log_error "Failed to generate artifacts/azuredevops-feature-096.md"
  exit 1
fi

log_info "All demo artifacts generated successfully"
