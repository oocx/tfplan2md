import json
import re
import sys
from datetime import datetime

# Retrospective Analysis Tool
# Suggested Improvements for Workflow Engineer:
# 1. Add "Detail-Slip" detection: Count repeated edits to the same file within a single agent's turn.
# 2. Add "Approval Latency" calculation: Measure time between agent handoff and user approval.
# 3. Add "Tool Failure" analysis: Parse tool results to identify flaky commands or permission issues.
# 4. Add "Context Bloat" detection: Monitor the size of attachments/context over time.
# 5. Export results to Markdown: Generate the "Session Overview" and "Agent Analysis" tables directly.

def analyze_chat(file_path):
    try:
        with open(file_path, 'r') as f:
            data = json.load(f)
    except Exception as e:
        print(f"Error reading {file_path}: {e}")
        return

    requests = data.get('requests', [])
    
    metrics = {
        'total_requests': len(requests),
        'agents': {},
        'models': {},
        'tools': {},
        'rejections': {
            'cancelled': 0,
            'failed': 0,
            'tool_rejections': 0
        },
        'agent_work_time': 0
    }

    current_agent = "Unknown"
    
    for i, req in enumerate(requests):
        # Timestamps and Work Time
        result = req.get('result', {})
        elapsed = 0
        if result:
            timings = result.get('timings', {})
            elapsed = timings.get('totalElapsed', 0)
            metrics['agent_work_time'] += elapsed
        
        # Model and Agent
        model_id = req.get('modelId', 'Unknown')
        metrics['models'][model_id] = metrics['models'].get(model_id, 0) + 1
        
        # Extract all text from response to identify agent role
        thinking_text = ""
        response_text = ""
        for resp in req.get('response', []):
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
        
        # Look for common agent roles
        roles = ['Requirements Engineer', 'Architect', 'Task Planner', 'Developer', 'Quality Engineer', 'UAT Tester', 'Release Manager', 'Code Reviewer', 'Workflow Engineer', 'Retrospective']
        found_role = False
        
        # Check message for @ mentions
        message_obj = req.get('message', {})
        message_text = ""
        if isinstance(message_obj, dict):
            message_text = message_obj.get('text', '')
        elif isinstance(message_obj, str):
            message_text = message_obj
        
        for role in roles:
            role_slug = role.lower().replace(' ', '-')
            if f"@{role_slug}" in message_text.lower():
                current_agent = role
                found_role = True
                break
        
        if not found_role:
            combined_text = thinking_text + response_text
            for role in roles:
                patterns = [
                    f"**{role}** agent",
                    f"I'm the **{role}**",
                    f"I am the **{role}**",
                    f"role: **{role}**",
                    f"as the **{role}**",
                    f"switch to the **{role}**",
                    f"hand off to **{role}**",
                    f"hand off to the **{role}**",
                    f"handoff to **{role}**",
                    f"you can now switch to the **{role}**",
                    f"switch to the **{role}** agent"
                ]
                for pattern in patterns:
                    if pattern.lower() in combined_text.lower():
                        current_agent = role
                        found_role = True
                        break
                if found_role:
                    break
        
        if current_agent not in metrics['agents']:
            metrics['agents'][current_agent] = {
                'requests': 0,
                'models': {},
                'tools': {},
                'rejections': {'cancelled': 0, 'failed': 0},
                'work_time': 0
            }
        
        metrics['agents'][current_agent]['requests'] += 1
        metrics['agents'][current_agent]['models'][model_id] = metrics['agents'][current_agent]['models'].get(model_id, 0) + 1
        metrics['agents'][current_agent]['work_time'] += elapsed
        
        # Tools
        for resp in req.get('response', []):
            kind = resp.get('kind')
            if kind == 'toolInvocationSerialized':
                metrics['tools']['tool'] = metrics['tools'].get('tool', 0) + 1
                metrics['agents'][current_agent]['tools']['tool'] = metrics['agents'][current_agent]['tools'].get('tool', 0) + 1
            elif kind == 'textEditGroup':
                metrics['tools']['edit'] = metrics['tools'].get('edit', 0) + 1
                metrics['agents'][current_agent]['tools']['edit'] = metrics['agents'][current_agent]['tools'].get('edit', 0) + 1

        # Rejections
        if result:
            error_details = result.get('errorDetails')
            if error_details:
                code = error_details.get('code', 'unknown')
                if code == 'failed':
                    metrics['rejections']['failed'] += 1
                    metrics['agents'][current_agent]['rejections']['failed'] += 1
                else:
                    metrics['rejections']['cancelled'] += 1
                    metrics['agents'][current_agent]['rejections']['cancelled'] += 1

    # Output summary
    print(f"Total Requests: {metrics['total_requests']}")
    print(f"Total Agent Work Time: {metrics['agent_work_time'] / 1000:.2f}s")
    print("\nAgents:")
    for agent, data in metrics['agents'].items():
        print(f"  {agent}: {data['requests']} requests")
        print(f"    Work Time: {data['work_time'] / 1000:.2f}s")
        print(f"    Models: {data['models']}")
        print(f"    Tools: {data['tools']}")
        print(f"    Rejections: {data['rejections']}")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python3 analyze-chat.py <path_to_chat.json>")
    else:
        analyze_chat(sys.argv[1])
