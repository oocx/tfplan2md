#!/usr/bin/env bash
# UAT Wrapper Script (GitHub + Azure DevOps)
#
# Purpose: Reduce Maintainer approval fatigue by batching UAT into one stable command.

set -euo pipefail

# Prevent interactive pagers from blocking automation
export GH_PAGER=cat
export GH_FORCE_TTY=false
export AZURE_CORE_PAGER=cat
export PAGER=cat

# Usage:
#   scripts/uat-run.sh [artifact-path] <test-description> [--platform both|github|azdo]
#
# Arguments:
#   artifact-path      - (Optional) Path to markdown artifact to test
#   test-description   - (Required) Detailed, resource-specific validation instructions
#
# Examples:
#   # Using default artifact
#   scripts/uat-run.sh "In module.security.azurerm_key_vault_secret.audit_policy, verify key_vault_id displays as 'Key Vault \`kv-name\` in resource group \`rg-name\`' instead of full /subscriptions/ path"
#
#   # Using custom artifact
#   scripts/uat-run.sh artifacts/custom.md "In azurerm_role_assignment.contributor, verify principal displays as 'John Doe (john.doe@example.com)' instead of GUID"
#
#   # GitHub only
#   scripts/uat-run.sh "Verify firewall rules show before/after in clear table format" --platform github
#
# Defaults:
# - If artifact-path is omitted, defaults are selected per platform:
#   - GitHub: $UAT_ARTIFACT_GITHUB (fallback: artifacts/comprehensive-demo.md)
#   - AzDO:   $UAT_ARTIFACT_AZDO   (fallback: artifacts/comprehensive-demo.md)
#
# Notes:
# - Creates a temporary, unique UAT branch off the current branch.
# - Creates UAT PR(s), polls for approval, then cleans up PR(s) and branches.

set -euo pipefail

# Prevent interactive pagers from blocking automation
export PAGER="${PAGER:-cat}"

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

log_info() { echo -e "${GREEN}[INFO]${NC} $*"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $*"; }
log_error() { echo -e "${RED}[ERROR]${NC} $*" >&2; }

die_usage() {
  log_error "Usage: $0 [artifact-path] <test-description> [--platform both|github|azdo]"
  log_error "Test description must be detailed and resource-specific:"
  log_error "Example: $0 'In module.security.azurerm_key_vault_secret.audit_policy, verify key_vault_id displays as \"Key Vault \\\`kv-name\\\` in resource group \\\`rg-name\\\`\" instead of full /subscriptions/ path'"
  log_error "Example: $0 artifacts/custom.md 'In azurerm_firewall_network_rule_collection.rules, verify attribute values use code blocks instead of bold'"
  exit 2
}

artifact_arg=""
test_description=""

# Parse positional arguments
if [[ "${1:-}" != "" && "${1:-}" != --* ]]; then
  # Could be artifact or test description
  if [[ "${2:-}" != "" && "${2:-}" != --* ]]; then
    # Two positional args: artifact + description
    artifact_arg="$1"
    test_description="$2"
    shift 2 || true
  else
    # One positional arg: treat as test description
    test_description="$1"
    shift || true
  fi
fi

platform="both"
while [[ $# -gt 0 ]]; do
  case "$1" in
    --platform)
      platform="${2:-}"
      shift 2
      ;;
    *)
      die_usage
      ;;
  esac
done

if [[ "$platform" != "both" && "$platform" != "github" && "$platform" != "azdo" ]]; then
  log_error "Invalid --platform value: $platform"
  die_usage
fi

# Validate required test description
if [[ -z "$test_description" ]]; then
  log_error "Test description is required"
  die_usage
fi

# Smart defaults: Let individual scripts determine platform-specific defaults
# unless explicitly overridden
artifact_github="${UAT_ARTIFACT_GITHUB:-}"
artifact_azdo="${UAT_ARTIFACT_AZDO:-}"

if [[ -n "$artifact_arg" ]]; then
  artifact_github="$artifact_arg"
  artifact_azdo="$artifact_arg"
fi

# Apply user-facing defaults for visibility, and summarize chosen artifacts before creating PRs
if [[ -z "$artifact_github" ]]; then
  artifact_github="artifacts/comprehensive-demo-simple-diff.md"
fi
if [[ -z "$artifact_azdo" ]]; then
  artifact_azdo="artifacts/comprehensive-demo.md"
fi
log_info "Artifacts to be used: GitHub: $artifact_github, AzDO: $artifact_azdo"

# Note: Artifact existence checks moved to individual scripts
# which will also apply smart defaults if artifact is empty

original_branch="$(git branch --show-current)"
if [[ "$original_branch" == "main" ]]; then
  log_error "Refusing to run UAT from 'main'. Switch to a feature branch first."
  exit 1
fi

if [[ -n "$(git status --porcelain)" ]]; then
  log_error "Working tree is not clean. Commit or stash changes before running UAT."
  exit 1
fi

# Artifact validation is now handled by individual scripts (uat-github.sh / uat-azdo.sh)
# which will enforce simulation blocking and apply platform-specific smart defaults

timestamp="$(date -u +%Y%m%d%H%M%S)"
# Create a unique, safe UAT branch name.
# Example: uat/feature-foo-uat-20251225205901
safe_original="${original_branch//\//-}"
uat_branch="uat/${safe_original}-uat-${timestamp}"

cleanup_on_exit() {
  # Best-effort restore if something fails.
  if git rev-parse --verify "$original_branch" >/dev/null 2>&1; then
    git switch "$original_branch" >/dev/null 2>&1 || true
  fi
}
trap cleanup_on_exit EXIT

log_info "Creating UAT branch: $uat_branch"
git switch -c "$uat_branch" >/dev/null

gh_pr=""
azdo_pr=""

simulate="${UAT_SIMULATE:-false}"

if [[ "$platform" == "both" || "$platform" == "github" ]]; then
  log_info "Creating GitHub UAT PR..."
  gh_out="$(scripts/uat-github.sh create "$artifact_github" "$test_description" | cat)"
  gh_pr="$(echo "$gh_out" | grep -oE 'PR created: #[0-9]+' | grep -oE '[0-9]+' | tail -n 1)"
  gh_url="$(echo "$gh_out" | grep -oE 'PR created: #[0-9]+ \((.*)\)' | sed -E 's/.*PR created: #[0-9]+ \((.*)\)/\1/' | tail -n 1)"
  if [[ -z "$gh_pr" ]]; then
    log_error "Failed to parse GitHub PR number from output."
    exit 1
  fi
  log_info "GitHub PR: #$gh_pr ($gh_url)"
fi

if [[ "$platform" == "both" || "$platform" == "azdo" ]]; then
  log_info "Ensuring Azure DevOps setup..."
  scripts/uat-azdo.sh setup

  log_info "Creating Azure DevOps UAT PR..."
  azdo_out="$(scripts/uat-azdo.sh create "$artifact_azdo" "$test_description" | cat)"
  azdo_pr="$(echo "$azdo_out" | grep -oE 'PR created: #[0-9]+' | grep -oE '[0-9]+' | tail -n 1)"
  azdo_url="$(echo "$azdo_out" | grep -oE 'PR created: #[0-9]+ \((.*)\)' | sed -E 's/.*PR created: #[0-9]+ \((.*)\)/\1/' | tail -n 1)"
  if [[ -z "$azdo_pr" ]]; then
    log_error "Failed to parse Azure DevOps PR id from output."
    exit 1
  fi
  log_info "Azure DevOps PR: #$azdo_pr ($azdo_url)"
fi

poll_interval_seconds=15
timeout_seconds=$((60 * 60))
start_epoch="$(date +%s)"

if [[ "$simulate" == "true" ]]; then
  log_warn "SIMULATION MODE enabled (UAT_SIMULATE=true): PRs created with [SIMULATION] prefix."
  log_warn "The script will now POLL for approval just like a real run."
  log_warn "Approve the PRs to test the detection logic, or use Ctrl+C to abort."
fi

while true; do
  now_epoch="$(date +%s)"
  elapsed=$((now_epoch - start_epoch))
  if [[ $elapsed -gt $timeout_seconds ]]; then
    log_error "Timed out waiting for approval after $elapsed seconds."
    log_error "Check the PR comments for feedback and re-run once resolved."
    exit 1
  fi

  gh_ok=0
  azdo_ok=0

  if [[ -n "$gh_pr" ]]; then
    scripts/uat-github.sh poll "$gh_pr" && gh_ok=1 || {
      rc=$?
      if [[ $rc -eq 2 ]]; then
        log_error "GitHub polling failed with a fatal error (exit code 2)."
        exit 1
      fi
      gh_ok=0
    }
  else
    gh_ok=1
  fi

  if [[ -n "$azdo_pr" ]]; then
    scripts/uat-azdo.sh poll "$azdo_pr" && azdo_ok=1 || {
      rc=$?
      if [[ $rc -eq 2 ]]; then
        log_error "Azure DevOps polling failed with a fatal error (exit code 2)."
        exit 1
      fi
      azdo_ok=0
    }
  else
    azdo_ok=1
  fi

  if [[ $gh_ok -eq 1 && $azdo_ok -eq 1 ]]; then
    log_info "UAT approved on selected platform(s)."
    break
  fi

  sleep "$poll_interval_seconds"
done

log_info "Cleaning up UAT PRs..."
if [[ -n "$gh_pr" ]]; then
  scripts/uat-github.sh cleanup "$gh_pr"
fi

if [[ -n "$azdo_pr" ]]; then
  scripts/uat-azdo.sh cleanup "$azdo_pr"
fi

log_info "Deleting remote branch: $uat_branch"
if git remote get-url uat-github >/dev/null 2>&1; then
  git push uat-github --delete "$uat_branch" >/dev/null 2>&1 || log_warn "Failed to delete uat-github/$uat_branch (may already be deleted)."
fi
if git remote get-url origin >/dev/null 2>&1; then
  git push origin --delete "$uat_branch" >/dev/null 2>&1 || log_warn "Failed to delete origin/$uat_branch (may already be deleted)."
fi

log_info "Restoring original branch: $original_branch"
git switch "$original_branch" >/dev/null

log_info "Deleting local UAT branch: $uat_branch"
git branch -D "$uat_branch" >/dev/null 2>&1 || true

log_info "UAT run complete."
