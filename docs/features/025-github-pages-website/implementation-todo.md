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
- [ ] Move final design from prototypes to root website structure
- [ ] Create shared navigation component (reusable across pages)
- [ ] Set up shared CSS variables and base styles (extracted from design6-final)

---

## 2. Core Pages (8 main pages)

- [ ] Homepage (index.html) - adapt from design6-final
- [ ] Getting Started (getting-started.html)
- [ ] Features index (features/index.html)
- [ ] Provider templates index (providers/index.html)
- [ ] Examples (examples.html)
- [ ] Documentation (docs.html)
- [ ] Architecture (architecture.html)
- [ ] Contributing (contributing.html)

---

## 3. Feature Pages (features/)

- [ ] Firewall rule semantic diffing
- [ ] Network security group diffing
- [ ] Azure role assignments
- [ ] Module grouping
- [ ] Custom templates
- [ ] Sensitive value masking
- [ ] Large value formatting
- [ ] Miscellaneous features

---

## 4. Provider Pages (providers/)

- [ ] azurerm (Azure)
- [ ] azuredevops
- [ ] azuread
- [ ] msgraph

---

## 5. Content Integration

- [ ] Extract content from README.md
- [ ] Extract content from docs/features.md
- [ ] Extract content from docs/architecture.md
- [ ] Extract content from CONTRIBUTING.md
- [ ] Extract content from docs/agents.md
- [ ] Add code examples from examples/
- [ ] Adapt content for web (scannable, conversion-focused)

---

## 6. Visual Assets

- [ ] Identify screenshots needed from examples/comprehensive-demo/
- [ ] Generate/capture screenshots
- [ ] Optimize images for web (compression, appropriate formats)
- [ ] Add before/after comparisons
- [ ] Create favicon from logo

---

## 7. Navigation & Interactivity

- [ ] Create shared navigation component
- [ ] Implement breadcrumb navigation
- [ ] Add "back to top" functionality where appropriate
- [ ] Ensure all internal links work correctly
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

**Completed Steps:** 3/9 sections (Setup & Structure partially complete)  
**Next Step:** Move design6-final to root and create shared navigation component  
**Estimated Remaining Work:** ~6-8 hours for full implementation

---

## Questions/Blockers

None currently.

---

**Last Updated:** 2025-12-30T17:42:00Z
