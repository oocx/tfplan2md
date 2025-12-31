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
        'agent_work_time': 0,
        'start_timestamp': requests[0].get('timestamp', 0) if requests else 0,
        'end_timestamp': requests[-1].get('timestamp', 0) if requests else 0
    }

    current_agent = "Requirements Engineer" # Default starting agent
    
    # Handoff prompt mapping
    handoff_prompts = {
        "Architect": "Review the feature specification created above and design the technical solution",
        "Quality Engineer": "Review the architecture decisions above and define the test plan",
        "Task Planner": "Review the test plan above and create actionable user stories",
        "Developer": [
            "Review the user stories above and begin implementation",
            "Review the issue analysis above and implement the fix",
            "Address the issues identified in the code review above",
            "The PR build validation or release pipeline failed",
            "User Acceptance Testing revealed rendering issues"
        ],
        "Code Reviewer": "Review the implementation and documentation updates for quality and completeness",
        "UAT Tester": "The code review is approved. Run UAT to validate markdown rendering",
        "Release Manager": [
            "The code review is approved and this change does not require UAT",
            "User Acceptance Testing passed on both GitHub and Azure DevOps",
            "The website changes are complete. Please create a PR"
        ],
        "Retrospective": "The release is complete. Please conduct a retrospective",
        "Workflow Engineer": "I have identified some workflow improvements in the retrospective",
        "Issue Analyst": "I have an issue with the project"
    }

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
        roles = ['Requirements Engineer', 'Architect', 'Task Planner', 'Developer', 'Quality Engineer', 'UAT Tester', 'Release Manager', 'Code Reviewer', 'Workflow Engineer', 'Retrospective', 'Issue Analyst']
        found_role = False
        
        # Check message for handoff prompts (User Request)
        message_obj = req.get('message', {})
        message_text = ""
        if isinstance(message_obj, dict):
            message_text = message_obj.get('text', '')
        elif isinstance(message_obj, str):
            message_text = message_obj
        
        # Extract userRequest from message_text if it's a complex prompt
        user_request_match = re.search(r'<userRequest>\n(.*?)\n</userRequest>', message_text, re.DOTALL)
        if user_request_match:
            user_request = user_request_match.group(1).strip()
        else:
            user_request = message_text.strip()

        for agent, prompts in handoff_prompts.items():
            if isinstance(prompts, str):
                prompts = [prompts]
            for prompt in prompts:
                # Simple similarity: check if prompt is a substring or if most words match
                if prompt.lower() in user_request.lower():
                    current_agent = agent
                    found_role = True
                    break
                
                # Word-based similarity for modified prompts
                prompt_words = set(re.findall(r'\w+', prompt.lower()))
                request_words = set(re.findall(r'\w+', user_request.lower()))
                if len(prompt_words) >= 8: # Only for long enough prompts
                    intersection = prompt_words.intersection(request_words)
                    if len(intersection) / len(prompt_words) >= 0.8:
                        current_agent = agent
                        found_role = True
                        break
            if found_role: break

        if not found_role:
            # Check for @ mentions in message
            for role in roles:
                role_slug = role.lower().replace(' ', '-')
                if f"@{role_slug}" in message_text.lower():
                    current_agent = role
                    found_role = True
                    break
        
        if not found_role:
            # Check thinking block for mode mentions
            combined_text = thinking_text + response_text
            for role in roles:
                patterns = [
                    f"I'm in \"{role}\" mode",
                    f"I am in \"{role}\" mode",
                    f"I'm in {role} mode",
                    f"I am in {role} mode",
                    f"switch to {role} mode",
                    f"switching to {role} mode",
                    f"as the {role} agent",
                    f"acting as the {role}"
                ]
                for pattern in patterns:
                    if pattern.lower() in combined_text.lower():
                        current_agent = role
                        found_role = True
                        break
                if found_role: break
        
        # Fallback: Use model ID as a hint if we are still "Unknown" or if the model strongly suggests a role
        if not found_role:
            if model_id == 'copilot/gpt-5.1-codex-max' and current_agent not in ['Developer', 'Task Planner']:
                current_agent = 'Developer'
            # Removed Claude Opus fallback as it's used by both Architect and Code Reviewer
            elif model_id == 'copilot/gemini-3-flash-preview' and current_agent not in ['UAT Tester', 'Retrospective', 'Quality Engineer', 'Task Planner', 'Release Manager']:
                # Gemini is used by many agents, so only fallback if we are really lost
                pass

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
    
    session_duration_ms = metrics['end_timestamp'] - metrics['start_timestamp']
    session_duration_s = session_duration_ms / 1000
    agent_work_time_s = metrics['agent_work_time'] / 1000
    user_wait_time_s = max(0, session_duration_s - agent_work_time_s)
    
    print(f"Session Duration: {session_duration_s:.2f}s ({session_duration_s/3600:.2f}h)")
    print(f"Agent Work Time: {agent_work_time_s:.2f}s ({agent_work_time_s/3600:.2f}h)")
    print(f"User Wait Time: {user_wait_time_s:.2f}s ({user_wait_time_s/3600:.2f}h)")
    
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
