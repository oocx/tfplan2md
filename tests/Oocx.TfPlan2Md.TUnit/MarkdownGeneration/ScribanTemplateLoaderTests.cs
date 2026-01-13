using System;
using System.IO;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Scriban;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ScribanTemplateLoaderTests
{
    [Test]
    public void BuiltInTemplate_IsLoaded()
    {
        var loader = new ScribanTemplateLoader();

        var result = loader.TryGetTemplate("default", out var template);

        result.Should().BeTrue();
        template.Should().Contain("include \"_header.sbn\"");
    }

    [Test]
    public void CustomDirectory_OverridesBuiltInTemplate()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var customTemplatePath = Path.Combine(tempDirectory, "default.sbn");
            File.WriteAllText(customTemplatePath, "custom-template");
            var loader = new ScribanTemplateLoader(tempDirectory);

            var result = loader.TryGetTemplate("default", out var template);

            result.Should().BeTrue();
            template.Should().Be("custom-template");
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Test]
    public void Loader_SupportsPartialsViaInclude()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var partialPath = Path.Combine(tempDirectory, "_partial.sbn");
            File.WriteAllText(partialPath, "partial-content");
            var loader = new ScribanTemplateLoader(tempDirectory);
            var template = Template.Parse("{{ include '_partial' }}", "main");
            var context = new TemplateContext
            {
                TemplateLoader = loader
            };

            var result = template.Render(context);

            result.Should().Contain("partial-content");
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Test]
    public void ResolveTemplate_ReturnsExistingResourceTemplate()
    {
        var loader = new ScribanTemplateLoader();
        var resolver = new TemplateResolver(loader);

        var result = resolver.ResolveTemplate("azurerm_role_assignment");

        result.Should().Be("azurerm/role_assignment");
    }

    [Test]
    public void ResolveTemplate_FallsBackToDefault()
    {
        var loader = new ScribanTemplateLoader();
        var resolver = new TemplateResolver(loader);

        var result = resolver.ResolveTemplate("unknown_provider_resource");

        result.Should().Be("_resource");
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "tfplan2md-templates-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        return directory;
    }
}
