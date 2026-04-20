using AwesomeAssertions;
using Oocx.TfPlan2Md.Platforms.Azure;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Platforms.Azure;

/// <summary>
/// Tests for the Microsoft Graph app role resolver that maps GUIDs to permission names.
/// Related feature: docs/features/116-azuread-app-role-assignment/specification.md.
/// </summary>
public class MicrosoftGraphAppRoleResolverTests
{
    /// <summary>
    /// TC-01: Verifies that a well-known GUID resolves to the correct RoleDefinitionInfo.
    /// </summary>
    [Test]
    public void GetAppRole_KnownGuid_ReturnsMappedRoleInfo()
    {
        var resolver = MicrosoftGraphAppRoleResolver.CreateBuiltIn();

        var result = resolver.GetAppRole("df021288-bdef-4463-88db-98f22de89214");

        result.Name.Should().Be("User.Read.All");
        result.Id.Should().Be("df021288-bdef-4463-88db-98f22de89214");
        result.FullName.Should().Be("User.Read.All (df021288-bdef-4463-88db-98f22de89214)");
    }

    /// <summary>
    /// TC-02: Verifies that an unknown GUID returns the raw GUID as all three fields.
    /// </summary>
    [Test]
    public void GetAppRole_UnknownGuid_ReturnsGuidAsAllFields()
    {
        var resolver = MicrosoftGraphAppRoleResolver.CreateBuiltIn();

        var result = resolver.GetAppRole("99999999-9999-9999-9999-999999999999");

        result.Name.Should().Be("99999999-9999-9999-9999-999999999999");
        result.Id.Should().Be("99999999-9999-9999-9999-999999999999");
        result.FullName.Should().Be("99999999-9999-9999-9999-999999999999");
    }

    /// <summary>
    /// TC-03: Verifies that null, empty, and whitespace inputs return graceful fallback values.
    /// </summary>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public void GetAppRole_NullOrEmptyInput_ReturnsEmptyRoleInfo(string? appRoleId)
    {
        var resolver = MicrosoftGraphAppRoleResolver.CreateBuiltIn();

        var result = resolver.GetAppRole(appRoleId);

        result.Name.Should().BeEmpty();
        result.Id.Should().BeEmpty();
        result.FullName.Should().BeEmpty();
    }

    /// <summary>
    /// TC-04: Verifies that GUID lookup is case-insensitive.
    /// </summary>
    [Test]
    public void GetAppRole_UppercaseGuid_ReturnsMappedRoleInfo()
    {
        var resolver = MicrosoftGraphAppRoleResolver.CreateBuiltIn();

        var result = resolver.GetAppRole("DF021288-BDEF-4463-88DB-98F22DE89214");

        result.Name.Should().Be("User.Read.All");
    }

    /// <summary>
    /// Verifies GetAppRoleName returns the FullName for a known GUID.
    /// </summary>
    [Test]
    public void GetAppRoleName_KnownGuid_ReturnsFullName()
    {
        var resolver = MicrosoftGraphAppRoleResolver.CreateBuiltIn();

        var result = resolver.GetAppRoleName("df021288-bdef-4463-88db-98f22de89214");

        result.Should().Be("User.Read.All (df021288-bdef-4463-88db-98f22de89214)");
    }

    /// <summary>
    /// Verifies a second well-known GUID (Directory.Read.All) resolves correctly.
    /// </summary>
    [Test]
    public void GetAppRole_DirectoryReadAll_ReturnsMappedRoleInfo()
    {
        var resolver = MicrosoftGraphAppRoleResolver.CreateBuiltIn();

        var result = resolver.GetAppRole("7ab1d382-f21e-4acd-a863-ba3e13f7da61");

        result.Name.Should().Be("Directory.Read.All");
        result.FullName.Should().Be("Directory.Read.All (7ab1d382-f21e-4acd-a863-ba3e13f7da61)");
    }

    /// <summary>
    /// Issue 120: pins the maintainer's exact scenario — Policy.ReadWrite.Authorization
    /// (GUID fb221be6-99f2-473f-bd32-01c6a0e9ca3b) must resolve from the expanded
    /// well-known mapping. Related issue: docs/issues/120-msgraph-permissions-mapping-coverage.
    /// </summary>
    [Test]
    public void GetAppRole_PolicyReadWriteAuthorization_ResolvesToName()
    {
        var resolver = MicrosoftGraphAppRoleResolver.CreateBuiltIn();

        var result = resolver.GetAppRole("fb221be6-99f2-473f-bd32-01c6a0e9ca3b");

        result.Name.Should().Be("Policy.ReadWrite.Authorization");
        result.Id.Should().Be("fb221be6-99f2-473f-bd32-01c6a0e9ca3b");
        result.FullName.Should().Be("Policy.ReadWrite.Authorization (fb221be6-99f2-473f-bd32-01c6a0e9ca3b)");
    }

    /// <summary>
    /// Issue 120: representative cross-section of the expanded well-known mapping —
    /// includes entries already present before the regeneration plus several newly
    /// added ones across different permission families. Guards against regressions
    /// in future regenerations of MicrosoftGraphAppRoles.json.
    /// </summary>
    [Test]
    [Arguments("fb221be6-99f2-473f-bd32-01c6a0e9ca3b", "Policy.ReadWrite.Authorization")]
    [Arguments("246dd0d5-5bd0-4def-940b-0421030a5b68", "Policy.Read.All")]
    [Arguments("df021288-bdef-4463-88db-98f22de89214", "User.Read.All")]
    [Arguments("7ab1d382-f21e-4acd-a863-ba3e13f7da61", "Directory.Read.All")]
    [Arguments("9a5d68dd-52b0-4cc2-bd40-abcf44ac3a30", "Application.Read.All")]
    [Arguments("1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9", "Application.ReadWrite.All")]
    [Arguments("b0afded3-3588-46d8-8b3d-9842eff778da", "AuditLog.Read.All")]
    [Arguments("9e3f62cf-ca93-4989-b6ce-bf83c28f9fe8", "RoleManagement.ReadWrite.Directory")]
    public void GetAppRole_RepresentativeWellKnownGuids_ResolveToExpectedNames(
        string appRoleId,
        string expectedName)
    {
        var resolver = MicrosoftGraphAppRoleResolver.CreateBuiltIn();

        var result = resolver.GetAppRole(appRoleId);

        result.Name.Should().Be(expectedName);
    }
}
