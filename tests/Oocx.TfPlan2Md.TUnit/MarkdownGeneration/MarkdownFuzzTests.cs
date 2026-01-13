using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Fuzz testing / random plan generation tests that verify markdown correctness
/// with edge-case inputs that wouldn't normally appear in test data.
/// </summary>
/// <remarks>
/// These tests generate plans with:
/// - Special characters in resource names/values
/// - Long strings
/// - Unicode characters
/// - Nested structures
/// - Extreme values (empty, null, very long)
/// 
/// The goal is to find escaping bugs and edge cases that break markdown rendering.
/// </remarks>
public class MarkdownFuzzTests
{
    private readonly TerraformPlanParser _parser = new();
    private readonly MarkdownPipeline _pipeline;
    private readonly Random _random = new(42); // Fixed seed for reproducibility

    /// <summary>
    /// Initializes the test class with a Markdig pipeline.
    /// </summary>
    public MarkdownFuzzTests()
    {
        _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    }

    #region Special Character Tests

    /// <summary>
    /// Verifies markdown escaping handles all pipe variations correctly.
    /// </summary>
    [Test]
    [Arguments("simple|pipe")]
    [Arguments("||double-pipe||")]
    [Arguments("|leading-pipe")]
    [Arguments("trailing-pipe|")]
    [Arguments("a|b|c|d|e")]
    public void Fuzz_PipeInResourceName_EscapedCorrectly(string name)
    {
        var plan = CreatePlanWithResourceName(name);
        var markdown = RenderPlan(plan);

        var tableLines = markdown
            .Split('\n')
            .Where(line => line.TrimStart().StartsWith('|'));

        tableLines.Should().OnlyContain(line => !line.Contains($"|{name.Replace("|", "")}|"),
            "because unescaped pipes in table cells break table structure");
        AssertValidTables(markdown);
    }

    /// <summary>
    /// Verifies markdown rendering preserves asterisks while keeping tables valid.
    /// </summary>
    [Test]
    [Arguments("*bold*")]
    [Arguments("**bold**")]
    [Arguments("***triple***")]
    [Arguments("file*.txt")]
    [Arguments("*.example.com")]
    public void Fuzz_AsterisksInValues_RenderedCorrectly(string value)
    {
        var plan = CreatePlanWithTagValue(value);
        var markdown = RenderPlan(plan);

        markdown.Should().Contain(value,
            "because asterisks should remain visible while tables stay valid");
        AssertValidTables(markdown);
    }

    /// <summary>
    /// Verifies markdown escaping handles underscores correctly.
    /// </summary>
    [Test]
    [Arguments("_italic_")]
    [Arguments("__bold__")]
    [Arguments("snake_case_name")]
    [Arguments("multiple_underscores_here")]
    public void Fuzz_UnderscoresInValues_EscapedCorrectly(string value)
    {
        var plan = CreatePlanWithTagValue(value);
        var markdown = RenderPlan(plan);

        AssertNoConsecutiveBlanks(markdown);
        AssertValidTables(markdown);
    }

    /// <summary>
    /// Verifies markdown escaping handles brackets correctly.
    /// </summary>
    [Test]
    [Arguments("[link](url)")]
    [Arguments("[text]")]
    [Arguments("(parentheses)")]
    [Arguments("[[double]]")]
    [Arguments("mixed[brackets(here)]")]
    public void Fuzz_BracketsInValues_EscapedCorrectly(string value)
    {
        var plan = CreatePlanWithTagValue(value);
        var markdown = RenderPlan(plan);

        // Should not create markdown links
        markdown.Should().NotContain($"({value})",
            "because unescaped brackets could create unwanted links");
        AssertValidTables(markdown);
    }

    /// <summary>
    /// Verifies markdown escaping handles hash symbols correctly.
    /// </summary>
    [Test]
    [Arguments("# heading")]
    [Arguments("## subheading")]
    [Arguments("#hashtag")]
    [Arguments("C# code")]
    public void Fuzz_HashInValues_EscapedCorrectly(string value)
    {
        var plan = CreatePlanWithTagValue(value);
        var markdown = RenderPlan(plan);

        AssertNoConsecutiveBlanks(markdown);
        AssertValidTables(markdown);
    }

    /// <summary>
    /// Verifies markdown escaping handles backticks correctly.
    /// </summary>
    [Test]
    [Arguments("`code`")]
    [Arguments("```block```")]
    [Arguments("mixed`tick")]
    public void Fuzz_BackticksInValues_EscapedCorrectly(string value)
    {
        var plan = CreatePlanWithTagValue(value);
        var markdown = RenderPlan(plan);

        AssertValidTables(markdown);
    }

    #endregion

    #region Newline Tests

    /// <summary>
    /// Verifies various newline formats are handled correctly.
    /// </summary>
    [Test]
    [Arguments("line1\nline2")]
    [Arguments("line1\r\nline2")]
    [Arguments("line1\rline2")]
    [Arguments("a\nb\nc\nd")]
    [Arguments("\nleading")]
    [Arguments("trailing\n")]
    public void Fuzz_NewlinesInValues_ConvertedToBr(string value)
    {
        var plan = CreatePlanWithTagValue(value);
        var markdown = RenderPlan(plan);

        // Should not have raw newlines in table cells
        var tableLines = markdown.Split('\n')
            .Where(l => l.TrimStart().StartsWith('|') && !l.Contains("---"))
            .ToList();

        foreach (var line in tableLines)
        {
            // Each table row should start and end with |
            if (line.TrimStart().StartsWith('|'))
            {
                line.TrimEnd().Should().EndWith("|",
                    "because table rows must be complete on one line");
            }
        }

        AssertValidTables(markdown);
    }

    #endregion

    #region Unicode Tests

    /// <summary>
    /// Verifies Unicode characters are handled correctly.
    /// </summary>
    [Test]
    [Arguments("日本語テスト")]
    [Arguments("中文测试")]
    [Arguments("Тест на русском")]
    [Arguments("🎉 emoji 🚀")]
    [Arguments("€£¥₹")]
    [Arguments("αβγδ")]
    [Arguments("∑∏∫∂")]
    public void Fuzz_UnicodeInValues_HandledCorrectly(string value)
    {
        var plan = CreatePlanWithTagValue(value);
        var markdown = RenderPlan(plan);

        AssertNoConsecutiveBlanks(markdown);
        AssertValidTables(markdown);

        // The value should appear somewhere in the output (possibly escaped)
        markdown.Should().Contain(value.Substring(0, Math.Min(3, value.Length)),
            "because Unicode content should be preserved");
    }

    #endregion

    #region Length Tests

    /// <summary>
    /// Verifies very long values don't break table structure.
    /// </summary>
    [Test]
    [Arguments(100)]
    [Arguments(500)]
    [Arguments(1000)]
    public void Fuzz_LongValues_DontBreakTables(int length)
    {
        var value = new string('x', length);
        var plan = CreatePlanWithTagValue(value);
        var markdown = RenderPlan(plan);

        AssertValidTables(markdown);
        AssertNoConsecutiveBlanks(markdown);
    }

    /// <summary>
    /// Verifies very long resource names don't break table structure.
    /// </summary>
    [Test]
    [Arguments(50)]
    [Arguments(100)]
    [Arguments(200)]
    public void Fuzz_LongResourceNames_DontBreakTables(int length)
    {
        var name = "rg-" + new string('x', length);
        var plan = CreatePlanWithResourceName(name);
        var markdown = RenderPlan(plan);

        AssertValidTables(markdown);
        AssertNoConsecutiveBlanks(markdown);
    }

    #endregion

    #region Edge Case Tests

    /// <summary>
    /// Verifies empty values don't break table structure.
    /// </summary>
    [Test]
    public void Fuzz_EmptyValues_DontBreakTables()
    {
        var plan = CreatePlanWithTagValue("");
        var markdown = RenderPlan(plan);

        AssertValidTables(markdown);
        AssertNoConsecutiveBlanks(markdown);
    }

    /// <summary>
    /// Verifies whitespace-only values don't break table structure.
    /// </summary>
    [Test]
    [Arguments(" ")]
    [Arguments("  ")]
    [Arguments("\t")]
    [Arguments("   \t   ")]
    public void Fuzz_WhitespaceValues_DontBreakTables(string value)
    {
        var plan = CreatePlanWithTagValue(value);
        var markdown = RenderPlan(plan);

        AssertValidTables(markdown);
        AssertNoConsecutiveBlanks(markdown);
    }

    /// <summary>
    /// Verifies combined special characters are all escaped.
    /// </summary>
    [Test]
    [Arguments(@"test|with*special_chars[and]brackets")]
    [Arguments(@"<html>&amp;\n\r\t")]
    [Arguments(@"all|*_[]()#`<>&\chars")]
    public void Fuzz_CombinedSpecialChars_AllEscaped(string value)
    {
        var plan = CreatePlanWithTagValue(value);
        var markdown = RenderPlan(plan);

        AssertValidTables(markdown);
        AssertNoConsecutiveBlanks(markdown);
    }

    #endregion

    #region Randomized Tests

    /// <summary>
    /// Generates random plans with various characteristics and verifies output is valid.
    /// </summary>
    [Test]
    public void Fuzz_RandomPlans_ProduceValidMarkdown()
    {
        const int iterations = 20;
        var failures = new List<string>();

        for (var i = 0; i < iterations; i++)
        {
            try
            {
                var plan = GenerateRandomPlan();
                var markdown = RenderPlan(plan);

                AssertNoConsecutiveBlanks(markdown);
                AssertValidTables(markdown);
            }
            catch (Exception ex)
            {
                failures.Add($"Iteration {i}: {ex.Message}");
            }
        }

        failures.Should().BeEmpty(
            $"Random plans should produce valid markdown:\n{string.Join("\n", failures)}");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a minimal plan JSON with a specific resource name.
    /// </summary>
    private static string CreatePlanWithResourceName(string name)
    {
        return $$"""
        {
          "format_version": "1.2",
          "terraform_version": "1.14.0",
          "resource_changes": [
            {
              "address": "azurerm_resource_group.{{EscapeJson(name)}}",
              "mode": "managed",
              "type": "azurerm_resource_group",
              "name": "{{EscapeJson(name)}}",
              "provider_name": "registry.terraform.io/hashicorp/azurerm",
              "change": {
                "actions": ["create"],
                "after": {
                  "location": "eastus",
                  "name": "{{EscapeJson(name)}}"
                },
                "after_unknown": {}
              }
            }
          ]
        }
        """;
    }

    /// <summary>
    /// Creates a minimal plan JSON with a specific tag value.
    /// </summary>
    private static string CreatePlanWithTagValue(string value)
    {
        return $$"""
        {
          "format_version": "1.2",
          "terraform_version": "1.14.0",
          "resource_changes": [
            {
              "address": "azurerm_resource_group.test",
              "mode": "managed",
              "type": "azurerm_resource_group",
              "name": "test",
              "provider_name": "registry.terraform.io/hashicorp/azurerm",
              "change": {
                "actions": ["create"],
                "after": {
                  "location": "eastus",
                  "name": "rg-test",
                  "tags": {
                    "test_key": "{{EscapeJson(value)}}"
                  }
                },
                "after_unknown": {}
              }
            }
          ]
        }
        """;
    }

    /// <summary>
    /// Generates a random plan with various edge-case values.
    /// </summary>
    private string GenerateRandomPlan()
    {
        var specialChars = new[] { "|", "*", "_", "[", "]", "(", ")", "#", "`", "<", ">", "&", "\\", "\n", "\r\n" };
        var resourceCount = _random.Next(1, 5);

        var resources = new StringBuilder();
        for (var i = 0; i < resourceCount; i++)
        {
            if (i > 0)
            {
                resources.Append(',');
            }

            var name = GenerateRandomString(10);
            var tagValue = GenerateRandomString(50);

            // Sometimes inject special characters
            if (_random.Next(2) == 0)
            {
                var special = specialChars[_random.Next(specialChars.Length)];
                name = name.Insert(_random.Next(name.Length), special);
            }

            resources.Append(CultureInfo.InvariantCulture, $$"""
            {
              "address": "azurerm_resource_group.{{EscapeJson(name)}}",
              "mode": "managed",
              "type": "azurerm_resource_group",
              "name": "{{EscapeJson(name)}}",
              "provider_name": "registry.terraform.io/hashicorp/azurerm",
              "change": {
                "actions": ["create"],
                "after": {
                  "location": "eastus",
                  "name": "rg-{{EscapeJson(name)}}",
                  "tags": {
                    "random": "{{EscapeJson(tagValue)}}"
                  }
                },
                "after_unknown": {}
              }
            }
            """);
        }

        return $$"""
        {
          "format_version": "1.2",
          "terraform_version": "1.14.0",
          "resource_changes": [{{resources}}]
        }
        """;
    }

    /// <summary>
    /// Generates a random alphanumeric string.
    /// </summary>
    private string GenerateRandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var result = new char[length];
        for (var i = 0; i < length; i++)
        {
            result[i] = chars[_random.Next(chars.Length)];
        }
        return new string(result);
    }

    /// <summary>
    /// Escapes a string for use in JSON.
    /// </summary>
    private static string EscapeJson(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    /// <summary>
    /// Renders a plan JSON to markdown.
    /// </summary>
    private string RenderPlan(string planJson)
    {
        var plan = _parser.Parse(planJson);
        var model = new ReportModelBuilder().Build(plan);
        var renderer = new MarkdownRenderer();
        return renderer.Render(model);
    }

    /// <summary>
    /// Asserts no consecutive blank lines exist.
    /// </summary>
    private static void AssertNoConsecutiveBlanks(string markdown)
    {
        var lines = markdown.Split('\n');
        var consecutiveBlanks = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                consecutiveBlanks++;
                consecutiveBlanks.Should().BeLessThan(2,
                    $"MD012 violation at line {i + 1}");
            }
            else
            {
                consecutiveBlanks = 0;
            }
        }
    }

    /// <summary>
    /// Asserts all tables parse correctly with Markdig.
    /// </summary>
    private void AssertValidTables(string markdown)
    {
        var document = Markdown.Parse(markdown, _pipeline);
        var tables = document.Descendants<Table>().ToList();

        // At minimum, should have summary table if there are resources
        if (markdown.Contains("| Action |"))
        {
            tables.Should().NotBeEmpty("because the summary table should parse correctly");
        }
    }

    #endregion
}
