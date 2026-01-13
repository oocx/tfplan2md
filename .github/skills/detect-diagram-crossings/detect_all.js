#!/usr/bin/env node

/**
 * SVG Workflow Validator - CLI Tool
 * 
 * Usage: node svg-validator.js <path-to-svg-file>
 * Example: node svg-validator.js workflow.svg
 */

const fs = require('fs');
const { JSDOM } = require('jsdom');

// Parse command line arguments
const args = process.argv.slice(2);
if (args.length === 0) {
  console.error('\n❌ Error: No SVG file specified');
  console.log('\nUsage: node svg-validator.js <path-to-svg-file>');
  console.log('Example: node svg-validator.js workflow.svg\n');
  process.exit(1);
}

const filePath = args[0];

// Check if file exists
if (!fs.existsSync(filePath)) {
  console.error(`\n❌ Error: File not found: ${filePath}\n`);
  process.exit(1);
}

// Read SVG file
const svgContent = fs.readFileSync(filePath, 'utf8');

// Parse file using JSDOM - detect if it's HTML or SVG
const isHTML = svgContent.includes('<!DOCTYPE html>') || svgContent.includes('<html');
const dom = new JSDOM(svgContent, { contentType: isHTML ? 'text/html' : 'image/svg+xml' });
const document = dom.window.document;
const svg = isHTML ? document.querySelector('svg') : document.documentElement;

// Utility functions
function getNodeBounds(element) {
  const x = parseFloat(element.getAttribute('x') || 0);
  const y = parseFloat(element.getAttribute('y') || 0);
  const width = parseFloat(element.getAttribute('width') || 0);
  const height = parseFloat(element.getAttribute('height') || 0);
  return { x, y, width, height, right: x + width, bottom: y + height };
}

function getPathPoints(pathString) {
  const commands = pathString.match(/[MLHVCQTAZmlhvcqtaz][^MLHVCQTAZmlhvcqtaz]*/g) || [];
  const points = [];
  let currentX = 0, currentY = 0;

  commands.forEach(cmd => {
    const type = cmd[0];
    const coords = cmd.slice(1).trim().split(/[\s,]+/).map(Number);
    
    switch(type.toUpperCase()) {
      case 'M':
        currentX = type === 'M' ? coords[0] : currentX + coords[0];
        currentY = type === 'M' ? coords[1] : currentY + coords[1];
        points.push({ x: currentX, y: currentY });
        break;
      case 'L':
        currentX = type === 'L' ? coords[0] : currentX + coords[0];
        currentY = type === 'L' ? coords[1] : currentY + coords[1];
        points.push({ x: currentX, y: currentY });
        break;
      case 'H':
        currentX = type === 'H' ? coords[0] : currentX + coords[0];
        points.push({ x: currentX, y: currentY });
        break;
      case 'V':
        currentY = type === 'V' ? coords[0] : currentY + coords[0];
        points.push({ x: currentX, y: currentY });
        break;
    }
  });
  
  return points;
}

function getPathSegments(pathString) {
  const points = getPathPoints(pathString);
  const segments = [];
  for (let i = 0; i < points.length - 1; i++) {
    segments.push({
      x1: points[i].x,
      y1: points[i].y,
      x2: points[i + 1].x,
      y2: points[i + 1].y
    });
  }
  return segments;
}

function doRectsOverlap(rect1, rect2) {
  return !(rect1.right <= rect2.x || 
           rect2.right <= rect1.x || 
           rect1.bottom <= rect2.y || 
           rect2.bottom <= rect1.y);
}

function doSegmentsIntersect(seg1, seg2) {
  const { x1, y1, x2, y2 } = seg1;
  const { x1: x3, y1: y3, x2: x4, y2: y4 } = seg2;

  const denom = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);
  if (Math.abs(denom) < 0.0001) return false;

  const ua = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / denom;
  const ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / denom;

  return ua > 0.01 && ua < 0.99 && ub > 0.01 && ub < 0.99;
}

function doesSegmentIntersectRect(segment, rect, tolerance = 2) {
  const { x1, y1, x2, y2 } = segment;
  
  // Check if segment endpoints are inside rect (with small tolerance for touching)
  const isP1Inside = x1 > rect.x + tolerance && x1 < rect.right - tolerance && 
                     y1 > rect.y + tolerance && y1 < rect.bottom - tolerance;
  const isP2Inside = x2 > rect.x + tolerance && x2 < rect.right - tolerance && 
                     y2 > rect.y + tolerance && y2 < rect.bottom - tolerance;
  
  if (isP1Inside || isP2Inside) return true;

  // Check intersection with rect edges
  const rectSegments = [
    { x1: rect.x, y1: rect.y, x2: rect.right, y2: rect.y },
    { x1: rect.right, y1: rect.y, x2: rect.right, y2: rect.bottom },
    { x1: rect.right, y1: rect.bottom, x2: rect.x, y2: rect.bottom },
    { x1: rect.x, y1: rect.bottom, x2: rect.x, y2: rect.y }
  ];

  if (rectSegments.some(rectSeg => doSegmentsIntersect(segment, rectSeg))) {
    return true;
  }
  
  // Special case: check if orthogonal segment passes through the interior of the rect
  const isHorizontal = Math.abs(y2 - y1) < 1;
  const isVertical = Math.abs(x2 - x1) < 1;
  
  if (isHorizontal) {
    const y = (y1 + y2) / 2;
    const minX = Math.min(x1, x2);
    const maxX = Math.max(x1, x2);
    // Check if horizontal segment passes through rect interior (use >= for edge inclusion)
    if (y > rect.y && y < rect.bottom && maxX >= rect.x && minX <= rect.right) {
      return true;
    }
  }
  
  if (isVertical) {
    const x = (x1 + x2) / 2;
    const minY = Math.min(y1, y2);
    const maxY = Math.max(y1, y2);
    // Check if vertical segment passes through rect interior (use >= for edge inclusion)
    if (x >= rect.x && x <= rect.right && maxY >= rect.y && minY <= rect.bottom) {
      return true;
    }
  }
  
  return false;
}

// Check if a segment runs along (parallel to) a node edge - this is a visual issue
function doesSegmentRunAlongNodeEdge(segment, rect, proximityThreshold = 8) {
  const { x1, y1, x2, y2 } = segment;
  
  // Calculate segment length
  const segmentLength = Math.sqrt((x2 - x1) ** 2 + (y2 - y1) ** 2);
  const minOverlapLength = 15; // Minimum overlap to consider it "running along"
  
  // Check if segment is horizontal (runs along top or bottom edge)
  const isHorizontal = Math.abs(y2 - y1) < 2;
  if (isHorizontal) {
    const segY = (y1 + y2) / 2;
    const segMinX = Math.min(x1, x2);
    const segMaxX = Math.max(x1, x2);
    
    // Check if near top edge and overlaps horizontally
    if (Math.abs(segY - rect.y) < proximityThreshold) {
      const overlapStart = Math.max(segMinX, rect.x);
      const overlapEnd = Math.min(segMaxX, rect.right);
      const overlapLength = overlapEnd - overlapStart;
      if (overlapLength > minOverlapLength) return true;
    }
    
    // Check if near bottom edge and overlaps horizontally
    if (Math.abs(segY - rect.bottom) < proximityThreshold) {
      const overlapStart = Math.max(segMinX, rect.x);
      const overlapEnd = Math.min(segMaxX, rect.right);
      const overlapLength = overlapEnd - overlapStart;
      if (overlapLength > minOverlapLength) return true;
    }
  }
  
  // Check if segment is vertical (runs along left or right edge)
  const isVertical = Math.abs(x2 - x1) < 2;
  if (isVertical) {
    const segX = (x1 + x2) / 2;
    const segMinY = Math.min(y1, y2);
    const segMaxY = Math.max(y1, y2);
    
    // Check if near left edge and overlaps vertically
    if (Math.abs(segX - rect.x) < proximityThreshold) {
      const overlapStart = Math.max(segMinY, rect.y);
      const overlapEnd = Math.min(segMaxY, rect.bottom);
      const overlapLength = overlapEnd - overlapStart;
      if (overlapLength > minOverlapLength) return true;
    }
    
    // Check if near right edge and overlaps vertically
    if (Math.abs(segX - rect.right) < proximityThreshold) {
      const overlapStart = Math.max(segMinY, rect.y);
      const overlapEnd = Math.min(segMaxY, rect.bottom);
      const overlapLength = overlapEnd - overlapStart;
      if (overlapLength > minOverlapLength) return true;
    }
  }
  
  return false;
}

function getNodeLabel(element) {
  const textEl = element.querySelector('text');
  return textEl ? textEl.textContent.trim() : 'Unknown';
}

// Main validation logic
console.log('\n🔍 Validating SVG file:', filePath);
console.log('═══════════════════════════════════════════════════════════\n');

const issues = {
  overlappingNodes: [],
  intersectingPaths: [],
  pathNodeCollisions: [],
  sharedStartPoints: [],
  offCenterConnections: [],
  cornerConnections: [],
  pathEntersNode: [],
  overlappingArrows: [],
  badApproachDirection: [],
  unnecessarySegments: [],
  suboptimalRoutes: []
};

// Get all nodes (rects that are part of groups)
const nodeRects = Array.from(svg.querySelectorAll('g rect'));
const nodes = nodeRects.map(rect => ({
  element: rect,
  bounds: getNodeBounds(rect),
  label: getNodeLabel(rect.parentElement),
  parent: rect.parentElement
}));

console.log(`📦 Found ${nodes.length} nodes to check`);

// Check for overlapping nodes
for (let i = 0; i < nodes.length; i++) {
  for (let j = i + 1; j < nodes.length; j++) {
    if (doRectsOverlap(nodes[i].bounds, nodes[j].bounds)) {
      issues.overlappingNodes.push({
        node1: nodes[i].label,
        node2: nodes[j].label,
        bounds1: nodes[i].bounds,
        bounds2: nodes[j].bounds
      });
    }
  }
}

// Get all paths (excluding those in markers and patterns)
const allPaths = Array.from(svg.querySelectorAll('path[d]'));
const paths = allPaths.filter(p => {
  const d = p.getAttribute('d');
  const inMarker = p.closest('marker');
  const inPattern = p.closest('pattern');
  return d && d.includes('M') && !inMarker && !inPattern;
});

console.log(`🔗 Found ${paths.length} paths to check\n`);

const pathSegments = paths.map((path, idx) => ({
  element: path,
  segments: getPathSegments(path.getAttribute('d')),
  index: idx
}));

// Check for intersecting paths
for (let i = 0; i < pathSegments.length; i++) {
  for (let j = i + 1; j < pathSegments.length; j++) {
    const path1 = pathSegments[i];
    const path2 = pathSegments[j];
    
    for (let seg1 of path1.segments) {
      for (let seg2 of path2.segments) {
        if (doSegmentsIntersect(seg1, seg2)) {
          issues.intersectingPaths.push({
            path1: `Path ${i + 1}`,
            path2: `Path ${j + 1}`,
            location: `(${Math.round(seg1.x1)}, ${Math.round(seg1.y1)})`
          });
          break;
        }
      }
    }
  }
}

// Check for path-node collisions
// - Middle segments: Check for any crossing/intersection
// - First/last segments: Only check if they run along a node edge (visual issue)
pathSegments.forEach((path, pathIdx) => {
  const segmentCount = path.segments.length;
  nodes.forEach(node => {
    path.segments.forEach((segment, segIdx) => {
      const isFirstSegment = segIdx === 0;
      const isLastSegment = segIdx === segmentCount - 1;
      
      let isCollision = false;
      let collisionType = '';
      
      if (isFirstSegment || isLastSegment) {
        // For connection segments: only flag if running along a node edge
        if (doesSegmentRunAlongNodeEdge(segment, node.bounds)) {
          isCollision = true;
          collisionType = 'runs along edge of';
        }
      } else {
        // For middle segments: flag any intersection
        if (doesSegmentIntersectRect(segment, node.bounds)) {
          isCollision = true;
          collisionType = 'crosses';
        }
      }
      
      if (isCollision) {
        issues.pathNodeCollisions.push({
          path: `Path ${pathIdx + 1}`,
          node: node.label,
          segment: segIdx + 1,
          collisionType: collisionType,
          location: `(${Math.round(segment.x1)}, ${Math.round(segment.y1)}) → (${Math.round(segment.x2)}, ${Math.round(segment.y2)})`
        });
      }
    });
  });
});

// ============================================================================
// NEW RULE 1: Multiple paths must not start at the same point of a node
// ============================================================================
function pointKey(x, y) {
  return `${Math.round(x)},${Math.round(y)}`;
}

const startPointMap = new Map(); // Maps point -> array of path indices
pathSegments.forEach((path, pathIdx) => {
  if (path.segments.length > 0) {
    const seg = path.segments[0];
    const key = pointKey(seg.x1, seg.y1);
    if (!startPointMap.has(key)) {
      startPointMap.set(key, []);
    }
    startPointMap.get(key).push(pathIdx + 1);
  }
});

startPointMap.forEach((pathIndices, point) => {
  if (pathIndices.length > 1) {
    issues.sharedStartPoints.push({
      point: point,
      paths: pathIndices.map(i => `Path ${i}`).join(', '),
      count: pathIndices.length
    });
  }
});

// ============================================================================
// NEW RULE 2: Single connections to a node must be centered on the border
// ============================================================================
function findNodeForPoint(x, y, nodeList, tolerance = 5) {
  for (const node of nodeList) {
    const b = node.bounds;
    // Check if point is on any edge of the node
    const onTop = Math.abs(y - b.y) <= tolerance && x >= b.x - tolerance && x <= b.right + tolerance;
    const onBottom = Math.abs(y - b.bottom) <= tolerance && x >= b.x - tolerance && x <= b.right + tolerance;
    const onLeft = Math.abs(x - b.x) <= tolerance && y >= b.y - tolerance && y <= b.bottom + tolerance;
    const onRight = Math.abs(x - b.right) <= tolerance && y >= b.y - tolerance && y <= b.bottom + tolerance;
    if (onTop || onBottom || onLeft || onRight) {
      return { node, edge: onTop ? 'top' : onBottom ? 'bottom' : onLeft ? 'left' : 'right' };
    }
  }
  return null;
}

function getEdgeCenter(node, edge) {
  const b = node.bounds;
  switch (edge) {
    case 'top': return { x: b.x + b.width / 2, y: b.y };
    case 'bottom': return { x: b.x + b.width / 2, y: b.bottom };
    case 'left': return { x: b.x, y: b.y + b.height / 2 };
    case 'right': return { x: b.right, y: b.y + b.height / 2 };
  }
}

// Build connection map: for each node-edge, track how many paths connect there
const connectionMap = new Map(); // "nodeLabel:edge" -> array of {pathIdx, point}

pathSegments.forEach((path, pathIdx) => {
  if (path.segments.length === 0) return;
  
  // Check start point
  const startSeg = path.segments[0];
  const startInfo = findNodeForPoint(startSeg.x1, startSeg.y1, nodes);
  if (startInfo) {
    const key = `${startInfo.node.label}:${startInfo.edge}`;
    if (!connectionMap.has(key)) connectionMap.set(key, []);
    connectionMap.get(key).push({ pathIdx: pathIdx + 1, point: { x: startSeg.x1, y: startSeg.y1 }, isStart: true });
  }
  
  // Check end point
  const endSeg = path.segments[path.segments.length - 1];
  const endInfo = findNodeForPoint(endSeg.x2, endSeg.y2, nodes);
  if (endInfo) {
    const key = `${endInfo.node.label}:${endInfo.edge}`;
    if (!connectionMap.has(key)) connectionMap.set(key, []);
    connectionMap.get(key).push({ pathIdx: pathIdx + 1, point: { x: endSeg.x2, y: endSeg.y2 }, isStart: false });
  }
});

// Check for off-center single connections
connectionMap.forEach((connections, key) => {
  if (connections.length === 1) {
    const conn = connections[0];
    const [nodeLabel, edge] = key.split(':');
    const node = nodes.find(n => n.label === nodeLabel);
    if (!node) return;
    
    const center = getEdgeCenter(node, edge);
    const tolerance = 15; // Allow some tolerance from exact center
    
    const dx = Math.abs(conn.point.x - center.x);
    const dy = Math.abs(conn.point.y - center.y);
    const isOffCenter = (edge === 'top' || edge === 'bottom') ? dx > tolerance : dy > tolerance;
    
    if (isOffCenter) {
      issues.offCenterConnections.push({
        path: `Path ${conn.pathIdx}`,
        node: nodeLabel,
        edge: edge,
        actual: `(${Math.round(conn.point.x)}, ${Math.round(conn.point.y)})`,
        expected: `(${Math.round(center.x)}, ${Math.round(center.y)})`,
        isStart: conn.isStart
      });
    }
  }
});

// ============================================================================
// NEW RULE: Corner connections - never connect at or near corners
// ============================================================================
function isNearCorner(point, bounds, cornerThreshold = 15) {
  const nearLeft = Math.abs(point.x - bounds.x) <= cornerThreshold;
  const nearRight = Math.abs(point.x - bounds.right) <= cornerThreshold;
  const nearTop = Math.abs(point.y - bounds.y) <= cornerThreshold;
  const nearBottom = Math.abs(point.y - bounds.bottom) <= cornerThreshold;
  
  // Near corner if close to both an X edge and a Y edge
  const isCorner = (nearLeft || nearRight) && (nearTop || nearBottom);
  if (!isCorner) return null;
  
  const corner = `${nearTop ? 'top' : 'bottom'}-${nearLeft ? 'left' : 'right'}`;
  return corner;
}

connectionMap.forEach((connections, key) => {
  const [nodeLabel, edge] = key.split(':');
  const node = nodes.find(n => n.label === nodeLabel);
  if (!node) return;
  
  connections.forEach(conn => {
    const corner = isNearCorner(conn.point, node.bounds);
    if (corner) {
      issues.cornerConnections.push({
        path: `Path ${conn.pathIdx}`,
        node: nodeLabel,
        corner: corner,
        point: `(${Math.round(conn.point.x)}, ${Math.round(conn.point.y)})`,
        isStart: conn.isStart
      });
    }
  });
});

// ============================================================================
// NEW RULE: Path segments should not pass through connected nodes
// (except for the final segment approaching the connection point)
// ============================================================================
pathSegments.forEach((path, pathIdx) => {
  if (path.segments.length < 2) return;
  
  const lastSeg = path.segments[path.segments.length - 1];
  const endPoint = { x: lastSeg.x2, y: lastSeg.y2 };
  const endNodeInfo = findNodeForPoint(endPoint.x, endPoint.y, nodes);
  
  if (!endNodeInfo) return;
  
  // Check all segments except the last one
  for (let i = 0; i < path.segments.length - 1; i++) {
    const seg = path.segments[i];
    if (doesSegmentIntersectRect(seg, endNodeInfo.node.bounds)) {
      issues.pathEntersNode.push({
        path: `Path ${pathIdx + 1}`,
        node: endNodeInfo.node.label,
        segment: i + 1,
        description: `Segment ${i + 1} passes through the target node before reaching it`
      });
      break; // Only report once per path
    }
  }
});

// ============================================================================
// NEW RULE: Multiple connections to same edge should be spread out
// ============================================================================
connectionMap.forEach((connections, key) => {
  if (connections.length < 2) return;
  
  const [nodeLabel, edge] = key.split(':');
  const node = nodes.find(n => n.label === nodeLabel);
  if (!node) return;
  
  // Sort connections by position along the edge
  const sorted = [...connections].sort((a, b) => {
    if (edge === 'top' || edge === 'bottom') return a.point.x - b.point.x;
    return a.point.y - b.point.y;
  });
  
  // Check if any two are too close (causing arrow overlap)
  const minSpacing = 30; // Minimum pixels between connection points
  for (let i = 0; i < sorted.length - 1; i++) {
    const dist = (edge === 'top' || edge === 'bottom') 
      ? Math.abs(sorted[i + 1].point.x - sorted[i].point.x)
      : Math.abs(sorted[i + 1].point.y - sorted[i].point.y);
    
    if (dist < minSpacing) {
      issues.overlappingArrows.push({
        paths: `Path ${sorted[i].pathIdx} and Path ${sorted[i + 1].pathIdx}`,
        node: nodeLabel,
        edge: edge,
        spacing: Math.round(dist),
        required: minSpacing,
        suggestion: `Spread connection points apart by at least ${minSpacing}px`
      });
    }
  }
});

// ============================================================================
// NEW RULE: Awkward arrow entry - detect T-junction patterns where a path 
// arrives perpendicular to the final arrow direction, creating a visual "T"
// Example: horizontal path into a top-edge arrow (should go up a bit first)
// ============================================================================
pathSegments.forEach((path, pathIdx) => {
  if (path.segments.length < 2) return;
  
  const lastSeg = path.segments[path.segments.length - 1];
  const prevSeg = path.segments[path.segments.length - 2];
  const endPoint = { x: lastSeg.x2, y: lastSeg.y2 };
  const endNodeInfo = findNodeForPoint(endPoint.x, endPoint.y, nodes);
  
  if (!endNodeInfo) return;
  
  // Get the length of the final segment
  const lastLen = Math.sqrt((lastSeg.x2 - lastSeg.x1) ** 2 + (lastSeg.y2 - lastSeg.y1) ** 2);
  
  // Only flag very short final segments (< 20px) where prev segment is perpendicular
  if (lastLen >= 20) return;
  
  // Check if previous segment is perpendicular to the edge
  const prevDx = Math.abs(prevSeg.x2 - prevSeg.x1);
  const prevDy = Math.abs(prevSeg.y2 - prevSeg.y1);
  const prevIsHorizontal = prevDx > prevDy;
  const prevIsVertical = prevDy > prevDx;
  
  const edgeIsHorizontal = endNodeInfo.edge === 'top' || endNodeInfo.edge === 'bottom';
  const edgeIsVertical = endNodeInfo.edge === 'left' || endNodeInfo.edge === 'right';
  
  // Awkward if prev segment is same orientation as edge (perpendicular to arrow direction)
  if ((prevIsHorizontal && edgeIsHorizontal) || (prevIsVertical && edgeIsVertical)) {
    issues.badApproachDirection.push({
      path: `Path ${pathIdx + 1}`,
      node: endNodeInfo.node.label,
      edge: endNodeInfo.edge,
      description: `Short final segment (${Math.round(lastLen)}px) after perpendicular approach - creates T-junction look`
    });
  }
});

// ============================================================================
// NEW RULE 3: Avoid multi-segment paths when direct connection is possible
// Only flag truly unnecessary segments (orthogonal 2-segment paths are OK)
// ============================================================================
function couldBeDirectConnection(path, nodeList) {
  // Only flag paths with more than 2 segments - 2-segment orthogonal paths are standard
  if (path.segments.length <= 2) return null;
  
  const firstSeg = path.segments[0];
  const lastSeg = path.segments[path.segments.length - 1];
  const start = { x: firstSeg.x1, y: firstSeg.y1 };
  const end = { x: lastSeg.x2, y: lastSeg.y2 };
  
  // Check if a simple 2-segment orthogonal path would work (no crossings)
  // Option A: go horizontal first, then vertical
  const midA = { x: end.x, y: start.y };
  const pathA = [
    { x1: start.x, y1: start.y, x2: midA.x, y2: midA.y },
    { x1: midA.x, y1: midA.y, x2: end.x, y2: end.y }
  ];
  
  // Option B: go vertical first, then horizontal
  const midB = { x: start.x, y: end.y };
  const pathB = [
    { x1: start.x, y1: start.y, x2: midB.x, y2: midB.y },
    { x1: midB.x, y1: midB.y, x2: end.x, y2: end.y }
  ];
  
  function pathCrossesAnyNode(segments) {
    for (let segIdx = 0; segIdx < segments.length; segIdx++) {
      const seg = segments[segIdx];
      for (const node of nodeList) {
        // Skip intersection check for nodes that the path connects to
        const startInfo = findNodeForPoint(start.x, start.y, [node]);
        const endInfo = findNodeForPoint(end.x, end.y, [node]);
        
        if (startInfo || endInfo) {
          // For connected nodes, still check edge-running (it's visually problematic)
          // unless it's a very short segment immediately leaving/entering the node
          if (doesSegmentRunAlongNodeEdge(seg, node.bounds)) {
            return true;
          }
          continue; // Skip intersection check for connected nodes
        }
        
        // Check both intersection and edge-running for non-connected nodes
        if (doesSegmentIntersectRect(seg, node.bounds)) {
          return true;
        }
        if (doesSegmentRunAlongNodeEdge(seg, node.bounds)) {
          return true;
        }
      }
    }
    return false;
  }
  
  const aWorks = !pathCrossesAnyNode(pathA);
  const bWorks = !pathCrossesAnyNode(pathB);
  
  if (!aWorks && !bWorks) return null; // No simple 2-segment path works
  
  const currentLength = path.segments.reduce((sum, seg) => {
    return sum + Math.sqrt((seg.x2 - seg.x1) ** 2 + (seg.y2 - seg.y1) ** 2);
  }, 0);
  
  const lengthA = Math.abs(end.x - start.x) + Math.abs(end.y - start.y);
  const lengthB = lengthA; // Same for orthogonal paths
  
  // Only report if the simpler path would be significantly shorter (>20% savings)
  const bestLength = aWorks && bWorks ? lengthA : (aWorks ? lengthA : lengthB);
  if (bestLength >= currentLength * 0.8) return null;
  
  return {
    segmentCount: path.segments.length,
    currentLength: Math.round(currentLength),
    simpleLength: Math.round(bestLength),
    start: start,
    end: end,
    suggestion: aWorks ? 'horizontal-then-vertical' : 'vertical-then-horizontal'
  };
}

pathSegments.forEach((path, pathIdx) => {
  const result = couldBeDirectConnection(path, nodes);
  if (result) {
    issues.unnecessarySegments.push({
      path: `Path ${pathIdx + 1}`,
      segments: result.segmentCount,
      from: `(${Math.round(result.start.x)}, ${Math.round(result.start.y)})`,
      to: `(${Math.round(result.end.x)}, ${Math.round(result.end.y)})`,
      currentLength: result.currentLength,
      simpleLength: result.simpleLength,
      suggestion: result.suggestion
    });
  }
});

// ============================================================================
// NEW RULE 4: If multiple intersection-free routes exist, use the shorter one
// Only applies to paths with 3+ segments where a simpler route would work
// ============================================================================
function generateOrthogonalRoutes(start, end) {
  // Generate the two standard orthogonal 2-segment routes
  const routes = [];
  
  // Route 1: Horizontal first, then vertical
  routes.push({
    name: 'horizontal-first',
    waypoints: [start, { x: end.x, y: start.y }, end],
    length: Math.abs(end.x - start.x) + Math.abs(end.y - start.y)
  });
  
  // Route 2: Vertical first, then horizontal
  routes.push({
    name: 'vertical-first',
    waypoints: [start, { x: start.x, y: end.y }, end],
    length: Math.abs(end.x - start.x) + Math.abs(end.y - start.y)
  });
  
  return routes;
}

function routeCrossesNodes(waypoints, nodeList, startNode, endNode) {
  for (let i = 0; i < waypoints.length - 1; i++) {
    const seg = { x1: waypoints[i].x, y1: waypoints[i].y, x2: waypoints[i + 1].x, y2: waypoints[i + 1].y };
    for (const node of nodeList) {
      // Skip intersection check for start and end nodes
      const isConnectedNode = node.label === startNode || node.label === endNode;
      
      if (isConnectedNode) {
        // For connected nodes, still check edge-running (it's visually problematic)
        if (doesSegmentRunAlongNodeEdge(seg, node.bounds)) {
          return true;
        }
        continue; // Skip intersection check for connected nodes
      }
      
      // Check both intersection and edge-running for non-connected nodes
      if (doesSegmentIntersectRect(seg, node.bounds)) {
        return true;
      }
      if (doesSegmentRunAlongNodeEdge(seg, node.bounds)) {
        return true;
      }
    }
  }
  return false;
}

function getCurrentRouteLength(path) {
  return path.segments.reduce((sum, seg) => {
    return sum + Math.sqrt((seg.x2 - seg.x1) ** 2 + (seg.y2 - seg.y1) ** 2);
  }, 0);
}

pathSegments.forEach((path, pathIdx) => {
  // Only check paths with 3+ segments
  if (path.segments.length < 3) return;
  
  const firstSeg = path.segments[0];
  const lastSeg = path.segments[path.segments.length - 1];
  const start = { x: firstSeg.x1, y: firstSeg.y1 };
  const end = { x: lastSeg.x2, y: lastSeg.y2 };
  
  // Find which nodes this path connects
  const startNodeInfo = findNodeForPoint(start.x, start.y, nodes);
  const endNodeInfo = findNodeForPoint(end.x, end.y, nodes);
  const startNodeLabel = startNodeInfo ? startNodeInfo.node.label : null;
  const endNodeLabel = endNodeInfo ? endNodeInfo.node.label : null;
  
  const currentLength = getCurrentRouteLength(path);
  const alternatives = generateOrthogonalRoutes(start, end);
  
  // Find valid (non-crossing) alternatives that are shorter
  const validShorterAlternatives = alternatives.filter(alt => {
    // Must be at least 15% shorter to be worth flagging
    if (alt.length >= currentLength * 0.85) return false;
    return !routeCrossesNodes(alt.waypoints, nodes, startNodeLabel, endNodeLabel);
  });
  
  if (validShorterAlternatives.length > 0) {
    const best = validShorterAlternatives.reduce((a, b) => a.length < b.length ? a : b);
    issues.suboptimalRoutes.push({
      path: `Path ${pathIdx + 1}`,
      currentLength: Math.round(currentLength),
      betterLength: Math.round(best.length),
      betterRoute: best.name,
      savings: Math.round((currentLength - best.length) / currentLength * 100) + '%',
      from: `(${Math.round(start.x)}, ${Math.round(start.y)})`,
      to: `(${Math.round(end.x)}, ${Math.round(end.y)})`
    });
  }
});

// Print results
const totalIssues = issues.overlappingNodes.length + 
                    issues.intersectingPaths.length + 
                    issues.pathNodeCollisions.length +
                    issues.sharedStartPoints.length +
                    issues.offCenterConnections.length +
                    issues.cornerConnections.length +
                    issues.pathEntersNode.length +
                    issues.overlappingArrows.length +
                    issues.badApproachDirection.length +
                    issues.unnecessarySegments.length +
                    issues.suboptimalRoutes.length;

if (totalIssues === 0) {
  console.log('✅ SUCCESS: No issues found!');
  console.log('\nAll validation checks passed:');
  console.log('  • No overlapping nodes');
  console.log('  • No intersecting paths');
  console.log('  • No path-node collisions');
  console.log('  • No shared start points');
  console.log('  • No off-center single connections');
  console.log('  • No corner connections');
  console.log('  • No paths entering target nodes');
  console.log('  • No overlapping arrows');
  console.log('  • No bad approach directions');
  console.log('  • No unnecessary multi-segment paths');
  console.log('  • No suboptimal routes');
  console.log('\n');
  process.exit(0);
}

console.log(`❌ VALIDATION FAILED: Found ${totalIssues} issue(s)\n`);

// Print overlapping nodes
if (issues.overlappingNodes.length > 0) {
  console.log(`\n⚠️  OVERLAPPING NODES (${issues.overlappingNodes.length}):`);
  console.log('─────────────────────────────────────────────────────────');
  issues.overlappingNodes.forEach((issue, idx) => {
    console.log(`\n${idx + 1}. ${issue.node1} ⚠️  ${issue.node2}`);
    console.log(`   Node 1: (${Math.round(issue.bounds1.x)}, ${Math.round(issue.bounds1.y)}) ${Math.round(issue.bounds1.width)}×${Math.round(issue.bounds1.height)}`);
    console.log(`   Node 2: (${Math.round(issue.bounds2.x)}, ${Math.round(issue.bounds2.y)}) ${Math.round(issue.bounds2.width)}×${Math.round(issue.bounds2.height)}`);
  });
}

// Print intersecting paths
if (issues.intersectingPaths.length > 0) {
  console.log(`\n\n⚠️  INTERSECTING PATHS (${issues.intersectingPaths.length}):`);
  console.log('─────────────────────────────────────────────────────────');
  issues.intersectingPaths.forEach((issue, idx) => {
    console.log(`\n${idx + 1}. ${issue.path1} ⚠️  ${issue.path2}`);
    console.log(`   Intersection near: ${issue.location}`);
  });
}

// Print path-node collisions
if (issues.pathNodeCollisions.length > 0) {
  console.log(`\n\n⚠️  PATH-NODE COLLISIONS (${issues.pathNodeCollisions.length}):`);
  console.log('─────────────────────────────────────────────────────────');
  issues.pathNodeCollisions.forEach((issue, idx) => {
    const verb = issue.collisionType || 'crosses';
    console.log(`\n${idx + 1}. ${issue.path} ${verb} ${issue.node}`);
    console.log(`   Segment ${issue.segment}: ${issue.location}`);
  });
}

// Print shared start points
if (issues.sharedStartPoints.length > 0) {
  console.log(`\n\n⚠️  SHARED START POINTS (${issues.sharedStartPoints.length}):`);
  console.log('─────────────────────────────────────────────────────────');
  issues.sharedStartPoints.forEach((issue, idx) => {
    console.log(`\n${idx + 1}. ${issue.count} paths share start point at ${issue.point}`);
    console.log(`   Paths: ${issue.paths}`);
  });
}

// Print off-center connections
if (issues.offCenterConnections.length > 0) {
  console.log(`\n\n⚠️  OFF-CENTER CONNECTIONS (${issues.offCenterConnections.length}):`);
  console.log('─────────────────────────────────────────────────────────');
  issues.offCenterConnections.forEach((issue, idx) => {
    const direction = issue.isStart ? 'starts from' : 'ends at';
    console.log(`\n${idx + 1}. ${issue.path} ${direction} ${issue.edge} of ${issue.node} off-center`);
    console.log(`   Actual: ${issue.actual}, Expected center: ${issue.expected}`);
  });
}

// Print corner connections
if (issues.cornerConnections.length > 0) {
  console.log(`\n\n⚠️  CORNER CONNECTIONS (${issues.cornerConnections.length}):`);
  console.log('─────────────────────────────────────────────────────────');
  issues.cornerConnections.forEach((issue, idx) => {
    const direction = issue.isStart ? 'starts from' : 'ends at';
    console.log(`\n${idx + 1}. ${issue.path} ${direction} ${issue.corner} corner of ${issue.node}`);
    console.log(`   Point: ${issue.point} - should avoid corners for cleaner appearance`);
  });
}

// Print path enters node issues
if (issues.pathEntersNode.length > 0) {
  console.log(`\n\n⚠️  PATH ENTERS TARGET NODE (${issues.pathEntersNode.length}):`);
  console.log('─────────────────────────────────────────────────────────');
  issues.pathEntersNode.forEach((issue, idx) => {
    console.log(`\n${idx + 1}. ${issue.path} crosses through ${issue.node}`);
    console.log(`   ${issue.description}`);
  });
}

// Print overlapping arrows
if (issues.overlappingArrows.length > 0) {
  console.log(`\n\n⚠️  OVERLAPPING ARROWS (${issues.overlappingArrows.length}):`);
  console.log('─────────────────────────────────────────────────────────');
  issues.overlappingArrows.forEach((issue, idx) => {
    console.log(`\n${idx + 1}. ${issue.paths} connect too close on ${issue.edge} of ${issue.node}`);
    console.log(`   Current spacing: ${issue.spacing}px, Required: ${issue.required}px`);
    console.log(`   ${issue.suggestion}`);
  });
}

// Print bad approach direction
if (issues.badApproachDirection.length > 0) {
  console.log(`\n\n⚠️  BAD APPROACH DIRECTION (${issues.badApproachDirection.length}):`);
  console.log('─────────────────────────────────────────────────────────');
  issues.badApproachDirection.forEach((issue, idx) => {
    console.log(`\n${idx + 1}. ${issue.path} has awkward approach to ${issue.node}`);
    console.log(`   ${issue.description}`);
  });
}

// Print unnecessary segments
if (issues.unnecessarySegments.length > 0) {
  console.log(`\n\n⚠️  UNNECESSARY MULTI-SEGMENT PATHS (${issues.unnecessarySegments.length}):`);
  console.log('─────────────────────────────────────────────────────────');
  issues.unnecessarySegments.forEach((issue, idx) => {
    console.log(`\n${idx + 1}. ${issue.path} has ${issue.segments} segments but 2 would suffice`);
    console.log(`   From ${issue.from} to ${issue.to}`);
    console.log(`   Current length: ${issue.currentLength}px, Simple 2-segment: ${issue.simpleLength}px`);
    console.log(`   Suggestion: Use ${issue.suggestion} routing`);
  });
}

// Print suboptimal routes
if (issues.suboptimalRoutes.length > 0) {
  console.log(`\n\n⚠️  SUBOPTIMAL ROUTES (${issues.suboptimalRoutes.length}):`);
  console.log('─────────────────────────────────────────────────────────');
  issues.suboptimalRoutes.forEach((issue, idx) => {
    console.log(`\n${idx + 1}. ${issue.path} uses a longer route than necessary`);
    console.log(`   From ${issue.from} to ${issue.to}`);
    console.log(`   Current: ${issue.currentLength}px, Better (${issue.betterRoute}): ${issue.betterLength}px (${issue.savings} shorter)`);
  });
}

console.log('\n═══════════════════════════════════════════════════════════\n');
process.exit(1);