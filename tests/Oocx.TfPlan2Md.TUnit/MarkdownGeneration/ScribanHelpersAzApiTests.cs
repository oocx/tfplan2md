using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Scriban.Runtime;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Unit tests for azapi_resource Scriban helper functions.
/// Related feature: docs/features/040-azapi-resource-template/test-plan.md
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
        var result = ScribanHelpers.FlattenJson(json);

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
        var result = ScribanHelpers.FlattenJson(json);

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
        var result = ScribanHelpers.FlattenJson(json);

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
        var result = ScribanHelpers.FlattenJson(json);

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
        var result = ScribanHelpers.FlattenJson(json);

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
        var result = ScribanHelpers.FlattenJson(json);

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
        var result = ScribanHelpers.FlattenJson(json);

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
        var result = ScribanHelpers.FlattenJson(nullInput);

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
        var result = ScribanHelpers.ParseAzureResourceType(resourceType);

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
        var result = ScribanHelpers.ParseAzureResourceType(resourceType);

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
        var result = ScribanHelpers.ParseAzureResourceType(resourceType);

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
        var result = ScribanHelpers.ParseAzureResourceType(resourceType);

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
        var result = ScribanHelpers.ParseAzureResourceType(resourceType);

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
        var result = ScribanHelpers.ParseAzureResourceType(resourceType);

        // Assert
        result["provider"].Should().Be(string.Empty);
        result["service"].Should().Be(string.Empty);
        result["resource_type"].Should().Be(string.Empty);
        result["api_version"].Should().Be(string.Empty);

        await Task.CompletedTask;
    }

    #endregion
}
