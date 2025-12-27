#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage:
  scripts/pr-github.sh create --title <title> --body-file <path>
  scripts/pr-github.sh create --title <title> --body <text>
  scripts/pr-github.sh create --title <title> --body-from-stdin

  scripts/pr-github.sh create-and-merge --title <title> --body-file <path>
  scripts/pr-github.sh create-and-merge --title <title> --body <text>
  scripts/pr-github.sh create-and-merge --title <title> --body-from-stdin

Options:
  --title <title>         PR title (use Conventional Commits style)
  --body-file <path>      Path to a markdown/text file used as PR body
  --body <text>           PR body text (alternative to --body-file)
  --body-from-stdin       Read PR body from stdin (avoids temp files)

Notes:
  - Required: provide an explicit title + body (agent-authored description)
  - This script intentionally does not guess title/body
  - **Agent guidance:** This script is the **authoritative** repo tool for creating and merging PRs. Use `scripts/pr-github.sh create` to create PRs and `scripts/pr-github.sh create-and-merge` to merge them (rebase + delete branch). **Prefer --body-from-stdin** to avoid temporary files.
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
  local -n _body_file="$2"

  shift 2

  _title=""
  _body_file=""
  BODY_TEXT=""
  USE_STDIN=false

  while [[ $# -gt 0 ]]; do
    case "$1" in
      --title)
        _title="$2"
        shift 2
        ;;
      --body-file)
        _body_file="$2"
        shift 2
        ;;
      --body)
        BODY_TEXT="$2"
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
    echo "Error: provide --title plus one of: --body-file, --body, --body-from-stdin." >&2
    usage
    exit 2
  fi

  # Count how many body sources are specified
  local body_sources=0
  [[ -n "$BODY_TEXT" ]] && ((body_sources++))
  [[ -n "$_body_file" ]] && ((body_sources++))
  [[ "$USE_STDIN" == "true" ]] && ((body_sources++))

  if [[ $body_sources -eq 0 ]]; then
    echo "Error: provide one of: --body, --body-file, --body-from-stdin." >&2
    usage
    exit 2
  fi

  if [[ $body_sources -gt 1 ]]; then
    echo "Error: provide only one of --body, --body-file, or --body-from-stdin." >&2
    exit 2
  fi

  if [[ -n "$_body_file" && ! -f "$_body_file" ]]; then
    echo "Error: body file not found: $_body_file" >&2
    exit 2
  fi

  # Read from stdin if requested
  if [[ "$USE_STDIN" == "true" ]]; then
    BODY_TEXT="$(cat)"
  fi

  require_non_empty "$_title" "--title"
  if [[ -n "$_body_file" ]]; then
    require_non_empty "$(cat "$_body_file")" "PR body"
  else
    require_non_empty "$BODY_TEXT" "PR body"
  fi
}

ensure_pr_exists() {
  local branch
  branch="$(git branch --show-current)"

  if gh_safe pr view --json number,url -q '.number' >/dev/null 2>&1; then
    return 0
  fi

  echo "No existing PR found for branch '$branch'; creating..." >&2
  require_non_empty "$TITLE" "PR title"
  if [[ -n "${BODY_FILE:-}" ]]; then
    gh_safe pr create --base main --head "$branch" --title "$TITLE" --body-file "$BODY_FILE"
  else
    require_non_empty "$BODY_TEXT" "PR body"
    gh_safe pr create --base main --head "$branch" --title "$TITLE" --body "$BODY_TEXT"
  fi
}

get_pr_number() {
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
  BODY_FILE=""
  parse_args TITLE BODY_FILE "$@"

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
