# UAT Test Plan: Display Enhancements

## Goal
Verify that the display enhancements (syntax highlighting, APIM summaries, named values, and subscription emojis) render correctly in GitHub and Azure DevOps PR environments.

## Artifacts
**Artifact to use:** `artifacts/apim-display-enhancements-demo.md`

**Creation Instructions:**
- **Source Plan:** `examples/apim-display-enhancements.json` (to be created)
- **Command:** `tfplan2md examples/apim-display-enhancements.json > artifacts/apim-display-enhancements-demo.md`
- **Rationale:** Need a focused artifact that exercises all four improvements in a single report.

## Test Steps
1. Run the UAT simulation using the `UAT Tester` agent.
2. Verify the generated PRs on GitHub and Azure DevOps.

## Validation Instructions (Test Description)

### 1. Syntax Highlighting
**Specific Resources/Sections:**
- `azurerm_api_management_api_policy.example` -> `xml_content`
- `azapi_resource.large_json` -> `body`

**Expected Outcome:**
- The XML/JSON block should be pretty-printed (properly indented).
- It should have syntax highlighting (colored text) in the PR UI.
- The code block should be labeled with its respective language (`xml` or `json`).

### 2. API Management Summaries
**Specific Resources/Sections:**
- `azurerm_api_management_api_operation.get_user`
- `azurerm_api_management_named_value.api_url`
- `azurerm_api_management_api_policy.example`

**Expected Outcome:**
- Operation summary should look like: `azurerm_api_management_api_operation` `this` `Get User` — `get-user` `apim-hello` in `📁 rg-hello`
- Named value summary should look like: `azurerm_api_management_named_value` `this` — `🆔 api_url` `apim-hello` in `📁 rg-hello`
- Policy summary should include `apim-hello`.

### 3. Named Values Sensitivity Override
**Specific Resources/Sections:**
- `azurerm_api_management_named_value.api_url` (where `secret=false`)

**Expected Outcome:**
- The `value` attribute should display its actual contents (e.g., `https://api.example.com`) instead of `(sensitive)`.

### 4. Subscription Attributes Emoji
**Specific Resources:**
- Any resource in the report.

**Exact Attributes:**
- `subscription_id`

**Expected Outcome:**
- The ID should be prefixed with 🔑 (e.g., `🔑 12345678-...`).

**Before/After Context:**
- Previously, `subscription_id` was just a plain UUID. Adding the key emoji makes it instantly recognizable as a subscription identifier, consistent with how resource groups use the folder emoji.
- APIM resources previously lacked context, making it hard to know which APIM service they belonged to without opening the details.
- JSON/XML was hard to read when minified or not formatted.
