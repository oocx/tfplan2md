using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Parsing;

/// <summary>
/// Tests configuration reference index building for parent-child matching.
/// </summary>
public class ConfigurationReferenceResolverTests
{
    private readonly TerraformPlanParser _parser = new();

    /// <summary>
    /// Ensures references are captured for root module resources.
    /// </summary>
    [Test]
    public void BuildReferenceIndex_RootModule_ContainsReferences()
    {
        var json = File.ReadAllText("TestData/azuread-group-members-known-after-apply-plan.json");
        var plan = _parser.Parse(json);

        var index = ConfigurationReferenceResolver.BuildReferenceIndex(plan.Configuration);

        index.ContainsKey(("azuread_group_member.platform_admin_member", "group_object_id")).Should().BeTrue();
        index[("azuread_group_member.platform_admin_member", "group_object_id")]
            .Should()
            .Contain("azuread_group.platform_engineers.id");
    }

    /// <summary>
    /// Ensures missing configuration returns an empty index.
    /// </summary>
    [Test]
    public void BuildReferenceIndex_NullConfiguration_ReturnsEmpty()
    {
        var index = ConfigurationReferenceResolver.BuildReferenceIndex(null);

        index.Should().BeEmpty();
    }

    /// <summary>
    /// Ensures nested module references are qualified with module prefixes.
    /// </summary>
    [Test]
    public void BuildReferenceIndex_NestedModules_QualifiesAddresses()
    {
        var json = File.ReadAllText("TestData/configuration-with-nested-modules.json");
        var plan = _parser.Parse(json);

        var index = ConfigurationReferenceResolver.BuildReferenceIndex(plan.Configuration);

        index.ContainsKey(("module.security.azuread_group_member.member1", "group_object_id")).Should().BeTrue();
        index[("module.security.azuread_group_member.member1", "group_object_id")]
            .Should()
            .Contain("module.security.azuread_group.admins.id");
    }

    /// <summary>
    /// Ensures for_each resources are indexed by their base address.
    /// </summary>
    [Test]
    public void BuildReferenceIndex_ForEach_UsesBaseAddress()
    {
        var json = File.ReadAllText("TestData/configuration-with-for-each.json");
        var plan = _parser.Parse(json);

        var index = ConfigurationReferenceResolver.BuildReferenceIndex(plan.Configuration);

        index.ContainsKey(("azuread_group_member.members", "group_object_id")).Should().BeTrue();
    }

    /// <summary>
    /// Ensures reference index building completes quickly for large configurations.
    /// </summary>
    [Test]
    public void BuildReferenceIndex_LargeConfiguration_CompletesQuickly()
    {
        const int resourceCount = 1000;
        var configuration = BuildLargeConfiguration(resourceCount);

        var stopwatch = Stopwatch.StartNew();
        var index = ConfigurationReferenceResolver.BuildReferenceIndex(configuration);
        stopwatch.Stop();

        index.Should().HaveCount(resourceCount);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Builds a large configuration block for performance testing.
    /// </summary>
    /// <param name="resourceCount">The number of resources to include.</param>
    /// <returns>The configuration element.</returns>
    private static JsonElement BuildLargeConfiguration(int resourceCount)
    {
        var builder = new StringBuilder();
        builder.Append("{\"root_module\":{\"resources\":[");

        for (var index = 0; index < resourceCount; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append("{\"address\":\"custom_child.member");
            builder.Append(index);
            builder.Append("\",\"mode\":\"managed\",\"type\":\"custom_child\",\"name\":\"member");
            builder.Append(index);
            builder.Append("\",\"expressions\":{\"parent_id\":{\"references\":[\"custom_parent.team.id\"]}}}");
        }

        builder.Append("]}}");

        return JsonDocument.Parse(builder.ToString()).RootElement;
    }

    /// <summary>
    /// Ensures references field with Object type does not crash.
    /// Related issue: 071-json-parsing-error-azurerm-resources
    /// </summary>
    [Test]
    public void BuildReferenceIndex_ReferencesAsObject_DoesNotCrash()
    {
        // Arrange - create a configuration with references as an Object instead of Array
        var json = """
        {
            "root_module": {
                "resources": [
                    {
                        "address": "azurerm_storage_container.example",
                        "mode": "managed",
                        "type": "azurerm_storage_container",
                        "name": "example",
                        "expressions": {
                            "storage_account_id": {
                                "references": {
                                    "type": "object",
                                    "value": "azurerm_storage_account.example.id"
                                }
                            }
                        }
                    }
                ]
            }
        }
        """;
        var configuration = JsonDocument.Parse(json).RootElement;

        // Act - should not throw
        var index = ConfigurationReferenceResolver.BuildReferenceIndex(configuration);

        // Assert - should return empty or skip the malformed reference
        index.Should().BeEmpty();
    }

    /// <summary>
    /// Ensures missing references property does not crash.
    /// Related issue: 071-json-parsing-error-azurerm-resources
    /// </summary>
    [Test]
    public void BuildReferenceIndex_MissingReferences_DoesNotCrash()
    {
        // Arrange - create a configuration without references property
        var json = """
        {
            "root_module": {
                "resources": [
                    {
                        "address": "azurerm_role_assignment.example",
                        "mode": "managed",
                        "type": "azurerm_role_assignment",
                        "name": "example",
                        "expressions": {
                            "principal_id": {
                                "constant_value": "12345-67890"
                            }
                        }
                    }
                ]
            }
        }
        """;
        var configuration = JsonDocument.Parse(json).RootElement;

        // Act - should not throw
        var index = ConfigurationReferenceResolver.BuildReferenceIndex(configuration);

        // Assert - should return empty since no references exist
        index.Should().BeEmpty();
    }

    /// <summary>
    /// Ensures null references value does not crash.
    /// Related issue: 071-json-parsing-error-azurerm-resources
    /// </summary>
    [Test]
    public void BuildReferenceIndex_NullReferencesValue_DoesNotCrash()
    {
        // Arrange - create a configuration with null references value
        var json = """
        {
            "root_module": {
                "resources": [
                    {
                        "address": "azurerm_resource.example",
                        "mode": "managed",
                        "type": "azurerm_resource",
                        "name": "example",
                        "expressions": {
                            "parent_id": {
                                "references": null
                            }
                        }
                    }
                ]
            }
        }
        """;
        var configuration = JsonDocument.Parse(json).RootElement;

        // Act - should not throw
        var index = ConfigurationReferenceResolver.BuildReferenceIndex(configuration);

        // Assert - should return empty since references is null
        index.Should().BeEmpty();
    }

    /// <summary>
    /// Ensures references as string (primitive) does not crash.
    /// Related issue: 071-json-parsing-error-azurerm-resources
    /// </summary>
    [Test]
    public void BuildReferenceIndex_ReferencesAsString_DoesNotCrash()
    {
        // Arrange - create a configuration with references as a string primitive
        var json = """
        {
            "root_module": {
                "resources": [
                    {
                        "address": "azurerm_resource.example",
                        "mode": "managed",
                        "type": "azurerm_resource",
                        "name": "example",
                        "expressions": {
                            "parent_id": {
                                "references": "azurerm_parent.example.id"
                            }
                        }
                    }
                ]
            }
        }
        """;
        var configuration = JsonDocument.Parse(json).RootElement;

        // Act - should not throw
        var index = ConfigurationReferenceResolver.BuildReferenceIndex(configuration);

        // Assert - should return empty since references is not an array
        index.Should().BeEmpty();
    }

    /// <summary>
    /// Ensures empty references array is handled correctly.
    /// Related issue: 071-json-parsing-error-azurerm-resources
    /// </summary>
    [Test]
    public void BuildReferenceIndex_EmptyReferencesArray_ReturnsEmpty()
    {
        // Arrange - create a configuration with empty references array
        var json = """
        {
            "root_module": {
                "resources": [
                    {
                        "address": "azurerm_resource.example",
                        "mode": "managed",
                        "type": "azurerm_resource",
                        "name": "example",
                        "expressions": {
                            "parent_id": {
                                "references": []
                            }
                        }
                    }
                ]
            }
        }
        """;
        var configuration = JsonDocument.Parse(json).RootElement;

        // Act
        var index = ConfigurationReferenceResolver.BuildReferenceIndex(configuration);

        // Assert - should return empty since references array is empty
        index.Should().BeEmpty();
    }

    /// <summary>
    /// Ensures expression properties with Array values (nested blocks) do not crash.
    /// Related issue: 072-json-element-wrong-type-error
    /// </summary>
    /// <remarks>
    /// When Terraform configurations have nested blocks (arrays), expression properties
    /// can have Array values like `authentication_credentials: [{ credential_id: "test" }]`
    /// instead of Object values like `{ "references": [...] }`.
    /// The code must check ValueKind BEFORE calling TryGetProperty to prevent
    /// JsonElementHasWrongType exceptions.
    /// </remarks>
    [Test]
    public void BuildReferenceIndex_ExpressionPropertyAsArray_DoesNotCrash()
    {
        // Arrange - create a configuration with array-valued expression property
        var json = """
        {
            "root_module": {
                "resources": [
                    {
                        "address": "azurerm_resource.test",
                        "mode": "managed",
                        "type": "azurerm_resource",
                        "name": "test",
                        "expressions": {
                            "authentication_credentials": [
                                {
                                    "credential_id": "test"
                                }
                            ]
                        }
                    }
                ]
            }
        }
        """;
        var configuration = JsonDocument.Parse(json).RootElement;

        // Act - should not throw JsonElementHasWrongType exception
        var index = ConfigurationReferenceResolver.BuildReferenceIndex(configuration);

        // Assert - should return empty index for this resource (no references)
        index.Should().BeEmpty();
    }

    /// <summary>
    /// Ensures the exact plan.json from issue #464 processes without error.
    /// Related issue: https://github.com/oocx/tfplan2md/issues/464
    /// </summary>
    /// <remarks>
    /// This is a real-world integration test using the exact Terraform plan that triggered
    /// the JsonElementHasWrongType error. The plan contains azurerm_data_factory_trigger_schedule
    /// resources with nested `pipeline` blocks that have Array-valued expression properties.
    /// </remarks>
    [Test]
    public void BuildReferenceIndex_Issue464PlanJson_DoesNotCrash()
    {
        // Arrange - use the exact file provided in issue #464
        var json = File.ReadAllText("TestData/issue-464-plan.json");
        var plan = _parser.Parse(json);

        // Act - should not throw JsonElementHasWrongType
        var index = ConfigurationReferenceResolver.BuildReferenceIndex(plan.Configuration);

        // Assert - should complete successfully
        index.Should().NotBeNull();
    }
}
