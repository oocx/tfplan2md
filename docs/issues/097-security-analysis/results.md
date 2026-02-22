# Aggregated Security Analysis Results

**Date:** 2026-02-21
**Branch reviewed:** `copilot/extend-mapping-to-azure-devops`
**Models:** Claude Sonnet 4.6, Claude Opus 4.6, GPT-5.3-Codex, Gemini 3.1 Pro

---

## Summary

| # | Title | CVSS v4 (Sonnet 4.6) | CVSS v4 (Opus 4.6) | CVSS v4 (GPT-5.3-Codex) | CVSS v4 (Gemini 3.1 Pro) | Can occur without manipulated input? | Fixed in |
|---|-------|-----------------------|---------------------|--------------------------|--------------------------|--------------------------------------|----------|
| 1 | [AzApi body renders sensitive values in plaintext (create/delete)](#issue-1) | 8.7 | 8.7 | 8.6 | — | **Yes** | [098](../098-sensitive-info-exposure/) |
| 2 | [AzApi body renders sensitive values in plaintext (update)](#issue-2) | 8.7 | 8.7 | 8.2 | — | **Yes** | [098](../098-sensitive-info-exposure/) |
| 3 | [`before_json` / `after_json` expose raw Terraform state to all templates (architectural root cause)](#issue-3) | 7.7 | 7.7 | (covered in #1) | — | **Yes** | [098](../098-sensitive-info-exposure/) |
| 4 | [Variable Group secret disclosure on `IsSecret` transition](#issue-4) | 7.1 | 7.1 | — | — | **Yes** | [098](../098-sensitive-info-exposure/) |
| 5 | [Static-analysis fail gate bypass via malformed SARIF inputs](#issue-5) | 6.5 | 6.8 | 8.0 | — | **Yes** | [099](../099-remaining-security-findings/) |
| 6 | [Root-level `after_sensitive: true` bypasses all attribute masking](#issue-6) | 6.5 | 6.9 | — | — | **Yes** (uncommon but valid Terraform construct) | [098](../098-sensitive-info-exposure/) |
| 7 | [Path traversal via resource type in custom template directory](#issue-7) | 6.0 | 5.1 | — | — | **No** (requires `--template-dir` and crafted plan) | [099](../099-remaining-security-findings/) |
| 8 | [Nested array keys without dot separator miss parent sensitivity check](#issue-8) | 5.5 | 5.9 | — | 7.1 | **Yes** (with certain providers/sensitive patterns) | [098](../098-sensitive-info-exposure/) |
| 9 | [Markdown link injection via SARIF `help_uri`](#issue-9) | 5.5 | 4.8 | — | — | **Yes** (URLs with parentheses) | [099](../099-remaining-security-findings/) |
| 10 | [HTML injection via unencoded `model.Type` in summary HTML](#issue-10) | 5.5 | 4.2 | — | 5.5 | **No** (plans) / **Partially** (SARIF) | [099](../099-remaining-security-findings/) |
| 11 | [Unknown or empty `actions` renders destructive changes as no-op](#issue-11) | 5.5 | 5.5 | — | 5.5 | **Yes** (Terraform 1.7+ `forget` action) | [099](../099-remaining-security-findings/) |
| 12 | [`BuildDefinitionVariableValues.SecretValue` stored but unused (latent risk)](#issue-12) | N/A | N/A | — | — | N/A (latent risk, no current exposure) | [098](../098-sensitive-info-exposure/) (resolved on main by 099) |
| 13 | [No output path validation](#issue-13) | 2.5 | 3.1 | — | (noted, not scored) | N/A (standard CLI behavior) | Won't fix |

---

## Details

### Issue 1
#### AzApi body renders sensitive values in plaintext (create/delete)

| Field | Value |
|-------|-------|
| **Found by** | Claude Opus 4.6 (Finding 1), GPT-5.3-Codex (Finding 1) |
| **Source** | [results.Claude-Opus-4.6.md](results.Claude-Opus-4.6.md) § FINDING 1, [results.GPT-5.3-Codex.md](results.GPT-5.3-Codex.md) § Finding 1 |
| **CVSS v4 (Opus 4.6)** | 8.7 — `CVSS:4.0/AV:N/AC:L/AT:N/PR:N/UI:N/VC:H/VI:N/VA:N/SC:N/SI:N/SA:N` |
| **CVSS v4 (GPT-5.3-Codex)** | 8.6 |
| **Threat class** | Sensitive data disclosure |
| **Primary location** | `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Rendering.CreateDelete.cs` |
| **Can occur without manipulated input?** | **Yes** — normal `terraform show -json` output with sensitive AzApi fields triggers this. |

**Description:**
`RenderCreateDeleteBody()` flattens the entire AzApi JSON body and renders every property value into a markdown table with zero sensitivity checking. The template invocation in `resource.sbn` passes `null` for both `beforeSensitive` and `afterSensitive` parameters during create or replace actions. Terraform marks sensitive properties via `before_sensitive` / `after_sensitive` metadata, but the create/delete render path never receives or checks this metadata.

Both models confirmed this with direct code evidence. GPT-5.3-Codex further demonstrated the issue using an existing test fixture (`azapi-sensitive-plan.json`) whose snapshot output confirms the password appears in plaintext.

**Sonnet 4.6 post-hoc assessment (8.7):** Confirmed. In `AzApi.Rendering.cs`, `RenderAzapiBody` accepts `beforeSensitive` and `afterSensitive` parameters but the non-update branch calls `RenderCreateDeleteBody(sb, heading, bodyJson, largeValueFormat)` with no sensitivity arguments — they are silently dropped. The fix requires threading sensitivity metadata into `RenderCreateDeleteBody` and checking each property key against the flattened sensitive paths before rendering. The existing snapshot test `Snapshot_AzapiSensitive_MatchesBaseline` likely encodes the broken behavior (plaintext values), so its baseline will need to be regenerated alongside the fix to serve as a proper regression guard.

---

### Issue 2
#### AzApi body renders sensitive values in plaintext (update)

| Field | Value |
|-------|-------|
| **Found by** | Claude Opus 4.6 (Finding 2), GPT-5.3-Codex (Finding 2) |
| **Source** | [results.Claude-Opus-4.6.md](results.Claude-Opus-4.6.md) § FINDING 2, [results.GPT-5.3-Codex.md](results.GPT-5.3-Codex.md) § Finding 2 |
| **CVSS v4 (Opus 4.6)** | 8.7 — `CVSS:4.0/AV:N/AC:L/AT:N/PR:N/UI:N/VC:H/VI:N/VA:N/SC:N/SI:N/SA:N` |
| **CVSS v4 (GPT-5.3-Codex)** | 8.2 |
| **Threat class** | Sensitive data disclosure |
| **Primary locations** | `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Data.cs`, `AzApi.Rendering.Update.cs` |
| **Can occur without manipulated input?** | **Yes** — standard AzApi update plan with sensitive properties triggers this. |

**Description:**
Two independent defects combine to expose sensitive values:

1. **Sensitivity metadata never reaches the template context:** `ResourceChangeModel` has `BeforeJson`/`AfterJson` but no `BeforeSensitive`/`AfterSensitive` properties. `AotScriptObjectMapper` maps `before_json` and `after_json` but never maps sensitivity metadata. Templates that reference `change.before_sensitive` / `change.after_sensitive` always resolve to `null`.

2. **`is_sensitive` flag is set but never used for masking:** `CompareJsonProperties()` correctly sets `is_sensitive = true` on comparison objects, but the `showSensitive` parameter is suppressed with `#pragma warning disable IDE0060`. All `RenderUpdate*` methods render raw values without checking `is_sensitive`. A comment states "masking handled by template" but templates have no sensitivity data.

**Sonnet 4.6 post-hoc assessment (8.7):** Confirmed. The `#pragma warning disable IDE0060` suppression on `showSensitive` in `CompareJsonProperties()` is a definitive code smell: the API contract implies sensitivity-aware rendering but the implementation deliberately ignores it. The comment "masking handled by template" is incorrect — templates only receive `before_json`/`after_json` (raw), not `before_sensitive`/`after_sensitive`, so no template-level masking exists. Critically, the two defects compound: even adding masking at the template level would first require propagating sensitivity metadata through `AotScriptObjectMapper` (Issue 3). Fixing Issue 3 alone does not fix this — the unused `showSensitive` in `RenderUpdate*` also needs to be wired up independently.

---

### Issue 3
#### `before_json` / `after_json` expose raw Terraform state to all templates (architectural root cause)

| Field | Value |
|-------|-------|
| **Found by** | Claude Opus 4.6 (Finding 3) |
| **Source** | [results.Claude-Opus-4.6.md](results.Claude-Opus-4.6.md) § FINDING 3 |
| **CVSS v4 (Opus 4.6)** | 7.7 — `CVSS:4.0/AV:N/AC:L/AT:N/PR:N/UI:N/VC:H/VI:N/VA:N/SC:N/SI:N/SA:N` |
| **Threat class** | Sensitive data disclosure (architectural) |
| **Primary location** | `src/Oocx.TfPlan2Md/MarkdownGeneration/AotScriptObjectMapper.cs` (`MapResourceChange` method) |
| **Can occur without manipulated input?** | **Yes** |

**Description:**
This is the systemic root cause underlying Issues 1 and 2. The codebase has two rendering paths with different sensitivity handling:

- **Attribute-level rendering** (via `ReportModelBuilder.ResourceChanges.cs`): Correctly checks `BeforeSensitive`/`AfterSensitive` dictionaries and masks values.
- **Template-based rendering** (via `before_json`/`after_json` in Scriban templates): No sensitivity metadata is available. Raw unmasked `JsonElement` objects are passed through to every template.

All resource-specific templates that access raw JSON are affected, including AzApi, role assignments, NSGs, firewall rules, and any custom templates provided via `--template-path`.

GPT-5.3-Codex also identified this as part of Finding 1 (evidence item #3 regarding `AotScriptObjectMapper` not mapping `before_sensitive`/`after_sensitive`), though they did not score it as a separate architectural finding.

**Sonnet 4.6 post-hoc assessment (7.7):** Confirmed. `MapResourceChange()` in `AotScriptObjectMapper.cs` lines 195–201 explicitly maps `before_json` and `after_json` as raw `ScriptObject` trees but has no corresponding `before_sensitive` / `after_sensitive` mappings. The `ResourceChangeModel` already holds these properties — the data exists in the domain model and is simply never forwarded to the template context. The fix is to add these alongside `before_json`/`after_json` using the same `ConvertToScriptObject` helper, making the change mechanical and low-risk. I rate it at 7.7 rather than higher because exploitation requires a template that actively reads raw JSON properties; templates using the `attribute_changes` array (the attribute-level rendering path) are unaffected.

---

### Issue 4
#### Variable Group secret disclosure on `IsSecret` transition

| Field | Value |
|-------|-------|
| **Found by** | Claude Opus 4.6 (Finding 4) |
| **Source** | [results.Claude-Opus-4.6.md](results.Claude-Opus-4.6.md) § FINDING 4 |
| **CVSS v4 (Opus 4.6)** | 7.1 — `CVSS:4.0/AV:N/AC:L/AT:N/PR:L/UI:N/VC:H/VI:N/VA:N/SC:N/SI:N/SA:N` |
| **Threat class** | Sensitive data disclosure |
| **Primary location** | `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/VariableGroupFormatters.cs` line 139 |
| **Can occur without manipulated input?** | **Yes** — realistic during secret rotation, variable restructuring, or migration workflows. |

**Description:**
`VariableGroupFormatters.CreateDiffRow()` only checks `after.IsSecret` when deciding whether to mask a modified variable's diff output. If a variable transitions from secret (`before.IsSecret = true`) to non-secret (`after.IsSecret = false`), the old secret value is rendered in the diff.

The correct implementation already exists in the same codebase: `BuildDefinitionFormatters.CreateDiffRow()` uses `(before.IsSecret || after.IsSecret)` to mask when either state was secret. This is a one-line fix to bring parity.

**Sonnet 4.6 post-hoc assessment (7.1):** Confirmed. `VariableGroupFormatters.CreateDiffRow()` at line 139 uses `after.IsSecret` in isolation while `BuildDefinitionFormatters.CreateDiffRow()` at line 141 correctly uses `(before.IsSecret || after.IsSecret)`. Both formatters live in the same `AzureDevOps` namespace — the inconsistency suggests `VariableGroupFormatters` was written without cross-referencing its counterpart. Both directions of the transition are problematic: `true → false` exposes the old secret in the before-value diff; `false → true` exposes the plaintext new value in the after-value diff. The `||` pattern handles both. I agree with Opus's 7.1; PR:L is correct because pipeline variable management requires some access level, but the secret value could still reach unintended viewers with PR read access.

---

### Issue 5
#### Static-analysis fail gate bypass via malformed SARIF inputs

| Field | Value |
|-------|-------|
| **Found by** | Claude Sonnet 4.6 (Issue 4), GPT-5.3-Codex (Finding 3) |
| **Source** | [results.Claude-Sonnet-4.6.md](results.Claude-Sonnet-4.6.md) § ISSUE-4, [results.GPT-5.3-Codex.md](results.GPT-5.3-Codex.md) § Finding 3 |
| **CVSS v4 (Sonnet 4.6)** | 6.5 — `CVSS:4.0/AV:L/AC:H/AT:N/PR:L/UI:P/VC:N/VI:H/VA:N/SC:N/SI:N/SA:N` |
| **CVSS v4 (GPT-5.3-Codex)** | 8.0 |
| **Threat class** | Missing critical information / PR gate bypass |
| **Primary location** | `src/Oocx.TfPlan2Md/CodeAnalysis/CodeAnalysisLoader.cs` |
| **Can occur without manipulated input?** | **Yes** — truncated, empty, or tool-incompatible SARIF files are common CI artifact issues. |

**Description:**
`CodeAnalysisLoader.Load` catches all exceptions during SARIF parsing and demotes them to `CodeAnalysisWarning` entries. The findings from the failed file are silently discarded. `HandleCodeAnalysisFailureAsync` then counts findings from the now-incomplete in-memory model and returns exit code `0` if the threshold is not met.

Both models agree this is exploitable: an attacker can replace or corrupt a SARIF file to drop findings, or it can happen accidentally via truncated files, encoding issues, or tool upgrades changing SARIF schema. GPT-5.3-Codex scored this higher (8.0) due to the integrity impact on CI/PR governance workflows, while Sonnet scored it at 6.5 considering the local attack vector and user interaction requirements.

**Opus 4.6 post-hoc assessment (6.8):** Confirmed. The `CodeAnalysisLoadResult.Warnings` collection captures failed files, but the threshold logic in `HandleCodeAnalysisFailureAsync` only counts findings — it never checks whether any warnings exist. This means the gate can pass with zero findings even when one or more SARIF files failed to parse. GPT-5.3-Codex's 8.0 score is arguably too high (AV should be L, not N — modifying CI artifacts requires pipeline access), while Sonnet's AC:H underestimates ease of accidental triggering (truncated files require no attack sophistication). A conservative fix should treat any parse warning as a gate failure unless an explicit `--ignore-sarif-errors` flag is provided.

**Resolution (Issue 099):** `HandleCodeAnalysisFailureAsync` now checks `codeAnalysisInput.Warnings.Count > 0` as the very first step, *before* evaluating finding counts. If any parse warning exists, the method immediately writes each failed file path and error message to stderr and returns `true` (failure). The bypass path is therefore eliminated: a corrupted, truncated, or schema-incompatible SARIF file that previously silently reduced the in-memory finding count to zero now always causes a non-zero exit code when `--fail-on-static-code-analysis-errors` is set. The stderr output also makes the failure diagnosable in CI logs. No opt-out flag was added (per `analysis.md`); the gate is intentionally strict.

---

### Issue 6
#### Root-level `after_sensitive: true` bypasses all attribute masking

| Field | Value |
|-------|-------|
| **Found by** | Claude Sonnet 4.6 (Issue 2) |
| **Source** | [results.Claude-Sonnet-4.6.md](results.Claude-Sonnet-4.6.md) § ISSUE-2 |
| **CVSS v4 (Sonnet 4.6)** | 6.5 — `CVSS:4.0/AV:L/AC:L/AT:N/PR:N/UI:P/VC:H/VI:N/VA:N/SC:N/SI:N/SA:N` |
| **Threat class** | Sensitive data disclosure |
| **Primary location** | `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs` — `IsSensitiveAttribute` / `GetHierarchicalPaths` |
| **Can occur without manipulated input?** | **Yes, potentially** — valid when a resource's output is wrapped in `sensitive()` or derived from a `sensitive = true` variable. Uncommon with major providers but a valid Terraform construct. |

**Description:**
When `after_sensitive` is the JSON boolean `true` (rather than a per-attribute object), `ConvertToFlatDictionary` produces `{"": "true"}`. `GetHierarchicalPaths` splits attribute names on `.`, and for simple names the parent loop never runs — so the empty-string key representing "root value itself" is never checked. Every attribute including passwords, connection strings, and API keys is rendered in plaintext.
**Opus 4.6 post-hoc assessment (6.9):** Confirmed. The same bypass applies symmetrically to `before_sensitive: true` (e.g., when destroying a resource whose state was entirely sensitive). The fix should add an explicit empty-string check at the top of `IsSensitiveAttribute` — if either sensitivity dictionary contains `"" -> "true"`, return `true` immediately for all keys. This is closely related to Issues 1–3 (the AzApi template rendering path bypasses sensitivity entirely), but Issue 6 is distinct because it affects the *attribute-level* rendering path that otherwise handles sensitivity correctly.
---

### Issue 7
#### Path traversal via resource type in custom template directory

| Field | Value |
|-------|-------|
| **Found by** | Claude Sonnet 4.6 (Issue 1), Claude Opus 4.6 (Finding 5) |
| **Source** | [results.Claude-Sonnet-4.6.md](results.Claude-Sonnet-4.6.md) § ISSUE-1, [results.Claude-Opus-4.6.md](results.Claude-Opus-4.6.md) § FINDING 5 |
| **CVSS v4 (Sonnet 4.6)** | 6.0 — `CVSS:4.0/AV:L/AC:L/AT:P/PR:N/UI:P/VC:L/VI:H/VA:N/SC:N/SI:N/SA:N` |
| **CVSS v4 (Opus 4.6)** | 5.1 — `CVSS:4.0/AV:L/AC:L/AT:N/PR:N/UI:N/VC:H/VI:N/VA:N/SC:N/SI:N/SA:N` |
| **Threat class** | Malicious-input attack / path traversal |
| **Primary location** | `src/Oocx.TfPlan2Md/MarkdownGeneration/TemplateLoader.cs` — `LoadInternal` |
| **Can occur without manipulated input?** | **No** — requires `--template-dir` and a crafted plan or template with `..` traversal sequences. |

**Description:**
`NormalizePath` strips a leading `/` and removes the `.sbn` extension but does not sanitize `..` path segments. When a custom template directory is configured, the resource type string from plan JSON (via `ResourceTypeParser.Parse`) is used in `Path.Combine`, potentially resolving outside the template directory.

Both Sonnet and Opus identified the same vulnerability with the same recommended fix: canonicalize the resolved path and assert it is a descendant of the custom template directory.

Sonnet additionally noted the path is re-entered by `TemplateResolver.ResolveTemplate` via the `resolve_template` Scriban helper, compounding the exposure. Opus noted that Scriban's `{{ include "../../..." }}` is also affected.

**Resolution (Issue 099):** Two hardening changes were applied to `ScribanTemplateLoader`:

1. **Constructor:** The custom template directory is now stored as a canonical absolute path via `Path.GetFullPath(customTemplateDirectory)`. This eliminates relative-path tricks at configuration time.
2. **`LoadInternal`:** Before reading any file from the custom directory, the candidate path is canonicalized via `Path.GetFullPath(Path.Combine(_customTemplateDirectory, requestedRelativePath))`. The new `IsPathWithinRoot` helper then asserts that this resolved path starts with the canonical root (using OS-appropriate case comparison — `OrdinalIgnoreCase` on Windows, `Ordinal` elsewhere). Any path that resolves outside the root directory throws `InvalidOperationException` with a message naming both the requested template and the configured directory.

The fix closes all traversal vectors: `../` segments in resource type names, `{{ include "../..." }}` inside templates, and the `resolve_template` Scriban helper all pass through the same canonicalization and containment check. A crafted `..` sequence now produces an exception rather than a file read outside the sandbox.

---

### Issue 8
#### Nested array keys without dot separator miss parent sensitivity check

| Field | Value |
|-------|-------|
| **Found by** | Claude Sonnet 4.6 (Issue 3), Gemini 3.1 Pro (Finding 1) |
| **Source** | [results.Claude-Sonnet-4.6.md](results.Claude-Sonnet-4.6.md) § ISSUE-3, [results.Gemini-3.1-Pro.md](results.Gemini-3.1-Pro.md) § Finding 1 |
| **CVSS v4 (Sonnet 4.6)** | 5.5 — `CVSS:4.0/AV:L/AC:H/AT:N/PR:N/UI:P/VC:H/VI:N/VA:N/SC:N/SI:N/SA:N` |
| **CVSS v4 (Gemini 3.1 Pro)** | 7.1 — `CVSS:4.0/AV:L/AC:L/AT:N/PR:N/UI:N/VC:H/VI:N/VA:N/SC:N/SI:N/SA:N` |
| **Threat class** | Sensitive data disclosure |
| **Primary location** | `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs` — `GetHierarchicalPaths` |
| **Can occur without manipulated input?** | **Yes** — top-level list attributes with parent-level sensitivity marking (e.g., `"tags": true` producing flattened key `tags[0]`) are not uncommon with sensitive variables. |

**Description:**
`GetHierarchicalPaths` generates parent paths by splitting on `.`. For a flattened key using only array-index notation without dots (e.g., `matrix[0][1]`), the split yields a single element, the parent loop never executes, and the array base name is never checked against the sensitivity dictionary.

**Opus 4.6 post-hoc assessment (5.9):** Confirmed. The root cause is that the array-index stripping logic (`parentPath[..parentPath.IndexOf('[')]`) is guarded by the `for (i = parts.Length - 1; i > 0; ...)` loop, which only executes when there's a dot in the key. For a key like `secrets[0]`, `parts.Length == 1`, the loop body never runs, and the parent `secrets` is never yielded. The fix should also strip array indices from the *key itself* before the loop — or better, always yield the array base name for any key containing `[`. I rate AC:H (not L) because triggering this requires a top-level list/set attribute marked sensitive at the parent level without any nested object properties introducing dots — a narrow but real pattern.

**Gemini 3.1 Pro assessment (7.1):** Independently confirmed the same root cause with identical code-level analysis. Gemini rates AC:L and UI:N (yielding 7.1 vs Opus's 5.9), arguing that the vulnerability is straightforward to trigger — any top-level array attribute (e.g., `secrets[1]`) where the parent is marked sensitive will bypass masking without requiring unusual provider configurations. Recommended refactoring `GetHierarchicalPaths` to strip array indices from the key itself before or independently of the dot-splitting loop.

---

### Issue 9
#### Markdown link injection via SARIF `help_uri`

| Field | Value |
|-------|-------|
| **Found by** | Claude Sonnet 4.6 (Issue 5) |
| **Source** | [results.Claude-Sonnet-4.6.md](results.Claude-Sonnet-4.6.md) § ISSUE-5 |
| **CVSS v4 (Sonnet 4.6)** | 5.5 — `CVSS:4.0/AV:L/AC:H/AT:N/PR:L/UI:P/VC:N/VI:H/VA:N/SC:N/SI:N/SA:N` |
| **Threat class** | Incorrect rendering / reviewer deception |
| **Primary location** | `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_code_analysis_findings.sbn` |
| **Can occur without manipulated input?** | **Yes** — legitimate `helpUri` values with parentheses (e.g., Wikipedia-style URLs) produce broken or misleading output. |

**Description:**
The `escape_markdown` helper escapes `\`, `|`, `` ` ``, `&`, and newlines but does not escape `(`, `)`, `[`, or `]`. A SARIF `helpUri` value containing `)` closes the Markdown link URL early, and any text after it appears as raw content in the table cell — enabling visual spoofing of the findings table.

**Opus 4.6 post-hoc assessment (4.8):** Confirmed, though I rate this lower than Sonnet's 5.5. The integrity impact is limited to the rendered markdown table — no system state is modified. The proper fix is to use angle-bracket URL syntax in the template (`[Details](<{{ finding.help_uri | escape_markdown }}>)`), which handles parentheses natively per CommonMark spec §6.7. Alternatively, percent-encode `(` → `%28` and `)` → `%29` in URLs specifically. Escaping `[` and `]` in URLs is less critical because they don't break markdown link syntax when inside the `()` portion, but should be addressed for completeness.

**Resolution (Issue 099):** Two changes eliminate the injection surface:

1. **Template (`_code_analysis_findings.sbn`):** The link is now rendered as `[Details](<{{ finding.help_uri | escape_markdown_link_destination }}>)`. In CommonMark, angle-bracket link destinations are terminated only by a bare `>` — parentheses inside them are inert, so `help_uri` values like `https://example.com/path(with-parens)` now render correctly without escaping.
2. **New helper (`EscapeMarkdownLinkDestination`):** Because the angle-bracket form is still terminated by `>`, a new dedicated helper was introduced. It percent-encodes `<` → `%3C` and `>` → `%3E` and strips embedded newlines. This ensures that no `help_uri` value, however malformed, can prematurely close the angle bracket or inject additional link syntax into the table cell.

The threat is eliminated: neither parentheses nor any other character in a well-formed or adversarial URL can break the link boundary or inject raw content into the surrounding table cell.

---

### Issue 10
#### HTML injection via unencoded `model.Type` in summary HTML

| Field | Value |
|-------|-------|
| **Found by** | Claude Sonnet 4.6 (Issue 6), Gemini 3.1 Pro (Finding 3) |
| **Source** | [results.Claude-Sonnet-4.6.md](results.Claude-Sonnet-4.6.md) § ISSUE-6, [results.Gemini-3.1-Pro.md](results.Gemini-3.1-Pro.md) § Finding 3 |
| **CVSS v4 (Sonnet 4.6)** | 5.5 — `CVSS:4.0/AV:L/AC:L/AT:N/PR:N/UI:P/VC:N/VI:H/VA:N/SC:N/SI:N/SA:N` |
| **CVSS v4 (Gemini 3.1 Pro)** | 5.5 — `CVSS:4.0/AV:L/AC:L/AT:N/PR:N/UI:P/VC:N/VI:H/VA:N/SC:N/SI:N/SA:N` |
| **Threat class** | Incorrect rendering / reviewer deception |
| **Primary location** | `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ResourceSummaryHtmlBuilder.cs` — `BuildSummaryHtml` |
| **Can occur without manipulated input?** | **No** (plan inputs) / **Partially** (SARIF inputs — SARIF `logicalLocation.fullyQualifiedName` is free-form text that could legitimately contain `<`, `>`). |

**Description:**
`BuildSummaryHtml` interpolates `model.Type` directly into the HTML `<summary>` element without HTML-encoding. While `FormatCodeSummary` HTML-encodes `displayName`, `model.Type` is not sanitized. GitHub/Azure DevOps sanitize JavaScript, making XSS unlikely, but visual spoofing via `<b>`, `<s>`, `<span style="display:none">` is achievable — making destructive changes appear benign in collapsed summary rows.

**Opus 4.6 post-hoc assessment (4.2):** Confirmed as a code quality issue, but I rate this lower than Sonnet's 5.5. For Terraform plan inputs, `model.Type` comes from `resource_changes[].type` in the plan JSON, which is always a well-formed provider resource type string (e.g., `azurerm_resource_group`). An attacker who controls the plan JSON already controls the entire input and could inject far more impactful content elsewhere. For SARIF-derived types, the attack surface is marginally larger since `logicalLocation.fullyQualifiedName` is free-form text. The fix is trivial — wrap `model.Type` in `EscapeHtmlForCode()` — and should be applied for defense-in-depth. Note that `model.ActionSymbol` is also interpolated unencoded on the same line, though it is internally assigned and not user-controlled.

**Gemini 3.1 Pro assessment (5.5):** Independently confirmed with identical CVSS vector to Sonnet's score. Gemini cited the same interpolation line in `BuildSummaryHtml` and noted that while `displayName` is properly encoded via `FormatCodeSummary`, `model.Type` is not. Gemini highlighted that even without XSS (due to platform sanitization), formatting tags like `<b>`, `<s>`, `<span style="display:none">` enable visual spoofing to hide malicious infrastructure changes. Recommended wrapping `model.Type` with `EscapeHtmlForCode()` — the same fix proposed by other models.

**Resolution (Issue 099):** `BuildSummaryHtml` now encodes `model.Type` via `HtmlEncoder.Default.Encode(model.Type)` before interpolating it into the `<summary>` HTML string. `.NET`'s `HtmlEncoder` converts `<` → `&lt;`, `>` → `&gt;`, `&` → `&amp;`, and `"` → `&quot;`. A crafted type string like `azurerm_resource_group<span style="display:none">` is therefore rendered as the literal text `azurerm_resource_group&lt;span style=&quot;display:none&quot;&gt;` in the HTML, which platforms display as plain text rather than a formatting element. The visual-spoofing vector is eliminated for both Terraform plan inputs and SARIF-derived resource types.

---

### Issue 11
#### Unknown or empty `actions` renders destructive changes as no-op

| Field | Value |
|-------|-------|
| **Found by** | Claude Sonnet 4.6 (Issue 7), Gemini 3.1 Pro (Finding 2) |
| **Source** | [results.Claude-Sonnet-4.6.md](results.Claude-Sonnet-4.6.md) § ISSUE-7, [results.Gemini-3.1-Pro.md](results.Gemini-3.1-Pro.md) § Finding 2 |
| **CVSS v4 (Sonnet 4.6)** | 5.5 — `CVSS:4.0/AV:L/AC:L/AT:N/PR:N/UI:P/VC:N/VI:H/VA:N/SC:N/SI:N/SA:N` |
| **CVSS v4 (Gemini 3.1 Pro)** | 5.5 — `CVSS:4.0/AV:L/AC:L/AT:N/PR:N/UI:P/VC:N/VI:H/VA:N/SC:N/SI:N/SA:N` |
| **Threat class** | Incorrect rendering / PR gate bypass |
| **Primary location** | `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs` — `DetermineAction` |
| **Can occur without manipulated input?** | **Yes** — Terraform 1.7+ introduced `"actions": ["forget"]` for `removed` blocks with `lifecycle { destroy = false }`. This unrecognized action falls through to no-op, misrepresenting state removal operations. |

**Description:**
`DetermineAction` falls through to `return NoOpAction` for any unrecognized or empty action list. Resources with empty actions or future action types (like `"forget"`) render with the ⊘ no-op icon and are counted in the no-op summary bucket — making them invisible to reviewers scanning for destructive changes.

**Opus 4.6 post-hoc assessment (5.5):** Confirmed, and I agree with Sonnet's 5.5 rating. This directly undermines the tool's core value proposition — making Terraform plan changes visible to reviewers. The `forget` action (Terraform 1.7+, used with `removed` blocks and `lifecycle { destroy = false }`) is a real-world example already in production. Additionally, Terraform 1.8 introduced `"actions": ["forget"]` for `import` blocks with `removed`. The fix should: (1) add explicit handling for `forget` (and potentially `import`) with a distinct icon, (2) render any truly unrecognized action with a ⚠️ warning icon rather than the no-op icon, and (3) emit a diagnostic warning to stderr when an unrecognized action is encountered, so the tool degrades visibly rather than silently.

**Gemini 3.1 Pro assessment (5.5):** Independently confirmed with identical CVSS vector. Gemini specifically identified the `"forget"` action from Terraform 1.7+ `removed` blocks as the primary real-world trigger. Proposed the same three-part fix: (1) add explicit handling for `forget`, (2) use a ⚠️ warning icon for unrecognized actions instead of the no-op ⊘ icon, and (3) emit a diagnostic warning to stderr.

**Resolution (Issue 099):** `DetermineAction` was updated with all three recommended changes:

1. **Explicit `forget` branch:** A new `ForgetAction = "forget"` constant and a dedicated branch in `DetermineAction` return `"forget"` for the Terraform 1.7+ state-removal action. `GetActionSymbol` maps `"forget"` to the delete icon (🗑️), matching the semantic of the operation. In the summary, `forget` contributes to `toDestroy` (not `toChange` or `noOp`).
2. **`unknown` for unrecognized action sets:** Any non-empty action list that does not match a known action constant now returns `"unknown"` rather than `"no-op"`. `GetActionSymbol` maps `"unknown"` to ⚠️, making these resources highly visible in the rendered report rather than invisible. `unknown` contributes to `toChange` in the summary.
3. **Diagnostic stderr warning:** When `DetermineAction` reaches the unknown branch, it writes a human-readable warning to `Console.Error` listing the unrecognized action set. This ensures the tool degrades visibly in CI logs rather than silently misrepresenting the plan.

The `forget` misclassification is fully remediated. Future unrecognized Terraform actions introduced in versions beyond 1.7 will also surface as visible ⚠️ warnings rather than silent no-ops, preventing the same class of PR-gate bypass from recurring.

---

### Issue 12
#### `BuildDefinitionVariableValues.SecretValue` stored but unused (latent risk)

| Field | Value |
|-------|-------|
| **Found by** | Claude Opus 4.6 (Finding 6) |
| **Source** | [results.Claude-Opus-4.6.md](results.Claude-Opus-4.6.md) § FINDING 6 |
| **CVSS v4 (Opus 4.6)** | N/A — no current exposure |
| **Threat class** | Latent risk / code quality |
| **Primary location** | `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/BuildDefinitionExtractors.cs` lines 112, 415–420 |
| **Can occur without manipulated input?** | N/A (no current exposure) |

**Description:**
`BuildDefinitionExtractors.ExtractVariableValues()` reads `secret_value` from the plan JSON and stores it in the `SecretValue` property. However, `SecretValue` is never referenced in rendering, formatting, or templates. While there is no current exposure, the field's existence creates a latent risk: if future code accesses `variable.SecretValue` without proper masking, it would expose secret values.

**Sonnet 4.6 post-hoc assessment (N/A):** Confirmed as a latent code quality issue. An important mitigating factor: the Terraform AzureDevOps provider masks secret variable values in plan output — `secret_value` in `terraform show -json` is typically `null` or empty, not the actual secret. The practical risk of this field containing a real secret is therefore low with the current provider. The `SecretValue` property should still be removed: both the `BuildDefinitionVariableValues` record parameter at lines 415–420 and the corresponding `GetString(varElement, "secret_value")` call at line 112 should be deleted. Retaining an unused field that reads secret-named data creates unnecessary confusion and a surface for future mistakes.

---

### Issue 13
#### No output path validation

| Field | Value |
|-------|-------|
| **Found by** | Claude Opus 4.6 (Finding 7) |
| **Source** | [results.Claude-Opus-4.6.md](results.Claude-Opus-4.6.md) § FINDING 7 |
| **CVSS v4 (Opus 4.6)** | 3.1 — `CVSS:4.0/AV:L/AC:L/AT:N/PR:N/UI:N/VC:N/VI:L/VA:N/SC:N/SI:N/SA:N` |
| **Threat class** | Path validation |
| **Primary location** | `src/Oocx.TfPlan2Md/CLI/ProgramEntry.cs` |
| **Can occur without manipulated input?** | N/A (standard CLI behavior — user already has filesystem access) |

**Description:**
`ProgramEntry` writes generated markdown to the user-specified `--output` path via `File.WriteAllTextAsync` without path sanitization. Similarly, input file paths are read without validation. This is standard CLI tool behavior; the risk is relevant only if the tool is ever wrapped in a web service or API.

**Sonnet 4.6 post-hoc assessment (2.5):** Confirmed as expected CLI behavior. I rate this slightly lower than Opus's 3.1. Any path traversal via `--output` is bounded by the filesystem permissions of the invoking user, who already has write access to the paths they specify. The VI:L metric describes the same write capability the user already possesses. The escalation path to a real vulnerability requires wrapping this in a privileged service, at which point the containing service bears responsibility for input sanitization. No code change is recommended for the current CLI use case; a comment in `ProgramEntry` documenting this constraint would be sufficient.

**Gemini 3.1 Pro assessment (not scored):** Noted the unsanitized output path in `ProgramEntry.cs` but explicitly assessed it as standard CLI tool behavior, not a vulnerability. Gemini concluded the risk is relevant only if the CLI is wrapped in a privileged web service or API without proper input validation at that boundary — aligning with the consensus of the other models.

---

## Cross-Model Coverage

| Issue | Sonnet 4.6 | Opus 4.6 | GPT-5.3-Codex | Gemini 3.1 Pro |
|-------|:----------:|:--------:|:--------------:|:--------------:|
| 1 — AzApi sensitive (create/delete) | | ✅ | ✅ | |
| 2 — AzApi sensitive (update) | | ✅ | ✅ | |
| 3 — before_json/after_json architectural gap | | ✅ | (partial) | |
| 4 — Variable Group IsSecret transition | | ✅ | | |
| 5 — SARIF fail gate bypass | ✅ | | ✅ | |
| 6 — Root-level after_sensitive: true bypass | ✅ | | | |
| 7 — Path traversal in template directory | ✅ | ✅ | | |
| 8 — Nested array keys miss parent sensitivity | ✅ | | | ✅ |
| 9 — Markdown link injection via help_uri | ✅ | | | |
| 10 — HTML injection via model.Type | ✅ | | | ✅ |
| 11 — Unknown/empty actions as no-op | ✅ | | | ✅ |
| 12 — SecretValue stored but unused | | ✅ | | |
| 13 — No output path validation | | ✅ | | (noted) |

**Unique findings per model:**
- **Sonnet 4.6:** Issues 6, 9 (2 unique)
- **Opus 4.6:** Issues 3, 4, 12, 13 (4 unique)
- **GPT-5.3-Codex:** 0 unique (all findings overlapped with Opus)
- **Gemini 3.1 Pro:** 0 unique (all findings overlapped with Sonnet)

**Consensus findings** (found by 2+ models): Issues 1, 2, 5, 7, 8, 10, 11
