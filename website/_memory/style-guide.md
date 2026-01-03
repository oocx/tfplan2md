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
| `--color-primary` | `#0969da` | Links, buttons, accents |
| `--color-primary-hover` | `#0860ca` | Primary hover state |
| `--color-accent` | `#1f883d` | Success/accent color |
| `--color-success` | `#1a7f37` | Success states |

### Dark Mode (`:root[data-theme="dark"]`)

| Token | Value | Usage |
|-------|-------|-------|
| `--color-bg` | `#0d1117` | Page background |
| `--color-surface` | `#161b22` | Card/section backgrounds |
| `--color-text` | `#e6edf3` | Primary text |
| `--color-text-secondary` | `#7d8590` | Secondary/muted text |
| `--color-border` | `#30363d` | Borders and dividers |
| `--color-primary` | `#58a6ff` | Links, buttons, accents |
| `--color-primary-hover` | `#79c0ff` | Primary hover state |
| `--color-accent` | `#a371f7` | Accent color (purple) |
| `--color-success` | `#3fb950` | Success states |

### Shared Tokens

| Token | Value | Usage |
|-------|-------|-------|
| `--font-sans` | System font stack | Body text |
| `--font-mono` | Monospace font stack | Code |
| `--shadow-sm` | `0 1px 3px rgba(0,0,0,0.12)` | Subtle elevation |
| `--shadow-md` | `0 4px 6px rgba(0,0,0,0.1)` | Card hover |
| `--shadow-lg` | `0 10px 20px rgba(0,0,0,0.15)` | Modal/overlay |
| `--shadow-xl` | `0 20px 30px rgba(0,0,0,0.2)` | Large elements |

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
- Icons use emoji for simplicity (no external icon library)
- SVG icons planned for `website/assets/icons/` but not yet created

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
