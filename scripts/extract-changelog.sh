#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 2 ]]; then
  echo "Usage: $0 <changelog_path> <current_version> [last_version]" >&2
  exit 1
fi

CHANGELOG_PATH="$1"
CURRENT_VERSION="${2#v}"
LAST_VERSION="${3-}"
LAST_VERSION="${LAST_VERSION#v}"

if [[ ! -f "$CHANGELOG_PATH" ]]; then
  echo "Changelog file not found: $CHANGELOG_PATH" >&2
  exit 1
fi

awk -v current="$CURRENT_VERSION" -v last="$LAST_VERSION" '
  function header_version(line) {
    if (line ~ /^##[[:space:]]+\[?v?[0-9]+\.[0-9]+\.[0-9]+\]?/) {
      match(line, /[0-9]+\.[0-9]+\.[0-9]+/);
      return substr(line, RSTART, RLENGTH);
    }
    return "";
  }
  {
    if (match($0, /^<a name="[^"]+"><\/a>$/)) {
      pending_anchor = $0;
      next;
    }

    version = header_version($0);

    if (version != "") {
      if (found && last != "" && version == last) {
        exit;
      }

      if (version == current) {
        found = 1;
        if (pending_anchor != "") {
          print pending_anchor;
        }
        print $0;
        pending_anchor = "";
        next;
      }

      if (found) {
        if (last == "") {
          exit;
        }

        if (pending_anchor != "") {
          print pending_anchor;
        }
        print $0;
        pending_anchor = "";
        next;
      }

      pending_anchor = "";
      next;
    }

    if (found) {
      print $0;
    }
  }
' "$CHANGELOG_PATH"
