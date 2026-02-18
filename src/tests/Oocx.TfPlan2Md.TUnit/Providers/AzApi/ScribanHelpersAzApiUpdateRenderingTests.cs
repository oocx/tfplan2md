using System.Globalization;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using TUnit.Core;
using AzApiHelpers = Oocx.TfPlan2Md.Providers.AzApi.ScribanHelpers;

namespace Oocx.TfPlan2Md.Tests.Providers.AzApi;

/// <summary>
/// Tests update-mode rendering for azapi body helpers.
/// Related feature: docs/features/028-azapi-resource-template/specification.md.
/// </summary>
public class ScribanHelpersAzApiUpdateRenderingTests
{
    private static readonly int[] AllArrayIndexes = [0, 1, 2, 3, 4, 5];
    private static readonly int[] TwoChangedIndexes = [1, 4];

    /// <summary>
    /// Ensures nested changes and large values render the expected sections.
    /// </summary>
    [Test]
    public void RenderAzapiBody_UpdateMode_RendersNestedAndLargeSections()
    {
        var before = BuildUpdateDocument("old", "x", "y", "z", "w", "small", new string('a', 10));
        var after = BuildUpdateDocument("new", "x1", "y1", "z1", "w1", "small2", new string('b', 260));

        var markdown = AzApiHelpers.RenderAzapiBody(
            bodyJson: after,
            heading: "Body Changes",
            mode: "update",
            beforeJson: before,
            beforeSensitive: null,
            afterSensitive: null,
            showUnchanged: false,
            largeValueFormat: "simple-diff");

        markdown.Should().Contain("#### Body Changes");
        markdown.Should().Contain("| Property | Before | After |");
        markdown.Should().Contain("###### `settings`");
        markdown.Should().Contain("Large body property changes");
        markdown.Should().Contain("**large:**");
    }

    /// <summary>
    /// Ensures unchanged updates emit the no-changes message.
    /// </summary>
    [Test]
    public void RenderAzapiBody_UpdateMode_NoChanges_EmitsNoChangesMessage()
    {
        var before = BuildUpdateDocument("same", "a", "b", "c", "d", "small", "short");
        var after = BuildUpdateDocument("same", "a", "b", "c", "d", "small", "short");

        var markdown = AzApiHelpers.RenderAzapiBody(
            bodyJson: after,
            heading: "Body Changes",
            mode: "update",
            beforeJson: before,
            beforeSensitive: null,
            afterSensitive: null,
            showUnchanged: false,
            largeValueFormat: "inline-diff");

        markdown.Should().Contain("*No body changes detected*");
    }

    /// <summary>
    /// Ensures that when only one array item changes in a nested array, only that item is rendered.
    /// Regression test for issue #089: Nested array changes should show only changed items, not all items.
    /// Related issue: docs/issues/089-nested-array-shows-all-items/analysis.md.
    /// </summary>
    [Test]
    public void RenderAzapiBody_UpdateMode_NestedArrayChange_ShowsOnlyChangedArrayItem()
    {
        // Create a document with an array of 6 items (simulating policyRule.if.allOf[0-5])
        // Only item [4] will have a change (adding a new nested value)
        var before = BuildDocumentWithArrayItems(arrayItemCount: 6, changedItemIndex: null, nestedChangeCount: null);
        var after = BuildDocumentWithArrayItems(arrayItemCount: 6, changedItemIndex: 4, nestedChangeCount: 4);

        var markdown = AzApiHelpers.RenderAzapiBody(
            bodyJson: after,
            heading: "Body Changes",
            mode: "update",
            beforeJson: before,
            beforeSensitive: null,
            afterSensitive: null,
            showUnchanged: false,
            largeValueFormat: "simple-diff");

        // Should contain the array section
        markdown.Should().Contain("`policyRule.if.allOf` Array");

        // Should contain only the changed item [4] in the Index column
        markdown.Should().Contain("| [4] |");

        // Should NOT contain unchanged items in the Index column
        markdown.Should().NotContain("| [0] |");
        markdown.Should().NotContain("| [1] |");
        markdown.Should().NotContain("| [2] |");
        markdown.Should().NotContain("| [3] |");
        markdown.Should().NotContain("| [5] |");

        // Should show the nested changes (in[0-3])
        markdown.Should().Contain("in[0]");
        markdown.Should().Contain("in[3]");
    }

    /// <summary>
    /// Ensures that when multiple array items change, all changed items are rendered.
    /// </summary>
    [Test]
    public void RenderAzapiBody_UpdateMode_NestedArrayChange_ShowsAllChangedArrayItems()
    {
        // Create a document with array where items [1] and [4] have changes
        var before = BuildDocumentWithMultipleArrayChanges(arrayItemCount: 6, changedIndexes: null);
        var after = BuildDocumentWithMultipleArrayChanges(arrayItemCount: 6, changedIndexes: TwoChangedIndexes);

        var markdown = AzApiHelpers.RenderAzapiBody(
            bodyJson: after,
            heading: "Body Changes",
            mode: "update",
            beforeJson: before,
            beforeSensitive: null,
            afterSensitive: null,
            showUnchanged: false,
            largeValueFormat: "simple-diff");

        // Should contain the array section
        markdown.Should().Contain("`policyRule.if.allOf` Array");

        // Should contain both changed items in the Index column
        markdown.Should().Contain("| [1] |");
        markdown.Should().Contain("| [4] |");

        // Should NOT contain unchanged items in the Index column
        markdown.Should().NotContain("| [0] |");
        markdown.Should().NotContain("| [2] |");
        markdown.Should().NotContain("| [3] |");
        markdown.Should().NotContain("| [5] |");

        // Should show the changes (modified values)
        markdown.Should().Contain("changedValue1");
        markdown.Should().Contain("changedValue4");
    }

    /// <summary>
    /// Ensures that when all array items change, all items are rendered.
    /// </summary>
    [Test]
    public void RenderAzapiBody_UpdateMode_NestedArrayChange_ShowsAllItemsWhenAllChanged()
    {
        var before = BuildDocumentWithMultipleArrayChanges(arrayItemCount: 6, changedIndexes: null);
        var after = BuildDocumentWithMultipleArrayChanges(
            arrayItemCount: 6,
            changedIndexes: AllArrayIndexes);

        var markdown = AzApiHelpers.RenderAzapiBody(
            bodyJson: after,
            heading: "Body Changes",
            mode: "update",
            beforeJson: before,
            beforeSensitive: null,
            afterSensitive: null,
            showUnchanged: false,
            largeValueFormat: "simple-diff");

        // Should contain the array section
        markdown.Should().Contain("`policyRule.if.allOf` Array");

        // Should contain all items in the Index column
        markdown.Should().Contain("| [0] |");
        markdown.Should().Contain("| [1] |");
        markdown.Should().Contain("| [2] |");
        markdown.Should().Contain("| [3] |");
        markdown.Should().Contain("| [4] |");
        markdown.Should().Contain("| [5] |");

        // Should show all changed values
        markdown.Should().Contain("changedValue0");
        markdown.Should().Contain("changedValue5");
    }

    /// <summary>
    /// Builds a JSON element used for update comparisons.
    /// </summary>
    /// <param name="name">Name value.</param>
    /// <param name="settingA">Nested setting A value.</param>
    /// <param name="settingB">Nested setting B value.</param>
    /// <param name="settingC">Nested setting C value.</param>
    /// <param name="settingD">Nested setting D value.</param>
    /// <param name="small">Small value.</param>
    /// <param name="large">Large value.</param>
    /// <returns>A cloned JSON element for the update body.</returns>
    private static JsonElement BuildUpdateDocument(
        string name,
        string settingA,
        string settingB,
        string settingC,
        string settingD,
        string small,
        string large)
    {
        using var document = JsonDocument.Parse($$"""
            {
              "properties": {
                "name": "{{name}}",
                "settings": {
                  "a": "{{settingA}}",
                  "b": "{{settingB}}",
                  "c": "{{settingC}}",
                  "d": "{{settingD}}"
                },
                "small": "{{small}}",
                "large": "{{large}}"
              }
            }
            """);

        return document.RootElement.Clone();
    }

    /// <summary>
    /// Builds a JSON document with nested array items simulating Azure Policy allOf structure.
    /// </summary>
    /// <param name="arrayItemCount">Number of array items to create.</param>
    /// <param name="changedItemIndex">Index of the item that has a change (null for no changes).</param>
    /// <param name="nestedChangeCount">Number of nested values in the changed item (null for before state).</param>
    /// <returns>A cloned JSON element with array structure.</returns>
    private static JsonElement BuildDocumentWithArrayItems(
        int arrayItemCount,
        int? changedItemIndex,
        int? nestedChangeCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"properties\": {");
        sb.AppendLine("    \"policyRule\": {");
        sb.AppendLine("      \"if\": {");
        sb.AppendLine("        \"allOf\": [");

        for (var i = 0; i < arrayItemCount; i++)
        {
            sb.Append("          {");
            sb.Append(CultureInfo.InvariantCulture, $"\"field\": \"property{i}\", ");
            sb.Append(CultureInfo.InvariantCulture, $"\"equals\": \"value{i}\"");

            // If this is the changed item and we're building the "after" state
            if (i == changedItemIndex && nestedChangeCount.HasValue)
            {
                sb.Append(", \"in\": [");
                for (var j = 0; j < nestedChangeCount.Value; j++)
                {
                    if (j > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(CultureInfo.InvariantCulture, $"{j}");
                }

                sb.Append(']');
            }

            sb.Append('}');
            if (i < arrayItemCount - 1)
            {
                sb.Append(',');
            }

            sb.AppendLine();
        }

        sb.AppendLine("        ]");
        sb.AppendLine("      }");
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine("}");

        using var document = JsonDocument.Parse(sb.ToString());
        return document.RootElement.Clone();
    }

    /// <summary>
    /// Builds a JSON document with nested array where multiple items can be changed.
    /// </summary>
    /// <param name="arrayItemCount">Number of array items to create.</param>
    /// <param name="changedIndexes">Indexes of items that have changes (null for before state).</param>
    /// <returns>A cloned JSON element with array structure.</returns>
    private static JsonElement BuildDocumentWithMultipleArrayChanges(
        int arrayItemCount,
        int[]? changedIndexes)
    {
        var changedSet = changedIndexes?.ToHashSet() ?? new HashSet<int>();
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"properties\": {");
        sb.AppendLine("    \"policyRule\": {");
        sb.AppendLine("      \"if\": {");
        sb.AppendLine("        \"allOf\": [");

        for (var i = 0; i < arrayItemCount; i++)
        {
            sb.Append("          {");
            sb.Append(CultureInfo.InvariantCulture, $"\"field\": \"property{i}\"");

            // If this is a changed item, modify the value
            if (changedSet.Contains(i))
            {
                sb.Append(CultureInfo.InvariantCulture, $", \"equals\": \"changedValue{i}\"");
            }
            else
            {
                sb.Append(CultureInfo.InvariantCulture, $", \"equals\": \"value{i}\"");
            }

            sb.Append('}');
            if (i < arrayItemCount - 1)
            {
                sb.Append(',');
            }

            sb.AppendLine();
        }

        sb.AppendLine("        ]");
        sb.AppendLine("      }");
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine("}");

        using var document = JsonDocument.Parse(sb.ToString());
        return document.RootElement.Clone();
    }
}
