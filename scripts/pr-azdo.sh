#!/usr/bin/env bash
set -euo pipefail

# Azure DevOps PR helper.
# Goal: reduce Maintainer approval interruptions by running PR creation as a single stable command.

AZDO_ORG="${AZDO_ORG:-https://dev.azure.com/oocx}"
AZDO_PROJECT="${AZDO_PROJECT:-test}"
AZDO_REPO="${AZDO_REPO:-test}"
AZDO_REMOTE_NAME="${AZDO_REMOTE_NAME:-azdo}"

usage() {
  cat <<'USAGE'
Usage:
  scripts/pr-azdo.sh create --title <title> --description <text>
  scripts/pr-azdo.sh create --fill
  scripts/pr-azdo.sh abandon --id <pr-id>

Options:
  --title <title>          PR title
  --description <text>     PR description
  --fill                   Derive title/description from git commits (best-effort)
  --id <pr-id>             Pull request ID (for abandon)

Environment:
  AZDO_ORG                 Azure DevOps org URL (default: https://dev.azure.com/oocx)
  AZDO_PROJECT             Azure DevOps project (default: test)
  AZDO_REPO                Azure DevOps repo (default: test)
  AZDO_REMOTE_NAME         git remote name used for pushing (default: azdo)

Notes:
  - Requires: git, Azure CLI (az) + azure-devops extension, jq
  - Merge policy: maintain linear history. In Azure DevOps UI, pick the most rebase/linear option available.
USAGE
}

require_clean_worktree() {
  if ! git diff --quiet || ! git diff --cached --quiet; then
    echo "Error: working tree has uncommitted changes." >&2
    exit 2
  fi
}

require_not_main() {
  local branch
  branch="$(git branch --show-current)"
  if [[ "$branch" == "main" ]]; then
    echo "Error: refusing to run on 'main'." >&2
    exit 2
  fi
}

ensure_azdo_auth() {
  if ! az account show >/dev/null 2>&1; then
    echo "Error: not authenticated. Run: az login" >&2
    exit 2
  fi

  if ! az extension show --name azure-devops >/dev/null 2>&1; then
    echo "azure-devops extension not found; installing..." >&2
    az extension add --name azure-devops >/dev/null
  fi

  az devops configure --defaults organization="$AZDO_ORG" project="$AZDO_PROJECT" >/dev/null
}

ensure_remote() {
  if git remote get-url "$AZDO_REMOTE_NAME" >/dev/null 2>&1; then
    return 0
  fi

  # Same remote URL format used by scripts/uat-azdo.sh
  git remote add "$AZDO_REMOTE_NAME" "https://oocx@dev.azure.com/oocx/$AZDO_PROJECT/_git/$AZDO_REPO"
}

derive_from_git() {
  # Best-effort: first line of last commit as title, body as description.
  # If there is only a single line, description becomes empty.
  local msg
  msg="$(git log -1 --pretty=%B)"
  TITLE="$(printf '%s' "$msg" | head -n 1)"
  DESCRIPTION="$(printf '%s' "$msg" | tail -n +2 | sed '/^\s*$/d' || true)"

  if [[ -z "$TITLE" ]]; then
    TITLE="workflow: update"
  fi

  if [[ -z "$DESCRIPTION" ]]; then
    DESCRIPTION="Created by scripts/pr-azdo.sh."
  fi
}

parse_args() {
  local cmd="$1"
  shift

  TITLE=""
  DESCRIPTION=""
  FILL="false"
  PR_ID=""

  while [[ $# -gt 0 ]]; do
    case "$1" in
      --title)
        TITLE="$2"
        shift 2
        ;;
      --description)
        DESCRIPTION="$2"
        shift 2
        ;;
      --fill)
        FILL="true"
        shift
        ;;
      --id)
        PR_ID="$2"
        shift 2
        ;;
      -h|--help)
        usage
        exit 0
        ;;
      *)
        echo "Error: unknown arg: $1" >&2
        usage
        exit 2
        ;;
    esac
  done

  case "$cmd" in
    create)
      if [[ "$FILL" == "true" ]]; then
        derive_from_git
      else
        if [[ -z "$TITLE" || -z "$DESCRIPTION" ]]; then
          echo "Error: provide --fill OR both --title and --description." >&2
          usage
          exit 2
        fi
      fi
      ;;
    abandon)
      if [[ -z "$PR_ID" ]]; then
        echo "Error: provide --id for abandon." >&2
        usage
        exit 2
      fi
      ;;
    *)
      echo "Error: unknown command: $cmd" >&2
      usage
      exit 2
      ;;
  esac
}

create_pr() {
  local branch
  branch="$(git branch --show-current)"

  ensure_remote
  echo "Pushing branch '$branch' to Azure DevOps remote '$AZDO_REMOTE_NAME'..." >&2
  git push -u "$AZDO_REMOTE_NAME" HEAD:"$branch" --force

  echo "Creating PR in Azure DevOps..." >&2
  local pr_json
  pr_json="$(az repos pr create \
    --organization "$AZDO_ORG" \
    --project "$AZDO_PROJECT" \
    --repository "$AZDO_REPO" \
    --source-branch "$branch" \
    --target-branch main \
    --title "$TITLE" \
    --description "$DESCRIPTION" \
    --output json)"

  local pr_id
  pr_id="$(printf '%s' "$pr_json" | jq -r '.pullRequestId')"

  echo "PR created: #$pr_id" >&2
  echo "URL: $AZDO_ORG/$AZDO_PROJECT/_git/$AZDO_REPO/pullrequest/$pr_id" >&2
  echo "" >&2
  echo "Merge guidance (linear history):" >&2
  echo "- In Azure DevOps UI, choose the most rebase/linear merge option available (often 'Rebase and fast-forward')." >&2
  echo "- Avoid squash/merge commits unless Maintainer requests otherwise." >&2
}

abandon_pr() {
  echo "Abandoning PR #$PR_ID in Azure DevOps..." >&2
  az repos pr update \
    --id "$PR_ID" \
    --status abandoned \
    --organization "$AZDO_ORG" \
    --output none
  echo "Abandoned PR #$PR_ID" >&2
}

main() {
  if [[ $# -lt 1 ]]; then
    usage
    exit 2
  fi

  local cmd="$1"
  shift

  if [[ "$cmd" == "create" ]]; then
    require_not_main
    require_clean_worktree
  fi

  if ! command -v az >/dev/null 2>&1; then
    echo "Error: az is not installed." >&2
    exit 2
  fi

  if ! command -v jq >/dev/null 2>&1; then
    echo "Error: jq is not installed." >&2
    exit 2
  fi

  ensure_azdo_auth
  parse_args "$cmd" "$@"

  case "$cmd" in
    create)
      create_pr
      ;;
    abandon)
      abandon_pr
      ;;
  esac
}

main "$@"
