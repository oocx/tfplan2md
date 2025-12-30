# Architecture: GitHub Pages Website

## Status

Proposed

## Context

The tfplan2md project needs a public-facing website to drive adoption, educate users, and build a contributor community. The website must be:

- **Agent-maintainable**: Easy for AI agents to modify via prompts
- **Accessible**: WCAG 2.1 AA compliant
- **Responsive**: Works on mobile, tablet, and desktop
- **Technical**: Example-driven content for developer audience
- **Automated**: Deployed via CI/CD with isolated pipeline triggers

The website will contain 8 main pages (homepage, getting-started, features, providers, examples, docs, architecture, contributing) with a focus on visual examples and real screenshots.

Reference: `docs/features/025-github-pages-website/specification.md`

## Options Considered

### Option 1: Static HTML/CSS (Recommended)

**Description:**
Pure HTML5 + CSS3 files with no build system or preprocessors. All pages are static `.html` files with shared CSS stylesheets and optional vanilla JavaScript for interactive elements.

**Structure:**
```
/website/
├── index.html
├── getting-started.html
├── examples.html
├── docs.html
├── architecture.html
├── contributing.html
├── features/
│   ├── index.html
│   ├── firewall-rules.html
│   ├── nsg-diffing.html
│   ├── role-assignments.html
│   ├── module-grouping.html
│   ├── custom-templates.html
│   ├── sensitive-masking.html
│   ├── large-values.html
│   └── misc.html
├── providers/
│   ├── index.html
│   ├── azurerm.html
│   ├── azuredevops.html
│   ├── azuread.html
│   └── msgraph.html
├── assets/
│   ├── css/
│   │   ├── main.css
│   │   └── responsive.css
│   ├── images/
│   │   └── screenshots/
│   └── js/
│       └── navigation.js (optional)
└── README.md (website development notes)
```

**Pros:**
- ✅ **Maximum agent maintainability**: Agents can directly edit HTML without learning build tools
- ✅ **Zero dependencies**: No npm, bundlers, or preprocessors to manage
- ✅ **Instant preview**: Open HTML files directly in browser during development
- ✅ **GitHub Pages native**: Direct deployment with no build step
- ✅ **Full control**: Complete control over markup and accessibility features
- ✅ **Fast**: No build time, instant deployments
- ✅ **Simple debugging**: Inspect source matches what's written

**Cons:**
- ⚠️ **Code duplication**: Navigation and common elements repeated across pages
- ⚠️ **Manual updates**: Changes to common elements require updating multiple files
- ⚠️ **No templating**: Cannot use variables or includes

**Mitigation:**
- Use consistent naming conventions and structure
- Document update procedures clearly
- Consider lightweight client-side includes via JS if duplication becomes problematic

### Option 2: Jekyll Static Site Generator

**Description:**
GitHub Pages' built-in static site generator. Uses Liquid templates, Markdown for content, and YAML for data files.

**Structure:**
```
/website/
├── _config.yml
├── _layouts/
│   └── default.html
├── _includes/
│   ├── header.html
│   └── footer.html
├── _data/
│   └── navigation.yml
├── index.md
├── getting-started.md
└── ...
```

**Pros:**
- ✅ **Templates**: Reusable layouts reduce duplication
- ✅ **GitHub Pages native**: Automatic build on push
- ✅ **Markdown content**: Easier content editing
- ✅ **Data files**: Structured data for navigation, etc.

**Cons:**
- ❌ **Build complexity**: Agents must understand Jekyll conventions
- ❌ **Liquid syntax**: Additional language for agents to learn
- ❌ **Debugging**: Source code differs from generated HTML
- ❌ **Local preview**: Requires Ruby environment setup
- ❌ **Limited control**: Jekyll conventions may conflict with accessibility requirements
- ❌ **Build time**: Adds deployment latency

### Option 3: Modern Static Site Generator (Hugo, Docusaurus, etc.)

**Description:**
Use a modern JavaScript-based or Go-based static site generator with advanced features.

**Pros:**
- ✅ **Rich features**: Search, versioning, theming
- ✅ **Templates**: Component-based structure
- ✅ **Modern DX**: Hot reload, fast builds

**Cons:**
- ❌ **High complexity**: npm dependencies, build configuration, toolchain
- ❌ **Agent maintainability**: Steep learning curve for AI agents
- ❌ **Overengineered**: Feature overkill for 8 static pages
- ❌ **Build pipeline**: Requires Node.js in CI/CD
- ❌ **Debugging**: Large gap between source and output
- ❌ **Breaking changes**: Framework updates can break builds

## Decision

**Selected: Option 1 - Static HTML/CSS**

## Rationale

The primary requirement is **agent maintainability** — the Web Designer agent must be able to easily modify the site via prompts. Static HTML/CSS provides:

1. **Direct manipulation**: Agents can read and edit exactly what renders in the browser
2. **No cognitive overhead**: No need to learn build tools, template languages, or framework conventions
3. **Predictable behavior**: HTML/CSS standards are stable and well-understood by LLMs
4. **Accessibility control**: Full control over semantic HTML and ARIA attributes
5. **Fast iteration**: No build step means immediate feedback

The cons (code duplication) are acceptable for a small 8-page site. If duplication becomes problematic, we can introduce lightweight client-side includes via vanilla JavaScript without breaking the simple architecture.

This approach aligns with the project's principle of **preferring simple solutions** and avoiding overengineering.

## Consequences

### Positive

- **Agent-friendly**: Web Designer agent can directly edit HTML with minimal context
- **Fast development**: No build tooling setup or configuration
- **Fast deployment**: Direct push to GitHub Pages, no build step
- **Reliable**: No framework updates or dependency vulnerabilities
- **Transparent**: What you write is what you deploy
- **Full accessibility**: Complete control over semantic HTML and WCAG compliance
- **Easy debugging**: Browser DevTools show exactly the source code

### Negative

- **Code duplication**: Navigation, header, footer repeated across pages
  - **Mitigation**: Document update procedures; consider JavaScript includes if needed
- **No templating**: Cannot use variables or partials
  - **Mitigation**: CSS custom properties (`--variables`) for theming
- **Manual consistency**: Must manually keep common elements in sync
  - **Mitigation**: Clear documentation and Web Designer agent training

## Implementation Notes

### File Structure

All website files live in `/website/` directory at repository root:

- **HTML pages**: Direct `.html` files for each page
- **Stylesheets**: Shared CSS in `/website/assets/css/`
- **Images**: Screenshots and assets in `/website/assets/images/`
- **Scripts** (optional): Vanilla JavaScript in `/website/assets/js/`

### CSS Architecture

Use a **utility-first approach with semantic naming**:

- `main.css`: Base styles, typography, colors, components
- `responsive.css`: Media queries for mobile, tablet, desktop breakpoints

**CSS Custom Properties** for theming:
```css
:root {
  --color-primary: #3b82f6;
  --color-text: #1f2937;
  --color-background: #ffffff;
  --font-body: system-ui, -apple-system, sans-serif;
  --font-mono: 'Courier New', monospace;
  --spacing-unit: 8px;
}
```

### HTML Structure

**Consistent structure across all pages:**
```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <meta name="description" content="...">
  <title>Page Title | tfplan2md</title>
  <link rel="stylesheet" href="/assets/css/main.css">
  <link rel="stylesheet" href="/assets/css/responsive.css">
</head>
<body>
  <header>
    <nav aria-label="Main navigation">
      <!-- Navigation menu -->
    </nav>
  </header>
  
  <main>
    <!-- Page content -->
  </main>
  
  <footer>
    <!-- Footer content -->
  </footer>
</body>
</html>
```

### Accessibility Requirements (WCAG 2.1 AA)

1. **Semantic HTML**: Use proper elements (`<nav>`, `<main>`, `<article>`, `<section>`)
2. **Heading hierarchy**: One `<h1>` per page, proper nesting (h1 → h2 → h3)
3. **Alt text**: All images have descriptive `alt` attributes
4. **Keyboard navigation**: All interactive elements are keyboard accessible
5. **Focus indicators**: Visible `:focus` styles for all interactive elements
6. **Color contrast**: 
   - Normal text: 4.5:1 minimum
   - Large text (18pt+ or 14pt+ bold): 3:1 minimum
7. **ARIA labels**: Use `aria-label`, `aria-labelledby` for screen reader context
8. **Skip links**: "Skip to main content" link at top of page
9. **Language**: `lang="en"` on `<html>` element
10. **Responsive text**: Use relative units (`rem`, `em`) not fixed pixels

### Browser Support

**Target browsers:** Latest versions of modern browsers only. No legacy browser support required.

- Chrome/Edge (Chromium-based)
- Firefox
- Safari
- Mobile browsers (iOS Safari, Chrome Mobile)

**Modern web features allowed:**
- CSS Grid, Flexbox, Custom Properties
- ES6+ JavaScript (if needed)
- Native HTML5 elements
- Modern accessibility APIs

**No polyfills or compatibility layers required.**

### Responsive Breakpoints

- **Mobile**: < 768px (single column)
- **Tablet**: 768px - 1023px (adaptive layout)
- **Desktop**: ≥ 1024px (full layout)

Use mobile-first approach: base styles for mobile, `@media (min-width: ...)` for larger screens.

### Navigation Component

**Desktop**: Horizontal menu in header
**Mobile**: Hamburger menu (collapsible, keyboard accessible)

All pages share identical navigation structure (manually maintained across files).

### Content Strategy

- **Extract from existing docs**: README.md, docs/features.md, docs/architecture.md, docs/agents.md, CONTRIBUTING.md
- **Screenshot sources**: Use existing comprehensive demo examples
- **Code snippets**: Copy/paste ready, use `<pre><code>` with syntax highlighting via CSS or lightweight library
- **Before/after comparisons**: Side-by-side on desktop, stacked on mobile

### CI/CD Integration

**New GitHub Actions workflow**: `.github/workflows/website-deploy.yml`

```yaml
name: Deploy Website

on:
  push:
    branches: [main]
    paths:
      - 'website/**'

permissions:
  contents: read
  pages: write
  id-token: write

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v6
      - uses: actions/configure-pages@v4
      - uses: actions/upload-pages-artifact@v3
        with:
          path: website
      - uses: actions/deploy-pages@v4
```

**Existing workflows modification**: Add `paths-ignore: ['website/**']` to prevent CI/release/PR validation from running on website-only changes.

**Key files to modify:**
- `.github/workflows/ci.yml` - add `website/**` to `paths-ignore`
- `.github/workflows/pr-validation.yml` - add `website/**` to ignore pattern
- `.github/workflows/release.yml` - add `website/**` to `paths-ignore`

### Development Workflow

1. **Local preview**: Open HTML files directly in browser (no server needed for basic preview)
2. **Live server** (optional): Use Python's built-in server for development:
   ```bash
   cd website
   python3 -m http.server 8000
   ```
3. **Validation**: Use browser DevTools accessibility audits + axe DevTools extension
4. **Deploy**: Push to main, GitHub Actions deploys automatically

### GitHub Pages Configuration

1. **Enable GitHub Pages** in repository settings
2. **Source**: GitHub Actions (not legacy branch deployment)
3. **Domain**: Default `<username>.github.io/tfplan2md`
4. **HTTPS**: Enabled by default

### Web Designer Agent Handoff

The Web Designer agent will:
1. Create initial HTML structure for all 8 pages
2. Implement CSS stylesheets with responsive design
3. Add screenshots and visual examples
4. Ensure WCAG 2.1 AA compliance
5. Test across browsers and devices
6. Create PR for review and deployment

The agent should follow the structure defined in this architecture and reference the specification for content requirements.

### Future Considerations

If code duplication becomes problematic:
- **JavaScript includes**: Use `fetch()` to load shared components dynamically
- **HTML templates**: Use `<template>` elements with lightweight JS
- **Web components**: Custom elements for reusable components

These can be added incrementally without breaking the simple architecture.

## Components Affected

**New files to create:**
- `/website/` directory and all HTML/CSS/asset files (Web Designer agent)
- `.github/workflows/website-deploy.yml` (Web Designer agent or Maintainer)

**Files to modify:**
- `.github/workflows/ci.yml` - add `paths-ignore`
- `.github/workflows/pr-validation.yml` - add ignore pattern
- `.github/workflows/release.yml` - add `paths-ignore`

**No changes to:**
- Source code in `src/`
- Tests in `tests/`
- Existing documentation structure
