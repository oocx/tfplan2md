#!/bin/bash
set -euo pipefail

# Homebrew Formula Update Script
# Updates the tfplan2md Homebrew formula with new version and checksums
# from the consolidated SHA256SUMS file generated during release.
#
# Usage: update-homebrew-formula.sh <VERSION> <CHECKSUMS_FILE> <FORMULA_FILE>
#
# Example:
#   update-homebrew-formula.sh 1.2.0 checksums/SHA256SUMS homebrew-tap/Formula/tfplan2md.rb

VERSION="$1"
CHECKSUMS_FILE="$2"
FORMULA_FILE="$3"

echo "Updating Homebrew formula to version $VERSION"

# Validate inputs
if [ ! -f "$CHECKSUMS_FILE" ]; then
  echo "❌ Error: Checksums file not found: $CHECKSUMS_FILE"
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TEMPLATE_FILE="$SCRIPT_DIR/tfplan2md.rb.template"

if [ ! -f "$TEMPLATE_FILE" ]; then
  echo "❌ Error: Formula template not found: $TEMPLATE_FILE"
  exit 1
fi

# Always regenerate the formula from the template so placeholders are present
mkdir -p "$(dirname "$FORMULA_FILE")"
cp "$TEMPLATE_FILE" "$FORMULA_FILE"

# Extract checksums for each platform
echo "Extracting checksums from $CHECKSUMS_FILE..."
MACOS_ARM64_SHA=$(grep "macos-arm64.tar.gz" "$CHECKSUMS_FILE" | awk '{print $1}')
LINUX_X64_SHA=$(grep "linux-x64.tar.gz" "$CHECKSUMS_FILE" | awk '{print $1}')

# Validate all checksums are present
if [ -z "$MACOS_ARM64_SHA" ] || [ -z "$LINUX_X64_SHA" ]; then
  echo "❌ Error: Missing checksums in $CHECKSUMS_FILE"
  echo "Contents of $CHECKSUMS_FILE:"
  cat "$CHECKSUMS_FILE"
  exit 1
fi

# Validate checksum format (64 hex characters)
for sha in "$MACOS_ARM64_SHA" "$LINUX_X64_SHA"; do
  if ! echo "$sha" | grep -qE '^[a-f0-9]{64}$'; then
    echo "❌ Error: Invalid checksum format: $sha"
    exit 1
  fi
done

echo "Checksums validated:"
echo "  macOS ARM64: $MACOS_ARM64_SHA"
echo "  Linux x64: $LINUX_X64_SHA"

# Update formula file with version and checksums
echo "Updating $FORMULA_FILE..."
sed -i "s/{{VERSION}}/$VERSION/g" "$FORMULA_FILE"
sed -i "s/{{MACOS_ARM64_SHA256}}/$MACOS_ARM64_SHA/g" "$FORMULA_FILE"
sed -i "s/{{LINUX_X64_SHA256}}/$LINUX_X64_SHA/g" "$FORMULA_FILE"

echo "✅ Formula updated to version $VERSION"
