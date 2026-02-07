#!/usr/bin/env bash
# Validate Azure CLI commands used for mapping file export.

set -euo pipefail

skip_login=false
if [[ "${1:-}" == "--skip-login" ]]; then
  skip_login=true
fi

if ! command -v az >/dev/null 2>&1; then
  echo "Azure CLI (az) is required." >&2
  exit 1
fi

if ! command -v python3 >/dev/null 2>&1; then
  echo "python3 is required to validate JSON output." >&2
  exit 1
fi

if ! az account show >/dev/null 2>&1; then
  if [[ "$skip_login" == "true" ]]; then
    echo "Azure CLI is not logged in. Run 'az login' or omit --skip-login." >&2
    exit 1
  fi
  az login >/dev/null
fi

validate_json_array() {
  local label="$1"
  python3 - "$label" <<'PY'
import json
import sys

label = sys.argv[1]
try:
    data = json.load(sys.stdin)
except json.JSONDecodeError as exc:
    raise SystemExit(f"{label}: output is not valid JSON ({exc})")

if not isinstance(data, list):
    raise SystemExit(f"{label}: expected a JSON array")

for item in data:
    if not isinstance(item, dict):
        raise SystemExit(f"{label}: expected array of objects")
    if "id" not in item or "displayName" not in item:
        raise SystemExit(f"{label}: each object must include 'id' and 'displayName'")
PY
}

run_and_validate() {
  local label="$1"
  shift
  echo "Validating $label..."
  "$@" | validate_json_array "$label"
}

run_and_validate "principals (users)" az ad user list --all --query "[].{id:id,displayName:displayName}" -o json
run_and_validate "principals (groups)" az ad group list --query "[].{id:id,displayName:displayName}" -o json
run_and_validate "principals (service principals)" az ad sp list --all --query "[].{id:id,displayName:displayName}" -o json
run_and_validate "subscriptions" az account list --query "[].{id:id,displayName:name}" -o json
run_and_validate "management groups" az account management-group list --query "[].{id:name,displayName:displayName}" -o json
run_and_validate "tenants" az account tenant list --query "[].{id:tenantId,displayName:displayName}" -o json
run_and_validate "custom roles" az role definition list --custom-role-only true --query "[].{id:name,displayName:roleName}" -o json

echo "Azure CLI commands validated."
