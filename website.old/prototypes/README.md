# Website Design Prototypes

This directory contains 6 design prototypes for the tfplan2md website. Each prototype demonstrates a different visual approach and style.

## How to View

Open each prototype's `index.html` file in a web browser:

```bash
# Using Python's built-in server
cd website/prototypes/design1
python3 -m http.server 8000
# Then visit http://localhost:8000 in your browser
```

Or simply open the HTML files directly in your browser.

## Designs Overview

### Group A: Familiar Technical Documentation Sites (GitHub/Azure DevOps inspired)

#### Design 1: GitHub Docs Style
**File:** `design1/index.html`

**Style:** Clean, minimal, technical
**Characteristics:**
- GitHub's signature clean aesthetic
- Simple navigation with sticky header
- Before/after comparison section
- Grid-based feature cards
- Numbered step workflow
- Light color scheme with blue accents

**Best for:** Developers familiar with GitHub's documentation style

---

#### Design 2: Azure Docs Style
**File:** `design2/index.html`

**Style:** Modern, professional, sidebar-based
**Characteristics:**
- Persistent sidebar navigation (like Azure Docs)
- Breadcrumb navigation
- Tabbed code examples
- Info alerts and callouts
- Copy buttons on code blocks
- Professional Microsoft aesthetic
- Two-pane layout (sidebar + content)

**Best for:** Enterprise users familiar with Azure's documentation

---

#### Design 3: VS Code Docs Style
**File:** `design3/index.html`

**Style:** Dark mode, technical, developer-focused
**Characteristics:**
- Dark theme optimized for developers
- Three-column layout (sidebar + content + TOC)
- Badge system for version/status
- Collapsible callouts
- Copy buttons on all code
- Theme toggle button
- VS Code aesthetic (dark grays, blue accents)

**Best for:** Developers who prefer dark mode and technical UIs

---

### Group B: Completely Different Designs

#### Design 4: Minimalist Single-Page (Brutalist/Swiss Style)
**File:** `design4/index.html`

**Style:** Ultra-minimal, brutalist, typographic
**Characteristics:**
- Monospace font (Courier New) throughout
- Black borders and lines
- Single-page scrolling design
- Numbered feature list (01, 02, 03...)
- No gradients or shadows
- High contrast black/white with red accent
- Grid-based statistics layout
- Swiss/brutalist design principles

**Best for:** Users who appreciate bold, minimal design; stands out from typical tech sites

---

#### Design 5: Interactive Terminal Theme (Retro/Hacker)
**File:** `design5/index.html`

**Style:** Terminal emulator, retro, interactive
**Characteristics:**
- Full terminal window simulation
- ASCII art logo
- Green-on-black terminal aesthetic
- Window controls (close/minimize/maximize)
- Blinking cursor
- Command-line interface metaphor
- Retro hacker/developer vibe
- All content presented as terminal output

**Best for:** Users who love terminal aesthetics; memorable and unique

---

#### Design 6: Card-Based Modern (Contemporary/Magazine Style)
**File:** `design6/index.html`

**Style:** Modern, vibrant, card-based
**Characteristics:**
- Gradient hero section (purple/pink)
- Large card-based layouts
- Elevated shadows and hover effects
- Modern design trends (rounded corners, gradients)
- Magazine/marketing style
- Multiple color zones per section
- Glassmorphism effects
- Contemporary web design aesthetic

**Best for:** Users who prefer modern, visually rich websites; good for conversion

---

## Design Comparison Table

| Design | Layout | Colors | Feel | Complexity | Mobile-First |
|--------|--------|--------|------|------------|--------------|
| 1. GitHub Docs | Single-column | Light, blue accent | Clean, familiar | Simple | ✓ |
| 2. Azure Docs | Sidebar + content | Light, corporate blue | Professional, organized | Medium | ✓ |
| 3. VS Code Docs | 3-column | Dark theme | Technical, focused | Complex | ✓ |
| 4. Brutalist | Single-page | B&W + red | Bold, minimalist | Simple | ✓ |
| 5. Terminal | Terminal window | Green on black | Retro, playful | Medium | ✓ |
| 6. Modern Cards | Multi-section | Gradients, vibrant | Contemporary, marketing | Medium | ✓ |

## Recommendation

All designs are responsive and accessible (WCAG 2.1 AA). The choice depends on:

- **For maximum familiarity:** Design 1 (GitHub Docs) or Design 2 (Azure Docs)
- **For developer appeal:** Design 3 (VS Code dark) or Design 5 (Terminal)
- **For standing out:** Design 4 (Brutalist) or Design 5 (Terminal)
- **For modern aesthetic:** Design 6 (Card-based Modern)

## Next Steps

1. Review all 6 designs
2. Select one design or request modifications
3. Once approved, the selected design will be expanded into the full website structure

## Technical Notes

- All designs use semantic HTML5
- All designs are responsive (mobile/tablet/desktop)
- All designs follow accessibility best practices
- No external dependencies (pure HTML/CSS)
- Logo path: `../../assets/images/tfplan2md-logo.svg`
