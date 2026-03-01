using System.Reflection;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Architecture validation tests for the pure C# rendering pipeline.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
public class TemplateArchitectureTests
{
    /// <summary>
    /// Verifies that no embedded Scriban templates remain in the main assembly.
    /// </summary>
    [Test]
    public void RenderingArchitecture_ShouldNotEmbedScribanTemplates()
    {
        var assembly = typeof(MarkdownRenderer).Assembly;
        var templateResources = assembly
            .GetManifestResourceNames()
            .Where(name => name.EndsWith(".sbn", StringComparison.Ordinal))
            .ToList();

        templateResources.Should().BeEmpty("pure C# rendering should not ship Scriban templates");
    }
}
