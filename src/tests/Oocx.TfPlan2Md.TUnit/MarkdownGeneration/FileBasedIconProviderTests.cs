using System.Text;
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
        var provider = CreateProvider("valid-icons.json");
        var context = new ServiceResolutionContext("provider", "resource", "name", "value");

        var icon = provider.TryGetIcon(context);

        icon.Should().Be("X");
    }

    /// <summary>
    /// Ensures invalid JSON triggers a registration exception.
    /// </summary>
    [Test]
    public void FileBasedIconProvider_InvalidJson_ThrowsServiceRegistrationException()
    {
        var action = () => CreateProvider("invalid-json.json");

        action.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*Failed to load icon rules from embedded resource*");
    }

    /// <summary>
    /// Ensures invalid regex patterns trigger a registration exception.
    /// </summary>
    [Test]
    public void FileBasedIconProvider_InvalidRegex_ThrowsServiceRegistrationException()
    {
        var action = () => CreateProvider("invalid-regex.json");

        action.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*Invalid attribute name regex pattern*");
    }

    /// <summary>
    /// Creates an icon provider backed by a test embedded resource.
    /// </summary>
    /// <param name="resourceFileName">The file name of the embedded resource.</param>
    /// <returns>The initialized icon provider.</returns>
    private static FileBasedIconProvider CreateProvider(string resourceFileName)
    {
        var json = resourceFileName switch
        {
            "valid-icons.json" =>
                "{\"rules\":[{\"providerPattern\":\"^provider$\",\"resourceTypePattern\":\"^resource$\",\"attributeNamePattern\":\"^name$\",\"valuePattern\":\"^value$\",\"icon\":\"X\"}]}",
            "invalid-json.json" =>
                "{\"rules\":[{\"providerPattern\":\"^provider$\",",
            "invalid-regex.json" =>
                "{\"rules\":[{\"providerPattern\":\"^provider$\",\"attributeNamePattern\":\"[\",\"icon\":\"X\"}]}",
            _ => throw new InvalidOperationException($"Unsupported test resource '{resourceFileName}'.")
        };

        return new FileBasedIconProvider(Encoding.UTF8.GetBytes(json), resourceFileName);
    }
}
