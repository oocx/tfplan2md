"""Regression tests for the workflow diagram renderer."""

from __future__ import annotations

import importlib.util
import sys
import unittest
from pathlib import Path


SCRIPT = Path(__file__).parents[1] / "render-workflow-diagram.py"
sys.path.insert(0, str(SCRIPT.parent))
SPEC = importlib.util.spec_from_file_location("render_workflow_diagram", SCRIPT)
assert SPEC and SPEC.loader
renderer = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(renderer)


RAW_SVG = """\
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 800 600">
  <g class="root" transform="matrix(1, 0, 0, 1, 400, 25)">
    <g class="clusters">
      <g class="cluster" id="diagram-FEATURE">
        <rect x="8" y="8" width="300" height="500"/>
        <g class="cluster-label" transform="translate(120, 8)">
          <foreignObject><div><span class="nodeLabel"><p>&amp;lt;b&amp;gt;Feature&amp;lt;/b&amp;gt;</p></span></div></foreignObject>
        </g>
      </g>
    </g>
    <g class="edgePaths">
      <path d="M10,20L30,40" id="diagram-L_A_B_0" class="edge-thickness-normal edge-pattern-solid flowchart-link"/>
      <path d="M300,20L330,20" id="diagram-L_FEATURE_BUG_0" class="edge-thickness-invisible edge-pattern-solid"/>
    </g>
    <g class="nodes">
      <g class="node default role" id="diagram-flowchart-A-0" transform="translate(50, 75)">
        <g class="basic label-container"><rect class="basic label-container" x="-20" y="-10" width="40" height="20"/></g>
        <g class="label"><foreignObject><div><span class="nodeLabel"><p>&amp;lt;i&amp;gt;Alpha&amp;lt;/i&amp;gt;</p></span></div></foreignObject></g>
      </g>
    </g>
  </g>
</svg>
"""


class WorkflowDiagramRendererTests(unittest.TestCase):
    """Protect Mermaid layout and semantic elements during restyling."""

    def test_clean_label_strips_entity_escaped_html(self) -> None:
        """Escaped formatting from Mermaid must not leak into SVG text."""
        self.assertEqual(["Alpha", "detail"], renderer.clean_label("&lt;b&gt;Alpha&lt;/b&gt;&lt;br/&gt;&lt;i&gt;detail&lt;/i&gt;"))

    def test_build_svg_retains_clusters_and_nested_layout(self) -> None:
        """Lane frames, titles, and global translations must survive restyling."""
        svg = renderer.build_svg(RAW_SVG, "flowchart LR")

        self.assertIn('<g class="cluster" transform="translate(400.000, 25.000)">', svg)
        self.assertIn('<rect x="8.000" y="8.000" width="300.000" height="500.000"/>', svg)
        self.assertIn('class="cluster-title" x="120.000" y="25.000">Feature</text>', svg)
        self.assertIn('<g class="node-agent" transform="translate(450.000, 100.000)">', svg)
        self.assertIn('>Alpha</text>', svg)

    def test_build_svg_preserves_visible_path_and_omits_invisible_layout_edge(self) -> None:
        """Ordering links influence Mermaid layout without becoming cyan arrows."""
        svg = renderer.build_svg(RAW_SVG, "flowchart LR")

        self.assertIn(
            '<path class="path-solid" marker-end="url(#arrow-solid)" '
            'transform="translate(400.000, 25.000)" d="M10,20L30,40"/>',
            svg,
        )
        self.assertNotIn('M300,20L330,20', svg)

if __name__ == "__main__":
    unittest.main()
