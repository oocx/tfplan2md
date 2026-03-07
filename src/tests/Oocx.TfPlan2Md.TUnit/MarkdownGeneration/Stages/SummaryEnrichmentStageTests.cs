using System.Collections.Generic;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Stages;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Stages;

/// <summary>
/// Tests for the explicit summary-enrichment stage.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
public class SummaryEnrichmentStageTests
{
    /// <summary>
    /// Verifies the stage produces the expected per-action summary counts and totals.
    /// </summary>
    [Test]
    public void SummaryEnrichmentStage_Build_ProducesExpectedSummary()
    {
        var stage = new SummaryEnrichmentStage();
        var changes = new List<ResourceChangeModel>
        {
            CreateChange("type_a", "create"),
            CreateChange("type_b", "update"),
            CreateChange("type_c", "unknown"),
            CreateChange("type_d", "delete"),
            CreateChange("type_e", "forget"),
            CreateChange("type_f", "replace"),
            CreateChange("type_g", "no-op")
        };

        var summary = stage.Build(changes);

        summary.ToAdd.Count.Should().Be(1);
        summary.ToChange.Count.Should().Be(2);
        summary.ToDestroy.Count.Should().Be(2);
        summary.ToReplace.Count.Should().Be(1);
        summary.NoOp.Count.Should().Be(1);
        summary.Total.Should().Be(6);
    }

    /// <summary>
    /// Creates a minimal resource change model for summary-stage testing.
    /// </summary>
    /// <param name="type">The resource type.</param>
    /// <param name="action">The Terraform action.</param>
    /// <returns>The test resource change model.</returns>
    private static ResourceChangeModel CreateChange(string type, string action)
    {
        return new ResourceChangeModel
        {
            Address = $"{type}.example",
            ModuleAddress = string.Empty,
            Type = type,
            Name = "example",
            ProviderName = "provider",
            Action = action,
            ActionSymbol = " ",
            AttributeChanges = []
        };
    }
}
