using System.Diagnostics.CodeAnalysis;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests template resolution behavior for resource-specific templates.
/// </summary>
public class TemplateResolverTests
{
    /// <summary>
    /// Ensures invalid resource types fall back to the default template.
    /// </summary>
    [Test]
    public void ResolveTemplate_WithInvalidResourceType_ReturnsDefault()
    {
        var loader = new ScribanTemplateLoader();
        var resolver = new TemplateResolver(loader);

        var result = resolver.ResolveTemplate("invalid");

        result.Should().Be("_resource");
    }

    /// <summary>
    /// Ensures missing templates fall back to the default template.
    /// </summary>
    [Test]
    public void ResolveTemplate_WithMissingTemplate_ReturnsDefault()
    {
        var loader = new ScribanTemplateLoader();
        var resolver = new TemplateResolver(loader);

        var result = resolver.ResolveTemplate("azurerm_missing_resource");

        result.Should().Be("_resource");
    }

    /// <summary>
    /// Ensures existing templates are returned with the expected path.
    /// </summary>
    [Test]
    public void ResolveTemplate_WithCustomTemplate_ReturnsTemplatePath()
    {
        var templateDirectory = CreateTemplateDirectory("azurerm/custom_resource.sbn", "Custom");
        var loader = new ScribanTemplateLoader(templateDirectory);
        var resolver = new TemplateResolver(loader);

        var result = resolver.ResolveTemplate("azurerm_custom_resource");

        result.Should().Be("azurerm/custom_resource");
    }

    /// <summary>
    /// Creates a template directory containing a single template.
    /// </summary>
    /// <param name="relativePath">Template path relative to the directory.</param>
    /// <param name="content">Template content.</param>
    /// <returns>Absolute path to the directory.</returns>
    private static string CreateTemplateDirectory(string relativePath, string content)
    {
        var root = Path.Combine(GetRepoRoot(), ".tmp", "template-resolver-tests", Guid.NewGuid().ToString("N"));
        var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, content);
        return root;
    }

    /// <summary>
    /// Resolves the repository root directory.
    /// </summary>
    /// <returns>Absolute path to the repo root.</returns>
    [SuppressMessage("Major Code Smell", "S1075", Justification = "Test helper uses repo marker for workspace-bound paths.")]
    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
