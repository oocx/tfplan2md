#!/usr/bin/env python3
import os
import re
import sys
from pathlib import Path

# Configuration
AGENTS_DIR = Path(".github/agents")
MODEL_REF_FILE = Path("docs/ai-model-reference.md")

# Regex patterns
FRONTMATTER_PATTERN = re.compile(r"^---\s*\n(.*?)\n---\s*\n", re.DOTALL)
NAME_PATTERN = re.compile(r"^name:\s*(.*)$", re.MULTILINE)
MODEL_PATTERN = re.compile(r"^model:\s*(.*)$", re.MULTILINE)
TOOLS_PATTERN = re.compile(r"^tools:\s*\[(.*)\]$", re.MULTILINE)
HANDOFF_AGENT_PATTERN = re.compile(r"agent:\s*\"(.*?)\"", re.MULTILINE)

# Sections to check (regex patterns)
REQUIRED_SECTIONS = [
    r"## Your Goal",
    r"## Boundaries",
    r"✅\s*(?:\*\*)?Always Do",
    r"⚠️\s*(?:\*\*)?Ask First",
    r"🚫\s*(?:\*\*)?Never Do"
]

def get_valid_models():
    if not MODEL_REF_FILE.exists():
        print(f"Warning: {MODEL_REF_FILE} not found. Skipping model validation.")
        return None
    
    content = MODEL_REF_FILE.read_text()
    # Extract models from tables. Look for columns that look like Copilot Model IDs.
    # They are usually in the second column of the benchmark tables.
    models = set()
    
    # Match table rows: | Name | ID | Score | ... |
    # We want the ID column.
    table_rows = re.findall(r"\|\s*[^|]+\s*\|\s*([^|]+)\s*\|\s*[\d.]+\s*\|", content)
    for row in table_rows:
        model_id = row.strip()
        if model_id and model_id != "Copilot Model ID":
            models.add(model_id)
            
    # Also check the "Available Models" tables which might not have scores
    available_rows = re.findall(r"\|\s*([^|]+)\s*\|\s*(?:GA|Public Preview)\s*\|", content)
    for row in available_rows:
        model_id = row.strip()
        if model_id and model_id != "Model":
            models.add(model_id)
            
    return models

def validate_agents():
    valid_models = get_valid_models()
    agent_files = list(AGENTS_DIR.glob("*.agent.md"))
    
    # First pass: collect all agent names
    agent_names = set()
    agent_data = {}
    
    for agent_file in agent_files:
        content = agent_file.read_text()
        match = FRONTMATTER_PATTERN.search(content)
        if not match:
            print(f"Error: {agent_file.name} has no frontmatter.")
            continue
            
        frontmatter = match.group(1)
        name_match = NAME_PATTERN.search(frontmatter)
        if not name_match:
            print(f"Error: {agent_file.name} has no name in frontmatter.")
            continue
            
        name = name_match.group(1).strip()
        agent_names.add(name)
        agent_data[agent_file.name] = {
            "name": name,
            "content": content,
            "frontmatter": frontmatter
        }

    errors = 0
    
    # Second pass: validate each agent
    for filename, data in agent_data.items():
        print(f"Validating {filename}...")
        file_errors = 0
        
        # 1. Validate Model
        model_match = MODEL_PATTERN.search(data["frontmatter"])
        if model_match:
            model = model_match.group(1).strip()
            if valid_models and model not in valid_models:
                print(f"  - Invalid model: '{model}' (not found in {MODEL_REF_FILE.name})")
                file_errors += 1
        else:
            print(f"  - Missing model in frontmatter")
            file_errors += 1
            
        # 2. Validate Handoffs
        handoff_agents = HANDOFF_AGENT_PATTERN.findall(data["frontmatter"])
        for target in handoff_agents:
            if target not in agent_names:
                print(f"  - Invalid handoff target: '{target}' (agent not found)")
                file_errors += 1
                
        # 3. Validate Sections
        for section_pattern in REQUIRED_SECTIONS:
            if not re.search(section_pattern, data["content"]):
                # Clean up pattern for display
                display_name = section_pattern.replace(r"\s*(?:\*\*)?", " ").replace(r"\\", "")
                print(f"  - Missing required section: '{display_name}'")
                file_errors += 1
                
        # 4. Validate Tools (basic format check)
        tools_match = TOOLS_PATTERN.search(data["frontmatter"])
        if not tools_match:
            print(f"  - Missing or invalid tools format in frontmatter")
            file_errors += 1
        else:
            tools_str = tools_match.group(1)
            # Check for snake_case tools which are often a sign of error
            if "_" in tools_str:
                # Some tools might legitimately have underscores, but most VS Code ones use camelCase or slashes
                # Let's just warn for now or check against a known list if we had one.
                # Actually, the instructions say "Never use snake_case names like read_file".
                snake_case_tools = re.findall(r"['\"](\w+_\w+)['\"]", tools_str)
                for tool in snake_case_tools:
                    print(f"  - Potential invalid tool name (snake_case): '{tool}'")
                    file_errors += 1

        if file_errors > 0:
            errors += file_errors
            print(f"  Result: {file_errors} errors found.\n")
        else:
            print(f"  Result: OK\n")

    return errors

if __name__ == "__main__":
    if not AGENTS_DIR.exists():
        print(f"Error: {AGENTS_DIR} directory not found.")
        sys.exit(1)
        
    total_errors = validate_agents()
    if total_errors > 0:
        print(f"Total errors found: {total_errors}")
        sys.exit(1)
    else:
        print("All agents validated successfully.")
        sys.exit(0)
