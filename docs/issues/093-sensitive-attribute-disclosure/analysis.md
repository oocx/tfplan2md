# Issue Analysis: Sensitive Attribute Disclosure

## Problem Description

Sensitive attribute values (e.g., `secret_value` on `azuredevops_build_definition.variable`) are being shown in tfplan2md markdown reports even when the `--show-sensitive` flag is not set. This is a **security vulnerability** because sensitive values will appear in pull request comments and reports.

The `--show-sensitive` flag is designed to control whether sensitive values are masked, but it is not working correctly for attributes inside arrays/nested objects.

## Root Cause Analysis

### How Terraform plan.json Represents Sensitive Attributes

Terraform's plan.json uses `before_sensitive` and `after_sensitive` fields to mark sensitive values. There are two patterns:

**Pattern 1: Simple Attributes** (✅ Works correctly)
```json
{
  "change": {
    "after": {
      "primary_access_key": "supersecretkey123"
    },
    "after_sensitive": {
      "primary_access_key": true
    }
  }
}
```

When flattened:
- Attribute path: `primary_access_key`
- Sensitive marker: `primary_access_key` → `"true"`
- **Result**: ✅ Match found, value is correctly masked

**Pattern 2: Array/Nested Attributes** (❌ Currently broken)
```json
{
  "change": {
    "after": {
      "variable": [
        {
          "name": "BUILD_CONFIG",
          "secret_value": "my-secret-123",
          "value": "Release"
        }
      ]
    },
    "after_sensitive": {
      "variable": true
    }
  }
}
```

When flattened:
- Attribute paths: `variable[0].name`, `variable[0].secret_value`, `variable[0].value`
- Sensitive marker: `variable` → `"true"` (marks the entire array)
- **Result**: ❌ No match found, values are NOT masked

### The Bug in the Code

File: `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs`

Lines 129-137:
```csharp
private static bool IsSensitiveAttribute(
    string key,
    Dictionary<string, string?> beforeSensitive,
    Dictionary<string, string?> afterSensitive)
{
    // Check if the key is marked as sensitive in either before or after state
    return (beforeSensitive.TryGetValue(key, out var bv) && bv == "true")
        || (afterSensitive.TryGetValue(key, out var av) && av == "true");
}
```

**The problem**: This method performs an **exact key match**. It checks if `variable[0].secret_value` exists in the sensitive dictionary, but when Terraform marks an entire array as sensitive, the dictionary only contains `variable` → `"true"`, not the individual item paths.

### Example of the Vulnerability

Given this Azure DevOps build definition:

```hcl
resource "azuredevops_build_definition" "example" {
  # ... other fields ...
  
  variable {
    name         = "API_KEY"
    secret_value = "super-secret-api-key-12345"
    is_secret    = true
  }
}
```

**Current behavior** (without fix):
- The report shows: `variable[0].secret_value` → `"super-secret-api-key-12345"`
- The secret is **exposed in the markdown report**
- This appears in PR comments, CI/CD logs, etc.

**Expected behavior**:
- When `--show-sensitive` is NOT set: `variable[0].secret_value` → `"(sensitive)"`
- When `--show-sensitive` IS set: `variable[0].secret_value` → `"super-secret-api-key-12345"`

## Affected Code Paths

### Primary Code Path
1. **Entry point**: `src/Oocx.TfPlan2Md/CompositionRoot.cs:212`
   - `ShowSensitive` flag is passed to `ReportModelBuilder`

2. **Sensitive checking**: `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs:129-137`
   - `IsSensitiveAttribute()` method checks if an attribute is sensitive
   - **BUG LOCATION**: Only checks for exact key match, doesn't check parent paths

3. **Masking logic**: `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs:103-104`
   - If `isSensitive && !_showSensitive`, value is replaced with `"(sensitive)"`
   - Otherwise, the actual value is shown

### Supporting Code
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/JsonFlattener.cs:20-71`
  - Flattens nested JSON into dotted paths (e.g., `variable[0].secret_value`)
  - This is working correctly; the bug is not in the flattener

## Examples of Sensitive Attribute Names

Based on common Terraform provider patterns, these attribute names commonly contain secrets:

| Attribute Name | Example Provider | Resource Type |
|----------------|------------------|---------------|
| `secret_value` | azuredevops | build_definition.variable |
| `password` | azurerm, aws, google | database, vm, user resources |
| `secret_key` | aws | iam_access_key |
| `client_secret` | azuread, azurerm | application, service_principal |
| `access_token` | github, gitlab | personal_access_token |
| `api_key` | various | api_key resources |
| `primary_access_key` | azurerm | storage_account |
| `secondary_access_key` | azurerm | storage_account |
| `connection_string` | azurerm | various resources |
| `private_key` | tls, aws | certificate, key_pair |
| `oauth_token` | various | oauth resources |
| `webhook_secret` | github | repository_webhook |

**Note**: The current implementation should NOT use heuristics based on attribute names. The proper fix is to respect Terraform's explicit sensitivity markers in the plan.json.

## Current Behavior vs Expected Behavior

### Test Case: Azure DevOps Build Definition with Secret Variable

**Input plan.json** (simplified):
```json
{
  "resource_changes": [{
    "address": "azuredevops_build_definition.example",
    "change": {
      "after": {
        "variable": [{
          "name": "API_KEY",
          "secret_value": "my-secret-123",
          "is_secret": true
        }]
      },
      "after_sensitive": {
        "variable": true
      }
    }
  }]
}
```

**Current behavior** (broken):
```
tfplan2md plan.json
# Shows:
# variable[0].secret_value: my-secret-123  ⚠️ SECRET EXPOSED!

tfplan2md --show-sensitive plan.json
# Shows:
# variable[0].secret_value: my-secret-123  ✓ Expected when flag is set
```

**Expected behavior** (after fix):
```
tfplan2md plan.json
# Shows:
# variable[0].secret_value: (sensitive)  ✓ Masked by default

tfplan2md --show-sensitive plan.json
# Shows:
# variable[0].secret_value: my-secret-123  ✓ Shown when explicitly requested
```

## Recommended Fix Approach

### Solution: Hierarchical Sensitivity Check

Modify `IsSensitiveAttribute()` to check not just the exact key, but also all parent paths in the hierarchy.

**Algorithm**:
```
For key "variable[0].secret_value":
1. Check if "variable[0].secret_value" is marked sensitive → NO
2. Check if "variable[0]" is marked sensitive → NO
3. Check if "variable" is marked sensitive → YES ✓
4. Return true (attribute is sensitive)
```

**Implementation approach** (pseudocode):
```csharp
private static bool IsSensitiveAttribute(
    string key,
    Dictionary<string, string?> beforeSensitive,
    Dictionary<string, string?> afterSensitive)
{
    // Check the key itself and all parent paths
    var pathsToCheck = GetHierarchicalPaths(key);
    
    foreach (var path in pathsToCheck)
    {
        if ((beforeSensitive.TryGetValue(path, out var bv) && bv == "true")
            || (afterSensitive.TryGetValue(path, out var av) && av == "true"))
        {
            return true;
        }
    }
    
    return false;
}

private static IEnumerable<string> GetHierarchicalPaths(string key)
{
    // For "variable[0].secret_value", return:
    // 1. "variable[0].secret_value"
    // 2. "variable[0]"
    // 3. "variable"
    
    yield return key;
    
    var parts = key.Split('.');
    for (int i = parts.Length - 1; i > 0; i--)
    {
        var parentPath = string.Join('.', parts.Take(i));
        
        // Strip array index if present (e.g., "variable[0]" → "variable")
        if (parentPath.Contains('['))
        {
            var arrayName = parentPath.Substring(0, parentPath.IndexOf('['));
            yield return arrayName;
        }
        
        yield return parentPath;
    }
}
```

### Edge Cases to Consider

1. **Multi-level nesting**: `repository[0].secrets[1].value`
   - Should check: `repository[0].secrets[1].value`, `repository[0].secrets[1]`, `repository[0].secrets`, `repository[0]`, `repository`

2. **Mixed sensitive arrays**: Some items marked, some not
   - If `variable` is marked `true`, all items should be masked
   - If `variable[0]` is marked `true` but not `variable[1]`, only index 0 should be masked

3. **Empty arrays**: No values to mask
   - Should not cause errors

4. **Object properties in arrays**: `config[0].database.password`
   - Should check all parent levels including the object path

## Security Impact

### Severity
**HIGH** - Secrets are disclosed in reports that may be shared publicly or stored in insecure locations.

### Attack Scenario
1. Developer runs `terraform plan -out=plan.tfplan`
2. Developer runs `terraform show -json plan.tfplan > plan.json`
3. Developer runs `tfplan2md plan.json` (without `--show-sensitive`)
4. Developer posts the markdown report as a PR comment
5. **Result**: Secret values (API keys, passwords, tokens) are visible in the PR comment
6. **Impact**: Secrets may be exposed to unauthorized users with PR read access

### Affected Resources
Based on test data analysis, at minimum:
- `azuredevops_build_definition.variable[*].secret_value`
- `azuredevops_variable_group.secret_variable[*].value`
- Potentially any resource with array-typed attributes where Terraform marks the array as sensitive

### Mitigation (Temporary)
Until fixed, users should:
1. **Always review** generated reports before posting to PRs
2. **Use `--show-sensitive` flag cautiously** only in secure environments
3. **Avoid** using tfplan2md with resources that have secret array attributes
4. **Manually redact** any exposed secrets from reports before sharing

## Related Tests

Existing tests verify basic sensitive masking:
- `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderTests.cs:68-84`
  - Tests simple attribute masking (works correctly)
- `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderTests.cs:87-105`
  - Tests `--show-sensitive` flag behavior

**Missing test coverage**:
- ❌ No tests for array-based sensitive attributes
- ❌ No tests for nested object sensitive attributes
- ❌ No tests for hierarchical sensitivity checking

## Verification Steps

After implementing the fix, verify:

1. **Unit tests**: Add test cases for:
   - Simple array attributes (e.g., `variable[0].secret_value`)
   - Nested object arrays (e.g., `config[0].db.password`)
   - Multi-level arrays (e.g., `items[0].subitems[1].secret`)

2. **Integration test**: Use the existing `TerraformShow/plan1.json` which contains:
   - `azuredevops_build_definition.example2` with `variable` array marked sensitive
   - Expected: All `variable[*].secret_value` should be masked by default

3. **Manual verification**:
   ```bash
   # Without flag - should mask
   tfplan2md src/tests/Oocx.TfPlan2Md.TUnit/TestData/TerraformShow/plan1.json | grep secret_value
   # Expected: (sensitive)
   
   # With flag - should show
   tfplan2md --show-sensitive src/tests/Oocx.TfPlan2Md.TUnit/TestData/TerraformShow/plan1.json | grep secret_value
   # Expected: actual value or empty string
   ```

## References

- Test data showing the issue: `src/tests/Oocx.TfPlan2Md.TUnit/TestData/TerraformShow/plan1.json`
- Terraform JSON output format: https://developer.hashicorp.com/terraform/internals/json-format
- Security policy: `SECURITY.md` (mentions sensitive data exposure as in-scope)
- CLI parser implementation: `src/Oocx.TfPlan2Md/CLI/CliParser.cs:183-184` (`--show-sensitive` flag)

## Next Steps

Hand off to **Developer** agent to:
1. Implement the hierarchical sensitivity check in `IsSensitiveAttribute()`
2. Add helper method `GetHierarchicalPaths()` to generate parent paths
3. Add comprehensive unit tests for array/nested sensitive attributes
4. Update existing integration tests to verify the fix
5. Run all tests to ensure no regressions
