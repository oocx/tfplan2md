using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.RenderTargets;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for <see cref="RenderContext"/>.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// Related test plan: docs/features/107-remove-scriban/test-plan.md (TC-RC-01).
/// </summary>
public class RenderContextTests
{
    /// <summary>
    /// Verifies <see cref="IRenderContext.ShowSensitive"/> is stored as true.
    /// </summary>
    [Test]
    public void RenderContext_ShowSensitiveTrue_PropertyIsTrue()
    {
        var context = new RenderContext(
            showSensitive: true,
            showUnchangedValues: false,
            ignoreAzureIdCaseChanges: true,
            renderTarget: RenderTarget.AzureDevOps,
            detailsDisplayMode: DetailsDisplayMode.Auto);

        context.ShowSensitive.Should().BeTrue();
    }

    /// <summary>
    /// Verifies <see cref="IRenderContext.ShowSensitive"/> is stored as false.
    /// </summary>
    [Test]
    public void RenderContext_ShowSensitiveFalse_PropertyIsFalse()
    {
        var context = new RenderContext(
            showSensitive: false,
            showUnchangedValues: true,
            ignoreAzureIdCaseChanges: false,
            renderTarget: RenderTarget.GitHub,
            detailsDisplayMode: DetailsDisplayMode.Open);

        context.ShowSensitive.Should().BeFalse();
    }

    /// <summary>
    /// Verifies GitHub render target is stored.
    /// </summary>
    [Test]
    public void RenderContext_RenderTargetGitHub_PropertyIsGitHub()
    {
        var context = new RenderContext(
            showSensitive: false,
            showUnchangedValues: false,
            ignoreAzureIdCaseChanges: true,
            renderTarget: RenderTarget.GitHub,
            detailsDisplayMode: DetailsDisplayMode.Closed);

        context.RenderTarget.Should().Be(RenderTarget.GitHub);
    }

    /// <summary>
    /// Verifies Azure DevOps render target is stored.
    /// </summary>
    [Test]
    public void RenderContext_RenderTargetAzureDevOps_PropertyIsAzureDevOps()
    {
        var context = new RenderContext(
            showSensitive: true,
            showUnchangedValues: true,
            ignoreAzureIdCaseChanges: false,
            renderTarget: RenderTarget.AzureDevOps,
            detailsDisplayMode: DetailsDisplayMode.Auto);

        context.RenderTarget.Should().Be(RenderTarget.AzureDevOps);
    }
}
