# Security Fix: Sensitive Attribute Disclosure in Array/Nested Structures

This patch fixes a **HIGH severity** security vulnerability where sensitive attribute values inside arrays and nested objects were exposed in markdown reports even when the `--show-sensitive` flag was not set.

## 🔒 Security fixes

### Fixed sensitive data disclosure in array and nested attributes

**Problem:** Sensitive attribute values inside arrays or nested objects were displayed in plain text in markdown reports, even when the `--show-sensitive` flag was not provided. This caused secrets (API keys, passwords, tokens) to be exposed in PR comments and CI/CD logs.

**Example affected resources:**
- `azuredevops_build_definition` — `variable[0].secret_value` shown in plain text
- `azuredevops_variable_group` — `secret_variable[0].value` shown in plain text
- Any Terraform resource with array-typed sensitive attributes

**Symptom:** When running `tfplan2md plan.json` (without `--show-sensitive`), reports displayed actual secret values like `variable[0].secret_value: my-secret-123` instead of masking them as `variable[0].secret_value: (sensitive)`.

**Root cause:** The `IsSensitiveAttribute()` method in `ReportModelBuilder.ResourceChanges.cs` only performed exact key matching against Terraform's `after_sensitive` markers. When Terraform marks an entire array as sensitive (e.g., `"variable": true`), individual array items have flattened paths like `variable[0].secret_value`, which didn't match the exact key `variable` and were therefore not masked.

**Fix:** Implemented hierarchical sensitivity checking. The method now checks not only the exact attribute key but also all parent paths in the hierarchy. For example, when evaluating `variable[0].secret_value`, it now checks:
1. `variable[0].secret_value` (exact match)
2. `variable[0]` (parent array item)
3. `variable` (parent array)

If any parent path is marked as sensitive in Terraform's plan JSON, the attribute value is masked.

### Impact on reports

- **Before:** Secret values in arrays/nested structures were exposed regardless of `--show-sensitive` flag
- **After:** Secret values are properly masked as `(sensitive)` by default and only shown when `--show-sensitive` is explicitly set

### Security Impact

**Severity:** HIGH — Secrets could be disclosed in reports shared publicly or stored in insecure locations.

**Attack scenario:**
1. Developer runs `terraform plan -out=plan.tfplan` with resources containing array-based secrets
2. Developer runs `terraform show -json plan.tfplan > plan.json`
3. Developer runs `tfplan2md plan.json` (without `--show-sensitive`)
4. Developer posts markdown report as a PR comment
5. **Result:** Secret values (API keys, passwords, tokens) visible in PR comment
6. **Impact:** Secrets exposed to unauthorized users with PR read access

**Mitigation:** Users affected by this vulnerability should:
- **Upgrade immediately** to this release
- **Review past PR comments** for exposed secrets and rotate any credentials that may have been disclosed
- **Avoid using older versions** of tfplan2md with resources containing sensitive array attributes

## 🔗 Commits

- [`11f816a`](https://github.com/oocx/tfplan2md/commit/11f816ae) fix: prevent sensitive data disclosure for array/nested attributes

## 🧪 Test coverage

Added 3 comprehensive unit tests to verify the fix:

1. **Array-based sensitive attributes** — Verifies `variable[0].secret_value` is masked when parent array is marked sensitive
2. **Nested object sensitive attributes** — Verifies `config.database.password` is masked when parent object is marked sensitive
3. **Multi-level nested arrays** — Verifies deeply nested paths like `items[0].subitems[1].secret` are masked correctly

All 1,132 tests passing.

## 📚 Related Documentation

The `--show-sensitive` flag is documented in:
- **README.md** — CLI options section
- **docs/features.md** — Sensitive Values section (lines 823-826)

Users should review these resources to understand the proper usage of the `--show-sensitive` flag:
- By default, sensitive values are **masked** as `(sensitive)` for security
- Use `--show-sensitive` only in secure environments where sensitive data disclosure is acceptable
- Always review generated reports before sharing them in public channels
