# UAT Test Plan: Improved AzAPI Attribute Grouping and Array Rendering

## Goal
Verify that complex AzAPI resources render with improved structure and readability in GitHub and Azure DevOps PR comments, using grouping for arrays and nested objects.

## Artifacts

### Artifact 1: Complex App Service
**Artifact to use:** `artifacts/azapi-complex-demo.md`

**Creation Instructions:**
- **Source Plan:** `TestData/azapi-complex-app-service.json` (Needs to be created with 5+ app settings and 3+ connection strings)
- **Command:** `tfplan2md --plan TestData/azapi-complex-app-service.json`
- **Rationale:** Validates both array grouping (app settings) and matrix table rendering (connection strings with few properties).

### Artifact 2: Nested Object Grouping (CORS)
**Artifact to use:** `artifacts/azapi-nested-grouping-demo.md`

**Creation Instructions:**
- **Source Plan:** `TestData/azapi-nested-grouping.json` (Contains `cors.allowedOrigins`, `cors.exposedHeaders`, `cors.maxAge`)
- **Command:** `tfplan2md --plan TestData/azapi-nested-grouping.json`
- **Rationale:** Validates non-array prefix grouping for nested objects.

## Test Steps
1. Run UAT using the `UAT Tester` agent.
2. Verify the generated PRs on GitHub and Azure DevOps.

## Validation Instructions (Test Description)

**Specific Resources/Sections:**
- `azapi_resource.app_service` -> `###### connectionStrings Array`
- `azapi_resource.app_service` -> `###### appSettings Array`
- `azapi_resource.web_app` -> `###### cors`

**Exact Attributes:**
- Check for H6 headings (`######`) for groups.
- Check that redundant prefixes (e.g., `connectionStrings[0].`) are REMOVED from the table property names.

**Expected Outcome:**
- Large bodies are broken into manageable sections.
- Array items are displayed in a compact table (if simple) or separate tables (if complex).
- Diffs within groups are clearly visible with before/after values.

**Before/After Context:**
- **Before**: 50+ rows of flat paths like `siteConfig.appSettings[12].value`, making it impossible to see the structure.
- **After**: Grouped sections that mirror the logical JSON structure, drastically reducing visual noise.
