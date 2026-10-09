"""Extract Mermaid geometry and labels for the blueprint workflow renderer."""

from __future__ import annotations

import html
import re
import xml.etree.ElementTree as ET


# classDef name in the mermaid source -> CSS class in the blueprint SVG.
NODE_CLASS = {
    "role": "node-agent",
    "meta": "node-metaagent",
    "artifact": "node-artifact",
    "gate": "node-gate",
    "external": "node-external",
    "default": "node-human",
}


def clean_label(raw: str) -> list[str]:
    """Return a Mermaid HTML label as plain text lines."""
    # Mermaid may entity-escape formatting once or twice depending on whether
    # the label came through an HTML label or XML text node. Decode before
    # removing tags so escaped <b>, <i>, and <br> markup cannot leak through.
    while (decoded := html.unescape(raw)) != raw:
        raw = decoded
    raw = re.sub(r"<br\s*/?>", "\n", raw)
    raw = re.sub(r"<[^>]+>", "", raw)
    return [line for text in raw.split("\n") if (line := text.strip())]


def _classes(element: ET.Element) -> set[str]:
    """Return an SVG element's CSS classes."""
    return set(element.attrib.get("class", "").split())


def _translation(value: str) -> tuple[float, float]:
    """Extract the translation component Mermaid uses for nested roots."""
    translate = re.search(r"translate\(\s*([-\d.]+)(?:[ ,]+([-\d.]+))?\s*\)", value)
    if translate:
        return float(translate.group(1)), float(translate.group(2) or 0)
    matrix = re.search(
        r"matrix\(\s*1(?:\.0+)?[ ,]+0(?:\.0+)?[ ,]+0(?:\.0+)?[ ,]+1(?:\.0+)?"
        r"[ ,]+([-\d.]+)[ ,]+([-\d.]+)\s*\)",
        value,
    )
    return (float(matrix.group(1)), float(matrix.group(2))) if matrix else (0.0, 0.0)


def _walk(element: ET.Element, x: float = 0, y: float = 0):
    """Yield SVG elements with composed ancestor translations."""
    dx, dy = _translation(element.attrib.get("transform", ""))
    x, y = x + dx, y + dy
    yield element, x, y
    for child in element:
        yield from _walk(child, x, y)


def _plain_text(element: ET.Element) -> list[str]:
    """Flatten a Mermaid HTML label while retaining explicit line breaks."""
    parts: list[str] = []

    def visit(current: ET.Element) -> None:
        if current.text:
            parts.append(current.text)
        for child in current:
            if child.tag.rsplit("}", 1)[-1].lower() == "br":
                parts.append("\n")
            else:
                visit(child)
            if child.tail:
                parts.append(child.tail)

    visit(element)
    return clean_label("".join(parts))


def _parse_svg(svg: str) -> ET.Element:
    """Parse Mermaid SVG or raise a useful renderer diagnostic."""
    try:
        return ET.fromstring(svg)
    except ET.ParseError as error:
        raise ValueError(f"invalid mermaid SVG: {error}") from error


def parse_nodes(svg: str) -> list[dict]:
    """Extract nodes with nested Mermaid translations composed globally."""
    nodes = []
    for element, x, y in _walk(_parse_svg(svg)):
        classes = _classes(element)
        match = re.search(r"flowchart-([A-Za-z0-9_]+)-\d+$", element.attrib.get("id", ""))
        if "node" not in classes or not match:
            continue
        node_id = match.group(1)
        kind = next((name for name in classes if name in NODE_CLASS and name != "default"),
                    "default")
        descendants = list(element.iter())
        rect = next((item for item in descendants
                     if item.tag.rsplit("}", 1)[-1] == "rect"
                     and {"basic", "label-container"} <= _classes(item)), None)
        polygon = next((item for item in descendants
                        if item.tag.rsplit("}", 1)[-1] == "polygon"), None)
        outer = next((item for item in descendants if "outer-path" in _classes(item)), None)
        stadium = next((item.attrib.get("d") for item in outer.iter()
                        if item.tag.rsplit("}", 1)[-1] == "path"), None) if outer is not None else None
        if rect is not None:
            shape = ("rect", tuple(float(rect.attrib[name])
                                   for name in ("x", "y", "width", "height")),
                     _translation(rect.attrib.get("transform", "")))
        elif polygon is not None and polygon.attrib.get("points"):
            shape = ("polygon", polygon.attrib["points"],
                     _translation(polygon.attrib.get("transform", "")))
        elif stadium:
            # Mermaid's stadium path control points vary slightly per render.
            # Derive its rounded box so strict same-machine checks stay stable.
            coords = [float(value) for value in re.findall(r"-?\d+\.?\d*", stadium)]
            xs, ys = coords[0::2], coords[1::2]
            x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
            shape = ("stadium", (x0, y0, x1 - x0, y1 - y0), (0.0, 0.0))
        else:
            continue
        label = next((item for item in descendants if "nodeLabel" in _classes(item)), None)
        lines = _plain_text(label) if label is not None else [node_id]
        nodes.append({"id": node_id, "kind": kind, "x": x, "y": y,
                      "shape": shape, "lines": lines})
    return nodes


def parse_edges(svg: str) -> list[dict]:
    """Extract visible edges while preserving Mermaid's path data."""
    edges = []
    for element, x, y in _walk(_parse_svg(svg)):
        classes = _classes(element)
        edge_id = element.attrib.get("id", "")
        if not element.attrib.get("d") or "edge-thickness-invisible" in classes or "-L_" not in edge_id:
            continue
        dashed = bool({"edge-pattern-dotted", "edge-pattern-dashed"} & classes)
        edges.append({"d": element.attrib["d"], "dashed": dashed, "x": x, "y": y})
    return edges


def parse_edge_labels(svg: str) -> list[dict]:
    """Extract edge labels with nested Mermaid translations composed."""
    labels = []
    for element, x, y in _walk(_parse_svg(svg)):
        if element.tag.rsplit("}", 1)[-1] != "g" or _classes(element) != {"edgeLabel"}:
            continue
        text = next((item for item in element.iter() if "edgeLabel" in _classes(item)
                     and item is not element), None)
        lines = _plain_text(text) if text is not None else []
        if lines:
            labels.append({"x": x, "y": y, "lines": lines})
    return labels


def parse_clusters(svg: str) -> list[dict]:
    """Extract Mermaid subgraph frames and titles."""
    clusters = []
    for element, x, y in _walk(_parse_svg(svg)):
        if element.tag.rsplit("}", 1)[-1] != "g" or _classes(element) != {"cluster"}:
            continue
        rect = next((child for child in element
                     if child.tag.rsplit("}", 1)[-1] == "rect"), None)
        label = next((child for child in element if "cluster-label" in _classes(child)), None)
        if rect is None or label is None:
            continue
        text = next((item for item in label.iter() if "nodeLabel" in _classes(item)), None)
        lines = _plain_text(text) if text is not None else []
        label_x, label_y = _translation(label.attrib.get("transform", ""))
        foreign = next((item for item in label.iter()
                        if item.tag.rsplit("}", 1)[-1] == "foreignObject"), None)
        label_width = float(foreign.attrib.get("width", 0)) if foreign is not None else 0.0
        clusters.append({
            "x": x,
            "y": y,
            "label_x": label_x + label_width / 2,
            "label_y": label_y,
            "rect": tuple(float(rect.attrib[name]) for name in ("x", "y", "width", "height")),
            "lines": lines,
        })
    return clusters
