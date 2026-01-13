using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using AwesomeAssertions;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Architecture validation tests for Scriban templates.
/// These tests enforce design constraints to prevent regression to patterns
/// that caused maintenance difficulties in the past.
/// </summary>
/// <remarks>
/// These tests were introduced as part of Feature 026 (Template Rendering Simplification)
/// to ensure that templates remain simple and maintainable. Key constraints enforced:
/// 
/// 1. No 'func' definitions - formatting logic belongs in C# ViewModels/helpers
/// 2. No anchor comments - the single-pass rendering eliminates the need for markers
/// 3. Line count limits - templates should focus on layout, not computation
/// 
/// See docs/features/026-template-rendering-simplification/architecture.md for rationale.
/// </remarks>
public partial class TemplateArchitectureTests
{
    private const string TemplateResourcePrefix = "Oocx.TfPlan2Md.MarkdownGeneration.Templates.";
    private const int MaxTemplateLines = 100;

    private static readonly Assembly TemplateAssembly = typeof(Oocx.TfPlan2Md.MarkdownGeneration.MarkdownRenderer).Assembly;

    /// <summary>
    /// Gets all embedded template resources.
    /// </summary>
    private static IEnumerable<string> GetAllTemplateResources()
    {
        return TemplateAssembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(TemplateResourcePrefix, StringComparison.Ordinal)
                && name.EndsWith(".sbn", StringComparison.Ordinal));
    }

    /// <summary>
    /// Reads template content from an embedded resource.
    /// </summary>
    private static string ReadTemplateContent(string resourceName)
    {
        using var stream = TemplateAssembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Extracts a friendly template name from the full resource name.
    /// </summary>
    private static string GetTemplateName(string resourceName)
    {
        return resourceName
            .Replace(TemplateResourcePrefix, "")
            .Replace(".sbn", "");
    }

    #region No Func Definitions

    /// <summary>
    /// Verifies that no template contains 'func' definitions.
    /// Formatting logic should be in C# ViewModels or helper classes, not templates.
    /// </summary>
    [Test]
    public void Templates_ShouldNotContainFuncDefinitions()
    {
        var templatesWithFuncs = new List<(string Name, IEnumerable<string> Violations)>();

        foreach (var resourceName in GetAllTemplateResources())
        {
            var content = ReadTemplateContent(resourceName);
            var templateName = GetTemplateName(resourceName);

            // Match 'func' followed by whitespace and identifier (function definition)
            var funcMatches = FuncDefinitionRegex().Matches(content);
            if (funcMatches.Count > 0)
            {
                var violations = funcMatches
                    .Cast<Match>()
                    .Select(m => m.Value.Trim())
                    .ToList();
                templatesWithFuncs.Add((templateName, violations));
            }
        }

        templatesWithFuncs.Should().BeEmpty(
            "templates should not contain 'func' definitions - formatting logic belongs in C# ViewModels. " +
            "Found violations: " + string.Join(
                ", ",
                templatesWithFuncs.Select(t => $"{t.Name}: [{string.Join(", ", t.Violations)}]")));
    }

    #endregion

    #region No Anchor Comments (Legacy Pattern)

    /// <summary>
    /// Verifies that no template contains legacy anchor comment patterns.
    /// The single-pass rendering eliminates the need for resource markers.
    /// </summary>
    /// <remarks>
    /// We still allow tfplan2md:resource-start/end comments for semantic purposes
    /// (they help with debugging and are harmless), but we prohibit the old
    /// generic "tfplan2md:anchor" pattern that was used for regex replacement.
    /// </remarks>
    [Test]
    public void Templates_ShouldNotContainLegacyAnchorComments()
    {
        var templatesWithAnchors = new List<(string Name, IEnumerable<string> Violations)>();

        foreach (var resourceName in GetAllTemplateResources())
        {
            var content = ReadTemplateContent(resourceName);
            var templateName = GetTemplateName(resourceName);

            // Match legacy anchor patterns (not resource-start/end which are allowed)
            var anchorMatches = LegacyAnchorRegex().Matches(content);
            if (anchorMatches.Count > 0)
            {
                var violations = anchorMatches
                    .Cast<Match>()
                    .Select(m => m.Value.Trim())
                    .ToList();
                templatesWithAnchors.Add((templateName, violations));
            }
        }

        templatesWithAnchors.Should().BeEmpty(
            "templates should not contain legacy anchor comments - single-pass rendering eliminates this pattern. " +
            "Found violations: " + string.Join(
                ", ",
                templatesWithAnchors.Select(t => $"{t.Name}: [{string.Join(", ", t.Violations)}]")));
    }

    #endregion

    #region Line Count Limits

    /// <summary>
    /// Verifies that all templates stay under the maximum line count.
    /// Templates should focus on layout, not computation.
    /// </summary>
    [Test]
    public void Templates_ShouldNotExceedMaximumLineCount()
    {
        var oversizedTemplates = new List<(string Name, int LineCount)>();

        foreach (var resourceName in GetAllTemplateResources())
        {
            var content = ReadTemplateContent(resourceName);
            var templateName = GetTemplateName(resourceName);
            var lineCount = content.Split('\n').Length;

            if (lineCount > MaxTemplateLines)
            {
                oversizedTemplates.Add((templateName, lineCount));
            }
        }

        oversizedTemplates.Should().BeEmpty(
            $"templates should not exceed {MaxTemplateLines} lines - " +
            "consider moving logic to C# ViewModels or splitting into partials. " +
            "Oversized templates: " + string.Join(
                ", ",
                oversizedTemplates.Select(t => $"{t.Name} ({t.LineCount} lines)")));
    }

    /// <summary>
    /// Documents the current line counts for all templates.
    /// This test always passes but provides visibility into template sizes.
    /// </summary>
    [Test]
    public async Task Templates_LineCountReport()
    {
        var report = new List<(string Name, int LineCount)>();

        foreach (var resourceName in GetAllTemplateResources().OrderBy(n => n))
        {
            var content = ReadTemplateContent(resourceName);
            var templateName = GetTemplateName(resourceName);
            var lineCount = content.Split('\n').Length;
            report.Add((templateName, lineCount));
        }

        // This test always passes - it's for documentation purposes
        // Output is visible in test runner details
        var totalLines = report.Sum(r => r.LineCount);
        var averageLines = report.Count > 0 ? totalLines / report.Count : 0;

        // Log to test output for visibility
        foreach (var (name, lines) in report.OrderByDescending(r => r.LineCount))
        {
            // Using Assert.True with a message to document in test output
            await Assert.That(true, $"Template '{name}': {lines} lines").IsTrue();
        }

        await Assert.That(true, $"Total: {report.Count} templates, {totalLines} total lines, {averageLines} average").IsTrue();
    }

    #endregion

    #region Template Discovery

    /// <summary>
    /// Verifies that the expected core templates are present.
    /// </summary>
    [Test]
    public void Templates_CoreTemplatesExist()
    {
        var expectedTemplates = new[]
        {
            "default",
            "summary",
            "_header",
            "_summary",
            "_resource",
            "azurerm.network_security_group",
            "azurerm.firewall_network_rule_collection",
            "azurerm.role_assignment"
        };

        var actualTemplates = GetAllTemplateResources()
            .Select(GetTemplateName)
            .ToHashSet();

        foreach (var expected in expectedTemplates)
        {
            actualTemplates.Should().Contain(expected,
                $"core template '{expected}' should be embedded in the assembly");
        }
    }

    #endregion

    #region Regex Patterns

    /// <summary>
    /// Matches Scriban 'func' definitions like: {{ func my_function }} or {{func foo}}
    /// </summary>
    [GeneratedRegex(@"\{\{[-~]?\s*func\s+\w+")]
    private static partial Regex FuncDefinitionRegex();

    /// <summary>
    /// Matches legacy anchor patterns that were used for regex replacement.
    /// Does NOT match resource-start/end which are allowed for semantic purposes.
    /// </summary>
    [GeneratedRegex(@"<!--\s*tfplan2md:anchor\b")]
    private static partial Regex LegacyAnchorRegex();

    #endregion
}
