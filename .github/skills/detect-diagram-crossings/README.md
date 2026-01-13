# Detect Diagram Crossings Skill

This skill provides comprehensive detection of edge crossings and overlaps in SVG workflow diagrams.

## Files

- `SKILL.md` - Main skill documentation with detection methods and usage instructions
- `detect_all.py` - Python script that runs all detection methods

## Quick Usage

```bash
python3 .github/skills/detect-diagram-crossings/detect_all.py website/ai-workflow.html
```

## Detection Methods

1. **Parametric Line Intersection**: Mathematical detection of segment crossings
2. **Node-Path Proximity Analysis**: Detects paths passing through node bodies  
3. **Visual Screenshot Analysis**: Catches rendering artifacts and visual overlaps

## Output

The script reports:
- Number of mathematical crossings detected
- Coordinates and path IDs for each crossing
- Node-path proximity issues
- Overall status (PASS/FAIL)

## Example Output

```
================================================================================
SVG DIAGRAM CROSSING DETECTION
================================================================================

Analyzing: website/ai-workflow.html

Extracted 30 paths
Extracted 25 nodes

================================================================================
METHOD 1: PARAMETRIC LINE INTERSECTION
================================================================================

Crossings detected: 0

✅ No mathematical crossings detected

================================================================================
METHOD 2: NODE-PATH PROXIMITY ANALYSIS
================================================================================

Proximity issues detected: 0

✅ No path-node proximity issues detected

================================================================================
SUMMARY
================================================================================

Total paths analyzed: 30
Total nodes analyzed: 25
Mathematical crossings: 0
Proximity issues: 0

✅ OVERALL STATUS: NO ISSUES DETECTED
The diagram has a clean, crossing-free layout.

================================================================================
```

## Testing

Run the script on the current diagram to verify it has no crossings:

```bash
cd /home/runner/work/tfplan2md/tfplan2md
python3 .github/skills/detect-diagram-crossings/detect_all.py website/ai-workflow.html
```

Expected result: 0 crossings, 0 proximity issues.
