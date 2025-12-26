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

# First and last timestamps (for start/end times)
jq '.requests | first.timestamp, last.timestamp' "$CHAT_FILE"

# Time breakdown (all in seconds)
jq '
{
  session_duration_sec: (((.requests | last.timestamp) - (.requests | first.timestamp)) / 1000 | floor),
  user_wait_time_sec: (([.requests[].timeSpentWaiting // 0] | add) / 1000 | floor),
  agent_work_time_sec: (([.requests[].result.timings.totalElapsed // 0] | add) / 1000 | floor)
}
| . + {
  user_wait_pct: (if .session_duration_sec > 0 then (.user_wait_time_sec / .session_duration_sec * 100 | floor) else 0 end),
  agent_work_pct: (if .session_duration_sec > 0 then (.agent_work_time_sec / .session_duration_sec * 100 | floor) else 0 end)
}
' "$CHAT_FILE"

# Format time breakdown as human-readable
jq '
  def format_time(s): "\(s / 3600 | floor)h \((s % 3600) / 60 | floor)m";
  {
    session: ((.requests | last.timestamp) - (.requests | first.timestamp)) / 1000,
    user_wait: ([.requests[].timeSpentWaiting // 0] | add) / 1000,
    agent_work: ([.requests[].result.timings.totalElapsed // 0] | add) / 1000
  }
  | {
    session_duration: format_time(.session),
    user_wait_time: format_time(.user_wait),
    agent_work_time: format_time(.agent_work)
  }
' "$CHAT_FILE"
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

### 11b. Rejection Analysis

Rejections include cancelled requests, failed requests, and cancelled/rejected tool invocations.

```bash
# Rejections grouped by agent
jq '
  def extract_agent:
    if .message.text then
      (.message.text | capture("@(?<agent>[a-zA-Z-]+)") | .agent) // "default"
    elif .message | type == "string" then
      (.message | capture("@(?<agent>[a-zA-Z-]+)") | .agent) // "default"
    else
      "default"
    end;
  [.requests[] | {
    agent: extract_agent,
    model: .modelId,
    state: .modelState.value,
    error_code: .result.errorDetails.code,
    cancelled_tools: ([.response[] | select(.kind == "toolInvocationSerialized" and .isConfirmed.type == 0)] | length)
  }]
  | group_by(.agent)
  | map({
      agent: .[0].agent,
      total_requests: length,
      cancelled: ([.[] | select(.state == 2)] | length),
      failed: ([.[] | select(.state == 3)] | length),
      tool_rejections: ([.[].cancelled_tools] | add),
      error_codes: ([.[] | select(.error_code != null) | .error_code] | group_by(.) | map({code: .[0], count: length}))
    })
  | map(. + {rejection_rate: (if .total_requests > 0 then (((.cancelled + .failed + .tool_rejections) / .total_requests) * 100 | floor) else 0 end)})
' "$CHAT_FILE"

# Rejections grouped by model
jq '
  [.requests[] | {
    model: .modelId,
    state: .modelState.value,
    error_code: .result.errorDetails.code,
    cancelled_tools: ([.response[] | select(.kind == "toolInvocationSerialized" and .isConfirmed.type == 0)] | length)
  }]
  | group_by(.model)
  | map({
      model: .[0].model,
      total_requests: length,
      cancelled: ([.[] | select(.state == 2)] | length),
      failed: ([.[] | select(.state == 3)] | length),
      tool_rejections: ([.[].cancelled_tools] | add),
      error_codes: ([.[] | select(.error_code != null) | .error_code] | group_by(.) | map({code: .[0], count: length}))
    })
  | map(. + {rejection_rate: (if .total_requests > 0 then (((.cancelled + .failed + .tool_rejections) / .total_requests) * 100 | floor) else 0 end)})
  | sort_by(-.total_requests)
' "$CHAT_FILE"

# Common rejection reasons (error codes across all requests)
jq '
  [.requests[] | select(.result.errorDetails != null) | {
    code: .result.errorDetails.code,
    message: .result.errorDetails.message
  }]
  | group_by(.code)
  | map({code: .[0].code, count: length, sample_message: .[0].message})
  | sort_by(-.count)
' "$CHAT_FILE"

# User vote-down reasons (explicit rejection feedback)
jq '
  [.requests[] | select(.voteDownReason != null) | .voteDownReason]
  | group_by(.)
  | map({reason: .[0], count: length})
  | sort_by(-.count)
' "$CHAT_FILE"
```

### 12. Extract Agent from Message

Custom agents are invoked with `@agent-name` in the message text. Extract agent names:

```bash
# Extract agent name from each request (looks for @agent-name pattern)
# Falls back to "default" if no agent mention found
jq '
  def extract_agent:
    if .message.text then
      (.message.text | capture("@(?<agent>[a-zA-Z-]+)") | .agent) // "default"
    elif .message | type == "string" then
      (.message | capture("@(?<agent>[a-zA-Z-]+)") | .agent) // "default"
    else
      "default"
    end;
  [.requests[] | {agent: extract_agent, model: .modelId}]
' "$CHAT_FILE"
```

### 13. Model Usage by Agent

```bash
# Models used grouped by agent
jq '
  def extract_agent:
    if .message.text then
      (.message.text | capture("@(?<agent>[a-zA-Z-]+)") | .agent) // "default"
    elif .message | type == "string" then
      (.message | capture("@(?<agent>[a-zA-Z-]+)") | .agent) // "default"
    else
      "default"
    end;
  [.requests[] | {agent: extract_agent, model: .modelId}]
  | group_by(.agent)
  | map({
      agent: .[0].agent,
      models: (group_by(.model) | map({model: .[0].model, count: length})),
      total: length
    })
  | sort_by(-.total)
' "$CHAT_FILE"
```

### 14. Request Count by Agent and Model

```bash
# Request count grouped by agent, then by model
jq '
  def extract_agent:
    if .message.text then
      (.message.text | capture("@(?<agent>[a-zA-Z-]+)") | .agent) // "default"
    elif .message | type == "string" then
      (.message | capture("@(?<agent>[a-zA-Z-]+)") | .agent) // "default"
    else
      "default"
    end;
  [.requests[] | {agent: extract_agent, model: .modelId}]
  | group_by(.agent)
  | map({
      agent: .[0].agent,
      by_model: (group_by(.model) | map({model: .[0].model, count: length}) | sort_by(-.count)),
      total_requests: length
    })
  | sort_by(-.total_requests)
' "$CHAT_FILE"
```

### 15. Automation Effectiveness by Agent

```bash
# Manual approvals grouped by agent
jq '
  def extract_agent:
    if .message.text then
      (.message.text | capture("@(?<agent>[a-zA-Z-]+)") | .agent) // "default"
    elif .message | type == "string" then
      (.message | capture("@(?<agent>[a-zA-Z-]+)") | .agent) // "default"
    else
      "default"
    end;
  [.requests[] | {
    agent: extract_agent,
    tools: [.response[] | select(.kind == "toolInvocationSerialized") | {
      tool: .toolId,
      approval_type: .isConfirmed.type
    }]
  }]
  | map(select(.tools | length > 0))
  | group_by(.agent)
  | map({
      agent: .[0].agent,
      total_tools: ([.[].tools[]] | length),
      auto_approved: ([.[].tools[] | select(.approval_type == 1 or .approval_type == 3)] | length),
      manual_approved: ([.[].tools[] | select(.approval_type == 4)] | length),
      pending_cancelled: ([.[].tools[] | select(.approval_type == 0)] | length),
      automation_rate: (
        ([.[].tools[] | select(.approval_type == 1 or .approval_type == 3)] | length) as $auto |
        ([.[].tools[]] | length) as $total |
        if $total > 0 then (($auto / $total) * 100 | floor) else 0 end
      )
    })
  | sort_by(-.total_tools)
' "$CHAT_FILE"
```

### 16. Tools Used by Agent

```bash
# Tool usage breakdown by agent
jq '
  def extract_agent:
    if .message.text then
      (.message.text | capture("@(?<agent>[a-zA-Z-]+)") | .agent) // "default"
    elif .message | type == "string" then
      (.message | capture("@(?<agent>[a-zA-Z-]+)") | .agent) // "default"
    else
      "default"
    end;
  [.requests[] | {
    agent: extract_agent,
    tools: [.response[] | select(.kind == "toolInvocationSerialized") | .toolId]
  }]
  | group_by(.agent)
  | map({
      agent: .[0].agent,
      tools: ([.[].tools[]] | group_by(.) | map({tool: .[0], count: length}) | sort_by(-.count))
    })
' "$CHAT_FILE"
```

### 17. Terminal Commands Analysis (Automation Opportunities)

```bash
# Extract terminal commands for automation analysis
jq '
  def extract_agent:
    if .message.text then
      (.message.text | capture("@(?<agent>[a-zA-Z-]+)") | .agent) // "default"
    elif .message | type == "string" then
      (.message | capture("@(?<agent>[a-zA-Z-]+)") | .agent) // "default"
    else
      "default"
    end;
  [.requests[] | {
    agent: extract_agent,
    terminal_cmds: [.response[]
      | select(.kind == "toolInvocationSerialized" and .toolId == "run_in_terminal")
      | {
          approval: (if .isConfirmed.type == 4 then "manual" elif .isConfirmed.type == 0 then "cancelled" else "auto" end),
          command: (.invocationMessage // .pastTenseMessage // "unknown" | tostring | gsub("^[^`]*`"; "") | gsub("`[^`]*$"; "") | split("\n")[0][0:100])
        }
    ]
  }]
  | map(select(.terminal_cmds | length > 0))
  | group_by(.agent)
  | map({
      agent: .[0].agent,
      commands: [.[].terminal_cmds[]],
      manual_count: ([.[].terminal_cmds[] | select(.approval == "manual")] | length),
      auto_count: ([.[].terminal_cmds[] | select(.approval == "auto")] | length)
    })
' "$CHAT_FILE"

# Identify repeated command patterns (candidates for scripts)
jq '
  [.requests[].response[]
    | select(.kind == "toolInvocationSerialized" and .toolId == "run_in_terminal")
    | (.invocationMessage // "" | tostring | gsub("^[^`]*`"; "") | gsub("`[^`]*$"; "") | split("\n")[0] | split(" ")[0:2] | join(" "))
  ]
  | group_by(.)
  | map({pattern: .[0], count: length})
  | sort_by(-.count)
  | .[0:10]
' "$CHAT_FILE"
```

### 18. Model Performance by Agent

```bash
# Response time statistics grouped by model
jq '
  [.requests[] | select(.result.timings.totalElapsed != null) | {
    model: .modelId,
    elapsed: .result.timings.totalElapsed,
    first_progress: (.result.timings.firstProgress // 0)
  }]
  | group_by(.model)
  | map({
      model: .[0].model,
      count: length,
      avg_elapsed_sec: (([.[].elapsed] | add) / length / 1000 | . * 100 | floor / 100),
      avg_first_progress_ms: (([.[].first_progress] | add) / length | floor),
      total_elapsed_sec: (([.[].elapsed] | add) / 1000 | floor)
    })
  | sort_by(-.count)
' "$CHAT_FILE"

# Model effectiveness: cancelled/failed rate by model
jq '
  [.requests[] | {model: .modelId, state: .modelState.value}]
  | group_by(.model)
  | map({
      model: .[0].model,
      total: length,
      complete: ([.[] | select(.state == 1)] | length),
      cancelled: ([.[] | select(.state == 2)] | length),
      failed: ([.[] | select(.state == 3)] | length),
      success_rate: (
        ([.[] | select(.state == 1)] | length) as $ok |
        (length) as $total |
        if $total > 0 then (($ok / $total) * 100 | floor) else 0 end
      )
    })
  | sort_by(-.total)
' "$CHAT_FILE"
```

### 19. Generate Agent-Grouped Summary Report
After running the extraction queries, compile results into comprehensive sections:

```markdown
## Session Overview

### Time Breakdown
| Metric | Duration | % of Session |
|--------|----------|--------------|
| **Session Duration** | Xh Ym | 100% |
| User Wait Time | Xh Ym | X% |
| Agent Work Time | Xh Ym | X% |

- **Start:** YYYY-MM-DD HH:MM
- **End:** YYYY-MM-DD HH:MM
- **Total Requests:** N

## Agent Analysis

### Model Usage by Agent
| Agent | Model | Requests | % of Agent |
|-------|-------|----------|------------|
| developer | copilot/gpt-5.1-codex-max | N | X% |
| ... | ... | ... | ... |

### Automation Effectiveness by Agent
| Agent | Total Tools | Auto | Manual | Cancelled | Automation Rate |
|-------|-------------|------|--------|-----------|-----------------|
| developer | N | N | N | N | X% |
| ... | ... | ... | ... | ... | ... |

### Tool Usage by Agent
| Agent | Top Tools |
|-------|-----------|
| developer | readFile (N), run_in_terminal (N), applyPatch (N) |
| ... | ... |

## Rejection Analysis

### Rejections by Agent
| Agent | Total | Cancelled | Failed | Tool Rejections | Rejection Rate |
|-------|-------|-----------|--------|-----------------|----------------|
| developer | N | N | N | N | X% |
| ... | ... | ... | ... | ... | ... |

### Rejections by Model
| Model | Total | Cancelled | Failed | Tool Rejections | Rejection Rate |
|-------|-------|-----------|--------|-----------------|----------------|
| gpt-5.1-codex-max | N | N | N | N | X% |
| ... | ... | ... | ... | ... | ... |

### Common Rejection Reasons
| Error Code | Count | Sample Message |
|------------|-------|----------------|
| rateLimited | N | ... |
| canceled | N | User cancelled |
| ... | ... | ... |

### User Vote-Down Reasons
| Reason | Count |
|--------|-------|
| incorrectCode | N |
| didNotFollowInstructions | N |
| ... | ... |

## Automation Opportunities
Based on terminal command analysis:

| Pattern | Count | Current Approval | Recommendation |
|---------|-------|------------------|----------------|
| `dotnet test` | N | Auto | ✅ Already automated |
| `git commit` | N | Manual | Consider: `scripts/commit.sh` wrapper |
| `gh pr create` | N | Manual | Use: `scripts/pr-github.sh` |

## Model Performance

### Response Time by Model
| Model | Requests | Avg Response (s) | Success Rate | Recommended For |
|-------|----------|------------------|--------------|-----------------|
| gpt-5.1-codex-max | N | X.Xs | X% | Coding tasks |
| gemini-3-pro | N | X.Xs | X% | Planning, docs |
| ... | ... | ... | ... | ... |

### Model Effectiveness Assessment
| Agent | Assigned Model | Actual Usage | Assessment |
|-------|----------------|--------------|------------|
| Developer | gpt-5.1-codex-max | 100% match | ✅ Correct |
| Task Planner | gemini-3-pro | 80% match | ⚠️ Some model switching |
| ... | ... | ... | ... |
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
