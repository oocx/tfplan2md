using System.Collections.Frozen;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Providers.AzureDevOps;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzureDevOps;

/// <summary>
/// Tests for Azure DevOps value formatters.
/// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
/// </summary>
public class AzdoValueFormatterTests
{
    /// <summary>
    /// Verifies that AzdoUserIdFormatter formats mapped user IDs with display names.
    /// </summary>
    [Test]
    public void AzdoUserIdFormatter_MappedUserId_ReturnsFormattedName()
    {
        var mappings = new Dictionary<string, string>
        {
            ["4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b"] = "Alice User"
        }.ToFrozenDictionary();
        var mapper = new AzdoUserMapper(mappings, null);
        var formatter = new AzdoUserIdFormatter(mapper);
        var context = new ServiceResolutionContext("azuredevops", null, "member", "4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b");

        var result = formatter.TryFormat(context);

        result.Should().Be("`👤\u00A0Alice User (4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b)`");
    }

    /// <summary>
    /// Verifies that AzdoUserIdFormatter returns null for unmapped user IDs.
    /// </summary>
    [Test]
    public void AzdoUserIdFormatter_UnmappedUserId_ReturnsNull()
    {
        var mapper = new AzdoUserMapper(FrozenDictionary<string, string>.Empty, null);
        var formatter = new AzdoUserIdFormatter(mapper);
        var context = new ServiceResolutionContext("azuredevops", null, "member", "unknown-user-id");

        var result = formatter.TryFormat(context);

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that AzdoUserIdFormatter handles null or empty values.
    /// </summary>
    [Test]
    public void AzdoUserIdFormatter_NullValue_ReturnsNull()
    {
        var mapper = new AzdoUserMapper(FrozenDictionary<string, string>.Empty, null);
        var formatter = new AzdoUserIdFormatter(mapper);
        var context = new ServiceResolutionContext("azuredevops", null, "member", null);

        var result = formatter.TryFormat(context);

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that AzdoGroupDescriptorFormatter formats mapped group descriptors with display names.
    /// </summary>
    [Test]
    public void AzdoGroupDescriptorFormatter_MappedDescriptor_ReturnsFormattedName()
    {
        var mappings = new Dictionary<string, string>
        {
            ["aadgp.Uy0xLTktMTU1MTM3NDI0NS0xMjA0NDAwOTY5LTI0MDI5ODY0MTMtMjE3OTQwODYxNi0zLTM4Mzg1ODYwMTUtMzIyMTk1OTc5OC0xMjM0NTY3ODkw"] = "Platform Team"
        }.ToFrozenDictionary();
        var mapper = new AzdoGroupMapper(mappings, null);
        var formatter = new AzdoGroupDescriptorFormatter(mapper);
        var context = new ServiceResolutionContext(
            "azuredevops",
            null,
            "group",
            "aadgp.Uy0xLTktMTU1MTM3NDI0NS0xMjA0NDAwOTY5LTI0MDI5ODY0MTMtMjE3OTQwODYxNi0zLTM4Mzg1ODYwMTUtMzIyMTk1OTc5OC0xMjM0NTY3ODkw");

        var result = formatter.TryFormat(context);

        result.Should().Be("`👥\u00A0Platform Team (aadgp.Uy0xLTktMTU1MTM3NDI0NS0xMjA0NDAwOTY5LTI0MDI5ODY0MTMtMjE3OTQwODYxNi0zLTM4Mzg1ODYwMTUtMzIyMTk1OTc5OC0xMjM0NTY3ODkw)`");
    }

    /// <summary>
    /// Verifies that AzdoGroupDescriptorFormatter returns null for unmapped descriptors.
    /// </summary>
    [Test]
    public void AzdoGroupDescriptorFormatter_UnmappedDescriptor_ReturnsNull()
    {
        var mapper = new AzdoGroupMapper(FrozenDictionary<string, string>.Empty, null);
        var formatter = new AzdoGroupDescriptorFormatter(mapper);
        var context = new ServiceResolutionContext("azuredevops", null, "group", "unknown-descriptor");

        var result = formatter.TryFormat(context);

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that AzdoGroupDescriptorFormatter handles null or empty values.
    /// </summary>
    [Test]
    public void AzdoGroupDescriptorFormatter_NullValue_ReturnsNull()
    {
        var mapper = new AzdoGroupMapper(FrozenDictionary<string, string>.Empty, null);
        var formatter = new AzdoGroupDescriptorFormatter(mapper);
        var context = new ServiceResolutionContext("azuredevops", null, "group", null);

        var result = formatter.TryFormat(context);

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that AzdoProjectIdFormatter formats mapped project IDs with display names.
    /// </summary>
    [Test]
    public void AzdoProjectIdFormatter_MappedProjectId_ReturnsFormattedName()
    {
        var mappings = new Dictionary<string, string>
        {
            ["8b3e9c1d-4f5a-6b7c-8d9e-0f1a2b3c4d5e"] = "Contoso Project"
        }.ToFrozenDictionary();
        var mapper = new AzdoProjectMapper(mappings, null);
        var formatter = new AzdoProjectIdFormatter(mapper);
        var context = new ServiceResolutionContext("azuredevops", null, "project_id", "8b3e9c1d-4f5a-6b7c-8d9e-0f1a2b3c4d5e");

        var result = formatter.TryFormat(context);

        result.Should().Be("`📋\u00A0Contoso Project (8b3e9c1d-4f5a-6b7c-8d9e-0f1a2b3c4d5e)`");
    }

    /// <summary>
    /// Verifies that AzdoProjectIdFormatter returns null for unmapped project IDs.
    /// </summary>
    [Test]
    public void AzdoProjectIdFormatter_UnmappedProjectId_ReturnsNull()
    {
        var mapper = new AzdoProjectMapper(FrozenDictionary<string, string>.Empty, null);
        var formatter = new AzdoProjectIdFormatter(mapper);
        var context = new ServiceResolutionContext("azuredevops", null, "project_id", "unknown-project-id");

        var result = formatter.TryFormat(context);

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that AzdoProjectIdFormatter handles null or empty values.
    /// </summary>
    [Test]
    public void AzdoProjectIdFormatter_NullValue_ReturnsNull()
    {
        var mapper = new AzdoProjectMapper(FrozenDictionary<string, string>.Empty, null);
        var formatter = new AzdoProjectIdFormatter(mapper);
        var context = new ServiceResolutionContext("azuredevops", null, "project_id", null);

        var result = formatter.TryFormat(context);

        result.Should().BeNull();
    }
}
