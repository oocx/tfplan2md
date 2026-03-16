using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Providers.AzureAD;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzureAD;

/// <summary>
/// Tests for the <see cref="AppRoleIdFormatter"/>.
/// </summary>
public class AppRoleIdFormatterTests
{
    private readonly AppRoleIdFormatter _formatter = new();

    [Test]
    public void TryFormat_KnownAppRoleId_ReturnsFormattedPermission()
    {
        var context = new ServiceResolutionContext(
            "azuread", "azuread_app_role_assignment", "app_role_id",
            "df021288-bdef-4463-88db-98f22de89214");

        var result = _formatter.TryFormat(context);

        result.Should().NotBeNull();
        result.Should().Contain("🔑");
        result.Should().Contain("User.Read.All");
        result.Should().Contain("df021288-bdef-4463-88db-98f22de89214");
    }

    [Test]
    public void TryFormat_UnknownAppRoleId_ReturnsNull()
    {
        var context = new ServiceResolutionContext(
            "azuread", "azuread_app_role_assignment", "app_role_id",
            "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        var result = _formatter.TryFormat(context);

        result.Should().BeNull();
    }

    [Test]
    public void TryFormat_EmptyValue_ReturnsNull()
    {
        var context = new ServiceResolutionContext(
            "azuread", "azuread_app_role_assignment", "app_role_id",
            string.Empty);

        var result = _formatter.TryFormat(context);

        result.Should().BeNull();
    }

    [Test]
    public void TryFormat_ApplicationReadAll_ReturnsFormattedPermission()
    {
        var context = new ServiceResolutionContext(
            "azuread", "azuread_app_role_assignment", "app_role_id",
            "9a5d68dd-52b0-4cc2-bd40-abcf44ac3a30");

        var result = _formatter.TryFormat(context);

        result.Should().NotBeNull();
        result.Should().Contain("Application.Read.All");
    }
}
