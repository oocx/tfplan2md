using System.Collections.Frozen;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Diagnostics;
using Oocx.TfPlan2Md.Providers.AzureDevOps;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzureDevOps;

/// <summary>
/// Tests for AzdoGroupMapper class.
/// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
/// </summary>
public class AzdoGroupMapperTests
{
    /// <summary>
    /// TC-10: Verifies that AzdoGroupMapper correctly resolves group descriptors including long ones.
    /// </summary>
    [Test]
    public void GetEntityName_LongDescriptor_PreservesFullDescriptor()
    {
        var longDescriptor = "vssgp.Uy0xLTktMTU1MTM3NDI0NS0yNzY5MzQwNjk3LTExMDE5ODM1NjMtMzU0Nzk5MjM2MS0zNzAyMTIxNjI4LTEtMTIzNDU2Nzg5MC0xMjM0NTY3ODkwLTEyMzQ1Njc4OTAtMTIzNDU2Nzg5MA";
        var mappings = new Dictionary<string, string>
        {
            [longDescriptor] = "Platform Team"
        }.ToFrozenDictionary();
        var mapper = new AzdoGroupMapper(mappings, null);

        var result = mapper.GetEntityName(longDescriptor);

        result.Should().Be($"Platform Team [{longDescriptor}]");
        result.Should().Contain(longDescriptor); // Full descriptor preserved
        result.Length.Should().BeGreaterThan(100); // Verify not truncated
    }

    /// <summary>
    /// TC-12: Verifies that unmapped group descriptors return the raw descriptor.
    /// </summary>
    [Test]
    public void GetEntityName_UnknownGroupDescriptor_ReturnsRawDescriptor()
    {
        var mapper = new AzdoGroupMapper(FrozenDictionary<string, string>.Empty, null);

        var result = mapper.GetEntityName("unknown-descriptor");

        result.Should().Be("unknown-descriptor");
    }

    /// <summary>
    /// TC-13: Verifies that failed resolutions are recorded in diagnostics.
    /// </summary>
    [Test]
    public void GetName_UnknownGroupWithAddress_RecordsFailedResolution()
    {
        var diagnostics = new DiagnosticContext();
        var mapper = new AzdoGroupMapper(FrozenDictionary<string, string>.Empty, diagnostics);

        var result = mapper.GetName("unknown-group", "azuredevops_group_membership.example");

        result.Should().BeNull();
        diagnostics.FailedResolutions.Should().ContainSingle();
        diagnostics.FailedResolutions[0].Type.Should().Be(FailedResolutionType.AzdoGroup);
        diagnostics.FailedResolutions[0].Id.Should().Be("unknown-group");
        diagnostics.FailedResolutions[0].ResourceAddress.Should().Be("azuredevops_group_membership.example");
    }
}
