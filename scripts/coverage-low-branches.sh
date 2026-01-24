#!/usr/bin/env bash
set -euo pipefail

# Lists classes with the lowest branch coverage from a Cobertura XML file.
# Usage: scripts/coverage-low-branches.sh [count] [path-to-cobertura.xml]

count="${1:-30}"
coverage_path="${2:-}"

if [[ -z "$coverage_path" ]]; then
  if [[ -f "src/TestResults/coverage.cobertura.xml" ]]; then
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
classes = []
for cls in root.iter('class'):
    br = cls.get('branch-rate')
    if br is None:
        continue
    try:
        br = float(br)
    except ValueError:
        continue
    classes.append((br, cls.get('name'), cls.get('filename')))
classes.sort()
for br, name, fname in classes[: int("$count")]:
    print(f"{br:.3f} {name} {fname}")
PY