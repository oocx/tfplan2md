# Security Analysis Report — tfplan2md

**Analyst:** Claude Opus 4.6  
**Date:** 2026-02-21  
**Scope:** Full codebase review of `oocx/tfplan2md` (branch `copilot/extend-mapping-to-azure-devops`)  
**Focus:** High-severity issues (CVSS v4.0 ≥ 7.0) — sensitive data disclosure, input-driven attacks, rendering correctness bugs that could cause PRs to be accepted with missing/incorrect critical information

---

## Executive Summary

The review identified **4 high-severity findings** and **3 lower-severity findings**. All high-severity issues relate to **sensitive data disclosure** — secrets and sensitive attribute values that Terraform marks as sensitive are rendered in plaintext in the generated PR markdown.

The root cause is an **architectural gap**: Terraform's `before_sensitive` / `after_sensitive` metadata is never propagated from the parsed plan through `ResourceChangeModel` into the Scriban template context. The core attribute-level rendering path (`ReportModelBuilder.ResourceChanges.cs`) correctly masks sensitive values, but all template-based rendering that accesses raw JSON (`before_json` / `after_json`) has **no sensitivity information available** and renders secrets in plaintext.

### Findings Overview

| # | Title | CVSS v4.0 | Severity |
|---|-------|-----------|----------|
| 1 | AzApi body renders sensitive values in plaintext (create/delete) | **8.7** | High |
| 2 | AzApi body renders sensitive values in plaintext (update) | **8.7** | High |
| 3 | `before_json` / `after_json` expose raw Terraform state to all templates | **7.7** | High |
| 4 | Variable Group secret disclosure on `IsSecret` transition | **7.1** | High |
| 5 | Template `include` path traversal | 5.1 | Medium |
| 6 | `BuildDefinitionVariableValues.SecretValue` stored but unused (latent risk) | — | Low |
| 7 | No output path validation | 3.1 | Low |

---

## Threat Model Context

tfplan2md is a .NET 10 CLI tool that converts Terraform plan JSON into markdown reports. These reports are typically posted as PR comments on GitHub or Azure DevOps, making them visible to all repository collaborators.

**Threat actors:**
- **Unintentional exposure:** A developer runs `tfplan2md` on a plan containing sensitive infrastructure secrets (database passwords, API keys, connection strings). The generated markdown is posted to a PR visible to the entire team or organization.
- **Malicious plan injection:** An attacker crafts a Terraform plan JSON file designed to exploit rendering bugs to either leak sensitive data or suppress critical information (e.g., security rule removals).

**Trust boundary:** The Terraform plan JSON is the primary untrusted input. It contains both the resource state values and the sensitivity metadata (`before_sensitive` / `after_sensitive`).

---

## High-Severity Findings

### FINDING 1 — AzApi body renders sensitive values in plaintext (CREATE/DELETE)

| Property | Value |
|----------|-------|
| **CVSS v4.0** | **8.7** — `CVSS:4.0/AV:N/AC:L/AT:N/PR:N/UI:N/VC:H/VI:N/VA:N/SC:N/SI:N/SA:N` |
| **Category** | Sensitive Data Disclosure |
| **Affected component** | AzApi provider — create and delete rendering |
| **Primary location** | `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Rendering.CreateDelete.cs` lines 37–50 |

#### Description

`RenderCreateDeleteBody()` flattens the entire AzApi JSON body and renders every property value into a markdown table with **zero sensitivity checking**. Terraform marks sensitive properties via `before_sensitive` / `after_sensitive` metadata structures, but the create/delete render path never receives or checks this metadata.

#### Root Cause Chain

1. **Template invocation** — `resource.sbn` line 62 calls `render_azapi_body` with `null` for both `beforeSensitive` and `afterSensitive` parameters when the action is `create` or `replace`:
   ```scriban
   {{ render_azapi_body change.after_json.body body_heading "create" null null null false "inline-diff" }}
   ```

2. **No sensitivity parameters** — `RenderCreateDeleteBody()` accepts only `(StringBuilder sb, string heading, object bodyJson, string largeValueFormat)`. There is no sensitivity parameter in its signature.

3. **Direct value rendering** — The method calls `FlattenJson(bodyJson, ...)` and renders all flattened key-value pairs directly to markdown without any masking.

#### Proof of Concept

Given a Terraform plan for an `azapi_resource` with:
```json
{
  "body": "{\"properties\":{\"administratorLoginPassword\":\"P@ssw0rd123!\",\"version\":\"12.0\"}}"
}
```

And `after_sensitive`:
```json
{
  "body": "{\"properties\":{\"administratorLoginPassword\":true}}"
}
```

The generated markdown will contain:
```markdown
| Property | Value |
|----------|-------|
| properties.administratorLoginPassword | `P@ssw0rd123!` |
| properties.version | `12.0` |
```

The password appears in plaintext despite Terraform marking it as sensitive.

#### Impact

Secrets embedded in `azapi_resource` body content — common for Azure REST API resources (storage account keys, connection strings, database passwords, API keys, certificates) — appear verbatim in PR comments visible to all repository collaborators.

#### Recommended Fix

1. Accept `afterSensitive` (create) or `beforeSensitive` (delete) in `RenderCreateDeleteBody()`
2. Flatten the sensitivity structure in parallel with the value structure
3. Mask values where sensitivity is `true` with `(sensitive)`

---

### FINDING 2 — AzApi body renders sensitive values in plaintext (UPDATE)

| Property | Value |
|----------|-------|
| **CVSS v4.0** | **8.7** — `CVSS:4.0/AV:N/AC:L/AT:N/PR:N/UI:N/VC:H/VI:N/VA:N/SC:N/SI:N/SA:N` |
| **Category** | Sensitive Data Disclosure |
| **Affected component** | AzApi provider — update rendering |
| **Primary locations** | `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Data.cs` lines 88–94; `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Rendering.Update.cs` lines 218–226 |

#### Description

Two independent defects combine to expose sensitive values in AzApi update-mode rendering:

**Defect A — Sensitivity metadata never reaches the template context:**

`ResourceChangeModel` (defined in `src/Oocx.TfPlan2Md/MarkdownGeneration/ResourceChangeModel.cs`) has `BeforeJson` and `AfterJson` properties for raw state, but **no** `BeforeSensitive` / `AfterSensitive` properties. `AotScriptObjectMapper` (in `src/Oocx.TfPlan2Md/MarkdownGeneration/AotScriptObjectMapper.cs`) maps `before_json` and `after_json` to the Scriban context, but never maps `before_sensitive` or `after_sensitive`.

As a result, the template's sensitivity check at `resource.sbn` lines 69–70:
```scriban
{{~ before_sensitive_body = change.before_sensitive ? change.before_sensitive.body : null ~}}
{{~ after_sensitive_body = change.after_sensitive ? change.after_sensitive.body : null ~}}
```
…always resolves to `null` because `change.before_sensitive` and `change.after_sensitive` are undefined properties.

**Defect B — `is_sensitive` flag is set but never used for masking:**

`CompareJsonProperties()` in `AzApi.Data.cs` correctly flattens the sensitivity structures and sets `is_sensitive = true` on comparison objects (line 90). However:

- The `showSensitive` parameter is explicitly suppressed as unused with `#pragma warning disable IDE0060` (line 48)
- `RenderUpdateMainTable()` at lines 218–226 renders `before` and `after` values directly without checking `is_sensitive`:
  ```csharp
  var beforeFormatted = FormatAttributeValueTable(path, before?.ToString(), ...);
  var afterFormatted = FormatAttributeValueTable(path, after?.ToString(), ...);
  sb.AppendLine($"| {EscapeMarkdown(path)} | {beforeFormatted} | {afterFormatted} |");
  ```
- No other rendering method (`RenderUpdateGroupedSections`, `RenderUpdatePrefixGroup`, `RenderUpdateArrayGroup`) checks `is_sensitive` either.

The code comment states "sensitive value masking handled by template" but the template layer cannot mask values because it never receives the sensitivity metadata (Defect A).

#### Impact

Same as Finding 1 — sensitive body properties in `azapi_resource` updates appear in plaintext in PR markdown.

#### Recommended Fix

1. Add `BeforeSensitive` / `AfterSensitive` to `ResourceChangeModel`
2. Map them in `AotScriptObjectMapper.MapResourceChange()` as `before_sensitive` / `after_sensitive`
3. In `CompareJsonProperties()`, use the `showSensitive` parameter (remove the warning suppression) to mask values where `is_sensitive` is `true`
4. In all `RenderUpdate*` methods, check `is_sensitive` and render `(sensitive)` instead of the actual value

---

### FINDING 3 — `before_json` / `after_json` expose raw Terraform state to all templates

| Property | Value |
|----------|-------|
| **CVSS v4.0** | **7.7** — `CVSS:4.0/AV:N/AC:L/AT:N/PR:N/UI:N/VC:H/VI:N/VA:N/SC:N/SI:N/SA:N` |
| **Category** | Sensitive Data Disclosure (architectural) |
| **Affected component** | Template rendering pipeline — all providers |
| **Primary location** | `src/Oocx.TfPlan2Md/MarkdownGeneration/AotScriptObjectMapper.cs` (`MapResourceChange` method) |

#### Description

The raw `JsonElement` objects from Terraform's `change.before` and `change.after` are passed through to every Scriban template as `before_json` / `after_json` in the `MapResourceChange` method of `AotScriptObjectMapper.cs`.

The core attribute-level rendering in `ReportModelBuilder.ResourceChanges.cs` (the `BuildAttributeChanges` method, lines 95–104) **correctly** checks `BeforeSensitive` / `AfterSensitive` dictionaries and masks values with `(sensitive)` when `_showSensitive` is false. The hierarchical path checking in `IsSensitiveAttribute()` and `GetHierarchicalPaths()` is properly implemented.

However, the raw JSON mapped as `before_json` / `after_json` **bypasses** this masking entirely. Templates that access these properties get unmasked values. There is no `before_sensitive` / `after_sensitive` counterpart in the template context.

#### Affected Templates

All resource-specific templates that access raw JSON are affected. These include (at minimum):
- `azapi/resource.sbn` — AzApi resource body rendering
- `azurerm/role_assignment.sbn` — Role assignment state access
- `azurerm/network_security_group.sbn` — NSG rule enumeration
- `azurerm/firewall_network_rule_collection.sbn` — Firewall rule access
- Any custom templates provided via `--template-path`

#### Impact

This is the **systemic root cause** underlying Findings 1 and 2. Any template that accesses raw JSON properties can inadvertently expose sensitive values. This affects all current provider-specific templates and any future custom templates.

#### Recommended Fix

1. Add `BeforeSensitive` and `AfterSensitive` properties (type `JsonElement?`) to `ResourceChangeModel`
2. Populate them from `ResourceChange.Change.BeforeSensitive` / `AfterSensitive` in `ReportModelBuilder`
3. Map them as `before_sensitive` / `after_sensitive` in `AotScriptObjectMapper.MapResourceChange()`
4. Update template documentation to instruct template authors to check sensitivity metadata before rendering raw JSON values
5. Consider providing a Scriban helper function like `mask_sensitive(value, is_sensitive)` to standardize masking across templates

---

### FINDING 4 — Variable Group secret disclosure on `IsSecret` transition

| Property | Value |
|----------|-------|
| **CVSS v4.0** | **7.1** — `CVSS:4.0/AV:N/AC:L/AT:N/PR:L/UI:N/VC:H/VI:N/VA:N/SC:N/SI:N/SA:N` |
| **Category** | Sensitive Data Disclosure |
| **Affected component** | AzureDevOps provider — Variable Group formatting |
| **Primary location** | `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/VariableGroupFormatters.cs` line 139 |

#### Description

`VariableGroupFormatters.CreateDiffRow()` only checks `after.IsSecret` when deciding whether to mask a modified variable's diff output:

```csharp
// For secret variables, always show masked value (no diff)
var valueDisplay = after.IsSecret
    ? "`(sensitive / hidden)`"
    : FormatDiff(before.Value, after.Value, format);
```

If a variable transitions from secret (`before.IsSecret = true`) to non-secret (`after.IsSecret = false`), the condition is `false`, and `FormatDiff(before.Value, after.Value, format)` is called — rendering the **old secret value** in the diff output.

#### Comparison with Correct Implementation

`BuildDefinitionFormatters.CreateDiffRow()` (same codebase, same provider) at `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/BuildDefinitionFormatters.cs` line 143 implements this correctly:

```csharp
// SECURITY: For secret variables, always show masked value (no diff)
// If is_secret changes to true, we still mask the value
var valueDisplay = (before.IsSecret || after.IsSecret)
    ? "`(sensitive / hidden)`"
    : FormatVariableValueDiff(before, after, format);
```

The `before.IsSecret || after.IsSecret` disjunction ensures that if **either** the before or after state was secret, the value is masked.

#### Proof of Concept

A Terraform plan that changes a variable group variable from `is_secret = true` to `is_secret = false` will produce a diff row showing:

```markdown
| 🔧 | `api_key` | `s3cr3t-k3y-v@lu3` → `the-new-value` | ... |
```

The old secret value `s3cr3t-k3y-v@lu3` is rendered in the diff.

#### Impact

Secret values in Azure DevOps Variable Groups are exposed when a Terraform plan transitions a variable from secret to non-secret. This is a realistic scenario during secret rotation, variable restructuring, or migration workflows.

#### Recommended Fix

Change line 139 in `VariableGroupFormatters.cs` from:

```csharp
var valueDisplay = after.IsSecret
```

to:

```csharp
var valueDisplay = (before.IsSecret || after.IsSecret)
```

This is a one-line fix that brings parity with the correct implementation in `BuildDefinitionFormatters.CreateDiffRow()`.

---

## Medium and Low Severity Findings

### FINDING 5 — Template `include` path traversal

| Property | Value |
|----------|-------|
| **CVSS v4.0** | **5.1** (Medium) — `CVSS:4.0/AV:L/AC:L/AT:N/PR:N/UI:N/VC:H/VI:N/VA:N/SC:N/SI:N/SA:N` |
| **Category** | Path Traversal |
| **Affected component** | Template loading |
| **Primary location** | `src/Oocx.TfPlan2Md/MarkdownGeneration/TemplateLoader.cs` lines 111–116 |

#### Description

`ScribanTemplateLoader.LoadInternal()` resolves custom template paths via:

```csharp
var customPath = Path.Combine(_customTemplateDirectory, normalized.Replace('/', Path.DirectorySeparatorChar));
if (File.Exists(customPath))
{
    return File.ReadAllText(customPath);
}
```

The `NormalizePath()` method strips leading `/` and standardizes separators but does **not** strip `..` path components. A custom template containing `{{ include "../../etc/shadow" }}` would resolve to a path outside the custom template directory.

#### Mitigating Factors

- Requires the user to supply `--template-path` (custom template directory)
- This is a local CLI tool — the user already has filesystem access
- The attacker would need to control or modify a template file, which is a local-access scenario

#### Recommended Fix

Add path traversal protection in `LoadInternal()`:
```csharp
var fullCustomPath = Path.GetFullPath(customPath);
var fullBaseDir = Path.GetFullPath(_customTemplateDirectory);
if (!fullCustomPath.StartsWith(fullBaseDir, StringComparison.OrdinalIgnoreCase))
{
    return null; // Reject path traversal attempts
}
```

---

### FINDING 6 — `BuildDefinitionVariableValues.SecretValue` stored but unused (latent risk)

| Property | Value |
|----------|-------|
| **CVSS v4.0** | N/A — no current exposure |
| **Category** | Latent Risk / Code Quality |
| **Affected component** | AzureDevOps provider — Build Definition models |
| **Primary location** | `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/BuildDefinitionExtractors.cs` lines 112, 415–420 |

#### Description

`BuildDefinitionExtractors.ExtractVariableValues()` at line 112 reads the `secret_value` field from the Terraform plan JSON and stores it in the `SecretValue` property of the `BuildDefinitionVariableValues` record (defined at lines 415–420):

```csharp
internal record BuildDefinitionVariableValues(
    string Name,
    string? Value,
    bool IsSecret,
    bool? AllowOverride,
    string? SecretValue);
```

However, `.SecretValue` is **never referenced** anywhere in the rendering, formatting, or template code. The secret value is extracted from JSON and stored in memory but never output.

#### Risk

While there is no current exposure, the presence of this field creates a latent risk: if future code accesses `variable.SecretValue` without proper masking, it would expose secret values. The field's existence invites usage.

#### Recommended Fix

Either remove the `SecretValue` field (if it serves no purpose) or add a code comment with `[SecurityCritical]` / `[Obsolete]` annotation making it clear that this value must never be rendered.

---

### FINDING 7 — No output path validation

| Property | Value |
|----------|-------|
| **CVSS v4.0** | **3.1** (Low) — `CVSS:4.0/AV:L/AC:L/AT:N/PR:N/UI:N/VC:N/VI:L/VA:N/SC:N/SI:N/SA:N` |
| **Category** | Path Validation |
| **Affected component** | CLI entry point |
| **Primary location** | `src/Oocx.TfPlan2Md/CLI/ProgramEntry.cs` |

#### Description

`ProgramEntry` writes the generated markdown to the user-specified `--output` path via `File.WriteAllTextAsync(options.OutputFile, markdown)` without any path sanitization or validation. Similarly, input files (`--input`, `--code-analysis-files`, `--template-path`, `--principal-mapping-file`) are read without path validation.

#### Mitigating Factors

- CLI tool — the user controls all paths and already has filesystem access
- No network-facing attack surface
- Standard behavior for CLI tools

#### Recommended Fix

No immediate fix required for a CLI tool. If the tool is ever wrapped in a web service or API, add path validation and sandboxing.

---

## Architectural Observations

### Sensitivity Masking Architecture

The codebase has **two rendering paths** with different sensitivity handling:

1. **Attribute-level rendering** (non-template path via `ReportModelBuilder.ResourceChanges.cs`):
   - ✅ Correctly checks `BeforeSensitive` / `AfterSensitive` dictionaries
   - ✅ Masks values with `(sensitive)` when `_showSensitive` is false
   - ✅ Hierarchical path checking handles nested sensitivity correctly

2. **Template-based rendering** (via `before_json` / `after_json` in Scriban templates):
   - ❌ No sensitivity metadata available (`before_sensitive` / `after_sensitive` not mapped)
   - ❌ Raw unmasked values exposed to all templates
   - ❌ AzApi rendering helpers set `is_sensitive` flag but never use it for masking
   - ❌ Comment says "masking handled by template" but template has no data to mask with

This split creates a false sense of security — the attribute-level path works correctly, but the template path (which is used for all provider-specific semantic rendering) has no protection.

### Scriban Template Sandbox

The Scriban template engine is configured with reasonable defaults:
- ✅ `LoopLimit = 10000` prevents infinite loops
- ✅ Custom `ITemplateLoader` controls template resolution
- ✅ No file I/O or process execution built-in functions enabled
- ✅ Only explicitly registered helper functions are available

### JSON Parsing

- ✅ `System.Text.Json` with source-generated serializers (AOT-compatible)
- ✅ Default `MaxDepth` of 64 protects against deeply nested JSON
- ✅ SARIF parsing uses `JsonDocument.Parse` with standard limits

---

## Recommended Remediation Priority

| Priority | Finding | Effort | Impact |
|----------|---------|--------|--------|
| **P0** | #3 — Map `before_sensitive` / `after_sensitive` to template context | Medium | Fixes the root cause for #1 and #2 |
| **P0** | #4 — Fix `VariableGroupFormatters.CreateDiffRow` `IsSecret` check | Trivial (1 line) | Prevents secret exposure on transition |
| **P1** | #1 — Add sensitivity masking to AzApi create/delete rendering | Medium | Requires #3 first |
| **P1** | #2 — Use `is_sensitive` flag in AzApi update rendering | Medium | Requires #3 first |
| **P2** | #5 — Add path traversal protection in `TemplateLoader` | Small | Defense in depth |
| **P3** | #6 — Remove or annotate `SecretValue` field | Trivial | Reduces latent risk |
