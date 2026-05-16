#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

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

  if [[ "$file" =~ ^(src/|scripts/|\.github/|examples/|docs/agents\.md$|docs/spec\.md$|README\.md$|CONTRIBUTING\.md$) ]]; then
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

    if [[ ! "$screenshot_ref" =~ ^https://raw\.githubusercontent\.com/oocx/tfplan2md/v[^/]+/(docs/(features|issues|workflow)/[^/]+/[^)]+\.png)$ ]]; then
      invalid_screenshots+=("${release_notes_path} -> ${screenshot_ref} (must use raw.githubusercontent.com URL under docs/)")
      continue
    fi

    local repo_image_path="${BASH_REMATCH[1]}"
    if ! git cat-file -e "${head_ref}:${repo_image_path}" 2>/dev/null; then
      invalid_screenshots+=("${release_notes_path} -> ${repo_image_path} (referenced PNG does not exist)")
      continue
    fi

    if command -v identify >/dev/null 2>&1 && [[ -f "${REPO_ROOT}/${repo_image_path}" ]]; then
      local dimensions
      dimensions="$(identify -format '%w %h' "${REPO_ROOT}/${repo_image_path}" 2>/dev/null || true)"
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
    if [[ "$metadata_line" != *"focus="* ]] || { [[ "$metadata_line" != *"selector="* ]] && [[ "$metadata_line" != *"target-resource-id="* ]]; }; then
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
  elif [[ -f "${REPO_ROOT}/${work_protocol_path}" ]] && ! grep -Eq '^### Release Manager\b' "${REPO_ROOT}/${work_protocol_path}"; then
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
