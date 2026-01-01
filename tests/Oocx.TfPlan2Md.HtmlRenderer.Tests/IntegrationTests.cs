using System;
using Oocx.TfPlan2Md.HtmlRenderer;
using Oocx.TfPlan2Md.HtmlRenderer.Rendering;

namespace Oocx.TfPlan2Md.HtmlRenderer.Tests;

/// <summary>
/// Integration-level tests rendering real artifacts for both flavors.
/// Related feature: docs/features/027-markdown-html-rendering/specification.md
/// </summary>
public sealed class IntegrationTests
{
    /// <summary>
    /// Renders all markdown artifacts for both flavors to ensure end-to-end coverage.
    /// Related acceptance: TC-12.
    /// </summary>
    [Fact]
    public void Render_AllArtifacts_BothFlavors_Succeeds()
    {
        var repoRoot = GetRepoRoot();
        var artifactDir = Path.Combine(repoRoot, "artifacts");
        var files = Directory.GetFiles(artifactDir, "*.md", SearchOption.TopDirectoryOnly);
        var renderer = new MarkdownToHtmlRenderer(new MarkdigPipelineFactory());

        Assert.NotEmpty(files);

        foreach (var file in files)
        {
            var markdown = File.ReadAllText(file);
            foreach (var flavor in new[] { HtmlFlavor.GitHub, HtmlFlavor.AzureDevOps })
            {
                var html = renderer.RenderFragment(markdown, flavor);
                Assert.False(string.IsNullOrWhiteSpace(html));
                Assert.Contains("<h1", html, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>
    /// Validates core content for the comprehensive demo using Azure DevOps flavor.
    /// Related acceptance: TC-18 (structure approximation).
    /// </summary>
    [Fact]
    public void Render_ComprehensiveDemo_Azdo_CoreContentPresent()
    {
        var repoRoot = GetRepoRoot();
        var path = Path.Combine(repoRoot, "artifacts", "comprehensive-demo.md");
        var markdown = File.ReadAllText(path);
        var renderer = new MarkdownToHtmlRenderer(new MarkdigPipelineFactory());

        var html = renderer.RenderFragment(markdown, HtmlFlavor.AzureDevOps);

        Assert.Contains("Terraform Plan Report", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Resource Changes", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("azurerm_virtual_network", html, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates GitHub flavor output for the simple diff demo and ensures styles are stripped.
    /// Related acceptance: TC-17 and TC-05.
    /// </summary>
    [Fact]
    public void Render_SimpleDiff_GitHub_CoreContentAndNoStyles()
    {
        var repoRoot = GetRepoRoot();
        var path = Path.Combine(repoRoot, "artifacts", "comprehensive-demo-simple-diff.md");
        var markdown = File.ReadAllText(path);
        var renderer = new MarkdownToHtmlRenderer(new MarkdigPipelineFactory());

        var html = renderer.RenderFragment(markdown, HtmlFlavor.GitHub);

        Assert.Contains("Terraform Plan Report", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Resource Changes", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("style=", html, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Computes the repository root from the test execution directory.
    /// </summary>
    /// <returns>Absolute path to the repository root.</returns>
    private static string GetRepoRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }
}
