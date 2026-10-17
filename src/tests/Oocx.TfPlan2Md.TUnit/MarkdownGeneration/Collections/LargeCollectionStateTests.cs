using AwesomeAssertions;
using TUnit.Core;
using static Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections.CollectionPlanTestSupport;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections;

/// <summary>
/// The large-value section keeps absent, <c>null</c> and <c>[]</c> distinct whenever the other side has elements,
/// exactly like the generic table (SC 9, SC 8; code review round 4, Blocker).
/// Related feature: docs/features/148-compact-collection-diffs/specification.md.
/// </summary>
public class LargeCollectionStateTests
{
    private static readonly string Large = new('x', 150);

    private static string WithItems(string? array) => array is null ? "{\"n\":1}" : "{\"n\":1,\"items\":" + array + "}";

    private static string Block(string label, params string[] lines)
        => $"{label}:\n\n```text\n{string.Join('\n', lines)}\n```";

    [Test]
    [Arguments(null, "-")]
    [Arguments("null", "null")]
    [Arguments("[]", "[]")]
    public void Update_ElementlessToLargeCollection_ShowsElementlessBeforeSide(string? before, string expected)
    {
        var markdown = Render(Res(["update"], WithItems(before), WithItems(L("a", Large))));

        markdown.Should().Contain("Large values: items");
        markdown.Should().Contain(Block("Before", expected));
        markdown.Should().Contain(Block("After", "➕ a", "➕ " + Large));
    }

    [Test]
    [Arguments(null, "-")]
    [Arguments("null", "null")]
    [Arguments("[]", "[]")]
    public void Update_LargeCollectionToElementless_ShowsElementlessAfterSide(string? after, string expected)
    {
        var markdown = Render(Res(["update"], WithItems(L("a", Large)), WithItems(after)));

        markdown.Should().Contain("Large values: items");
        markdown.Should().Contain(Block("Before", "➖ a", "➖ " + Large));
        markdown.Should().Contain(Block("After", expected));
    }

    [Test]
    public void Update_NullAndAbsentBeforeLargeCollection_RenderDifferently()
    {
        var fromNull = Render(Res(["update"], WithItems("null"), WithItems(L(Large))));
        var fromAbsent = Render(Res(["update"], WithItems(null), WithItems(L(Large))));

        fromNull.Should().NotBe(fromAbsent);
    }

    [Test]
    public void Update_LargeCollectionToUnknown_LabelsBothSides()
    {
        var markdown = Render(Res(["update"], WithItems(L(Large)), "{\"n\":1}", afterUnknown: "{\"items\":true}"));

        markdown.Should().Contain("Before:\n\n```text\n" + Large);
        markdown.Should().Contain("After:\n\n```text\n");
    }

    [Test]
    public void Update_LargeCollectionToUnknown_SummaryCountsUnknownAsChange()
    {
        var markdown = Render(Res(["update"], WithItems(L(Large)), "{\"n\":1}", afterUnknown: "{\"items\":true}"));

        markdown.Should().Contain("Large values: items (1 line, 1 change)");
        markdown.Should().NotContain("0 changes");
    }

    [Test]
    public void Update_LargeCollectionWithUnknownElement_SummaryCountsUnknownElementAsChange()
    {
        var markdown = Render(Res(["update"], WithItems(L(Large)), WithItems("[" + L(Large)[1..^1] + ",null]"),
            afterUnknown: "{\"items\":[false,true]}"));

        markdown.Should().Contain("Large values: items (2 lines, 1 change)");
    }

    [Test]
    public void Update_MaskedEntryBesideNullSide_ShowsNullAndPlaceholderOnly()
    {
        const string secret = "s3cr3t-value";
        var markdown = Render(Res(["update"], WithItems("null"), WithItems(L(Large, secret)),
            beforeSensitive: "{}", afterSensitive: "{\"items\":[false,true]}"));

        markdown.Should().Contain(Block("Before", "null"));
        markdown.Should().Contain(Block("After", "➕ " + Large, "➕ (sensitive)"));
        markdown.Should().NotContain(secret);
    }

    [Test]
    public void Delete_LargeCollection_RendersSingleBlockWithoutLabelsOrMarkers()
    {
        var markdown = Render(Res(["delete"], WithItems(L("a", Large)), null));

        markdown.Should().Contain("Large values: items");
        markdown.Should().Contain("```text\na\n" + Large + "\n```");
        markdown.Should().NotContain("Before:").And.NotContain("After:").And.NotContain("➖ ");
    }
}
