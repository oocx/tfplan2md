using System;
using System.IO;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests JSON-based icon provider behavior.
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </summary>
public class FileBasedIconProviderTests
{
    /// <summary>
    /// Ensures icon rules load from JSON and resolve matching icons.
    /// </summary>
    [Test]
    public void FileBasedIconProvider_LoadsRulesAndResolvesIcon()
    {
        var json = "{\"rules\":[{\"attributeNamePattern\":\"^name$\",\"icon\":\"X\"}]}";
        var filePath = WriteTempFile(json);

        try
        {
            var provider = new FileBasedIconProvider(filePath);
            var context = new ServiceResolutionContext("provider", "resource", "name", "value");

            var icon = provider.TryGetIcon(context);

            icon.Should().Be("X");
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// Ensures invalid JSON triggers a registration exception.
    /// </summary>
    [Test]
    public void FileBasedIconProvider_InvalidJson_ThrowsServiceRegistrationException()
    {
        var json = "not-json";
        var filePath = WriteTempFile(json);

        try
        {
            var action = () => new FileBasedIconProvider(filePath);

            action.Should().Throw<ServiceRegistrationException>()
                .WithMessage("*Failed to load icon rules*");
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// Ensures invalid regex patterns trigger a registration exception.
    /// </summary>
    [Test]
    public void FileBasedIconProvider_InvalidRegex_ThrowsServiceRegistrationException()
    {
        var json = "{\"rules\":[{\"attributeNamePattern\":\"[[\",\"icon\":\"X\"}]}";
        var filePath = WriteTempFile(json);

        try
        {
            var action = () => new FileBasedIconProvider(filePath);

            action.Should().Throw<ServiceRegistrationException>()
                .WithMessage("*Invalid attribute name regex pattern*");
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// Writes JSON to a temporary file for testing.
    /// </summary>
    /// <param name="content">The JSON content to write.</param>
    /// <returns>The temporary file path.</returns>
    private static string WriteTempFile(string content)
    {
        var baseDirectory = Path.Combine(AppContext.BaseDirectory, "tmp");
        Directory.CreateDirectory(baseDirectory);

        var filePath = Path.Combine(baseDirectory, $"icon-rules-{Guid.NewGuid():N}.json");
        File.WriteAllText(filePath, content);
        return filePath;
    }
}
