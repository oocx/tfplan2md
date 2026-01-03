# Website Chat Summary

This document consolidates all maintainer instructions and decisions from the website design sessions (chat1.json and chat2.json).

**Last Updated:** 2026-01-03
**Source Files:** `website/_memory/chat1.json`, `website/_memory/chat2.json`

---

## Project Goals & Priorities

### Primary Goals (Ordered by Priority)
1. **Drive adoption** - Help users discover tfplan2md solves their problems
2. **Educate users** - Teach users how to use the tool effectively
3. **Build community** - Encourage contributions

### Target Audiences
1. **Evaluators** - Users who want to find out if tfplan2md is useful for them
2. **Users** - Users who want to use tfplan2md to review their pull requests
3. **Power Users** - Users who want to extend tfplan2md with custom templates
4. **Contributors** - Developers who want to provide pull requests

---

## Content Strategy

### Core Principles
- **Show, don't tell** - Use real examples and screenshots, not marketing fluff
- **Technical audience** - Users are developers with strong technical backgrounds
- **No marketing material** - Prefer technical content over marketing copy
- **Real examples** - Show before/after comparisons where tfplan2md provides most value

### Key Visual Elements
- Screenshot of raw terraform plan output in build log vs. PR comment with rendered tfplan2md output
- Screenshots for each important feature (rendered output)
- Before/after examples for high-value features (e.g., firewall rules)

### Content Sources
- Derive content from existing documentation
- Ask maintainer if further input is required
- **CRITICAL: Never make up or guess information** - Research in codebase first, ask if unsure

---

## Page Structure

### Required Pages
1. **Homepage** (`/index.html`)
   - Hero with problem/solution framing
   - Feature highlights (carousel for desktop)
   - Call to action

2. **Features** (`/features/index.html`)
   - Dedicated page for each important feature
   - Misc page for all remaining minor features
   - Categorized by value (High/Medium/Low)

3. **Providers** (`/providers/index.html`)
   - Lists provider-specific templates by provider
   - Example: "As an Azure developer, I want to quickly see how this tool helps me"

4. **Getting Started** (`/getting-started.html`)
   - Installation instructions
   - **Note:** Remove `docker pull` - `docker run` will pull automatically (simpler, one step)

5. **Documentation** (`/docs.html`)
   - CLI reference
   - Template customization

6. **Examples** (`/examples.html`)
   - Show examples in both rendered and source code (markdown) views
   - Ability to switch between views
   - Markdown syntax highlighting in source view
   - Rendered view must approximate Azure DevOps Services PR style
   - "Full screen" button for both views
   - **Must use real tfplan2md reports, not mockups**

7. **Architecture** (`/architecture.html`)
   - Based on arc42 document
   - Dedicated page

8. **Contributing** (`/contributing.html`)
   - How to contribute
   - AI-driven development process (from agents.md)
   - Workflow description

---

## Feature Definitions

### Feature Categories (Section Headlines)
| Category | Value Level | Section Name |
|----------|-------------|--------------|
| High | High | "What Sets Us Apart" |
| Medium | Medium | "Built-In Capabilities" |
| Low | Low | "Also Included" |

### Final Feature Table (Agreed Upon)

| Feature | Description | Group | Value |
|---------|-------------|-------|-------|
| Semantic Diffs | Shows "Before" and "After" values side-by-side for changed attributes | What Sets Us Apart | High |
| Firewall Rule Interpretation | Renders Azure Firewall rule collections as readable tables | What Sets Us Apart | High |
| NSG Rule Interpretation | Renders Network Security Group rules as readable tables | What Sets Us Apart | High |
| Role Assignment Mapping | Resolves Principal IDs, Scopes, and Role Names (GUIDs) to human-readable names | What Sets Us Apart | High |
| Large Value Formatting | Handles large text (JSON policies, scripts) by showing computed diff with inline highlighting | What Sets Us Apart | High |
| CI/CD Integration | Native support for GitHub Actions, Azure DevOps, GitLab CI | Built-In Capabilities | Medium |
| PR Platform Compatibility | Designed and tested for rendering in PR comments on Azure DevOps Services and GitHub | What Sets Us Apart | High |
| Friendly Resource Names | Displays friendly names instead of complex resource ID strings | What Sets Us Apart | High |
| Plan Summary | High-level overview table with counts by resource type | Built-In Capabilities | Medium |
| Module Grouping | Groups resources by Terraform module hierarchy | Built-In Capabilities | Medium |
| Collapsible Details | Hides verbose details in `<details>` tags | Built-In Capabilities | Medium |
| Tag Visualization | Renders resource tags with specific icons | Built-In Capabilities | Medium |
| Smart Iconography | Context-aware icons for Locations, IPs, Ports | Built-In Capabilities | Medium |
| Custom Templates | Customize markdown output using templates | Built-In Capabilities | Medium |
| Provider Agnostic Core | Works with any Terraform provider (AWS, GCP, etc.) | Built-In Capabilities | Medium |
| Local Resource Names | In modules, renders local name part instead of full path | Built-In Capabilities | Medium |
| Docker Support | Distributed as lightweight Docker container | Also Included | Low |
| Sensitive Value Masking | Masks sensitive values; optionally can be included in report | Also Included | Low |
| Minimal Container Image | Based on mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled | Also Included | Low |
| Dark/Light Mode | Website supports dark and light themes | Also Included | Low |

### Feature Notes
- **Role Assignment Mapping** must mention mapping of scopes and role names (not just principals)
- **Sensitive Value Masking** should note: "Optionally, sensitive values can be included in the report"
- **Minimal Container Image** uses `mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled`

---

## Design Decisions

### Theme Support
- **Light mode**: Variation 1 (GitHub-inspired colors)
- **Dark mode**: Variation 2 (High contrast dark theme)
- Theme toggle must be clearly recognizable (not look like a banana)

### Visual Hierarchy
- Clear separation between sections with different background colors
- **Dark mode must have visible background differences** like light mode (not same color for all sections)
- Problem section needs different background to separate from hero and solution

### Layout Principles
- Reduce vertical whitespace to show more content without scrolling
- Hero headlines should render without line breaks
- Problem section: Single row layout (not 2x2 grid) - uses less vertical space
- Feature carousel on desktop (shows 3 at once)
- "Also Included" cards should be smaller than other feature cards

### Homepage Structure
1. Badges at top ("Built 100% with GitHub Copilot", "Docker Ready", "Free & Open Source")
2. Hero section with main headline
3. Problem section (screenshot + problem list with red X icons)
4. Solution section (screenshot + feature list with green checkmarks)
5. Call to action section (Get Started button, View Examples button, docker run command)
6. Features carousel/highlight
7. Footer

### Problem List (index.html)
Replace "No Structure" with something else (original not preferred)

### Solution List (index.html)
- Firewall & NSG Rule Tables → Security changes rendered as readable tables
- Friendly Names, Not GUIDs → Principal IDs, role names, and scopes resolved to readable text
- Inline Diffs for Large Values → JSON policies and scripts with highlighted changes
- Optimized for PR Comments → Designed and tested for GitHub and Azure DevOps rendering

### Navigation
- All main pages accessible from top navbar (Features, Install, Docs, Examples, Providers, Architecture, Contributing)
- "Built 100% with GitHub Copilot" could link to page explaining AI-driven dev process

### Logo
- Current logo is not suitable for small sizes
- Needs redesign (not based on old one)

---

## Technical Decisions

### Hosting
- GitHub Pages
- Default GitHub Pages domain (may change later - out of scope)

### Technology
- Simple HTML/CSS for agent maintainability
- No complex build tools or site generators (unless needed)
- Agents generate HTML directly
- Easy for agents to provide design choices and prototypes

### Browser Support
- **Latest versions only** of current browsers (Edge, Firefox)
- No backward compatibility for legacy browsers or mobile devices
- May use modern web features supported by latest browsers

### Deployment
- New pipeline triggered only for website changes
- Existing pipelines should NOT run for website changes
- Website files in `/website/` directory (chosen over separate branch)

---

## Accessibility Requirements

- **WCAG 2.1 AA compliance** required
- Inclusive design (per Code of Conduct)
- All accessibility best practices must be implemented

---

## Agent Workflow

### Web Designer Agent
- Specialist agent for website design and content
- Handles all website changes (content, design, technical)
- Generates designs, prototypes, mockups, and content
- Invoked whenever changes to website are required

### Handoffs
- **Initial creation**: Architect → Web Designer
- **Ongoing changes**: Maintainer → Web Designer
- Web Designer not included in normal feature development workflow

---

## Preserved Design Artifacts

**Instruction:** "Don't throw any of them away, including the ones we did not select. I'd like to view them again later."

All design variations should be preserved for future reference.

---

## Open Issues / To-Do

1. Replace mockup examples with real tfplan2md reports
2. Logo redesign (not suitable for small sizes)
3. Review navigation for accessibility
4. Implement example viewer with render/source toggle and fullscreen
