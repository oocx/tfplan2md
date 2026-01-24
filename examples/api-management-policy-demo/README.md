# API Management Policy Demo

This example demonstrates tfplan2md's **Large Value Handling** feature with a realistic Azure API Management policy.

## Overview

The example shows an `azurerm_api_management_policy` resource with a 112-line XML policy being updated. The changes include:

- Increased rate limits (100 → 200 calls/minute)
- Additional allowed IP networks
- New CORS origins and HTTP methods
- Backend URL change (backend-old → backend-new)
- Enhanced caching configuration
- Added security headers
- Improved error handling with notifications

## Files

- `plan.json` - Terraform plan with API Management policy change
- `output.md` - Generated markdown report with large value diff
- `output-github.html` - GitHub-flavored HTML render
- `output-azdo.html` - Azure DevOps-flavored HTML render

## Key Feature

tfplan2md automatically detects the large XML value (112 lines) and:

1. Displays a summary in the main resource table (`(see below)`)
2. Shows the full diff in a separate section
3. Highlights changes at the character level with inline HTML spans
4. Reports statistics: "112 lines, 57 changed"

This makes reviewing large configuration files, policies, and scripts in PR comments far more practical.

## Usage

Regenerate this example:

```bash
# Generate markdown
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- \
  examples/api-management-policy-demo/plan.json > examples/api-management-policy-demo/output.md

# Generate GitHub HTML
dotnet run --project src/tools/Oocx.TfPlan2Md.HtmlRenderer/Oocx.TfPlan2Md.HtmlRenderer.csproj -- \
  --input examples/api-management-policy-demo/output.md \
  --flavor github \
  --output examples/api-management-policy-demo/output-github.html

# Generate Azure DevOps HTML
dotnet run --project src/tools/Oocx.TfPlan2Md.HtmlRenderer/Oocx.TfPlan2Md.HtmlRenderer.csproj -- \
  --input examples/api-management-policy-demo/output.md \
  --flavor azdo \
  --output examples/api-management-policy-demo/output-azdo.html
```
