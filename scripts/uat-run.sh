#!/usr/bin/env bash
# UAT Wrapper Script (GitHub + Azure DevOps)
#
# Purpose: Reduce Maintainer approval fatigue by batching UAT into one stable command.
#
# Usage:
#   scripts/uat-run.sh [artifact-path] [--platform both|github|azdo]
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

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

log_info() { echo -e "${GREEN}[INFO]${NC} $*"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $*"; }
log_error() { echo -e "${RED}[ERROR]${NC} $*" >&2; }

die_usage() {
  log_error "Usage: $0 [artifact-path] [--platform both|github|azdo]"
  exit 2
}

artifact_arg=""
if [[ "${1:-}" != "" && "${1:-}" != --* ]]; then
  artifact_arg="$1"
  shift || true
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

default_artifact="artifacts/comprehensive-demo.md"
artifact_github="${UAT_ARTIFACT_GITHUB:-$default_artifact}"
artifact_azdo="${UAT_ARTIFACT_AZDO:-$default_artifact}"

if [[ -n "$artifact_arg" ]]; then
  artifact_github="$artifact_arg"
  artifact_azdo="$artifact_arg"
fi

if [[ "$platform" == "both" || "$platform" == "github" ]]; then
  if [[ ! -f "$artifact_github" ]]; then
    log_error "Artifact not found for GitHub: $artifact_github"
    exit 1
  fi
fi

if [[ "$platform" == "both" || "$platform" == "azdo" ]]; then
  if [[ ! -f "$artifact_azdo" ]]; then
    log_error "Artifact not found for AzDO: $artifact_azdo"
    exit 1
  fi
fi

original_branch="$(git branch --show-current)"
if [[ "$original_branch" == "main" ]]; then
  log_error "Refusing to run UAT from 'main'. Switch to a feature branch first."
  exit 1
fi

if [[ -n "$(git status --porcelain)" ]]; then
  log_error "Working tree is not clean. Commit or stash changes before running UAT."
  exit 1
fi

# Artifact guardrails: prevent accidental posting of minimal/simulation artifacts.
# Override by setting UAT_ALLOW_MINIMAL=1 (intended for simulate-uat).
if [[ "${UAT_ALLOW_MINIMAL:-}" != "1" ]]; then
  if [[ "$platform" == "both" || "$platform" == "github" ]]; then
    if echo "$(basename "$artifact_github")" | grep -qiE '(minimal|simulation)'; then
      log_error "Refusing to use a minimal/simulation artifact for real UAT: $artifact_github"
      log_error "Use $default_artifact (default), or set UAT_ALLOW_MINIMAL=1 to override."
      exit 1
    fi
  fi

  if [[ "$platform" == "both" || "$platform" == "azdo" ]]; then
    if echo "$(basename "$artifact_azdo")" | grep -qiE '(minimal|simulation)'; then
      log_error "Refusing to use a minimal/simulation artifact for real UAT: $artifact_azdo"
      log_error "Use $default_artifact (default), or set UAT_ALLOW_MINIMAL=1 to override."
      exit 1
    fi
  fi
fi

if [[ "$platform" == "both" ]]; then
  log_info "Using artifacts:"
  log_info "  GitHub: $artifact_github"
  log_info "  AzDO:   $artifact_azdo"
elif [[ "$platform" == "github" ]]; then
  log_info "Using artifact (GitHub): $artifact_github"
else
  log_info "Using artifact (AzDO): $artifact_azdo"
fi

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

if [[ "$platform" == "both" || "$platform" == "github" ]]; then
  log_info "Creating GitHub UAT PR..."
  gh_out="$(scripts/uat-github.sh create "$artifact_github" | cat)"
  gh_pr="$(echo "$gh_out" | grep -oE 'PR created: #[0-9]+' | grep -oE '[0-9]+' | tail -n 1)"
  if [[ -z "$gh_pr" ]]; then
    log_error "Failed to parse GitHub PR number from output."
    exit 1
  fi
  log_info "GitHub PR: #$gh_pr"
fi

if [[ "$platform" == "both" || "$platform" == "azdo" ]]; then
  log_info "Ensuring Azure DevOps setup..."
  scripts/uat-azdo.sh setup

  log_info "Creating Azure DevOps UAT PR..."
  azdo_out="$(scripts/uat-azdo.sh create "$artifact_azdo" | cat)"
  azdo_pr="$(echo "$azdo_out" | grep -oE 'PR created: #[0-9]+' | grep -oE '[0-9]+' | tail -n 1)"
  if [[ -z "$azdo_pr" ]]; then
    log_error "Failed to parse Azure DevOps PR id from output."
    exit 1
  fi
  log_info "Azure DevOps PR: #$azdo_pr"
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
    scripts/uat-github.sh poll "$gh_pr" && gh_ok=1 || gh_ok=0
  else
    gh_ok=1
  fi

  if [[ -n "$azdo_pr" ]]; then
    scripts/uat-azdo.sh poll "$azdo_pr" && azdo_ok=1 || azdo_ok=0
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

log_info "Deleting GitHub remote branch: $uat_branch"
git push origin --delete "$uat_branch" >/dev/null 2>&1 || log_warn "Failed to delete origin/$uat_branch (may already be deleted)."

log_info "Restoring original branch: $original_branch"
git switch "$original_branch" >/dev/null

log_info "Deleting local UAT branch: $uat_branch"
git branch -D "$uat_branch" >/dev/null 2>&1 || true

log_info "UAT run complete."
