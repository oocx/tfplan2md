using System.Linq;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using TUnit.Core;
using static Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections.CollectionPlanTestSupport;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections;

/// <summary>
/// Model-level tests for per-entry, per-side sensitive masking of primitive collections
/// (TC-50, TC-51, TC-53, TC-54, TC-59, TC-120, TC-128, TC-129, TC-130).
/// Related feature: docs/features/148-compact-collection-diffs/test-plan.md.
/// </summary>
public class SensitiveCollectionModelTests
{
    private const string Secret1 = "SECRET-AAA-148";
    private const string Secret2 = "SECRET-BBB-148";

    private static AttributeChangeModel Row(Resource resource, ReportModelBuilderOptions? options = null)
        => BuildModel(PlanJson([resource]), options).Changes.Single().AttributeChanges.Single(row => row.Name == "items");

    private static string Items(params string[] values) => "{\"items\":" + L(values) + "}";

    private static string Flags(params bool[] flags) => "{\"items\":[" + string.Join(',', flags.Select(f => f ? "true" : "false")) + "]}";

    /// <summary>
    /// Writes every property of a row, including all element fields, so that a test can assert that no masked
    /// value is carried anywhere in the model.
    /// </summary>
    internal static string Dump(AttributeChangeModel row, bool includeSensitiveFlag = true)
    {
        var builder = new StringBuilder();
        builder.Append(row.Name).Append('|').Append(row.Before).Append('|').Append(row.After).Append('|')
            .Append(includeSensitiveFlag ? row.IsSensitive : null).Append('|').Append(row.IsLarge).Append('\n');
        if (row.Collection is { } collection)
        {
            builder.Append(collection.BeforeState).Append('|').Append(collection.AfterState).Append('|')
                .Append(collection.OrderChanged).Append('|').Append(collection.IsUnresolved).Append('|')
                .Append(collection.HasMixedKinds).Append('\n');
            foreach (var element in collection.Before.Concat(collection.After))
            {
                builder.Append(element.Index).Append('|').Append(element.Value).Append('|').Append(element.Kind).Append('|')
                    .Append(element.Status).Append('|').Append(element.IsMasked).Append('\n');
            }
        }

        return builder.ToString();
    }

    [Test]
    public void Update_OneSensitiveEntryChanged_MasksOnlyThatEntryAndKeepsMarkers()
    {
        var row = Row(Res(["update"], Items("a", Secret1, "b"), Items("a", Secret2, "c"),
            beforeSensitive: Flags(false, true, false), afterSensitive: Flags(false, true, false)));

        var collection = row.Collection!;
        collection.BeforeState.Should().Be(CollectionState.Elements);
        collection.AfterState.Should().Be(CollectionState.Elements);
        collection.Before.Select(e => e.Status).Should().Equal(
            CollectionElementStatus.Unchanged, CollectionElementStatus.Removed, CollectionElementStatus.Removed);
        collection.After.Select(e => e.Status).Should().Equal(
            CollectionElementStatus.Unchanged, CollectionElementStatus.Added, CollectionElementStatus.Added);
        collection.Before.Select(e => e.IsMasked).Should().Equal(false, true, false);
        collection.After.Select(e => e.IsMasked).Should().Equal(false, true, false);
        collection.Before[0].Value.Should().Be("a");
        collection.After[2].Value.Should().Be("c");
        row.IsSensitive.Should().BeTrue();
    }

    [Test]
    public void Update_MaskedEntries_ModelCarriesNoValueTypeOrLength()
    {
        var row = Row(Res(["update"], "{\"items\":[\"a\",\"" + Secret1 + "\",42]}", "{\"items\":[\"a\",\"" + Secret2 + "\",true]}",
            beforeSensitive: Flags(false, true, true), afterSensitive: Flags(false, true, true)));

        foreach (var element in row.Collection!.Before.Concat(row.Collection.After).Where(e => e.IsMasked))
        {
            element.Value.Should().BeNull();
            element.Kind.Should().Be(JsonValueKind.Undefined);
        }

        row.Collection.Before.Count(e => e.IsMasked).Should().Be(2);
        var dump = Dump(row);
        dump.Should().NotContain("SECRET").And.NotContain("42").And.NotContain("|Number|")
            .And.NotContain("|True|Removed").And.NotContain("|True|Added");
    }

    [Test]
    public void Update_PerEntryMasking_CanonicalTextHasBareSensitiveTokens()
    {
        var row = Row(Res(["update"], Items("a", Secret1, "b"), Items("a", Secret1, "c"),
            beforeSensitive: Flags(false, true, false), afterSensitive: Flags(false, true, false)));

        row.Before.Should().Be("[\"a\",(sensitive),\"b\"]");
        row.After.Should().Be("[\"a\",(sensitive),\"c\"]");
        row.Collection!.Before[1].Status.Should().Be(CollectionElementStatus.Unchanged);
        row.Collection.After[1].Status.Should().Be(CollectionElementStatus.Unchanged);
    }

    [Test]
    [Arguments("{\"items\":true}", "{\"items\":[false,false]}")]
    [Arguments("{\"items\":[false]}", "{\"items\":true}")]
    [Arguments("{\"items\":true}", "{\"items\":true}")]
    [Arguments("true", "{}")]
    public void Update_CollectionMarkedAsWholeOnEitherSide_MasksBothSidesWithoutElements(string beforeSensitive, string afterSensitive)
    {
        var row = Row(Res(["update"], Items(Secret1), Items(Secret1, Secret2),
            beforeSensitive: beforeSensitive, afterSensitive: afterSensitive));

        row.Collection!.BeforeState.Should().Be(CollectionState.Masked);
        row.Collection.AfterState.Should().Be(CollectionState.Masked);
        row.Collection.Before.Should().BeEmpty();
        row.Collection.After.Should().BeEmpty();
        row.Before.Should().Be("(sensitive)");
        row.After.Should().Be("(sensitive)");
        row.IsSensitive.Should().BeTrue();
        Dump(row).Should().NotContain("SECRET");
    }

    [Test]
    public void Update_VisibleValueBecomesMaskedWithSameValue_IsRemovedAndAdded()
    {
        var row = Row(Res(["update"], Items("x"), Items("x"), beforeSensitive: Flags(false), afterSensitive: Flags(true)));

        row.Collection!.Before.Single().Status.Should().Be(CollectionElementStatus.Removed);
        row.Collection.Before.Single().IsMasked.Should().BeFalse();
        row.Collection.After.Single().Status.Should().Be(CollectionElementStatus.Added);
        row.Collection.After.Single().IsMasked.Should().BeTrue();
    }

    [Test]
    public void Update_MaskedValueBecomesVisibleWithSameValue_IsRemovedAndAdded()
    {
        var row = Row(Res(["update"], Items("x"), Items("x"), beforeSensitive: Flags(true), afterSensitive: Flags(false)));

        row.Collection!.Before.Single().Should().Be(new CollectionElementModel(0, null, JsonValueKind.Undefined, CollectionElementStatus.Removed, IsMasked: true));
        row.Collection.After.Single().Should().Be(new CollectionElementModel(0, "x", JsonValueKind.String, CollectionElementStatus.Added));
    }

    [Test]
    public void Update_SensitiveOnOneSideOnly_MasksOnlyThatSide()
    {
        var row = Row(Res(["update"], Items("a", "visible-before"), Items("a", Secret1),
            beforeSensitive: Flags(false, false), afterSensitive: Flags(false, true)));

        row.Collection!.Before[1].Value.Should().Be("visible-before");
        row.Collection.Before[1].IsMasked.Should().BeFalse();
        row.Collection.After[1].IsMasked.Should().BeTrue();
        Dump(row).Should().NotContain("SECRET");
    }

    [Test]
    [Arguments("{\"items\":true}", "{\"items\":true}")]
    [Arguments("{\"items\":[false,true]}", "{\"items\":[false,true,false]}")]
    public void Update_SensitiveEntriesWithShowSensitive_RowEqualsRowWithoutSensitivity(string beforeSensitive, string afterSensitive)
    {
        var options = new ReportModelBuilderOptions(ShowSensitive: true);
        var sensitive = Row(Res(["update"], Items("a", "s"), Items("a", "s", "b"), beforeSensitive: beforeSensitive, afterSensitive: afterSensitive), options);
        var plain = Row(Res(["update"], Items("a", "s"), Items("a", "s", "b")), options);

        Dump(sensitive, includeSensitiveFlag: false).Should().Be(Dump(plain, includeSensitiveFlag: false));
        sensitive.IsSensitive.Should().BeTrue();
        sensitive.Collection!.Before.Concat(sensitive.Collection.After).Should().OnlyContain(e => !e.IsMasked);
    }

    [Test]
    public void Update_UnknownAndSensitiveEntry_UsesCombinedPlaceholderAndKnownMaskedEntryStaysMasked()
    {
        var row = Row(Res(
            ["update"],
            "{\"items\":[\"a\",\"b\",\"" + Secret1 + "\"]}",
            "{\"items\":[\"a\",null,\"" + Secret2 + "\"]}",
            afterUnknown: Flags(false, true, false),
            beforeSensitive: Flags(false, false, true),
            afterSensitive: Flags(false, true, true)));

        var collection = row.Collection!;
        collection.IsUnresolved.Should().BeTrue();
        collection.After[1].Status.Should().Be(CollectionElementStatus.Unknown);
        collection.After[1].Value.Should().Be("🔒(known after apply)");
        collection.After[2].IsMasked.Should().BeTrue();
        collection.Before[2].IsMasked.Should().BeTrue();
        collection.Before.Concat(collection.After).Select(e => e.Status)
            .Should().OnlyContain(s => s == CollectionElementStatus.Unchanged || s == CollectionElementStatus.Unknown);
        collection.OrderChanged.Should().BeFalse();
        Dump(row).Should().NotContain("SECRET");
    }

    [Test]
    public void Update_WholeUnknownAfterWithPerEntryMaskedBefore_KeepsPerEntryBeforeText()
    {
        var row = Row(Res(["update"], Items("a", Secret1), "{}", afterUnknown: "{\"items\":true}",
            beforeSensitive: Flags(false, true), afterSensitive: "{}"));

        row.Before.Should().Be("[\"a\",(sensitive)]");
        row.After.Should().Be("(known after apply)");
        row.Collection!.AfterState.Should().Be(CollectionState.Unknown);
        row.Collection.Before[0].Value.Should().Be("a");
        row.Collection.Before[1].IsMasked.Should().BeTrue();
        Dump(row).Should().NotContain("SECRET");
    }

    [Test]
    public void Update_MaskedEntryOfOtherKind_DoesNotMakeKindsMixed()
    {
        var row = Row(Res(["update"], "{\"items\":[\"1\",42]}", "{\"items\":[\"1\",43,\"2\"]}",
            beforeSensitive: Flags(false, true), afterSensitive: Flags(false, true, false)));

        row.Collection!.HasMixedKinds.Should().BeFalse();
    }

    [Test]
    public void Update_OnlyLargeElementIsMasked_RowIsNotLarge()
    {
        var large = new string('S', 150);
        var row = Row(Res(["update"], Items("a", large), Items("a", large, "b"),
            beforeSensitive: Flags(false, true), afterSensitive: Flags(false, true, false)));

        row.IsLarge.Should().BeFalse();
        row.Collection!.After.Select(e => e.Status).Should().Equal(
            CollectionElementStatus.Unchanged, CollectionElementStatus.Unchanged, CollectionElementStatus.Added);
    }

    [Test]
    public void Update_LargeVisibleElementBesideMaskedEntry_RowIsLarge()
    {
        var large = new string('v', 150);
        var row = Row(Res(["update"], Items(large, Secret1), Items(large, Secret2),
            beforeSensitive: Flags(false, true), afterSensitive: Flags(false, true)));

        row.IsLarge.Should().BeTrue();
    }
}
