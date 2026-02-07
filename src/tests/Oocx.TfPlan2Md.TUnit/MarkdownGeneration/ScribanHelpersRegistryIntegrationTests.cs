using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.RenderTargets.GitHub;
using Scriban;
using Scriban.Runtime;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ScribanHelpersRegistryIntegrationTests
{
    [Test]
    public void RegisterHelpers_FormatValue_UsesValueFormatterRegistry()
    {
        var registry = new ValueFormatterRegistry();
        registry.Register(new MatchPattern(null, null, null, null), new FixedValueFormatter("formatted"));

        var output = RenderTemplate(
            "{{ format_value value provider }}",
            new ScriptObject
            {
                ["value"] = "raw",
                ["provider"] = "test"
            },
            registry,
            null);

        output.Should().Be("formatted");
    }

    [Test]
    public void RegisterHelpers_FormatAttributeValueTable_UsesIconProviderRegistry()
    {
        var registry = new IconProviderRegistry();
        registry.Register(new MatchPattern(null, null, null, null), new FixedIconProvider("⭐"));

        var output = RenderTemplate(
            "{{ format_attribute_value_table name value provider }}",
            new ScriptObject
            {
                ["name"] = "name",
                ["value"] = "value",
                ["provider"] = "test"
            },
            null,
            registry);

        output.Should().Be("`⭐\u00A0value`");
    }

    private static string RenderTemplate(
        string templateText,
        ScriptObject scriptObject,
        ValueFormatterRegistry? valueFormatterRegistry,
        IconProviderRegistry? iconProviderRegistry)
    {
        var template = Template.Parse(templateText, "registry-test");
        var context = new TemplateContext();

        ScribanHelpers.RegisterHelpers(
            scriptObject,
            new NullPrincipalMapper(),
            new GitHubDiffFormatter(),
            valueFormatterRegistry,
            iconProviderRegistry);

        context.PushGlobal(scriptObject);
        return template.Render(context);
    }

    private sealed class FixedValueFormatter : IValueFormatter
    {
        private readonly string _formatted;

        public FixedValueFormatter(string formatted)
        {
            _formatted = formatted;
        }

        public string? TryFormat(ServiceResolutionContext context)
        {
            return _formatted;
        }
    }

    private sealed class FixedIconProvider : IIconProvider
    {
        private readonly string _icon;

        public FixedIconProvider(string icon)
        {
            _icon = icon;
        }

        public string? TryGetIcon(ServiceResolutionContext context)
        {
            return _icon;
        }
    }
}
