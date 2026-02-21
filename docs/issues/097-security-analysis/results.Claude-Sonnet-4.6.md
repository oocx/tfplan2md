# Security Analysis Results — Claude Sonnet 4.6

**Date:** 2026-02-21  
**Model:** Claude Sonnet 4.6  
**Branch reviewed:** `copilot/extend-mapping-to-azure-devops`  
**Scope:** Full source under `src/Oocx.TfPlan2Md/`  

---

## Methodology

A manual static code review was performed focusing on four threat classes requested by the maintainer:

1. **Sensitive-value disclosure** — secrets marked sensitive in the plan are rendered in plaintext.
2. **Malicious-input attacks** — plan JSON or principal-mapping files cause unexpected side-effects (path traversal, file writes, etc.).
3. **Missing critical information** — code-analysis findings or resource changes are silently dropped, making the report incomplete.
4. **Incorrect rendering** — changes are shown with wrong actions/values, leading reviewers to approve a PR they would otherwise reject.

Each finding is rated with an approximate [CVSS v4.0](https://www.first.org/cvss/v4.0/) score.

---

## Findings

### ISSUE-1 — Path Traversal via Resource Type in Custom Template Directory

| Field | Value |
|-------|-------|
| **Severity (CVSS v4)** | ~6.0 (Medium) |
| **CVSS vector** | `CVSS:4.0/AV:L/AC:L/AT:P/PR:N/UI:P/VC:L/VI:H/VA:N/SC:N/SI:N/SA:N` |
| **File** | `src/Oocx.TfPlan2Md/MarkdownGeneration/TemplateLoader.cs` — `LoadInternal` |
| **Threat class** | Malicious-input attack |

#### Description

`NormalizePath` strips a leading `/` and removes the `.sbn` extension suffix, but does **not sanitize `..` path segments**. When a custom template directory is configured via `--template-dir`, the resource type string from the plan JSON is parsed into `{provider}/{resource}` by `ResourceTypeParser.Parse`, which is then passed to `TemplateLoader.LoadInternal`:

```csharp
var customPath = Path.Combine(_customTemplateDirectory,
    normalized.Replace('/', Path.DirectorySeparatorChar));
if (File.Exists(customPath))
    return File.ReadAllText(customPath);  // reads arbitrary *.sbn files
```

#### Exploit

A plan JSON with a resource type containing `..` traversal sequences:

```json
{
  "type": "azurerm_../../../../../../some/path/malicious"
}
```

causes `NormalizePath` to produce `azurerm/../../../../../../some/path/malicious`, which `Path.Combine` resolves against the custom template directory, potentially reading an attacker-controlled `.sbn` file outside that directory. The `.sbn` extension appended by `EnsureExtension` limits the file read to `.sbn` files, but an attacker who plants a malicious `.sbn` template at a predictable path can inject arbitrary Markdown into the report — hiding destructions, fabricating summaries, or impersonating legitimate report sections to trick reviewers.

The same path is re-entered by `TemplateResolver.ResolveTemplate`, which is exposed to Scriban templates via the registered `resolve_template` helper; this means a template that calls `{{ include (resolve_template change.type) }}` re-invokes the loader for every resource, compounding the exposure.

#### Conditions

- Requires the `--template-dir` CLI option to be set.
- Not exploitable when using only embedded resources.

#### Reachable without malicious input?

**No.** Terraform-generated resource type strings are always in the form `provider_resource_name` (lowercase letters, digits, underscores). A legitimate `terraform show -json` or `terraform plan -out … && terraform show -json` output never contains `..` path segments in a resource type. This finding requires a deliberately crafted or manually edited plan JSON.

#### Recommended Fix

Canonicalise the resolved path and assert it is a descendant of the custom template directory:

```csharp
var customPath = Path.GetFullPath(
    Path.Combine(_customTemplateDirectory,
        normalized.Replace('/', Path.DirectorySeparatorChar)));
var root = Path.GetFullPath(_customTemplateDirectory);
if (!customPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
    return null; // refuse path traversal attempts
if (File.Exists(customPath))
    return File.ReadAllText(customPath);
```

---

### ISSUE-2 — Sensitive Attribute Disclosure: Root-Level `after_sensitive: true` Bypasses All Masking

| Field | Value |
|-------|-------|
| **Severity (CVSS v4)** | ~6.5 (Medium-High) |
| **CVSS vector** | `CVSS:4.0/AV:L/AC:L/AT:N/PR:N/UI:P/VC:H/VI:N/VA:N/SC:N/SI:N/SA:N` |
| **File** | `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs` — `IsSensitiveAttribute` / `GetHierarchicalPaths` |
| **Threat class** | Sensitive-value disclosure |

#### Description

`ConvertToFlatDictionary` called on a root-level JSON boolean `true` produces `{"": "true"}` — the empty-string key represents "the root value itself."  `GetHierarchicalPaths` splits on `.` and yields only the unmodified key, iterating one fewer time for each dot. For a simple attribute name (no dots), the parent loop never runs, and only the literal attribute name is yielded — never the empty string.

```csharp
// Trace for after_sensitive: true   (JSON: Boolean "true" at root)
afterSensitiveDict = { "": "true" }     // from ConvertToFlatDictionary(true)

IsSensitiveAttribute("password", {}, { "": "true" })
  GetHierarchicalPaths("password")
    → yields "password"  only
    // loop: for (i = parts.Length-1=0; i > 0; ...) → never runs
  afterSensitiveDict["password"] → miss
  → returns false
  → "password" rendered in plaintext  ❌
```

#### Exploit

A crafted (or accidentally produced) plan where `after_sensitive` is the JSON boolean `true` rather than an attribute-keyed object fully disables masking. Every attribute including passwords, connection strings, and API keys is rendered in plaintext in the PR Markdown regardless of how sensitive those values are. This is a variant of the v1.23.1 sensitive-disclosure class that survives the hierarchical-path fix introduced to address that issue.

In a real scenario this would arise from a custom Terraform provider that incorrectly emits `"after_sensitive": true`, or from a deliberately crafted plan supplied as part of a supply-chain attack.

#### Reachable without malicious input?

**Yes, potentially.** The Terraform plan JSON schema allows `after_sensitive` to be either a per-attribute boolean object *or* a root-level boolean `true` when the entire resource after-state is considered sensitive. This can occur legitimately when:

- A resource's entire output is wrapped in a `sensitive()` call or derived from a `sensitive = true` variable.
- A community or custom provider implements sensitivity marking at the resource level rather than per-attribute.

While uncommon with major providers (AzureRM, AWS, AzAPI), it is a valid Terraform construct and could be encountered in real pipelines without any malicious intent.

#### Recommended Fix

```csharp
// In BuildAttributeChanges, before the per-key loop:
var afterSensitiveIsRoot =
    afterSensitiveDict.TryGetValue("", out var rootSens) && rootSens == "true";
var beforeSensitiveIsRoot =
    beforeSensitiveDict.TryGetValue("", out var rootSensB) && rootSensB == "true";

// Then in the loop:
var isSensitive = afterSensitiveIsRoot || beforeSensitiveIsRoot
    || IsSensitiveAttribute(key, beforeSensitiveDict, afterSensitiveDict);
```

---

### ISSUE-3 — Sensitive Attribute Disclosure: Nested Array Keys Without Dot Separator Miss Parent Sensitivity Check

| Field | Value |
|-------|-------|
| **Severity (CVSS v4)** | ~5.5 (Medium) |
| **CVSS vector** | `CVSS:4.0/AV:L/AC:H/AT:N/PR:N/UI:P/VC:H/VI:N/VA:N/SC:N/SI:N/SA:N` |
| **File** | `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs` — `GetHierarchicalPaths` |
| **Threat class** | Sensitive-value disclosure |

#### Description

`GetHierarchicalPaths` generates parent paths by splitting on `.`. For a flattened key that uses only array-index notation without any dot separator — e.g., `matrix[0][1]` as produced by `JsonFlattener` for a nested array — the split yields a single-element parts array, the `for` loop body never executes, and the only path yielded is the full original key. The parent array name (`matrix`) is never checked.

```csharp
// key = "matrix[0][1]"
var parts = key.Split('.');     // → ["matrix[0][1]"]  (length 1)
// for (i = length-1 = 0; i > 0; i--) → never iterates

// Sensitive dict contains: { "matrix": "true" }
// "matrix" is never checked → IsSensitiveAttribute returns false → value exposed  ❌
```

#### Conditions

Requires a Terraform resource attribute that is a nested array (no intermediate object key at the relevant flattened path level) with the parent array container marked sensitive rather than individual elements. Unusual in standard Terraform provider schemas but reachable with crafted plans or custom providers.

#### Reachable without malicious input?

**Yes, with certain providers or sensitive variable patterns.** When a `sensitive = true` variable or `sensitive()` call is used as the value for a list attribute, Terraform can mark the entire list as `true` in the sensitive structure rather than marking individual elements. For example, an `azurerm_key_vault` resource with a `network_acls[*].ip_rules` list whose value comes from a sensitive variable would produce `"after_sensitive": {"network_acls": [{"ip_rules": true}]}`, which after flattening by `JsonFlattener` yields keys like `network_acls[0].ip_rules[0]` with the sensitive dict containing `network_acls[0].ip_rules`. That specific key *does* get checked. However, a simpler top-level list attribute — e.g., `"tags": ["sensitive-tag-1"]` with sensitive marking `{"tags": true}` — produces `tags[0]` as the flattened key and `tags` as the sensitive dict key, which is the exact gap described above. Such patterns occur in real infrastructure code.

#### Recommended Fix

Extract the base name (before the first `[`) as an additional candidate path:

```csharp
private static IEnumerable<string> GetHierarchicalPaths(string key)
{
    yield return key;

    var parts = key.Split('.');
    for (var i = parts.Length - 1; i > 0; i--)
    {
        var parentPath = string.Join('.', parts.Take(i));
        if (parentPath.Contains('['))
        {
            yield return parentPath[..parentPath.IndexOf('[')];
        }
        yield return parentPath;
    }

    // Also yield the root array name for pure-index keys (e.g., "matrix[0][1]" → "matrix")
    if (parts.Length == 1 && key.Contains('['))
    {
        yield return key[..key.IndexOf('[')];
    }
}
```

---

### ISSUE-4 — Code Analysis: Silent Exception Suppression Drops Security Findings, Enabling PR Gate Bypass

| Field | Value |
|-------|-------|
| **Severity (CVSS v4)** | ~6.5 (Medium-High) |
| **CVSS vector** | `CVSS:4.0/AV:L/AC:H/AT:N/PR:L/UI:P/VC:N/VI:H/VA:N/SC:N/SI:N/SA:N` |
| **File** | `src/Oocx.TfPlan2Md/CodeAnalysis/CodeAnalysisLoader.cs` — `Load` |
| **Threat class** | Missing critical information / PR gate bypass |

#### Description

`CodeAnalysisLoader.Load` catches all exceptions thrown during SARIF parsing and demotes them to `CodeAnalysisWarning` entries. The findings from the failed file are silently discarded. `HandleCodeAnalysisFailureAsync` then counts findings from the now-incomplete in-memory model and returns exit code `0` if the threshold is not met:

```csharp
catch (Exception ex)
{
    warnings.Add(new CodeAnalysisWarning { FilePath = file, Message = ex.Message });
    // ⚠️ findings from this file are gone; process continues
}
```

#### Exploit

When a CI/CD pipeline uses `--fail-on-static-code-analysis-errors=high`:

1. An attacker replaces or corrupts a SARIF file in the CI artifact store so that JSON parsing throws.
2. All high-severity findings in that file are dropped.
3. `HandleCodeAnalysisFailureAsync` counts zero failures and returns exit code `0`.
4. The pipeline passes the quality gate.

A warning **is** rendered in the Markdown report, but:
- It appears as informational text, not an error.
- The process exit code remains `0`.
- Most CI/CD quality gates act on exit code, not report contents.

The warnings appearing in the report provide incomplete mitigation — a reviewer acting on the rendered report would see the warning, but automated gates relying solely on exit codes would not trigger a failure.

#### Reachable without malicious input?

**Yes.** This is a realistic non-malicious failure mode. SARIF files produced by static analysis tools may be:

- **Empty** (tool ran but found nothing and wrote a 0-byte file, or the file was not created at all and the glob pattern matched something else).
- **Truncated** (CI runner hit disk quota mid-write).
- **Schema-variant** (a tool upgrade changed output format in a way the parser does not yet handle, causing a `JsonException`).
- **Encoding issues** (BOM or non-UTF-8 encoding causing parse failure).

In all these situations, a team relying on `--fail-on-static-code-analysis-errors` to gate PRs would silently lose the gate without realising it.

#### Recommended Fix

Options in order of preference:

**Option A** — Treat any SARIF parse failure as a non-zero exit code:

```csharp
// In ProgramEntry: check warnings count and return 1 if any warnings exist
// when --fail-on-static-code-analysis-errors is set
if (services.CodeAnalysisInput?.FailOnLevel is not null
    && services.CodeAnalysisInput.Model.Warnings.Count > 0)
{
    await Console.Error.WriteLineAsync("One or more SARIF files could not be parsed.");
    return 1;
}
```

**Option B** — Add a dedicated `--fail-on-sarif-parse-error` flag so pipeline authors can opt in to hard failure.

---

### ISSUE-5 — Markdown Link Injection via SARIF `help_uri`

| Field | Value |
|-------|-------|
| **Severity (CVSS v4)** | ~5.5 (Medium) |
| **CVSS vector** | `CVSS:4.0/AV:L/AC:H/AT:N/PR:L/UI:P/VC:N/VI:H/VA:N/SC:N/SI:N/SA:N` |
| **File** | `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_code_analysis_findings.sbn` |
| **Threat class** | Incorrect rendering / reviewer deception |

#### Description

The `escape_markdown` helper escapes `\`, `|`, `` ` ``, `&`, and newlines but does **not** escape `(`, `)`, `[`, or `]`. The `_code_analysis_findings.sbn` template inlines `finding.help_uri` directly into a Markdown link:

```scriban
| ... | {{ if finding.help_uri }}[Details]({{ finding.help_uri | escape_markdown }}){{ else }}-{{ end }} |
```

A SARIF `helpUri` value containing `)` closes the link URL early, and any text after it lands in the Markdown body of the table cell:

```
help_uri = "http://a.example.com) ❌ APPROVED — All clear [Fake](http://attacker.example.com"
```

renders as:

```markdown
[Details](http://a.example.com) ❌ APPROVED — All clear [Fake](http://attacker.example.com)
```

#### Conditions

Requires injecting a crafted SARIF file into the pipeline — e.g., by tampering with SARIF artifacts in a CI/CD artifact store, or by contributing a Terraform module change that influences a SARIF generator.

#### Reachable without malicious input?

**Yes, with some SARIF generators.** Legitimate `helpUri` values sometimes contain parentheses — this is common in:

- **Wikipedia-style URLs**: `https://en.wikipedia.org/wiki/Role-based_access_control_(RBAC)`
- **Documentation sites** with query parameters using parentheses: `https://docs.example.com/rules?filter=(active)`
- **Azure documentation links** with disambiguation suffixes.

In these cases no attack is intended, but the rendering will still produce broken or visually misleading output in the findings table — the `)` terminates the Markdown link early and the remainder appears as raw text in the PR.

#### Recommended Fix

URL-encode the `help_uri` before inserting it into a Markdown link, or restrict the rendered URI to known-good prefixes:

```csharp
// In ScribanHelpers Markdown.cs – add a helper specifically for URIs
public static string EscapeMarkdownUri(string? uri)
{
    if (string.IsNullOrEmpty(uri)) return string.Empty;
    // Replace characters that can break Markdown link syntax
    return uri.Replace("(", "%28").Replace(")", "%29")
              .Replace("[", "%5B").Replace("]", "%5D")
              .Replace(" ", "%20");
}
```

And register it:

```csharp
scriptObject.Import("escape_markdown_uri", new Func<string?, string>(EscapeMarkdownUri));
```

Then update the template:

```scriban
{{ if finding.help_uri }}[Details]({{ finding.help_uri | escape_markdown_uri }}){{ else }}-{{ end }}
```

---

### ISSUE-6 — HTML Injection via Unencoded `model.Type` in Summary HTML

| Field | Value |
|-------|-------|
| **Severity (CVSS v4)** | ~5.5 (Medium) |
| **CVSS vector** | `CVSS:4.0/AV:L/AC:L/AT:N/PR:N/UI:P/VC:N/VI:H/VA:N/SC:N/SI:N/SA:N` |
| **File** | `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ResourceSummaryHtmlBuilder.cs` — `BuildSummaryHtml` |
| **Threat class** | Incorrect rendering / reviewer deception |

#### Description

`BuildSummaryHtml` interpolates `model.Type` (the resource type string verbatim from the plan JSON) directly into the HTML `<summary>` element prefix without HTML-encoding:

```csharp
var prefix = $"{model.ActionSymbol}\u00A0{model.Type} <b>{FormatCodeSummary(displayName)}</b>";
```

`FormatCodeSummary` HTML-encodes `displayName`, but `model.Type` is not touched. A plan with:

```json
{ "type": "azurerm_nsg<s style=\"color:green\">✅ Safe change</s>" }
```

injects `<s>` (strike-through) or `<span>` tags into the `<summary>` element of every resource collapsible block in the PR.

The same injection path is reachable from the SARIF code-path: `GetOrCreateResourceChange` → `ParseResourceTypeAndName` uses the last two dot-separated tokens of a SARIF `logicalLocation.fullyQualifiedName` as type and name, with no sanitisation.

#### Real-World Impact

GitHub and Azure DevOps both sanitize JavaScript in PR Markdown, making XSS execution unlikely. However, **visual spoofing** is still achievable: tags like `<b>`, `<s>`, `<span style="display:none">`, `<img>` can make a critical destructive change appear to be a benign update in the collapsed summary row that reviewers skim before expanding.

#### Reachable without malicious input?

**No, for plan-file inputs.** All standard Terraform providers and Terraform's own plan serialisation produce resource types using only lowercase letters, digits, and underscores (`[a-z0-9_]+`). `<`, `>`, and similar HTML-significant characters never appear in a legitimately generated plan JSON resource type.

**Partially, via SARIF inputs.** The SARIF code-path `GetOrCreateResourceChange → ParseResourceTypeAndName` derives a resource `type` from the last two dot-separated tokens of a `logicalLocation.fullyQualifiedName`. The `fullyQualifiedName` field in SARIF is free-form text and could legitimately include characters such as `<`, `>`, or `/` (e.g., C++ template instantiations, generic type names). If such a SARIF file is supplied to tfplan2md alongside a plan, those characters would be injected into the summary HTML without encoding — no malicious intent required.

#### Recommended Fix

HTML-encode `model.Type` and `model.ActionSymbol` before interpolation:

```csharp
var prefix = $"{HtmlEncode(model.ActionSymbol)}\u00A0{HtmlEncode(model.Type)} <b>{FormatCodeSummary(displayName)}</b>";
```

`HtmlEncode` is already a private helper in `ScribanHelpers/Markdown.cs`; expose it at `internal` access from `ResourceSummaryHtmlBuilder`.

---

### ISSUE-7 — Unknown or Empty `actions` List Renders Destructive Changes as No-Op

| Field | Value |
|-------|-------|
| **Severity (CVSS v4)** | ~5.5 (Medium) |
| **CVSS vector** | `CVSS:4.0/AV:L/AC:L/AT:N/PR:N/UI:P/VC:N/VI:H/VA:N/SC:N/SI:N/SA:N` |
| **File** | `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs` — `DetermineAction` |
| **Threat class** | Incorrect rendering / PR gate bypass |

#### Description

`DetermineAction` falls through to `return NoOpAction` for any unrecognised or empty action list:

```csharp
private static string DetermineAction(IReadOnlyList<string> actions)
{
    if (actions.Contains(CreateAction) && actions.Contains(DeleteAction)) return ReplaceAction;
    if (actions.Contains(CreateAction)) return CreateAction;
    if (actions.Contains(DeleteAction)) return DeleteAction;
    if (actions.Contains(UpdateAction)) return UpdateAction;
    if (actions.Contains(ReadAction)) return ReadAction;
    return NoOpAction; // ← catches [], ["unknown_future_action"], etc.
}
```

A plan resource with `"actions": []` renders with the ⊘ no-op icon. The resource is still **visible** in the report body, but:

- It is counted in the **no-op** summary bucket, not in the **Destroy** bucket.
- The Summary table shows `❌ Destroy | 0` — a reviewer scanning for destructions misses it.
- The `<summary>` element shows the no-op icon, so visual skimming of the collapsed resource list also misses the severity.

Future Terraform versions may introduce new action types (e.g., `"forget"`, already present in recent Terraform versions for state removal). These would also fall through to no-op, potentially misrepresenting impactful state operations.

#### Reachable without malicious input?

**Yes — this is a real gap with current Terraform versions.** Terraform 1.7 introduced the `removed` block with `lifecycle { destroy = false }`, which produces `"actions": ["forget"]` in the plan JSON. `"forget"` removes a resource from the Terraform state without destroying the actual infrastructure. This action:

- Is not recognised by `DetermineAction` and falls through to `no-op`.
- Is displayed with the ⊘ no-op icon.
- Is counted in the no-op summary bucket rather than a dedicated "forget" or "remove from state" bucket.

A reviewer looking at the Summary table would see `❌ Destroy | 0` and might miss that a resource is being detached from state management — which could be as significant as a destroy in certain compliance scenarios. This is a straightforward bug triggered by normal `terraform plan` output on a codebase that uses `removed` blocks.

#### Recommended Fix

```csharp
private static string DetermineAction(IReadOnlyList<string> actions)
{
    if (actions.Count == 0)
    {
        // Terraform should never emit an empty actions list for a real change.
        // Treat as unknown rather than silently masking as no-op.
        return "unknown";
    }
    if (actions.Contains(CreateAction) && actions.Contains(DeleteAction)) return ReplaceAction;
    if (actions.Contains(CreateAction)) return CreateAction;
    if (actions.Contains(DeleteAction)) return DeleteAction;
    if (actions.Contains(UpdateAction)) return UpdateAction;
    if (actions.Contains(ReadAction)) return ReadAction;
    if (actions.Contains(NoOpAction)) return NoOpAction;
    // Unknown future action types
    return $"unknown:{string.Join(",", actions)}";
}
```

Add a corresponding icon mapping in `GetActionSymbol` for `"unknown"` (e.g., ❓) and ensure the summary table counts unknown-action resources separately so reviewers are alerted rather than misled.

---

## Summary

| # | Title | CVSS v4 (approx.) | Threat Class |
|---|-------|:-----------------:|--------------|
| 1 | Path traversal via resource type in custom template directory | **6.0** | Malicious-input attack |
| 2 | `after_sensitive: true` bypasses all attribute masking | **6.5** | Sensitive-value disclosure |
| 3 | Nested array keys without dot separator miss parent sensitivity | **5.5** | Sensitive-value disclosure |
| 4 | Silent SARIF exception drops findings → exit code 0 | **6.5** | Missing critical info / PR gate bypass |
| 5 | Markdown link injection via SARIF `help_uri` | **5.5** | Incorrect rendering / reviewer deception |
| 6 | HTML injection via unencoded `model.Type` in summary | **5.5** | Incorrect rendering / reviewer deception |
| 7 | Empty/unknown `actions` renders destructive changes as no-op | **5.5** | Incorrect rendering / PR gate bypass |

None of the findings reach the CVSS 4.0 **High** threshold (≥ 8.0) in isolation, because the attack vector is always **Local** (the attacker must control an input file) and **User Interaction** is required (a human must run tfplan2md). Issues 2 and 4 are the most impactful: issue 2 can expose all secrets in a plan with a single malformed attribute, and issue 4 can silently disable the entire code-analysis PR gate with a corrupted SARIF file.

Issues 5, 6, and 7 are reviewer-deception risks: they do not expose data, but they can cause a security-critical change to appear safe or absent in the PR report, leading a reviewer to approve what they should reject.
