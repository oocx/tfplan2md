# Feature: GitHub Pages Website

## Overview

Create a public-facing website for tfplan2md hosted on GitHub Pages to drive adoption, educate users, and build a contributor community. The website targets technically-minded developers who prefer concrete examples and technical content over marketing material.

## User Goals

### Target Audiences

1. **Evaluators** - Developers who have heard about tfplan2md and want to assess if it solves their Terraform PR review challenges
2. **Users** - DevOps/Platform engineers who want to integrate tfplan2md into their CI/CD pipelines
3. **Power Users** - Advanced users who want to extend tfplan2md with custom templates or provider-specific functionality
4. **Contributors** - Developers interested in contributing to the project

### Primary Goals (Priority Order)

1. **Drive Adoption** - Help potential users quickly understand the value proposition through real examples
2. **Educate Users** - Provide clear getting-started guides and comprehensive documentation
3. **Build Community** - Make it easy for contributors to understand the project and get involved

## Scope

### In Scope

**Content Pages:**

1. **Homepage** (/)
   - Hero section with problem statement
   - Visual proof: Screenshot comparison of raw Terraform plan output (in build log) vs. rendered tfplan2md output (in PR comment)
   - Feature showcase with screenshots demonstrating high-value features (firewall rules, NSG rules, role assignments, etc.)
   - Clear "Get Started" call-to-action

2. **Getting Started** (/getting-started)
   - Installation instructions (Docker pull)
   - First usage example (piping from terraform)
   - CI/CD integration snippets for GitHub Actions and Azure Pipelines

3. **Features** (/features)
   - Overview/index page listing all features
   - **Dedicated page per major feature:**
     - Firewall rule semantic diffing
     - Network security group diffing
     - Azure role assignments with principal mapping
     - Module grouping
     - Custom templates
     - Sensitive value masking
     - Large value formatting
   - **Miscellaneous features page** covering minor features

4. **Provider Templates** (/providers)
   - Index page listing all supported providers
   - **Dedicated page per provider:**
     - azurerm (Azure)
     - azuredevops
     - azuread
     - msgraph
   - Each provider page shows available resource-specific templates with examples

5. **Examples** (/examples)
   - Interactive or expandable examples of different use cases
   - Before/after visuals for each scenario
   - Links to live comprehensive demo in the repository

6. **Documentation** (/docs)
   - CLI reference (all flags and options)
   - Template customization guide
   - Troubleshooting section

7. **Architecture** (/architecture)
   - Content based on the arc42 architecture documentation
   - High-level system overview and design decisions

8. **Contributing** (/contributing)
   - How to contribute to the project
   - Links to GitHub repository and issue tracker
   - Development environment setup
   - **AI-Assisted Development workflow** - explanation of the agent-based workflow (content from docs/agents.md)
   - Multi-model AI approach description

**Visual Design:**

- Multiple design options/mockups to explore different visual styles
- Technical aesthetic (no marketing fluff) suitable for developer audience
- Responsive design for desktop and mobile
- Screenshot-heavy to show real value quickly

**Technical Implementation:**

- Hosted on GitHub Pages
- Static site generation (specific technology to be determined by Architect)
- Automated deployment from repository

**Accessibility:**

- Must implement accessibility best practices (WCAG 2.1 AA compliance as a target)
- Semantic HTML structure
- Proper heading hierarchy
- Alt text for all images and screenshots
- Keyboard navigation support
- Sufficient color contrast
- Screen reader compatibility
- Focus indicators for interactive elements
- Responsive text sizing
- Clear, descriptive link text

This aligns with the project's Code of Conduct commitment to creating an inclusive environment for all users.

### Out of Scope

- Interactive demos or live editors (users can try via Docker)
- User authentication or personalization
- Analytics dashboard (basic analytics may be added later)
- Multi-language support (English only initially)
- Search functionality (can be added in future iteration)
- Blog or news section (can be added later if needed)
- Versioned documentation (show latest version only)

## User Experience

### Content Strategy

**Principle: "Show, Don't Tell"**

- Lead with visual examples (screenshots of rendered output in actual PRs)
- Minimize marketing language; use technical, straightforward descriptions
- Provide code snippets that users can copy/paste
- Show before/after comparisons for high-value features
- Make it easy to see real-world usage scenarios

### Navigation Structure

- Simple top-level navigation menu
- Clear visual hierarchy
- Easy access to Getting Started and Examples from any page
- Prominent links to GitHub repository

### Mobile Experience

- Responsive design that works on mobile devices
- Screenshots should be readable on smaller screens
- Code snippets should be scrollable/copyable on mobile

### Visual Examples Priority

**Most Important (Homepage):**
1. Raw Terraform plan output in build log vs. tfplan2md rendered in PR comment
2. Firewall rule semantic diffing (before: index-based confusion, after: clear rule-level changes)
3. Module grouping (organized, scannable output)

**Feature-Specific Pages:**
- Each major feature page includes:
  - Screenshot of the rendered output
  - Explanation of what problem it solves
  - Code snippet showing how to enable/use it (if applicable)

## Success Criteria

Website is considered complete when:

- [ ] All 8 main pages are created with content structure defined
- [ ] Homepage includes visual before/after comparison screenshots
- [ ] Major feature pages include screenshots of rendered output
- [ ] Provider template pages show examples for all 4 providers (azurerm, azuredevops, azuread, msgraph)
- [ ] Getting Started guide includes copy/paste-ready code snippets for Docker and CI/CD integration
- [ ] Contributing page includes AI workflow explanation from agents.md
- [ ] Architecture page reflects arc42 documentation structure
- [ ] Multiple design mockups/options have been created for review
- [ ] Site is responsive and works on mobile devices
- [ ] Site is deployed and accessible via GitHub Pages URL
- [ ] Navigation is functional across all pages
- [ ] All links to GitHub repository, issues, and documentation work correctly

## Technical Constraints

1. **Agent-Friendly Technology:** The site must be easy for AI agents to modify via prompts. Technology choice should prioritize agent maintainability (direct HTML generation may be preferable to complex site generators).
2. **Design Prototyping:** Design tools should allow quick iteration and easy presentation of multiple design options to the Maintainer without complex setup.
3. **Screenshot Sources:** Reuse existing examples where possible; create new demo scenarios specifically for the website only if current examples are insufficient.
4. **Content Strategy:** Technical Writer derives content from existing documentation (README.md, docs/, etc.) and requests Maintainer input only when needed.
5. **Deployment:** Fully automated deployment via CI/CD pipeline.
6. **Domain:** Default GitHub Pages domain (username.github.io/tfplan2md) initially. Custom domain is out of scope for this feature.

## Agent Workflow Considerations

**New Agent Required:** This feature requires creating a specialized **Web Designer agent** to handle:
- Visual design and UX decisions
- Creating multiple design prototypes for Maintainer review
- Writing web-optimized content (conversion-focused, scannable)
- Implementing responsive HTML/CSS
- Iterating on design based on feedback

**Workflow Modification:** Before implementation begins, the Workflow Engineer must define the Web Designer agent role, responsibilities, and handoff procedures.

**Rationale:** Website design requires specialized skills distinct from code development or technical documentation. A dedicated agent enables iterative design exploration that aligns with the Maintainer's "I know it when I see it" evaluation approach.
