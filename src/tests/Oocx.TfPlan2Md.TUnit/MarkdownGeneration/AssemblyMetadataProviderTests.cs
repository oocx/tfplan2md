using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests metadata extraction from build-time generated metadata.
/// </summary>
public class AssemblyMetadataProviderTests
{
    /// <summary>
    /// Ensures generated build metadata provides version and commit hash.
    /// </summary>
    [Test]
    public void GetMetadata_UsesGeneratedBuildInfo()
    {
        var provider = new AssemblyMetadataProvider();

        var metadata = provider.GetMetadata();

        metadata.Version.Should().NotBeNullOrWhiteSpace();
        metadata.CommitHash.Should().NotBeNullOrWhiteSpace();
        metadata.CommitHash.Length.Should().BeLessThanOrEqualTo(7);
    }

    /// <summary>
    /// Ensures generated metadata includes a UTC timestamp.
    /// </summary>
    [Test]
    public void GetMetadata_SetsGeneratedAtUtc()
    {
        var provider = new AssemblyMetadataProvider();

        var metadata = provider.GetMetadata();

        metadata.GeneratedAtUtc.Offset.Should().Be(TimeSpan.Zero);
    }
}
