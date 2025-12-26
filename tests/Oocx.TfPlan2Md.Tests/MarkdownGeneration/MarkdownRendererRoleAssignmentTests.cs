using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Azure;
using Oocx.TfPlan2Md.MarkdownGeneration;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class MarkdownRendererRoleAssignmentTests
{
    private static string Escape(string value) => ScribanHelpers.EscapeMarkdown(value);

    [Fact]
    public void Render_RoleAssignment_UsesEnhancedTemplate()
    {
        // Arrange
        var change = CreateRoleAssignmentChange("principal-id", "User");
        var renderer = new MarkdownRenderer(principalMapper: new StubPrincipalMapper("principal-id", "John Doe"));

        // Act
        var result = renderer.RenderResourceChange(change);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("**Summary:** `John Doe` (User) → `Reader` on `my-rg`");
        // Table is now wrapped in <details> for collapsible display
        result.Should().Contain("<details>");
        result.Should().Contain("| Attribute | Value |");
        result.Should().Contain("`Reader` (`acdd72a7-3385-48ef-bd42-f606fba81ae7`)");
        result.Should().Contain("`my-rg` in subscription `sub-id`");
        result.Should().Contain("`John Doe` (User) [`principal-id`]");
    }

    [Fact]
    public void Render_RoleAssignment_WithoutPrincipalMapping_FallsBackToGuid()
    {
        // Arrange
        var change = CreateRoleAssignmentChange("missing-principal", "Group");
        var renderer = new MarkdownRenderer(principalMapper: new StubPrincipalMapper("principal-id", "John Doe"));

        // Act
        var result = renderer.RenderResourceChange(change);

        // Assert
        result.Should().NotBeNull();
        result!.Should().Contain("`missing-principal`");
    }

    private static ResourceChangeModel CreateRoleAssignmentChange(string principalId, string principalType)
    {
        var json = $"{{\n  \"scope\": \"/subscriptions/sub-id/resourceGroups/my-rg\",\n  \"role_definition_id\": \"/subscriptions/sub-id/providers/Microsoft.Authorization/roleDefinitions/acdd72a7-3385-48ef-bd42-f606fba81ae7\",\n  \"principal_id\": \"{principalId}\",\n  \"principal_type\": \"{principalType}\"\n}}";
        var afterJson = JsonDocument.Parse(json).RootElement;

        return new ResourceChangeModel
        {
            Address = "azurerm_role_assignment.example",
            ModuleAddress = string.Empty,
            Type = "azurerm_role_assignment",
            Name = "example",
            ProviderName = "registry.terraform.io/hashicorp/azurerm",
            Action = "create",
            ActionSymbol = "➕",
            AttributeChanges = Array.Empty<AttributeChangeModel>(),
            BeforeJson = null,
            AfterJson = afterJson
        };
    }

    private sealed class StubPrincipalMapper(string id, string name) : IPrincipalMapper
    {
        public string GetPrincipalName(string principalId)
        {
            return principalId == id
                ? $"{name} [{principalId}]"
                : principalId;
        }

        public string? GetName(string principalId)
        {
            return principalId == id ? name : null;
        }
    }
}
