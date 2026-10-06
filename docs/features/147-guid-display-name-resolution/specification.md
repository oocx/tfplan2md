# Feature: GUID Display-Name Resolution

## Overview

Terraform plans for generic resources can contain important identities only as GUIDs in
free-form attributes such as Conditional Access policy bodies. Existing display-name
mapping is limited to attributes whose meaning tfplan2md already recognizes, leaving
reviewers to cross-reference users, groups, tenants, directory roles, and other IDs by
hand.

This feature adds an explicit opt-in that resolves GUID-shaped resource attribute
values from every applicable section of the loaded mapping file. It also provides an
offline catalog of Microsoft Entra built-in role template IDs and built-in
authentication strength policy IDs. Resolved values retain their original GUID so a
reviewer can still verify the exact identity.

This specification covers ideas 3 and 4 from
[GitHub issue #674](https://github.com/oocx/tfplan2md/issues/674). Resource and report
clarity ideas 1, 2, 7, 9's consistent-label item, and 10 are covered by Feature 146,
`generic-resource-review-clarity`. The other rendering defects in idea 9 are deferred
to a grouped bug-fix follow-up. Collection display and diff ideas 5, 6, and 8 are
covered by Feature 148, `compact-collection-diffs`.

Authoritative built-in catalogs include Microsoft's
[Microsoft Entra built-in roles reference](https://learn.microsoft.com/en-us/entra/identity/role-based-access-control/permissions-reference)
and the Microsoft Graph
[authentication strength policies response](https://learn.microsoft.com/en-us/graph/api/authenticationstrengthroot-list-policies?view=graph-rest-1.0).

## User Goals

- Review generic `msgraph_resource`, `msgraph_update_resource`, `azapi_resource`, and
  other resource attributes without manually looking up GUIDs.
- Choose explicitly when broad GUID matching is appropriate for a report.
- Recognize common Entra directory roles and authentication strengths without network
  access or a tenant-specific mapping file.
- Supply readable names for tenant-specific custom Entra directory roles.
- Keep the exact GUID visible after resolution so similarly named objects remain
  distinguishable and reviewable.
- Retain current security behavior for values Terraform marks as sensitive.

## Scope

### In Scope

- Add a `--resolve-all-guids` command-line option. When enabled, every displayed,
  GUID-shaped leaf value in resource attributes is eligible for display-name
  resolution, regardless of provider, resource type, attribute name, or nesting path.
- Broad resolution uses all applicable mappings loaded through the existing
  `--principal-mapping` option, including users, groups, service principals,
  subscriptions, tenants, Azure RBAC roles, and any other supported GUID-keyed mapping
  section.
- Apply broad resolution consistently wherever resource attribute values are shown,
  including planned-change values and drift values. Feature 148 owns collection layout
  and collection diff presentation; values shown within those collections remain
  eligible for this feature's resolution.
- Apply the same eligibility to individual leaves shown inside large-attribute or
  expanded-value presentations. Display size, truncation, or a specialized large-value
  presentation must not bypass resolution of an otherwise eligible standalone GUID.
- Preserve the existing entity-specific presentation, including semantic icons, when
  the matching mapping identifies an entity type.
- Display the original GUID alongside every resolved name. GUID comparison is
  case-insensitive, while the original value remains identifiable in the report.
- Leave an unknown GUID unchanged. Ordinary strings, partial GUIDs, Azure resource IDs,
  and strings that merely contain a GUID as part of larger text are not treated as a
  standalone GUID by this option.
- When the attribute's meaning is known, use only the matching semantic mapping
  category. When broad matching has no semantic context and the same GUID occurs in
  more than one user mapping section, leave the value raw rather than choosing a name
  or entity type arbitrarily. A single matching user mapping takes precedence over an
  offline built-in catalog entry.
- Ship offline display names for Microsoft Entra built-in directory role template IDs,
  including the catalog entries published by Microsoft, such as Global Administrator
  (`62e90394-69f5-4237-9190-012177145e10`) and Security Administrator
  (`194ae4cb-b126-40b2-bd5b-6091b380977d`).
- Ship offline display names for the three built-in Microsoft Entra authentication
  strength policies: Multifactor authentication
  (`00000000-0000-0000-0000-000000000002`), Passwordless MFA
  (`00000000-0000-0000-0000-000000000003`), and Phishing-resistant MFA
  (`00000000-0000-0000-0000-000000000004`).
- Resolve built-in Entra IDs by default when an attribute is already recognized as an
  Entra directory-role template or authentication-strength policy reference. In
  arbitrary or free-form attributes, these same IDs resolve only when
  `--resolve-all-guids` is enabled.
- Extend the mapping file with an optional `directoryRoles` section containing IDs and
  display names for custom Microsoft Entra directory roles. User-provided directory
  role mappings take precedence over an offline built-in name for the same ID.
- Keep `directoryRoles` separate from the existing `roles` section. The existing
  `roles` contract continues to represent custom Azure RBAC role definitions.
- Accept existing mapping files unchanged. The new section is optional, and omitting it
  does not produce an error.
- Continue to work entirely offline during report generation. No Microsoft Graph,
  Azure, Entra, or other network lookup is performed.
- Keep masked values masked. A sensitive value is not exposed or inferred through
  display-name resolution. If the user has explicitly enabled the existing sensitive
  value display behavior, a visible GUID is eligible under the same rules as any other
  visible value.

### Out of Scope

- Well-known first-party application IDs such as Microsoft Graph, Azure Resource
  Manager, SharePoint, Exchange, or Intune. They are deferred to a later catalog
  extension so this initial feature has a bounded, authoritative scope.
- Runtime discovery or refresh of users, groups, service principals, roles,
  authentication strengths, applications, or other directory objects.
- Fuzzy matching, substring replacement, or resolution of non-GUID identifiers.
- Changes to how resource summaries choose a label or show Terraform instance keys;
  those belong to Feature 146.
- Natural sorting, compact collection rendering, or element-wise collection diffs;
  those belong to Feature 148.
- Renaming or repurposing the existing `roles` mapping section.
- Changing the existing default resolution behavior for arbitrary GUID-valued
  attributes when `--resolve-all-guids` is absent.

## User Experience

Broad matching is off by default:

```text
tfplan2md plan.json
```

In this mode, existing recognized mappings continue to behave as documented. Built-in
Entra directory roles and authentication strengths are readable in recognized semantic
attributes. An otherwise unknown GUID in a generic body remains unchanged.

Users opt into broad matching explicitly:

```text
tfplan2md --resolve-all-guids plan.json
tfplan2md --principal-mapping mappings.json --resolve-all-guids plan.json
```

For example, a generic Conditional Access policy attribute can change from a GUID wall:

```markdown
| body.conditions.users.excludeUsers[0] | - | `11111111-1111-1111-1111-111111111111` |
| body.conditions.users.includeRoles[0] | - | `62e90394-69f5-4237-9190-012177145e10` |
| body.grantControls.authenticationStrength.id | - | `00000000-0000-0000-0000-000000000004` |
```

to readable values while preserving each ID:

```markdown
| body.conditions.users.excludeUsers[0] | - | `👤 Break Glass Account (11111111-1111-1111-1111-111111111111)` |
| body.conditions.users.includeRoles[0] | - | `🛡️ Global Administrator (62e90394-69f5-4237-9190-012177145e10)` |
| body.grantControls.authenticationStrength.id | - | `Phishing-resistant MFA (00000000-0000-0000-0000-000000000004)` |
```

The mapping file can add custom directory roles without changing Azure RBAC mappings:

```json
{
  "roles": [
    { "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "displayName": "Custom Azure Deployment Role" }
  ],
  "directoryRoles": [
    { "id": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", "displayName": "Custom Entra Application Operator" }
  ]
}
```

If a displayed GUID has no match in any applicable loaded mapping or built-in catalog,
the report shows the original GUID exactly as before. If the same GUID occurs as a
masked sensitive value, the report continues to show only the existing sensitive-value
placeholder.

## Success Criteria

- [ ] Without `--resolve-all-guids`, arbitrary GUID-shaped values in generic resource
  attributes render exactly as they do today.
- [ ] With `--resolve-all-guids`, a standalone GUID in any scalar or nested resource
  attribute path can resolve from each supported GUID-keyed section of the loaded
  mapping file.
- [ ] Broad resolution applies to displayed before and after values in planned changes
  and to displayed drift values, independent of provider and resource type.
- [ ] An eligible standalone GUID inside a large-attribute or expanded-value display
  resolves under the same rules as the same value in an ordinary attribute row.
- [ ] A resolved value shows the mapped display name, its entity-specific presentation
  when known, and the original GUID.
- [ ] GUID lookup is case-insensitive and does not change the identity shown to the
  reviewer.
- [ ] An unmapped GUID remains a raw GUID, and non-GUID strings or GUID substrings remain
  unchanged.
- [ ] A semantically recognized attribute uses its matching mapping category. With no
  semantic context, a GUID found in multiple user mapping sections remains raw, while
  a sole user mapping overrides an offline built-in catalog name.
- [ ] The published Microsoft Entra built-in directory role template catalog is
  available offline, including the Global Administrator and Security Administrator
  examples from issue #674.
- [ ] The three published built-in authentication strength policy IDs resolve offline
  to Multifactor authentication, Passwordless MFA, and Phishing-resistant MFA.
- [ ] Recognized Entra directory-role and authentication-strength attributes use the
  offline names without requiring `--resolve-all-guids`; free-form occurrences require
  the option.
- [ ] A custom Entra directory role in `directoryRoles` resolves to its configured
  display name, and that mapping overrides a built-in directory-role name for the same
  ID.
- [ ] The existing `roles` section continues to resolve Azure RBAC custom roles and is
  not interpreted as an Entra directory-role mapping.
- [ ] Existing mapping files with no `directoryRoles` section remain valid and retain
  their current behavior.
- [ ] Report generation performs no runtime network or tenant lookup for any resolution.
- [ ] Sensitive values remain masked when sensitive display is disabled, even when
  their underlying value is a known GUID and broad resolution is enabled.
- [ ] When the existing sensitive display option exposes a GUID, it follows the same
  resolution rules as another visible GUID.
- [ ] An invalid or unavailable mapping file retains the existing error behavior; the
  broad option does not silently substitute an online lookup.
- [ ] Automated acceptance coverage demonstrates default-off behavior, every mapping
  category, nested generic attributes, unknown and malformed values, GUID casing,
  built-in catalogs, custom directory-role precedence, Azure RBAC separation, drift,
  offline execution, and sensitive masking.

## Source-Idea Traceability

| Source | Covered requirement |
| --- | --- |
| [Issue #674](https://github.com/oocx/tfplan2md/issues/674) idea 3 | Explicit opt-in for GUID resolution in arbitrary resource attributes using all loaded mappings; unknown values remain unchanged. |
| [Issue #674](https://github.com/oocx/tfplan2md/issues/674) idea 4 | Offline built-in names for Entra role template IDs and authentication strength IDs; mapping support for custom directory roles. |
| [Issue #674](https://github.com/oocx/tfplan2md/issues/674) optional first-party application IDs | Deferred from the bounded initial scope and recorded for specification approval. |
| Existing Azure display enhancements | Preserve original IDs, existing mapping compatibility, offline behavior, and Azure RBAC `roles` semantics. |
| Existing sensitive-value behavior and ADR-009 | Resolution never reveals a value that the report masks. |
| Feature 146 | Owns resource summary labels, instance keys, reference hints, the consistent-label item from idea 9, and import/report clarity. |
| Feature 148 | Owns natural collection sorting, compact collection display, and collection-aware diffs. |
| Grouped bug-fix follow-up | Owns idea 9's version fallback, workspace escaping, stray separator, and empty attribute-path defects. |
