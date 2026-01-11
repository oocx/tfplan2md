# Website Style Guide

This document captures **design and style decisions** for the tfplan2md website.

## Principles

- Keep the site technical and example-driven (no marketing fluff).
- Prefer simple, maintainable HTML/CSS.
- Use consistent spacing, typography, and component patterns across pages.

## Theme Support

- **Light mode**: GitHub-inspired colors (`data-theme="light"`)
- **Dark mode**: Dark theme with high contrast (`data-theme="dark"`)
- Theme toggle in navbar with aria-label for accessibility
- Theme preference persisted in `localStorage`

## Color Tokens

### Light Mode (`:root[data-theme="light"]`)

| Token | Value | Usage |
|-------|-------|-------|
| `--color-bg` | `#ffffff` | Page background |
| `--color-surface` | `#f6f8fa` | Card/section backgrounds |
| `--color-text` | `#24292f` | Primary text |
| `--color-text-secondary` | `#57606a` | Secondary/muted text |
| `--color-border` | `#d0d7de` | Borders and dividers |
| `--color-primary` | `#844fba` | Links, buttons, accents (purple brand color) |
| `--color-primary-hover` | `#6a3f96` | Primary hover state |
| `--color-accent` | `#1f883d` | Success/accent color (green) |
| `--color-success` | `#1a7f37` | Success states |
| `--code-bg` | `#eaeef2` | Code block backgrounds |
| `--code-header-bg` | `#d0d7de` | Code block headers |
| `--code-text` | `#24292f` | Code text color |

### Dark Mode (`:root[data-theme="dark"]`)

| Token | Value | Usage |
|-------|-------|-------|
| `--color-bg` | `#0d1117` | Page background |
| `--color-surface` | `#161b22` | Card/section backgrounds |
| `--color-text` | `#e6edf3` | Primary text |
| `--color-text-secondary` | `#7d8590` | Secondary/muted text |
| `--color-border` | `#30363d` | Borders and dividers |
| `--color-primary` | `#a371f7` | Links, buttons, accents (purple) |
| `--color-primary-hover` | `#d2a8ff` | Primary hover state |
| `--color-accent` | `#a371f7` | Accent color (purple) |
| `--color-success` | `#3fb950` | Success states |
| `--code-bg` | `#1a202c` | Code block backgrounds |
| `--code-header-bg` | `#2d3748` | Code block headers |
| `--code-text` | `#e2e8f0` | Code text color |

### Shared Tokens

| Token | Value | Usage |
|-------|-------|-------|
| `--font-sans` | System font stack | Body text |
| `--font-mono` | Monospace font stack | Code |
| `--shadow-sm` | `0 1px 3px rgba(0,0,0,0.12)` | Subtle elevation |
| `--shadow-md` | `0 4px 6px rgba(0,0,0,0.1)` | Card hover |
| `--shadow-lg` | `0 10px 20px rgba(0,0,0,0.15)` | Modal/overlay |
| `--shadow-xl` | `0 20px 30px rgba(0,0,0,0.2)` | Large elements |
| `--color-mark-purple` | `#844fba` | Brand color (Logo `2md` text) |
| `--color-diff-add` | `#2da44e` | Diff `+` green |
| `--color-diff-del` | `#cf222e` | Diff `-` red |
| `--color-diff-chg` | `#d29922` | Diff `~` yellow |
| `--color-diff-bg` | `#1e1e1e` | Diff Code Block Background |

## Logo Design

- **Primary Logo**: Icon + Text (Two-Tone).
- **Font**: `Inter` ExtraBold (Weight 800).
- **Colors**: `tfplan` (Text Color), `2md` (Brand Purple).
- **Icon**: Stacked Diff Card (Dark) + Markdown Card (White/Purple).

## Layout

- **Max width**: `1280px` (`.nav-container`, `.hero-container`, `.section-container`)
- **Horizontal padding**: `24px` on containers
- **Section padding**: `80px 0` vertical (`.section`)

## Breakpoints

| Breakpoint | Usage |
|------------|-------|
| `1024px` | Tablet/medium screens - collapse grids to 2 columns |
| `768px` | Small tablets - adjust typography and spacing |
| `640px` | Mobile - single column layouts |

## Typography

- **Font stack (sans)**: `-apple-system, BlinkMacSystemFont, "Segoe UI", "Noto Sans", Helvetica, Arial, sans-serif`
- **Font stack (mono)**: `ui-monospace, SFMono-Regular, "SF Mono", Menlo, Consolas, monospace`
- **Base line-height**: `1.6`

### Heading Sizes

| Element | Size | Weight | Letter-spacing |
|---------|------|--------|----------------|
| `.hero-title` | `48px` | `700` | `-0.02em` |
| `.section-title` | `40px` | `700` | `-0.02em` |
| `.feature-category-title` | (cards) | `600` | — |
| `h3` in cards | `20px` | `600` | — |

## Navigation

- Sticky navbar with blur backdrop (`backdrop-filter: blur(10px)`)
- Mobile hamburger menu at `768px` breakpoint
- Theme toggle button with CSS-only sun/moon icons
- Primary CTA button styled with `.nav-cta`

## Components

### Cards (`.feature-category-card`)
- Background: `--color-surface`
- Border: `1px solid --color-border`
- Border radius: `16px`
- Padding: `32px`
- Hover: `translateY(-4px)` + `--shadow-md`

### Buttons (`.btn`)
- Padding: `12px 32px` (standard), `16px 40px` (`.btn-lg`)
- Border radius: `6px`
- Variants: `.btn-primary`, `.btn-secondary`

### Feature Icons (`.feature-icon`)
- Size: `48px` font-size
- Margin-bottom: `16px`

### Compact Cards (`.feature-category-card.compact`)
- Flexbox layout with icon on left
- Smaller icon (`.feature-icon-sm`)

## Icons and Images

- Feature representation must follow `website/_memory/feature-definitions.md`
- Do not reuse the same icon/image for different features
- SVG icons are stored in `website/assets/icons/`
- All icons use the **White Halo** technique (white stroke behind black stroke for visibility on any background)

### Dark Mode for Icons (IMPORTANT)

Icons must work correctly in both light and dark modes. Because the site uses `data-theme="dark"` on the `<html>` element (rather than `prefers-color-scheme`), SVGs loaded via `<img>` tags cannot access the parent document's theme state.

**Solution**: Apply a CSS filter to invert icon colors in dark mode:

```css
[data-theme="dark"] .feature-icon,
[data-theme="dark"] .feature-icon-lg,
[data-theme="dark"] .feature-icon-sm {
    filter: invert(1) hue-rotate(180deg);
}
```

**Requirements for icons to work in dark mode:**

1. All `<img>` elements displaying icons MUST use one of these classes:
   - `.feature-icon` (64×64px, for features page)
   - `.feature-icon-lg` (48px font-size equivalent, for homepage)
   - `.feature-icon-sm` (32×32px, for compact cards)

2. If you add a new icon class, you MUST add it to the dark mode filter rule in `style.css`

3. Do NOT rely on `@media (prefers-color-scheme: dark)` inside SVG files — it won't work because the site uses `data-theme` attribute-based theming

**Why this approach:**
- SVGs loaded via `<img>` are sandboxed and cannot access parent document CSS variables or attributes
- The `invert(1) hue-rotate(180deg)` filter inverts colors while preserving hue relationships
- This keeps icons looking correct (black becomes white, white becomes black)

## Accessibility

- Theme toggle has `aria-label` and `title` attributes
- Mobile menu button has `aria-label`
- Semantic heading hierarchy (h1 → h2 → h3)
- Focus indicators on interactive elements
- Color contrast targets WCAG 2.1 AA

## Footer

- 3-column grid layout (Product, Resources, Community)
- Copyright and attribution line at bottom
- Links styled with `.footer-link`

## Decision Log

- 2026-01-03: Initial style guide created based on the current `website/style.css` design system.
- 2026-01-03: Documented all color tokens, breakpoints, typography, and component patterns.
