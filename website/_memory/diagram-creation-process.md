# Diagram Creation Process Documentation

## Overview
This document describes how custom SVG diagrams were created for the tfplan2md website, specifically the AI workflow diagram on [ai-workflow.html](../ai-workflow.html).

## Design Inspiration
The diagram style was inspired by **Design 7: Blueprint** from the [diagram-designs-svg.html](../prototypes/diagram-designs-svg.html) prototype collection. This design uses a technical drawing aesthetic with:
- Dark blue background (#0d1b2a)
- Blueprint grid pattern
- White/cyan lines on dark background
- Monospace fonts (technical/engineering feel)
- Dashed borders for artifacts
- Solid borders for agents
- Cyan (#00ffff) accent color

## Creation Process

### 1. Research & Design Selection
**What was done:**
- Reviewed 38 existing diagram design prototypes in `diagram-designs-svg.html`
- Selected Design 7 (Blueprint) for its technical, professional aesthetic
- Analyzed the design patterns: grid backgrounds, stroke styles, color palette, typography

**Why this design:**
- Technical drawing style aligns with infrastructure/DevOps audience
- High contrast (white/cyan on dark) ensures readability
- Grid pattern provides spatial reference without clutter
- Monospace fonts reinforce technical/code context

### 2. Layout Planning
**What was done:**
- Mapped out the complex agent workflow structure:
  - Human (Maintainer) at top center
  - Three entry agents (Requirements Engineer, Issue Analyst, Workflow Engineer)
  - Vertical flow through planning, quality, development stages
  - Branching paths for parallel workflows (Tech Writer + Code Reviewer)
  - Decision points (rework loops, conditional UAT)
  
**Challenges addressed:**
- Large number of nodes (13+ agents, 12+ artifacts)
- Multiple paths and feedback loops
- Need to maintain visual clarity despite complexity
- Mobile responsiveness (viewBox approach)

### 3. SVG Implementation
**Technical approach:**
1. **SVG Canvas**: Used `viewBox="0 0 1200 1600"` for scalable vector graphics
2. **Defs Section**: Defined reusable patterns and markers:
   ```xml
   <pattern id="grid-blueprint-workflow">  <!-- Blueprint grid -->
   <marker id="arrow-blueprint-cyan">      <!-- Arrow heads -->
   ```
3. **Layering Strategy**:
   - Background grid first
   - Connecting paths second
   - Node rectangles and text on top
   - Legend/annotations last

4. **Node Types**:
   - **Agents**: Solid cyan borders (`stroke="#00ffff"`)
   - **Artifacts**: Dashed white borders (`stroke-dasharray="3,3"`)
   - **Human**: Heavy dashed white border (unique style)
   - **Meta-agent**: Green borders (`stroke="#10b981"` for Workflow Engineer)

5. **Path Styling**:
   - Solid lines: Primary flow
   - Dashed lines: Optional/conditional paths
   - Different arrow marker colors for visual hierarchy

### 4. Typography & Readability
**Font choices:**
- `font-family="monospace"` for all text (technical aesthetic)
- `font-weight="700"` for primary labels
- `font-size="15-18"` for node names (readable at zoom levels)
- Uppercase text for emphasis (`ARCHITECT`, `DEVELOPER`)

**Accessibility:**
- High contrast ratios (WCAG AA compliant)
- Sufficient font sizes (min 11px even at smallest labels)
- Clear visual hierarchy through size and weight

### 5. Integration
**How it was integrated:**
- Replaced the existing Mermaid.js diagram in `ai-workflow.html`
- Added dark background container (`background: #0d1b2a`) around SVG
- Set responsive width (`width: 100%`) and max-width constraint
- Centered with `margin: 0 auto`

**Advantages over Mermaid:**
- Full design control (exact colors, grid, typography)
- No external library dependency for this specific diagram
- Faster rendering (no JavaScript parsing)
- Design consistency with prototype collection

## Key SVG Techniques Used

### Blueprint Grid Pattern
```xml
<pattern id="grid-blueprint-workflow" width="20" height="20" patternUnits="userSpaceOnUse">
    <path d="M 20 0 L 0 0 0 20" fill="none" stroke="rgba(255,255,255,0.1)" stroke-width="0.5"/>
</pattern>
<rect width="1200" height="1600" fill="url(#grid-blueprint-workflow)"/>
```
Creates repeating grid background for technical drawing feel.

### Arrow Markers
```xml
<marker id="arrow-blueprint-cyan" markerWidth="8" markerHeight="8" refX="7" refY="4" orient="auto">
    <path d="M0,0 L0,8 L8,4 z" fill="#00ffff"/>
</marker>
<path stroke="#00ffff" marker-end="url(#arrow-blueprint-cyan)"/>
```
Reusable arrow heads that automatically orient along paths.

### Dashed Borders (Artifacts)
```xml
<rect stroke="#ffffff" stroke-width="2" stroke-dasharray="3,3"/>
```
Visual distinction between agents (solid) and artifacts (dashed).

### Color Coding
- **Cyan (#00ffff)**: Primary agent color and flow lines
- **White (#ffffff)**: Secondary borders, human node, text
- **Green (#10b981)**: Meta-agent (Workflow Engineer)
- **Semi-transparent fills**: `rgba(0,255,255,0.1)` for artifact backgrounds

## Maintenance & Future Updates

### Adding New Nodes
1. Copy existing node group `<g class="node agent">` or `<g class="node artifact">`
2. Update position with `x` and `y` attributes
3. Update text content
4. Add connecting path with appropriate markers

### Updating Paths
- Path syntax: `<path d="M x1 y1 L x2 y2" />`
  - `M`: Move to starting point
  - `L`: Draw line to point
- Add `marker-end="url(#arrow-blueprint-cyan)"` for arrows
- Use `stroke-dasharray="5,5"` for conditional/feedback paths

### Responsive Considerations
- The `viewBox` attribute ensures the diagram scales proportionally
- Container has `max-width: 1200px` to prevent excessive stretching on large screens
- Mobile users can pan/zoom on the SVG

## Related Files
- Main diagram: [website/ai-workflow.html](../ai-workflow.html)
- Design prototypes: [website/prototypes/diagram-designs-svg.html](../prototypes/diagram-designs-svg.html)
- Style guide: [website/_memory/style-guide.md](style-guide.md)

## Lessons Learned
1. **Start with a clear layout sketch**: Complex workflows need spatial planning before coding
2. **Use consistent spacing**: 60px node height, 10-20px padding, 60-70px vertical gaps
3. **Group related elements**: Use `<g>` tags to organize nodes conceptually
4. **Test at multiple zoom levels**: Ensure text remains readable when zoomed out
5. **Keep SVG source readable**: Add comments and consistent indentation for maintainability

## Future Improvements
- [ ] Add interactive hover states (highlight paths related to hovered node)
- [ ] Implement collapsible sections for mobile (show/hide detail levels)
- [ ] Add annotations/tooltips explaining each agent's role
- [ ] Create light mode variant (though blueprint style works best on dark)
- [ ] Generate diagram programmatically from agent definitions (if workflow changes frequently)
