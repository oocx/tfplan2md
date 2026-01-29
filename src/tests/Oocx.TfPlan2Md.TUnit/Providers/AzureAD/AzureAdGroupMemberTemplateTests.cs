using System.Text.RegularExpressions;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzureAD;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class AzureAdGroupMemberTemplateTests
{
    private const string Nbsp = "\u00A0";
    private readonly TerraformPlanParser _parser = new();

    [Test]
    public void Create_RendersGroupToMemberSummaryWithIcons()
    {
        var markdown = Render();
        var section = ExtractSection(markdown, "azuread_group_member.group_to_user");

        section.Should().Contain($"<summary>➕{Nbsp}azuread_group_member <b><code>group_to_user</code></b> — <code>👥{Nbsp}DevOps Team</code> (<code>group-001</code>) → <code>👤{Nbsp}Jane Doe</code> (<code>user-001</code>)</summary>");
    }

    [Test]
    public void Create_WithMissingMemberId_StopsAtGroup()
    {
        var markdown = Render();
        var section = ExtractSection(markdown, "azuread_group_member.group_missing_member");

        section.Should().Contain($"<summary>➕{Nbsp}azuread_group_member <b><code>group_missing_member</code></b> — <code>👥{Nbsp}Platform Team</code> (<code>group-002</code>)</summary>");
        section.Should().NotContain("→");
    }

    private string Render()
    {
        var principalMapper = new PrincipalMapper(DemoPaths.AzureAdPrincipalMappingPath);
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.AzureAdGroupMemberPlanPath));

        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureADModule());

        var builder = new ReportModelBuilder(
            principalMapper: principalMapper,
            providerRegistry: providerRegistry);
        var model = builder.Build(plan);
        var renderer = new MarkdownRenderer(
            principalMapper: principalMapper,
            providerRegistry: providerRegistry);
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
        var parts = address.Split('.');
        var resourceType = parts[0];
        var resourceName = parts.Length > 1 ? parts[1] : parts[0];

        var pattern = $@"(?s)<details[^>]*>\s*<summary>[^<]*{Regex.Escape(resourceType)}\s+<b><code>{Regex.Escape(resourceName)}</code></b>(.*?)</details>";

        var match = Regex.Match(markdown, pattern, RegexOptions.Singleline, TimeSpan.FromSeconds(2));
        return match.Success ? match.Value : string.Empty;
    }
}
