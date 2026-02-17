#!/usr/bin/env bash
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

base_ref=""
head_ref=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --base-ref)
      base_ref="$2"
      shift 2
      ;;
    --head-ref)
      head_ref="$2"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 2
      ;;
  esac
done

if [[ -z "$head_ref" ]]; then
  head_ref="HEAD"
fi

if [[ -z "$base_ref" ]]; then
  if git show-ref --verify --quiet refs/remotes/origin/main; then
    base_ref="origin/main"
  else
    base_ref="main"
  fi
fi

merge_base="$(git merge-base "$base_ref" "$head_ref")"
changed_files="$(git diff --name-only "$merge_base".."$head_ref" || true)"

if [[ -z "$changed_files" ]]; then
  exit 0
fi

declare -A changed_work_items=()

while IFS= read -r file; do
  if [[ "$file" =~ ^docs/(features|issues)/([^/]+)/ ]]; then
    changed_work_items["docs/${BASH_REMATCH[1]}/${BASH_REMATCH[2]}"]=1
  fi
done <<< "$changed_files"

if [[ ${#changed_work_items[@]} -eq 0 ]]; then
  exit 0
fi

missing=()
for work_item in "${!changed_work_items[@]}"; do
  if ! git cat-file -e "${head_ref}:${work_item}/release-notes.md" 2>/dev/null; then
    missing+=("${work_item}/release-notes.md")
  fi
done

if [[ ${#missing[@]} -eq 0 ]]; then
  echo "✓ Release notes present for changed docs/features and docs/issues work items."
  exit 0
fi

echo "❌ ERROR: Missing release notes for changed feature/issue work items:" >&2
for path in "${missing[@]}"; do
  echo "  - $path" >&2
done
echo "" >&2
echo "Each changed directory under docs/features/* or docs/issues/* must include release-notes.md." >&2
exit 1
