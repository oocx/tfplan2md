#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage: scripts/website2-lint.sh [--all]

Builds and lints the Website2 Eleventy site:
  1) Builds website2/dist
  2) Lints generated HTML in dist/
  3) Lints source CSS and JS
  4) Lints Website2 markdown documentation
EOF
}

if [[ ${1:-} == "-h" || ${1:-} == "--help" ]]; then
  usage
  exit 0
fi

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

if ! command -v node >/dev/null 2>&1; then
  echo "Error: node is required for Website2 linting." >&2
  exit 1
fi

if ! command -v npx >/dev/null 2>&1; then
  echo "Error: npx is required for Website2 linting." >&2
  exit 1
fi

if [[ ! -d website2/node_modules ]]; then
  echo "Installing Website2 dependencies..."
  (cd website2 && npm ci)
fi

echo "Building Website2..."
(cd website2 && npm run clean && npm run build)

mapfile -t html_files < <(find website2/dist -type f -name '*.html' -print | sort)
if [[ ${#html_files[@]} -gt 0 ]]; then
  echo "Linting generated HTML (${#html_files[@]} file(s))..."
  npx --yes htmlhint@1.1.4 --config website2/.htmlhintrc "${html_files[@]}"
fi

echo "Linting Website2 CSS..."
npx --yes stylelint@16.10.0 --config website2/.stylelintrc.json website2/src/style.css

echo "Linting Website2 JS..."
eslint_targets=(website2/.eleventy.js)

while IFS= read -r file; do
  eslint_targets+=("$file")
done < <(find website2/src/site-assets/js -type f -name '*.js' -print | sort)

while IFS= read -r file; do
  eslint_targets+=("$file")
done < <(find website2/lib -type f -name '*.js' -print | sort)

if [[ -d website2/tools ]]; then
  while IFS= read -r file; do
    eslint_targets+=("$file")
  done < <(find website2/tools -type f -name '*.mjs' -print | sort)
fi

npx --yes eslint@9.20.0 --config website2/eslint.config.js "${eslint_targets[@]}"

echo "Linting Website2 markdown..."
scripts/markdownlint.sh website2/*.md website2/adrs/*.md

echo "Website2 lint OK."