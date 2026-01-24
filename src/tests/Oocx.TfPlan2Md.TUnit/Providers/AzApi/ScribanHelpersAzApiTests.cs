using System.Linq;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Scriban.Runtime;
using TUnit.Core;
using AzApiHelpers = Oocx.TfPlan2Md.Providers.AzApi.ScribanHelpers;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Unit tests for azapi_resource Scriban helper functions.
/// Related feature: docs/features/040-azapi-resource-template/test-plan.md.
/// </summary>
public class ScribanHelpersAzApiTests
{
    #region FlattenJson Tests (TC-05 to TC-09, TC-30)

    [Test]
    public async Task FlattenJson_SimpleObject_ReturnsFlattenedProperties()
    {
        // Arrange - TC-05: Simple nested object
        var json = JsonDocument.Parse("""
            {
                "properties": {
                    "sku": {
                        "name": "Basic"
                    },
                    "enabled": true
                }
            }
            """).RootElement;

        // Act
        var result = AzApiHelpers.FlattenJson(json);

        // Assert
        result.Should().HaveCount(2);

        var prop1 = result[0] as ScriptObject;
        prop1?["path"].Should().Be("properties.sku.name");
        prop1?["value"].Should().Be("Basic");
        prop1?["is_large"].Should().Be(false);

        var prop2 = result[1] as ScriptObject;
        prop2?["path"].Should().Be("properties.enabled");
        prop2?["value"].Should().Be(true);
        prop2?["is_large"].Should().Be(false);

        await Task.CompletedTask;
    }

    [Test]
    public async Task FlattenJson_ArrayProperty_UsesArrayIndexNotation()
    {
        // Arrange - TC-06: Arrays with index notation
        var json = JsonDocument.Parse("""
            {
                "tags": [
                    {"key": "env", "value": "prod"},
                    {"key": "owner", "value": "team"}
                ]
            }
            """).RootElement;

        // Act
        var result = AzApiHelpers.FlattenJson(json);

        // Assert
        result.Should().HaveCount(4);

        var prop1 = result[0] as ScriptObject;
        prop1?["path"].Should().Be("tags[0].key");
        prop1?["value"].Should().Be("env");

        var prop2 = result[1] as ScriptObject;
        prop2?["path"].Should().Be("tags[0].value");
        prop2?["value"].Should().Be("prod");

        var prop3 = result[2] as ScriptObject;
        prop3?["path"].Should().Be("tags[1].key");
        prop3?["value"].Should().Be("owner");

        var prop4 = result[3] as ScriptObject;
        prop4?["path"].Should().Be("tags[1].value");
        prop4?["value"].Should().Be("team");

        await Task.CompletedTask;
    }

    [Test]
    public async Task FlattenJson_DeepNesting_HandlesMultipleLevels()
    {
        // Arrange - TC-07: Deep nesting (5+ levels)
        var json = JsonDocument.Parse("""
            {
                "level1": {
                    "level2": {
                        "level3": {
                            "level4": {
                                "level5": {
                                    "deepValue": "test"
                                }
                            }
                        }
                    }
                }
            }
            """).RootElement;

        // Act
        var result = AzApiHelpers.FlattenJson(json);

        // Assert
        result.Should().ContainSingle();

        var prop = result[0] as ScriptObject;
        prop?["path"].Should().Be("level1.level2.level3.level4.level5.deepValue");
        prop?["value"].Should().Be("test");
        prop?["is_large"].Should().Be(false);

        await Task.CompletedTask;
    }

    [Test]
    public async Task FlattenJson_EmptyObject_ReturnsEmptyArray()
    {
        // Arrange - TC-08: Empty object
        var json = JsonDocument.Parse("{}").RootElement;

        // Act
        var result = AzApiHelpers.FlattenJson(json);

        // Assert
        result.Should().BeEmpty();

        await Task.CompletedTask;
    }

    [Test]
    public async Task FlattenJson_NullValue_IncludesNullInResult()
    {
        // Arrange - TC-08: Null values
        var json = JsonDocument.Parse("""
            {
                "property": null,
                "other": "value"
            }
            """).RootElement;

        // Act
        var result = AzApiHelpers.FlattenJson(json);

        // Assert
        result.Should().HaveCount(2);

        var prop1 = result[0] as ScriptObject;
        prop1?["path"].Should().Be("property");
        prop1?["value"].Should().BeNull();

        var prop2 = result[1] as ScriptObject;
        prop2?["path"].Should().Be("other");
        prop2?["value"].Should().Be("value");

        await Task.CompletedTask;
    }

    [Test]
    public async Task FlattenJson_LargeValue_MarksAsLarge()
    {
        // Arrange - TC-09: Large value detection (>200 chars)
        var longValue = new string('x', 250); // Exceeds 200 char threshold
        var json = JsonDocument.Parse($$"""
            {
                "connectionString": "{{longValue}}"
            }
            """).RootElement;

        // Act
        var result = AzApiHelpers.FlattenJson(json);

        // Assert
        result.Should().ContainSingle();

        var prop = result[0] as ScriptObject;
        prop?["path"].Should().Be("connectionString");
        prop?["is_large"].Should().Be(true);

        await Task.CompletedTask;
    }

    [Test]
    public async Task FlattenJson_MixedTypeArray_HandlesAllTypes()
    {
        // Arrange - TC-30: Mixed types in array
        var json = JsonDocument.Parse("""
            {
                "mixed": [
                    "string",
                    123,
                    true,
                    null,
                    {"nested": "object"}
                ]
            }
            """).RootElement;

        // Act
        var result = AzApiHelpers.FlattenJson(json);

        // Assert
        result.Should().HaveCount(5);

        var prop1 = result[0] as ScriptObject;
        prop1?["path"].Should().Be("mixed[0]");
        prop1?["value"].Should().Be("string");

        var prop2 = result[1] as ScriptObject;
        prop2?["path"].Should().Be("mixed[1]");
        prop2?["value"].Should().Be(123L);

        var prop3 = result[2] as ScriptObject;
        prop3?["path"].Should().Be("mixed[2]");
        prop3?["value"].Should().Be(true);

        var prop4 = result[3] as ScriptObject;
        prop4?["path"].Should().Be("mixed[3]");
        prop4?["value"].Should().BeNull();

        var prop5 = result[4] as ScriptObject;
        prop5?["path"].Should().Be("mixed[4].nested");
        prop5?["value"].Should().Be("object");

        await Task.CompletedTask;
    }

    [Test]
    public async Task FlattenJson_NullInput_ReturnsEmptyArray()
    {
        // Arrange
        object? nullInput = null;

        // Act
        var result = AzApiHelpers.FlattenJson(nullInput);

        // Assert
        result.Should().BeEmpty();

        await Task.CompletedTask;
    }

    #endregion

    #region ParseAzureResourceType Tests (TC-16)

    [Test]
    public async Task ParseAzureResourceType_ValidType_ParsesAllComponents()
    {
        // Arrange - TC-16: Parse resource type components
        var resourceType = "Microsoft.Automation/automationAccounts@2021-06-22";

        // Act
        var result = AzApiHelpers.ParseAzureResourceType(resourceType);

        // Assert
        result["provider"].Should().Be("Microsoft.Automation");
        result["service"].Should().Be("Automation");
        result["resource_type"].Should().Be("automationAccounts");
        result["api_version"].Should().Be("2021-06-22");

        await Task.CompletedTask;
    }

    [Test]
    public async Task ParseAzureResourceType_WithoutApiVersion_ParsesProviderAndType()
    {
        // Arrange
        var resourceType = "Microsoft.Storage/storageAccounts";

        // Act
        var result = AzApiHelpers.ParseAzureResourceType(resourceType);

        // Assert
        result["provider"].Should().Be("Microsoft.Storage");
        result["service"].Should().Be("Storage");
        result["resource_type"].Should().Be("storageAccounts");
        result["api_version"].Should().Be(string.Empty);

        await Task.CompletedTask;
    }

    [Test]
    public async Task ParseAzureResourceType_NonMicrosoftProvider_ParsesButEmptyService()
    {
        // Arrange
        var resourceType = "Custom.Provider/customResource@2023-01-01";

        // Act
        var result = AzApiHelpers.ParseAzureResourceType(resourceType);

        // Assert
        result["provider"].Should().Be("Custom.Provider");
        result["service"].Should().Be(string.Empty);
        result["resource_type"].Should().Be("customResource");
        result["api_version"].Should().Be("2023-01-01");

        await Task.CompletedTask;
    }

    [Test]
    public async Task ParseAzureResourceType_InvalidFormat_ReturnsEmptyComponents()
    {
        // Arrange
        var resourceType = "InvalidFormat";

        // Act
        var result = AzApiHelpers.ParseAzureResourceType(resourceType);

        // Assert
        result["provider"].Should().Be(string.Empty);
        result["service"].Should().Be(string.Empty);
        result["resource_type"].Should().Be(string.Empty);
        result["api_version"].Should().Be(string.Empty);

        await Task.CompletedTask;
    }

    [Test]
    public async Task ParseAzureResourceType_NullInput_ReturnsEmptyComponents()
    {
        // Arrange
        string? resourceType = null;

        // Act
        var result = AzApiHelpers.ParseAzureResourceType(resourceType);

        // Assert
        result["provider"].Should().Be(string.Empty);
        result["service"].Should().Be(string.Empty);
        result["resource_type"].Should().Be(string.Empty);
        result["api_version"].Should().Be(string.Empty);

        await Task.CompletedTask;
    }

    [Test]
    public async Task ParseAzureResourceType_EmptyString_ReturnsEmptyComponents()
    {
        // Arrange
        var resourceType = "";

        // Act
        var result = AzApiHelpers.ParseAzureResourceType(resourceType);

        // Assert
        result["provider"].Should().Be(string.Empty);
        result["service"].Should().Be(string.Empty);
        result["resource_type"].Should().Be(string.Empty);
        result["api_version"].Should().Be(string.Empty);

        await Task.CompletedTask;
    }

    #endregion

    #region AzureApiDocLink Tests (TC-14, TC-15)

    [Test]
    public async Task AzureApiDocLink_MicrosoftResourceType_ConstructsUrl()
    {
        // Arrange - TC-14: Generate documentation link
        var resourceType = "Microsoft.Automation/automationAccounts@2021-06-22";

        // Act
        var result = AzApiHelpers.AzureApiDocLink(resourceType);

        // Assert
        result.Should().Be("https://learn.microsoft.com/rest/api/automation/automation-accounts/");

        await Task.CompletedTask;
    }

    [Test]
    public async Task AzureApiDocLink_DifferentServices_GeneratesCorrectUrls()
    {
        // Arrange
        var tests = new[]
        {
            ("Microsoft.Storage/storageAccounts@2023-01-01", "https://learn.microsoft.com/rest/api/storage/storage-accounts/"),
            ("Microsoft.Network/virtualNetworks@2022-07-01", "https://learn.microsoft.com/rest/api/network/virtual-networks/"),
            ("Microsoft.Compute/virtualMachines@2023-03-01", "https://learn.microsoft.com/rest/api/compute/virtual-machines/")
        };

        // Act & Assert
        foreach (var (resourceType, expectedUrl) in tests)
        {
            var result = AzApiHelpers.AzureApiDocLink(resourceType);
            result.Should().Be(expectedUrl, $"for resource type {resourceType}");
        }

        await Task.CompletedTask;
    }

    [Test]
    public async Task AzureApiDocLink_NonMicrosoftProvider_ReturnsNull()
    {
        // Arrange - TC-15: Non-Microsoft provider
        var resourceType = "Custom.Provider/customResource@2023-01-01";

        // Act
        var result = AzApiHelpers.AzureApiDocLink(resourceType);

        // Assert
        result.Should().BeNull();

        await Task.CompletedTask;
    }

    [Test]
    public async Task AzureApiDocLink_NullInput_ReturnsNull()
    {
        // Arrange
        string? resourceType = null;

        // Act
        var result = AzApiHelpers.AzureApiDocLink(resourceType);

        // Assert
        result.Should().BeNull();

        await Task.CompletedTask;
    }

    [Test]
    public async Task AzureApiDocLink_InvalidFormat_ReturnsNull()
    {
        // Arrange
        var resourceType = "InvalidFormat";

        // Act
        var result = AzApiHelpers.AzureApiDocLink(resourceType);

        // Assert
        result.Should().BeNull();

        await Task.CompletedTask;
    }

    #endregion

    #region ExtractAzapiMetadata Tests (TC-02, TC-03, TC-04)

    [Test]
    public async Task ExtractAzapiMetadata_CreateOperation_ExtractsFromAfterState()
    {
        // Arrange - TC-02: Extract standard attributes
        var change = new ResourceChangeModel
        {
            Address = "azapi_resource.test",
            Type = "azapi_resource",
            Name = "test",
            ProviderName = "azapi",
            Action = "create",
            ActionSymbol = "➕",
            AttributeChanges = [],
            AfterJson = new ScriptObject
            {
                ["type"] = "Microsoft.Automation/automationAccounts@2021-06-22",
                ["name"] = "myAccount",
                ["parent_id"] = "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/example-resources",
                ["location"] = "westeurope",
                ["tags"] = new ScriptObject
                {
                    ["environment"] = "dev",
                    ["project"] = "demo"
                }
            }
        };

        // Act
        var result = AzApiHelpers.ExtractAzapiMetadata(change);

        // Assert
        result["type"].Should().Be("`Microsoft.Automation/automationAccounts@2021-06-22`");
        result["name"].Should().Be("`myAccount`");
        result["parent_id"].Should().Be("example-resources"); // Resource group summary format
        result["location"].Should().Be("🌍 `westeurope`");
        result["tags"].Should().NotBeNull();

        await Task.CompletedTask;
    }

    [Test]
    public async Task ExtractAzapiMetadata_DeleteOperation_ExtractsFromBeforeState()
    {
        // Arrange - TC-03: Extract from before state for delete
        var change = new ResourceChangeModel
        {
            Address = "azapi_resource.test",
            Type = "azapi_resource",
            Name = "test",
            ProviderName = "azapi",
            Action = "delete",
            ActionSymbol = "❌",
            AttributeChanges = [],
            BeforeJson = new ScriptObject
            {
                ["type"] = "Microsoft.Storage/storageAccounts@2023-01-01",
                ["name"] = "myStorageAccount",
                ["parent_id"] = "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/storage-rg",
                ["location"] = "eastus"
            }
        };

        // Act
        var result = AzApiHelpers.ExtractAzapiMetadata(change);

        // Assert
        result["type"].Should().Be("`Microsoft.Storage/storageAccounts@2023-01-01`");
        result["name"].Should().Be("`myStorageAccount`");
        result["parent_id"].Should().Be("storage-rg"); // Resource group summary format
        result["location"].Should().Be("🌍 `eastus`");

        await Task.CompletedTask;
    }

    [Test]
    public async Task ExtractAzapiMetadata_MissingOptionalAttributes_HandlesGracefully()
    {
        // Arrange - TC-04: Missing optional attributes
        var change = new ResourceChangeModel
        {
            Address = "azapi_resource.test",
            Type = "azapi_resource",
            Name = "test",
            ProviderName = "azapi",
            Action = "create",
            ActionSymbol = "➕",
            AttributeChanges = [],
            AfterJson = new ScriptObject
            {
                ["type"] = "Microsoft.Resources/resourceGroups@2021-04-01",
                ["name"] = "minimal-rg"
                // No parent_id, location, or tags
            }
        };

        // Act
        var result = AzApiHelpers.ExtractAzapiMetadata(change);

        // Assert
        result["type"].Should().Be("`Microsoft.Resources/resourceGroups@2021-04-01`");
        result["name"].Should().Be("`minimal-rg`");
        result.ContainsKey("parent_id").Should().BeFalse();
        result.ContainsKey("location").Should().BeFalse();
        result.ContainsKey("tags").Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task ExtractAzapiMetadata_NullChange_ReturnsEmptyObject()
    {
        // Arrange
        object? nullChange = null;

        // Act
        var result = AzApiHelpers.ExtractAzapiMetadata(nullChange);

        // Assert
        result.Should().BeEmpty();

        await Task.CompletedTask;
    }

    #endregion

    #region CompareJsonProperties Tests (TC-10 to TC-13, TC-34)

    [Test]
    public async Task CompareJsonProperties_AddedProperties_DetectsNewProperties()
    {
        // Arrange - TC-10: Detect added properties
        var before = JsonDocument.Parse("""
            {
                "properties": {
                    "enabled": true,
                    "name": "test"
                }
            }
            """).RootElement;

        var after = JsonDocument.Parse("""
            {
                "properties": {
                    "enabled": true,
                    "name": "test",
                    "newProperty": "newValue"
                }
            }
            """).RootElement;

        // Act
        var result = AzApiHelpers.CompareJsonProperties(
            before, after, null, null, showUnchanged: false, showSensitive: false);

        // Assert - Should only return the added property (showUnchanged: false)
        result.Should().HaveCount(1);

        var addedProp = result[0] as ScriptObject;
        addedProp?["path"].Should().Be("properties.newProperty");
        addedProp?["before"].Should().BeNull();
        addedProp?["after"].Should().Be("newValue");
        addedProp?["is_changed"].Should().Be(true);

        await Task.CompletedTask;
    }

    [Test]
    public async Task CompareJsonProperties_RemovedProperties_DetectsDeletedProperties()
    {
        // Arrange - TC-11: Detect removed properties
        var before = JsonDocument.Parse("""
            {
                "properties": {
                    "enabled": true,
                    "name": "test",
                    "removedProp": "oldValue"
                }
            }
            """).RootElement;

        var after = JsonDocument.Parse("""
            {
                "properties": {
                    "enabled": true,
                    "name": "test"
                }
            }
            """).RootElement;

        // Act
        var result = AzApiHelpers.CompareJsonProperties(
            before, after, null, null, showUnchanged: false, showSensitive: false);

        // Assert - Should only return the removed property
        result.Should().HaveCount(1);

        var removedProp = result[0] as ScriptObject;
        removedProp?["path"].Should().Be("properties.removedProp");
        removedProp?["before"].Should().Be("oldValue");
        removedProp?["after"].Should().BeNull();
        removedProp?["is_changed"].Should().Be(true);

        await Task.CompletedTask;
    }

    [Test]
    public async Task CompareJsonProperties_ModifiedProperties_DetectsValueChanges()
    {
        // Arrange - TC-12: Detect modified properties
        var before = JsonDocument.Parse("""
            {
                "properties": {
                    "sku": {
                        "name": "Basic"
                    }
                }
            }
            """).RootElement;

        var after = JsonDocument.Parse("""
            {
                "properties": {
                    "sku": {
                        "name": "Standard"
                    }
                }
            }
            """).RootElement;

        // Act
        var result = AzApiHelpers.CompareJsonProperties(
            before, after, null, null, showUnchanged: false, showSensitive: false);

        // Assert - Should return the changed property
        result.Should().HaveCount(1);

        var changedProp = result[0] as ScriptObject;
        changedProp?["path"].Should().Be("properties.sku.name");
        changedProp?["before"].Should().Be("Basic");
        changedProp?["after"].Should().Be("Standard");
        changedProp?["is_changed"].Should().Be(true);

        await Task.CompletedTask;
    }

    [Test]
    public async Task CompareJsonProperties_ShowUnchangedTrue_ReturnsAllProperties()
    {
        // Arrange - TC-13: showUnchanged flag
        var before = JsonDocument.Parse("""
            {
                "properties": {
                    "enabled": true,
                    "name": "test"
                }
            }
            """).RootElement;

        var after = JsonDocument.Parse("""
            {
                "properties": {
                    "enabled": true,
                    "name": "updated"
                }
            }
            """).RootElement;

        // Act
        var result = AzApiHelpers.CompareJsonProperties(
            before, after, null, null, showUnchanged: true, showSensitive: false);

        // Assert - Should return both properties (1 changed, 1 unchanged)
        result.Should().HaveCount(2);

        var unchangedProp = result.FirstOrDefault(p =>
            (p as ScriptObject)?["path"]?.ToString() == "properties.enabled") as ScriptObject;
        unchangedProp?["before"].Should().Be(true);
        unchangedProp?["after"].Should().Be(true);
        unchangedProp?["is_changed"].Should().Be(false);

        var changedProp = result.FirstOrDefault(p =>
            (p as ScriptObject)?["path"]?.ToString() == "properties.name") as ScriptObject;
        changedProp?["before"].Should().Be("test");
        changedProp?["after"].Should().Be("updated");
        changedProp?["is_changed"].Should().Be(true);

        await Task.CompletedTask;
    }

    [Test]
    public async Task CompareJsonProperties_SensitiveProperties_MarksSensitiveFlags()
    {
        // Arrange - TC-34: Per-property sensitivity
        var before = JsonDocument.Parse("""
            {
                "properties": {
                    "password": "oldSecret",
                    "enabled": true
                }
            }
            """).RootElement;

        var after = JsonDocument.Parse("""
            {
                "properties": {
                    "password": "newSecret",
                    "enabled": true
                }
            }
            """).RootElement;

        var beforeSensitive = JsonDocument.Parse("""
            {
                "properties": {
                    "password": true
                }
            }
            """).RootElement;

        var afterSensitive = JsonDocument.Parse("""
            {
                "properties": {
                    "password": true
                }
            }
            """).RootElement;

        // Act
        var result = AzApiHelpers.CompareJsonProperties(
            before, after, beforeSensitive, afterSensitive, showUnchanged: false, showSensitive: false);

        // Assert - Should mark password as sensitive
        result.Should().HaveCount(1);

        var sensitiveProp = result[0] as ScriptObject;
        sensitiveProp?["path"].Should().Be("properties.password");
        sensitiveProp?["is_sensitive"].Should().Be(true);
        sensitiveProp?["is_changed"].Should().Be(true);

        await Task.CompletedTask;
    }

    [Test]
    public async Task CompareJsonProperties_NullInputs_ReturnsEmptyList()
    {
        // Arrange
        object? nullBefore = null;
        object? nullAfter = null;

        // Act
        var result = AzApiHelpers.CompareJsonProperties(
            nullBefore, nullAfter, null, null, showUnchanged: false, showSensitive: false);

        // Assert
        result.Should().BeEmpty();

        await Task.CompletedTask;
    }

    #endregion

    #region CompareJsonProperties Tests

    [Test]
    public async Task CompareJsonProperties_WhenShowUnchangedFalse_ReturnsOnlyChanges()
    {
        var before = JsonDocument.Parse("""
            {
                "unchanged": "same",
                "changed": "before"
            }
            """).RootElement;

        var after = JsonDocument.Parse("""
            {
                "unchanged": "same",
                "changed": "after"
            }
            """).RootElement;

        var result = AzApiHelpers.CompareJsonProperties(before, after, null, null, showUnchanged: false, showSensitive: false);

        result.Should().ContainSingle();
        var change = result[0] as ScriptObject;
        change?["path"].Should().Be("changed");
        change?["is_changed"].Should().Be(true);

        await Task.CompletedTask;
    }

    [Test]
    public async Task CompareJsonProperties_WhenShowUnchangedTrue_IncludesUnchanged()
    {
        var before = JsonDocument.Parse("""
            {
                "unchanged": "same",
                "changed": "before"
            }
            """).RootElement;

        var after = JsonDocument.Parse("""
            {
                "unchanged": "same",
                "changed": "after"
            }
            """).RootElement;

        var result = AzApiHelpers.CompareJsonProperties(before, after, null, null, showUnchanged: true, showSensitive: false);

        result.Should().HaveCount(2);
        var unchanged = result.Cast<ScriptObject>().Single(item => item["path"]?.ToString() == "unchanged");
        unchanged.Should().NotBeNull();

        await Task.CompletedTask;
    }

    [Test]
    public async Task CompareJsonProperties_MarksSensitivePaths_FromNestedStructure()
    {
        var before = JsonDocument.Parse("""
            {
                "secret": "value",
                "nested": { "inner": "value" },
                "array": ["value", "value2"]
            }
            """).RootElement;

        var after = JsonDocument.Parse("""
            {
                "secret": "value",
                "nested": { "inner": "value" },
                "array": ["value", "value2"]
            }
            """).RootElement;

        var sensitive = JsonDocument.Parse("""
            {
                "secret": true,
                "nested": { "inner": true },
                "array": [true, { "deep": true }]
            }
            """).RootElement;

        var result = AzApiHelpers.CompareJsonProperties(before, after, sensitive, null, showUnchanged: true, showSensitive: false);

        result.Cast<ScriptObject>().Single(item => item["path"]?.ToString() == "secret")["is_sensitive"].Should().Be(true);
        result.Cast<ScriptObject>().Single(item => item["path"]?.ToString() == "nested.inner")["is_sensitive"].Should().Be(true);
        result.Cast<ScriptObject>().Single(item => item["path"]?.ToString() == "array[0]")["is_sensitive"].Should().Be(true);

        await Task.CompletedTask;
    }

    [Test]
    public async Task CompareJsonProperties_NumericValues_WithDifferentTypes_AreEqual()
    {
        var before = JsonDocument.Parse("""
            {
                "count": 1
            }
            """).RootElement;

        var after = JsonDocument.Parse("""
            {
                "count": 1.0
            }
            """).RootElement;

        var result = AzApiHelpers.CompareJsonProperties(before, after, null, null, showUnchanged: true, showSensitive: false);

        var change = result[0] as ScriptObject;
        change?["is_changed"].Should().Be(false);

        await Task.CompletedTask;
    }

    [Test]
    public async Task CompareJsonProperties_MarksLargeValues()
    {
        var largeValue = new string('x', 250);
        var before = JsonDocument.Parse($$"""
            {
                "payload": "{{largeValue}}"
            }
            """).RootElement;

        var after = JsonDocument.Parse($$"""
            {
                "payload": "{{largeValue}}"
            }
            """).RootElement;

        var result = AzApiHelpers.CompareJsonProperties(before, after, null, null, showUnchanged: true, showSensitive: false);

        var change = result[0] as ScriptObject;
        change?["is_large"].Should().Be(true);

        await Task.CompletedTask;
    }

    #endregion
}
