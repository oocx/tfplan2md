#!/usr/bin/env bash
# extract-metrics.sh - Extract metrics from VS Code chat export JSON
#
# Usage: ./extract-metrics.sh <chat.json> [output-base]
#
# If output-base is specified, generates:
#   - <output-base>.md   - Human-readable markdown report
#   - <output-base>.json - Raw metrics data for cross-feature analysis
# If output-base is not specified, outputs markdown to stdout.

set -euo pipefail

if [[ $# -lt 1 ]]; then
    echo "Usage: $0 <chat.json> [output-base]" >&2
    echo "  chat.json    - Path to VS Code chat export file" >&2
    echo "  output-base  - Optional: base name for output files (generates .md and .json)" >&2
    echo "" >&2
    echo "Examples:" >&2
    echo "  $0 chat.json                    # Output markdown to stdout" >&2
    echo "  $0 chat.json analysis           # Creates analysis.md and analysis.json" >&2
    echo "  $0 chat.json results/metrics    # Creates results/metrics.md and results/metrics.json" >&2
    exit 1
fi

CHAT_FILE="$1"
OUTPUT_BASE="${2:-}"

if [[ ! -f "$CHAT_FILE" ]]; then
    echo "Error: File not found: $CHAT_FILE" >&2
    exit 1
fi

# Verify it's valid JSON with expected structure
if ! jq -e '.requests' "$CHAT_FILE" > /dev/null 2>&1; then
    echo "Error: Invalid chat export format (missing .requests array)" >&2
    exit 1
fi

# --- Extraction Functions ---

extract_session_metrics() {
    jq -r '
    {
        total_requests: (.requests | length),
        start_timestamp: (.requests | first.timestamp),
        end_timestamp: (.requests | last.timestamp),
        session_duration_sec: (((.requests | last.timestamp) - (.requests | first.timestamp)) / 1000),
        user_wait_time_sec: ([.requests[].timeSpentWaiting // 0] | add / 1000),
        agent_work_time_sec: ([.requests[].result.timings.totalElapsed // 0] | add / 1000)
    }
    ' "$CHAT_FILE"
}

extract_model_usage() {
    jq -r '
    [.requests[].modelId // "unknown"] 
    | group_by(.) 
    | map({model: (.[0] // "unknown" | if type == "string" then sub("^copilot/"; "") else "unknown" end), count: length}) 
    | sort_by(-.count)
    ' "$CHAT_FILE"
}

extract_tool_usage() {
    jq -r '
    [.requests[].response[] | select(.kind == "toolInvocationSerialized") | .toolId]
    | group_by(.)
    | map({tool: (.[0] | sub("^copilot_"; "")), count: length})
    | sort_by(-.count)
    | .[:15]
    ' "$CHAT_FILE"
}

extract_approval_types() {
    jq -r '
    [.requests[].response[] | select(.kind == "toolInvocationSerialized") | .isConfirmed.type]
    | group_by(.)
    | map({type: .[0], count: length})
    | sort_by(-.count)
    ' "$CHAT_FILE"
}

extract_model_success_rates() {
    jq -r '
    .requests
    | group_by(.modelId)
    | map({
        model: (.[0].modelId // "unknown" | if type == "string" then sub("^copilot/"; "") else "unknown" end),
        total: length,
        complete: [.[] | select(.result.errorDetails == null)] | length,
        failed: [.[] | select(.result.errorDetails != null)] | length
    })
    | sort_by(-.total)
    ' "$CHAT_FILE"
}

extract_response_times() {
    jq -r '
    [.requests[] | select(.result.timings.totalElapsed != null) | {model: .modelId, elapsed: .result.timings.totalElapsed}]
    | group_by(.model)
    | map({
        model: (.[0].model // "unknown" | if type == "string" then sub("^copilot/"; "") else "unknown" end),
        count: length,
        avg_sec: (([.[].elapsed] | add) / length / 1000 | floor),
        total_sec: (([.[].elapsed] | add) / 1000 | floor)
    })
    | sort_by(-.avg_sec)
    ' "$CHAT_FILE"
}

extract_error_codes() {
    jq -r '
    [.requests[] | select(.result.errorDetails != null) | .result.errorDetails.message | split("\n")[0]]
    | group_by(.)
    | map({message: .[0], count: length})
    | sort_by(-.count)
    ' "$CHAT_FILE"
}

extract_user_votes() {
    jq -r '
    [.requests[] | select(.vote != null) | .vote]
    | group_by(.)
    | map({vote: (if .[0] == 1 then "up" elif .[0] == 0 then "down" else "unknown" end), count: length})
    ' "$CHAT_FILE"
}

# --- Generate Markdown Report ---

generate_report() {
    local session_metrics model_usage tool_usage approval_types
    local model_success response_times error_codes user_votes
    
    # Extract all metrics
    session_metrics=$(extract_session_metrics)
    model_usage=$(extract_model_usage)
    tool_usage=$(extract_tool_usage)
    approval_types=$(extract_approval_types)
    model_success=$(extract_model_success_rates)
    response_times=$(extract_response_times)
    error_codes=$(extract_error_codes)
    user_votes=$(extract_user_votes)
    
    # Parse session metrics
    local total_requests session_sec user_wait_sec agent_work_sec
    total_requests=$(echo "$session_metrics" | jq -r '.total_requests')
    session_sec=$(echo "$session_metrics" | jq -r '.session_duration_sec | floor')
    user_wait_sec=$(echo "$session_metrics" | jq -r '.user_wait_time_sec | floor')
    agent_work_sec=$(echo "$session_metrics" | jq -r '.agent_work_time_sec | floor')
    
    # Format durations
    format_duration() {
        local sec=$1
        local hours=$((sec / 3600))
        local mins=$(((sec % 3600) / 60))
        if [[ $hours -gt 0 ]]; then
            echo "${hours}h ${mins}m"
        else
            echo "${mins}m"
        fi
    }
    
    local session_dur user_wait_dur agent_work_dur
    session_dur=$(format_duration "$session_sec")
    user_wait_dur=$(format_duration "$user_wait_sec")
    agent_work_dur=$(format_duration "$agent_work_sec")
    
    # Calculate totals for approval types
    local total_approvals auto_approved manual_approved
    total_approvals=$(echo "$approval_types" | jq '[.[].count] | add // 0')
    auto_approved=$(echo "$approval_types" | jq '[.[] | select(.type == 1 or .type == 3) | .count] | add // 0')
    manual_approved=$(echo "$approval_types" | jq '[.[] | select(.type == 4) | .count] | add // 0')
    
    # Calculate automation rate
    local automation_rate=0
    if [[ $total_approvals -gt 0 ]]; then
        automation_rate=$(echo "scale=1; $auto_approved * 100 / $total_approvals" | bc)
    fi
    
    cat << EOF
# Chat Export Analysis Results

**Source:** \`$(basename "$CHAT_FILE")\`  
**Analysis Date:** $(date +%Y-%m-%d)  
**Generated by:** extract-metrics.sh

---

## Session Overview

| Metric | Value |
|--------|-------|
| Total Requests | $total_requests |
| Session Duration | $session_dur |
| User Wait Time | $user_wait_dur (cumulative) |
| Agent Work Time | $agent_work_dur (cumulative) |

---

## Model Usage

| Model | Requests | % |
|-------|----------|---|
$(echo "$model_usage" | jq -r --argjson total "$total_requests" '.[] | "| \(.model) | \(.count) | \(.count * 100 / $total | floor)% |"')

---

## Tool Usage (Top 15)

| Tool | Count |
|------|-------|
$(echo "$tool_usage" | jq -r '.[] | "| \(.tool) | \(.count) |"')

---

## Automation Effectiveness

| Type | Count | Description |
|------|-------|-------------|
$(echo "$approval_types" | jq -r '.[] | "| \(.type) | \(.count) | \(if .type == 1 then "Auto-approved" elif .type == 3 then "Profile-scoped" elif .type == 4 then "Manual" elif .type == 0 then "Pending/cancelled" else "Unknown" end) |"')

**Total Tool Invocations:** $total_approvals  
**Auto-approved:** $auto_approved (${automation_rate}%)  
**Manual:** $manual_approved

---

## Model Success Rates

| Model | Total | Complete | Failed | Success Rate |
|-------|-------|----------|--------|--------------|
$(echo "$model_success" | jq -r '.[] | "| \(.model) | \(.total) | \(.complete) | \(.failed) | \(if .total > 0 then (.complete * 100 / .total | floor) else 0 end)% |"')

---

## Response Times by Model

| Model | Requests | Avg (s) | Total (s) |
|-------|----------|---------|-----------|
$(echo "$response_times" | jq -r '.[] | "| \(.model) | \(.count) | \(.avg_sec) | \(.total_sec) |"')

---

## Errors

$(if [[ $(echo "$error_codes" | jq 'length') -eq 0 ]]; then
    echo "No errors recorded."
else
    echo "| Message | Count |"
    echo "|---------|-------|"
    echo "$error_codes" | jq -r '.[] | "| \(.message) | \(.count) |"'
fi)

---

## User Feedback

$(if [[ $(echo "$user_votes" | jq 'length') -eq 0 ]]; then
    echo "No user votes recorded."
else
    echo "| Vote | Count |"
    echo "|------|-------|"
    echo "$user_votes" | jq -r '.[] | "| \(.vote) | \(.count) |"'
fi)
EOF
}

# --- Generate JSON Data ---

generate_json() {
    local session_metrics model_usage tool_usage approval_types
    local model_success response_times error_codes user_votes
    
    # Extract all metrics
    session_metrics=$(extract_session_metrics)
    model_usage=$(extract_model_usage)
    tool_usage=$(extract_tool_usage)
    approval_types=$(extract_approval_types)
    model_success=$(extract_model_success_rates)
    response_times=$(extract_response_times)
    error_codes=$(extract_error_codes)
    user_votes=$(extract_user_votes)
    
    # Calculate derived metrics
    local total_approvals auto_approved manual_approved
    total_approvals=$(echo "$approval_types" | jq '[.[].count] | add // 0')
    auto_approved=$(echo "$approval_types" | jq '[.[] | select(.type == 1 or .type == 3) | .count] | add // 0')
    manual_approved=$(echo "$approval_types" | jq '[.[] | select(.type == 4) | .count] | add // 0')
    
    # Build complete JSON object
    jq -n \
        --arg source "$(basename "$CHAT_FILE")" \
        --arg date "$(date +%Y-%m-%d)" \
        --arg generator "extract-metrics.sh" \
        --argjson session "$session_metrics" \
        --argjson model_usage "$model_usage" \
        --argjson tool_usage "$tool_usage" \
        --argjson approval_types "$approval_types" \
        --argjson model_success "$model_success" \
        --argjson response_times "$response_times" \
        --argjson error_codes "$error_codes" \
        --argjson user_votes "$user_votes" \
        --argjson total_tool_invocations "$total_approvals" \
        --argjson auto_approved "$auto_approved" \
        --argjson manual_approved "$manual_approved" \
        '{
            metadata: {
                source: $source,
                analysis_date: $date,
                generator: $generator
            },
            session: $session,
            model_usage: $model_usage,
            tool_usage: $tool_usage,
            automation: {
                approval_types: $approval_types,
                total_invocations: $total_tool_invocations,
                auto_approved: $auto_approved,
                manual_approved: $manual_approved,
                automation_rate_pct: (if $total_tool_invocations > 0 then ($auto_approved * 100 / $total_tool_invocations | floor) else 0 end)
            },
            model_success: $model_success,
            response_times: $response_times,
            errors: $error_codes,
            user_feedback: $user_votes
        }'
}

# --- Main ---

if [[ -n "$OUTPUT_BASE" ]]; then
    OUTPUT_MD="${OUTPUT_BASE}.md"
    OUTPUT_JSON="${OUTPUT_BASE}.json"
    
    generate_report > "$OUTPUT_MD"
    generate_json > "$OUTPUT_JSON"
    
    echo "Report written to: $OUTPUT_MD"
    echo "Raw data written to: $OUTPUT_JSON"
else
    generate_report
fi
