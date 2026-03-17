using System.Collections.Generic;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureAD.Models;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzureAD;

/// <summary>
/// Tests for Azure AD summary builders covering app role, directory role,
/// and delegated permission grant resource types.
/// Related feature: docs/features/116-azuread-app-role-assignment/specification.md.
/// </summary>
public class AzureAdAppRoleAssignmentTests
{
    /// <summary>
    /// Known Microsoft Graph app role GUID for User.Read.All.
    /// </summary>
    private const string UserReadAllGuid = "df021288-bdef-4463-88db-98f22de89214";

    /// <summary>
    /// Sample principal object ID used across tests.
    /// </summary>
    private const string PrincipalGuid = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

    /// <summary>
    /// Sample resource object ID used across tests.
    /// </summary>
    private const string ResourceGuid = "11111111-2222-3333-4444-555555555555";

    /// <summary>
    /// Sample service principal object ID used for delegated permission grant tests.
    /// </summary>
    private const string ServicePrincipalGuid = "22222222-3333-4444-5555-666666666666";

    // ──────────────────────────────────────────────
    // App Role Assignment tests (TC-09 through TC-15)
    // ──────────────────────────────────────────────

    /// <summary>
    /// TC-09: Verifies full summary when all GUIDs are resolved.
    /// </summary>
    [Test]
    public void AppRoleAssignment_AllMapped_ReturnsFullSummary()
    {
        var state = ParseState($$"""
        {
            "app_role_id": "{{UserReadAllGuid}}",
            "principal_object_id": "{{PrincipalGuid}}",
            "resource_object_id": "{{ResourceGuid}}"
        }
        """);

        var mapper = CreateMapper(
            (PrincipalGuid, "My Service Principal"),
            (ResourceGuid, "Microsoft Graph"));

        var summary = BuildSummary("azuread_app_role_assignment", "example", state, mapper,
            appRoleResolver: MicrosoftGraphAppRoleResolver.CreateBuiltIn());

        summary.Should().Contain("User.Read.All");
        summary.Should().Contain("My Service Principal");
        summary.Should().Contain("Microsoft Graph");
        summary.Should().StartWith("➕");
    }

    /// <summary>
    /// TC-10: Verifies summary shows raw GUIDs when no mappings are available.
    /// </summary>
    [Test]
    public void AppRoleAssignment_NoMappings_DisplaysRawGuids()
    {
        var unknownRoleGuid = "99999999-9999-9999-9999-999999999999";
        var state = ParseState($$"""
        {
            "app_role_id": "{{unknownRoleGuid}}",
            "principal_object_id": "{{PrincipalGuid}}",
            "resource_object_id": "{{ResourceGuid}}"
        }
        """);

        var summary = BuildSummary("azuread_app_role_assignment", "example", state, new NullPrincipalMapper(),
            appRoleResolver: MicrosoftGraphAppRoleResolver.CreateBuiltIn());

        summary.Should().Contain(unknownRoleGuid);
        summary.Should().Contain(PrincipalGuid);
        summary.Should().Contain(ResourceGuid);
    }

    /// <summary>
    /// TC-12: Verifies partial mapping — role resolved but principal/resource raw.
    /// </summary>
    [Test]
    public void AppRoleAssignment_PartialMapping_MixesResolvedAndRaw()
    {
        var state = ParseState($$"""
        {
            "app_role_id": "{{UserReadAllGuid}}",
            "principal_object_id": "{{PrincipalGuid}}",
            "resource_object_id": "{{ResourceGuid}}"
        }
        """);

        var summary = BuildSummary("azuread_app_role_assignment", "example", state, new NullPrincipalMapper(),
            appRoleResolver: MicrosoftGraphAppRoleResolver.CreateBuiltIn());

        summary.Should().Contain("User.Read.All");
        summary.Should().Contain(PrincipalGuid);
        summary.Should().Contain(ResourceGuid);
    }

    /// <summary>
    /// TC-14: Verifies computed attribute fallbacks from Terraform state.
    /// </summary>
    [Test]
    public void AppRoleAssignment_ComputedFallbacks_UsesStateValues()
    {
        var state = ParseState($$"""
        {
            "app_role_id": "{{UserReadAllGuid}}",
            "principal_object_id": "{{PrincipalGuid}}",
            "resource_object_id": "{{ResourceGuid}}",
            "principal_display_name": "My SP",
            "resource_display_name": "Microsoft Graph"
        }
        """);

        var summary = BuildSummary("azuread_app_role_assignment", "example", state, new NullPrincipalMapper(),
            appRoleResolver: MicrosoftGraphAppRoleResolver.CreateBuiltIn());

        summary.Should().Contain("My SP");
        summary.Should().Contain("Microsoft Graph");
    }

    /// <summary>
    /// TC-15: Verifies graceful handling when attributes are missing.
    /// </summary>
    [Test]
    public void AppRoleAssignment_MissingAttributes_HandlesGracefully()
    {
        var state = ParseState("{}");

        var act = () => BuildSummary("azuread_app_role_assignment", "example", state, new NullPrincipalMapper(),
            appRoleResolver: MicrosoftGraphAppRoleResolver.CreateBuiltIn());

        act.Should().NotThrow();
    }

    // ──────────────────────────────────────────────
    // Directory Role Assignment tests (TC-18, TC-19)
    // ──────────────────────────────────────────────

    /// <summary>
    /// TC-18: Verifies directory role assignment summary with a mapped principal.
    /// </summary>
    [Test]
    public void DirectoryRoleAssignment_MappedPrincipal_ShowsPrincipalAndRole()
    {
        var roleTemplateId = "fdd7a751-b60b-444a-984c-02652fe8fa1c";
        var state = ParseState($$"""
        {
            "principal_object_id": "{{PrincipalGuid}}",
            "role_definition_id": "{{roleTemplateId}}"
        }
        """);

        var mapper = CreateMapper((PrincipalGuid, "My Service Principal"));

        var summary = BuildSummary("azuread_directory_role_assignment", "example", state, mapper);

        summary.Should().Contain("My Service Principal");
        summary.Should().Contain(roleTemplateId);
        summary.Should().Contain("\u2192"); // arrow separator
    }

    /// <summary>
    /// TC-19: Verifies directory role assignment summary with raw GUIDs.
    /// </summary>
    [Test]
    public void DirectoryRoleAssignment_NoMappings_ShowsRawGuids()
    {
        var roleTemplateId = "fdd7a751-b60b-444a-984c-02652fe8fa1c";
        var state = ParseState($$"""
        {
            "principal_object_id": "{{PrincipalGuid}}",
            "role_definition_id": "{{roleTemplateId}}"
        }
        """);

        var summary = BuildSummary("azuread_directory_role_assignment", "example", state, new NullPrincipalMapper());

        summary.Should().Contain(PrincipalGuid);
        summary.Should().Contain(roleTemplateId);
    }

    // ──────────────────────────────────────────────
    // Delegated Permission Grant tests (TC-20, TC-21, TC-22)
    // ──────────────────────────────────────────────

    /// <summary>
    /// TC-20: Verifies delegated permission grant summary with claims and mapped principals.
    /// </summary>
    [Test]
    public void DelegatedPermissionGrant_WithClaimsAndMappedPrincipals_ShowsFullSummary()
    {
        var state = ParseState($$"""
        {
            "service_principal_object_id": "{{ServicePrincipalGuid}}",
            "resource_object_id": "{{ResourceGuid}}",
            "claim_values": ["User.Read", "openid"]
        }
        """);

        var mapper = CreateMapper(
            (ServicePrincipalGuid, "My App"),
            (ResourceGuid, "Microsoft Graph"));

        var summary = BuildSummary("azuread_service_principal_delegated_permission_grant", "example", state, mapper);

        summary.Should().Contain("My App");
        summary.Should().Contain("User.Read, openid");
        summary.Should().Contain("Microsoft Graph");
    }

    /// <summary>
    /// TC-21: Verifies delegated permission grant summary with no claims.
    /// </summary>
    [Test]
    public void DelegatedPermissionGrant_NoClaims_ShowsNoClaimsPlaceholder()
    {
        var state = ParseState($$"""
        {
            "service_principal_object_id": "{{ServicePrincipalGuid}}",
            "resource_object_id": "{{ResourceGuid}}",
            "claim_values": []
        }
        """);

        var summary = BuildSummary("azuread_service_principal_delegated_permission_grant", "example", state, new NullPrincipalMapper());

        summary.Should().Contain("(no claims)");
    }

    /// <summary>
    /// TC-22: Verifies delegated permission grant summary with raw GUIDs.
    /// </summary>
    [Test]
    public void DelegatedPermissionGrant_RawGuids_ShowsUnmappedValues()
    {
        var state = ParseState($$"""
        {
            "service_principal_object_id": "{{ServicePrincipalGuid}}",
            "resource_object_id": "{{ResourceGuid}}",
            "claim_values": ["User.Read"]
        }
        """);

        var summary = BuildSummary("azuread_service_principal_delegated_permission_grant", "example", state, new NullPrincipalMapper());

        summary.Should().Contain(ServicePrincipalGuid);
        summary.Should().Contain(ResourceGuid);
        summary.Should().Contain("User.Read");
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    /// <summary>
    /// Parses a JSON string into a JsonElement for use as Terraform state.
    /// </summary>
    /// <param name="json">Raw JSON string representing the state.</param>
    /// <returns>The parsed JsonElement.</returns>
    private static JsonElement ParseState(string json)
    {
        return JsonDocument.Parse(json).RootElement;
    }

    /// <summary>
    /// Creates a PrincipalMapper with the specified mappings.
    /// </summary>
    /// <param name="mappings">Tuples of (Id, Name).</param>
    /// <returns>A configured PrincipalMapper.</returns>
    private static PrincipalMapper CreateMapper(params (string Id, string Name)[] mappings)
    {
        var principals = new Dictionary<string, string>();
        var types = new Dictionary<string, string>();
        foreach (var (Id, Name) in mappings)
        {
            principals[Id] = Name;
        }

        return new PrincipalMapper(principals, types);
    }

    /// <summary>
    /// Builds a summary via AzureAdSummaryBuilder by constructing a ResourceChangeModel and ResourceChange.
    /// </summary>
    /// <param name="resourceType">The Terraform resource type.</param>
    /// <param name="name">The resource name.</param>
    /// <param name="afterState">The JSON state object (after).</param>
    /// <param name="principalMapper">The principal mapper to use.</param>
    /// <param name="action">The Terraform action (defaults to "create").</param>
    /// <param name="appRoleResolver">Optional app role resolver.</param>
    /// <returns>The generated summary HTML string.</returns>
    private static string BuildSummary(
        string resourceType,
        string name,
        JsonElement afterState,
        IPrincipalMapper principalMapper,
        string action = "create",
        IAppRoleResolver? appRoleResolver = null)
    {
        var model = new ResourceChangeModel
        {
            Address = $"{resourceType}.{name}",
            Type = resourceType,
            Name = name,
            ProviderName = "azuread",
            Action = action,
            ActionSymbol = action == "delete" ? "❌" : "➕",
            AttributeChanges = new List<AttributeChangeModel>()
        };

        var resourceChange = new ResourceChange(
            model.Address,
            null,
            "managed",
            resourceType,
            name,
            "azuread",
            new Change([action], null, afterState, null, null, null));

        return AzureAdSummaryBuilder.BuildSummaryHtml(
            model,
            resourceChange,
            action,
            principalMapper,
            iconProviderRegistry: null,
            appRoleResolver);
    }
}
