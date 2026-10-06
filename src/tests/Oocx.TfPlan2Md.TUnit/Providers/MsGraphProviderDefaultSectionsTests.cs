using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Providers.MsGraph.Renderers;
using Oocx.TfPlan2Md.RenderTargets;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.MsGraph;

/// <summary>
/// Verifies Microsoft Graph's renderer retains default resource card sections.
/// </summary>
public class MsGraphProviderDefaultSectionsTests
{
    /// <summary>
    /// Verifies provider settings rendering leaves annotations, analysis findings, and inline actions intact.
    /// </summary>
    [Test]
    public void Render_MsGraphResource_PreservesDefaultCardSections()
    {
        var change = new ResourceChangeModel
        {
            Address = "msgraph_resource.sample",
            Type = "msgraph_resource",
            Name = "sample",
            ProviderName = "registry.terraform.io/microsoft/msgraph",
            Action = "replace",
            ActionSymbol = "♻️",
            AttributeChanges = [new AttributeChangeModel { Name = "body.displayName", Before = "Before", After = "After" }],
            SecondaryAttributeChanges = [new AttributeChangeModel { Name = "url", After = "/groups/sample" }],
            SummaryHtml = "♻️ msgraph_resource `sample`",
            CodeAnalysisFindings = [new CodeAnalysisFindingModel
            {
                Severity = "High",
                SeverityIcon = "⚠️",
                SeverityRank = 3,
                Message = "Review this resource.",
                ToolName = "test-tool",
                AttributePath = "body.displayName"
            }],
            ForcedReplacementAnnotations = [new ForcedReplacementAnnotation
            {
                LocalAttribute = "body",
                UpstreamResource = "example_resource.parent",
                UpstreamAttributePath = "id",
                IsChangingInThisPlan = true
            }],
            DependsOnAnnotations = [new DependsOnAnnotation
            {
                UpstreamResource = "example_resource.dependency",
                UpstreamAttributePath = "name",
                IsChangingInThisPlan = false
            }],
            Actions = [new ActionInvocationModel
            {
                Invocation = new ActionInvocation(
                    "terraform_data.followup",
                    "terraform_data",
                    "followup",
                    "registry.terraform.io/hashicorp/terraform",
                    LifecycleActionTrigger: new LifecycleActionTrigger("msgraph_resource.sample", "after_create"))
            }]
        };
        var context = new RenderContext(
            showSensitive: false,
            showUnchangedValues: false,
            ignoreAzureIdCaseChanges: true,
            renderTarget: RenderTarget.GitHub,
            detailsDisplayMode: DetailsDisplayMode.Auto);
        var writer = new MarkdownWriter();

        new MsGraphResourceRenderer("msgraph_resource").Render(writer, change, context);

        var markdown = writer.Build();
        markdown.Should().Contain("Security & Quality");
        markdown.Should().Contain("Forced replacement");
        markdown.Should().Contain("Also depends on:");
        markdown.Should().Contain("🎬\u00A0Actions");
        markdown.Should().Contain("Provider settings");
    }
}
