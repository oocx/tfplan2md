#!/usr/bin/env python3
"""
Diagram Crossing Detection Script

Detects edge crossings and overlaps in SVG workflow diagrams using:
1. Node-path proximity analysis (segment-by-segment)
2. Path-path intersection detection
3. Shared endpoint/overlapping segment detection

Usage:
    python3 detect_crossings.py <svg_file_or_html_file>
    
Example:
    python3 detect_crossings.py website/ai-workflow.html
"""

import re
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Optional


@dataclass
class Node:
    """Represents a rectangular node in the diagram."""
    name: str
    x: float
    y: float
    width: float
    height: float
    
    @property
    def x_min(self) -> float:
        return self.x
    
    @property
    def x_max(self) -> float:
        return self.x + self.width
    
    @property
    def y_min(self) -> float:
        return self.y
    
    @property
    def y_max(self) -> float:
        return self.y + self.height
    
    def contains_point(self, px: float, py: float, exclude_edges: bool = True) -> bool:
        """Check if a point is inside this node (optionally excluding edges)."""
        if exclude_edges:
            return self.x_min < px < self.x_max and self.y_min < py < self.y_max
        return self.x_min <= px <= self.x_max and self.y_min <= py <= self.y_max


@dataclass
class Point:
    """A 2D point."""
    x: float
    y: float
    
    def __eq__(self, other):
        if not isinstance(other, Point):
            return False
        return abs(self.x - other.x) < 0.01 and abs(self.y - other.y) < 0.01
    
    def __hash__(self):
        return hash((round(self.x), round(self.y)))


@dataclass
class Segment:
    """A line segment from p1 to p2."""
    p1: Point
    p2: Point
    
    @property
    def is_horizontal(self) -> bool:
        return abs(self.p1.y - self.p2.y) < 0.01
    
    @property
    def is_vertical(self) -> bool:
        return abs(self.p1.x - self.p2.x) < 0.01
    
    @property
    def x_min(self) -> float:
        return min(self.p1.x, self.p2.x)
    
    @property
    def x_max(self) -> float:
        return max(self.p1.x, self.p2.x)
    
    @property
    def y_min(self) -> float:
        return min(self.p1.y, self.p2.y)
    
    @property
    def y_max(self) -> float:
        return max(self.p1.y, self.p2.y)
    
    def __repr__(self):
        if self.is_horizontal:
            return f"H-Seg(y={self.p1.y}, x:{self.x_min}-{self.x_max})"
        elif self.is_vertical:
            return f"V-Seg(x={self.p1.x}, y:{self.y_min}-{self.y_max})"
        else:
            return f"D-Seg({self.p1.x},{self.p1.y})->({self.p2.x},{self.p2.y})"


@dataclass
class PathDef:
    """A path definition with its segments."""
    name: str
    d_attr: str
    segments: list[Segment]
    
    @property
    def start_point(self) -> Optional[Point]:
        if self.segments:
            return self.segments[0].p1
        return None
    
    @property
    def end_point(self) -> Optional[Point]:
        if self.segments:
            return self.segments[-1].p2
        return None


@dataclass
class Issue:
    """A detected issue."""
    issue_type: str
    severity: str  # "error", "warning", "info"
    description: str
    path_name: str
    segment_index: Optional[int] = None
    node_name: Optional[str] = None
    other_path_name: Optional[str] = None


def ranges_overlap(a_min: float, a_max: float, b_min: float, b_max: float) -> bool:
    """Check if two 1D ranges overlap (exclusive of exact edge touches)."""
    return a_min < b_max and b_min < a_max


def segment_crosses_node(segment: Segment, node: Node, is_start: bool = False, is_end: bool = False) -> bool:
    """
    Check if a segment crosses through a node's interior.
    
    Excludes valid connection points (segment starts/ends at node edge).
    """
    if segment.is_vertical:
        # Vertical segment at x, spanning y_min to y_max
        x = segment.p1.x
        # Check if x is within node's x-range
        if not (node.x_min < x < node.x_max):
            return False
        # Check if y-range overlaps with node's y-range
        if not ranges_overlap(segment.y_min, segment.y_max, node.y_min, node.y_max):
            return False
        # It's a crossing unless it's a valid endpoint connection
        if is_start and (abs(segment.p1.y - node.y_max) < 1 or abs(segment.p1.y - node.y_min) < 1):
            # Starts at node edge - check if the rest passes through
            pass
        if is_end and (abs(segment.p2.y - node.y_max) < 1 or abs(segment.p2.y - node.y_min) < 1):
            # Ends at node edge - might be valid
            if segment.y_min >= node.y_min and segment.y_max <= node.y_max:
                return False  # Segment ends at node, doesn't pass through
        return True
        
    elif segment.is_horizontal:
        # Horizontal segment at y, spanning x_min to x_max
        y = segment.p1.y
        # Check if y is within node's y-range
        if not (node.y_min < y < node.y_max):
            return False
        # Check if x-range overlaps with node's x-range
        if not ranges_overlap(segment.x_min, segment.x_max, node.x_min, node.x_max):
            return False
        # It's a crossing unless it's a valid endpoint connection
        if is_end and (abs(segment.p2.x - node.x_max) < 1 or abs(segment.p2.x - node.x_min) < 1):
            if segment.x_min >= node.x_min and segment.x_max <= node.x_max:
                return False
        return True
        
    else:
        # Diagonal segment - use line-rectangle intersection
        return diagonal_crosses_node(segment, node)


def diagonal_crosses_node(segment: Segment, node: Node) -> bool:
    """Check if a diagonal segment crosses through a node."""
    # Simplified: check if line intersects any of the 4 edges of the rectangle
    # This is a basic implementation - could be more sophisticated
    
    # Check if both endpoints are outside the node on the same side
    p1_inside = node.contains_point(segment.p1.x, segment.p1.y, exclude_edges=False)
    p2_inside = node.contains_point(segment.p2.x, segment.p2.y, exclude_edges=False)
    
    if p1_inside or p2_inside:
        return True
    
    # Check intersection with each edge of the rectangle
    edges = [
        Segment(Point(node.x_min, node.y_min), Point(node.x_max, node.y_min)),  # top
        Segment(Point(node.x_max, node.y_min), Point(node.x_max, node.y_max)),  # right
        Segment(Point(node.x_min, node.y_max), Point(node.x_max, node.y_max)),  # bottom
        Segment(Point(node.x_min, node.y_min), Point(node.x_min, node.y_max)),  # left
    ]
    
    for edge in edges:
        if segments_intersect(segment, edge):
            return True
    
    return False


def segments_intersect(s1: Segment, s2: Segment) -> bool:
    """
    Check if two line segments intersect using parametric equations.
    Returns True if they intersect at an interior point (not at endpoints).
    """
    x1, y1 = s1.p1.x, s1.p1.y
    x2, y2 = s1.p2.x, s1.p2.y
    x3, y3 = s2.p1.x, s2.p1.y
    x4, y4 = s2.p2.x, s2.p2.y
    
    denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4)
    
    if abs(denom) < 1e-10:
        # Lines are parallel
        return False
    
    t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denom
    u = -((x1 - x2) * (y1 - y3) - (y1 - y2) * (x1 - x3)) / denom
    
    # Interior intersection (excluding endpoints with small tolerance)
    tolerance = 0.01
    return tolerance < t < (1 - tolerance) and tolerance < u < (1 - tolerance)


def parse_path_d(d_attr: str) -> list[Point]:
    """Parse SVG path 'd' attribute into a list of points."""
    points = []
    
    # Extract all numbers from the path
    # Handle M (move to) and L (line to) commands
    d_attr = d_attr.strip()
    
    # Split into commands
    commands = re.findall(r'([MLHVZmlhvz])\s*([^MLHVZmlhvz]*)', d_attr)
    
    current_x, current_y = 0.0, 0.0
    
    for cmd, args in commands:
        args = args.strip()
        if not args and cmd.upper() not in ['Z']:
            continue
            
        numbers = [float(n) for n in re.findall(r'-?\d+\.?\d*', args)]
        
        if cmd == 'M':  # Move to (absolute)
            if len(numbers) >= 2:
                current_x, current_y = numbers[0], numbers[1]
                points.append(Point(current_x, current_y))
                # Additional coordinate pairs are implicit L commands
                for i in range(2, len(numbers) - 1, 2):
                    current_x, current_y = numbers[i], numbers[i + 1]
                    points.append(Point(current_x, current_y))
                    
        elif cmd == 'm':  # Move to (relative)
            if len(numbers) >= 2:
                current_x += numbers[0]
                current_y += numbers[1]
                points.append(Point(current_x, current_y))
                    
        elif cmd == 'L':  # Line to (absolute)
            for i in range(0, len(numbers) - 1, 2):
                current_x, current_y = numbers[i], numbers[i + 1]
                points.append(Point(current_x, current_y))
                
        elif cmd == 'l':  # Line to (relative)
            for i in range(0, len(numbers) - 1, 2):
                current_x += numbers[i]
                current_y += numbers[i + 1]
                points.append(Point(current_x, current_y))
                
        elif cmd == 'H':  # Horizontal line to (absolute)
            for n in numbers:
                current_x = n
                points.append(Point(current_x, current_y))
                
        elif cmd == 'h':  # Horizontal line to (relative)
            for n in numbers:
                current_x += n
                points.append(Point(current_x, current_y))
                
        elif cmd == 'V':  # Vertical line to (absolute)
            for n in numbers:
                current_y = n
                points.append(Point(current_x, current_y))
                
        elif cmd == 'v':  # Vertical line to (relative)
            for n in numbers:
                current_y += n
                points.append(Point(current_x, current_y))
                
        elif cmd.upper() == 'Z':  # Close path
            if points:
                points.append(points[0])
    
    return points


def points_to_segments(points: list[Point]) -> list[Segment]:
    """Convert a list of points to line segments."""
    segments = []
    for i in range(len(points) - 1):
        segments.append(Segment(points[i], points[i + 1]))
    return segments


def extract_nodes_from_svg(svg_content: str) -> list[Node]:
    """Extract all node rectangles from SVG content."""
    nodes = []
    
    # Find all <g class="node ..."> groups with their content
    group_pattern = r'<g\s+class="node[^"]*"[^>]*>(.*?)</g>'
    groups = re.findall(group_pattern, svg_content, re.DOTALL)
    
    for group_content in groups:
        # Extract rect attributes
        rect_match = re.search(
            r'<rect[^>]*\s+x="([^"]+)"[^>]*\s+y="([^"]+)"[^>]*\s+width="([^"]+)"[^>]*\s+height="([^"]+)"',
            group_content
        )
        if not rect_match:
            # Try different attribute order
            rect_match = re.search(
                r'<rect[^>]*>',
                group_content
            )
            if rect_match:
                rect_str = rect_match.group(0)
                x = re.search(r'x="([^"]+)"', rect_str)
                y = re.search(r'y="([^"]+)"', rect_str)
                w = re.search(r'width="([^"]+)"', rect_str)
                h = re.search(r'height="([^"]+)"', rect_str)
                if x and y and w and h:
                    rect_match = type('obj', (object,), {
                        'group': lambda self, i: [None, x.group(1), y.group(1), w.group(1), h.group(1)][i]
                    })()
        
        if rect_match:
            # Extract node name from text content
            text_match = re.search(r'<text[^>]*>([^<]+)</text>', group_content)
            name = text_match.group(1).strip() if text_match else "Unknown"
            # Clean up name (remove emoji prefixes)
            name = re.sub(r'^[^\w]+', '', name).strip()
            
            try:
                nodes.append(Node(
                    name=name,
                    x=float(rect_match.group(1)),
                    y=float(rect_match.group(2)),
                    width=float(rect_match.group(3)),
                    height=float(rect_match.group(4))
                ))
            except (ValueError, AttributeError):
                pass
    
    return nodes


def extract_paths_from_svg(svg_content: str) -> list[PathDef]:
    """Extract all path definitions from SVG content."""
    paths = []
    
    # Find all <path> elements (not inside <defs>)
    # First, remove <defs> section
    svg_no_defs = re.sub(r'<defs>.*?</defs>', '', svg_content, flags=re.DOTALL)
    
    # Find paths with their d attributes
    path_pattern = r'<!--\s*([^>]+?)\s*-->\s*<path\s+d="([^"]+)"'
    matches = re.findall(path_pattern, svg_no_defs)
    
    for i, (comment, d_attr) in enumerate(matches):
        name = comment.strip()[:50]  # Use comment as path name
        points = parse_path_d(d_attr)
        segments = points_to_segments(points)
        if segments:
            paths.append(PathDef(name=name, d_attr=d_attr, segments=segments))
    
    # Also find paths without preceding comments
    all_paths = re.findall(r'<path\s+d="([^"]+)"[^>]*>', svg_no_defs)
    existing_d_attrs = {p.d_attr for p in paths}
    
    for d_attr in all_paths:
        if d_attr not in existing_d_attrs:
            points = parse_path_d(d_attr)
            segments = points_to_segments(points)
            if segments:
                # Generate name from path shape
                if len(points) >= 2:
                    name = f"Path({points[0].x},{points[0].y})->({points[-1].x},{points[-1].y})"
                else:
                    name = f"Path_{len(paths) + 1}"
                paths.append(PathDef(name=name, d_attr=d_attr, segments=segments))
    
    return paths


def detect_node_crossings(paths: list[PathDef], nodes: list[Node]) -> list[Issue]:
    """Detect all path segments that cross through nodes."""
    issues = []
    
    for path in paths:
        for seg_idx, segment in enumerate(path.segments):
            is_start = (seg_idx == 0)
            is_end = (seg_idx == len(path.segments) - 1)
            
            for node in nodes:
                if segment_crosses_node(segment, node, is_start, is_end):
                    issues.append(Issue(
                        issue_type="node_crossing",
                        severity="error",
                        description=f"Segment {seg_idx + 1} ({segment}) passes through node",
                        path_name=path.name,
                        segment_index=seg_idx,
                        node_name=node.name
                    ))
    
    return issues


def detect_path_intersections(paths: list[PathDef]) -> list[Issue]:
    """Detect intersections between different paths."""
    issues = []
    
    for i, path1 in enumerate(paths):
        for j, path2 in enumerate(paths):
            if i >= j:
                continue
            
            for seg1_idx, seg1 in enumerate(path1.segments):
                for seg2_idx, seg2 in enumerate(path2.segments):
                    if segments_intersect(seg1, seg2):
                        issues.append(Issue(
                            issue_type="path_intersection",
                            severity="error",
                            description=f"Segment {seg1_idx + 1} intersects with {path2.name} segment {seg2_idx + 1}",
                            path_name=path1.name,
                            segment_index=seg1_idx,
                            other_path_name=path2.name
                        ))
    
    return issues


def detect_shared_endpoints(paths: list[PathDef]) -> list[Issue]:
    """Detect paths that share start or end points."""
    issues = []
    
    # Group paths by their endpoints
    start_points: dict[tuple, list[str]] = {}
    end_points: dict[tuple, list[str]] = {}
    
    for path in paths:
        if path.start_point:
            key = (round(path.start_point.x), round(path.start_point.y))
            start_points.setdefault(key, []).append(path.name)
        if path.end_point:
            key = (round(path.end_point.x), round(path.end_point.y))
            end_points.setdefault(key, []).append(path.name)
    
    # Check for shared start points
    for point, path_names in start_points.items():
        if len(path_names) > 1:
            issues.append(Issue(
                issue_type="shared_start",
                severity="warning",
                description=f"Multiple paths share start point at ({point[0]}, {point[1]}): {', '.join(path_names)}",
                path_name=path_names[0],
                other_path_name=path_names[1] if len(path_names) > 1 else None
            ))
    
    # Check for shared end points
    for point, path_names in end_points.items():
        if len(path_names) > 1:
            issues.append(Issue(
                issue_type="shared_end",
                severity="info",
                description=f"Multiple paths share end point at ({point[0]}, {point[1]}): {', '.join(path_names)}",
                path_name=path_names[0],
                other_path_name=path_names[1] if len(path_names) > 1 else None
            ))
    
    return issues


def detect_overlapping_segments(paths: list[PathDef]) -> list[Issue]:
    """Detect path segments that overlap (share the same line)."""
    issues = []
    
    for i, path1 in enumerate(paths):
        for j, path2 in enumerate(paths):
            if i >= j:
                continue
            
            for seg1_idx, seg1 in enumerate(path1.segments):
                for seg2_idx, seg2 in enumerate(path2.segments):
                    # Check for overlapping horizontal segments
                    if seg1.is_horizontal and seg2.is_horizontal:
                        if abs(seg1.p1.y - seg2.p1.y) < 1:  # Same y
                            if ranges_overlap(seg1.x_min, seg1.x_max, seg2.x_min, seg2.x_max):
                                issues.append(Issue(
                                    issue_type="segment_overlap",
                                    severity="warning",
                                    description=f"Horizontal segments overlap at y={seg1.p1.y}",
                                    path_name=path1.name,
                                    segment_index=seg1_idx,
                                    other_path_name=path2.name
                                ))
                    
                    # Check for overlapping vertical segments
                    elif seg1.is_vertical and seg2.is_vertical:
                        if abs(seg1.p1.x - seg2.p1.x) < 1:  # Same x
                            if ranges_overlap(seg1.y_min, seg1.y_max, seg2.y_min, seg2.y_max):
                                issues.append(Issue(
                                    issue_type="segment_overlap",
                                    severity="warning",
                                    description=f"Vertical segments overlap at x={seg1.p1.x}",
                                    path_name=path1.name,
                                    segment_index=seg1_idx,
                                    other_path_name=path2.name
                                ))
    
    return issues


def print_node_inventory(nodes: list[Node]) -> None:
    """Print the node inventory table."""
    print("\n" + "=" * 70)
    print("NODE INVENTORY")
    print("=" * 70)
    print(f"{'Node Name':<25} {'x-min':>8} {'x-max':>8} {'y-min':>8} {'y-max':>8}")
    print("-" * 70)
    for node in sorted(nodes, key=lambda n: (n.y, n.x)):
        print(f"{node.name:<25} {node.x_min:>8.0f} {node.x_max:>8.0f} {node.y_min:>8.0f} {node.y_max:>8.0f}")
    print()


def print_path_inventory(paths: list[PathDef]) -> None:
    """Print the path inventory with segments."""
    print("\n" + "=" * 70)
    print("PATH INVENTORY")
    print("=" * 70)
    for path in paths:
        print(f"\n{path.name}")
        print(f"  d=\"{path.d_attr[:60]}{'...' if len(path.d_attr) > 60 else ''}\"")
        print(f"  Segments ({len(path.segments)}):")
        for i, seg in enumerate(path.segments):
            print(f"    {i + 1}. {seg}")
    print()


def print_issues(issues: list[Issue]) -> None:
    """Print detected issues."""
    print("\n" + "=" * 70)
    print("DETECTED ISSUES")
    print("=" * 70)
    
    if not issues:
        print("\n✅ No issues detected! Diagram has a clean layout.\n")
        return
    
    errors = [i for i in issues if i.severity == "error"]
    warnings = [i for i in issues if i.severity == "warning"]
    infos = [i for i in issues if i.severity == "info"]
    
    if errors:
        print(f"\n❌ ERRORS ({len(errors)}):")
        print("-" * 50)
        for issue in errors:
            print(f"  • [{issue.issue_type}] {issue.path_name}")
            if issue.node_name:
                print(f"    → Crosses: {issue.node_name}")
            if issue.other_path_name:
                print(f"    → With: {issue.other_path_name}")
            print(f"    {issue.description}")
            print()
    
    if warnings:
        print(f"\n⚠️  WARNINGS ({len(warnings)}):")
        print("-" * 50)
        for issue in warnings:
            print(f"  • [{issue.issue_type}] {issue.description}")
            print()
    
    if infos:
        print(f"\nℹ️  INFO ({len(infos)}):")
        print("-" * 50)
        for issue in infos:
            print(f"  • {issue.description}")
            print()


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)
    
    file_path = Path(sys.argv[1])
    if not file_path.exists():
        print(f"Error: File not found: {file_path}")
        sys.exit(1)
    
    content = file_path.read_text()
    
    # Extract SVG content if it's an HTML file
    svg_match = re.search(r'<svg[^>]*>.*?</svg>', content, re.DOTALL)
    if svg_match:
        svg_content = svg_match.group(0)
    else:
        svg_content = content
    
    print("=" * 70)
    print("DIAGRAM CROSSING DETECTION REPORT")
    print("=" * 70)
    print(f"File: {file_path}")
    
    # Extract nodes and paths
    nodes = extract_nodes_from_svg(svg_content)
    paths = extract_paths_from_svg(svg_content)
    
    print(f"Nodes found: {len(nodes)}")
    print(f"Paths found: {len(paths)}")
    
    # Print inventories
    print_node_inventory(nodes)
    print_path_inventory(paths)
    
    # Run all detections
    all_issues = []
    
    print("\nRunning detection algorithms...")
    
    # Method 1: Node-path crossings
    node_issues = detect_node_crossings(paths, nodes)
    print(f"  • Node-path proximity: {len(node_issues)} issues")
    all_issues.extend(node_issues)
    
    # Method 2: Path-path intersections
    intersection_issues = detect_path_intersections(paths)
    print(f"  • Path-path intersections: {len(intersection_issues)} issues")
    all_issues.extend(intersection_issues)
    
    # Method 3: Shared endpoints
    endpoint_issues = detect_shared_endpoints(paths)
    print(f"  • Shared endpoints: {len(endpoint_issues)} issues")
    all_issues.extend(endpoint_issues)
    
    # Method 4: Overlapping segments
    overlap_issues = detect_overlapping_segments(paths)
    print(f"  • Overlapping segments: {len(overlap_issues)} issues")
    all_issues.extend(overlap_issues)
    
    # Print results
    print_issues(all_issues)
    
    # Summary
    print("=" * 70)
    print("SUMMARY")
    print("=" * 70)
    errors = len([i for i in all_issues if i.severity == "error"])
    warnings = len([i for i in all_issues if i.severity == "warning"])
    
    if errors == 0 and warnings == 0:
        print("✅ PASS - No crossing issues detected")
        sys.exit(0)
    elif errors == 0:
        print(f"⚠️  WARNINGS - {warnings} warnings, no blocking errors")
        sys.exit(0)
    else:
        print(f"❌ FAIL - {errors} errors, {warnings} warnings")
        sys.exit(1)


if __name__ == "__main__":
    main()
