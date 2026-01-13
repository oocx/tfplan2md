using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Oocx.TfPlan2Md.Azure;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Template isolation tests that verify each template produces valid markdown
/// with controlled inputs. These tests isolate template behavior from the
/// complexity of full plan rendering.
/// </summary>
/// <remarks>
/// Each template (default, role_assignment, firewall_network_rule_collection, summary)
/// is tested independently to ensure:
/// - Proper table structure
/// - Correct escaping
/// - No blank line violations
/// - HTML tags balanced
/// 
/// This isolation helps pinpoint which template causes issues when problems occur.
/// </remarks>
public class TemplateIsolationTests
{
    private readonly TerraformPlanParser _parser = new();
    private readonly MarkdownPipeline _pipeline;

    /// <summary>
    /// Initializes the test class with a Markdig pipeline.
    /// </summary>
    public TemplateIsolationTests()
    {
        _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    }

    #region Default Template Tests

    /// <summary>
    /// Verifies the default template produces valid markdown for a simple create action.
    /// </summary>
    [Test]
    public void DefaultTemplate_CreateAction_ProducesValidMarkdown()
    {
        var json = File.ReadAllText("TestData/create-only-plan.json");
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder().Build(plan);
        var renderer = new MarkdownRenderer();

        var markdown = renderer.Render(model);

        AssertValidMarkdown(markdown, "default template with create actions");
    }

    /// <summary>
    /// Verifies the default template produces valid markdown for delete actions.
    /// </summary>
    [Test]
    public void DefaultTemplate_DeleteAction_ProducesValidMarkdown()
    {
        var json = File.ReadAllText("TestData/delete-only-plan.json");
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder().Build(plan);
        var renderer = new MarkdownRenderer();

        var markdown = renderer.Render(model);

        AssertValidMarkdown(markdown, "default template with delete actions");
    }

    /// <summary>
    /// Verifies the default template handles empty plans gracefully.
    /// </summary>
    [Test]
    public void DefaultTemplate_EmptyPlan_ProducesValidMarkdown()
    {
        var json = File.ReadAllText("TestData/empty-plan.json");
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder().Build(plan);
        var renderer = new MarkdownRenderer();

        var markdown = renderer.Render(model);

        // Empty plan should still produce valid markdown structure
        markdown.Should().Contain("# Terraform Plan");
        AssertNoConsecutiveBlanks(markdown);
    }

    /// <summary>
    /// Verifies the default template handles special characters correctly.
    /// </summary>
    [Test]
    public void DefaultTemplate_SpecialCharacters_EscapesCorrectly()
    {
        var json = File.ReadAllText("TestData/markdown-breaking-plan.json");
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder().Build(plan);
        var renderer = new MarkdownRenderer();

        var markdown = renderer.Render(model);

        AssertValidMarkdown(markdown, "default template with special characters");

        // Verify escaping worked
        markdown.Should().Contain(@"rg-with-pipe\|and*asterisk", "because pipes must be escaped while leaving benign characters readable");
        markdown.Should().NotContain("rg-with-pipe|and*asterisk", "because unescaped pipes break table structure");
    }

    #endregion

    #region Role Assignment Template Tests

    /// <summary>
    /// Verifies the role assignment template produces valid markdown.
    /// </summary>
    [Test]
    public void RoleAssignmentTemplate_WithPrincipals_ProducesValidMarkdown()
    {
        var json = File.ReadAllText("TestData/role-assignments.json");
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder().Build(plan);
        var renderer = new MarkdownRenderer(new PrincipalMapper(DemoPaths.DemoPrincipalsPath));

        var markdown = renderer.Render(model);

        AssertValidMarkdown(markdown, "role assignment template");

        // Verify role assignment specific elements remain readable
        markdown.Should().Contain("role_assignment");
    }

    /// <summary>
    /// Verifies role assignment template works without principal mapping.
    /// </summary>
    [Test]
    public void RoleAssignmentTemplate_WithoutPrincipals_ProducesValidMarkdown()
    {
        var json = File.ReadAllText("TestData/role-assignments.json");
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder().Build(plan);
        var renderer = new MarkdownRenderer(); // No principal mapper

        var markdown = renderer.Render(model);

        AssertValidMarkdown(markdown, "role assignment template without principals");
    }

    /// <summary>
    /// Verifies role assignment tables have no blank lines between rows.
    /// This was a specific bug that broke table rendering.
    /// </summary>
    [Test]
    public void RoleAssignmentTemplate_NoBlankLinesBetweenTableRows()
    {
        var json = File.ReadAllText("TestData/role-assignments.json");
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder().Build(plan);
        var renderer = new MarkdownRenderer(new PrincipalMapper(DemoPaths.DemoPrincipalsPath));

        var markdown = renderer.Render(model);

        // Find all table rows and verify no blank lines between them
        var tableRowPattern = new Regex(@"(?<=\|[^\n]*)\n[ \t]*\n(?=[ \t]*\|)");
        var matches = tableRowPattern.Matches(markdown);

        matches.Should().BeEmpty(
            "because blank lines between table rows break markdown table rendering");
    }

    #endregion

    #region Firewall Template Tests

    /// <summary>
    /// Verifies the firewall rule template produces valid markdown.
    /// </summary>
    [Test]
    public void FirewallTemplate_RuleChanges_ProducesValidMarkdown()
    {
        var json = File.ReadAllText("TestData/firewall-rule-changes.json");
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder().Build(plan);
        var renderer = new MarkdownRenderer();

        var markdown = renderer.Render(model);

        AssertValidMarkdown(markdown, "firewall template");
    }

    /// <summary>
    /// Verifies firewall template shows before/after rule comparisons.
    /// </summary>
    [Test]
    public void FirewallTemplate_ShowsRuleComparison()
    {
        var json = File.ReadAllText("TestData/firewall-rule-changes.json");
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder().Build(plan);
        var renderer = new MarkdownRenderer();

        var markdown = renderer.Render(model);

        // Firewall template should show rule details
        markdown.Should().Contain("firewall_network_rule_collection", "because firewall resources should be rendered");
    }

    #endregion

    #region Network Security Group Template Tests

    /// <summary>
    /// Verifies the NSG template produces valid markdown.
    /// </summary>
    [Test]
    public void NetworkSecurityGroupTemplate_ProducesValidMarkdown()
    {
        var json = File.ReadAllText("TestData/nsg-rule-changes.json");
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder().Build(plan);
        var renderer = new MarkdownRenderer();

        var markdown = renderer.Render(model);

        AssertValidMarkdown(markdown, "network security group template");
    }

    /// <summary>
    /// Verifies NSG tables contain no blank lines between rows.
    /// </summary>
    [Test]
    public void NetworkSecurityGroupTemplate_NoBlankLinesBetweenTableRows()
    {
        var json = File.ReadAllText("TestData/nsg-rule-changes.json");
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder().Build(plan);
        var renderer = new MarkdownRenderer();

        var markdown = renderer.Render(model);

        var tableRowPattern = new Regex(@"(?<=\|[^\n]*)\n[ \t]*\n(?=[ \t]*\|)");
        var matches = tableRowPattern.Matches(markdown);

        matches.Should().BeEmpty("because blank lines between table rows break markdown tables");
    }

    #endregion

    #region Summary Template Tests

    /// <summary>
    /// Verifies the summary template produces valid markdown.
    /// </summary>
    [Test]
    public void SummaryTemplate_ProducesValidMarkdown()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var model = new ReportModelBuilder().Build(plan);
        var renderer = new MarkdownRenderer();

        var markdown = renderer.Render(model, "summary");

        AssertValidMarkdown(markdown, "summary template");
        markdown.Should().Contain("Terraform Plan Summary");
    }

    /// <summary>
    /// Verifies summary template handles empty plans.
    /// </summary>
    [Test]
    public void SummaryTemplate_EmptyPlan_ProducesValidMarkdown()
    {
        var json = File.ReadAllText("TestData/empty-plan.json");
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder().Build(plan);
        var renderer = new MarkdownRenderer();

        var markdown = renderer.Render(model, "summary");

        AssertNoConsecutiveBlanks(markdown);
    }

    #endregion

    #region Multi-Module Tests

    /// <summary>
    /// Verifies multi-module plans render with proper heading hierarchy.
    /// </summary>
    [Test]
    public void MultiModule_ProperHeadingHierarchy()
    {
        var json = File.ReadAllText("TestData/multi-module-plan.json");
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder().Build(plan);
        var renderer = new MarkdownRenderer();

        var markdown = renderer.Render(model);

        AssertValidMarkdown(markdown, "multi-module template");

        // Verify heading hierarchy
        var document = Markdown.Parse(markdown, _pipeline);
        var headings = document.Descendants<HeadingBlock>().ToList();

        // Should have h1 (Terraform Plan) and h2 (sections) at minimum
        headings.Any(h => h.Level == 1).Should().BeTrue("because there should be an h1 heading");
        headings.Any(h => h.Level == 2).Should().BeTrue("because there should be h2 headings");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Asserts that the markdown is valid according to multiple criteria.
    /// </summary>
    private void AssertValidMarkdown(string markdown, string context)
    {
        // 1. No consecutive blank lines (MD012)
        AssertNoConsecutiveBlanks(markdown);

        // 2. All tables parse correctly
        AssertTablesParseCorrectly(markdown, context);

        // 3. HTML tags balanced
        AssertHtmlTagsBalanced(markdown, context);

        // 4. Headings surrounded by blank lines
        AssertHeadingSpacing(markdown, context);
    }

    /// <summary>
    /// Asserts no consecutive blank lines exist in the markdown.
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
                    $"MD012 violation: found consecutive blank lines at line {i + 1}");
            }
            else
            {
                consecutiveBlanks = 0;
            }
        }
    }

    /// <summary>
    /// Asserts all tables in the markdown parse correctly.
    /// </summary>
    private void AssertTablesParseCorrectly(string markdown, string context)
    {
        var document = Markdown.Parse(markdown, _pipeline);
        var tables = document.Descendants<Table>().ToList();

        // Count expected tables by markdown patterns
        var expectedAttributeTables = Regex.Matches(markdown, @"\| Attribute \|").Count;
        var expectedSummaryTable = markdown.Contains("| Action |") ? 1 : 0;

        tables.Count.Should().BeGreaterThanOrEqualTo(
            expectedAttributeTables + expectedSummaryTable,
            $"{context}: some tables failed to parse (broken structure)");
    }

    /// <summary>
    /// Asserts HTML tags are balanced.
    /// </summary>
    private static void AssertHtmlTagsBalanced(string markdown, string context)
    {
        var detailsOpen = Regex.Matches(markdown, @"<details(?:\s[^>]*)?>").Count;
        var detailsClose = Regex.Matches(markdown, @"</details>").Count;
        detailsOpen.Should().Be(detailsClose, $"{context}: unbalanced <details> tags");

        var summaryOpen = Regex.Matches(markdown, @"<summary(?:\s[^>]*)?>").Count;
        var summaryClose = Regex.Matches(markdown, @"</summary>").Count;
        summaryOpen.Should().Be(summaryClose, $"{context}: unbalanced <summary> tags");
    }

    /// <summary>
    /// Asserts headings have proper spacing.
    /// </summary>
    private static void AssertHeadingSpacing(string markdown, string context)
    {
        var lines = markdown.Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            if (Regex.IsMatch(lines[i], @"^#{1,6}\s"))
            {
                // Check line before (except for first line)
                if (i > 0)
                {
                    string.IsNullOrWhiteSpace(lines[i - 1]).Should().BeTrue(
                        $"{context}: heading at line {i + 1} should have blank line before it");
                }

                // Check line after (except for last line)
                if (i < lines.Length - 1)
                {
                    string.IsNullOrWhiteSpace(lines[i + 1]).Should().BeTrue(
                        $"{context}: heading at line {i + 1} should have blank line after it");
                }
            }
        }
    }

    #endregion
}
