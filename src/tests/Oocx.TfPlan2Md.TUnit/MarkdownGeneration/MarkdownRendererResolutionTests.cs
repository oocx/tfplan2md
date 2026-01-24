using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Diagnostics;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.RenderTargets;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Validates template resolution and error handling in <see cref="MarkdownRenderer"/>.
/// </summary>
public class MarkdownRendererResolutionTests
{
    /// <summary>
    /// Ensures built-in templates are resolved and recorded in diagnostics.
    /// </summary>
    [Test]
    public void Render_WithBuiltInTemplate_RecordsResolution()
    {
        var diagnosticContext = new DiagnosticContext();
        var renderer = new MarkdownRenderer(diagnosticContext: diagnosticContext);
        var model = CreateModel("azurerm_custom_resource");

        var result = renderer.Render(model, "summary");

        result.Should().NotBeNullOrWhiteSpace();
        diagnosticContext.TemplateResolutions.Should().Contain(
            entry => entry.ResourceType == "_main" && entry.TemplateSource.Contains("Built-in template", StringComparison.Ordinal));
    }

    /// <summary>
    /// Ensures custom template paths are resolved and recorded.
    /// </summary>
    [Test]
    public void Render_WithCustomTemplate_RecordsResolution()
    {
        var diagnosticContext = new DiagnosticContext();
        var renderer = new MarkdownRenderer(diagnosticContext: diagnosticContext);
        var model = CreateModel("azurerm_custom_resource");
        var templatePath = CreateTemplateFile("custom-template.sbn", "Custom Template Output");

        var result = renderer.Render(model, templatePath);

        result.Should().Contain("Custom Template Output");
        diagnosticContext.TemplateResolutions.Should().Contain(
            entry => entry.ResourceType == "_main" && entry.TemplateSource.Contains("Custom template", StringComparison.Ordinal));
    }

    /// <summary>
    /// Ensures missing templates surface a render exception.
    /// </summary>
    [Test]
    public void Render_WithMissingTemplate_ThrowsMarkdownRenderException()
    {
        var renderer = new MarkdownRenderer();
        var model = CreateModel("azurerm_custom_resource");

        var action = () => renderer.Render(model, "missing-template.sbn");

        action.Should().Throw<MarkdownRenderException>();
    }

    /// <summary>
    /// Ensures async template rendering reads custom templates from disk.
    /// </summary>
    [Test]
    public async Task RenderAsync_WithCustomTemplate_ReturnsContent()
    {
        var renderer = new MarkdownRenderer();
        var model = CreateModel("azurerm_custom_resource");
        var templatePath = CreateTemplateFile("async-template.sbn", "Async Template Output");

        var result = await renderer.RenderAsync(model, templatePath);

        result.Should().Contain("Async Template Output");
    }

    /// <summary>
    /// Ensures async resolution throws when templates are missing.
    /// </summary>
    [Test]
    public async Task RenderAsync_WithMissingTemplate_ThrowsMarkdownRenderException()
    {
        var renderer = new MarkdownRenderer();
        var model = CreateModel("azurerm_custom_resource");

        var action = () => renderer.RenderAsync(model, "missing-template.sbn");

        await action.Should().ThrowAsync<MarkdownRenderException>();
    }

    /// <summary>
    /// Ensures resource rendering returns null when no template matches.
    /// </summary>
    [Test]
    public void RenderResourceChange_WithUnknownResourceType_ReturnsNull()
    {
        var renderer = new MarkdownRenderer();
        var model = CreateModel("invalid");
        var change = model.Changes[0];

        var result = renderer.RenderResourceChange(change);

        result.Should().BeNull();
    }

    /// <summary>
    /// Ensures custom resource templates are discovered and rendered.
    /// </summary>
    [Test]
    public void RenderResourceChange_WithCustomTemplate_RendersOutput()
    {
        var diagnosticContext = new DiagnosticContext();
        var templateDirectory = CreateTemplateDirectory(
            ("azurerm/custom_resource.sbn", "### {{ change.address }}"));
        var renderer = new MarkdownRenderer(templateDirectory, diagnosticContext: diagnosticContext);
        var model = CreateModel("azurerm_custom_resource");
        var change = model.Changes[0];

        var result = renderer.RenderResourceChange(change, RenderTarget.GitHub);

        result.Should().Contain(change.Address);
        diagnosticContext.TemplateResolutions.Should().Contain(
            entry => entry.ResourceType == "azurerm_custom_resource" && entry.TemplateSource.Contains("Custom resource-specific template", StringComparison.Ordinal));
    }

    /// <summary>
    /// Ensures invalid resource templates surface a render exception.
    /// </summary>
    [Test]
    public void RenderResourceChange_WithInvalidTemplate_ThrowsMarkdownRenderException()
    {
        var templateDirectory = CreateTemplateDirectory(
            ("azurerm/bad_resource.sbn", "{{ include \"missing\" }}"));
        var renderer = new MarkdownRenderer(templateDirectory);
        var model = CreateModel("azurerm_bad_resource");
        var change = model.Changes[0];

        var action = () => renderer.RenderResourceChange(change);

        action.Should().Throw<MarkdownRenderException>();
    }

    /// <summary>
    /// Builds a minimal report model for template rendering tests.
    /// </summary>
    /// <param name="resourceType">Resource type used in the plan.</param>
    /// <returns>The constructed report model.</returns>
    private static ReportModel CreateModel(string resourceType)
    {
        using var document = JsonDocument.Parse("{\"name\":\"example\"}");
        var after = document.RootElement.Clone();
        var plan = new TerraformPlan(
            FormatVersion: "1.2",
            TerraformVersion: "1.6.0",
            ResourceChanges:
            [
                new ResourceChange(
                    Address: $"{resourceType}.example",
                    ModuleAddress: null,
                    Mode: "managed",
                    Type: resourceType,
                    Name: "example",
                    ProviderName: "provider.azurerm",
                    Change: new Change(
                        actions: ["create"],
                        before: null,
                        after: after,
                        afterUnknown: null,
                        beforeSensitive: null,
                        afterSensitive: null))
            ],
            Timestamp: null);

        var builder = new ReportModelBuilder();
        return builder.Build(plan);
    }

    /// <summary>
    /// Creates a custom template file under the workspace .tmp directory.
    /// </summary>
    /// <param name="fileName">Template file name.</param>
    /// <param name="content">Template content.</param>
    /// <returns>Absolute template file path.</returns>
    private static string CreateTemplateFile(string fileName, string content)
    {
        var root = CreateTempDirectory();
        var templatePath = Path.Combine(root, fileName);
        File.WriteAllText(templatePath, content);
        return templatePath;
    }

    /// <summary>
    /// Creates a template directory with provided relative template paths.
    /// </summary>
    /// <param name="templates">Template definitions to write.</param>
    /// <returns>Absolute directory path.</returns>
    private static string CreateTemplateDirectory(params (string RelativePath, string Content)[] templates)
    {
        var root = CreateTempDirectory();

        foreach (var (relativePath, content) in templates)
        {
            var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, content);
        }

        return root;
    }

    /// <summary>
    /// Creates a unique temp directory under the workspace .tmp folder.
    /// </summary>
    /// <returns>Absolute path to the created directory.</returns>
    private static string CreateTempDirectory()
    {
        var root = Path.Combine(GetRepoRoot(), ".tmp", "markdown-renderer-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    /// <summary>
    /// Resolves the repository root directory.
    /// </summary>
    /// <returns>Absolute path to the repo root.</returns>
    [SuppressMessage("Major Code Smell", "S1075", Justification = "Test helper uses a fixed repo marker to keep paths workspace-bound.")]
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
