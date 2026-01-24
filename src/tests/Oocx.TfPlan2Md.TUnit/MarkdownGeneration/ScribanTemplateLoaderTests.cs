using System;
using System.IO;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzureRM;
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
        var providerRegistry = CreateProviderRegistry();
        var loader = new ScribanTemplateLoader(
            coreTemplateResourcePrefix: "Oocx.TfPlan2Md.MarkdownGeneration.Templates.",
            providerTemplateResourcePrefixes: providerRegistry.GetTemplateResourcePrefixes());
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

    /// <summary>
    /// Verifies that ScribanTemplateLoader can load templates from multiple prefixes.
    /// Related feature: docs/features/047-provider-code-separation/specification.md.
    /// </summary>
    [Test]
    public void MultiPrefix_LoadsFromCoreFirst()
    {
        // Arrange - Use actual embedded resources
        var corePrefix = "Oocx.TfPlan2Md.MarkdownGeneration.Templates.";
        var providerPrefixes = new[] { "Oocx.TfPlan2Md.MarkdownGeneration.Templates." };
        var loader = new ScribanTemplateLoader(
            coreTemplateResourcePrefix: corePrefix,
            providerTemplateResourcePrefixes: providerPrefixes);

        // Act - Try to load a core template
        var result = loader.TryGetTemplate("default", out var template);

        // Assert - Core template should be loaded
        result.Should().BeTrue();
        template.Should().Contain("include \"_header.sbn\"");
    }

    /// <summary>
    /// Verifies that ScribanTemplateLoader can load provider-specific templates.
    /// Related feature: docs/features/047-provider-code-separation/specification.md.
    /// </summary>
    [Test]
    public void MultiPrefix_LoadsFromProviderWhenNotInCore()
    {
        // Arrange - Use actual embedded resources with correct provider prefix
        var corePrefix = "Oocx.TfPlan2Md.MarkdownGeneration.Templates.";
        var providerPrefixes = new[] { "Oocx.TfPlan2Md.Providers.AzureRM.Templates." };
        var loader = new ScribanTemplateLoader(
            coreTemplateResourcePrefix: corePrefix,
            providerTemplateResourcePrefixes: providerPrefixes);

        // Act - Try to load a provider-specific template (azurerm/role_assignment)
        var result = loader.TryGetTemplate("azurerm/role_assignment", out var template);

        // Assert - Provider template should be loaded
        result.Should().BeTrue();
        template.Should().Contain("azurerm_role_assignment");
    }

    /// <summary>
    /// Verifies that custom templates take precedence over all embedded resources.
    /// Related feature: docs/features/047-provider-code-separation/specification.md.
    /// </summary>
    [Test]
    public void MultiPrefix_CustomDirectoryTakesPrecedence()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            // Arrange - Create custom template that would otherwise be in provider
            var customTemplatePath = Path.Combine(tempDirectory, "azurerm");
            Directory.CreateDirectory(customTemplatePath);
            var customFile = Path.Combine(customTemplatePath, "role_assignment.sbn");
            File.WriteAllText(customFile, "custom-provider-template");

            var corePrefix = "Oocx.TfPlan2Md.MarkdownGeneration.Templates.";
            var providerPrefixes = new[] { "Oocx.TfPlan2Md.MarkdownGeneration.Templates." };
            var loader = new ScribanTemplateLoader(
                tempDirectory,
                coreTemplateResourcePrefix: corePrefix,
                providerTemplateResourcePrefixes: providerPrefixes);

            // Act
            var result = loader.TryGetTemplate("azurerm/role_assignment", out var template);

            // Assert - Custom template should be loaded
            result.Should().BeTrue();
            template.Should().Be("custom-provider-template");
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "tfplan2md-templates-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>
    /// Creates a ProviderRegistry with AzureRM provider for testing.
    /// </summary>
    private static ProviderRegistry CreateProviderRegistry()
    {
        var registry = new ProviderRegistry();
        registry.RegisterProvider(new AzureRMModule(
            largeValueFormat: LargeValueFormat.InlineDiff,
            principalMapper: new NullPrincipalMapper()));
        return registry;
    }
}
