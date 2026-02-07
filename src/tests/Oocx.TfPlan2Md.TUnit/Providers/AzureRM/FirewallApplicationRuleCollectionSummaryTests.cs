using System.Collections.Generic;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Providers.AzureRM.Models;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzureRM;

/// <summary>
/// Tests for firewall application rule collection summary logic to ensure branch coverage.
/// </summary>
public class FirewallApplicationRuleCollectionSummaryTests
{
    private const string Nbsp = "\u00A0";

    /// <summary>
    /// Verifies non-update actions return an empty summary.
    /// </summary>
    [Test]
    public void BuildChangedAttributesSummary_WhenNotUpdate_ReturnsEmpty()
    {
        var model = new FirewallApplicationRuleCollectionViewModel
        {
            RuleChanges =
            [
                CreateChangeRow(ActionIcons.Add, "`allow-github`")
            ]
        };

        var summary = FirewallApplicationRuleCollectionViewModelFactory.BuildChangedAttributesSummary(model, "create");

        summary.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies update summaries are empty when no rule changes are present.
    /// </summary>
    [Test]
    public void BuildChangedAttributesSummary_WhenNoChanges_ReturnsEmpty()
    {
        var model = new FirewallApplicationRuleCollectionViewModel
        {
            RuleChanges =
            [
                CreateChangeRow(ActionIcons.Unchanged, "`allow-github`")
            ]
        };

        var summary = FirewallApplicationRuleCollectionViewModelFactory.BuildChangedAttributesSummary(model, "update");

        summary.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies summaries truncate after three changes and append the remaining count.
    /// </summary>
    [Test]
    public void BuildChangedAttributesSummary_WhenMoreThanThreeChanges_Truncates()
    {
        var model = new FirewallApplicationRuleCollectionViewModel
        {
            RuleChanges =
            [
                CreateChangeRow(ActionIcons.Add, "`allow-github`"),
                CreateChangeRow(ActionIcons.Update, "`allow-microsoft`"),
                CreateChangeRow(ActionIcons.Delete, "`allow-old-site`"),
                CreateChangeRow(ActionIcons.Add, "`allow-azure`")
            ]
        };

        var summary = FirewallApplicationRuleCollectionViewModelFactory.BuildChangedAttributesSummary(model, "update");

        summary.Should().Be(
            $"4🔧{Nbsp}{ActionIcons.Add}{Nbsp}<code>allow-github</code>, {ActionIcons.Update}{Nbsp}<code>allow-microsoft</code>, {ActionIcons.Delete}{Nbsp}<code>allow-old-site</code>, +1 more");
    }

    /// <summary>
    /// Verifies summary formatting preserves rule names without backticks.
    /// </summary>
    [Test]
    public void BuildChangedAttributesSummary_WhenNameNotCodeWrapped_PreservesText()
    {
        var model = new FirewallApplicationRuleCollectionViewModel
        {
            RuleChanges =
            [
                CreateChangeRow(ActionIcons.Add, "allow-github")
            ]
        };

        var summary = FirewallApplicationRuleCollectionViewModelFactory.BuildChangedAttributesSummary(model, "update");

        summary.Should().Be($"1🔧{Nbsp}{ActionIcons.Add}{Nbsp}<code>allow-github</code>");
    }

    /// <summary>
    /// Creates a minimal rule change row for summary testing.
    /// </summary>
    /// <param name="change">Change symbol for the rule.</param>
    /// <param name="name">Rule name value.</param>
    /// <returns>Populated change row view model.</returns>
    private static FirewallApplicationRuleChangeRowViewModel CreateChangeRow(string change, string name)
    {
        return new FirewallApplicationRuleChangeRowViewModel
        {
            Change = change,
            Name = name,
            Protocols = string.Empty,
            SourceAddresses = string.Empty,
            SourceIpGroups = string.Empty,
            TargetFqdns = string.Empty,
            FqdnTags = string.Empty,
            Description = string.Empty
        };
    }
}
