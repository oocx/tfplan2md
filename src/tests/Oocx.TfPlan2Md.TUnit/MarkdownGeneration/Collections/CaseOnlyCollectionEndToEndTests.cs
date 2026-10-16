using System.Linq;
using AwesomeAssertions;
using TUnit.Core;
using static Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections.CollectionPlanTestSupport;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections;

/// <summary>
/// The case-only Azure resource ID rule for collections, through the services the command line composes
/// (SC 27; TC-131, TC-132).
/// Related feature: docs/features/148-compact-collection-diffs/test-plan.md.
/// </summary>
public class CaseOnlyCollectionEndToEndTests
{
    private static string Id(string group, string name = "vnet")
        => $"/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/{group}/providers/Microsoft.Network/virtualNetworks/{name}";

    private static Resource Thing(string[] before, string[] after, string address = "azurerm_thing.main", string beforeName = "a", string afterName = "b")
        => Res(
            ["update"],
            "{\"name\":\"" + beforeName + "\",\"subnet_ids\":" + L(before) + "}",
            "{\"name\":\"" + afterName + "\",\"subnet_ids\":" + L(after) + "}",
            address: address,
            type: "azurerm_thing",
            provider: "registry.terraform.io/hashicorp/azurerm");

    private static readonly string[] Upper = [Id("RG-A"), Id("RG-B")];
    private static readonly string[] Lower = [Id("rg-a"), Id("rg-b")];

    [Test]
    [Arguments("")]
    [Arguments("--show-unchanged-values")]
    [Arguments("--ignore-azure-id-case-changes")]
    public void Update_CollectionDiffersOnlyInAzureIdCase_HiddenEverywhere(string args)
    {
        var (model, markdown) = RenderWithCliDefaults(
            PlanJson([Thing(Upper, Lower)], [Thing(Upper, Lower, "azurerm_thing.drifted")]),
            args.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        markdown.Should().NotContain("subnet_ids").And.NotContain("changed collection");
        var resource = model.Changes.Single();
        resource.AttributeChanges.Select(a => a.Name).Should().NotContain("subnet_ids");
        resource.ChangedAttributesSummary.Should().NotContain("subnet_ids");
        model.CollectionDrift.Should().BeEmpty();
    }

    [Test]
    public void Update_CollectionDiffersOnlyInAzureIdCase_WithFilterDisabled_RowShown()
    {
        var (model, markdown) = RenderWithCliDefaults(
            PlanJson([Thing(Upper, Lower)], [Thing(Upper, Lower, "azurerm_thing.drifted")]), "--no-ignore-azure-id-case-changes");

        markdown.Should().Contain("| subnet_ids |");
        markdown.Should().Contain("1 changed collection: <code>subnet_ids</code>");
        model.Changes.Single().AttributeChanges.Select(a => a.Name).Should().Contain("subnet_ids");
    }

    /// <summary>
    /// A case-only difference combined with: an added member, a removed member, an order change, a non-case change
    /// at another position, and an unrelated element change.
    /// </summary>
    private static (string[] Before, string[] After) OtherChange(string scenario) => scenario switch
    {
        "added" => (Upper, [.. Lower, Id("rg-c")]),
        "removed" => ([.. Upper, Id("RG-C")], Lower),
        "order" => ([Id("RG-A"), Id("rg-a")], [Id("rg-a"), Id("RG-A")]),
        "non-case" => ([Id("RG-A"), Id("rg-x")], [Id("rg-a"), Id("rg-y")]),
        _ => ([Id("RG-A"), Id("rg-b", "one")], [Id("rg-a"), Id("rg-b", "two")]),
    };

    [Test]
    [Arguments("added")]
    [Arguments("removed")]
    [Arguments("order")]
    [Arguments("non-case")]
    [Arguments("unrelated")]
    public void Update_CaseOnlyDifferencePlusOtherChange_ShowsRowAndDriftEntry(string scenario)
    {
        var lists = OtherChange(scenario);
        var planned = Thing(lists.Before, lists.After);
        var drifted = Res(
            ["update"],
            "{\"subnet_ids\":" + L(lists.Before) + ",\"other_ids\":" + L(Upper) + "}",
            "{\"subnet_ids\":" + L(lists.After) + ",\"other_ids\":" + L(Lower) + "}",
            address: "azurerm_thing.drifted",
            type: "azurerm_thing",
            provider: "registry.terraform.io/hashicorp/azurerm");

        var (model, markdown) = RenderWithCliDefaults(PlanJson([planned], [drifted]));

        markdown.Should().Contain("| subnet_ids |");
        model.Changes.Single().AttributeChanges.Select(a => a.Name).Should().Contain("subnet_ids");
        markdown.Should().Contain("1 changed collection: <code>subnet_ids</code>");
        model.CollectionDrift.Single().Paths.Select(p => p.Path).Should().Equal("subnet_ids");
    }
}
