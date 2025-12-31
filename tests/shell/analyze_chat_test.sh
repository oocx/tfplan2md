#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$REPO_ROOT"

chmod +x scripts/analyze-chat.py

output="$(scripts/analyze-chat.py tests/shell/testdata/chat-minimal.json)"

echo "$output" | grep -q "Agent Attribution:" || {
  echo "ERROR: expected Agent Attribution section" >&2
  exit 1
}

echo "$output" | grep -q "per-agent metrics are not reported" || {
  echo "ERROR: expected explicit 'per-agent metrics are not reported' note" >&2
  exit 1
}

echo "$output" | grep -q "Retrospective Feedback (verbatim):" || {
  echo "ERROR: expected Retrospective Feedback section" >&2
  exit 1
}

echo "$output" | grep -q "note for retro" || {
  echo "ERROR: expected retro feedback to be included verbatim" >&2
  exit 1
}

echo "$output" | grep -q "Plausibility Warnings:" || {
  echo "ERROR: expected Plausibility Warnings section" >&2
  exit 1
}

echo "OK: analyze-chat.py outputs attribution note, feedback, and warnings"
