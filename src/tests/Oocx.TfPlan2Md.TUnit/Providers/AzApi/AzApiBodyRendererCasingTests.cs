using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.Providers.AzApi.Helpers;
using Oocx.TfPlan2Md.RenderTargets;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzApi;

/// <summary>
/// Tests for Azure ID case-insensitive filtering in <see cref="AzApiBodyRenderer.RenderUpdateBody"/>.
/// Related issue: docs/issues/108-azapi-body-casing-filter/analysis.md.
/// </summary>
[Category("Unit")]
public class AzApiBodyRendererCasingTests
{
    private static RenderContext CreateContext(bool ignoreAzureIdCaseChanges) =>
        new RenderContext(
            showSensitive: false,
            showUnchangedValues: false,
            ignoreAzureIdCaseChanges: ignoreAzureIdCaseChanges,
            renderTarget: RenderTarget.GitHub,
            detailsDisplayMode: DetailsDisplayMode.Auto);

    private static object ParseJson(string json) =>
        JsonSerializer.Deserialize<object>(json)!;

    // -------------------------------------------------------------------------
    // TC-01: Azure ID casing-only body change is filtered when flag is enabled.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-01: When IgnoreAzureIdCaseChanges=true and the only body difference is Azure ID casing,
    /// "No body changes detected" is rendered.
    /// </summary>
    [Test]
    public async Task RenderUpdateBody_AzureIdCasingOnlyChange_FlagEnabled_ShowsNoChanges()
    {
        // Arrange — diskAccessId differs only in casing of resource group segment
        var before = ParseJson("""
            {
                "diskAccessId": "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/APP-RG-GWC/providers/Microsoft.Compute/diskAccesses/app-gwc"
            }
            """);
        var after = ParseJson("""
            {
                "diskAccessId": "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/app-rg-gwc/providers/Microsoft.Compute/diskAccesses/app-gwc"
            }
            """);

        var writer = new MarkdownWriter();
        var context = CreateContext(ignoreAzureIdCaseChanges: true);

        // Act
        AzApiBodyRenderer.RenderUpdateBody(writer, "Body Changes", before, after, null, null, context);
        var output = writer.Build();

        // Assert
        output.Should().Contain("*No body changes detected*",
            "a casing-only Azure ID change should be suppressed when the flag is enabled");
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // TC-02: Azure ID casing-only body change is shown when flag is disabled.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-02: When IgnoreAzureIdCaseChanges=false, a casing-only Azure ID change is shown as a change.
    /// </summary>
    [Test]
    public async Task RenderUpdateBody_AzureIdCasingOnlyChange_FlagDisabled_ShowsChange()
    {
        // Arrange
        var before = ParseJson("""
            {
                "diskAccessId": "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/APP-RG-GWC/providers/Microsoft.Compute/diskAccesses/app-gwc"
            }
            """);
        var after = ParseJson("""
            {
                "diskAccessId": "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/app-rg-gwc/providers/Microsoft.Compute/diskAccesses/app-gwc"
            }
            """);

        var writer = new MarkdownWriter();
        var context = CreateContext(ignoreAzureIdCaseChanges: false);

        // Act
        AzApiBodyRenderer.RenderUpdateBody(writer, "Body Changes", before, after, null, null, context);
        var output = writer.Build();

        // Assert
        output.Should().NotContain("*No body changes detected*",
            "casing-only Azure ID changes should appear when the flag is disabled");
        output.Should().Contain("diskAccessId",
            "the changed property should be listed in the output");
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // TC-03: Non-Azure-ID casing change is always shown.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-03: A plain-string casing change (not an Azure ID) is never filtered, even when the flag is enabled.
    /// </summary>
    [Test]
    public async Task RenderUpdateBody_NonAzureIdCasingChange_FlagEnabled_ShowsChange()
    {
        // Arrange — a plain display name differs only in casing
        var before = ParseJson("""{"displayName": "MyApp"}""");
        var after = ParseJson("""{"displayName": "myapp"}""");

        var writer = new MarkdownWriter();
        var context = CreateContext(ignoreAzureIdCaseChanges: true);

        // Act
        AzApiBodyRenderer.RenderUpdateBody(writer, "Body Changes", before, after, null, null, context);
        var output = writer.Build();

        // Assert
        output.Should().NotContain("*No body changes detected*",
            "a non-Azure-ID casing change must never be suppressed");
        output.Should().Contain("displayName",
            "the changed property should be listed in the output");
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // TC-04: Genuine content change is always shown.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-04: When the before/after Azure IDs differ in actual content (not just casing), the change
    /// is always shown even when the flag is enabled.
    /// </summary>
    [Test]
    public async Task RenderUpdateBody_AzureIdGenuineContentChange_FlagEnabled_ShowsChange()
    {
        // Arrange — different resource group names (not just casing)
        var before = ParseJson("""
            {
                "parentId": "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/old-rg"
            }
            """);
        var after = ParseJson("""
            {
                "parentId": "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/new-rg"
            }
            """);

        var writer = new MarkdownWriter();
        var context = CreateContext(ignoreAzureIdCaseChanges: true);

        // Act
        AzApiBodyRenderer.RenderUpdateBody(writer, "Body Changes", before, after, null, null, context);
        var output = writer.Build();

        // Assert
        output.Should().NotContain("*No body changes detected*",
            "a genuine content change should never be suppressed");
        output.Should().Contain("parentId",
            "the changed property should be listed in the output");
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // TC-05: Mixed changes — one casing-only Azure ID, one genuine change.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-05: When there are two properties changed — one Azure ID casing-only and one genuine — only
    /// the genuine change is shown when the flag is enabled.
    /// </summary>
    [Test]
    public async Task RenderUpdateBody_MixedChanges_FlagEnabled_ShowsOnlyGenuineChange()
    {
        // Arrange
        var before = ParseJson("""
            {
                "diskAccessId": "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/APP-RG-GWC/providers/Microsoft.Compute/diskAccesses/app-gwc",
                "location": "westeurope"
            }
            """);
        var after = ParseJson("""
            {
                "diskAccessId": "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/app-rg-gwc/providers/Microsoft.Compute/diskAccesses/app-gwc",
                "location": "eastus"
            }
            """);

        var writer = new MarkdownWriter();
        var context = CreateContext(ignoreAzureIdCaseChanges: true);

        // Act
        AzApiBodyRenderer.RenderUpdateBody(writer, "Body Changes", before, after, null, null, context);
        var output = writer.Build();

        // Assert
        output.Should().NotContain("diskAccessId",
            "the casing-only Azure ID property should be suppressed");
        output.Should().Contain("location",
            "the genuine location change should be shown");
        await Task.CompletedTask;
    }
}
