#!/usr/bin/env bash
set -euo pipefail

# Prevent interactive pagers from blocking automation
export GH_PAGER=cat
export GH_FORCE_TTY=false
export PAGER=cat

usage() {
  cat <<'USAGE'
Usage:
  scripts/pr-github.sh create --title <title> --body-from-stdin

  scripts/pr-github.sh create-and-merge --title <title> --body-from-stdin

Options:
  --title <title>         PR title (use Conventional Commits style)
  --body-from-stdin       Read PR body from stdin

Notes:
  - Required: provide an explicit title + body via stdin (agent-authored description)
  - This script intentionally does not guess title/body
  - **Agent guidance:** This script is the **authoritative** repo tool for creating and merging PRs. Use `scripts/pr-github.sh create` to create PRs and `scripts/pr-github.sh create-and-merge` to merge them (rebase + delete branch). Body must be piped via stdin.
  - **Fallback:** Use GitHub chat tools (`github/*`) only when the script does not support a necessary advanced operation or for quick inspection of checks.
  - Requires: git + GitHub CLI (gh) authenticated when used as a CLI fallback
  - Merge policy: uses rebase-and-merge for linear history (per CONTRIBUTING.md)
USAGE
}

require_non_empty() {
  local value="$1"
  local label="$2"
  if [[ -z "${value//[[:space:]]/}" ]]; then
    echo "Error: $label must not be empty." >&2
    exit 2
  fi
}

gh_safe() {
  GH_PAGER=cat GH_FORCE_TTY=false gh "$@"
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

parse_args() {
  local -n _title="$1"

  shift 1

  _title=""
  BODY_TEXT=""
  USE_STDIN=false

  while [[ $# -gt 0 ]]; do
    case "$1" in
      --title)
        _title="$2"
        shift 2
        ;;
      --body-from-stdin)
        USE_STDIN=true
        shift
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

  if [[ -z "$_title" ]]; then
    echo "Error: provide --title and --body-from-stdin." >&2
    usage
    exit 2
  fi

  if [[ "$USE_STDIN" != "true" ]]; then
    echo "Error: --body-from-stdin is required (pipe body via stdin)." >&2
    usage
    exit 2
  fi

  # Read from stdin
  BODY_TEXT="$(cat)"

  require_non_empty "$_title" "--title"
  require_non_empty "$BODY_TEXT" "PR body"
}

ensure_pr_exists() {
  local branch
  branch="$(git branch --show-current)"

  # Only consider OPEN PRs for this branch.
  # `gh pr view` can resolve merged/closed PRs, which breaks workflows when a branch name is reused.
  local open_pr_number
  open_pr_number="$(gh_safe pr list --head "$branch" --state open --json number -q '.[0].number' 2>/dev/null || true)"
  if [[ -n "${open_pr_number//[[:space:]]/}" ]]; then
    return 0
  fi

  echo "No open PR found for branch '$branch'; creating..." >&2
  require_non_empty "$TITLE" "PR title"
  require_non_empty "$BODY_TEXT" "PR body"
  echo "$BODY_TEXT" | gh_safe pr create --base main --head "$branch" --title "$TITLE" --body-file -
}

get_pr_number() {
  local branch
  branch="$(git branch --show-current)"

  # Prefer the open PR for this branch.
  local open_pr_number
  open_pr_number="$(gh_safe pr list --head "$branch" --state open --json number -q '.[0].number' 2>/dev/null || true)"
  if [[ -n "${open_pr_number//[[:space:]]/}" ]]; then
    echo "$open_pr_number"
    return 0
  fi

  # Fall back to gh's branch inference (may resolve closed PRs). Keep this as a last resort.
  gh_safe pr view --json number -q '.number'
}

main() {
  if [[ $# -lt 1 ]]; then
    usage
    exit 2
  fi

  local cmd
  cmd="$1"
  shift

  case "$cmd" in
    create|create-and-merge)
      ;;
    *)
      echo "Error: unknown command: $cmd" >&2
      usage
      exit 2
      ;;
  esac

  require_not_main

  TITLE=""
  parse_args TITLE "$@"

  require_clean_worktree

  if ! command -v gh >/dev/null 2>&1; then
    echo "Error: gh is not installed." >&2
    exit 2
  fi

  if ! gh auth status >/dev/null 2>&1; then
    echo "Error: gh is not authenticated. Run: gh auth login" >&2
    exit 2
  fi

  git push -u origin HEAD

  ensure_pr_exists

  local pr
  pr="$(get_pr_number)"
  echo "PR: #$pr" >&2

  if [[ "$cmd" == "create-and-merge" ]]; then
    gh_safe pr merge "$pr" --rebase --delete-branch
  fi
}

main "$@"
