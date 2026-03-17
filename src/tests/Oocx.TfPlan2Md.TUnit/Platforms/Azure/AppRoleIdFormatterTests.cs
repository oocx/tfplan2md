using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Platforms.Azure;

/// <summary>
/// Tests for the AppRoleIdFormatter that formats app role GUIDs with resolved names and icons.
/// Related feature: docs/features/116-azuread-app-role-assignment/specification.md.
/// </summary>
public class AppRoleIdFormatterTests
{
    /// <summary>
    /// TC-05: Verifies a known Microsoft Graph app role GUID is formatted with the 🛡️ icon.
    /// </summary>
    [Test]
    public void TryFormat_KnownAppRoleId_ReturnsFormattedString()
    {
        var formatter = new AppRoleIdFormatter();
        var context = new ServiceResolutionContext(
            "azuread", "azuread_app_role_assignment", "app_role_id",
            "df021288-bdef-4463-88db-98f22de89214");

        var result = formatter.TryFormat(context);

        result.Should().Be("`🛡️\u00a0User.Read.All (df021288-bdef-4463-88db-98f22de89214)`");
    }

    /// <summary>
    /// TC-06: Verifies an unknown GUID returns null to allow default raw value display.
    /// </summary>
    [Test]
    public void TryFormat_UnknownAppRoleId_ReturnsNull()
    {
        var formatter = new AppRoleIdFormatter();
        var context = new ServiceResolutionContext(
            "azuread", "azuread_app_role_assignment", "app_role_id",
            "99999999-9999-9999-9999-999999999999");

        var result = formatter.TryFormat(context);

        result.Should().BeNull();
    }

    /// <summary>
    /// TC-07: Verifies null and empty string values return null without exceptions.
    /// </summary>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public void TryFormat_NullOrEmptyValue_ReturnsNull(string? value)
    {
        var formatter = new AppRoleIdFormatter();
        var context = new ServiceResolutionContext(
            "azuread", "azuread_app_role_assignment", "app_role_id", value);

        var result = formatter.TryFormat(context);

        result.Should().BeNull();
    }

    /// <summary>
    /// TC-08: Verifies another known GUID (Directory.Read.All) is formatted correctly.
    /// </summary>
    [Test]
    public void TryFormat_DirectoryReadAllGuid_ReturnsFormattedString()
    {
        var formatter = new AppRoleIdFormatter();
        var context = new ServiceResolutionContext(
            "azuread", "azuread_app_role_assignment", "app_role_id",
            "7ab1d382-f21e-4acd-a863-ba3e13f7da61");

        var result = formatter.TryFormat(context);

        result.Should().Contain("Directory.Read.All");
        result.Should().Contain("🛡️");
    }
}
