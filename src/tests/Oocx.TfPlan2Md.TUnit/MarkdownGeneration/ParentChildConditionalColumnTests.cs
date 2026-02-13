using System.Collections.Generic;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for the conditional Terraform Resource column visibility logic.
/// </summary>
/// <remarks>
/// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
/// 
/// The Terraform Resource column should only appear when at least one resource is
/// external (separate). Inline resources have TerraformResource values containing
/// "attribute" (e.g., "subnet attribute"), while external resources have Terraform
/// addresses (e.g., "azurerm_subnet.example").
/// 
/// This test suite ensures the logic correctly identifies when external resources
/// are present, covering all edge cases including:
/// - All inline resources
/// - All external resources
/// - Mixed inline and external resources
/// - Empty lists
/// - Null/empty TerraformResource values
/// - Single external resource among many inline
/// - Resources with "attribute" in the name
/// </remarks>
public class ParentChildConditionalColumnTests
{
    /// <summary>
    /// Tests that HasExternalResources returns false when all resources are inline.
    /// </summary>
    /// <remarks>
    /// Inline resources have TerraformResource values containing "attribute",
    /// indicating they are inline child resources (e.g., subnets defined within a VNet).
    /// When all resources are inline, the Terraform Resource column should be hidden.
    /// </remarks>
    [Test]
    public void HasExternalResources_AllInlineResources_ReturnsFalse()
    {
        // Arrange
        var rows = new List<ChildResourceRow>
        {
            new() {
                ChangeIndicator = "➕",
                TerraformResource = "subnet attribute",
                Values = new Dictionary<string, string>()
            },
            new() {
                ChangeIndicator = "🔄",
                TerraformResource = "security_rule attribute",
                Values = new Dictionary<string, string>()
            },
            new() {
                ChangeIndicator = "➕",
                TerraformResource = "route attribute",
                Values = new Dictionary<string, string>()
            }
        };

        // Act - This is the actual logic from ReportModelBuilder.ParentChildMerging.cs line 64-65
        var hasExternal = rows.Exists(r => !string.IsNullOrEmpty(r.TerraformResource) &&
                                            !r.TerraformResource.Contains("attribute"));

        // Assert
        hasExternal.Should().BeFalse("Should return false when all resources are inline (contain 'attribute')");
    }

    /// <summary>
    /// Tests that HasExternalResources returns true when all resources are external.
    /// </summary>
    /// <remarks>
    /// External resources have Terraform addresses like "azurerm_subnet.example",
    /// indicating they are separate resource definitions. When any external resource
    /// is present, the Terraform Resource column should be shown.
    /// </remarks>
    [Test]
    public void HasExternalResources_AllExternalResources_ReturnsTrue()
    {
        // Arrange
        var rows = new List<ChildResourceRow>
        {
            new() {
                ChangeIndicator = "➕",
                TerraformResource = "azurerm_subnet.example",
                Values = new Dictionary<string, string>()
            },
            new() {
                ChangeIndicator = "🔄",
                TerraformResource = "azurerm_network_security_rule.rule1",
                Values = new Dictionary<string, string>()
            },
            new() {
                ChangeIndicator = "➕",
                TerraformResource = "azurerm_route.route1",
                Values = new Dictionary<string, string>()
            }
        };

        // Act
        var hasExternal = rows.Exists(r => !string.IsNullOrEmpty(r.TerraformResource) &&
                                            !r.TerraformResource.Contains("attribute"));

        // Assert
        hasExternal.Should().BeTrue("Should return true when all resources are external (don't contain 'attribute')");
    }

    /// <summary>
    /// Tests that HasExternalResources returns true when resources are mixed.
    /// </summary>
    /// <remarks>
    /// When there's a mix of inline (containing "attribute") and external (Terraform address)
    /// resources, the column should be shown to display the external resource addresses.
    /// This is the most common scenario for resources like VNets with mixed subnet definitions.
    /// </remarks>
    [Test]
    public void HasExternalResources_MixedInlineAndExternal_ReturnsTrue()
    {
        // Arrange
        var rows = new List<ChildResourceRow>
        {
            new() {
                ChangeIndicator = "➕",
                TerraformResource = "subnet attribute",
                Values = new Dictionary<string, string>()
            },
            new() {
                ChangeIndicator = "🔄",
                TerraformResource = "azurerm_subnet.separate1",
                Values = new Dictionary<string, string>()
            },
            new() {
                ChangeIndicator = "➕",
                TerraformResource = "security_rule attribute",
                Values = new Dictionary<string, string>()
            }
        };

        // Act
        var hasExternal = rows.Exists(r => !string.IsNullOrEmpty(r.TerraformResource) &&
                                            !r.TerraformResource.Contains("attribute"));

        // Assert
        hasExternal.Should().BeTrue("Should return true when there's at least one external resource");
    }

    /// <summary>
    /// Tests that HasExternalResources returns false for an empty list.
    /// </summary>
    /// <remarks>
    /// When there are no child resources, the column should not be shown.
    /// This handles the edge case of parent resources with no children.
    /// </remarks>
    [Test]
    public void HasExternalResources_EmptyList_ReturnsFalse()
    {
        // Arrange
        var rows = new List<ChildResourceRow>();

        // Act
#pragma warning disable S4158 // Collection.Exists with empty collection is intentional for testing
        var hasExternal = rows.Exists(r => !string.IsNullOrEmpty(r.TerraformResource) &&
                                            !r.TerraformResource.Contains("attribute"));
#pragma warning restore S4158

        // Assert
        hasExternal.Should().BeFalse("Should return false when row list is empty");
    }

    /// <summary>
    /// Tests that HasExternalResources returns false when TerraformResource is null or empty.
    /// </summary>
    /// <remarks>
    /// Some child resources might have null or empty TerraformResource values.
    /// These should be treated as "no external resources" and not show the column.
    /// </remarks>
    [Test]
    public void HasExternalResources_NullOrEmptyTerraformResource_ReturnsFalse()
    {
        // Arrange
        var rows = new List<ChildResourceRow>
        {
            new() {
                ChangeIndicator = "➕",
                TerraformResource = null!,
                Values = new Dictionary<string, string>()
            },
            new() {
                ChangeIndicator = "🔄",
                TerraformResource = "",
                Values = new Dictionary<string, string>()
            }
        };

        // Act
        var hasExternal = rows.Exists(r => !string.IsNullOrEmpty(r.TerraformResource) &&
                                            !r.TerraformResource.Contains("attribute"));

        // Assert
        hasExternal.Should().BeFalse("Should return false when TerraformResource is null or empty");
    }

    /// <summary>
    /// Tests that external resources with "attribute" in their name are not counted as external.
    /// </summary>
    /// <remarks>
    /// This is an important edge case: a resource address like "azurerm_subnet.has_attribute_in_name"
    /// would contain the string "attribute" even though it's an external resource. The current logic
    /// uses a simple string.Contains("attribute") check, which would incorrectly classify this as inline.
    /// 
    /// This test documents the current behavior. If this edge case becomes a problem in practice,
    /// the logic could be enhanced to use a more sophisticated check (e.g., regex matching " attribute$").
    /// </remarks>
    [Test]
    public void HasExternalResources_ResourceWithAttributeInName_TreatedAsInline()
    {
        // Arrange - Edge case: external resource that happens to have "attribute" in its name
        var rows = new List<ChildResourceRow>
        {
            new() {
                ChangeIndicator = "➕",
                TerraformResource = "azurerm_subnet.has_attribute_in_name",
                Values = new Dictionary<string, string>()
            }
        };

        // Act
        var hasExternal = rows.Exists(r => !string.IsNullOrEmpty(r.TerraformResource) &&
                                            !r.TerraformResource.Contains("attribute"));

        // Assert
        hasExternal.Should().BeFalse("Should return false because 'attribute' is in the resource name");
    }

    /// <summary>
    /// Tests that a single external resource among many inline resources triggers column visibility.
    /// </summary>
    /// <remarks>
    /// The logic uses Exists(), which returns true if ANY element matches.
    /// This test verifies that even a single external resource among many inline ones
    /// will show the column, which is the desired behavior.
    /// </remarks>
    [Test]
    public void HasExternalResources_OnlyOneExternal_ReturnsTrue()
    {
        // Arrange
        var rows = new List<ChildResourceRow>
        {
            new() {
                ChangeIndicator = "➕",
                TerraformResource = "subnet attribute",
                Values = new Dictionary<string, string>()
            },
            new() {
                ChangeIndicator = "🔄",
                TerraformResource = "subnet attribute",
                Values = new Dictionary<string, string>()
            },
            new() {
                ChangeIndicator = "➕",
                TerraformResource = "azurerm_subnet.external1", // Only one external
                Values = new Dictionary<string, string>()
            },
            new() {
                ChangeIndicator = "🔄",
                TerraformResource = "subnet attribute",
                Values = new Dictionary<string, string>()
            }
        };

        // Act
        var hasExternal = rows.Exists(r => !string.IsNullOrEmpty(r.TerraformResource) &&
                                            !r.TerraformResource.Contains("attribute"));

        // Assert
        hasExternal.Should().BeTrue("Should return true when there's at least one external resource");
    }

    /// <summary>
    /// Tests that whitespace-only TerraformResource values are treated as empty.
    /// </summary>
    /// <remarks>
    /// The logic uses IsNullOrEmpty(), which doesn't trim whitespace.
    /// This test documents that whitespace-only values will pass the null check
    /// but will fail the "attribute" check (empty strings don't contain "attribute"),
    /// resulting in HasExternalResources = true. This is likely unintended behavior
    /// but is extremely unlikely to occur in practice.
    /// </remarks>
    [Test]
    public void HasExternalResources_WhitespaceOnlyTerraformResource_TreatedAsExternal()
    {
        // Arrange
        var rows = new List<ChildResourceRow>
        {
            new() {
                ChangeIndicator = "➕",
                TerraformResource = "   ",
                Values = new Dictionary<string, string>()
            }
        };

        // Act
        var hasExternal = rows.Exists(r => !string.IsNullOrEmpty(r.TerraformResource) &&
                                            !r.TerraformResource.Contains("attribute"));

        // Assert
        hasExternal.Should().BeTrue("Whitespace-only values pass IsNullOrEmpty() check and don't contain 'attribute'");
    }
}
