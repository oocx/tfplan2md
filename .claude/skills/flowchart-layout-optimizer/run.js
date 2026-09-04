// run.js (Updated for custom SVG structure)
const fs = require('fs');
const dagre = require('@dagrejs/dagre');
const { JSDOM } = require('jsdom');
const d3 = require('d3-selection');

// --- Configuration ---
const LAYOUT_CONFIG = {
    rankdir: 'TB',
    ranksep: 50,
    nodesep: 50,
    ranker: 'network-simplex',
    acyclic: true
};

// --- Helper Functions ---

/**
 * Extracts the dimensions (width/height) of a node element from its inner <rect>.
 * @param {Element} nodeEl - The SVG <g> element representing the node.
 * @returns {{width: number, height: number}} The calculated dimensions.
 */
function getNodeDimensions(nodeEl) {
    const rect = nodeEl.querySelector('rect');
    if (rect) {
        return { 
            width: parseFloat(d3.select(rect).attr('width')) || 180, 
            height: parseFloat(d3.select(rect).attr('height')) || 40
        };
    }
    // Fallback if no inner rect is found
    return { width: 180, height: 40 }; 
}

/**
 * Converts a Dagre edge's polyline points into an SVG path string ('d' attribute).
 * (Same as before, used to render the clean orthogonal path)
 */
function pointsToSvgPath(points) {
    if (!points || points.length === 0) return '';
    let d = `M${points[0].x},${points[0].y}`;
    for (let i = 1; i < points.length; i++) {
        d += `L${points[i].x},${points[i].y}`;
    }
    return d;
}

// --- Main Execution Function ---

async function optimizeLayout(svgFilePath, nodeSelector, edgeSelector) {
    if (!fs.existsSync(svgFilePath)) {
        console.error(`Error: Input file not found at ${svgFilePath}`);
        return;
    }

    // 1. Load and Parse SVG
    const svgContent = fs.readFileSync(svgFilePath, 'utf8');
    const dom = new JSDOM(svgContent, { contentType: "image/svg+xml" });
    const document = dom.window.document;
    const svg = d3.select(document.documentElement);

    // 2. Build the Graph Data Structure (Addressing Missing IDs)
    const g = new dagre.graphlib.Graph({ compound: true, directed: true });
    g.setGraph(LAYOUT_CONFIG);
    g.setDefaultEdgeLabel(() => ({}));

    const nodesMap = new Map(); // Maps Node Title -> { gEl: Element, id: string }
    const edgesArray = []; // List of original path elements

    // 2.1. Extract Nodes and Assign Sequential IDs
    document.querySelectorAll(nodeSelector).forEach((el, index) => {
        // Use the text content as the key for mapping the flow
        const textEl = el.querySelector('text:first-of-type');
        let title = textEl ? textEl.textContent.trim().replace(/\s/g, '_').replace(/[^a-zA-Z0-9_]/g, '') : `NODE_${index}`;
        
        // Ensure unique ID, if multiple text elements exist, or it's a generic title
        let id = title;
        let count = 1;
        while (nodesMap.has(id)) {
            id = `${title}_${count++}`;
        }

        const dimensions = getNodeDimensions(el);
        g.setNode(id, dimensions);
        nodesMap.set(id, { gEl: el, id: id, title: title });
        d3.select(el).attr('data-dagre-id', id); // Add temporary ID for later lookup
    });
    
    // 2.2. Extract Edges (Paths)
    document.querySelectorAll(edgeSelector).forEach(el => {
        edgesArray.push(el);
    });


    // 3. MANUAL GRAPH FLOW DEFINITION (CRITICAL FIX FOR YOUR SVG)
    // The script cannot infer the connections (data-source/data-target) from your SVG.
    // We must manually define the flow based on the structure provided in your code:
    const FLOW_MAP = {
        // Node_ID/Title: [Target_ID_1, Target_ID_2, ...]
        "MAINTAINER": ["REQUIREMENTSENG", "ISSUEANALYST", "WORKFLOWENG"],
        "REQUIREMENTSENG": ["FEATURESPEC"],
        "ISSUEANALYST": ["ISSUEANALYSIS"],
        "WORKFLOWENG": ["WORKFLOWDOC"],
        "FEATURESPEC": ["ARCHITECT"],
        "ISSUEANALYSIS": ["DEVELOPER", "TASKPLANNER"], // Note: Task Planner is on the main path, Developer is the bug fix path
        "ARCHITECT": ["ADR"],
        "ADR": ["QUALITYENGINEER"],
        "QUALITYENGINEER": ["TESTPLAN"],
        "TESTPLAN": ["TASKPLANNER"],
        "TASKPLANNER": ["USERSTORIES"],
        "USERSTORIES": ["DEVELOPER"],
        "DEVELOPER": ["CODEANDTESTS"],
        "CODEANDTESTS": ["TECHWRITER", "CODEREVIEWER"],
        "TECHWRITER": ["DOCUMENTATION"],
        "DOCUMENTATION": ["CODEREVIEWER"],
        "CODEREVIEWER": ["DEVELOPER_rework_1", "REVIEWREPORT"], // Rework arrow back to developer, then forward
        "REVIEWREPORT": ["UATTESTER", "RELEASEMGR"], // UAT Path / No UAT Path
        "UATTESTER": ["UATPR"],
        "UATPR": ["DEVELOPER_rework_2", "RELEASEMGR"], // Rework arrow back to developer, then forward
        "RELEASEMGR": ["RELEASEPR", "RETROSPECTIVE"], // Path to Release / Path to Retro
        "RETROSPECTIVE": ["RETROREPORT"],
        "RETROREPORT": ["WORKFLOWENG"] // Loop back for improvement
    };

    // 3.1. Add Edges to Dagre Graph using the FLOW_MAP
    let edgeIndex = 0;
    for (const sourceTitle in FLOW_MAP) {
        const sourceInfo = nodesMap.get(sourceTitle);
        if (!sourceInfo) continue;

        for (const targetTitle of FLOW_MAP[sourceTitle]) {
            const targetInfo = nodesMap.get(targetTitle.replace(/_rework_\d+/g, '')); // Target is always the base node
            
            if (targetInfo) {
                // Ensure we use the original path element from the edgesArray
                const originalPathEl = edgesArray[edgeIndex++]; 
                if (originalPathEl) {
                    const edgeId = `${sourceInfo.id}_to_${targetInfo.id}_${edgeIndex}`;
                    g.setEdge(sourceInfo.id, targetInfo.id, { id: edgeId });
                    d3.select(originalPathEl).attr('data-dagre-id', edgeId); // Map path to new ID
                }
            }
        }
    }


    // 4. Calculate Layout
    dagre.layout(g);

    // 5. Apply New Positions and Paths to SVG
    const graphData = g.graph();
    svg.attr("width", graphData.width)
       .attr("height", graphData.height)
       .attr("viewBox", `0 0 ${graphData.width} ${graphData.height}`);

    // 5.1. Update Node Positions
    g.nodes().forEach(v => {
        if (!g.isNode(v)) return;
        const node = g.node(v);
        const nodeInfo = nodesMap.get(v);

        if (nodeInfo && nodeInfo.gEl) {
            // Apply transform to position the node group based on its center
            d3.select(nodeInfo.gEl)
                .attr('transform', `translate(${node.x - node.width / 2}, ${node.y - node.height / 2})`);
        }
    });

    // 5.2. Update Edge Paths
    // We need to re-find the path elements using the temporary ID we set
    const edgesToUpdate = document.querySelectorAll(`path[data-dagre-id]`);
    
    edgesToUpdate.forEach(el => {
        const edgeId = d3.select(el).attr('data-dagre-id');
        const edge = g.edge(dagre.util.splitEdgeId(edgeId));
        
        if (edge && edge.points) {
            // Dagre points define the optimal polyline path that avoids nodes and crossings
            const pathData = pointsToSvgPath(edge.points);
            d3.select(el).attr('d', pathData);
            
            // Clean up the temporary ID
            d3.select(el).attr('data-dagre-id', null);
            
            // Optional: The agent would also need to update markers (arrowheads)
            // if their position depends on the end point of the path.
        }
    });

    // 6. Save the Output
    // Clean up temporary node IDs before saving
    document.querySelectorAll(nodeSelector).forEach(el => {
        d3.select(el).attr('data-dagre-id', null);
    });
    
    const newSvgContent = dom.window.document.documentElement.outerHTML;
    fs.writeFileSync(svgFilePath, newSvgContent, 'utf8');
    
    console.log(`Successfully optimized and updated layout in: ${svgFilePath}`);
}

// --- Entry Point for the Agent Script ---
if (require.main === module) {
    const args = process.argv.slice(2);
    // Usage: node run.js <svg_file_path> <node_selector> <edge_selector>
    if (args.length !== 3) {
        console.error("Usage: node run.js <svg_file_path> <node_selector> <edge_selector>");
        process.exit(1);
    }
    optimizeLayout(args[0], args[1], args[2]);
}