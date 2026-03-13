# Website Content Strategy

This document defines the content principles and guidelines for the tfplan2md website.

## Project Goals (Priority Order)

1. **Drive adoption** - Help users discover tfplan2md solves their problems
2. **Educate users** - Teach users how to use the tool effectively
3. **Build community** - Encourage contributions

## Target Audiences

| Audience | Description | Primary Needs |
|----------|-------------|---------------|
| **Evaluators** | Users researching if tfplan2md is useful | See real examples, understand value proposition |
| **Users** | Users implementing tfplan2md for PR reviews | Installation, CI/CD integration, basic usage |
| **Power Users** | Users extending with custom templates | Template customization, provider-specific features |
| **Contributors** | Developers wanting to contribute | Architecture, development setup, AI workflow |

## Content Principles

### Show, Don't Tell
- Use **real examples and screenshots**, not marketing fluff
- Before/after comparisons for high-value features (e.g., firewall rules)
- Screenshot of raw terraform plan vs. rendered tfplan2md output

### Technical Audience
- Users are developers with **strong technical backgrounds**
- Prefer **technical content** over marketing material
- No marketing "bla bla" - be direct and factual

### Real Examples Only
- **Never use mockups** in production - always use real tfplan2md reports
- Screenshots must show actual rendered output
- Code examples must be copy/paste ready

### Content Sources
- Derive content from existing documentation (README.md, docs/)
- Ask maintainer if further input is required
- **CRITICAL: Never make up or guess information** - research in codebase first, ask if unsure

## Page-Specific Guidelines

### Homepage
- Lead with the **problem** users face (reviewing complex terraform PRs)
- Show the **solution** with visual evidence
- Call to action after user understands value (not before)
- Remove unnecessary steps (e.g., `docker pull` - `docker run` auto-pulls)

### Features Page
- Categorize by value (High/Medium/Low)
- High-value features get prominent placement
- Each important feature gets dedicated page
- Minor features grouped on misc page

### Examples Page
- Show examples in **both rendered and source code** views
- Ability to **switch between views**
- **Markdown syntax highlighting** in source view
- Rendered view must **approximate Azure DevOps Services PR style**
- **Full screen button** for both views

### Providers Page
- Organize by provider (azurerm, azuread, azuredevops, msgraph)
- Quick answer to: "As an Azure developer, how does this tool help me?"

### Contributing Page
- Include AI-driven development process from agents.md
- Describe the multi-agent workflow
- Link to "Built 100% with GitHub Copilot" explanation

## Writing Style

- Clear, concise, technical
- Action-oriented headings
- Code-first where applicable
- Avoid jargon that isn't Terraform/DevOps standard

## Decision Log

- 2026-01-03: Initial content strategy created from chat session analysis.
