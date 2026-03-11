#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage: scripts/website-lint.sh [--all] [--base <ref>]

Lints the Eleventy website:
  1) Lints generated HTML in website/dist
  2) Lints source CSS and JavaScript
  3) Lints website documentation markdown

Options:
  --all           Accepted for compatibility; linting always targets the current website source/output
  --base <ref>    Accepted for compatibility; currently ignored
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --all)
      shift
      ;;
    --base)
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
  exit 1
fi

if ! command -v npx >/dev/null 2>&1; then
  echo "Error: npx is required for website linting." >&2
  exit 1
fi

if [[ ! -d website/node_modules ]]; then
  echo "Installing website dependencies..."
  (cd website && npm ci)
fi

if [[ ! -d website/dist ]]; then
  echo "Error: website/dist does not exist. Run scripts/website-build.sh first, or use scripts/website-verify.sh --all." >&2
  exit 1
fi

mapfile -t html_files < <(find website/dist -type f -name '*.html' -print | sort)
if [[ ${#html_files[@]} -gt 0 ]]; then
  echo "Linting generated HTML (${#html_files[@]} file(s))..."
  npx --yes htmlhint@1.1.4 --config website/.htmlhintrc "${html_files[@]}"
fi

echo "Linting website CSS..."
npx --yes stylelint@16.10.0 --config website/.stylelintrc.json website/src/style.css

echo "Linting website JS..."
eslint_targets=(website/.eleventy.js)

while IFS= read -r file; do
  eslint_targets+=("$file")
done < <(find website/src/site-assets/js -type f -name '*.js' -print | sort)

while IFS= read -r file; do
  eslint_targets+=("$file")
done < <(find website/lib -type f -name '*.js' -print | sort)

if [[ -d website/tools ]]; then
  while IFS= read -r file; do
    eslint_targets+=("$file")
  done < <(find website/tools -type f -name '*.mjs' -print | sort)
fi

npx --yes eslint@9.20.0 --config website/eslint.config.js "${eslint_targets[@]}"

echo "Linting website markdown..."
scripts/markdownlint.sh website/*.md website/adrs/*.md

echo "Website lint OK."