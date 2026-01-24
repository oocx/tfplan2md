#!/usr/bin/env bash
set -euo pipefail

# Prints line/branch coverage from a Cobertura XML file.
# Usage: scripts/coverage-summary.sh [path-to-cobertura.xml]

coverage_path="${1:-}"

if [[ -z "$coverage_path" ]]; then
  if [[ -f "TestResults/coverage.cobertura.xml" ]]; then
    coverage_path="TestResults/coverage.cobertura.xml"
  elif [[ -f "src/TestResults/coverage.cobertura.xml" ]]; then
    coverage_path="src/TestResults/coverage.cobertura.xml"
  else
    matches=(src/tests/Oocx.TfPlan2Md.TUnit/bin/**/TestResults/*.cobertura.xml)
    if (( ${#matches[@]} > 0 )); then
      coverage_path="${matches[0]}"
    fi
  fi
fi

if [[ -z "$coverage_path" || ! -f "$coverage_path" ]]; then
  echo "Coverage file not found. Pass the cobertura.xml path explicitly." >&2
  exit 1
fi

python3 - <<PY
import xml.etree.ElementTree as ET
from pathlib import Path
path = Path("$coverage_path")
root = ET.parse(path).getroot()
line = float(root.get('line-rate')) * 100
branch = float(root.get('branch-rate')) * 100
print(f"Coverage: line {line:.2f}% branch {branch:.2f}%")
PY
