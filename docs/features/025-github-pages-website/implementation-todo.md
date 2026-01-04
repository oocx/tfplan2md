# Website Implementation Todo List

## Phase 4: Implementation - Building Complete Website Structure

**Status:** In Progress  
**Started:** 2025-12-30  
**Branch:** feature/website-initial

---

## 1. Setup & Structure ✅

- [x] Create complete directory structure in /website/
- [x] Design prototypes created (6 original + 6 variations)
- [x] Final design approved (design6-final with light/dark mode)
- [x] Move final design from prototypes to root website structure
- [x] Set up shared CSS variables and base styles (extracted from design6-final)
- [x] Create shared navigation component (reusable across pages)

---

## 2. Core Pages (8 main pages)

- [x] Homepage (index.html) - moved from design6-final with updated navigation
- [x] Getting Started (getting-started.html) - Docker + CI/CD integration complete
- [x] Features index (features/index.html) - 8 feature categories + detailed sections
- [x] Examples (examples.html) - Template comparison + feature examples + real-world demos
- [x] Documentation (docs.html) - CLI reference, templates, troubleshooting complete
- [x] Architecture (architecture.html) - arc42 overview, components, tech stack, ADRs complete
- [x] Contributing (contributing.html) - Development workflow, commit conventions, AI workflow complete
- [x] Provider templates index (providers/index.html) - azurerm, azuredevops, azuread, msgraph complete

---

## 3. Feature Pages (features/)

Note: Feature index page created with inline detailed sections. Individual feature pages may be added later if needed.

- [x] Feature index with detailed sections for:
  - [x] Firewall rule semantic diffing
  - [x] Network security group diffing
  - [x] Module grouping
  - [x] Azure resource ID formatting
  - [x] Role assignment display
  - [x] Principal name mapping
  - [x] Semantic value icons
  - [x] Consistent value formatting
- [ ] Additional feature pages (Resource summaries, Sensitive masking, Large values, Custom templates) - to be added if needed

---

## 4. Provider Pages (providers/)

- [ ] azurerm (Azure)
- [ ] azuredevops
- [ ] azuread
- [ ] msgraph

---

## 5. Content Integration

- [x] Extract content from README.md (homepage hero, features overview)
- [x] Extract content from docs/features.md (features page, getting started examples)
- [ ] Extract content from docs/architecture.md (for architecture page)
- [ ] Extract content from CONTRIBUTING.md (for contributing page)
- [ ] Extract content from docs/agents.md (for contributing page)
- [x] Add code examples from examples/ (CI/CD snippets, installation commands)
- [x] Adapt content for web (scannable, conversion-focused)

---

## 6. Visual Assets

- [ ] Identify screenshots needed from examples/comprehensive-demo/
- [ ] Generate/capture screenshots
- [ ] Optimize images for web (compression, appropriate formats)
- [ ] Add before/after comparisons
- [ ] Create favicon from logo

---

## 7. Navigation & Interactivity

- [x] Create shared navigation component (navbar with theme toggle)
- [x] Implement theme toggle (light/dark mode with localStorage)
- [x] Implement copy-to-clipboard for code blocks (getting-started page)
- [x] Implement tab switching (CI/CD platforms on getting-started page)
- [ ] Add "back to top" functionality where appropriate
- [x] Ensure all internal links work correctly (verified working)
- [ ] Test external links (GitHub, Docker Hub, etc.)

---

## 8. Verification

- [ ] Test responsive design (320px, 768px, 1024px, 1920px)
- [ ] Validate HTML5 (all pages)
- [ ] Check accessibility with axe DevTools (WCAG 2.1 AA)
- [ ] Test keyboard navigation (Tab, Enter, Escape)
- [ ] Verify all links work (internal and external)
- [ ] Check color contrast ratios (4.5:1 for text, 3:1 for large text)
- [ ] Test theme toggle on all pages
- [ ] Test on different browsers (Chrome, Firefox, Safari, Edge)
- [ ] Test focus indicators visibility
- [ ] Verify proper heading hierarchy (no skipped levels)
- [ ] Check alt text on all images

---

## 9. Finalization

- [ ] Update documentation in docs/features/025-github-pages-website/
- [ ] Add implementation notes and decisions
- [ ] Commit all changes with conventional commit format
- [ ] Post PR Title + Description in chat
- [ ] Hand off to Release Manager

---

## Notes

### Current Structure
```
website/
├── assets/
│   └── images/
│       └── tfplan2md-logo.svg
├── features/
├── providers/
└── prototypes/
    ├── design1/ through design6/
    ├── design6-variations/
    └── design6-final/  ← Approved design with light/dark mode
```

### Design Decisions
- **Final Design:** design6-final (Variation 1 for light mode, Variation 2 for dark mode)
- **Theme Toggle:** CSS-drawn icons (moon/sun) with localStorage persistence
- **Technology:** Direct HTML/CSS (no build tools, agent-maintainable)
- **Accessibility:** WCAG 2.1 AA compliance target
- **Content Strategy:** Show, Don't Tell - visual examples and screenshots

### Key Features to Highlight
1. Semantic diffs (firewall rules, NSG rules)
2. Module grouping
3. Azure optimizations (resource IDs, principal mapping)
4. Visual icons for IPs, ports, protocols
5. Security (sensitive value masking)
6. CI/CD integration ready

### Screenshots Needed
- Before/after: raw terraform output vs. tfplan2md markdown
- PR comment rendering examples
- Feature-specific examples (firewall rules, NSG rules, role assignments)
- CI/CD integration examples

---

## Progress Tracking

**Completed Steps:** 9/9 sections (8 of 8 core pages complete - 100% milestone! 🎉)  
**Next Step:** Verification phase - test responsive design, accessibility, and links  
**Estimated Remaining Work:** ~30-60 minutes for verification and finalization

---

## Questions/Blockers

None currently.

---

**Last Updated:** 2025-12-30T21:22:00Z
