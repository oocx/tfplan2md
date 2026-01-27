using System.Text.Json;
using AwesomeAssertions;
using TUnit.Core;
using AzApiHelpers = Oocx.TfPlan2Md.Providers.AzApi.ScribanHelpers;

namespace Oocx.TfPlan2Md.Tests.Providers.AzApi;

/// <summary>
/// Tests update-mode rendering for azapi body helpers.
/// Related feature: docs/features/040-azapi-resource-template/specification.md.
/// </summary>
public class ScribanHelpersAzApiUpdateRenderingTests
{
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
}
