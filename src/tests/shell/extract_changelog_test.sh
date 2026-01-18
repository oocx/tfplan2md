#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$REPO_ROOT"

fail=0

# Test: Extract prerelease version (alpha)
output=$(bash scripts/extract-changelog.sh CHANGELOG.md "1.0.0-alpha.29" "")
if echo "$output" | grep -q "1.0.0-alpha.29"; then
  echo "OK: prerelease version extracted"
else
  echo "ERROR: failed to extract prerelease version 1.0.0-alpha.29"
  fail=1
fi

# Test: Extract stable version
output=$(bash scripts/extract-changelog.sh CHANGELOG.md "0.49.0" "")
if echo "$output" | grep -q "0.49.0"; then
  echo "OK: stable version extracted"
else
  echo "ERROR: failed to extract stable version 0.49.0"
  fail=1
fi

# Test: Extract range between two prerelease versions
output=$(bash scripts/extract-changelog.sh CHANGELOG.md "1.0.0-alpha.28" "1.0.0-alpha.27")
if echo "$output" | grep -q "1.0.0-alpha.28"; then
  # Check that alpha.27 is NOT included as a header (only in compare link is OK)
  last_version_anchor='<a name="1.0.0-alpha.27"></a>'
  if ! echo "$output" | grep -Fq "$last_version_anchor"; then
    echo "OK: range extraction stopped at last version"
  else
    echo "ERROR: range extraction should stop at last version"
    fail=1
  fi
else
  echo "ERROR: failed to extract prerelease version range"
  fail=1
fi

# Test: Extract range between stable versions
output=$(bash scripts/extract-changelog.sh CHANGELOG.md "0.49.0" "0.48.0")
if echo "$output" | grep -q "0.49.0"; then
  # Check that 0.48.0 is NOT included as a header (only in compare link is OK)
  last_version_anchor='<a name="0.48.0"></a>'
  if ! echo "$output" | grep -Fq "$last_version_anchor"; then
    echo "OK: stable version range extraction stopped at last version"
  else
    echo "ERROR: stable version range extraction should stop at last version"
    fail=1
  fi
else
  echo "ERROR: failed to extract stable version range"
  fail=1
fi

# Test: Non-existent version returns empty
output=$(bash scripts/extract-changelog.sh CHANGELOG.md "99.99.99" "")
if [ -z "$output" ]; then
  echo "OK: non-existent version returns empty"
else
  echo "ERROR: non-existent version should return empty"
  fail=1
fi

if [[ $fail -ne 0 ]]; then
  echo "One or more tests failed." >&2
  exit 1
fi

echo "All tests passed."
