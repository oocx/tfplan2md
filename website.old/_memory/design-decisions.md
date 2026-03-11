# Website Design Decisions

This document captures specific design decisions made during the website development.

## Theme Configuration

### Dual Theme Support
- **Light mode**: Variation 1 (GitHub-inspired colors)
- **Dark mode**: Variation 2 (High contrast dark theme)
- Theme preference persisted in `localStorage`

### Theme Toggle
- Must be **clearly recognizable** as dark/light mode toggle
- Previous banana-shaped design was rejected
- Current: CSS-only sun/moon icons

## Logo Design

### Icon Geometry
- **Concept**: Start with the "Diff Card" (dark background, `+ - ~` symbols) and transition to "Markdown" (white card, stacked on top).
- **Style**: Overlapping cards, symbolizing transformation.
- **Diff Card**: Dark Grey (`#1e1e1e`) with standard syntax colors (`+` green, `-` red, `~` yellow).
- **Markdown Card**: White card overlapping the Diff Card.
- **Alignment**: "Compact Overlap" (X=70 offset) creates a tight, unified symbol.

### Logo Text (Logotype)
- **Font**: Inter ExtraBold (`font-weight: 800`).
- **Structure**: Two-Tone color scheme.
  - `tfplan`: Dark Grey / Theme Text Color (anchors the source).
  - `2md`: Brand Purple `#844fba` (highlights the destination/value).
- **Layout**: Text placed to the right of the icon, vertically centered.

## Visual Hierarchy

### Section Separation
- **Clear background differences** between sections
- Dark mode must have visible background differences (not same color for all sections)
- Problem section needs distinct background separating it from hero and solution

### Spacing
- Reduce vertical whitespace to show more content without scrolling
- Section container padding: 40px (reduced from 80px)
- Hero headlines should render without line breaks

## Style Isolation (ADR-004)

### Problem
Website CSS was interfering with rendered examples (e.g., `border-radius: 6px` on `details` elements made examples look different from actual Azure DevOps rendering).

### Decision
Use **CSS Layers (`@layer`)** to isolate example styles from website styles.

### Alternatives Considered
1. **iframe Isolation** - Complete separation but more complex (requires JavaScript for height management, separate HTML files)
2. **Shadow DOM** - Native encapsulation but requires JavaScript and has quirks
3. **CSS Layers** ✅ - Chosen for agent-maintainability, declarative CSS, modern best practice
4. **CSS Reset (all: revert)** - Too aggressive with unpredictable side effects
5. **Strict BEM/Namespacing** - Doesn't provide true isolation (tag selectors still interfere)
6. **Separate Stylesheet + containment** - `contain: style` is too limited (only counters/quotes)

### Implementation
```css
@layer base, website, examples;

@layer website {
  /* All website styles */
}

@layer examples {
  .rendered-view { /* Example styles */ }
}
```

See [ADR-004](../../docs/adr-004-css-layers-for-example-isolation.md) for full rationale.

### Iconography
- **Style**: Duotone SVG with `#646cff` (Brand Blue) and `currentColor` (Black/Text).
- **Dark Mode Compatibility**: All icons use a "White Halo" technique (white stroke behind black lines) to ensure visibility on dark backgrounds while remaining invisible on light backgrounds.
- **Stroke Width**: 
  - Structural lines: 2px (Black) / 4px (White Halo)
  - Detail lines: 1.5px (Black) / 3.5px (White Halo)

## Homepage Layout

### Badge Row (Top)
Position at top, above main headline:
- "Built 100% with GitHub Copilot" (links to AI workflow page)
- "Docker Ready"
- "Free & Open Source"

### Hero Section
- Main headline renders without line break
- Subtitle renders without line break
- Centered alignment

### Problem Section
- **Single row layout** (4 columns, not 2x2 grid) - uses less vertical space
- Screenshot of terraform plan (hard to read example)
- Problem list with red X icons
- Different background color from hero/solution

### Solution Section
- Screenshot of tfplan2md report
- Feature list with green checkmarks
- Features shown:
  - Firewall & NSG Rule Tables → Security changes rendered as readable tables
  - Friendly Names, Not GUIDs → Principal IDs, role names, and scopes resolved to readable text
  - Inline Diffs for Large Values → JSON policies and scripts with highlighted changes
  - Optimized for PR Comments → Designed and tested for GitHub and Azure DevOps rendering

### Call to Action Section
- Get Started button
- View Examples button
- Docker run command (inline, same row as buttons)
- Placed **after** problem/solution (user sees value first)

### Features Section
- Carousel on desktop (shows 3 features at once)
- Consistent with features page categorization

## Features Page Layout

### Section Headlines
| Value Level | Section Name |
|-------------|--------------|
| High | "What Sets Us Apart" |
| Medium | "Built-In Capabilities" |
| Low | "Also Included" |

### Card Sizing
- "Also Included" cards should be **smaller** than other feature cards
- Reduced spacing between feature sections (50% less than initial)
- Reduced spacing between page title and first section (40% less)

## Logo

### Current State
- Current logo not suitable for small sizes
- Needs complete **redesign** (not based on old one)
- Must work at navbar scale

### Requirements (TBD)
- Recognizable at small sizes
- Works in both light and dark modes
- Represents the tool's purpose

## Navigation

### Desktop
- All main pages in top navbar
- Features, Install, Docs, Examples, Providers, Architecture, Contributing
- GitHub CTA button on right

### Mobile
- Hamburger menu
- All navigation items accessible

## Responsive Design

### Breakpoints
| Breakpoint | Behavior |
|------------|----------|
| 1024px | Tablet - collapse grids to 2 columns |
| 768px | Small tablet - adjust typography |
| 640px | Mobile - single column layouts |

### Mobile Fixes Applied
- Remove `white-space: nowrap` from hero-subtitle
- Fix `width: max-content` in hero-title
- Ensure screenshots don't overflow
- Pipeline integration examples responsive

## Preserved Design Artifacts

**Instruction from maintainer:** "Don't throw any of them away, including the ones we did not select. I'd like to view them again later."

All design variations should be preserved for future reference:
- Design 1-6 (initial explorations)
- Variations 1-6 based on Design 6
- All intermediate states

## Decision Log

- 2026-01-03: Initial design decisions documented from chat session analysis.
- 2026-01-03: Theme toggle redesigned (banana shape rejected).
- 2026-01-03: Problem section changed from 2x2 to single row.
- 2026-01-03: CTA moved below problem/solution sections.
- 2026-01-03: "Also Included" cards sized smaller.
