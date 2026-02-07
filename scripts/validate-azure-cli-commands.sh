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
  local file_path="$2"
  python3 - "$label" "$file_path" <<'PY'
import json
import sys

label = sys.argv[1]
file_path = sys.argv[2]
try:
  with open(file_path, "r", encoding="utf-8") as handle:
    data = json.load(handle)
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
  local tmp_dir=".tmp/azure-cli-validation"
  local safe_label="${label// /-}"
  local output_file="${tmp_dir}/${safe_label}.json"
  local error_file="${tmp_dir}/${safe_label}.err"

  echo "Validating $label..."
  mkdir -p "$tmp_dir"
  if ! "$@" >"$output_file" 2>"$error_file"; then
    cat "$error_file" >&2
    exit 1
  fi

  if [[ ! -s "$output_file" ]]; then
    echo "$label: command produced no output." >&2
    cat "$error_file" >&2
    exit 1
  fi

  validate_json_array "$label" "$output_file"
}

supports_flag() {
  local flag="$1"
  shift
  "$@" -h 2>/dev/null | grep -q -- " ${flag}"
}

user_cmd=(az ad user list)
if supports_flag "--all" "${user_cmd[@]}"; then
  user_cmd+=(--all)
fi
user_cmd+=(--query "[].{id:id,displayName:displayName}" -o json)
run_and_validate "principals (users)" "${user_cmd[@]}"

run_and_validate "principals (groups)" az ad group list --query "[].{id:id,displayName:displayName}" -o json

sp_cmd=(az ad sp list)
if supports_flag "--all" "${sp_cmd[@]}"; then
  sp_cmd+=(--all)
fi
sp_cmd+=(--query "[].{id:id,displayName:displayName}" -o json)
run_and_validate "principals (service principals)" "${sp_cmd[@]}"
run_and_validate "subscriptions" az account list --query "[].{id:id,displayName:name}" -o json
run_and_validate "management groups" az account management-group list --query "[].{id:name,displayName:displayName}" -o json
run_and_validate "tenants" az account tenant list --query "[].{id:tenantId,displayName:displayName}" -o json
run_and_validate "custom roles" az role definition list --custom-role-only true --query "[].{id:name,displayName:roleName}" -o json

echo "Azure CLI commands validated."
