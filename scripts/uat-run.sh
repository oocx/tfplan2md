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

state_file_default=".tmp/uat-run/last-run.json"
uat_submodule_github_default="uat-repos/github"
uat_submodule_azdo_default="uat-repos/azdo"

get_submodule_head() {
  local path="$1"
  if [[ -e "$path/.git" ]]; then
    git -C "$path" rev-parse HEAD 2>/dev/null || echo ""
    return 0
  fi
  echo ""
}

restore_submodule_head() {
  local path="$1"
  local head="$2"
  if [[ -z "$path" || -z "$head" ]]; then
    return 0
  fi
  if [[ ! -e "$path/.git" ]]; then
    return 0
  fi
  git -C "$path" checkout --detach "$head" >/dev/null 2>&1 || true
  git -C "$path" reset --hard "$head" >/dev/null 2>&1 || true
  git -C "$path" clean -fd >/dev/null 2>&1 || true
}

die_usage() {
  log_error "Usage: $0 [artifact-path] <test-description> [--platform both|github|azdo] [--create-only]"
  log_error "       $0 --cleanup-last [--state-file <path>]"
  log_error "Test description must be detailed and resource-specific:"
  log_error "Example: $0 'In module.security.azurerm_key_vault_secret.audit_policy, verify key_vault_id displays as \"Key Vault \\\`kv-name\\\` in resource group \\\`rg-name\\\`\" instead of full /subscriptions/ path'"
  log_error "Example: $0 artifacts/custom.md 'In azurerm_firewall_network_rule_collection.rules, verify attribute values use code blocks instead of bold'"
  exit 2
}

cmd_cleanup_last() {
  local state_file="$1"

  if [[ ! -f "$state_file" ]]; then
    log_error "State file not found: $state_file"
    log_error "Run with --create-only first (or pass --state-file)."
    exit 1
  fi

  local original_branch
  original_branch="$(jq -r '.original_branch // ""' "$state_file" 2>/dev/null || echo "")"
  local uat_branch
  uat_branch="$(jq -r '.uat_branch // ""' "$state_file" 2>/dev/null || echo "")"
  local gh_pr
  gh_pr="$(jq -r '.github.pr // ""' "$state_file" 2>/dev/null || echo "")"
  local azdo_pr
  azdo_pr="$(jq -r '.azdo.pr // ""' "$state_file" 2>/dev/null || echo "")"

  if [[ -z "$uat_branch" ]]; then
    log_error "Invalid state file (missing uat_branch): $state_file"
    exit 1
  fi

  if [[ -n "$original_branch" && "$(git branch --show-current)" != "$original_branch" ]]; then
    log_warn "Not on original branch '$original_branch' (current: $(git branch --show-current))."
  fi

  log_info "Cleaning up UAT PRs from state: $state_file"
  if [[ -n "$gh_pr" && "$gh_pr" != "null" ]]; then
    scripts/uat-github.sh cleanup "$gh_pr" || true
  fi
  if [[ -n "$azdo_pr" && "$azdo_pr" != "null" ]]; then
    scripts/uat-azdo.sh cleanup "$azdo_pr" || true
  fi

  local uat_submodule_github
  uat_submodule_github="$(jq -r '.submodules.github.path // .submodules.github // ""' "$state_file" 2>/dev/null || echo "")"
  local uat_submodule_github_head
  uat_submodule_github_head="$(jq -r '.submodules.github.head // ""' "$state_file" 2>/dev/null || echo "")"
  local uat_submodule_azdo
  uat_submodule_azdo="$(jq -r '.submodules.azdo.path // .submodules.azdo // ""' "$state_file" 2>/dev/null || echo "")"
  local uat_submodule_azdo_head
  uat_submodule_azdo_head="$(jq -r '.submodules.azdo.head // ""' "$state_file" 2>/dev/null || echo "")"

  log_info "Deleting remote branches: $uat_branch"
  if [[ -n "$uat_submodule_github" && -e "$uat_submodule_github/.git" ]]; then
    git -C "$uat_submodule_github" push origin --delete "$uat_branch" >/dev/null 2>&1 || log_warn "Failed to delete GitHub UAT branch '$uat_branch' (may already be deleted)."
  fi
  if [[ -n "$uat_submodule_azdo" && -e "$uat_submodule_azdo/.git" ]]; then
    git -C "$uat_submodule_azdo" push origin --delete "$uat_branch" >/dev/null 2>&1 || log_warn "Failed to delete AzDO UAT branch '$uat_branch' (may already be deleted)."
  fi

  # Restore submodules to their original HEADs so the parent repo stays clean.
  restore_submodule_head "$uat_submodule_github" "$uat_submodule_github_head"
  restore_submodule_head "$uat_submodule_azdo" "$uat_submodule_azdo_head"

  if [[ -n "$original_branch" ]] && git rev-parse --verify "$original_branch" >/dev/null 2>&1; then
    log_info "Restoring original branch: $original_branch"
    git switch "$original_branch" >/dev/null 2>&1 || true
  fi

  log_info "Cleanup complete."
}

artifact_arg=""
test_description=""

create_only=false
cleanup_last=false
state_file="$state_file_default"

if [[ "${1:-}" == "--cleanup-last" ]]; then
  cleanup_last=true
  shift || true
fi

while [[ $# -gt 0 ]]; do
  case "$1" in
    --state-file)
      state_file="${2:-}"
      shift 2
      ;;
    --cleanup-last)
      cleanup_last=true
      shift
      ;;
    --create-only)
      create_only=true
      shift
      ;;
    *)
      break
      ;;
  esac
done

if [[ "$cleanup_last" == "true" ]]; then
  cmd_cleanup_last "$state_file"
  exit 0
fi

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
    --create-only)
      create_only=true
      shift
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
# Create a unique, safe UAT branch name in the UAT repositories (not this repo).
safe_original="${original_branch//\//-}"
uat_branch="uat/${safe_original}-uat-${timestamp}"

gh_pr=""
azdo_pr=""
gh_url=""
azdo_url=""

print_pr_links_block() {
  echo ""
  echo "UAT PR links (copy/paste):"
  if [[ -n "${gh_url:-}" ]]; then
    echo "  GitHub: ${gh_url}"
  fi
  if [[ -n "${azdo_url:-}" ]]; then
    echo "  Azure DevOps: ${azdo_url}"
  fi
  echo ""
}

uat_submodule_github="${UAT_GITHUB_SUBMODULE_PATH:-$uat_submodule_github_default}"
uat_submodule_azdo="${AZDO_SUBMODULE_PATH:-$uat_submodule_azdo_default}"

uat_submodule_github_head_before="$(get_submodule_head "$uat_submodule_github")"
uat_submodule_azdo_head_before="$(get_submodule_head "$uat_submodule_azdo")"

if [[ "$platform" == "both" || "$platform" == "github" ]]; then
  log_info "Creating GitHub UAT PR..."
  gh_out="$(scripts/uat-github.sh create "$artifact_github" "$test_description" --branch "$uat_branch" | cat)"
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
  azdo_out="$(scripts/uat-azdo.sh create "$artifact_azdo" "$test_description" --branch "$uat_branch" | cat)"
  azdo_pr="$(echo "$azdo_out" | grep -oE 'PR created: #[0-9]+' | grep -oE '[0-9]+' | tail -n 1)"
  azdo_url="$(echo "$azdo_out" | grep -oE 'PR created: #[0-9]+ \((.*)\)' | sed -E 's/.*PR created: #[0-9]+ \((.*)\)/\1/' | tail -n 1)"
  if [[ -z "$azdo_pr" ]]; then
    log_error "Failed to parse Azure DevOps PR id from output."
    exit 1
  fi
  log_info "Azure DevOps PR: #$azdo_pr ($azdo_url)"
fi

print_pr_links_block

if [[ "$create_only" == "true" ]]; then
  mkdir -p "$(dirname "$state_file")"
  jq -n \
    --arg original_branch "$original_branch" \
    --arg uat_branch "$uat_branch" \
    --arg platform "$platform" \
    --arg uat_submodule_github "$uat_submodule_github" \
    --arg uat_submodule_github_head_before "$uat_submodule_github_head_before" \
    --arg uat_submodule_azdo "$uat_submodule_azdo" \
    --arg uat_submodule_azdo_head_before "$uat_submodule_azdo_head_before" \
    --arg artifact_github "$artifact_github" \
    --arg artifact_azdo "$artifact_azdo" \
    --arg gh_pr "$gh_pr" \
    --arg gh_url "${gh_url:-}" \
    --arg azdo_pr "$azdo_pr" \
    --arg azdo_url "${azdo_url:-}" \
    '{
      original_branch: $original_branch,
      uat_branch: $uat_branch,
      platform: $platform,
      submodules: {
        github: { path: $uat_submodule_github, head: $uat_submodule_github_head_before },
        azdo: { path: $uat_submodule_azdo, head: $uat_submodule_azdo_head_before }
      },
      artifacts: { github: $artifact_github, azdo: $artifact_azdo },
      github: { pr: $gh_pr, url: $gh_url },
      azdo: { pr: $azdo_pr, url: $azdo_url }
    }' > "$state_file"

  log_info "Create-only complete. State saved to: $state_file"
  echo ""
  echo "Next steps:"
  echo "  1. Review the PR(s) in the browser"
  echo "  2. Decide PASS/FAIL (record decision in chat)"
  echo "  3. Cleanup when ready: $0 --cleanup-last"
  exit 0
fi

poll_interval_seconds=15
timeout_seconds=$((60 * 60))
start_epoch="$(date +%s)"

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
        log_error "UAT FAILED: Negative feedback or rejection detected on GitHub (PR #$gh_pr)."
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
        log_error "UAT FAILED: Negative feedback or rejection detected on Azure DevOps (PR #$azdo_pr)."
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
if [[ "$platform" == "both" || "$platform" == "github" ]]; then
  if [[ -e "$uat_submodule_github/.git" ]]; then
    git -C "$uat_submodule_github" push origin --delete "$uat_branch" >/dev/null 2>&1 || log_warn "Failed to delete GitHub UAT branch '$uat_branch' (may already be deleted)."
  fi
fi
if [[ "$platform" == "both" || "$platform" == "azdo" ]]; then
  if [[ -e "$uat_submodule_azdo/.git" ]]; then
    git -C "$uat_submodule_azdo" push origin --delete "$uat_branch" >/dev/null 2>&1 || log_warn "Failed to delete AzDO UAT branch '$uat_branch' (may already be deleted)."
  fi
fi

log_info "UAT run complete."
