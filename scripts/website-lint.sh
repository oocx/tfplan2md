#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage: scripts/website-lint.sh [--all] [--base <ref>]

Lints website files (HTML/CSS/JS) using on-demand tools via npx.

Default behavior:
  - Lints files changed vs <base> (auto-detected, default origin/main)
  - Also includes staged and unstaged changes on top of HEAD

Options:
  --all           Lint all website files (can be slower / noisier)
  --base <ref>    Git ref to diff against (default: origin/main if present)
EOF
}

lint_all=false
base_ref=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --all)
      lint_all=true
      shift
      ;;
    --base)
      base_ref="${2:-}"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage
      exit 2
      ;;
  esac
done

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

if ! command -v node >/dev/null 2>&1; then
  echo "Error: node is required for website linting." >&2
  echo "Install Node.js, then re-run: scripts/website-lint.sh" >&2
  exit 1
fi

if ! command -v npx >/dev/null 2>&1; then
  echo "Error: npx is required for website linting." >&2
  echo "Install npm (Node.js), then re-run: scripts/website-lint.sh" >&2
  exit 1
fi

if [[ -z "$base_ref" ]]; then
  if git show-ref --verify --quiet refs/remotes/origin/main; then
    base_ref="origin/main"
  else
    base_ref="HEAD~1"
  fi
fi

collect_changed_files() {
  local pattern="$1"
  (
    git diff --name-only --diff-filter=ACMR "${base_ref}...HEAD" -- website || true
    git diff --name-only --diff-filter=ACMR --cached -- website || true
    git diff --name-only --diff-filter=ACMR -- website || true
  ) | grep -E "$pattern" \
    | sort -u || true
}

html_files=()
css_files=()
js_files=()

if [[ "$lint_all" == "true" ]]; then
  while IFS= read -r file; do html_files+=("$file"); done < <(
    find website \
      -type f -name '*.html' \
      ! -path 'website/prototypes/*' \
      ! -path 'website/assets/icons/*.html' \
      -print
  )
  while IFS= read -r file; do css_files+=("$file"); done < <(find website -type f -name '*.css' -print)
  while IFS= read -r file; do js_files+=("$file"); done < <(find website -type f -name '*.js' -print)
else
  while IFS= read -r file; do html_files+=("$file"); done < <(
    collect_changed_files '\\.html$' \
      | grep -vE '^website/prototypes/' \
      | grep -vE '^website/assets/icons/.*\\.html$' || true
  )
  while IFS= read -r file; do css_files+=("$file"); done < <(collect_changed_files '\\.css$')
  while IFS= read -r file; do js_files+=("$file"); done < <(collect_changed_files '\\.js$')
fi

if [[ ${#html_files[@]} -eq 0 && ${#css_files[@]} -eq 0 && ${#js_files[@]} -eq 0 ]]; then
  echo "No changed website HTML/CSS/JS files detected. Nothing to lint."
  exit 0
fi

echo "Website lint base: ${base_ref} (plus staged/unstaged)"

if [[ ${#html_files[@]} -gt 0 ]]; then
  echo "Linting HTML (${#html_files[@]} file(s))..."
  npx --yes htmlhint@1.1.4 --config website/.htmlhintrc "${html_files[@]}"
fi

if [[ ${#css_files[@]} -gt 0 ]]; then
  echo "Linting CSS (${#css_files[@]} file(s))..."
  npx --yes stylelint@16.10.0 --config website/.stylelintrc.json "${css_files[@]}"
fi

if [[ ${#js_files[@]} -gt 0 ]]; then
  echo "Linting JS (${#js_files[@]} file(s))..."
  # Only lint our website JS (avoid noise in third-party/vendor JS if any gets added later)
  filtered_js_files=()
  for file in "${js_files[@]}"; do
    if [[ "$file" == website/assets/js/* ]]; then
      filtered_js_files+=("$file")
    fi
  done

  if [[ ${#filtered_js_files[@]} -gt 0 ]]; then
    npx --yes eslint@9.20.0 --config website/eslint.config.js "${filtered_js_files[@]}"
  else
    echo "Skipping JS lint: no changed files under website/assets/js/."
  fi
fi

echo "Website lint OK."