using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.MarkdownGeneration.Stages;
using Oocx.TfPlan2Md.MarkdownGeneration.Summaries;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureRM;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Stages;

/// <summary>
/// Tests for the explicit attribute-filtering stage.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
public class AttributeFilteringStageTests
{
    private const string DisplayNameAttribute = "display_name";
    private const string ScopeBefore = "/subscriptions/ABC123/resourceGroups/my-rg";
    private const string ScopeAfter = "/subscriptions/abc123/resourceGroups/my-rg";

    /// <summary>
    /// Verifies Azure ID case-only attribute rows are removed by the extracted stage.
    /// </summary>
    [Test]
    public void AttributeFilteringStage_Build_SuppressesCaseChangeOnlyAttributes()
    {
        var filterRegistry = new AttributeChangeFilterRegistry();
        new AzureRMModule(LargeValueFormat.InlineDiff, new NullPrincipalMapper())
            .RegisterAttributeChangeFilters(filterRegistry);
        var stage = new AttributeFilteringStage(
            ignoreAzureIdCaseChanges: true,
            attributeChangeFilterRegistry: filterRegistry,
            summaryBuilder: new ResourceSummaryBuilder());
        var resourceChange = new ResourceChangeModel
        {
            Address = "azurerm_role_assignment.example",
            ModuleAddress = string.Empty,
            Type = "azurerm_role_assignment",
            Name = "example",
            ProviderName = "azurerm",
            Action = "update",
            ActionSymbol = ActionIcons.Update,
            AttributeChanges = new List<AttributeChangeModel>
            {
                new AttributeChangeModel
                {
                    Name = "scope",
                    Before = ScopeBefore,
                    After = ScopeAfter
                },
                new AttributeChangeModel
                {
                    Name = DisplayNameAttribute,
                    Before = "old-name",
                    After = "new-name"
                }
            },
            ChangedAttributesSummary = ResourceSummaryHtmlBuilder.BuildChangedAttributesSummary(
                new List<AttributeChangeModel>
                {
                    new AttributeChangeModel
                    {
                        Name = "scope",
                        Before = ScopeBefore,
                        After = ScopeAfter
                    },
                    new AttributeChangeModel
                    {
                        Name = DisplayNameAttribute,
                        Before = "old-name",
                        After = "new-name"
                    }
                },
                "update")
        };

        var filtered = stage.Build([resourceChange]);

        filtered.Should().ContainSingle();
        filtered[0].AttributeChanges.Should().HaveCount(1);
        filtered[0].AttributeChanges.Single().Name.Should().Be(DisplayNameAttribute);
        filtered[0].ChangedAttributesSummary.Should().Contain(DisplayNameAttribute);
        filtered[0].ChangedAttributesSummary.Should().NotContain("scope");
    }
}
