#!/usr/bin/env python3
"""
Comprehensive SVG Diagram Crossing Detection

This script detects edge crossings and overlaps in SVG workflow diagrams using
multiple methods: parametric line intersection, node-path proximity analysis,
and prepares data for visual inspection.

Usage:
    python3 detect_all.py <path-to-html-file>
"""

import re
import sys
from typing import List, Tuple, Dict
import json

def parse_path_to_segments(d: str) -> List[Tuple[Tuple[float, float], Tuple[float, float]]]:
    """Parse SVG path 'd' attribute into line segments"""
    segments = []
    points = []
    
    # Split by commands (M=moveto, L=lineto, Z=closepath)
    parts = re.split(r'([MLZ])', d)
    
    for part in parts:
        part = part.strip()
        if not part or part in ['M', 'L', 'Z']:
            continue
        coords = [float(x) for x in part.split()]
        if len(coords) >= 2:
            for i in range(0, len(coords), 2):
                if i + 1 < len(coords):
                    points.append((coords[i], coords[i+1]))
    
    # Create segments from consecutive points
    for i in range(len(points) - 1):
        segments.append((points[i], points[i+1]))
    
    return segments

def segments_intersect(seg1: Tuple[Tuple[float, float], Tuple[float, float]], 
                      seg2: Tuple[Tuple[float, float], Tuple[float, float]],
                      tolerance: float = 0.01) -> Tuple[bool, Dict]:
    """
    Check if two line segments intersect using parametric equations.
    
    Args:
        seg1: First line segment ((x1, y1), (x2, y2))
        seg2: Second line segment ((x3, y3), (x4, y4))
        tolerance: Minimum distance from endpoints (as fraction of segment length)
    
    Returns:
        (intersects, details) where details contains intersection information
    """
    p1, p2 = seg1
    p3, p4 = seg2
    
    x1, y1 = p1
    x2, y2 = p2
    x3, y3 = p3
    x4, y4 = p4
    
    # Check if segments share an endpoint (allowed - valid connections)
    endpoint_threshold = 1.0
    if (abs(x1 - x3) < endpoint_threshold and abs(y1 - y3) < endpoint_threshold) or \
       (abs(x1 - x4) < endpoint_threshold and abs(y1 - y4) < endpoint_threshold) or \
       (abs(x2 - x3) < endpoint_threshold and abs(y2 - y3) < endpoint_threshold) or \
       (abs(x2 - x4) < endpoint_threshold and abs(y2 - y4) < endpoint_threshold):
        return False, {}
    
    # Calculate intersection using parametric line equations
    # Line 1: P = P1 + t(P2 - P1)
    # Line 2: P = P3 + u(P4 - P3)
    denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4)
    
    if abs(denom) < 0.001:  # Parallel or collinear
        return False, {}
    
    t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denom
    u = -((x1 - x2) * (y1 - y3) - (y1 - y2) * (x1 - x3)) / denom
    
    # Calculate intersection point
    intersect_x = x1 + t * (x2 - x1)
    intersect_y = y1 + t * (y2 - y1)
    
    # Check if intersection is within both segments (with tolerance)
    if tolerance < t < (1 - tolerance) and tolerance < u < (1 - tolerance):
        return True, {
            't': t,
            'u': u,
            'point': (intersect_x, intersect_y),
            'seg1': seg1,
            'seg2': seg2
        }
    
    return False, {}

def extract_svg_content(html_file: str) -> str:
    """Extract SVG content from HTML file"""
    with open(html_file, 'r') as f:
        content = f.read()
    
    start_idx = content.find('<svg')
    if start_idx == -1:
        raise ValueError("No SVG found in HTML file")
    
    end_idx = content.find('</svg>', start_idx) + 6
    return content[start_idx:end_idx]

def extract_paths(svg_content: str) -> List[Dict]:
    """Extract all path elements from SVG"""
    path_pattern = r'<path d="([^"]+)"[^>]*(?:stroke="([^"]*)")?[^>]*(?:marker-end="([^"]*)")?[^>]*>'
    paths = []
    
    for match in re.finditer(path_pattern, svg_content):
        d = match.group(1)
        stroke = match.group(2) if match.group(2) else 'unknown'
        
        # Skip very short paths (likely markers or patterns)
        if 'M 20 0' in d or 'M0,0' in d or len(d) < 10:
            continue
        
        paths.append({
            'd': d,
            'stroke': stroke,
            'segments': parse_path_to_segments(d)
        })
    
    return paths

def extract_nodes(svg_content: str) -> List[Dict]:
    """Extract all node bounding boxes from SVG"""
    node_pattern = r'<rect[^>]*x="(\d+)"[^>]*y="(\d+)"[^>]*width="(\d+)"[^>]*height="(\d+)"'
    nodes = []
    
    for match in re.finditer(node_pattern, svg_content):
        x, y, w, h = map(int, match.groups())
        # Skip very small rects (likely decorations)
        if w > 50 and h > 20:
            nodes.append({
                'x': x,
                'y': y,
                'width': w,
                'height': h,
                'x2': x + w,
                'y2': y + h
            })
    
    return nodes

def detect_line_crossings(paths: List[Dict], tolerance: float = 0.01) -> List[Dict]:
    """Detect all line segment crossings between paths"""
    crossings = []
    
    for i in range(len(paths)):
        for j in range(i + 1, len(paths)):
            path1 = paths[i]
            path2 = paths[j]
            
            for seg1 in path1['segments']:
                for seg2 in path2['segments']:
                    intersects, details = segments_intersect(seg1, seg2, tolerance)
                    if intersects:
                        crossings.append({
                            'path1_idx': i + 1,
                            'path2_idx': j + 1,
                            'path1_stroke': path1['stroke'],
                            'path2_stroke': path2['stroke'],
                            'intersection_point': details['point'],
                            'parameters': {'t': details['t'], 'u': details['u']},
                            'path1_d': path1['d'][:100],
                            'path2_d': path2['d'][:100]
                        })
    
    return crossings

def check_path_node_proximity(paths: List[Dict], nodes: List[Dict], margin: float = 5) -> List[Dict]:
    """Check for paths passing through node bodies"""
    proximity_issues = []
    
    for path_idx, path in enumerate(paths, 1):
        for seg in path['segments']:
            # Check both endpoints of each segment
            for point in [seg[0], seg[1]]:
                px, py = point
                
                for node_idx, node in enumerate(nodes, 1):
                    # Check if point is inside node boundary
                    if (node['x'] - margin <= px <= node['x2'] + margin and 
                        node['y'] - margin <= py <= node['y2'] + margin):
                        
                        # Check if it's actually on an edge (valid connection)
                        is_on_edge = (
                            abs(px - node['x']) <= margin or
                            abs(px - node['x2']) <= margin or
                            abs(py - node['y']) <= margin or
                            abs(py - node['y2']) <= margin
                        )
                        
                        if not is_on_edge:
                            # Path point is inside node body
                            proximity_issues.append({
                                'path_idx': path_idx,
                                'node_idx': node_idx,
                                'point': point,
                                'node_bounds': node
                            })
    
    return proximity_issues

def main():
    if len(sys.argv) != 2:
        print("Usage: python3 detect_all.py <path-to-html-file>")
        sys.exit(1)
    
    html_file = sys.argv[1]
    
    print("=" * 80)
    print("SVG DIAGRAM CROSSING DETECTION")
    print("=" * 80)
    print(f"\nAnalyzing: {html_file}\n")
    
    # Extract SVG content
    svg_content = extract_svg_content(html_file)
    
    # Extract paths and nodes
    paths = extract_paths(svg_content)
    nodes = extract_nodes(svg_content)
    
    print(f"Extracted {len(paths)} paths")
    print(f"Extracted {len(nodes)} nodes\n")
    
    print("=" * 80)
    print("METHOD 1: PARAMETRIC LINE INTERSECTION")
    print("=" * 80)
    
    crossings = detect_line_crossings(paths, tolerance=0.01)
    
    print(f"\nCrossings detected: {len(crossings)}")
    
    if crossings:
        print("\n❌ CROSSINGS FOUND:")
        for idx, crossing in enumerate(crossings, 1):
            print(f"\n[Crossing #{idx}]")
            print(f"  Path {crossing['path1_idx']} (color: {crossing['path1_stroke']})")
            print(f"  × Path {crossing['path2_idx']} (color: {crossing['path2_stroke']})")
            print(f"  Intersection at: ({crossing['intersection_point'][0]:.1f}, {crossing['intersection_point'][1]:.1f})")
            print(f"  Parameters: t={crossing['parameters']['t']:.3f}, u={crossing['parameters']['u']:.3f}")
    else:
        print("\n✅ No mathematical crossings detected")
    
    print("\n" + "=" * 80)
    print("METHOD 2: NODE-PATH PROXIMITY ANALYSIS")
    print("=" * 80)
    
    proximity_issues = check_path_node_proximity(paths, nodes, margin=5)
    
    print(f"\nProximity issues detected: {len(proximity_issues)}")
    
    if proximity_issues:
        print("\n⚠️  PROXIMITY ISSUES:")
        for idx, issue in enumerate(proximity_issues[:10], 1):
            print(f"\n[Issue #{idx}]")
            print(f"  Path {issue['path_idx']} passes through Node {issue['node_idx']}")
            print(f"  At point: ({issue['point'][0]:.1f}, {issue['point'][1]:.1f})")
    else:
        print("\n✅ No path-node proximity issues detected")
    
    print("\n" + "=" * 80)
    print("SUMMARY")
    print("=" * 80)
    print(f"\nTotal paths analyzed: {len(paths)}")
    print(f"Total nodes analyzed: {len(nodes)}")
    print(f"Mathematical crossings: {len(crossings)}")
    print(f"Proximity issues: {len(proximity_issues)}")
    
    if len(crossings) == 0 and len(proximity_issues) == 0:
        print("\n✅ OVERALL STATUS: NO ISSUES DETECTED")
        print("The diagram has a clean, crossing-free layout.")
    else:
        print("\n❌ OVERALL STATUS: ISSUES DETECTED")
        print("The diagram requires layout adjustments.")
    
    print("\n" + "=" * 80)

if __name__ == '__main__':
    main()
