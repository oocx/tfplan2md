using System.Collections.Frozen;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Diagnostics;
using Oocx.TfPlan2Md.Providers.AzureDevOps;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzureDevOps;

/// <summary>
/// Tests for AzdoRepositoryMapper class.
/// Related feature: docs/features/096-azdo-repo-mapping-and-icons/specification.md.
/// </summary>
public class AzdoRepositoryMapperTests
{
    /// <summary>
    /// Verifies that AzdoRepositoryMapper correctly resolves repository IDs to formatted names with icon.
    /// </summary>
    [Test]
    public void GetEntityName_KnownRepositoryId_ReturnsFormattedNameWithIcon()
    {
        var mappings = new Dictionary<string, string>
        {
            ["a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"] = "Infrastructure Repo"
        }.ToFrozenDictionary();
        var mapper = new AzdoRepositoryMapper(mappings, null);

        var result = mapper.GetEntityName("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d");

        result.Should().Be("🗃️ Infrastructure Repo [a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d]");
    }

    /// <summary>
    /// Verifies that unmapped repository IDs return the raw ID with icon.
    /// </summary>
    [Test]
    public void GetEntityName_UnknownRepositoryId_ReturnsRawIdWithIcon()
    {
        var mapper = new AzdoRepositoryMapper(FrozenDictionary<string, string>.Empty, null);

        var result = mapper.GetEntityName("unknown-repo-id");

        result.Should().Be("🗃️ unknown-repo-id");
    }

    /// <summary>
    /// Verifies that failed resolutions are recorded in diagnostics.
    /// </summary>
    [Test]
    public void GetName_UnknownRepositoryIdWithAddress_RecordsFailedResolution()
    {
        var diagnostics = new DiagnosticContext();
        var mapper = new AzdoRepositoryMapper(FrozenDictionary<string, string>.Empty, diagnostics);

        var result = mapper.GetName("unknown-repo", "azuredevops_build_definition.example");

        result.Should().BeNull();
        diagnostics.FailedResolutions.Should().ContainSingle();
        diagnostics.FailedResolutions[0].Type.Should().Be(FailedResolutionType.AzdoRepository);
        diagnostics.FailedResolutions[0].Id.Should().Be("unknown-repo");
        diagnostics.FailedResolutions[0].ResourceAddress.Should().Be("azuredevops_build_definition.example");
    }

    /// <summary>
    /// Verifies that GetName returns null without recording failures when no resource address is provided.
    /// </summary>
    [Test]
    public void GetName_UnknownRepositoryIdWithoutAddress_DoesNotRecordFailure()
    {
        var diagnostics = new DiagnosticContext();
        var mapper = new AzdoRepositoryMapper(FrozenDictionary<string, string>.Empty, diagnostics);

        var result = mapper.GetName("unknown-repo");

        result.Should().BeNull();
        diagnostics.FailedResolutions.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that GetName returns display name when repository ID is mapped.
    /// </summary>
    [Test]
    public void GetName_KnownRepositoryId_ReturnsDisplayName()
    {
        var mappings = new Dictionary<string, string>
        {
            ["a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"] = "Infrastructure Repo"
        }.ToFrozenDictionary();
        var mapper = new AzdoRepositoryMapper(mappings, null);

        var result = mapper.GetName("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d");

        result.Should().Be("Infrastructure Repo");
    }
}
