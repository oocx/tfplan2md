using System.Text.RegularExpressions;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
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

        section.Should().Contain($"<summary>{ActionIcons.Add}{Nbsp}azuread_group_member <b><code>group_to_user</code></b> — <code>👥{Nbsp}DevOps Team</code> (<code>group-001</code>) → <code>👤{Nbsp}Jane Doe</code> (<code>user-001</code>)</summary>");
    }

    [Test]
    public void Create_WithMissingMemberId_StopsAtGroup()
    {
        var markdown = Render();
        var section = ExtractSection(markdown, "azuread_group_member.group_missing_member");

        section.Should().Contain($"<summary>{ActionIcons.Add}{Nbsp}azuread_group_member <b><code>group_missing_member</code></b> — <code>👥{Nbsp}Platform Team</code> (<code>group-002</code>)</summary>");
        section.Should().NotContain("→");
    }

    /// <summary>
    /// Verifies that when both group_object_id and member_object_id are unknown at plan time,
    /// the summary shows "(known after apply)" for both IDs.
    /// Related issue: docs/issues/575-azuread-group-member-empty-summary/analysis.md.
    /// </summary>
    [Test]
    public void Create_WithAllUnknownAttributes_ShowsKnownAfterApplySummary()
    {
        var markdown = RenderAllUnknown();
        var section = ExtractSection(markdown, "azuread_group_member.all_unknown");

        section.Should().Contain("(known after apply)");
        section.Should().Contain("→");
    }

    /// <summary>
    /// Verifies that when both group_object_id and member_object_id are unknown at plan time,
    /// the attributes table includes both attributes with "(known after apply)" values.
    /// Related issue: docs/issues/575-azuread-group-member-empty-summary/analysis.md.
    /// </summary>
    [Test]
    public void Create_WithAllUnknownAttributes_ShowsAttributeTable()
    {
        var markdown = RenderAllUnknown();
        var section = ExtractSection(markdown, "azuread_group_member.all_unknown");

        section.Should().Contain("group_object_id");
        section.Should().Contain("member_object_id");
        section.Should().NotContain("_No attribute changes._");
    }

    private string Render()
    {
        var mappingResult = AzureMappingFileLoader.Load(DemoPaths.AzureAdPrincipalMappingPath, diagnosticContext: null);
        var principalMapper = new PrincipalMapper(mappingResult.Principals, mappingResult.PrincipalTypes);
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
    /// Renders the all-unknown group member plan (both IDs are computed at plan time).
    /// Related issue: docs/issues/575-azuread-group-member-empty-summary/analysis.md.
    /// </summary>
    /// <returns>The rendered markdown output.</returns>
    private string RenderAllUnknown()
    {
        var mappingResult = AzureMappingFileLoader.Load(DemoPaths.AzureAdPrincipalMappingPath, diagnosticContext: null);
        var principalMapper = new PrincipalMapper(mappingResult.Principals, mappingResult.PrincipalTypes);
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.AzureAdGroupMemberAllUnknownPlanPath));

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
    /// Verifies that when both IDs are computed but the plan contains a configuration block with
    /// static resource references, the summary shows those resource names instead of "(known after apply)".
    /// Related issue: docs/issues/575-azuread-group-member-empty-summary/analysis.md.
    /// </summary>
    [Test]
    public void Create_WithStaticConfigurationReferences_ShowsResourceNames()
    {
        var markdown = RenderStaticRef();
        var section = ExtractSection(markdown, "azuread_group_member.platform_admin_member");

        section.Should().Contain("azuread_group.platform_engineers");
        section.Should().Contain("azuread_user.admin");
        section.Should().Contain("→");
        // The summary line shows resource references; the attribute table correctly shows
        // "(known after apply)" for the actual null values. Verify the summary specifically:
        section.Should().Contain("<code>azuread_group.platform_engineers</code> → <code>azuread_user.admin</code>");
    }

    /// <summary>
    /// Verifies that when both IDs are computed and only dynamic (each.value) references exist,
    /// the summary shows the for_each instance key as context instead of "(known after apply)".
    /// The instance key often contains meaningful information (e.g., group name and user email).
    /// Related issue: docs/issues/575-azuread-group-member-empty-summary/analysis.md.
    /// </summary>
    [Test]
    public void Create_WithForEachStringKey_ShowsInstanceKeyAsContext()
    {
        var markdown = RenderForEachUnknown();

        // The instance key "team-example - user@example.de" should appear in the output
        // as context for both the group and member references
        markdown.Should().Contain("team-example - user@example.de");
        markdown.Should().NotContain("(known after apply) →");
    }

    private string RenderStaticRef()
    {
        var mappingResult = AzureMappingFileLoader.Load(DemoPaths.AzureAdPrincipalMappingPath, diagnosticContext: null);
        var principalMapper = new PrincipalMapper(mappingResult.Principals, mappingResult.PrincipalTypes);
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.AzureAdGroupMemberStaticRefPlanPath));

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

    private string RenderForEachUnknown()
    {
        var mappingResult = AzureMappingFileLoader.Load(DemoPaths.AzureAdPrincipalMappingPath, diagnosticContext: null);
        var principalMapper = new PrincipalMapper(mappingResult.Principals, mappingResult.PrincipalTypes);
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.AzureAdGroupMemberForEachUnknownPlanPath));

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
