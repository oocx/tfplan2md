---
name: analyze-chat-export
description: Export and analyze VS Code Copilot chat logs for retrospective metrics. Extracts model usage, tool invocations, approval patterns, and timing data.
compatibility: Requires jq for JSON processing. Chat must be exported first using VS Code command.
---

# Analyze Chat Export

## Purpose
Extract structured metrics from VS Code Copilot chat exports to support retrospective analysis. Provides data on model usage, tool invocations, manual approvals, and session timing.

## Hard Rules
### Must
- Use `jq` for all JSON extraction (the files are too large for direct loading).
- Redact sensitive information before committing chat logs.
- Save analysis results alongside the chat export in the feature folder.

### Must Not
- Commit unredacted chat logs containing passwords, tokens, API keys, secrets, or PII.
- Load the entire JSON file into memory (use streaming jq queries).

## Pre-requisites
- `jq` command-line JSON processor installed.
- Chat export file (`.json`) already saved via `workbench.action.chat.export` command.

## Export Structure Reference

See [Chat Export Analysis Reference](reference/chat-export-structure.md) for complete documentation of the export format.

### Quick Reference: Top-Level Keys
```json
{
  "initialLocation": "...",
  "requests": [...],
  "responderAvatarIconUri": "...",
  "responderUsername": "..."
}
```

### Quick Reference: Request Fields
| Field | Description |
|-------|-------------|
| `modelId` | Model used (e.g., `copilot/gpt-5.1-codex-max`) |
| `timestamp` | Unix timestamp in milliseconds |
| `timeSpentWaiting` | Agent processing time in milliseconds |
| `message.text` | User's input text |
| `response[]` | Array of response elements (text, thinking, tool invocations) |

### Quick Reference: Confirmation Types
| Type | Meaning |
|------|---------|
| 0 | Pending or cancelled |
| 1 | Auto-approved |
| 3 | Profile-scoped auto-approve |
| 4 | Manually approved |

## Actions

### 1. Export Chat (Prerequisite)
Ask the Maintainer to:
1. Focus the chat panel.
2. Run command: `workbench.action.chat.export`
3. Save to: `docs/features/<feature-name>/chat.json`

### 2. Extract Session Metrics
```bash
CHAT_FILE="docs/features/<feature-name>/chat.json"

# Total requests/turns
jq '.requests | length' "$CHAT_FILE"

# Session duration in minutes
jq '((.requests | last.timestamp) - (.requests | first.timestamp)) / 1000 / 60 | floor' "$CHAT_FILE"

# Total agent work time in minutes
jq '[.requests[].timeSpentWaiting] | add / 1000 / 60 | floor' "$CHAT_FILE"

# First and last timestamps (for start/end times)
jq '.requests | first.timestamp, last.timestamp' "$CHAT_FILE"
```

### 3. Extract Model Usage
```bash
# Models used with counts
jq '[.requests[].modelId] | group_by(.) | map({model: .[0], count: length}) | sort_by(-.count)' "$CHAT_FILE"
```

### 4. Extract Tool Usage
```bash
# Total tool invocations
jq '[.requests[].response[] | select(.kind == "toolInvocationSerialized")] | length' "$CHAT_FILE"

# Tool usage breakdown
jq '[.requests[].response[] | select(.kind == "toolInvocationSerialized") | .toolId] | group_by(.) | map({tool: .[0], count: length}) | sort_by(-.count)' "$CHAT_FILE"
```

### 5. Extract Approval Patterns
```bash
# Approval type distribution
jq '[.requests[].response[] | select(.kind == "toolInvocationSerialized") | .isConfirmed.type // "unknown"] | group_by(.) | map({type: .[0], count: length})' "$CHAT_FILE"

# Count manual approvals (type 0 = pending/cancelled, type 4 = manual)
jq '[.requests[].response[] | select(.kind == "toolInvocationSerialized") | select(.isConfirmed.type == 0 or .isConfirmed.type == 4)] | length' "$CHAT_FILE"
```

### 6. Calculate Premium Request Estimate
```bash
# Model multipliers (update as needed based on docs/ai-model-reference.md)
jq '
  def multiplier:
    if . == "copilot/gpt-5.1-codex-max" then 50
    elif . == "copilot/claude-opus-4.5" then 50
    elif . == "copilot/gpt-5.2" then 10
    elif . == "copilot/gemini-3-pro-preview" then 1
    elif . == "copilot/claude-sonnet-4.5" then 1
    elif . == "copilot/gemini-3-flash-preview" then 0.33
    elif . == "copilot/gpt-5-mini" then 0.25
    elif . == "copilot/claude-haiku-4.5" then 0.05
    else 1
    end;
  [.requests[].modelId | multiplier] | add
' "$CHAT_FILE"
```

### 7. Redact Sensitive Data
```bash
# Create redacted copy
jq '
  .requests |= map(
    .message.text |= (
      gsub("(?i)(password|token|secret|key|bearer)[=: ]+[^\\s\"]+"; "[REDACTED]") |
      gsub("[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}"; "[EMAIL_REDACTED]")
    )
  )
' "$CHAT_FILE" > "${CHAT_FILE%.json}-redacted.json"
```

### 8. Generate Summary Report
After running the extraction queries, compile results into a metrics section:

```markdown
## Chat Log Metrics

| Metric | Value |
|--------|-------|
| Total Requests | N |
| Session Duration | Xh Ym |
| Agent Work Time | Xh Ym |
| Tool Invocations | N |
| Manual Approvals | N |
| Est. Premium Requests | N |

### Model Usage
| Model | Count | % |
|-------|-------|---|
| ... | ... | ... |

### Top Tools
| Tool | Count |
|------|-------|
| ... | ... |
```

## Metrics Available

### ✅ Reliably Extractable
- Total requests/turns
- Models used (with counts)
- Session start/end timestamps
- Total agent processing time
- Tool usage breakdown
- Manual vs auto-approval counts
- Terminal command exit codes

### ⚠️ Partially Available
- Custom agent names (must parse from message text for `@agent-name` patterns)
- Extended thinking content (may be encrypted)

### ❌ Not Available
- Token counts
- Actual cost in dollars
- User reaction/thinking time
- Agent handoff events as distinct records

## Output
Metrics extracted from chat export for inclusion in `retrospective.md`.
