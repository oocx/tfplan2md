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

/// <summary>
/// Tests for azuread_app_role_assignment, azuread_directory_role_assignment,
/// and azuread_service_principal_delegated_permission_grant rendering.
/// </summary>
public class AzureAdAppRoleAssignmentTests
{
    private const string Arrow = "\u2192";
    private readonly TerraformPlanParser _parser = new();

    [Test]
    public void AppRoleAssignment_WithKnownGraphPermission_ResolvesSummary()
    {
        var markdown = Render();
        var section = ExtractSection(markdown, "azuread_app_role_assignment.graph_user_read_all");

        section.Should().Contain("azuread_app_role_assignment");
        section.Should().Contain("graph_user_read_all");
        section.Should().Contain("User.Read.All");
        section.Should().Contain("df021288-bdef-4463-88db-98f22de89214");
    }

    [Test]
    public void AppRoleAssignment_WithUnknownRole_ShowsRawGuid()
    {
        var markdown = Render();
        var section = ExtractSection(markdown, "azuread_app_role_assignment.custom_role");

        section.Should().Contain("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        section.Should().NotContain("User.Read.All");
    }

    [Test]
    public void AppRoleAssignment_WithPrincipalMapping_ResolvesPrincipalName()
    {
        var markdown = RenderWithPrincipalMapping();
        var section = ExtractSection(markdown, "azuread_app_role_assignment.graph_user_read_all");

        section.Should().Contain("Test Service Principal");
        section.Should().Contain("11111111-1111-1111-1111-111111111111");
    }

    [Test]
    public void AppRoleAssignment_Delete_ShowsBeforeState()
    {
        var markdown = Render();
        var section = ExtractSection(markdown, "azuread_app_role_assignment.delete_assignment");

        section.Should().Contain("Application.Read.All");
        section.Should().Contain("9a5d68dd-52b0-4cc2-bd40-abcf44ac3a30");
    }

    [Test]
    public void AppRoleAssignment_SummaryContainsArrows()
    {
        var markdown = Render();
        var section = ExtractSection(markdown, "azuread_app_role_assignment.graph_user_read_all");

        section.Should().Contain(Arrow);
    }

    [Test]
    public void DirectoryRoleAssignment_RendersSummaryWithPrincipalAndRole()
    {
        var markdown = Render();
        var section = ExtractSection(markdown, "azuread_directory_role_assignment.admin_role");

        section.Should().Contain("azuread_directory_role_assignment");
        section.Should().Contain("admin_role");
        section.Should().Contain("55555555-5555-5555-5555-555555555555");
        section.Should().Contain("62e90394-69f5-4237-9190-012177145e10");
    }

    [Test]
    public void DelegatedPermissionGrant_RendersSummaryWithClaimValues()
    {
        var markdown = Render();
        var section = ExtractSection(markdown, "azuread_service_principal_delegated_permission_grant.oauth2_grant");

        section.Should().Contain("azuread_service_principal_delegated_permission_grant");
        section.Should().Contain("openid, profile, email");
    }

    [Test]
    public void AppRoleAssignment_AttributeTable_ContainsRoleIdWithIcon()
    {
        var markdown = Render();
        var section = ExtractSection(markdown, "azuread_app_role_assignment.graph_user_read_all");

        section.Should().Contain("app_role_id");
        section.Should().Contain("🔑");
        section.Should().Contain("User.Read.All");
    }

    private string Render()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.AzureAdAppRoleAssignmentPlanPath));

        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureADModule());

        var builder = new ReportModelBuilder(
            services: new ReportModelBuilderServices(ProviderRegistry: providerRegistry));
        var model = builder.Build(plan);
        var renderer = new MarkdownRenderer(
            providerRegistry: providerRegistry);
        return renderer.Render(model);
    }

    private string RenderWithPrincipalMapping()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.AzureAdAppRoleAssignmentPlanPath));

        var principalMapper = new PrincipalMapper(
            new Dictionary<string, string>
            {
                ["11111111-1111-1111-1111-111111111111"] = "Test Service Principal",
                ["22222222-2222-2222-2222-222222222222"] = "Microsoft Graph"
            },
            new Dictionary<string, string>
            {
                ["11111111-1111-1111-1111-111111111111"] = "ServicePrincipal",
                ["22222222-2222-2222-2222-222222222222"] = "ServicePrincipal"
            },
            diagnosticContext: null);

        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureADModule(principalMapper: principalMapper));

        var builder = new ReportModelBuilder(
            services: new ReportModelBuilderServices(
                ProviderRegistry: providerRegistry,
                PrincipalMapper: principalMapper));
        var model = builder.Build(plan);
        var renderer = new MarkdownRenderer(
            providerRegistry: providerRegistry);
        return renderer.Render(model);
    }

    /// <summary>
    /// Extracts a resource section from markdown based on the resource address.
    /// </summary>
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
