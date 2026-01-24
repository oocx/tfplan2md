# UAT Test Plan: Azure API Documentation Mapping

## Goal

Verify that Azure API documentation links render correctly in GitHub and Azure DevOps PR comments, and that the links are accurate, functional, and improve the user experience when reviewing azapi resources.

## Artifacts

### Artifact 1: Mapped Resources Demo

**Artifact to use:** `artifacts/azapi-mapped-resources-demo.md`

**Creation Instructions:**

**Source Plan:** Create `TestData/azapi-mapped-resources-demo.json` with the following structure:

```json
{
  "format_version": "1.2",
  "terraform_version": "1.14.0",
  "resource_changes": [
    {
      "address": "azapi_resource.vm",
      "type": "azapi_resource",
      "change": {
        "actions": ["create"],
        "before": null,
        "after": {
          "type": "Microsoft.Compute/virtualMachines@2023-03-01",
          "location": "eastus",
          "name": "example-vm",
          "body": "{\"properties\":{\"hardwareProfile\":{\"vmSize\":\"Standard_DS2_v2\"},\"storageProfile\":{\"imageReference\":{\"publisher\":\"Canonical\",\"offer\":\"UbuntuServer\",\"sku\":\"18.04-LTS\",\"version\":\"latest\"}}}}"
        }
      }
    },
    {
      "address": "azapi_resource.storage",
      "type": "azapi_resource",
      "change": {
        "actions": ["create"],
        "before": null,
        "after": {
          "type": "Microsoft.Storage/storageAccounts@2021-06-01",
          "location": "eastus",
          "name": "examplestorageacct",
          "body": "{\"sku\":{\"name\":\"Standard_LRS\"},\"kind\":\"StorageV2\",\"properties\":{}}"
        }
      }
    },
    {
      "address": "azapi_resource.vnet",
      "type": "azapi_resource",
      "change": {
        "actions": ["create"],
        "before": null,
        "after": {
          "type": "Microsoft.Network/virtualNetworks@2020-11-01",
          "location": "eastus",
          "name": "example-vnet",
          "body": "{\"properties\":{\"addressSpace\":{\"addressPrefixes\":[\"10.0.0.0/16\"]}}}"
        }
      }
    }
  ]
}
```

**Command:**
```bash
tfplan2md < TestData/azapi-mapped-resources-demo.json > artifacts/azapi-mapped-resources-demo.md
```

**Rationale:** Demonstrates the new feature with common Azure resources that have documentation mappings.

---

### Artifact 2: Unmapped Resources Demo

**Artifact to use:** `artifacts/azapi-unmapped-resources-demo.md`

**Creation Instructions:**

**Source Plan:** Create `TestData/azapi-unmapped-resources-demo.json` with the following structure:

```json
{
  "format_version": "1.2",
  "terraform_version": "1.14.0",
  "resource_changes": [
    {
      "address": "azapi_resource.unknown_service",
      "type": "azapi_resource",
      "change": {
        "actions": ["create"],
        "before": null,
        "after": {
          "type": "Microsoft.UnknownService/unknownResource@2023-01-01",
          "location": "eastus",
          "name": "example-unknown",
          "body": "{\"properties\":{}}"
        }
      }
    },
    {
      "address": "azapi_resource.fake_provider",
      "type": "azapi_resource",
      "change": {
        "actions": ["create"],
        "before": null,
        "after": {
          "type": "Microsoft.FakeProvider/fakeResource@2022-12-01",
          "location": "westus",
          "name": "example-fake",
          "body": "{\"properties\":{}}"
        }
      }
    }
  ]
}
```

**Command:**
```bash
tfplan2md < TestData/azapi-unmapped-resources-demo.json > artifacts/azapi-unmapped-resources-demo.md
```

**Rationale:** Demonstrates graceful degradation when no mapping exists for a resource type.

---

### Artifact 3: Mixed Mappings (Comprehensive)

**Artifact to use:** `artifacts/azapi-mixed-mappings-demo.md`

**Creation Instructions:**

**Source Plan:** Create `TestData/azapi-mixed-mappings-demo.json` with both mapped and unmapped resources:

```json
{
  "format_version": "1.2",
  "terraform_version": "1.14.0",
  "resource_changes": [
    {
      "address": "azapi_resource.vm",
      "type": "azapi_resource",
      "change": {
        "actions": ["create"],
        "before": null,
        "after": {
          "type": "Microsoft.Compute/virtualMachines@2023-03-01",
          "location": "eastus",
          "name": "example-vm",
          "body": "{\"properties\":{\"hardwareProfile\":{\"vmSize\":\"Standard_DS2_v2\"}}}"
        }
      }
    },
    {
      "address": "azapi_resource.unknown",
      "type": "azapi_resource",
      "change": {
        "actions": ["create"],
        "before": null,
        "after": {
          "type": "Microsoft.UnknownService/unknownResource@2023-01-01",
          "location": "eastus",
          "name": "example-unknown",
          "body": "{\"properties\":{}}"
        }
      }
    },
    {
      "address": "azapi_resource.storage",
      "type": "azapi_resource",
      "change": {
        "actions": ["update"],
        "before": {
          "type": "Microsoft.Storage/storageAccounts@2021-06-01",
          "location": "eastus",
          "name": "examplestorageacct",
          "body": "{\"sku\":{\"name\":\"Standard_LRS\"}}"
        },
        "after": {
          "type": "Microsoft.Storage/storageAccounts@2021-06-01",
          "location": "eastus",
          "name": "examplestorageacct",
          "body": "{\"sku\":{\"name\":\"Standard_GRS\"}}"
        }
      }
    },
    {
      "address": "azapi_resource.nested_blob_service",
      "type": "azapi_resource",
      "change": {
        "actions": ["create"],
        "before": null,
        "after": {
          "type": "Microsoft.Storage/storageAccounts/blobServices@2021-06-01",
          "name": "default",
          "parent_id": "azapi_resource.storage.id",
          "body": "{\"properties\":{}}"
        }
      }
    }
  ]
}
```

**Command:**
```bash
tfplan2md < TestData/azapi-mixed-mappings-demo.json > artifacts/azapi-mixed-mappings-demo.md
```

**Rationale:** Realistic scenario with both mapped and unmapped resources, including nested resources.

---

## Test Steps

### Step 1: Generate Artifacts

Run the commands above to generate the three test artifacts:
1. `artifacts/azapi-mapped-resources-demo.md`
2. `artifacts/azapi-unmapped-resources-demo.md`
3. `artifacts/azapi-mixed-mappings-demo.md`

### Step 2: Run UAT Simulation

Use the `UAT Tester` agent or manual UAT workflow:

**Automated (Preferred):**
```bash
scripts/uat-run.sh run artifacts/azapi-mapped-resources-demo.md
scripts/uat-run.sh run artifacts/azapi-unmapped-resources-demo.md
scripts/uat-run.sh run artifacts/azapi-mixed-mappings-demo.md
```

**Manual:**
1. Create GitHub PR with artifact as comment
2. Create Azure DevOps PR with artifact as comment
3. Review rendering in both platforms
4. Validate against validation instructions below

### Step 3: Verify Rendering

Review the generated PRs on GitHub and Azure DevOps according to the validation instructions.

---

## Validation Instructions (Test Description)

Use this description verbatim in UAT PRs:

---

### Validation: Mapped Resources (Artifact 1)

**Specific Resources:**
- `azapi_resource.vm` (`Microsoft.Compute/virtualMachines@2023-03-01`)
- `azapi_resource.storage` (`Microsoft.Storage/storageAccounts@2021-06-01`)
- `azapi_resource.vnet` (`Microsoft.Network/virtualNetworks@2020-11-01`)

**Exact Attributes to Check:**
- Each resource should display: `📚 [View API Documentation](URL)`
- The link text should be **"View API Documentation"** (no "(best-effort)" disclaimer)
- URLs should be:
  - VM: `https://learn.microsoft.com/rest/api/compute/virtual-machines`
  - Storage: `https://learn.microsoft.com/rest/api/storagerp/storage-accounts`
  - VNet: `https://learn.microsoft.com/rest/api/virtualnetwork/virtual-networks`

**Expected Outcome:**
- Documentation links are clickable hyperlinks (blue, underlined)
- Clicking each link navigates to the correct Azure REST API documentation page
- No broken links (404 errors)
- No "(best-effort)" text appears anywhere in the output

**Before/After Context:**
- **Before**: Documentation links displayed as `📚 [View API Documentation (best-effort)](URL)` with heuristic-guessed URLs that often broke
- **After**: Links displayed without disclaimer, using curated mappings from Microsoft Learn, ensuring accuracy

---

### Validation: Unmapped Resources (Artifact 2)

**Specific Resources:**
- `azapi_resource.unknown_service` (`Microsoft.UnknownService/unknownResource@2023-01-01`)
- `azapi_resource.fake_provider` (`Microsoft.FakeProvider/fakeResource@2022-12-01`)

**Exact Attributes to Check:**
- Each resource should display: `**Type:** \`Microsoft.UnknownService/unknownResource@2023-01-01\``
- **No** `📚 [View API Documentation]` link should appear
- No error messages, placeholder text, or broken formatting

**Expected Outcome:**
- Resource type is displayed clearly
- Absence of documentation link does not break markdown formatting
- Output is clean and professional (no misleading placeholders)

**Before/After Context:**
- **Before**: Heuristic URLs were generated even for unknown resources, leading to broken links
- **After**: No link is shown when no mapping exists, avoiding broken links and maintaining clean output

---

### Validation: Mixed Mappings (Artifact 3)

**Specific Resources:**
- `azapi_resource.vm` (mapped) - should show link
- `azapi_resource.unknown` (unmapped) - should NOT show link
- `azapi_resource.storage` (mapped, update action) - should show link
- `azapi_resource.nested_blob_service` (mapped, nested resource) - should show link

**Exact Attributes to Check:**
- **Mapped resources** (VM, Storage, Blob Service):
  - Display `📚 [View API Documentation](URL)` with correct URLs
  - No "(best-effort)" disclaimer
- **Unmapped resource** (unknown):
  - Display `**Type:** \`Microsoft.UnknownService/unknownResource@2023-01-01\``
  - No documentation link
- **Nested resource** (Blob Service):
  - Has its own specific documentation URL (not inherited from Storage Account)
  - URL: `https://learn.microsoft.com/rest/api/storagerp/blob-services`

**Expected Outcome:**
- Consistent formatting across all resources
- Mixed behavior (some links, some not) is clear and not confusing
- Nested resources have specific documentation links (not parent URLs)
- Overall readability and professionalism is maintained

**Before/After Context:**
- **Before**: All resources showed "(best-effort)" links, many broken; nested resources didn't have specific URLs
- **After**: Only resources with verified mappings show links; nested resources have individual mappings; clean degradation for unknown resources

---

## Success Criteria (Overall)

### Functionality
- [ ] Mapped resources show clickable documentation links with correct URLs
- [ ] Unmapped resources omit documentation links (no broken links)
- [ ] Nested resources have individual documentation links (not parent fallbacks)
- [ ] No "(best-effort)" disclaimer appears anywhere
- [ ] All documentation links navigate to valid Azure REST API documentation pages

### Rendering (GitHub)
- [ ] Markdown renders correctly in GitHub PR comments
- [ ] Links are properly formatted (blue, underlined, clickable)
- [ ] No broken markdown formatting (tables, headings, etc.)
- [ ] Emoji (📚) displays correctly

### Rendering (Azure DevOps)
- [ ] Markdown renders correctly in Azure DevOps PR comments
- [ ] Links are properly formatted and clickable
- [ ] No broken markdown formatting
- [ ] Emoji (📚) displays correctly (or acceptable alternative)

### User Experience
- [ ] Feature improves review experience (easier to find documentation)
- [ ] Absence of links for unmapped resources is not confusing
- [ ] Mixed mapped/unmapped resources are handled gracefully
- [ ] Overall output is professional and trustworthy

## Feedback Opportunities

During UAT review, consider:

1. **Link Text Clarity**: Is "View API Documentation" clear and actionable? Should it be more specific?

2. **Link Placement**: Is the documentation link positioned intuitively in the resource output?

3. **Visual Distinction**: Are mapped vs. unmapped resources easy to distinguish?

4. **Missing Mappings**: If you encounter unmapped resources in real usage, does the absence of a link degrade the experience significantly?

5. **Documentation Quality**: When clicking links, does the Azure documentation page answer typical user questions about the resource type?

6. **Emoji Usage**: Does the 📚 emoji work well, or should it be changed to text or a different symbol?

7. **Format Consistency**: Is the format consistent with other tfplan2md documentation links (e.g., Terraform registry links)?

## Notes for UAT Tester Agent

- Use all three artifacts to cover mapped, unmapped, and mixed scenarios
- Post artifacts as PR comments (not PR description) for accurate rendering validation
- Poll for Maintainer feedback on each artifact
- If Maintainer reports broken links or rendering issues, apply fixes and post updated comments
- Validate that clicked links navigate to correct pages (manual check by Maintainer)
- Ensure both GitHub and Azure DevOps rendering is validated before approval
