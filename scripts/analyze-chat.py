#!/usr/bin/env python3

import json
import re
import sys

# Retrospective Analysis Tool
# Suggested Improvements for Workflow Engineer:
# 1. Add "Detail-Slip" detection: Count repeated edits to the same file within a single agent's turn.
# 2. Add "Approval Latency" calculation: Measure time between agent handoff and user approval.
# 3. Add "Tool Failure" analysis: Parse tool results to identify flaky commands or permission issues.
# 4. Add "Context Bloat" detection: Monitor the size of attachments/context over time.
# 5. Export results to Markdown: Generate the "Session Overview" and "Agent Analysis" tables directly.

def _get_message_text(req: dict) -> str:
    message_obj = req.get('message', {})
    if isinstance(message_obj, dict):
        value = message_obj.get('text', '')
        return value if isinstance(value, str) else ''
    if isinstance(message_obj, str):
        return message_obj
    return ''


def _extract_user_request(message_text: str) -> str:
    user_request_match = re.search(r'<userRequest>\n(.*?)\n</userRequest>', message_text, re.DOTALL)
    if user_request_match:
        return user_request_match.group(1).strip()
    return message_text.strip()

def _is_retro_feedback(user_request: str) -> bool:
    # Captures feedback provided during retrospectives and any chat message that explicitly
    # references the retrospective (e.g. "note for retro: ...").
    return re.search(r'\b(retro|retrospective)\b', user_request, re.IGNORECASE) is not None


def analyze_chat(file_path: str) -> int:
    try:
        with open(file_path, 'r') as f:
            data = json.load(f)
    except Exception as e:
        print(f"Error reading {file_path}: {e}")
        return 2

    requests = data.get('requests', [])
    
    metrics = {
        'total_requests': len(requests),
        'agents': {},
        'models': {},
        'tools': {},
        'file_edits': {'kept': 0, 'undone': 0, 'modified': 0},
        'votes': {'up': 0, 'down': 0},
        'vote_down_reasons': {},
        'retro_feedback': [],
        'warnings': [],
        'rejections': {
            'cancelled': 0,
            'failed': 0,
            'tool_rejections': 0
        },
        'agent_work_time': 0,
        'user_wait_time': 0,
        'start_timestamp': requests[0].get('timestamp', 0) if requests else 0,
        'end_timestamp': requests[-1].get('timestamp', 0) if requests else 0
    }

    missing_timestamp_count = 0
    missing_elapsed_count = 0
    missing_wait_count = 0
    missing_model_count = 0
    missing_message_count = 0
    missing_response_count = 0

    for i, req in enumerate(requests):
        # Timestamps and Work Time
        result = req.get('result', {})
        elapsed = 0
        if result:
            timings = result.get('timings', {})
            elapsed = timings.get('totalElapsed', 0)
            if elapsed:
                metrics['agent_work_time'] += elapsed
            else:
                missing_elapsed_count += 1
        else:
            missing_elapsed_count += 1

        wait = req.get('timeSpentWaiting')
        if isinstance(wait, int):
            metrics['user_wait_time'] += wait
        else:
            missing_wait_count += 1
        
        # Model
        model_id = req.get('modelId')
        if not isinstance(model_id, str) or not model_id:
            model_id = 'Unknown'
            missing_model_count += 1
        metrics['models'][model_id] = metrics['models'].get(model_id, 0) + 1
        
        # Extract all text from response for reporting/plausibility only (no agent attribution).
        thinking_text = ""
        response_text = ""
        response_items = req.get('response')
        if not isinstance(response_items, list):
            response_items = []
            missing_response_count += 1

        for resp in response_items:
            kind = resp.get('kind')
            val = resp.get('value', '')
            text = resp.get('text', '')
            
            content = ""
            if isinstance(val, str):
                content = val
            elif isinstance(val, list):
                for part in val:
                    if isinstance(part, dict) and 'text' in part:
                        content += part['text']
                    elif isinstance(part, str):
                        content += part
            elif isinstance(text, str):
                content = text
            
            if kind == 'thinking':
                thinking_text += content
            else:
                response_text += content
        
        message_text = _get_message_text(req)
        if not message_text:
            missing_message_count += 1
        user_request = _extract_user_request(message_text)

        # NOTE: VS Code chat exports do not reliably include which custom agent was selected.
        # Do not guess. All per-agent metrics must be treated as unavailable.
        current_agent = 'Unattributed'

        if not isinstance(req.get('timestamp'), int):
            missing_timestamp_count += 1

        if _is_retro_feedback(user_request):
            metrics['retro_feedback'].append({
                'index': i,
                'timestamp': req.get('timestamp'),
                'text': user_request,
            })

        if current_agent not in metrics['agents']:
            metrics['agents'][current_agent] = {
                'requests': 0,
                'models': {},
                'tools': {},
                'rejections': {'cancelled': 0, 'failed': 0},
                'work_time': 0,
                'wait_time': 0,
            }
        
        metrics['agents'][current_agent]['requests'] += 1
        metrics['agents'][current_agent]['models'][model_id] = metrics['agents'][current_agent]['models'].get(model_id, 0) + 1
        metrics['agents'][current_agent]['work_time'] += elapsed
        if isinstance(wait, int):
            metrics['agents'][current_agent]['wait_time'] += wait
        
        # Tools
        for resp in response_items:
            kind = resp.get('kind')
            if kind == 'toolInvocationSerialized':
                tool_id = resp.get('toolId', 'unknown-tool')
                metrics['tools'][tool_id] = metrics['tools'].get(tool_id, 0) + 1
                metrics['agents'][current_agent]['tools'][tool_id] = metrics['agents'][current_agent]['tools'].get(tool_id, 0) + 1
            elif kind == 'textEditGroup':
                metrics['tools']['edit'] = metrics['tools'].get('edit', 0) + 1
                metrics['agents'][current_agent]['tools']['edit'] = metrics['agents'][current_agent]['tools'].get('edit', 0) + 1

        # File edit events
        for evt in req.get('editedFileEvents', []) or []:
            event_kind = evt.get('eventKind')
            if event_kind == 1:
                metrics['file_edits']['kept'] += 1
            elif event_kind == 2:
                metrics['file_edits']['undone'] += 1
            elif event_kind == 3:
                metrics['file_edits']['modified'] += 1

        # Votes / Feedback
        vote = req.get('vote')
        if vote == 1:
            metrics['votes']['up'] += 1
        elif vote == 0:
            metrics['votes']['down'] += 1
            reason = req.get('voteDownReason')
            if isinstance(reason, str) and reason:
                metrics['vote_down_reasons'][reason] = metrics['vote_down_reasons'].get(reason, 0) + 1

        # Rejections
        model_state = None
        model_state_obj = req.get('modelState')
        if isinstance(model_state_obj, dict):
            model_state = model_state_obj.get('value')

        if model_state == 3:
            metrics['rejections']['failed'] += 1
            metrics['agents'][current_agent]['rejections']['failed'] += 1
        elif model_state == 2:
            metrics['rejections']['cancelled'] += 1
            metrics['agents'][current_agent]['rejections']['cancelled'] += 1
        elif result:
            error_details = result.get('errorDetails')
            if error_details:
                code = error_details.get('code', 'unknown')
                if code == 'failed':
                    metrics['rejections']['failed'] += 1
                    metrics['agents'][current_agent]['rejections']['failed'] += 1
                else:
                    metrics['rejections']['cancelled'] += 1
                    metrics['agents'][current_agent]['rejections']['cancelled'] += 1

    # Plausibility checks
    if metrics['total_requests'] != sum(a['requests'] for a in metrics['agents'].values()):
        metrics['warnings'].append('Request count mismatch: sum(agents.requests) != total_requests')

    if metrics['total_requests'] != sum(metrics['models'].values()):
        metrics['warnings'].append('Request count mismatch: sum(models) != total_requests')

    if metrics['start_timestamp'] and metrics['end_timestamp'] and metrics['end_timestamp'] < metrics['start_timestamp']:
        metrics['warnings'].append('Timestamps invalid: end_timestamp < start_timestamp')

    session_duration_ms = metrics['end_timestamp'] - metrics['start_timestamp']
    if session_duration_ms < 0:
        session_duration_ms = 0

    if missing_timestamp_count:
        metrics['warnings'].append(f"{missing_timestamp_count} request(s) missing a valid timestamp")
    if missing_elapsed_count:
        metrics['warnings'].append(f"{missing_elapsed_count} request(s) missing totalElapsed timing")
    if missing_wait_count:
        metrics['warnings'].append(f"{missing_wait_count} request(s) missing timeSpentWaiting")
    if missing_model_count:
        metrics['warnings'].append(f"{missing_model_count} request(s) missing modelId")
    if missing_message_count:
        metrics['warnings'].append(f"{missing_message_count} request(s) missing message.text")
    if missing_response_count:
        metrics['warnings'].append(f"{missing_response_count} request(s) missing response[]")

    agent_work_time_ms = metrics['agent_work_time']
    user_wait_time_ms = metrics['user_wait_time']
    if session_duration_ms > 0:
        if agent_work_time_ms > session_duration_ms * 1.2:
            metrics['warnings'].append('Agent work time is unexpectedly larger than session duration')
        if user_wait_time_ms > session_duration_ms * 1.2:
            metrics['warnings'].append('User wait time is unexpectedly larger than session duration')
        other_time_ms = session_duration_ms - agent_work_time_ms - user_wait_time_ms
        if other_time_ms < -int(session_duration_ms * 0.05):
            metrics['warnings'].append('Time breakdown exceeds session duration (agent + user wait > session)')

    # Output summary
    print(f"Total Requests: {metrics['total_requests']}")

    session_duration_s = session_duration_ms / 1000
    agent_work_time_s = metrics['agent_work_time'] / 1000
    user_wait_time_s = metrics['user_wait_time'] / 1000
    other_time_s = max(0.0, session_duration_s - agent_work_time_s - user_wait_time_s)
    
    print(f"Session Duration: {session_duration_s:.2f}s ({session_duration_s/3600:.2f}h)")
    print(f"Agent Work Time: {agent_work_time_s:.2f}s ({agent_work_time_s/3600:.2f}h)")
    print(f"User Wait Time: {user_wait_time_s:.2f}s ({user_wait_time_s/3600:.2f}h)")
    print(f"Other Time (unattributed): {other_time_s:.2f}s ({other_time_s/3600:.2f}h)")

    print(f"\nFile Edits: {metrics['file_edits']}")
    print(f"Votes: {metrics['votes']}")
    if metrics['vote_down_reasons']:
        print(f"Vote-Down Reasons: {metrics['vote_down_reasons']}")

    print("\nAgent Attribution:")
    print("  Custom agent/role attribution is not available in VS Code chat exports; per-agent metrics are not reported.")
    
    print("\nAgents:")
    for agent, data in metrics['agents'].items():
        print(f"  {agent}: {data['requests']} requests")
        print(f"    Work Time: {data['work_time'] / 1000:.2f}s")
        print(f"    Wait Time: {data['wait_time'] / 1000:.2f}s")
        print(f"    Models: {data['models']}")
        print(f"    Tools: {data['tools']}")
        print(f"    Rejections: {data['rejections']}")

    print("\nRetrospective Feedback (verbatim):")
    if metrics['retro_feedback']:
        for item in metrics['retro_feedback']:
            ts = item.get('timestamp')
            ts_display = str(ts) if ts is not None else 'unknown-ts'
            print(f"  - [{item['index']}; {ts_display}] {item['text']}")
    else:
        print("  (none detected)")

    print("\nImprovement Opportunities (derived from feedback):")
    if metrics['retro_feedback']:
        for i, item in enumerate(metrics['retro_feedback'], start=1):
            print(f"  - Feedback #{i}: Investigate and address: {item['text']}")
    else:
        print("  (none)")

    print("\nPlausibility Warnings:")
    if metrics['warnings']:
        for warning in metrics['warnings']:
            print(f"  - {warning}")
    else:
        print("  (none)")

    return 0

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: scripts/analyze-chat.py <path_to_chat.json>")
        sys.exit(2)
    else:
        sys.exit(analyze_chat(sys.argv[1]))
