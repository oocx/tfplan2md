using System.Text.RegularExpressions;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Azure;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class RoleAssignmentTemplateTests
{
    private const string Nbsp = "\u00A0";
    private readonly TerraformPlanParser _parser = new();
    private static string Escape(string value) => ScribanHelpers.EscapeMarkdown(value);

    [Test]
    public void Create_RendersSummaryAndDetailsTable()
    {
        var markdown = Render();
        var section = ExtractSection(markdown, "azurerm_role_assignment.create_no_description");

        section.Should().Contain($"<summary>➕{Nbsp}azurerm_role_assignment <b><code>create_no_description</code></b> — ");
        section.Should().Contain("Jane Doe");
        section.Should().Contain("| Attribute | Value |");
        section.Should().Contain($"| scope | `📁{Nbsp}rg-tfplan2md-demo` in subscription `sub-one` |");
        section.Should().Contain($"| role_definition_id | `🛡️{Nbsp}Reader` (`acdd72a7-3385-48ef-bd42-f606fba81ae7`) |");
        section.Should().Contain($"| principal_id | `👤{Nbsp}Jane Doe (User)` [`11111111-1111-1111-1111-111111111111`] |");
    }

    [Test]
    public void Create_WithDescription_RendersDescriptionLine()
    {
        var markdown = Render();
        var section = ExtractSection(markdown, "azurerm_role_assignment.create_with_description");

        section.Should().Contain($"<summary>➕{Nbsp}azurerm_role_assignment <b><code>create_with_description</code></b> — ");
        section.Should().Contain("DevOps Team");
        section.Should().Contain("Allow DevOps team to read logs from the storage account");
        section.Should().Contain($"| scope | Storage Account `🆔{Nbsp}sttfplan2mdlogs-with-extended-name-1234567890` in resource group `📁{Nbsp}rg-tfplan2md-demo` of subscription `sub-one` |");
        section.Should().Contain($"| role_definition_id | `🛡️{Nbsp}Storage Blob Data Reader` (`2a2b9908-6ea1-4ae2-8e65-a410df84e7d1`) |");
        section.Should().Contain($"| principal_id | `👥{Nbsp}DevOps Team (Group)` [`22222222-2222-2222-2222-222222222222`] |");
    }

    [Test]
    public void Update_RendersBeforeAfterTable()
    {
        var markdown = Render();
        var section = ExtractSection(markdown, "azurerm_role_assignment.update_assignment");

        section.Should().Contain($"<summary>🔄{Nbsp}azurerm_role_assignment <b><code>update_assignment</code></b> — ");
        section.Should().Contain("Security Team");
        section.Should().Contain("| Attribute | Before | After |");
        section.Should().Contain($"| scope | Storage Account `🆔{Nbsp}sttfplan2mdlogs` in resource group `📁{Nbsp}rg-tfplan2md-demo` of subscription `sub-one` | Storage Account `🆔{Nbsp}sttfplan2mddata` in resource group `📁{Nbsp}rg-tfplan2md-demo` of subscription `sub-one` |");
        section.Should().Contain($"| role_definition_id | `🛡️{Nbsp}Storage Blob Data Reader` (`2a2b9908-6ea1-4ae2-8e65-a410df84e7d1`) | `🛡️{Nbsp}Storage Blob Data Contributor` (`ba92f5b4-2d11-453d-a403-e96b0029c9fe`) |");
        section.Should().Contain($"| principal_id | `👥{Nbsp}DevOps Team (Group)` [`22222222-2222-2222-2222-222222222222`] | `👥{Nbsp}Security Team (Group)` [`33333333-3333-3333-3333-333333333333`] |");
        section.Should().Contain("| description | `Allow team to read storage data` | `Upgraded permissions for security auditing` |");
    }

    [Test]
    public void Update_IncludesOptionalAttributesOnlyWhenPresent()
    {
        var markdown = Render();
        var section = ExtractSection(markdown, "azurerm_role_assignment.update_assignment");

        section.Should().Contain($"| {Escape("condition")} | - | `{Escape("request.clientip != '10.0.0.0/24'")}` |");
        // Booleans should be lowercase with semantic icons (Terraform convention)
        section.Should().Contain($"| {Escape("skip_service_principal_aad_check")} | `❌{Nbsp}false` | `✅{Nbsp}true` |");
        section.Should().NotContain(Escape("delegated_managed_identity_resource_id"));
    }

    [Test]
    public void Replace_RendersRecreateSummary()
    {
        var markdown = Render();
        var section = ExtractSection(markdown, "azurerm_role_assignment.replace_assignment");

        section.Should().Contain($"<summary>♻️{Nbsp}azurerm_role_assignment <b><code>replace_assignment</code></b> — ");
        section.Should().Contain("Custom Contributor Long Name 1234567890");
        section.Should().Contain("| Attribute | Before | After |");
        section.Should().Contain($"| role_definition_id | `🛡️{Nbsp}Reader` (`acdd72a7-3385-48ef-bd42-f606fba81ae7`) | `🛡️{Nbsp}Custom Contributor Long Name 1234567890` |");
    }

    [Test]
    public void Delete_RendersRemoveSummary()
    {
        var markdown = Render();
        var section = ExtractSection(markdown, "azurerm_role_assignment.delete_assignment");

        section.Should().Contain($"<summary>❌{Nbsp}azurerm_role_assignment <b><code>delete_assignment</code></b> — ");
        section.Should().Contain("Contributor");
        section.Should().Contain("| Attribute | Value |");
        section.Should().Contain($"| principal_id | `👤{Nbsp}John Doe (User)` [`33333333-3333-3333-3333-333333333333`] |");
    }

    [Test]
    public void UnmappedPrincipal_FallsBackToIdentifier()
    {
        var markdown = Render(new UnmappedPrincipalMapper());
        var section = ExtractSection(markdown, "azurerm_role_assignment.unmapped_principal");

        section.Should().Contain($"<summary>➕{Nbsp}azurerm_role_assignment <b><code>unmapped_principal</code></b> — ");
        section.Should().Contain("Extremely Verbose Custom Role Name For Long Output Validation 1234567890");
        section.Should().Contain("Extremely Verbose Custom Role Name For Long Output Validation 1234567890");
    }

    private string Render(IPrincipalMapper? mapper = null)
    {
        var principalMapper = mapper ?? new StubPrincipalMapper();
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.RoleAssignmentsPlanPath));
        var builder = new ReportModelBuilder(principalMapper: principalMapper);
        var model = builder.Build(plan);
        var renderer = new MarkdownRenderer(principalMapper);
        return renderer.Render(model);
    }

    /// <summary>
    /// Extracts a resource section from markdown based on the resource address.
    /// </summary>
    /// <param name="markdown">The full markdown document.</param>
    /// <param name="address">The terraform resource address (e.g., "azurerm_role_assignment.create_no_description").</param>
    /// <returns>The content of the resource section.</returns>
    private static string ExtractSection(string markdown, string address)
    {
        // Parse address to get resource type and name
        var parts = address.Split('.');
        var resourceType = parts[0];
        var resourceName = parts.Length > 1 ? parts[1] : parts[0];

        // Look for a <details> block containing the resource name in <b><code>{name}</code></b>
        var pattern = $@"(?s)<details[^>]*>\s*<summary>[^<]*{Regex.Escape(resourceType)}\s+<b><code>{Regex.Escape(resourceName)}</code></b>(.*?)</details>";

        var match = Regex.Match(markdown, pattern, RegexOptions.Singleline);
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
