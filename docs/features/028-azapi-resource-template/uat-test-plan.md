# UAT Test Plan: Custom Template for azapi_resource

## Goal

Verify that the azapi_resource template renders correctly in GitHub and Azure DevOps PR comments, and that the body configuration is easy to understand, review, and validate.

## Test Artifacts

We will use multiple test artifacts to cover different scenarios:

### Artifact 1: Simple Create Operation

**Artifact to use:** `artifacts/azapi-create-demo.md`

**Creation Instructions:**
- **Source Plan:** Create new test plan `examples/azapi-create.json`
- **Command:** `tfplan2md examples/azapi-create.json -o artifacts/azapi-create-demo.md`
- **Rationale:** Tests basic create operation with simple nested body properties

**Plan Contents:**
```json
{
  "terraform_version": "1.14.0",
  "format_version": "1.2",
  "resource_changes": [
    {
      "address": "azapi_resource.automation_account",
      "mode": "managed",
      "type": "azapi_resource",
      "provider_name": "registry.terraform.io/azure/azapi",
      "change": {
        "actions": ["create"],
        "before": null,
        "after": {
          "type": "Microsoft.Automation/automationAccounts@2021-06-22",
          "name": "myAccount",
          "parent_id": "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/example-resources",
          "location": "westeurope",
          "tags": {
            "environment": "production",
            "team": "platform"
          },
          "body": {
            "properties": {
              "disableLocalAuth": true,
              "publicNetworkAccess": false,
              "sku": {
                "name": "Basic"
              }
            }
          }
        },
        "after_unknown": {},
        "before_sensitive": false,
        "after_sensitive": {
          "body": {}
        }
      }
    }
  ]
}
```

---

### Artifact 2: Update Operation with Changed Properties

**Artifact to use:** `artifacts/azapi-update-demo.md`

**Creation Instructions:**
- **Source Plan:** Create new test plan `examples/azapi-update.json`
- **Command:** `tfplan2md examples/azapi-update.json -o artifacts/azapi-update-demo.md`
- **Rationale:** Tests update operation showing before/after comparison for changed body properties

**Plan Contents:**
```json
{
  "terraform_version": "1.14.0",
  "format_version": "1.2",
  "resource_changes": [
    {
      "address": "azapi_resource.automation_account",
      "mode": "managed",
      "type": "azapi_resource",
      "provider_name": "registry.terraform.io/azure/azapi",
      "change": {
        "actions": ["update"],
        "before": {
          "type": "Microsoft.Automation/automationAccounts@2021-06-22",
          "name": "myAccount",
          "parent_id": "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/example-resources",
          "location": "westeurope",
          "body": {
            "properties": {
              "disableLocalAuth": false,
              "publicNetworkAccess": true,
              "sku": {
                "name": "Basic"
              }
            }
          }
        },
        "after": {
          "type": "Microsoft.Automation/automationAccounts@2021-06-22",
          "name": "myAccount",
          "parent_id": "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/example-resources",
          "location": "westeurope",
          "body": {
            "properties": {
              "disableLocalAuth": true,
              "publicNetworkAccess": true,
              "sku": {
                "name": "Standard"
              }
            }
          }
        },
        "after_unknown": {},
        "before_sensitive": {
          "body": {}
        },
        "after_sensitive": {
          "body": {}
        }
      }
    }
  ]
}
```

---

### Artifact 3: Complex Nested JSON

**Artifact to use:** `artifacts/azapi-complex-demo.md`

**Creation Instructions:**
- **Source Plan:** Create new test plan `examples/azapi-complex.json`
- **Command:** `tfplan2md examples/azapi-complex.json -o artifacts/azapi-complex-demo.md`
- **Rationale:** Tests deep nesting (5+ levels), arrays, and large property values

**Plan Contents:**
```json
{
  "terraform_version": "1.14.0",
  "format_version": "1.2",
  "resource_changes": [
    {
      "address": "azapi_resource.app_service",
      "mode": "managed",
      "type": "azapi_resource",
      "provider_name": "registry.terraform.io/azure/azapi",
      "change": {
        "actions": ["create"],
        "before": null,
        "after": {
          "type": "Microsoft.Web/sites@2022-03-01",
          "name": "myWebApp",
          "parent_id": "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/example-resources",
          "location": "westeurope",
          "body": {
            "properties": {
              "serverFarmId": "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/example-resources/providers/Microsoft.Web/serverfarms/myPlan",
              "siteConfig": {
                "netFrameworkVersion": "v6.0",
                "alwaysOn": true,
                "connectionStrings": [
                  {
                    "name": "Database",
                    "connectionString": "Server=tcp:myserver.database.windows.net,1433;Initial Catalog=mydb;User ID=admin;Password=***;",
                    "type": "SQLAzure"
                  },
                  {
                    "name": "Redis",
                    "connectionString": "myredis.redis.cache.windows.net:6380,password=***,ssl=True",
                    "type": "RedisCache"
                  }
                ],
                "appSettings": [
                  {
                    "name": "ASPNETCORE_ENVIRONMENT",
                    "value": "Production"
                  },
                  {
                    "name": "APPLICATIONINSIGHTS_CONNECTION_STRING",
                    "value": "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/"
                  }
                ],
                "cors": {
                  "allowedOrigins": [
                    "https://portal.azure.com",
                    "https://myapp.azurewebsites.net"
                  ],
                  "supportCredentials": true
                },
                "metadata": [
                  {
                    "name": "CURRENT_STACK",
                    "value": "dotnet"
                  }
                ]
              },
              "httpsOnly": true,
              "clientAffinityEnabled": false
            }
          }
        },
        "after_unknown": {},
        "before_sensitive": false,
        "after_sensitive": {
          "body": {
            "properties": {
              "siteConfig": {
                "connectionStrings": [
                  {
                    "connectionString": true
                  },
                  {
                    "connectionString": true
                  }
                ]
              }
            }
          }
        }
      }
    }
  ]
}
```

---

## Test Steps

1. **Developer** creates the three test plan JSON files in `examples/` directory
2. **Developer** runs tfplan2md to generate the three artifacts in `artifacts/` directory
3. **UAT Tester** (or `run-uat` skill) creates PRs on GitHub and Azure DevOps using these artifacts
4. **Maintainer** reviews PRs and validates rendering

## Validation Instructions (Test Description)

### For All Artifacts

**General Markdown Rendering:**
- [ ] Markdown renders without errors in both platforms
- [ ] Tables display correctly (aligned columns, no broken rows)
- [ ] Collapsible `<details>` sections work (can expand/collapse)
- [ ] Inline code formatting displays correctly (backticks render as code)
- [ ] Icons/emoji display correctly (✅, ❌, 🌍, 📚, ➕, 🔄, ❌)
- [ ] No raw HTML or escaped characters visible where they shouldn't be

---

### Artifact 1: Simple Create Operation (azapi-create-demo.md)

**Specific Resources/Sections:**
- `azapi_resource.automation_account` (create operation)

**Validation Checklist:**

#### Summary Line
- [ ] Shows: `➕ azapi_resource automation_account — Microsoft.Automation/automationAccounts | example-resources | westeurope`
- [ ] Create icon (➕) displays correctly
- [ ] Resource type includes API version
- [ ] Resource group and location are visible in summary

#### Resource Details (Expanded)
- [ ] **Type** shown as inline code: `Microsoft.Automation/automationAccounts@2021-06-22`
- [ ] **Documentation link** displayed with book emoji 📚
- [ ] Link text: "View API Documentation (best-effort)"
- [ ] Link URL follows pattern: `https://learn.microsoft.com/rest/api/automation/automation-accounts/`

#### Standard Attributes Table
- [ ] Table has two columns: "Attribute" and "Value"
- [ ] Shows `name` as inline code: `myAccount`
- [ ] Shows `parent_id` as: "Resource Group `example-resources`"
- [ ] Shows `location` with globe emoji: `🌍 westeurope`

#### Tags Display
- [ ] Tags displayed as badges: `🏷️ environment: production` `🏷️ team: platform`
- [ ] Badges appear below the standard attributes table

#### Body Section
- [ ] Heading: "Body"
- [ ] Table has two columns: "Property" and "Value"
- [ ] Property paths use dot notation:
  - `properties.disableLocalAuth`
  - `properties.publicNetworkAccess`
  - `properties.sku.name`
- [ ] Values formatted correctly:
  - `properties.disableLocalAuth` → `✅ true` (checkmark for true)
  - `properties.publicNetworkAccess` → `❌ false` (cross for false)
  - `properties.sku.name` → `Basic` (inline code)

**Expected Outcome:**
Reviewer can immediately understand what Azure Automation Account is being created and what its configuration is without parsing JSON.

**Before/After Context:**
Previously, users would see only "body changed" for azapi resources, making it difficult to review what's actually configured. Now, the body is flattened into a scannable table.

---

### Artifact 2: Update Operation (azapi-update-demo.md)

**Specific Resources/Sections:**
- `azapi_resource.automation_account` (update operation)

**Validation Checklist:**

#### Summary Line
- [ ] Shows: `🔄 azapi_resource automation_account — Microsoft.Automation/automationAccounts | 2 properties changed`
- [ ] Update icon (🔄) displays correctly
- [ ] Change count (2 properties) is accurate
- [ ] No location/resource group in update summary (only shown in details)

#### Resource Details (Expanded)
- [ ] Type, documentation link, and standard attributes table present (same as create)

#### Body Changes Section (NOT "Body")
- [ ] Heading: "Body Changes"
- [ ] Table has **three columns**: "Property", "Before", "After"
- [ ] Only **changed properties** are shown (not unchanged ones)
- [ ] Changed properties:
  - `properties.disableLocalAuth`: `❌ false` → `✅ true`
  - `properties.sku.name`: `Basic` → `Standard`
- [ ] **Unchanged property** `properties.publicNetworkAccess` is **NOT** shown
- [ ] Before and After columns are clearly distinguishable

**Expected Outcome:**
Reviewer can immediately see what changed in the body without comparing JSON manually. The diff is focused on changed properties only.

**Before/After Context:**
Previously, update operations would show "body: changed" with no details. Now, specific property changes are highlighted with before/after values.

---

### Artifact 3: Complex Nested JSON (azapi-complex-demo.md)

**Specific Resources/Sections:**
- `azapi_resource.app_service` (create operation with deep nesting)

**Validation Checklist:**

#### Flattened Property Paths
- [ ] Deep nested properties are flattened with dot notation:
  - `properties.siteConfig.netFrameworkVersion` → `v6.0`
  - `properties.siteConfig.alwaysOn` → `✅ true`
  - `properties.siteConfig.cors.supportCredentials` → `✅ true`
- [ ] Array elements use index notation:
  - `properties.siteConfig.connectionStrings[0].name` → `Database`
  - `properties.siteConfig.connectionStrings[1].name` → `Redis`
  - `properties.siteConfig.appSettings[0].name` → `ASPNETCORE_ENVIRONMENT`
  - `properties.siteConfig.appSettings[1].name` → `APPLICATIONINSIGHTS_CONNECTION_STRING`
  - `properties.siteConfig.cors.allowedOrigins[0]` → `https://portal.azure.com`
- [ ] No broken paths or missing properties due to nesting depth

#### Sensitive Values
- [ ] Connection strings are masked:
  - `properties.siteConfig.connectionStrings[0].connectionString` → `(sensitive)`
  - `properties.siteConfig.connectionStrings[1].connectionString` → `(sensitive)`
- [ ] Non-sensitive properties display normally:
  - `properties.siteConfig.connectionStrings[0].name` → `Database` (visible)
  - `properties.siteConfig.connectionStrings[0].type` → `SQLAzure` (visible)

#### Large Values Handling
- [ ] Long values (>200 chars) moved to collapsible section:
  - `properties.siteConfig.appSettings[1].value` (Application Insights connection string ~100+ chars)
  - If present, should be in "Large body properties" collapsible section
- [ ] Small properties remain in main table

#### Array Representation
- [ ] Arrays are flattened with index notation (verified above)
- [ ] Array structure is clear and reviewable
- [ ] No confusion about which items are in which array

**Expected Outcome:**
Reviewer can understand a complex, deeply nested azapi_resource configuration without being overwhelmed. Sensitive values are protected, and large values don't clutter the main view.

**Before/After Context:**
Previously, complex azapi bodies were impenetrable JSON blobs. Now, even deep nesting with arrays and sensitive values is presented in a scannable, human-readable format.

---

## Success Criteria

### Overall Success Criteria
- [ ] All markdown artifacts render without errors on GitHub
- [ ] All markdown artifacts render without errors on Azure DevOps
- [ ] Body configuration is immediately understandable without parsing JSON
- [ ] Property paths (dot notation and array indices) are clear and unambiguous
- [ ] Change detection (for updates) clearly highlights what changed
- [ ] Sensitive values are properly masked
- [ ] Large values don't clutter the main view
- [ ] Documentation links are helpful (even if best-effort)
- [ ] Icons and formatting enhance readability
- [ ] No markdown syntax errors or broken tables

### Platform-Specific Success Criteria

**GitHub:**
- [ ] Collapsible `<details>` sections work correctly
- [ ] Inline HTML elements render as intended
- [ ] Table formatting is correct (aligned, no overflow)
- [ ] Icons/emoji display correctly

**Azure DevOps:**
- [ ] Collapsible sections work correctly (or fallback gracefully if not supported)
- [ ] Tables render correctly
- [ ] Icons/emoji display correctly
- [ ] No platform-specific rendering issues

## Feedback Opportunities

During UAT review, the Maintainer should consider:

1. **Readability**: Is the flattened table format easy to scan? Are property paths clear?
2. **Completeness**: Is all necessary information visible? Is anything missing?
3. **Noise**: Is there too much information? Should any sections be collapsed by default?
4. **Formatting**: Are values formatted helpfully? Do icons enhance or clutter?
5. **Change Detection**: For updates, is it immediately clear what changed?
6. **Sensitive Values**: Is masking working as expected? Is the balance between security and visibility appropriate?
7. **Documentation Links**: Are the best-effort links helpful? Do they often lead to correct documentation?
8. **Edge Cases**: Do any specific Azure resource types need special handling beyond generic flattening?

## Approval Process

1. **UAT Tester** creates PRs with test artifacts on GitHub and Azure DevOps
2. **Maintainer** reviews rendering in both platforms
3. **Maintainer** provides feedback via PR comments if issues found
4. **Developer** addresses feedback and updates artifacts
5. **UAT Tester** posts updated artifacts as new PR comments
6. **Repeat** until Maintainer approves or aborts
7. **Approval detected** when Maintainer:
   - Comments "approved", "passed", "lgtm", or "accept"
   - **OR** closes/resolves the PR/thread
8. **UAT Tester** cleans up test PRs and branches

## Notes

- **Best-Effort Links**: The documentation links are heuristic-based and may not always be correct. This is expected and acceptable per the architecture design.
- **Sensitivity**: Per-property sensitivity is based on Terraform's `before_sensitive` and `after_sensitive` structures, which may not always match Azure's actual sensitivity markers.
- **Large Value Threshold**: The 200-character threshold is configurable and may be adjusted based on feedback.
- **Array Representation**: Arrays are flattened with index notation. Large arrays may produce many table rows; feedback on summarization strategies is valuable.
