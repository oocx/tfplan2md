using AwesomeAssertions;
using Oocx.TfPlan2Md.Platforms.Azure;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Platforms.Azure;

/// <summary>
/// Tests for the Microsoft Graph app role resolver.
/// </summary>
public class MicrosoftGraphAppRoleResolverTests
{
    private readonly IAppRoleResolver _resolver = MicrosoftGraphAppRoleResolver.CreateBuiltIn();

    [Test]
    public void GetAppRoleName_KnownGuid_ReturnsPermissionNameWithGuid()
    {
        var result = _resolver.GetAppRoleName("df021288-bdef-4463-88db-98f22de89214");

        result.Should().Be("User.Read.All (df021288-bdef-4463-88db-98f22de89214)");
    }

    [Test]
    public void GetAppRoleName_UnknownGuid_ReturnsRawGuid()
    {
        var result = _resolver.GetAppRoleName("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        result.Should().Be("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    }

    [Test]
    public void GetAppRoleName_NullOrEmpty_ReturnsEmpty()
    {
        _resolver.GetAppRoleName(null).Should().Be(string.Empty);
        _resolver.GetAppRoleName(string.Empty).Should().Be(string.Empty);
        _resolver.GetAppRoleName("  ").Should().Be(string.Empty);
    }

    [Test]
    public void GetPermissionName_KnownGuid_ReturnsPermissionName()
    {
        var result = _resolver.GetPermissionName("df021288-bdef-4463-88db-98f22de89214");

        result.Should().Be("User.Read.All");
    }

    [Test]
    public void GetPermissionName_UnknownGuid_ReturnsNull()
    {
        var result = _resolver.GetPermissionName("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        result.Should().BeNull();
    }

    [Test]
    public void GetPermissionName_NullOrEmpty_ReturnsNull()
    {
        _resolver.GetPermissionName(null).Should().BeNull();
        _resolver.GetPermissionName(string.Empty).Should().BeNull();
    }

    [Test]
    public void GetAppRoleName_CaseInsensitive_ResolvesCorrectly()
    {
        var result = _resolver.GetAppRoleName("DF021288-BDEF-4463-88DB-98F22DE89214");

        result.Should().Be("User.Read.All (DF021288-BDEF-4463-88DB-98F22DE89214)");
    }

    [Test]
    public void GetAppRoleName_GroupReadAll_ResolvesCorrectly()
    {
        var result = _resolver.GetAppRoleName("5b567255-7703-4780-807c-7be8301ae99b");

        result.Should().Be("Group.Read.All (5b567255-7703-4780-807c-7be8301ae99b)");
    }

    [Test]
    public void GetAppRoleName_DirectoryReadAll_ResolvesCorrectly()
    {
        var result = _resolver.GetAppRoleName("7ab1d382-f21e-4acd-a863-ba3e13f7da61");

        result.Should().Be("Directory.Read.All (7ab1d382-f21e-4acd-a863-ba3e13f7da61)");
    }

    [Test]
    public void GetAppRoleName_ApplicationReadWriteAll_ResolvesCorrectly()
    {
        var result = _resolver.GetAppRoleName("1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9");

        result.Should().Be("Application.ReadWrite.All (1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9)");
    }
}
