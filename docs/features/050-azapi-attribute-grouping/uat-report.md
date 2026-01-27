# UAT Report: Improved AzAPI Attribute Grouping and Array Rendering

## Status: ❌ Failed

## Date: 2026-01-27

## Environment
- **GitHub PR:** [#29](https://github.com/oocx/tfplan2md-uat/pull/29) (Status: Closed - Feedback Provided)
- **Azure DevOps PR:** [#36](https://dev.azure.com/oocx/test/_git/test/pullrequest/36) (Status: Active - Rejected/Vote -10)

## Test Summary
The UAT was executed on both GitHub and Azure DevOps using a manually generated comprehensive artifact that includes complex AzAPI resources. While the attribute grouping (H6 headings) and table rendering for arrays worked as expected, several visual regressions were identified in the metadata attributes.

## Key Findings

### 1. Duplicate Emojis and Formatting for Standard Attributes
In the AzAPI resource metadata table, the `location` and `name` attributes exhibit double-formatting.

- **Issue:** The `location` attribute shows two globe emojis and redundant backticks/slashes.
- **Evidence:** 
  - Reported text: `🌍 🌍 \westeurope``
  - Reported text: `🆔 \complexApp``
- **Root Cause Analysis:** 
  - The C# helper `ExtractAzapiMetadata` in [src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Metadata.cs](src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Metadata.cs) already adds emojis and backticks.
  - The Scriban template [src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/resource.sbn](src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/resource.sbn) passes these already-formatted values into `format_attribute_value_table`, which performs its own semantic formatting based on the attribute name.

## Recommendations
- Update `ExtractAzapiMetadata` to return raw values without emojis or backticks, allowing the standard formatting helpers to handle them consistently.
- Alternatively, update the template to not use `format_attribute_value_table` for these specific properties if custom formatting in the helper is preferred.

## Artifacts used
- `artifacts/azapi-uat-combined.md` (Generated from `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azapi-complex-nested-plan.json`)
