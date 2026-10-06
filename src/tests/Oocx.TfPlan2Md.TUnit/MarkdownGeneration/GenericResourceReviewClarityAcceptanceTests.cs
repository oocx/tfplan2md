using System.Collections.Immutable;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzApi;
using Oocx.TfPlan2Md.Providers.AzureDevOps;
using Oocx.TfPlan2Md.Providers.AzureRM;
using Oocx.TfPlan2Md.Providers.MsGraph;
using Oocx.TfPlan2Md.RenderTargets;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Exercises the complete Feature 146 review fixture through the report model and each renderer.
/// </summary>
public class GenericResourceReviewClarityAcceptanceTests
{
    /// <summary>
    /// Verifies feature signals survive a full plan render for every supported target.
    /// </summary>
    [Test]
    [Arguments("GitHub")]
    [Arguments("AzureDevOps")]
    [Arguments("Bitbucket")]
    public void Render_UatPlan_AllTargetsPreserveReviewSignals(string targetName)
    {
        var plan = LoadPlan("TestData/feature-146-uat-plan.json");
        var markdown = Render(plan, Enum.Parse<RenderTarget>(targetName));

        markdown.Should().Contain("defaults[\"first\"]");
        markdown.Should().Contain("Default Name Wins");
        markdown.Should().Contain("fallback[0]");
        markdown.Should().Contain("Review &amp; &lt;script&gt; \"quoted\"");
        markdown.Should().NotContain("literal dotted key");
        markdown.Should().Contain("none[1]");
        markdown.Should().Contain("update[\"a.b[0]\"]");
        markdown.Should().Contain("Prior Update Name");
        markdown.Should().Contain("deleted[2]");
        markdown.Should().Contain("replaced[\"quote\\\"and\\\\slash\"]");
        markdown.Should().Contain("Replacement &amp; &lt;new&gt;");
        markdown.Should().Contain("Safe Visible Identity");
        markdown.Should().NotContain("SENSITIVE-IDENTITY-DO-NOT-LEAK");
        markdown.Should().Contain("> 📥\u00A0Prior state from import is empty");
        markdown.Should().Contain("module.demo.review_object.imported[\"named\"]");
        markdown.Should().Contain("module.demo.review_object.moved[\"new\"]");
        markdown.Should().Contain("<details><summary>Provider settings</summary>");
        markdown.Should().Contain("| Setting | Value |");
        markdown.Should().Contain("mapped-api");
        markdown.Should().Contain("Review variables");
        markdown.Should().Contain("root-unchanged-value");
        markdown.Should().Contain("module-unchanged-value");
        markdown.Should().Contain("module-noop-value");
        markdown.Should().Contain("(known after apply)");
        markdown.Should().Contain("(known after apply: each.value.group_object_id)");
        markdown.Should().Contain("(known after apply: var.tenant_id)");
        markdown.Should().NotContain("(known after apply: each.key)");
        markdown.Should().NotContain("ROOT-SENSITIVE-OUTPUT-DO-NOT-LEAK");
    }

    /// <summary>
    /// Verifies the two opt-in options compose while mapped identities and meaningful outputs remain visible.
    /// </summary>
    [Test]
    [Arguments("GitHub")]
    [Arguments("AzureDevOps")]
    [Arguments("Bitbucket")]
    public void Render_UatPlan_WithOverridesAndFiltering_PreservesMappedAndChangedContent(string targetName)
    {
        var plan = LoadPlan("TestData/feature-146-uat-plan.json");
        var target = Enum.Parse<RenderTarget>(targetName);
        var model = BuildModel(
            plan,
            target,
            showUnchangedValues: true,
            hideUnchangedOutputs: true,
            summaryNameAttributes: ["body.title", "body.properties.title"]);
        var markdown = CreateRenderer().Render(model);

        markdown.Should().Contain("<summary>➕ review_object <b><code>override[\"custom\"]</code></b> — <code>Override Title</code></summary>");
        markdown.Should().Contain("<summary>🔄 azapi_resource <b><code>mapped[0]</code></b> — <code>🆔 mapped-api</code>");
        markdown.Should().Contain("/policies/conditionalAccessPolicies/policy-id");
        markdown.Should().Contain("root-created-value");
        markdown.Should().Contain("root-updated-value");
        markdown.Should().Contain("root-deleted-value");
        markdown.Should().Contain("module-created-value");
        markdown.Should().NotContain("root-unchanged-value");
        markdown.Should().NotContain("module-unchanged-value");
        markdown.Should().NotContain("module-noop-value");
        model.ModuleChanges.Should().NotContain(module => module.ModuleAddress == "module.noop");
    }

    /// <summary>
    /// Verifies filtering the only root no-op removes the complete output section.
    /// </summary>
    [Test]
    public void Render_RootOnlyNoOpVariant_HidesTheEmptyOutputsSection()
    {
        var plan = LoadPlan("TestData/feature-146-uat-plan-root-noop.json");
        var defaultMarkdown = CreateRenderer().Render(BuildModel(plan, RenderTarget.GitHub));
        var filteredModel = BuildModel(plan, RenderTarget.GitHub, hideUnchangedOutputs: true);
        var filteredMarkdown = CreateRenderer().Render(filteredModel);

        defaultMarkdown.Should().Contain("root-only-after");
        filteredModel.GlobalOutputs.Should().BeEmpty();
        filteredMarkdown.Should().NotContain("root-only-before");
        filteredMarkdown.Should().NotContain("## 📤");
    }

    /// <summary>
    /// Verifies the authorized-sensitive variant reveals a generic identity only when requested.
    /// </summary>
    [Test]
    public void Render_UatPlan_WithSensitiveAuthorization_UsesExistingRevealPolicy()
    {
        var plan = LoadPlan("TestData/feature-146-uat-plan.json");
        var markdown = CreateRenderer().Render(BuildModel(plan, RenderTarget.GitHub, showSensitive: true));

        markdown.Should().Contain("SENSITIVE-IDENTITY-DO-NOT-LEAK");
        markdown.Should().Contain("ROOT-SENSITIVE-OUTPUT-DO-NOT-LEAK");
    }

    /// <summary>
    /// Parses a checked-in plan fixture for end-to-end rendering assertions.
    /// </summary>
    /// <param name="path">The fixture path relative to the test data directory.</param>
    /// <returns>The parsed Terraform plan.</returns>
    private static TerraformPlan LoadPlan(string path)
    {
        return new TerraformPlanParser().Parse(File.ReadAllText(path));
    }

    /// <summary>
    /// Builds and renders the default acceptance report for one target.
    /// </summary>
    /// <param name="plan">The plan to render.</param>
    /// <param name="target">The output platform.</param>
    /// <returns>The rendered report Markdown.</returns>
    private static string Render(TerraformPlan plan, RenderTarget target)
    {
        return CreateRenderer().Render(BuildModel(plan, target));
    }

    /// <summary>
    /// Builds the report model with the same provider registrations used by the CLI.
    /// </summary>
    /// <param name="plan">The plan to build.</param>
    /// <param name="target">The output platform.</param>
    /// <param name="showSensitive">Whether sensitive content is authorized.</param>
    /// <param name="showUnchangedValues">Whether unchanged resource attributes are included.</param>
    /// <param name="hideUnchangedOutputs">Whether effective no-op outputs are filtered.</param>
    /// <param name="summaryNameAttributes">Optional replacement paths for generic identities.</param>
    /// <returns>The assembled report model.</returns>
    private static ReportModel BuildModel(
        TerraformPlan plan,
        RenderTarget target,
        bool showSensitive = false,
        bool showUnchangedValues = false,
        bool hideUnchangedOutputs = false,
        ImmutableArray<string> summaryNameAttributes = default)
    {
        var providerRegistry = CreateProviderRegistry();
        var providerContributions = providerRegistry.CreateContributionSet();
        return new ReportModelBuilder(
            new ReportModelBuilderOptions(
                ShowSensitive: showSensitive,
                ShowUnchangedValues: showUnchangedValues,
                RenderTarget: target,
                SummaryNameAttributes: summaryNameAttributes,
                HideUnchangedOutputs: hideUnchangedOutputs),
            new ReportModelBuilderServices(
                MetadataProvider: TestMetadataProvider.Instance,
                ProviderRegistry: providerRegistry,
                ProviderContributions: providerContributions))
            .Build(plan);
    }

    /// <summary>
    /// Creates the provider registry required by mapped compatibility cases in the fixture.
    /// </summary>
    /// <returns>A registry with the CLI's relevant provider modules.</returns>
    private static ProviderRegistry CreateProviderRegistry()
    {
        var registry = new ProviderRegistry();
        registry.RegisterProvider(new AzApiModule());
        registry.RegisterProvider(new AzureRMModule(LargeValueFormat.SimpleDiff));
        registry.RegisterProvider(new AzureDevOpsModule());
        registry.RegisterProvider(new MsGraphModule());
        return registry;
    }

    /// <summary>
    /// Creates a renderer with matching provider-specific renderers and formatters.
    /// </summary>
    /// <returns>A Markdown renderer configured for the fixture providers.</returns>
    private static MarkdownRenderer CreateRenderer()
    {
        var registry = CreateProviderRegistry();
        var contributions = registry.CreateContributionSet();
        return new MarkdownRenderer(providerRegistry: registry, providerContributions: contributions);
    }
}
