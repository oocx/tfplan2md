using System.Collections.Frozen;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Diagnostics;
using Oocx.TfPlan2Md.Providers.AzureDevOps;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzureDevOps;

/// <summary>
/// Tests for AzdoProjectMapper class.
/// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
/// </summary>
public class AzdoProjectMapperTests
{
    /// <summary>
    /// TC-11: Verifies that AzdoProjectMapper correctly resolves project IDs to formatted names.
    /// </summary>
    [Test]
    public void GetEntityName_KnownProjectId_ReturnsFormattedName()
    {
        var mappings = new Dictionary<string, string>
        {
            ["8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f"] = "Infrastructure Project"
        }.ToFrozenDictionary();
        var mapper = new AzdoProjectMapper(mappings, null);

        var result = mapper.GetEntityName("8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f");

        result.Should().Be("Infrastructure Project [8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f]");
    }

    /// <summary>
    /// TC-12: Verifies that unmapped project IDs return the raw ID.
    /// </summary>
    [Test]
    public void GetEntityName_UnknownProjectId_ReturnsRawId()
    {
        var mapper = new AzdoProjectMapper(FrozenDictionary<string, string>.Empty, null);

        var result = mapper.GetEntityName("unknown-project-id");

        result.Should().Be("unknown-project-id");
    }

    /// <summary>
    /// TC-13: Verifies that failed resolutions are recorded in diagnostics.
    /// </summary>
    [Test]
    public void GetName_UnknownProjectWithAddress_RecordsFailedResolution()
    {
        var diagnostics = new DiagnosticContext();
        var mapper = new AzdoProjectMapper(FrozenDictionary<string, string>.Empty, diagnostics);

        var result = mapper.GetName("unknown-project", "azuredevops_project.example");

        result.Should().BeNull();
        diagnostics.FailedResolutions.Should().ContainSingle();
        diagnostics.FailedResolutions[0].Type.Should().Be(FailedResolutionType.AzdoProject);
        diagnostics.FailedResolutions[0].Id.Should().Be("unknown-project");
        diagnostics.FailedResolutions[0].ResourceAddress.Should().Be("azuredevops_project.example");
    }
}
