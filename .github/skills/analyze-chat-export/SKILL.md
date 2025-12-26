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

See these reference documents:
- [Chat Export Structure](reference/chat-export-structure.md) - Empirical analysis of exported data
- [Chat Export Format Specification](reference/chat-export-format.md) - VS Code source-based type definitions

### Quick Reference: Top-Level Keys
```json
{
  "initialLocation": "panel",
  "requests": [...],
  "responderAvatarIconUri": { "id": "copilot" },
  "responderUsername": "Copilot"
}
```

### Quick Reference: Request Fields
| Field | Description |
|-------|-------------|
| `modelId` | Model used (e.g., `copilot/gpt-5.1-codex-max`) |
| `timestamp` | Unix timestamp in milliseconds |
| `timeSpentWaiting` | Time waiting for user confirmation (ms) |
| `message.text` | User's input text |
| `response[]` | Array of response elements (text, thinking, tool invocations) |
| `result.timings.totalElapsed` | Total response time (ms) |
| `result.timings.firstProgress` | Time to first content (ms) |
| `modelState.value` | Response state (0=Pending, 1=Complete, 2=Cancelled, 3=Failed, 4=NeedsInput) |
| `vote` | User feedback (0=down, 1=up) |
| `editedFileEvents[]` | Files edited with accept/reject status |

### Quick Reference: Confirmation Types (isConfirmed.type)
| Type | Meaning |
|------|---------|
| 0 | Pending or cancelled |
| 1 | Auto-approved |
| 3 | Profile-scoped auto-approve |
| 4 | Manually approved |

### Quick Reference: Response State (modelState.value)
| Value | Meaning |
|-------|---------|
| 0 | Pending - still generating |
| 1 | Complete - success |
| 2 | Cancelled - user cancelled |
| 3 | Failed - error occurred |
| 4 | NeedsInput - waiting for confirmation |

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

### 8. Extract Response Timings
```bash
# Average response time (totalElapsed) in seconds
jq '[.requests[].result.timings.totalElapsed // 0] | add / length / 1000' "$CHAT_FILE"

# Average time to first progress in milliseconds
jq '[.requests[].result.timings.firstProgress // 0] | add / length' "$CHAT_FILE"

# Response state distribution (1=Complete, 2=Cancelled, 3=Failed)
jq '[.requests[].modelState.value] | group_by(.) | map({state: .[0], count: length})' "$CHAT_FILE"
```

### 9. Extract User Feedback
```bash
# Vote distribution (0=down, 1=up)
jq '[.requests[] | select(.vote != null) | .vote] | group_by(.) | map({vote: (if .[0] == 1 then "up" else "down" end), count: length})' "$CHAT_FILE"

# Vote down reasons
jq '[.requests[] | select(.voteDownReason != null) | .voteDownReason] | group_by(.) | map({reason: .[0], count: length})' "$CHAT_FILE"
```

### 10. Extract File Edit Statistics
```bash
# Files edited with accept/reject status (1=Keep, 2=Undo, 3=UserModification)
jq '[.requests[].editedFileEvents[]? | {uri: .uri.path, status: (if .eventKind == 1 then "kept" elif .eventKind == 2 then "undone" else "modified" end)}]' "$CHAT_FILE"

# Count of edits by status
jq '[.requests[].editedFileEvents[]?.eventKind] | group_by(.) | map({status: (if .[0] == 1 then "kept" elif .[0] == 2 then "undone" else "modified" end), count: length})' "$CHAT_FILE"
```

### 11. Detect Errors and Cancellations
```bash
# Failed requests (modelState.value == 3)
jq '[.requests[] | select(.modelState.value == 3) | {id: .requestId, error: .result.errorDetails.message}]' "$CHAT_FILE"

# Cancelled requests (modelState.value == 2)
jq '[.requests[] | select(.modelState.value == 2)] | length' "$CHAT_FILE"

# Error codes
jq '[.requests[] | select(.result.errorDetails != null) | .result.errorDetails.code] | group_by(.) | map({code: .[0], count: length})' "$CHAT_FILE"
```

### 12. Generate Summary Report
After running the extraction queries, compile results into a metrics section:

```markdown
## Chat Log Metrics

| Metric | Value |
|--------|-------|
| Total Requests | N |
| Session Duration | Xh Ym |
| Avg Response Time | X.Xs |
| Tool Invocations | N |
| Manual Approvals | N |
| Cancelled/Failed | N |
| User Upvotes | N |
| Est. Premium Requests | N |

### Model Usage
| Model | Count | % |
|-------|-------|---|
| ... | ... | ... |

### Top Tools
| Tool | Count |
|------|-------|
| ... | ... |

### File Edits
| Status | Count |
|--------|-------|
| Kept | N |
| Undone | N |
| Modified | N |
```

## Metrics Available

### ✅ Reliably Extractable
- Total requests/turns
- Models used (with counts)
- Session start/end timestamps
- Response timings (`totalElapsed`, `firstProgress`)
- Tool usage breakdown
- Manual vs auto-approval counts
- Terminal command exit codes
- Response states (complete, cancelled, failed)
- User feedback votes and reasons
- File edit acceptance/rejection status

### ⚠️ Partially Available
- Custom agent names (must parse from message text for `@agent-name` patterns)
- Extended thinking content (may be encrypted)
- `timeSpentWaiting` - appears to be time waiting for user confirmation, not agent processing time

### ❌ Not Available
- Token counts
- Actual cost in dollars
- User reaction/thinking time between responses
- Agent handoff events as distinct records

## Output
Metrics extracted from chat export for inclusion in `retrospective.md`.
