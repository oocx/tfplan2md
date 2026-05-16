#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

# Directory paths that indicate the PR is changing workflow or shipped behavior and
# therefore must be anchored to a documented work item with release artifacts.
WORK_ITEM_REQUIRED_DIR_PATTERN='^(src/|scripts/|\.github/|examples/)'

# Individual documentation files that can change workflow expectations globally and
# should therefore also require a matching work item folder.
WORK_ITEM_REQUIRED_FILE_PATTERN='^(docs/agents\.md$|docs/spec\.md$|README\.md$|CONTRIBUTING\.md$)'

# Release-note screenshots must point at versioned raw GitHub URLs inside this repo's
# docs work-item folders so the images render correctly in GitHub Releases.
SCREENSHOT_URL_PATTERN='^https://raw\.githubusercontent\.com/oocx/tfplan2md/v[^/]+/docs/(features|issues|workflow)/[^/]+/[^/]+\.png$'
SCREENSHOT_URL_TO_REPO_PATH_PATTERN='^https://raw\.githubusercontent\.com/oocx/tfplan2md/v[^/]+/(docs/(features|issues|workflow)/[^/]+/[^/]+\.png)$'

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
work_item_required=false

while IFS= read -r file; do
  if [[ "$file" =~ ^docs/(features|issues|workflow)/([^/]+)/ ]]; then
    changed_work_items["docs/${BASH_REMATCH[1]}/${BASH_REMATCH[2]}"]=1
  fi

  if [[ "$file" =~ $WORK_ITEM_REQUIRED_DIR_PATTERN ]] || [[ "$file" =~ $WORK_ITEM_REQUIRED_FILE_PATTERN ]]; then
    work_item_required=true
  fi
done <<< "$changed_files"

if [[ "$work_item_required" == true && ${#changed_work_items[@]} -eq 0 ]]; then
  echo "❌ ERROR: This change requires a work item folder under docs/features/, docs/issues/, or docs/workflow/." >&2
  echo "Update the matching work item folder with release-notes.md and work-protocol.md so Release Manager handoffs can be validated." >&2
  exit 1
fi

if [[ ${#changed_work_items[@]} -eq 0 ]]; then
  exit 0
fi

missing=()
invalid_work_protocols=()
invalid_screenshots=()
invalid_screenshot_metadata=()

validate_release_notes_file() {
  local release_notes_path="$1"
  local release_notes_file="${REPO_ROOT}/${release_notes_path}"
  local screenshot_count=0
  local metadata_count=0

  if [[ ! -f "$release_notes_file" ]]; then
    invalid_screenshots+=("${release_notes_path} (file missing from working tree)")
    return
  fi

  while IFS= read -r screenshot_ref; do
    [[ -z "$screenshot_ref" ]] && continue
    screenshot_count=$((screenshot_count + 1))

    if ! printf '%s\n' "$screenshot_ref" | grep -Eq "$SCREENSHOT_URL_PATTERN"; then
      invalid_screenshots+=("${release_notes_path} -> ${screenshot_ref} (must use raw.githubusercontent.com URL under docs/)")
      continue
    fi

    local repo_image_path
    repo_image_path="$(printf '%s\n' "$screenshot_ref" | sed -E "s#${SCREENSHOT_URL_TO_REPO_PATH_PATTERN}#\\1#")"
    if ! git cat-file -e "${head_ref}:${repo_image_path}" 2>/dev/null; then
      invalid_screenshots+=("${release_notes_path} -> ${repo_image_path} (referenced PNG does not exist)")
      continue
    fi

    if command -v identify >/dev/null 2>&1; then
      local dimensions
      dimensions="$(git show "${head_ref}:${repo_image_path}" | identify -format '%w %h' - 2>/dev/null || true)"
      if [[ "$dimensions" =~ ^([0-9]+)\ ([0-9]+)$ ]]; then
        local width="${BASH_REMATCH[1]}"
        local height="${BASH_REMATCH[2]}"
        if (( width > 580 || height > 400 )); then
          invalid_screenshots+=("${release_notes_path} -> ${repo_image_path} (${width}x${height}; release-note screenshots must be <= 580x400)")
        fi
      fi
    fi
  done < <(grep -oE '!\[[^]]*\]\(([^)]+\.png)\)' "$release_notes_file" | sed -E 's/^!\[[^]]*\]\(([^)]+)\)$/\1/' || true)

  while IFS= read -r metadata_line; do
    [[ -z "$metadata_line" ]] && continue
    metadata_count=$((metadata_count + 1))
    if ! printf '%s\n' "$metadata_line" | grep -Eq 'focus="[^"]+"' || \
      ! printf '%s\n' "$metadata_line" | grep -Eq '(selector|target-resource-id)="[^"]+"'; then
      invalid_screenshot_metadata+=("${release_notes_path} -> ${metadata_line}")
    fi
  done < <(grep -E '^<!-- release-screenshot:' "$release_notes_file" || true)

  if (( screenshot_count > 0 && metadata_count != screenshot_count )); then
    invalid_screenshot_metadata+=("${release_notes_path} (expected ${screenshot_count} release-screenshot metadata comment(s), found ${metadata_count})")
  fi
}

for work_item in "${!changed_work_items[@]}"; do
  release_notes_path="${work_item}/release-notes.md"
  work_protocol_path="${work_item}/work-protocol.md"

  if ! git cat-file -e "${head_ref}:${release_notes_path}" 2>/dev/null; then
    missing+=("${release_notes_path}")
  else
    validate_release_notes_file "$release_notes_path"
  fi

  if ! git cat-file -e "${head_ref}:${work_protocol_path}" 2>/dev/null; then
    missing+=("${work_protocol_path}")
  elif ! git show "${head_ref}:${work_protocol_path}" | grep -Eq '^###\s*Release Manager(\s+—.*)?\s*$'; then
    invalid_work_protocols+=("${work_protocol_path} (missing Release Manager entry)")
  fi
done

if [[ ${#missing[@]} -gt 0 ]]; then
  echo "❌ ERROR: Missing required release artifacts:" >&2
  for path in "${missing[@]}"; do
    echo "  - $path" >&2
  done
  echo "" >&2
  echo "Each changed work item under docs/features/*, docs/issues/*, or docs/workflow/* must include release-notes.md and work-protocol.md." >&2
fi

if [[ ${#invalid_work_protocols[@]} -gt 0 ]]; then
  echo "❌ ERROR: Invalid work-protocol.md entries:" >&2
  for problem in "${invalid_work_protocols[@]}"; do
    echo "  - $problem" >&2
  done
  echo "" >&2
  echo "Release Manager must append a log entry to each changed work item's work-protocol.md before merge." >&2
fi

if [[ ${#invalid_screenshots[@]} -gt 0 ]]; then
  echo "❌ ERROR: Invalid release-note screenshot references:" >&2
  for problem in "${invalid_screenshots[@]}"; do
    echo "  - $problem" >&2
  done
  echo "" >&2
  echo "Use raw.githubusercontent.com URLs that point to real PNG files under the changed work item folder." >&2
fi

if [[ ${#invalid_screenshot_metadata[@]} -gt 0 ]]; then
  echo "❌ ERROR: Invalid screenshot targeting metadata:" >&2
  for problem in "${invalid_screenshot_metadata[@]}"; do
    echo "  - $problem" >&2
  done
  echo "" >&2
  echo "Each screenshot needs a one-line comment like:" >&2
  echo "  <!-- release-screenshot: selector=\"summary:has-text('resource')\"; focus=\"Shows the changed summary line\" -->" >&2
fi

if [[ ${#missing[@]} -gt 0 || ${#invalid_work_protocols[@]} -gt 0 || ${#invalid_screenshots[@]} -gt 0 || ${#invalid_screenshot_metadata[@]} -gt 0 ]]; then
  exit 1
fi

echo "✓ Release artifacts valid for changed work items."
