using AwesomeAssertions;
using Oocx.TfPlan2Md.Azure;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for role assignment rendering through MarkdownRenderer.
/// These tests use the full pipeline (parser -> builder -> renderer) to verify behavior.
/// </summary>
public class MarkdownRendererRoleAssignmentTests
{
    private const string Nbsp = "\u00A0";
    private readonly TerraformPlanParser _parser = new();

    [Test]
    public void Render_RoleAssignment_UsesEnhancedTemplate()
    {
        // Arrange
        var principalMapper = new StubPrincipalMapper();
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.RoleAssignmentsPlanPath));
        var builder = new ReportModelBuilder(principalMapper: principalMapper);
        var model = builder.Build(plan);
        var renderer = new MarkdownRenderer(principalMapper: principalMapper);

        // Act
        var result = renderer.Render(model);

        // Assert - verify the create_no_description assignment
        result.Should().Contain("<summary>");
        result.Should().Contain("azurerm_role_assignment <b><code>create_no_description</code></b>");
        result.Should().Contain("Jane Doe");
        result.Should().Contain("Reader");
        result.Should().Contain("rg-tfplan2md-demo");
        result.Should().Contain("| Attribute | Value |");
        result.Should().Contain($"`🛡️{Nbsp}Reader` (`acdd72a7-3385-48ef-bd42-f606fba81ae7`)");
        result.Should().Contain($"`📁{Nbsp}rg-tfplan2md-demo` in subscription `sub-one`");
        result.Should().Contain($"`👤{Nbsp}Jane Doe (User)` [`11111111-1111-1111-1111-111111111111`]");
    }

    [Test]
    public void Render_RoleAssignment_WithoutPrincipalMapping_FallsBackToGuid()
    {
        // Arrange - use a mapper that doesn't resolve any principals
        var principalMapper = new NullPrincipalMapper();
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.RoleAssignmentsPlanPath));
        var builder = new ReportModelBuilder(principalMapper: principalMapper);
        var model = builder.Build(plan);
        var renderer = new MarkdownRenderer(principalMapper: principalMapper);

        // Act
        var result = renderer.Render(model);

        // Assert - verify the principal ID is shown as-is when not mapped
        result.Should().Contain("`11111111-1111-1111-1111-111111111111`");
    }

    private sealed class StubPrincipalMapper : IPrincipalMapper
    {
        private static readonly Dictionary<(string Id, string? Type), string> TypedNames = new()
        {
            [("11111111-1111-1111-1111-111111111111", "User")] = "Jane Doe",
            [("22222222-2222-2222-2222-222222222222", "Group")] = "DevOps Team",
            [("33333333-3333-3333-3333-333333333333", "Group")] = "Security Team",
            [("33333333-3333-3333-3333-333333333333", "User")] = "John Doe"
        };

        private static readonly Dictionary<string, string> Names = new()
        {
            ["11111111-1111-1111-1111-111111111111"] = "Jane Doe",
            ["22222222-2222-2222-2222-222222222222"] = "DevOps Team",
            ["33333333-3333-3333-3333-333333333333"] = "Security Team"
        };

        public string GetPrincipalName(string principalId)
        {
            var name = GetName(principalId);
            return name is null ? principalId : $"{name} [{principalId}]";
        }

        public string? GetName(string principalId)
        {
            return Names.TryGetValue(principalId, out var name) ? name : null;
        }

        public string? GetName(string principalId, string? principalType)
        {
            if (principalType != null && TypedNames.TryGetValue((principalId, principalType), out var typedName))
            {
                return typedName;
            }

            return GetName(principalId);
        }
    }
}
