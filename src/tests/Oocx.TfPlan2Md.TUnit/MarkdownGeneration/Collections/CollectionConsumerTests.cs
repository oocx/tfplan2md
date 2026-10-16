using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureDevOps;
using TUnit.Core;
using static Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections.CollectionPlanTestSupport;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections;

/// <summary>
/// Verifies consumers of attribute rows tolerate compact collection rows (TC-81 to TC-84, TC-108, TC-109).
/// Related feature: docs/features/148-compact-collection-diffs/test-plan.md.
/// </summary>
public class CollectionConsumerTests
{
    private const string Nbsp = " ";

    private static string[] Numbered(int count, string prefix = "v") => Enumerable.Range(0, count).Select(i => $"{prefix}{i:D2}").ToArray();

    [Test]
    public void Summary_CompactCollectionWithManyOccurrences_CountsAsOneAttribute()
    {
        var before = Numbered(50);
        var after = before.Skip(5).Concat(Numbered(5, "new")).ToArray();

        var change = BuildModel(PlanJson([Res(["update"], "{\"items\":" + L(before) + "}", "{\"items\":" + L(after) + "}")])).Changes.Single();

        change.ChangedAttributesSummary.Should().Be($"1{Nbsp}🔧{Nbsp}items");
    }

    [Test]
    public void Summary_CollectionPlusScalar_CountsTwoAndMatchesTableRows()
    {
        var markdown = Render(Res(["update"], "{\"items\":" + L("a") + ",\"n\":1}", "{\"items\":" + L("a", "b") + ",\"n\":2}"));
        var model = BuildModel(PlanJson([Res(["update"], "{\"items\":" + L("a") + ",\"n\":1}", "{\"items\":" + L("a", "b") + ",\"n\":2}")]));

        var change = model.Changes.Single();
        change.ChangedAttributesSummary.Should().StartWith($"2{Nbsp}🔧{Nbsp}");
        change.AttributeChanges.Should().HaveCount(2);
        markdown.Split('\n').Count(line => line.StartsWith("| items |", StringComparison.Ordinal) || line.StartsWith("| n |", StringComparison.Ordinal)).Should().Be(2);
    }

    [Test]
    public void Summary_CompactCollection_ConsistentAcrossSummaryAndDetailTable()
    {
        var resource = Res(["update"], "{\"a\":[\"x\"],\"b\":[\"y\"],\"c\":[\"z\"]}", "{\"a\":[\"x\",\"1\"],\"b\":[\"y\",\"2\"],\"c\":[\"z\",\"3\"]}");

        var change = BuildModel(PlanJson([resource])).Changes.Single();
        var tableRows = Render(resource).Split('\n').Count(line => line.StartsWith("| a |", StringComparison.Ordinal)
            || line.StartsWith("| b |", StringComparison.Ordinal) || line.StartsWith("| c |", StringComparison.Ordinal));

        change.ChangedAttributesSummary.Should().StartWith($"3{Nbsp}🔧{Nbsp}");
        tableRows.Should().Be(3);
    }

    [Test]
    public void ParentChildMerging_CompactRowNamedAsInlineAttribute_IsRemovedFromParentTable()
    {
        var module = new AzureDevOpsModule(
            azdoUserMapper: new AzdoUserMapper(new Dictionary<string, string>().ToFrozenDictionary(), diagnostics: null),
            azdoGroupMapper: new AzdoGroupMapper(new Dictionary<string, string>().ToFrozenDictionary(), diagnostics: null));
        var registry = new ProviderRegistry();
        registry.RegisterProvider(module);
        var resource = Res(
            ["update"],
            "{\"display_name\":\"g\",\"members\":[\"aad.one\"]}",
            "{\"display_name\":\"g2\",\"members\":[\"aad.one\",\"aad.two\"]}",
            address: "azuredevops_group.team",
            type: "azuredevops_group",
            provider: "registry.terraform.io/microsoft/azuredevops");
        var model = new ReportModelBuilder(new ReportModelBuilderOptions(), new ReportModelBuilderServices(ProviderRegistry: registry))
            .Build(new TerraformPlanParser().Parse(PlanJson([resource])));

        var change = model.Changes.Single();

        change.AttributeChanges.Select(a => a.Name).Should().Equal("display_name");
        change.ChildResourceGroups.Should().ContainSingle(group => group.Label == "Members");
    }

    [Test]
    public void FindingIndicator_SarifPathIndexedElement_MarksCompactRow()
    {
        var model = BuildModel(PlanJson([Res(["update"], "{\"items\":[\"a\"],\"other\":[\"x\"]}", "{\"items\":[\"a\",\"b\"],\"other\":[\"x\",\"y\"]}")]));
        var change = model.Changes.Single();
        change.CodeAnalysisFindings =
        [
            new CodeAnalysisFindingModel
            {
                Severity = "High",
                SeverityIcon = "⚠️",
                SeverityRank = 3,
                Message = "finding",
                AttributePath = "items[3]"
            }
        ];

        var markdown = new MarkdownRenderer().Render(model);

        markdown.Split('\n').Single(line => line.StartsWith("| items", StringComparison.Ordinal)).Should().Contain("⚠️");
        markdown.Split('\n').Single(line => line.StartsWith("| other", StringComparison.Ordinal)).Should().NotContain("⚠️");
    }
}
