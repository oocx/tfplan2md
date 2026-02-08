#!/usr/bin/env bash
set -euo pipefail

# Prevent interactive pagers from blocking automation
export GH_PAGER=cat
export GH_FORCE_TTY=false
export PAGER=cat

usage() {
  cat <<'USAGE'
Usage:
  scripts/gh-release-view.sh [--repo <owner/repo>] <tag>
  scripts/gh-release-view.sh [--repo <owner/repo>] --latest

Options:
  --repo <owner/repo>       Target repository (overrides GH_REPO)
  --latest                  View the latest release instead of a specific tag

Arguments:
  <tag>                     Release tag to view (e.g., v1.0.0)

Examples:
  # View a specific release
  scripts/gh-release-view.sh v1.0.0

  # View latest release
  scripts/gh-release-view.sh --latest

  # View release in a specific repository
  scripts/gh-release-view.sh --repo oocx/tfplan2md v1.0.0

Notes:
  - All commands suppress interactive pagers (GH_PAGER=cat, GH_FORCE_TTY=false)
  - Designed for permanent approval in VS Code (single script vs multiple gh commands)
  - Use this instead of raw `gh release view` commands to reduce approval friction
  - Returns structured JSON output with release details
USAGE
}

repo=""
tag=""
latest=false

gh_safe() {
  unset GH_REPO
  if [[ -n "$repo" ]]; then
    GH_PAGER=cat GH_FORCE_TTY=false gh "$@" --repo "$repo"
  else
    GH_PAGER=cat GH_FORCE_TTY=false gh "$@"
  fi
}

main() {
  if [[ $# -eq 0 ]]; then
    usage
    exit 1
  fi

  # Parse global options
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --repo)
        repo="$2"
        shift 2
        ;;
      --latest)
        latest=true
        shift
        ;;
      -h|--help|help)
        usage
        exit 0
        ;;
      -*)
        echo "Error: Unknown option '$1'" >&2
        usage
        exit 1
        ;;
      *)
        if [[ -z "$tag" ]]; then
          tag="$1"
          shift
        else
          echo "Error: Unexpected argument '$1'" >&2
          usage
          exit 1
        fi
        ;;
    esac
  done

  if [[ "$latest" == "true" ]]; then
    # View latest release
    gh_safe release view --json tagName,name,body,assets,createdAt,publishedAt,url
  elif [[ -n "$tag" ]]; then
    # View specific release by tag
    gh_safe release view "$tag" --json tagName,name,body,assets,createdAt,publishedAt,url
  else
    echo "Error: Either provide a tag or use --latest flag" >&2
    usage
    exit 1
  fi
}

main "$@"
