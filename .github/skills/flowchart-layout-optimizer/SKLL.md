---
name: flowchart_layout_optimizer
description: Calculates an optimal, hierarchical, and non-intersecting layout for a flowchart provided as an SVG file using the Sugiyama algorithm (Dagre).
tools:
  - name: run_script
    description: Executes the layout script.
    arguments:
      - name: svg_file_path
        type: string
        description: The file path to the input SVG flowchart.
      - name: node_selector
        type: string
        description: A CSS selector (e.g., 'g.node') to identify node elements in the SVG.
      - name: edge_selector
        type: string
        description: A CSS selector (e.g., 'path.edge') to identify edge elements in the SVG.
    script: run.js
---
# Flowchart Layout Optimizer

This skill uses the industry-standard **Dagre** library to apply the **Sugiyama Layered Graph Drawing** algorithm to an SVG-rendered flowchart. The goal is to produce a beautiful, easy-to-read diagram where nodes are aligned into logical layers (ranks) and edges follow clean, non-intersecting paths.

## 💡 When to Use

Use this skill when you have an existing SVG that visually represents a flow, process, or directed graph, but the current node positions and arrow paths are messy, overlapping, or manually placed and hard to follow.

## 🛠 Prerequisites

The agent must be in an environment where Node.js and the dependencies listed in `package.json` can be installed and executed.


## 📝 Instructions for the Agent

1.  **Install Dependencies:** Run `npm install` in the skill's directory.
2.  **Graph Mapping (Critical Pre-step):** The provided SVG uses position-based routing and lacks unique `id` attributes on node groups (`g.node`) and explicit `data-source`/`data-target` attributes on edge paths (`path`).
    * **Agent Action:** The agent **MUST** execute a mapping logic *before* calling the layout script. This is done by instructing the script to use sequential IDs and providing the connection list.
3.  **Execute Script:** Call the `run_script` tool with the appropriate selectors and **the required flow map** (passed as a simplified JSON string).

    > **Required Selectors:**
    > * `node_selector`: `g.node`
    > * `edge_selector`: `path[d]` (Selects all paths that define edges)

    > **Connection Map Generation (Example for Agent):**
    > The agent will internally generate a flow map by analyzing the text content of the nodes and the flow:
    > ```json
    > {
    >   "MAINTAINER": ["REQUIREMENTS ENG", "ISSUE ANALYST", "WORKFLOW ENG"],
    >   "REQUIREMENTS ENG": ["FEATURE SPEC"],
    >   "ISSUE ANALYST": ["ISSUE ANALYSIS"],
    >   // ... and so on for all 14 nodes/artifacts
    > }
    > ```
    > *Note: Since the provided SVG has nodes/edges without IDs, the `run.js` script will now handle the sequential ID assignment.*