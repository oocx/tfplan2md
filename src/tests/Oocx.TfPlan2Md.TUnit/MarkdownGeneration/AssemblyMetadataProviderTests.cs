using System.Reflection;
using System.Reflection.Emit;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests metadata extraction from assembly attributes.
/// </summary>
public class AssemblyMetadataProviderTests
{
    /// <summary>
    /// Ensures assembly metadata attributes provide the commit hash when available.
    /// </summary>
    [Test]
    public void GetMetadata_UsesAssemblyMetadataCommitHash()
    {
        var assembly = CreateAssembly(
            informationalVersion: "1.2.3+abcdef123",
            metadata: ("CommitHash", "abcdef123"));
        var provider = new AssemblyMetadataProvider(assembly);

        var metadata = provider.GetMetadata();

        metadata.Version.Should().Be("1.2.3");
        metadata.CommitHash.Should().Be("abcdef1");
    }

    /// <summary>
    /// Ensures informational version metadata is used when no commit attribute is present.
    /// </summary>
    [Test]
    public void GetMetadata_FallsBackToInformationalVersionCommitHash()
    {
        var assembly = CreateAssembly(informationalVersion: "2.0.0+1234567");
        var provider = new AssemblyMetadataProvider(assembly);

        var metadata = provider.GetMetadata();

        metadata.Version.Should().Be("2.0.0");
        metadata.CommitHash.Should().Be("1234567");
    }

    /// <summary>
    /// Ensures unknown placeholders are used when metadata is missing.
    /// </summary>
    [Test]
    public void GetMetadata_UsesUnknownWhenNoMetadataAvailable()
    {
        var assembly = CreateAssembly();
        var provider = new AssemblyMetadataProvider(assembly);

        var metadata = provider.GetMetadata();

        metadata.Version.Should().Be("unknown");
        metadata.CommitHash.Should().Be("unknown");
    }

    /// <summary>
    /// Creates a dynamic assembly with optional informational version and metadata attributes.
    /// </summary>
    /// <param name="informationalVersion">Optional informational version value.</param>
    /// <param name="metadata">Optional metadata key/value.</param>
    /// <returns>The generated assembly builder.</returns>
    private static AssemblyBuilder CreateAssembly(string? informationalVersion = null, (string Key, string Value)? metadata = null)
    {
        var name = new AssemblyName($"TestAssembly.{Guid.NewGuid():N}");
        var builder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        builder.DefineDynamicModule("Main");

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var ctor = typeof(AssemblyInformationalVersionAttribute).GetConstructor([typeof(string)]);
            var attribute = new CustomAttributeBuilder(ctor!, [informationalVersion]);
            builder.SetCustomAttribute(attribute);
        }

        if (metadata.HasValue)
        {
            var ctor = typeof(AssemblyMetadataAttribute).GetConstructor([typeof(string), typeof(string)]);
            var attribute = new CustomAttributeBuilder(ctor!, [metadata.Value.Key, metadata.Value.Value]);
            builder.SetCustomAttribute(attribute);
        }

        return builder;
    }
}
