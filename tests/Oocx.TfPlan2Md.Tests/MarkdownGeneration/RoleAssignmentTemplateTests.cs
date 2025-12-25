using System.Text.RegularExpressions;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Azure;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Tests.TestData;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class RoleAssignmentTemplateTests
{
    private readonly TerraformPlanParser _parser = new();
    private readonly ReportModelBuilder _builder = new();
    private static string Escape(string value) => ScribanHelpers.EscapeMarkdown(value);

    [Fact]
    public void Create_RendersSummaryAndDetailsTable()
    {
        var markdown = Render();
        var section = ExtractSection(markdown, "azurerm_role_assignment.create_no_description");

        section.Should().Contain("**Summary:** `Jane Doe` (User) → `Reader` on `rg-tfplan2md-demo`");
        section.Should().Contain("<details>");
        section.Should().Contain("| Attribute | Value |");
        section.Should().Contain("| scope | `rg-tfplan2md-demo` in subscription `sub-one` |");
        section.Should().Contain("| role_definition_id | `Reader` (`acdd72a7-3385-48ef-bd42-f606fba81ae7`) |");
        section.Should().Contain("| principal_id | `Jane Doe` (User) [`11111111-1111-1111-1111-111111111111`] |");
    }

    [Fact]
    public void Create_WithDescription_RendersDescriptionLine()
    {
        var markdown = Render();
        var section = ExtractSection(markdown, "azurerm_role_assignment.create_with_description");

        section.Should().Contain("**Summary:** `DevOps Team` (Group) → `Storage Blob Data Reader` on Storage Account `sttfplan2mdlogs-with-extended-name-1234567890`");
        section.Should().Contain("Allow DevOps team to read logs from the storage account");
        section.Should().Contain("| scope | `rg-tfplan2md-demo` in subscription `sub-one` |");
        section.Should().Contain("| role_definition_id | `Storage Blob Data Reader` (`2a2b9908-6ea1-4ae2-8e65-a410df84e7d1`) |");
        section.Should().Contain("| principal_id | `DevOps Team` (Group) [`22222222-2222-2222-2222-222222222222`] |");
    }

    [Fact]
    public void Update_RendersBeforeAfterTable()
    {
        var markdown = Render();
        var section = ExtractSection(markdown, "azurerm_role_assignment.update_assignment");

        section.Should().Contain("**Summary:** `Security Team` (Group) → `Storage Blob Data Contributor` on Storage Account `sttfplan2mddata`");
        section.Should().Contain("| Attribute | Before | After |");
        section.Should().Contain("| scope | `rg-tfplan2md-demo` in subscription `sub-one` | `rg-tfplan2md-demo` in subscription `sub-one` |");
        section.Should().Contain("| role_definition_id | `Storage Blob Data Reader` (`2a2b9908-6ea1-4ae2-8e65-a410df84e7d1`) | `Storage Blob Data Contributor` (`ba92f5b4-2d11-453d-a403-e96b0029c9fe`) |");
        section.Should().Contain("| principal_id | `DevOps Team` (Group) [`22222222-2222-2222-2222-222222222222`] | `Security Team` (Group) [`33333333-3333-3333-3333-333333333333`] |");
        section.Should().Contain("| description | `Allow team to read storage data` | `Upgraded permissions for security auditing` |");
    }

    [Fact]
    public void Update_IncludesOptionalAttributesOnlyWhenPresent()
    {
        var markdown = Render();
        var section = ExtractSection(markdown, "azurerm_role_assignment.update_assignment");

        section.Should().Contain($"| {Escape("condition")} | - | `{Escape("request.clientip != '10.0.0.0/24'")}` |");
        section.Should().Contain($"| {Escape("skip_service_principal_aad_check")} | `{Escape("false")}` | `{Escape("true")}` |");
        section.Should().NotContain(Escape("delegated_managed_identity_resource_id"));
    }

    [Fact]
    public void Replace_RendersRecreateSummary()
    {
        var markdown = Render();
        var section = ExtractSection(markdown, "azurerm_role_assignment.replace_assignment");

        section.Should().Contain("**Summary:** recreate as `Security Team` (Group) → `Custom Contributor Long Name 1234567890` on `rg-production`");
        section.Should().Contain("| Attribute | Before | After |");
        section.Should().Contain("| role_definition_id | `Reader` (`acdd72a7-3385-48ef-bd42-f606fba81ae7`) | `Custom Contributor Long Name 1234567890` |");
    }

    [Fact]
    public void Delete_RendersRemoveSummary()
    {
        var markdown = Render();
        var section = ExtractSection(markdown, "azurerm_role_assignment.delete_assignment");

        section.Should().Contain("**Summary:** remove `Contributor` on `rg-legacy` from User `John Doe`");
        section.Should().Contain("| Attribute | Value |");
        section.Should().Contain("| principal_id | `John Doe` (User) [`33333333-3333-3333-3333-333333333333`] |");
    }

    [Fact]
    public void UnmappedPrincipal_FallsBackToIdentifier()
    {
        var markdown = Render(new UnmappedPrincipalMapper());
        var section = ExtractSection(markdown, "azurerm_role_assignment.unmapped_principal");

        section.Should().Contain("**Summary:** `99999999-9999-9999-9999-999999999999` (ServicePrincipal) → `Extremely Verbose Custom Role Name For Long Output Validation 1234567890` on `rg-long-names-example`");
        section.Should().Contain("Extremely Verbose Custom Role Name For Long Output Validation 1234567890");
    }

    private string Render(IPrincipalMapper? mapper = null)
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.RoleAssignmentsPlanPath));
        var model = _builder.Build(plan);
        var renderer = new MarkdownRenderer(mapper ?? new StubPrincipalMapper());
        return renderer.Render(model);
    }

    private static string ExtractSection(string markdown, string address)
    {
        var escapedAddress = Escape(address);
        var pattern = $@"(?ms)^#### .*?{Regex.Escape(escapedAddress)}.*?(?=^#### |\z)";
        var match = Regex.Match(markdown, pattern);
        return match.Success ? match.Value : string.Empty;
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

        public string GetPrincipalName(string principalId, string? principalType)
        {
            var name = GetName(principalId, principalType);
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

    private sealed class UnmappedPrincipalMapper : IPrincipalMapper
    {
        public string GetPrincipalName(string principalId)
        {
            return principalId;
        }

        public string? GetName(string principalId)
        {
            return null;
        }
    }
}
