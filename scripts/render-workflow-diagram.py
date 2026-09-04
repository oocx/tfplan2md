#!/usr/bin/env python3
"""Render the workflow mermaid diagram into the website's blueprint-styled SVG.

The website shows a technical-drawing rendering of the workflow: cyan on dark
navy, over a grid. Its layout must match mermaid's exactly — positions and edge
curves are extracted from a mermaid render rather than recomputed, because
hand-placed nodes drift from the diagram they claim to depict.

    docs/workflow.md  ```mermaid``` block
      -> mmdc (layout)
      -> this script (styling)
      -> website/src/media-root/ai-workflow.svg

Usage:
    scripts/render-workflow-diagram.py            extract, render and restyle
    scripts/render-workflow-diagram.py --check    fail if the SVG is out of date
"""

from __future__ import annotations

import html
import json
import re
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
SOURCE_DOC = REPO / "docs" / "workflow.md"
TARGET_SVG = REPO / "website" / "src" / "media-root" / "ai-workflow.svg"
MERMAID_VERSION = "11.17.0"

# classDef name in the mermaid source -> CSS class in the blueprint SVG.
NODE_CLASS = {
    "role": "node-agent",
    "meta": "node-metaagent",
    "artifact": "node-artifact",
    "gate": "node-gate",
    "external": "node-external",
    "default": "node-human",
}

STYLE = """
    /* Blueprint: a technical drawing of the workflow, not a UI. Node role is
       carried by border colour and dash pattern so it survives greyscale. */
    .bg { fill: #0d1b2a; }
    .grid { fill: url(#grid-blueprint); }

    g[class^="node-"] rect,
    g[class^="node-"] polygon,
    g[class^="node-"] path { filter: drop-shadow(0 0 4px rgba(0,255,255,0.45)); }

    .node-agent      rect { fill: rgba(0,255,255,0.06); stroke: #00ffff; stroke-width: 2; }
    .node-agent      text { fill: #ffffff; }

    .node-metaagent  rect { fill: rgba(52,211,153,0.08); stroke: #34d399; stroke-width: 2; }
    .node-metaagent  text { fill: #34d399; }

    .node-external   rect { fill: rgba(244,114,182,0.08); stroke: #f472b6; stroke-width: 2; }
    .node-external   text { fill: #f9a8d4; }

    .node-artifact   rect { fill: rgba(0,255,255,0.10); stroke: #ffffff; stroke-width: 2;
                            stroke-dasharray: 2,2; }
    .node-artifact   text { fill: #00ffff; }

    .node-gate       polygon { fill: rgba(251,191,36,0.10); stroke: #fbbf24; stroke-width: 2.5; }
    .node-gate       text    { fill: #fbbf24; }

    .node-human      rect,
    .node-human      path { fill: rgba(0,255,255,0.05); stroke: #ffffff; stroke-width: 2;
                            stroke-dasharray: 4,2; }
    .node-human      text { fill: #00ffff; }

    g[class^="node-"] text {
        font-family: ui-monospace, "SF Mono", Menlo, monospace;
        font-size: 13px;
        text-anchor: middle;
    }
    g[class^="node-"] text.sub { font-size: 11px; opacity: 0.75; }

    .path-solid  { fill: none; stroke: #00ffff; stroke-width: 2;
                   filter: drop-shadow(0 0 2px rgba(0,255,255,0.4)); }
    .path-dashed { fill: none; stroke: #ff6b6b; stroke-width: 2; stroke-dasharray: 6,3;
                   filter: drop-shadow(0 0 2px rgba(255,107,107,0.35)); }

    .edge-label {
        font-family: ui-monospace, "SF Mono", Menlo, monospace;
        font-size: 11px;
        fill: #9fb3c8;
        text-anchor: middle;
    }
"""


def die(msg: str) -> None:
    print(f"error: {msg}", file=sys.stderr)
    sys.exit(1)


def extract_mermaid() -> str:
    text = SOURCE_DOC.read_text(encoding="utf-8")
    m = re.search(r"^```mermaid\n(.*?)^```", text, re.S | re.M)
    if not m:
        die(f"no ```mermaid block in {SOURCE_DOC}")
    return m.group(1)


def render_with_mermaid(mmd: str, workdir: Path) -> str:
    src = workdir / "workflow.mmd"
    out = workdir / "raw.svg"
    src.write_text(mmd, encoding="utf-8")
    # Pinned: this script parses mermaid's SVG markup, so an upstream release can
    # change the output and break --check with no change in this repository.
    # Bumping the pin can require a matching puppeteer Chrome — if the render
    # fails with "Could not find Chrome", run:
    #     npx puppeteer browsers install chrome-headless-shell
    if shutil.which("mmdc"):
        cmd = ["mmdc"]
    else:
        cmd = ["npx", "--yes", "-p", f"@mermaid-js/mermaid-cli@{MERMAID_VERSION}", "mmdc"]
    args = cmd + ["-i", str(src), "-o", str(out), "-t", "dark"]

    def run(extra: list[str]) -> subprocess.CompletedProcess:
        return subprocess.run(args + extra, capture_output=True, text=True)

    result = run([])

    # GitHub's runners disable unprivileged user namespaces, so Chrome's sandbox
    # cannot start and the render dies before mermaid sees the diagram. Retry
    # without it rather than disabling it everywhere: on a developer machine the
    # sandbox works and is worth keeping. The input is this repository's own
    # diagram source, and the flag does not affect the rendered bytes.
    if result.returncode != 0 and "No usable sandbox" in (result.stderr + result.stdout):
        cfg = workdir / "puppeteer.json"
        cfg.write_text(
            json.dumps({"args": ["--no-sandbox", "--disable-setuid-sandbox"]}),
            encoding="utf-8",
        )
        result = run(["-p", str(cfg)])

    if result.returncode != 0 or not out.exists():
        die(f"mermaid render failed:\n{result.stdout}\n{result.stderr}")
    return out.read_text(encoding="utf-8")


def clean_label(raw: str) -> list[str]:
    """Mermaid labels are HTML. Return them as plain text lines."""
    raw = re.sub(r"<br\s*/?>", "\n", raw)
    raw = re.sub(r"<[^>]+>", "", raw)
    lines = [html.unescape(x).strip() for x in raw.split("\n")]
    return [x for x in lines if x]


def parse_nodes(svg: str) -> list[dict]:
    nodes = []
    pattern = re.compile(
        r'<g class="node ([^"]+)" id="[^"]*flowchart-([A-Za-z0-9_]+)-\d+"'
        r'[^>]*transform="translate\(([-\d.]+),\s*([-\d.]+)\)">(.*?)</g></g>',
        re.S,
    )
    for m in pattern.finditer(svg):
        classes, node_id, x, y, body = m.groups()
        kind = next((c for c in classes.split() if c in NODE_CLASS and c != "default"), "default")

        # Mermaid emits three shape forms, and each needs different handling:
        # a plain rect; a polygon that carries its OWN transform (dropping it
        # leaves the label outside the shape); and a stadium, drawn as a free
        # path inside an "outer-path" group, which matches neither pattern and
        # was silently skipping the node.
        shape = None
        rect = re.search(r'<rect class="basic label-container"[^>]*x="([-\d.]+)" y="([-\d.]+)" '
                         r'width="([\d.]+)" height="([\d.]+)"', body)
        # Grab the whole element first, then pull points and transform out of
        # it separately: combining them into one pattern lets [^>]* swallow the
        # transform, and an optional group the engine is happy to skip means the
        # hexagon silently renders 100px away from its own label.
        poly_el = re.search(r'<polygon\b[^>]*/>', body)
        poly_points = poly_xform = None
        if poly_el:
            el = poly_el.group(0)
            pts = re.search(r'points="([^"]+)"', el)
            xf = re.search(r'transform="translate\(([-\d.]+),\s*([-\d.]+)\)"', el)
            poly_points = pts.group(1) if pts else None
            poly_xform = (float(xf.group(1)), float(xf.group(2))) if xf else (0.0, 0.0)
        stadium = re.search(r'class="[^"]*outer-path"[^>]*>\s*<path d="([^"]+)"', body)
        if rect:
            shape = ("rect", tuple(float(v) for v in rect.groups()), (0.0, 0.0))
        elif poly_points:
            shape = ("polygon", poly_points, poly_xform)
        elif stadium:
            # A stadium is a fully-rounded rectangle, and mermaid draws it as a
            # Bezier path whose control points differ slightly on every render —
            # the one source of nondeterminism in this pipeline, which would make
            # a --check in CI fail at random. Derive the box from the path's
            # extents and emit a rect instead: same shape, stable bytes.
            coords = [float(v) for v in re.findall(r"-?\d+\.?\d*", stadium.group(1))]
            xs, ys = coords[0::2], coords[1::2]
            x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
            shape = ("stadium", (x0, y0, x1 - x0, y1 - y0), (0.0, 0.0))
        if shape is None:
            continue

        label = re.search(r'class="nodeLabel"[^>]*>(.*?)</span>', body, re.S)
        lines = clean_label(label.group(1)) if label else [node_id]
        nodes.append({"id": node_id, "kind": kind, "x": float(x), "y": float(y),
                      "shape": shape, "lines": lines})
    return nodes


def parse_edges(svg: str) -> list[dict]:
    edges = []
    for m in re.finditer(r'<path d="([^"]+)" id="[^"]*L_([A-Za-z0-9_]+)_\d+" class="([^"]*)"', svg):
        d, name, classes = m.groups()
        dashed = "edge-pattern-dotted" in classes or "edge-pattern-dashed" in classes
        edges.append({"d": d, "dashed": dashed, "name": name})
    return edges


def parse_edge_labels(svg: str) -> list[dict]:
    labels = []
    for m in re.finditer(
        r'<g class="edgeLabel"[^>]*transform="translate\(([-\d.]+),\s*([-\d.]+)\)">(.*?)</g></g></g>',
        svg, re.S,
    ):
        x, y, body = m.groups()
        text = re.search(r'class="edgeLabel"[^>]*>(.*?)</span>', body, re.S)
        if not text:
            continue
        lines = clean_label(text.group(1))
        if lines:
            labels.append({"x": float(x), "y": float(y), "lines": lines})
    return labels


def build_svg(raw: str) -> str:
    vb = re.search(r'viewBox="([^"]+)"', raw)
    if not vb:
        die("no viewBox in the mermaid output")
    minx, miny, width, height = (float(v) for v in vb.group(1).split())

    nodes = parse_nodes(raw)
    edges = parse_edges(raw)
    labels = parse_edge_labels(raw)
    if not nodes:
        die("no nodes parsed from the mermaid output — its markup may have changed")

    out: list[str] = []
    out.append(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="{minx} {miny} {width} {height}" '
               'role="graphics-document document" aria-roledescription="flowchart-v2">')
    out.append('  <title>tfplan2md agent workflow</title>')
    out.append("  <defs>")
    out.append('    <pattern id="grid-blueprint" width="20" height="20" patternUnits="userSpaceOnUse">')
    out.append('      <path d="M 20 0 L 0 0 0 20" fill="none" stroke="rgba(255,255,255,0.1)" stroke-width="0.5"/>')
    out.append("    </pattern>")
    for name, colour in (("arrow-solid", "#00ffff"), ("arrow-dashed", "#ff6b6b")):
        out.append(f'    <marker id="{name}" markerWidth="4" markerHeight="4" refX="3.5" refY="2" orient="auto">')
        out.append(f'      <path d="M0,0 L0,4 L4,2 z" fill="{colour}"/>')
        out.append("    </marker>")
    out.append("  </defs>")
    out.append(f"  <style>{STYLE}  </style>")
    out.append(f'  <rect class="bg" x="{minx}" y="{miny}" width="{width}" height="{height}"/>')
    out.append(f'  <rect class="grid" x="{minx}" y="{miny}" width="{width}" height="{height}"/>')

    for e in edges:
        cls = "path-dashed" if e["dashed"] else "path-solid"
        marker = "arrow-dashed" if e["dashed"] else "arrow-solid"
        out.append(f'  <path class="{cls}" marker-end="url(#{marker})" d="{e["d"]}"/>')

    for lab in labels:
        n = len(lab["lines"])
        for i, line in enumerate(lab["lines"]):
            dy = (i - (n - 1) / 2) * 13 + 4
            out.append(f'  <text class="edge-label" x="{lab["x"]:.3f}" y="{lab["y"] + dy:.3f}">'
                       f"{html.escape(line)}</text>")

    for node in nodes:
        cls = NODE_CLASS[node["kind"]]
        out.append(f'  <g class="{cls}" transform="translate({node["x"]:.3f}, {node["y"]:.3f})">')
        kind, geom, (sdx, sdy) = node["shape"]
        shift = f' transform="translate({sdx:.3f}, {sdy:.3f})"' if (sdx or sdy) else ""
        if kind == "rect":
            x, y, w, h = geom
            out.append(f'    <rect x="{x:.3f}" y="{y:.3f}" width="{w:.3f}" height="{h:.3f}" rx="6" ry="6"{shift}/>')
        elif kind == "polygon":
            out.append(f'    <polygon points="{geom}"{shift}/>')
        else:
            x, y, w, h = geom
            out.append(f'    <rect x="{x:.3f}" y="{y:.3f}" width="{w:.3f}" height="{h:.3f}" '
                       f'rx="{h / 2:.3f}" ry="{h / 2:.3f}"{shift}/>')
        n = len(node["lines"])
        for i, line in enumerate(node["lines"]):
            dy = (i - (n - 1) / 2) * 15 + 5
            sub = ' class="sub"' if i > 0 else ""
            out.append(f'    <text{sub} x="0" y="{dy:.3f}">{html.escape(line)}</text>')
        out.append("  </g>")

    out.append("</svg>")
    return "\n".join(out) + "\n"


def main() -> int:
    check = "--check" in sys.argv[1:]
    with tempfile.TemporaryDirectory() as tmp:
        raw = render_with_mermaid(extract_mermaid(), Path(tmp))
    svg = build_svg(raw)

    if check:
        if not TARGET_SVG.exists():
            print(f"FAIL: {TARGET_SVG.relative_to(REPO)} does not exist")
            return 1
        if TARGET_SVG.read_text(encoding="utf-8") != svg:
            print(f"FAIL: {TARGET_SVG.relative_to(REPO)} is out of date with the mermaid "
                  f"diagram in {SOURCE_DOC.relative_to(REPO)}")
            print("Run scripts/render-workflow-diagram.py and commit the result.")
            return 1
        print(f"OK: {TARGET_SVG.relative_to(REPO)} matches the diagram")
        return 0

    TARGET_SVG.parent.mkdir(parents=True, exist_ok=True)
    TARGET_SVG.write_text(svg, encoding="utf-8")
    nodes = svg.count('<g class="node-')
    paths = svg.count("<path class=\"path-")
    print(f"Wrote {TARGET_SVG.relative_to(REPO)}: {nodes} nodes, {paths} edges")
    return 0


if __name__ == "__main__":
    sys.exit(main())
