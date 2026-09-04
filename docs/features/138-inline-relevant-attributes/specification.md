# Feature: Inline Relevant Attributes

## Overview

The current "Relevant Attributes" section is a standalone table at the bottom of the report showing upstream resource attributes that influenced changes in the plan. This placement is disconnected from the changes it explains, making it unusable in practice. This feature redesigns relevant-attributes rendering so that the dependency context appears *inline* on each affected resource card — directly where reviewers need it.

## User Goals

- When a reviewer sees a resource being **replaced or destroyed**, they need to understand *why* close to that change — not in a separate section at the bottom of the report.
- Specifically, when a forced replacement cascades from an upstream attribute change (e.g. a network interface ID that another resource depends on), that causal chain must be clearly visible on the replaced resource's card.
- When a resource depends on upstream attributes that are themselves **changing** in this plan, this is flagged inline so the reviewer understands the blast radius of upstream changes.
- For relevant attributes that cannot be correlated to any specific changed resource, they remain visible in a collapsible fallback section at the end of the report so no information is silently dropped.

## Scope

### In Scope

- **Per-resource forced-replacement causal annotation (Option 5):** For each resource being *replaced or destroyed*, if any of its force-replacement paths (`replace_paths`) trace back to an upstream `relevant_attributes` entry, render a callout line directly inside that resource's card: `⚠️ Forced replacement — <attribute> reads <upstream resource>.<upstream attribute>, which is **changing in this plan**.`
- **"Changing in this plan" flagging:** When the upstream resource referenced in a relevant attribute is itself being changed (replaced, destroyed, or updated) in the same plan, this is highlighted with **bold** emphasis. This applies to both the forced-replacement line and the depends-on line.
- **Per-resource dependency annotation (Option 3):** For each resource card (replace/destroy only — not in-place updates), if any `relevant_attributes` entries correlate to that resource's configuration references, render a `🔗 Depends on:` line listing all correlated upstream attributes. List **all** correlated upstream values (no truncation limit).
- **The ⚠️ marker** appears on individual upstream entries in the `🔗 Depends on:` line when that upstream value is itself being replaced or destroyed in this plan.
- **Uncorrelated-inputs fallback section (Example D):** Relevant attributes that cannot be correlated to any specific changed resource are rendered in a collapsible `<details>` section near the end of the report (replacing the current flat `## Relevant Attributes` H2 table). The fallback section is omitted when all relevant attributes were correlated.
- **Applies to planned changes only:** Drift entries (`🌀 Drift Detected`) do not receive inline dependency annotations.
- The existing `## Relevant Attributes` H2 table is removed.

### Out of Scope

- In-place update resources (`🔧 Change` action without replace/destroy) do not get the `🔗 Depends on:` line, even if their configuration references appear in `relevant_attributes`.
- Drift section resources do not receive inline dependency annotations.
- The `🔗 Depends on:` list is not truncated to a fixed maximum (all correlated attributes are listed).
- No new CLI options or configuration flags are added.
- No changes to how `replace_paths` are displayed in the resource summary line (those remain as-is).

## User Experience

### Forced-replacement card (Option 5)

When a resource is being replaced and the replacement traces to an upstream relevant attribute:

```markdown
<details style="...">
<summary>♻️ azurerm_virtual_machine <b><code>web</code></b> — recreate (network_interface_ids changed: force replacement)</summary>
<br>

> ⚠️ **Forced replacement** — `network_interface_ids` reads `azurerm_network_interface.web.id`, which is **changing in this plan**.

| Attribute | Before | After |
| --- | --- | --- |
| network_interface_ids[0] | `…/nic-web-old` | `(known after apply)` |

</details>
```

### Combined card (both lines)

When a replaced resource also has additional upstream dependencies:

```markdown
<details style="...">
<summary>♻️ azurerm_app_service <b><code>api</code></b> — recreate (app_settings changed: force replacement)</summary>
<br>

> ⚠️ **Forced replacement** — `app_settings` reads `azurerm_key_vault.main.vault_uri`, which is **changing in this plan**.
> 🔗 **Also depends on:** `data.azurerm_client_config.current.tenant_id`, `azurerm_key_vault.main.id`

| Attribute | Before | After |
| --- | --- | --- |
| app_settings."KV_URI" | `https://kv-old…` | `(known after apply)` |

</details>
```

### Uncorrelated-inputs fallback (replacing current ## Relevant Attributes table)

```markdown
<details>
<summary>🔗 Other plan inputs (2) — read by this plan but not tied to a specific change</summary>

> These existing values were read to compute the plan. If they change before apply, the plan may be stale.

- `data.azurerm_subscription.current.subscription_id`
- `azurerm_resource_group.main.location`

</details>
```

### No relevant attributes

When `relevant_attributes[]` is absent or empty, nothing is rendered — no change from today's behaviour.

## Success Criteria

- [ ] For each replaced or destroyed resource card, if any `replace_paths` entry correlates (via configuration references) to an upstream `relevant_attributes` entry, a `⚠️ Forced replacement` callout line is rendered inside that card's `<details>` block, above the diff table.
- [ ] The forced-replacement callout names the local attribute, the upstream resource, and the upstream attribute path.
- [ ] When the upstream resource is itself being replaced or destroyed in the same plan, the callout includes the **bold** phrase "changing in this plan".
- [ ] For each replaced or destroyed resource card, a `🔗 Depends on:` line lists **all** `relevant_attributes` entries correlated to that resource (not only those tied to `replace_paths`). The list is not truncated.
- [ ] In-place update resources (`🔧 Change`) do not receive either callout line.
- [ ] Drift-section resources do not receive either callout line.
- [ ] Relevant attributes that could not be correlated to any specific changed resource appear in a collapsible `<details>` fallback section (Example D style) at the end of the report, instead of the current flat `## Relevant Attributes` H2 table.
- [ ] The fallback section is omitted entirely when all relevant attributes were successfully correlated.
- [ ] The existing `## Relevant Attributes` H2 table is removed from the report.
- [ ] Plans without `relevant_attributes[]` produce identical output to the current behaviour (no regression).
- [ ] All existing snapshot tests continue to pass; new snapshot tests cover at minimum: a forced-replacement cascade, a combined card, and the fallback section.

## Open Questions

None — all design questions have been answered by the Maintainer:
1. **Truncation:** list all upstream attributes (no cap).
2. **⚠️ threshold:** only for upstream values that are themselves replaced or destroyed (not in-place updates).
3. **Fallback section:** keep, as shown in Example D.
4. **Drift interaction:** inline annotations apply to planned changes only, not drift.
