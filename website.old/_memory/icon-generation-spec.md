# Icon Generation Specification for tfplan2md

## Project Context

**Project Name:** tfplan2md  
**Project Purpose:** A command-line tool that converts Terraform plan output into readable, well-formatted Markdown reports optimized for display in pull request comments on Azure DevOps Services and GitHub.

**Target Audience:** DevOps engineers, infrastructure developers, and teams using Infrastructure-as-Code with Terraform.

**Project Goals:**
- Make Terraform plan reviews faster and more effective
- Highlight changes clearly with semantic diffs (before/after comparisons)
- Render complex Azure resources (firewall rules, NSG rules, role assignments) in human-readable formats
- Reduce cognitive load during code reviews
- Prevent infrastructure mistakes by making changes obvious

## Design Requirements

### Technical Constraints
- **Format:** SVG (Scalable Vector Graphics)
- **Canvas size:** 48x48 viewBox
- **Must work at small sizes:** Icons will be displayed at 16px (navbar), 24px, 32px, 48px, and 64px
- **Clarity requirement:** Must be recognizable and distinguishable from other icons at 16px size
- **No text:** Icons should be purely graphical (no letters, numbers, or words)
- **Theme compatibility:** Must work in both light mode (light background) and dark mode (dark background)
- **Professional aesthetic:** Clean, modern, technical look appropriate for a developer tool

### Visual Requirements
- **Simplicity:** Avoid excessive detail that becomes muddy at small sizes
- **Consistency:** All icons should feel like they belong to the same family
- **Clarity:** Each icon should clearly communicate its purpose

## Icon Inventory & Meanings

Each icon represents a specific feature of tfplan2md. The icon should visually communicate the **essence** of what that feature does.

### Category 1: What Sets Us Apart (High Value Features)

#### 1. Semantic Diffs
**Feature:** Shows "Before" and "After" values side-by-side for changed attributes with character-level diff highlighting.  
**Symbolism:** Comparison, side-by-side view, change detection, differences, magnifying glass on changes.

#### 2. Firewall Rule Interpretation
**Feature:** Renders complex Azure Firewall rule collections as readable tables with protocols, ports, and actions.  
**Symbolism:** Firewall, network security, protection, filtering, rules, barrier.

#### 3. NSG Rule Interpretation
**Feature:** Renders Network Security Group rules as readable tables for easy security auditing.  
**Symbolism:** Network security, shield, rules, traffic control, network protection, access control.

#### 4. Role Assignment Mapping
**Feature:** Resolves cryptic GUIDs to human-readable names: Principal IDs → "Jane Doe", Role IDs → "Reader", Scope IDs → "rg-myresourcegroup".  
**Symbolism:** Identity, people, permissions, mapping, translation, user roles, access rights.

#### 5. Large Value Formatting
**Feature:** Handles large text blocks (JSON policies, scripts) by showing computed diffs with inline highlighting instead of overwhelming walls of text.  
**Symbolism:** Document, large content, condensed view, text optimization, readable formatting, collapsing long content.

#### 6. PR Platform Compatibility
**Feature:** Designed and tested for rendering in pull request comments on Azure DevOps Services and GitHub.  
**Symbolism:** Pull request, code review, browser/window, markdown rendering, platform support, preview.

#### 7. Friendly Resource Names
**Feature:** Displays friendly names for resources instead of complex 200-character resource ID strings (e.g., "kv-tfplan2md" instead of full Azure resource ID).  
**Symbolism:** Simplification, ID badge, naming, human-readable, label, clarity.

### Category 2: Built-In Capabilities (Medium Value Features)

#### 8. CI/CD Integration
**Feature:** Native support for GitHub Actions, Azure DevOps, GitLab CI with ready-to-use examples.  
**Symbolism:** Continuous integration, automation, pipeline, workflow, circular flow, DevOps.

#### 9. Plan Summary
**Feature:** High-level overview table showing counts of adds, changes, replaces, destroys by resource type.  
**Symbolism:** Summary, overview, statistics, table, counts, dashboard, metrics.

#### 10. Module Grouping
**Feature:** Groups resources logically by Terraform module hierarchy (e.g., module.network.module.monitoring).  
**Symbolism:** Hierarchy, organization, folder structure, tree, nested groups, layers.

#### 11. Collapsible Details
**Feature:** Hides verbose resource details inside expandable sections to keep PR comments scannable.  
**Symbolism:** Collapse/expand, hide/show, accordion, dropdown, details panel, disclosure.

#### 12. Tag Visualization
**Feature:** Renders resource tags with specific icons and formatting for easy metadata scanning.  
**Symbolism:** Tags, labels, metadata, categorization, badges, classification.

#### 13. Smart Iconography
**Feature:** Adds context-aware icons for common attributes like Locations (🌍), IPs (🌐), Ports (🔌), booleans.  
**Symbolism:** Icons, symbols, visual indicators, smart formatting, contextual display, visual language.

#### 14. Custom Templates
**Feature:** Allows complete customization of markdown output using Scriban templates.  
**Symbolism:** Customization, templates, flexibility, configuration, personalization, tools.
**Visual:** A document icon with a grid layout (two columns) in the body, representing a complex report structure.

#### 15. Provider Agnostic Core
**Feature:** Works with any Terraform provider (AWS, GCP, Azure, etc.) using standard resource rendering.  
**Symbolism:** Hexagon network, central core, connections, hub and spoke, infrastructure, multi-cloud.

#### 16. Local Resource Names
**Feature:** In modules, displays just the local name ("hub") instead of full module path for cleaner summaries.  
**Symbolism:** Local scope, simplified naming, local reference, short name, context-aware display.

### Category 3: Also Included (Low Value Features)

#### 17. Sensitive Value Masking
**Feature:** Automatically detects and masks sensitive values (marked as sensitive in Terraform) to prevent leaks.  
**Symbolism:** Security, privacy, masking, hidden values, redacted, password protection, confidentiality.

#### 18. Minimal Container Image
**Feature:** Uses mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled for minimal attack surface.  
**Symbolism:** Container, minimalism, security, lightweight, small footprint, optimized.

#### 19. Container Support
**Feature:** Distributed as lightweight container image for easy usage in any environment.  
**Symbolism:** Container, portability, shipping container, package, deployment.

#### 20. Dark/Light Mode
**Feature:** Website supports dark and light theme toggle.  
**Symbolism:** Theme switching, day/night, light/dark, contrast, appearance, toggle.

## Uniqueness Requirement

**Critical:** Each icon must be visually distinct from all other icons in the set. When displayed together at small sizes (16px), users should be able to differentiate them at a glance without confusion.

**Avoid duplicates:**
- Only one shield icon (used for firewall OR NSG, not both)
- Only one folder/hierarchy icon (module grouping)
- Only one tag icon (tag visualization)
- Only one document icon (large values OR collapsible details, not both)
- Only one user/person icon (role assignment)

## Delivery Format

Each icon should be delivered as:
- Individual SVG file named with kebab-case (e.g., `semantic-diffs.svg`, `firewall-rules.svg`)
- Valid SVG with `viewBox="0 0 48 48"`
- No embedded fonts or external dependencies
- Optimized for web use (clean paths, no unnecessary attributes)

## Testing Criteria

Generated icons will be evaluated on:
1. **Recognition:** Can users identify what the icon represents without a label?
2. **Distinction:** Are all icons visually distinct at 16px size?
3. **Professional appearance:** Do icons look polished and appropriate for a developer tool?
4. **Theme compatibility:** Do icons work well in both light and dark themes?
5. **Style consistency:** Do all icons feel like they belong to the same family?
6. **Small size clarity:** Are icons still clear and recognizable at 16px?

## Reference Examples

**Good icon design for technical tools:**
- GitHub's icon set (simple, recognizable, works at small sizes)
- VS Code's icon set (clear symbolism, professional)
- Material Design Icons (MDI) system icons subset
- Feather Icons (minimal, consistent stroke weight)

**Avoid:**
- Overly complex details that become muddy at small sizes
- Generic/ambiguous symbols that could mean multiple things
- Icons that look too similar to each other
- Pure emoji style (we want a professional, technical aesthetic)
