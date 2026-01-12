---
name: detect-diagram-crossings
description: Detect and analyze edge crossings and overlaps in SVG workflow diagrams using geometric intersection algorithms and visual analysis. Use when validating diagram layouts.
---

# Detect Diagram Crossings

## Purpose
Provide a rigorous, multi-method approach to detecting edge crossings and overlaps in SVG flowchart diagrams. This skill combines mathematical geometric analysis with visual inspection to ensure diagrams have clean, crossing-free layouts.

## When to Use
- **Validating Diagram Layouts**: After creating or modifying workflow diagrams
- **Quality Assurance**: Before committing diagram changes to ensure no crossings
- **Debugging Visual Issues**: When users report crossing/overlap problems
- **Comparative Analysis**: Testing multiple layout versions to find the best one

## Detection Methods

### Method 1: Parametric Line Intersection (Mathematical)
The primary detection method uses parametric line equations to mathematically determine if two line segments intersect.

**Algorithm:**
1. Parse SVG paths into line segments (sequences of points connected by lines)
2. For each pair of line segments from different paths:
   - Calculate intersection using parametric equations:
     - Line 1: P = P1 + t(P2 - P1)
     - Line 2: P = P3 + u(P4 - P3)
   - Solve for parameters t and u
   - Intersection occurs if 0.01 < t < 0.99 AND 0.01 < u < 0.99
3. Exclude endpoint connections (valid node-to-node connections)
4. Report all internal segment intersections as crossings

**Why This Works:**
- Mathematically precise: detects exact intersection points
- Coordinate-based: doesn't rely on visual rendering
- Tolerance-aware: excludes near-endpoint intersections (within 1% of segment length)

**Limitations:**
- May not detect visual overlaps that aren't true mathematical intersections
- Doesn't account for stroke width or arrow markers
- Requires well-formed SVG path data

### Method 2: Node-Path Proximity Analysis
Detects when paths pass through or very close to node bodies (not at connection points).

**Algorithm:**
1. Extract all node bounding boxes (x, y, width, height)
2. Extract all points from path definitions
3. For each path point:
   - Check if it falls inside any node boundary
   - Exclude points that are on node edges (valid connection points)
   - Report interior intersections as overlaps

**Why This Works:**
- Catches paths that visually obscure nodes
- Identifies routing issues where paths don't avoid nodes
- Complements line intersection by checking path-node relationships

### Method 3: Visual Screenshot Analysis
Uses actual rendered output to catch visual issues that math might miss.

**Algorithm:**
1. Capture high-resolution screenshot of rendered diagram
2. Visually inspect for:
   - Lines crossing over each other
   - Paths obscuring node labels
   - Arrow markers colliding with nodes/paths
   - Color overlaps making paths indistinguishable
3. Document specific coordinates of visual crossings

**Why This Works:**
- Catches rendering artifacts (antialiasing, stroke width effects)
- Validates user-perceived quality
- Identifies issues in arrow markers and decorations
- Accounts for font rendering and text overlaps

## Hard Rules

### Must
- Run all three detection methods for thorough validation
- Report exact coordinates of intersection points
- Identify which paths are involved in each crossing
- Calculate parametric intersection parameters (t, u values)
- Test with actual SVG data from the diagram file
- Create visual annotations showing crossing locations

### Must Not
- Rely on only one detection method
- Report endpoint connections as crossings
- Skip proximity analysis
- Ignore visual rendering validation

## Testing Approach

### Test 1: Mathematical Verification
Run parametric line intersection detection on all paths.

### Test 2: Node Proximity Check
Verify no paths pass through node bodies.

### Test 3: Visual Inspection
Analyze rendered screenshot for visual crossing artifacts.

## Actions

### 1. Run Full Detection Suite

Execute the comprehensive detection script:
```bash
python3 .github/skills/detect-diagram-crossings/detect_all.py website/ai-workflow.html
```

This will:
- Parse SVG and extract all paths and nodes
- Run parametric intersection tests on all path pairs
- Check path-node proximity
- Generate a detailed report

### 2. Generate Visual Analysis

Capture and analyze a screenshot:
```bash
python3 .github/skills/detect-diagram-crossings/visual_check.py website/ai-workflow.html
```

### 3. Validate Fix

After modifying diagram, re-run detection to confirm improvements.

## Detection Metrics

### Success Criteria
- ✅ **Zero mathematical crossings**: All parametric tests pass
- ✅ **Zero proximity issues**: No paths through node bodies
- ✅ **Clean visual rendering**: Screenshot confirms no visual crossings
- ✅ **All paths routed**: Every connection has a clear, non-crossing path

### Quality Thresholds
- **Excellent**: 0 crossings by all methods
- **Good**: 0 mathematical crossings, minor visual artifacts only
- **Fair**: 1-2 mathematical crossings in complex areas
- **Poor**: 3+ crossings or major visual overlaps
- **Unacceptable**: Multiple obvious crossings visible in screenshot

## Common Issues and Solutions

### Issue: False Positives at Endpoints
**Symptom**: Detections at valid node-to-node connections  
**Fix**: Increase endpoint tolerance from 0.01 to 0.05

### Issue: Missed Visual Crossings
**Symptom**: Math says no crossings but visually there are  
**Fix**: Check stroke width, arrow markers, and rendering artifacts

### Issue: Paths Too Close to Nodes
**Symptom**: Proximity warnings but no crossings  
**Fix**: Add routing margins, use wider node spacing

## Best Practices

1. **Test Early and Often**: Run detection after every layout change
2. **Use All Three Methods**: Don't rely on just mathematical analysis
3. **Document Findings**: Save screenshots and coordinate data
4. **Iterative Refinement**: Fix one crossing at a time, re-test after each fix
5. **Validate Visually**: Always check rendered output, not just data

## Example Output

```
CROSSING DETECTION REPORT
=========================

Method 1: Parametric Line Intersection
- Paths analyzed: 30
- Crossings detected: 0
- Status: ✅ PASS

Method 2: Node-Path Proximity  
- Nodes analyzed: 25
- Proximity issues: 0
- Status: ✅ PASS

Method 3: Visual Screenshot Analysis
- Resolution: 1920x2000px
- Visual crossings: 0
- Status: ✅ PASS

OVERALL: ✅ NO CROSSINGS DETECTED
Diagram has a clean, crossing-free layout.
```
