# Feature: Template Rendering Simplification

## Overview

Simplify the template system architecture to make feature development faster and more reliable for both human developers and AI agents. The current render-then-replace mechanism and scattered template logic create complexity that leads to trial-and-error development and missed implementation points.

## User Goals

### For Maintainers (Adding Built-in Features)
- Quickly identify all code locations that need changes when adding new formatting rules or icons
- Understand side effects of changes without extensive codebase exploration
- Confidently implement features knowing all affected areas are addressed
- Reduce UAT-stage bug discovery caused by incomplete implementations

### For Template Authors (Creating Custom Templates)
- Write resource-specific templates without understanding anchor mechanics or replacement patterns
- Focus on layout and data presentation, not value transformation logic
- Access rich formatting capabilities through discoverable helpers
- Avoid trial-and-error when determining how to format values consistently

## Problem Statement

The current template system has three primary complexity sources:

1. **Render-then-Replace Mechanism**: The system renders the entire report with the default template, then uses regex to find anchor comments and replace sections with resource-specific template output. This requires:
   - Template authors to emit matching anchor comments
   - Understanding of invisible HTML comment patterns
   - Coordination between default and resource-specific templates

2. **Logic in Templates**: Templates contain extensive `func` definitions for value transformation (e.g., `source_addresses`, `destination_ports`, `format_rule_diff`). This creates:
   - Duplication when multiple templates need similar formatting
   - Difficulty finding where formatting rules are defined
   - Complex template files that mix layout with computation

3. **Scattered Change Points**: When adding formatting features (like icons for users/groups/roles), code changes span:
   - C# helper functions
   - Template func definitions
   - Multiple template files
   - With no clear pattern for finding all locations

**Evidence**: During feature 024 development, agents experienced significant trial-and-error, and missed implementation points were only caught during UAT, not during development or testing.

## Scope

### In Scope

#### Rendering Mechanism
- Eliminate the render-then-replace pattern with HTML anchor comments
- Implement direct template selection during rendering (single-pass)
- Remove regex-based section replacement logic
- Simplify template coordination between default and resource-specific templates

#### Template Content
- Move value transformation logic from Scriban `func` definitions into C# code
- Enrich model objects with pre-computed display values
- Expand helper function library for consistent formatting
- Reduce templates to layout-only: loops, conditionals, and printing prepared values

#### Developer Experience
- Establish clear patterns for where code changes belong
- Make formatting rule locations easily discoverable
- Improve impact analysis for code changes
- Enable completeness checking when implementing features

### Out of Scope

- Changes to report output format or visual appearance (user-facing output should remain functionally equivalent)
- Performance optimization (acceptable if rendering is slightly slower if architecture is clearer)
- Support for backward-compatible custom templates from before this change (breaking changes acceptable for private project)
- Changes to CLI interface or command-line options

## User Experience

### Before: Adding Icon Formatting

When adding icons for principal types (users, groups, roles):

1. Unclear where formatting logic should live (C# helpers? Template funcs? Model properties?)
2. Must search codebase to find similar patterns
3. May need to update multiple templates individually
4. Easy to miss some locations (only caught in UAT)
5. Need to understand anchor comment patterns if touching default template

### After: Adding Icon Formatting

When adding icons for principal types:

1. Clear guideline: formatting logic goes in C# (model properties or helpers)
2. Single location to implement icon mapping logic
3. Helper function automatically available in all templates
4. Compile-time or test-time detection of incomplete implementations
5. No need to understand rendering pipeline internals

### Template Authoring

#### Before
```scriban
{{## Must emit anchors for replacement to work }}
<!-- tfplan2md:resource-start address={{ address }} -->

{{ func source_addresses(rule) }}
     {{- # Complex logic here -}}
     {{- if rule.source_address_prefixes && rule.source_address_prefixes.size > 0 -}}
          {{ ret rule.source_address_prefixes | array.join ", " }}
     {{- end -}}
     {{ ret "*" }}
{{ end }}

### {{ action_symbol }} {{ address | escape_markdown }}
...
<!-- tfplan2md:resource-end address={{ address }} -->
```

#### After
```scriban
{{## Anchors handled automatically, focus on layout }}

### {{ action_symbol }} {{ address | escape_markdown }}

{{~ for rule in security_rules ~}}
| {{ rule.formatted_source_addresses }} | {{ rule.formatted_destination_ports }} |
{{~ end ~}}
```

## Success Criteria

### Measurable Outcomes

- [ ] Zero HTML anchor comments in templates (render-then-replace eliminated)
- [ ] Zero `func` definitions in resource-specific templates (logic moved to C#)
- [ ] All value formatting accessible via helpers or model properties
- [ ] Template files under 100 lines (current: 105-118 lines for complex templates)
- [ ] Single-file changes for adding new formatting rules (not spread across templates)

### Quality Attributes

- [ ] **Discoverability**: New contributors can identify change locations by reading architecture docs and following naming conventions
- [ ] **Impact Analysis**: Changes to formatting helpers show compile-time or test-time errors in affected areas
- [ ] **Completeness**: Adding a new icon/format requires changes in <3 distinct code locations
- [ ] **Agent Success**: AI agents can implement formatting features without UAT-discovered bugs

### Backward Compatibility

- [ ] All existing UAT test outputs remain functionally equivalent (visual changes acceptable only if they improve clarity)
- [ ] Built-in resource-specific templates (NSG, firewall, role assignment) continue working with same semantic diff behavior
- [ ] Custom template directory still supported (but custom templates may need migration)

## Edge Cases and Considerations

1. **Template Error Handling**: Without anchors, how are template rendering errors surfaced to users?
2. **Template Resolution**: How does single-pass rendering determine which template to use for each resource?
3. **Shared Layout**: How do resource-specific templates share common outer structure (e.g., `<details>` blocks)?
4. **Migration Path**: Do we provide tooling or documentation for migrating existing custom templates?
5. **Helper Discoverability**: How do template authors discover available helper functions?

## Open Questions

1. Should the default template become a "layout wrapper" that delegates to resource-specific templates, or should each resource template be self-contained?
2. How should complex formatting logic be organized in C#: static helpers, model methods, or dedicated formatter classes?
3. Should template selection happen at model-building time (rich model knows its template) or at rendering time (renderer selects template)?
4. What's the migration strategy for the existing anchor-based system during the transition?

## References

- Current architecture: [docs/architecture.md](../../architecture.md#84-templating-architecture)
- Feature 024 issues: [docs/features/024-visual-report-enhancements/](../024-visual-report-enhancements/)
- Template simplicity checklist: [docs/features/024-visual-report-enhancements/architecture.md](../024-visual-report-enhancements/architecture.md#template-simplicity-checklist)
- Scriban templating decision: [docs/adr-001-scriban-templating.md](../../adr-001-scriban-templating.md)
