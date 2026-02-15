using System.Collections.Frozen;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Diagnostics;
using Oocx.TfPlan2Md.Providers.AzureDevOps;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzureDevOps;

/// <summary>
/// Tests for AzdoUserMapper class.
/// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
/// </summary>
public class AzdoUserMapperTests
{
    /// <summary>
    /// TC-09: Verifies that AzdoUserMapper correctly resolves user IDs to formatted names.
    /// </summary>
    [Test]
    public void GetEntityName_KnownUserId_ReturnsFormattedName()
    {
        var mappings = new Dictionary<string, string>
        {
            ["4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b"] = "John Smith"
        }.ToFrozenDictionary();
        var mapper = new AzdoUserMapper(mappings, null);

        var result = mapper.GetEntityName("4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b");

        result.Should().Be("John Smith [4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b]");
    }

    /// <summary>
    /// TC-12: Verifies that unmapped user IDs return the raw ID.
    /// </summary>
    [Test]
    public void GetEntityName_UnknownUserId_ReturnsRawId()
    {
        var mapper = new AzdoUserMapper(FrozenDictionary<string, string>.Empty, null);

        var result = mapper.GetEntityName("unknown-user-id");

        result.Should().Be("unknown-user-id");
    }

    /// <summary>
    /// TC-13: Verifies that failed resolutions are recorded in diagnostics.
    /// </summary>
    [Test]
    public void GetName_UnknownUserIdWithAddress_RecordsFailedResolution()
    {
        var diagnostics = new DiagnosticContext();
        var mapper = new AzdoUserMapper(FrozenDictionary<string, string>.Empty, diagnostics);

        var result = mapper.GetName("unknown-user", "azuredevops_group_membership.example");

        result.Should().BeNull();
        diagnostics.FailedResolutions.Should().ContainSingle();
        diagnostics.FailedResolutions[0].Type.Should().Be(FailedResolutionType.AzdoUser);
        diagnostics.FailedResolutions[0].Id.Should().Be("unknown-user");
        diagnostics.FailedResolutions[0].ResourceAddress.Should().Be("azuredevops_group_membership.example");
    }

    /// <summary>
    /// Verifies that GetName returns null without recording failures when no resource address is provided.
    /// </summary>
    [Test]
    public void GetName_UnknownUserIdWithoutAddress_DoesNotRecordFailure()
    {
        var diagnostics = new DiagnosticContext();
        var mapper = new AzdoUserMapper(FrozenDictionary<string, string>.Empty, diagnostics);

        var result = mapper.GetName("unknown-user");

        result.Should().BeNull();
        diagnostics.FailedResolutions.Should().BeEmpty();
    }
}
