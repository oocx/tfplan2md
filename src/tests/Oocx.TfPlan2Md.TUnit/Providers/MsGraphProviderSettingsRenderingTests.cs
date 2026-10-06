using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Providers.MsGraph;
using Oocx.TfPlan2Md.RenderTargets;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.MsGraph;

/// <summary>
/// Verifies the collapsed Microsoft Graph settings section uses the default resource rendering path.
/// </summary>
public class MsGraphProviderSettingsRenderingTests
{
    /// <summary>
    /// Verifies desired settings are readable in the collapsed area for every supported target and unchanged mode.
    /// </summary>
    [Test]
    [Arguments("AzureDevOps", false)]
    [Arguments("AzureDevOps", true)]
    [Arguments("GitHub", false)]
    [Arguments("GitHub", true)]
    [Arguments("Bitbucket", false)]
    [Arguments("Bitbucket", true)]
    public void Render_MsGraphCreate_ShowsCollapsedSettingsForEveryTarget(string renderTargetName, bool showUnchangedValues)
    {
        var renderTarget = Enum.Parse<RenderTarget>(renderTargetName);
        var markdown = Render(
            "msgraph_resource",
            ["create"],
            before: null,
            after: CreateState("v1.0", "/groups/new", false, "New Group"),
            renderTarget,
            showUnchangedValues);

        markdown.Should().Contain("<details><summary>Provider settings</summary>");
        markdown.Should().Contain("| Setting | Value |");
        markdown.Should().Contain("api_version");
        markdown.Should().Contain("/groups/new");
        markdown.Should().Contain("ignore_missing_property");
    }

    /// <summary>
    /// Verifies an unchanged settings area is still present when a resource is updated.
    /// </summary>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public void Render_MsGraphUpdate_ShowsUnchangedSettingsRegardlessOfUnchangedMode(bool showUnchangedValues)
    {
        var markdown = Render(
            "msgraph_update_resource",
            ["update"],
            before: CreateState("v1.0", "/groups/legacy", false, "Before"),
            after: CreateState("v1.0", "/groups/legacy", false, "After"),
            RenderTarget.AzureDevOps,
            showUnchangedValues);

        markdown.Should().Contain("<details><summary>Provider settings</summary>");
        markdown.Should().Contain("| Setting | Value |");
        markdown.Should().Contain("/groups/legacy");
        markdown.Should().Contain("body.displayName");
    }

    /// <summary>
    /// Verifies changed provider settings remain in the primary diff and are not repeated in the collapsed area.
    /// </summary>
    [Test]
    public void Render_PopulatedMsGraphImport_KeepsChangedSettingsInPrimaryDiffOnce()
    {
        var before = CreateState("v1.0", "/groups/old", false, "Before");
        var after = CreateState("v1.0", "/groups/new", false, "After");
        var markdown = Render(
            "msgraph_resource",
            ["update"],
            before,
            after,
            RenderTarget.GitHub,
            showUnchangedValues: true,
            isImport: true);

        var sectionStart = markdown.IndexOf("<details><summary>Provider settings</summary>", StringComparison.Ordinal);
        var sectionEnd = markdown.IndexOf("</details>", sectionStart, StringComparison.Ordinal);
        var settingsSection = markdown[sectionStart..sectionEnd];

        markdown.Should().NotContain("Prior state from import is empty");
        markdown.Should().Contain("/groups/old");
        markdown.Should().Contain("/groups/new");
        settingsSection.Should().NotContain("url");
        CountOccurrences(markdown, "/groups/new").Should().Be(1);
    }

    /// <summary>
    /// Verifies an empty imported prior state note precedes desired attributes and settings remain inside the card.
    /// </summary>
    [Test]
    public void Render_MsGraphEmptyImport_PreservesImportNoteBeforeDesiredAttributes()
    {
        var markdown = Render(
            "msgraph_resource",
            ["create"],
            before: """{"id":"existing","api_version":"v1.0","url":"/groups/legacy","ignore_missing_property":false,"body":{}}""",
            after: CreateState("v1.0", "/groups/legacy", false, "Imported Group"),
            RenderTarget.Bitbucket,
            showUnchangedValues: false,
            isImport: true);

        var noteIndex = markdown.IndexOf("Prior state from import is empty", StringComparison.Ordinal);
        var attributeTableIndex = markdown.IndexOf("| Attribute | Value |", StringComparison.Ordinal);
        var settingsIndex = markdown.IndexOf("<details><summary>Provider settings</summary>", StringComparison.Ordinal);

        noteIndex.Should().BeGreaterThanOrEqualTo(0);
        attributeTableIndex.Should().BeGreaterThan(noteIndex);
        settingsIndex.Should().BeGreaterThan(attributeTableIndex);
        markdown.Should().Contain("| Setting | Value |");
    }

    /// <summary>
    /// Verifies classified sensitive settings remain masked in rendered markdown.
    /// </summary>
    [Test]
    public void Render_MsGraphCreateWithSensitiveUrl_DoesNotExposeSecret()
    {
        const string secret = "sensitive-msgraph-url";
        var markdown = Render(
            "msgraph_resource",
            ["create"],
            before: null,
            after: $"{{\"api_version\":\"v1.0\",\"url\":\"{secret}\",\"ignore_missing_property\":false,\"body\":{{\"displayName\":\"New Group\"}}}}",
            RenderTarget.GitHub,
            showUnchangedValues: false,
            afterSensitive: """{"url":true}""");

        markdown.Should().Contain("(sensitive)");
        markdown.Should().NotContain(secret);
    }

    /// <summary>
    /// Verifies large secondary values retain the default renderer's collapsed large-value handling.
    /// </summary>
    [Test]
    public void Render_MsGraphCreateWithLargeUrl_UsesLargeValuePresentation()
    {
        var largeUrl = "/groups/" + new string('x', 5_000);
        var markdown = Render(
            "msgraph_resource",
            ["create"],
            before: null,
            after: CreateState("v1.0", largeUrl, false, "New Group"),
            RenderTarget.AzureDevOps,
            showUnchangedValues: false);

        markdown.Should().Contain("Large values: url");
    }

    /// <summary>
    /// Builds and renders a plan using the production Microsoft Graph model and renderer contributions.
    /// </summary>
    /// <param name="resourceType">Terraform resource type.</param>
    /// <param name="actions">Terraform actions.</param>
    /// <param name="before">Prior state or its JSON text.</param>
    /// <param name="after">Desired state or its JSON text.</param>
    /// <param name="renderTarget">Target Markdown platform.</param>
    /// <param name="showUnchangedValues">Whether unchanged primary attributes should be shown.</param>
    /// <param name="isImport">Whether Terraform marks the resource as imported.</param>
    /// <param name="afterSensitive">Optional desired-state sensitivity markers.</param>
    /// <returns>Rendered Markdown report.</returns>
    private static string Render(
        string resourceType,
        IReadOnlyList<string> actions,
        object? before,
        object? after,
        RenderTarget renderTarget,
        bool showUnchangedValues,
        bool isImport = false,
        object? afterSensitive = null)
    {
        var resourceChange = new ResourceChange(
            $"{resourceType}.sample",
            null,
            "managed",
            resourceType,
            "sample",
            "registry.terraform.io/microsoft/msgraph",
            new Change(
                actions,
                ParseState(before),
                ParseState(after),
                null,
                null,
                ParseState(afterSensitive),
                importing: isImport ? new Importing { Id = "existing-id" } : null));
        var registry = new ProviderRegistry();
        registry.RegisterProvider(new MsGraphModule());
        var model = new ReportModelBuilder(
            new ReportModelBuilderOptions(RenderTarget: renderTarget, ShowUnchangedValues: showUnchangedValues),
            new ReportModelBuilderServices(ProviderRegistry: registry))
            .Build(new TerraformPlan("1.0", "1.0", [resourceChange]));

        return new MarkdownRenderer(providerRegistry: registry).Render(model);
    }

    /// <summary>
    /// Converts a string or object state to an owned JSON element for plan construction.
    /// </summary>
    /// <param name="state">State text, dictionary, or null.</param>
    /// <returns>A cloned JSON element or null.</returns>
    private static JsonElement? ParseState(object? state)
    {
        if (state is null)
        {
            return null;
        }

        using var document = state is string json
            ? JsonDocument.Parse(json)
            : JsonDocument.Parse(JsonSerializer.Serialize(state));
        return document.RootElement.Clone();
    }

    /// <summary>
    /// Creates Graph resource state containing provider settings and a display-name body value.
    /// </summary>
    /// <param name="apiVersion">The API version setting.</param>
    /// <param name="url">The resource URL setting.</param>
    /// <param name="ignoreMissingProperty">The missing-property behavior setting.</param>
    /// <param name="displayName">The desired resource display name.</param>
    /// <returns>Serialized resource state.</returns>
    private static string CreateState(string apiVersion, string url, bool ignoreMissingProperty, string displayName)
    {
        return JsonSerializer.Serialize(new
        {
            api_version = apiVersion,
            url,
            ignore_missing_property = ignoreMissingProperty,
            body = new { displayName }
        });
    }

    /// <summary>
    /// Counts non-overlapping occurrences of one string in another.
    /// </summary>
    /// <param name="text">Text to scan.</param>
    /// <param name="value">Value to count.</param>
    /// <returns>Number of occurrences.</returns>
    private static int CountOccurrences(string text, string value)
    {
        return text.Split([value], StringSplitOptions.None).Length - 1;
    }
}
