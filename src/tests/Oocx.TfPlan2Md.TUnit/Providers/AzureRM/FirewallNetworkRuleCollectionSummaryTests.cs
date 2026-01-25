using System.Collections.Generic;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Providers.AzureRM.Models;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzureRM;

/// <summary>
/// Tests for firewall rule collection summary logic to ensure branch coverage.
/// </summary>
public class FirewallNetworkRuleCollectionSummaryTests
{
    private const string Nbsp = "\u00A0";

    /// <summary>
    /// Verifies non-update actions return an empty summary.
    /// </summary>
    [Test]
    public void BuildChangedAttributesSummary_WhenNotUpdate_ReturnsEmpty()
    {
        var model = new FirewallNetworkRuleCollectionViewModel
        {
            RuleChanges =
            [
                CreateChangeRow("➕", "`allow-dns`")
            ]
        };

        var summary = FirewallNetworkRuleCollectionViewModelFactory.BuildChangedAttributesSummary(model, "create");

        summary.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies update summaries are empty when no rule changes are present.
    /// </summary>
    [Test]
    public void BuildChangedAttributesSummary_WhenNoChanges_ReturnsEmpty()
    {
        var model = new FirewallNetworkRuleCollectionViewModel
        {
            RuleChanges =
            [
                CreateChangeRow("⏺️", "`allow-dns`")
            ]
        };

        var summary = FirewallNetworkRuleCollectionViewModelFactory.BuildChangedAttributesSummary(model, "update");

        summary.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies summaries truncate after three changes and append the remaining count.
    /// </summary>
    [Test]
    public void BuildChangedAttributesSummary_WhenMoreThanThreeChanges_Truncates()
    {
        var model = new FirewallNetworkRuleCollectionViewModel
        {
            RuleChanges =
            [
                CreateChangeRow("➕", "`allow-dns`"),
                CreateChangeRow("🔄", "`allow-http`"),
                CreateChangeRow("❌", "`allow-ssh-old`"),
                CreateChangeRow("➕", "`allow-redis`")
            ]
        };

        var summary = FirewallNetworkRuleCollectionViewModelFactory.BuildChangedAttributesSummary(model, "update");

        summary.Should().Be(
            $"4🔧{Nbsp}➕{Nbsp}<code>allow-dns</code>, 🔄{Nbsp}<code>allow-http</code>, ❌{Nbsp}<code>allow-ssh-old</code>, +1 more");
    }

    /// <summary>
    /// Verifies summary formatting preserves rule names without backticks.
    /// </summary>
    [Test]
    public void BuildChangedAttributesSummary_WhenNameNotCodeWrapped_PreservesText()
    {
        var model = new FirewallNetworkRuleCollectionViewModel
        {
            RuleChanges =
            [
                CreateChangeRow("➕", "allow-dns")
            ]
        };

        var summary = FirewallNetworkRuleCollectionViewModelFactory.BuildChangedAttributesSummary(model, "update");

        summary.Should().Be($"1🔧{Nbsp}➕{Nbsp}<code>allow-dns</code>");
    }

    /// <summary>
    /// Creates a minimal rule change row for summary testing.
    /// </summary>
    /// <param name="change">Change symbol for the rule.</param>
    /// <param name="name">Rule name value.</param>
    /// <returns>Populated change row view model.</returns>
    private static FirewallRuleChangeRowViewModel CreateChangeRow(string change, string name)
    {
        return new FirewallRuleChangeRowViewModel
        {
            Change = change,
            Name = name,
            Protocols = string.Empty,
            SourceAddresses = string.Empty,
            DestinationAddresses = string.Empty,
            DestinationPorts = string.Empty,
            Description = string.Empty
        };
    }
}
