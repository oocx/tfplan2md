#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage:
  scripts/pr-github.sh preview --title <title> --body-file <path>
  scripts/pr-github.sh preview --fill

  scripts/pr-github.sh create --title <title> --body-file <path>
  scripts/pr-github.sh create --fill

  scripts/pr-github.sh create-and-merge --title <title> --body-file <path>
  scripts/pr-github.sh create-and-merge --fill

Options:
  --title <title>         PR title (use Conventional Commits style)
  --body-file <path>      Path to a markdown/text file used as PR body
  --fill                  Let gh derive title/body from commits

Notes:
  - Requires: git + GitHub CLI (gh) authenticated
  - Merge policy: uses rebase-and-merge for linear history (per CONTRIBUTING.md)
USAGE
}

ensure_origin_main_exists() {
  if git show-ref --verify --quiet refs/remotes/origin/main; then
    return 0
  fi

  git fetch origin main >/dev/null 2>&1 || true
}

print_preview() {
  local base_ref="origin/main"

  ensure_origin_main_exists
  if ! git show-ref --verify --quiet refs/remotes/origin/main; then
    base_ref="main"
  fi

  local title
  local file_count
  local add_total
  local del_total
  local shortstat
  local top_files

  if [[ "$FILL" == "true" ]]; then
    title="$(git log -1 --pretty=%s)"
  else
    title="$TITLE"
  fi

  file_count="$(git diff --name-only "$base_ref"...HEAD | wc -l | tr -d ' ')"
  add_total="$(git diff --numstat "$base_ref"...HEAD | awk '{a+=$1} END {print a+0}')"
  del_total="$(git diff --numstat "$base_ref"...HEAD | awk '{d+=$2} END {print d+0}')"
  shortstat="$(git diff --shortstat "$base_ref"...HEAD || true)"
  top_files="$(git diff --name-only "$base_ref"...HEAD | head -n 3 | sed 's/^/- /')"

  echo "PR Preview"
  echo "Title: $title"
  echo "Summary:"
  if [[ -n "$top_files" ]]; then
    echo "$top_files"
  fi
  echo "- $file_count file(s) changed; +$add_total/-$del_total lines"
  if [[ -n "$shortstat" ]]; then
    echo "- $shortstat"
  fi
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
  local -n _fill="$3"

  shift 3

  _title=""
  _body_file=""
  _fill="false"

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
      --fill)
        _fill="true"
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

  if [[ "$_fill" != "true" ]]; then
    if [[ -z "$_title" || -z "$_body_file" ]]; then
      echo "Error: provide --fill OR both --title and --body-file." >&2
      usage
      exit 2
    fi
    if [[ ! -f "$_body_file" ]]; then
      echo "Error: body file not found: $_body_file" >&2
      exit 2
    fi
  fi
}

ensure_pr_exists() {
  local branch
  branch="$(git branch --show-current)"

  if PAGER=cat gh pr view --json number,url -q '.number' >/dev/null 2>&1; then
    return 0
  fi

  echo "No existing PR found for branch '$branch'; creating..." >&2
  if [[ "$FILL" == "true" ]]; then
    PAGER=cat gh pr create --base main --head "$branch" --fill
  else
    PAGER=cat gh pr create --base main --head "$branch" --title "$TITLE" --body-file "$BODY_FILE"
  fi
}

get_pr_number() {
  PAGER=cat gh pr view --json number -q '.number'
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
    preview|create|create-and-merge)
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
  FILL="false"

  parse_args TITLE BODY_FILE FILL "$@"

  if [[ "$cmd" == "preview" ]]; then
    print_preview
    exit 0
  fi

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
    PAGER=cat gh pr merge "$pr" --rebase --delete-branch
  fi
}

main "$@"
