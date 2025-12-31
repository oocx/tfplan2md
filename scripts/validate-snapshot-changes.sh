#!/usr/bin/env bash
set -euo pipefail

# Validate Snapshot Changes
#
# Fails when snapshot files change without an explicit commit-message token.
#
# Token: SNAPSHOT_UPDATE_OK
#
# Usage (local):
#   scripts/validate-snapshot-changes.sh
#
# Usage (CI):
#   scripts/validate-snapshot-changes.sh --base-ref "origin/${GITHUB_BASE_REF}" --head-ref "${GITHUB_SHA}"

TOKEN="SNAPSHOT_UPDATE_OK"
SNAPSHOT_PATH_PREFIX="tests/Oocx.TfPlan2Md.Tests/TestData/Snapshots/"

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

snapshot_changed="false"
while IFS= read -r file; do
  if [[ "$file" == ${SNAPSHOT_PATH_PREFIX}* ]]; then
    snapshot_changed="true"
    break
  fi
done <<< "$changed_files"

if [[ "$snapshot_changed" != "true" ]]; then
  exit 0
fi

commit_messages="$(git log --format=%B "$merge_base".."$head_ref")"

if grep -Fq "$TOKEN" <<< "$commit_messages"; then
  exit 0
fi

echo "ERROR: Snapshot files changed, but no allow token was found in commit messages." >&2
echo "" >&2
echo "Snapshot path: ${SNAPSHOT_PATH_PREFIX}" >&2
echo "Required token (add to at least one commit message in this PR): ${TOKEN}" >&2
echo "" >&2
echo "Why: Snapshot updates must be intentional and explicitly justified." >&2
exit 1
