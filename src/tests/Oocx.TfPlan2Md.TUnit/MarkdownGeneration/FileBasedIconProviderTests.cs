using System.Reflection;
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
        var assembly = typeof(FileBasedIconProviderTests).Assembly;
        var resourceName = BuildResourceName(assembly, resourceFileName);

        return new FileBasedIconProvider(resourceName, assembly);
    }

    /// <summary>
    /// Builds the embedded resource name for icon rule test data.
    /// </summary>
    /// <param name="assembly">The assembly containing the resources.</param>
    /// <param name="resourceFileName">The resource file name.</param>
    /// <returns>The fully qualified resource name.</returns>
    private static string BuildResourceName(Assembly assembly, string resourceFileName)
    {
        var baseName = assembly.GetName().Name ?? "Oocx.TfPlan2Md.TUnit";
        return $"{baseName}.TestData.IconRules.{resourceFileName}";
    }
}
