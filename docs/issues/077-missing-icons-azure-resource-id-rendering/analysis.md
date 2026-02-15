# Issue 465: Missing Icons in Azure Resource ID Rendering

## Problem Description

When Azure resource IDs (like `routeTable.id` in azapi_resource attributes) are rendered in attribute values, the resource names and resource group names are missing their semantic icons. Only the subscription ID correctly shows its 🔑 icon.

### Example

An azapi_resource for a subnet (`Microsoft.Network/virtualNetworks/subnets@2023-11-01`) with a `routeTable.id` attribute currently renders as:

```
RouteTables 'deployment-rt-lv2-gwc' in resource group 'deploymentnet-rg-lv2-gwc' of subscription 🔑 d1828a48-fced-4ea2-b2ec-4b9623f327fd
```

### Expected Behavior

The output should include semantic icons for all components:

```
RouteTables '🆔 deployment-rt-lv2-gwc' in resource group '📁 deploymentnet-rg-lv2-gwc' of subscription 🔑 d1828a48-fced-4ea2-b2ec-4b9623f327fd
```

Where:
- Resource name (`deployment-rt-lv2-gwc`) should have 🆔 icon
- Resource group name (`deploymentnet-rg-lv2-gwc`) should have 📁 icon
- Subscription already correctly shows 🔑 icon

## Steps to Reproduce

1. Create an azapi_resource with a reference to another Azure resource (e.g., routeTable.id, networkSecurityGroup.id)
2. The referenced resource ID is an Azure resource identifier (e.g., `/subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Network/routeTables/{name}`)
3. Generate the markdown report
4. Observe that the resource name and resource group name lack semantic icons

## Root Cause Analysis

### Affected Components

The issue exists in **two methods** that format Azure resource IDs:

#### 1. `EnrichedAzureScopeFormatter.Format` Method
- **File**: `src/Oocx.TfPlan2Md/Platforms/Azure/EnrichedAzureScopeFormatter.cs`
- **Lines**: 102-104
- **Issue**: Lines construct formatted text by directly interpolating values without applying icon formatting

```csharp
ScopeLevel.Resource when !string.IsNullOrWhiteSpace(scopeInfo.ResourceGroup) =>
    $"{scopeInfo.Type} `{scopeInfo.Name}` in resource group `{resourceGroupValue}` of subscription `{subscriptionValue}`",
ScopeLevel.Resource => $"{scopeInfo.Type} `{scopeInfo.Name}` in subscription `{subscriptionValue}`",
```

**Problem**:
- `scopeInfo.Name` is wrapped in backticks but has no icon
- `resourceGroupValue` already has the 📁 icon (from `FormatResourceGroupLabel` on line 95)
- `subscriptionValue` already has the 🔑 icon (from `FormatSubscriptionLabel` on line 94)
- But the resource name (`scopeInfo.Name`) has no icon applied

#### 2. `AzureScopeParser.ParseScope` Method  
- **File**: `src/Oocx.TfPlan2Md/Platforms/Azure/AzureScopeParser.cs`
- **Lines**: 162-164
- **Issue**: Similar problem - resource names and resource groups lack icons

```csharp
ScopeLevel.Resource when !string.IsNullOrWhiteSpace(parsed.ResourceGroup) =>
    $"{parsed.Type} `{parsed.Name}` in resource group `{parsed.ResourceGroup}` of subscription `{FormatSubscriptionId(parsed.SubscriptionId ?? string.Empty)}`",
ScopeLevel.Resource => $"{parsed.Type} `{parsed.Name}` in subscription `{FormatSubscriptionId(parsed.SubscriptionId ?? string.Empty)}`",
```

**Problem**:
- `parsed.Name` has no icon
- `parsed.ResourceGroup` has no icon (unlike in `EnrichedAzureScopeFormatter` where it calls `FormatResourceGroupLabel`)
- Only subscription gets an icon from `FormatSubscriptionId`

### What's Broken

The two formatters responsible for converting Azure resource IDs into human-readable text are:
1. **Not applying semantic icon formatting** to resource names
2. **Inconsistent** - `EnrichedAzureScopeFormatter` applies icons to resource groups, but `AzureScopeParser` doesn't
3. **Not leveraging existing icon constants** - The 🆔 and 📁 icons are already defined for `name` and `resource_group_name` attributes in `SemanticFormatting.Identity.cs`

### Why It Happened

The icon formatting for resource IDs was implemented separately from the general semantic attribute formatting logic. When `FormatSubscriptionLabel` and `FormatResourceGroupLabel` were added to `EnrichedAzureScopeFormatter`, they correctly added icons for subscriptions and resource groups. However:

1. The resource **name** formatting was overlooked
2. The older `AzureScopeParser.ParseScope` method was not updated to use the icon formatting helpers
3. There's no centralized helper for formatting resource names with icons in the scope formatting context

## Comprehensive Analysis: All Affected Cases

Based on code analysis, the missing icons affect **all Azure resource IDs** rendered as attribute values, including but not limited to:

### Network Resources
- **Route Tables**: `Microsoft.Network/routeTables` (reported issue)
- **Network Security Groups**: `Microsoft.Network/networkSecurityGroups`
- **Virtual Networks**: `Microsoft.Network/virtualNetworks`
- **Subnets**: `Microsoft.Network/virtualNetworks/subnets`
- **Public IP Addresses**: `Microsoft.Network/publicIPAddresses`
- **Load Balancers**: `Microsoft.Network/loadBalancers`
- **Application Gateways**: `Microsoft.Network/applicationGateways`
- **Azure Firewalls**: `Microsoft.Network/azureFirewalls`
- **VPN Gateways**: `Microsoft.Network/vpnGateways`
- **Private Endpoints**: `Microsoft.Network/privateEndpoints`

### Storage Resources
- **Storage Accounts**: `Microsoft.Storage/storageAccounts`
- **Blob Services**: `Microsoft.Storage/storageAccounts/blobServices`
- **File Services**: `Microsoft.Storage/storageAccounts/fileServices`

### Compute Resources
- **Virtual Machines**: `Microsoft.Compute/virtualMachines`
- **VM Scale Sets**: `Microsoft.Compute/virtualMachineScaleSets`
- **Managed Disks**: `Microsoft.Compute/disks`

### Key Vault Resources
- **Key Vaults**: `Microsoft.KeyVault/vaults`

### Database Resources
- **SQL Servers**: `Microsoft.Sql/servers`
- **SQL Databases**: `Microsoft.Sql/servers/databases`
- **Cosmos DB**: `Microsoft.DocumentDB/databaseAccounts`

### Container Resources
- **AKS Clusters**: `Microsoft.ContainerService/managedClusters`
- **Container Registries**: `Microsoft.ContainerRegistry/registries`

### Web Resources
- **App Services**: `Microsoft.Web/sites`
- **App Service Plans**: `Microsoft.Web/serverfarms`

### Messaging Resources
- **Event Hubs**: `Microsoft.EventHub/namespaces`
- **Service Bus**: `Microsoft.ServiceBus/namespaces`

### Monitoring Resources
- **Log Analytics Workspaces**: `Microsoft.OperationalInsights/workspaces`
- **Application Insights**: `Microsoft.Insights/components`

### Other Resources
- **Azure Cache for Redis**: `Microsoft.Cache/Redis`
- **App Configuration**: `Microsoft.AppConfiguration/configurationStores`
- **Traffic Manager**: `Microsoft.Network/trafficManagerProfiles`

**Note**: Any Azure resource that can be referenced via resource ID in azapi_resource or azurerm_resource attributes is affected by this issue.

## Suggested Fix Approach

### High-Level Solution

Add a helper method to format resource names with the 🆔 icon, similar to existing `FormatSubscriptionLabel` and `FormatResourceGroupLabel` methods, and apply it consistently in both formatters.

### Detailed Changes

#### 1. Add Resource Name Formatting Helper to `EnrichedAzureScopeFormatter`

**File**: `src/Oocx.TfPlan2Md/Platforms/Azure/EnrichedAzureScopeFormatter.cs`

Add a new private method (after line 215):

```csharp
/// <summary>
/// Formats a resource name label with the ID icon when available.
/// </summary>
/// <param name="resourceName">The resource name to format.</param>
/// <returns>Resource name with icon prefix.</returns>
private static string FormatResourceNameLabel(string? resourceName)
{
    if (string.IsNullOrWhiteSpace(resourceName))
    {
        return string.Empty;
    }

    return $"🆔{AzureLabelFormatter.NonBreakingSpace}{resourceName}";
}
```

**Icon Constant**: The 🆔 icon is already used for `name` attributes in `SemanticFormatting.Identity.cs` line 198-199.

#### 2. Update `EnrichedAzureScopeFormatter.Format` Method

**File**: `src/Oocx.TfPlan2Md/Platforms/Azure/EnrichedAzureScopeFormatter.cs`  
**Lines**: 83-107

Update the Format method to use the new helper:

```csharp
internal string Format(ScopeInfo scopeInfo, string? resourceAddress = null)
{
    if (scopeInfo.Level == ScopeLevel.Unknown)
    {
        return scopeInfo.Details;
    }

    var subscriptionDisplay = _entityMapper.GetSubscriptionDisplayName(scopeInfo.SubscriptionId, resourceAddress);
    var subscriptionLabel = string.IsNullOrWhiteSpace(subscriptionDisplay)
        ? scopeInfo.SubscriptionId ?? string.Empty
        : subscriptionDisplay;
    var subscriptionValue = FormatSubscriptionLabel(subscriptionLabel);
    var resourceGroupValue = FormatResourceGroupLabel(scopeInfo.ResourceGroup);
    var resourceNameValue = FormatResourceNameLabel(scopeInfo.Name); // NEW LINE

    return scopeInfo.Level switch
    {
        ScopeLevel.ManagementGroup => FormatManagementGroup(scopeInfo.Name, resourceAddress),
        ScopeLevel.Subscription => $"subscription `{subscriptionValue}`",
        ScopeLevel.ResourceGroup => $"`{resourceGroupValue}` in subscription `{subscriptionValue}`",
        ScopeLevel.Resource when !string.IsNullOrWhiteSpace(scopeInfo.ResourceGroup) =>
            $"{scopeInfo.Type} `{resourceNameValue}` in resource group `{resourceGroupValue}` of subscription `{subscriptionValue}`", // MODIFIED
        ScopeLevel.Resource => $"{scopeInfo.Type} `{resourceNameValue}` in subscription `{subscriptionValue}`", // MODIFIED
        _ => scopeInfo.Details
    };
}
```

#### 3. Update `AzureScopeParser.ParseScope` Method

**File**: `src/Oocx.TfPlan2Md/Platforms/Azure/AzureScopeParser.cs`  
**Lines**: 154-168

Add icon constants and update the ParseScope method:

```csharp
/// <summary>
/// Icon for resource group identifiers.
/// </summary>
private const string ResourceGroupIcon = "📁";

/// <summary>
/// Icon for resource name identifiers.
/// </summary>
private const string ResourceNameIcon = "🆔";

/// <summary>
/// Formats a resource group label with icon.
/// </summary>
private static string FormatResourceGroupLabel(string? resourceGroup)
{
    if (string.IsNullOrWhiteSpace(resourceGroup))
    {
        return string.Empty;
    }
    return $"{ResourceGroupIcon}{NonBreakingSpace}{resourceGroup}";
}

/// <summary>
/// Formats a resource name label with icon.
/// </summary>
private static string FormatResourceNameLabel(string? resourceName)
{
    if (string.IsNullOrWhiteSpace(resourceName))
    {
        return string.Empty;
    }
    return $"{ResourceNameIcon}{NonBreakingSpace}{resourceName}";
}

public static string ParseScope(string? scope)
{
    var parsed = Parse(scope);

    return parsed.Level switch
    {
        ScopeLevel.ManagementGroup => $"`{parsed.Name}` (Management Group)",
        ScopeLevel.Subscription => $"subscription `{FormatSubscriptionId(parsed.SubscriptionId ?? string.Empty)}`",
        ScopeLevel.Resource when !string.IsNullOrWhiteSpace(parsed.ResourceGroup) =>
            $"{parsed.Type} `{FormatResourceNameLabel(parsed.Name)}` in resource group `{FormatResourceGroupLabel(parsed.ResourceGroup)}` of subscription `{FormatSubscriptionId(parsed.SubscriptionId ?? string.Empty)}`",
        ScopeLevel.Resource => $"{parsed.Type} `{FormatResourceNameLabel(parsed.Name)}` in subscription `{FormatSubscriptionId(parsed.SubscriptionId ?? string.Empty)}`",
        ScopeLevel.ResourceGroup => $"`{FormatResourceGroupLabel(parsed.ResourceGroup)}` in subscription `{FormatSubscriptionId(parsed.SubscriptionId ?? string.Empty)}`",
        _ => parsed.Details
    };
}
```

### Why This Approach

1. **Consistent with existing patterns**: Uses the same approach as `FormatSubscriptionLabel` and `FormatResourceGroupLabel`
2. **Leverages existing icons**: Uses the 🆔 icon already established for `name` attributes and 📁 for resource groups
3. **Non-breaking space handling**: Maintains consistent spacing with `NonBreakingSpace` constant
4. **Fixes both paths**: Updates both `EnrichedAzureScopeFormatter` (used with entity mapper) and `AzureScopeParser` (fallback without mapper)
5. **Minimal change scope**: Localized changes to two files, reducing risk

## Related Tests That Need to Pass

After implementing the fix, the following test categories should be verified:

### Unit Tests
- [ ] `AzureScopeParserTests.cs` - Tests for `ParseScope` method output
- [ ] `AzureValueFormatterTests.cs` - Tests for Azure resource ID formatting
- [ ] `ScribanHelpersAzureScopeFormattingTests.cs` - Tests for scope formatting in templates

### Integration Tests
- [ ] `MarkdownRendererTests.cs` - Full rendering tests with azapi resources
- [ ] `ComprehensiveDemoTests.cs` - Comprehensive demo output verification

### Snapshot Tests
- [ ] Any snapshot tests that include Azure resource IDs in attribute values will need updates
- [ ] Look for tests with patterns like `Microsoft.Network/routeTables`, `Microsoft.Network/networkSecurityGroups`, etc.

### Manual Testing
- [ ] Create a test plan with an azapi_resource that references:
  - A route table (the reported issue)
  - A network security group
  - A subnet
  - Any other Azure resource via ID
- [ ] Verify all resource names and resource groups show the correct icons
- [ ] Verify subscription icons are still present
- [ ] Check both table and summary rendering contexts

## Additional Context

### Related Features
- **docs/features/019-azure-resource-id-formatting/specification.md** - Original Azure resource ID formatting feature
- **docs/features/024-visual-report-enhancements/specification.md** - Semantic icons for attributes
- **docs/features/029-report-presentation-enhancements/specification.md** - Name attribute formatting with icons
- **docs/features/051-display-enhancements/specification.md** - Subscription icon formatting
- **docs/features/063-azure-display-enhancements/specification.md** - Azure display name enrichment
- **docs/features/065-tenant-display-mapping/specification.md** - Entity mapper and display names

### Icon Reference
Per the codebase standards established in semantic formatting:
- 🆔 - Used for `name` attributes (resource identifiers)
- 📁 - Used for `resource_group_name` attributes
- 🔑 - Used for `subscription_id` attributes
- 🗂️ - Used for management group identifiers
- 🏢 - Used for tenant identifiers

### Impact Assessment
- **Severity**: Medium - Visual consistency issue, not functional
- **Scope**: All Azure resource ID attribute values across all resource types
- **Breaking Change**: No - only adds icons to existing output
- **Test Updates Required**: Yes - snapshot tests will detect the icon additions

## Definition of Done

The fix is complete when:
- [ ] Resource names in Azure resource IDs display with 🆔 icon
- [ ] Resource groups in Azure resource IDs display with 📁 icon
- [ ] Subscriptions continue to display with 🔑 icon
- [ ] Both `EnrichedAzureScopeFormatter.Format` and `AzureScopeParser.ParseScope` are updated
- [ ] All existing tests pass (or are updated for new icon expectations)
- [ ] Manual verification with route table, NSG, and subnet resource IDs confirms icons appear
- [ ] Code review confirms consistent application of icon formatting patterns
