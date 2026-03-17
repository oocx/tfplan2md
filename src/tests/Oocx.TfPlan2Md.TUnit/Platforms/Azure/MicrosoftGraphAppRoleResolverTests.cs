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
}
