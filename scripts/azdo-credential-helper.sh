#!/usr/bin/env bash
# Git credential helper for Azure DevOps using Azure CLI tokens.
#
# Replaces the Windows Git Credential Manager in WSL environments where
# the Windows .exe binary cannot be executed (e.g. WSL interop is disabled).
# Obtains ephemeral OAuth tokens from Azure CLI (az account get-access-token).

set -euo pipefail

case "${1:-}" in
  get)
    # Read and discard stdin fields (protocol, host, etc.)
    while IFS= read -r line; do
      [[ -z "$line" ]] && break
    done

    token="$(az account get-access-token \
      --resource 499b84ac-1321-427f-aa17-267ca6975798 \
      --query accessToken --output tsv 2>/dev/null || true)"

    if [[ -n "$token" ]]; then
      printf 'username=%s\npassword=%s\n' "azdo-token" "$token"
    fi
    ;;
  store|erase)
    # No-op: tokens are ephemeral
    ;;
esac
