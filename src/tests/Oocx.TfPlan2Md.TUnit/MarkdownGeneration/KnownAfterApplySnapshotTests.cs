using System.IO;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzureAD;
using Oocx.TfPlan2Md.Providers.AzureRM;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Snapshot test for known-after-apply rendering scenarios.
/// </summary>
/// <remarks>
/// Verifies one combined plan that exercises all 9 specification scenarios.
/// Related feature: docs/features/102-known-after-apply-rendering/specification.md.
/// </remarks>
public class KnownAfterApplySnapshotTests
{
    private readonly TerraformPlanParser _parser = new();

    /// <summary>
    /// Ensures all known-after-apply scenarios render as expected in a single report snapshot.
    /// </summary>
    [Test]
    public void Snapshot_KnownAfterApply_AllScenarios_MatchesBaseline()
    {
        var json = File.ReadAllText("TestData/known-after-apply-all-scenarios-plan.json");
        var plan = _parser.Parse(json);

        var principalMapper = new NullPrincipalMapper();
        var providerRegistry = CreateProviderRegistry(principalMapper);
        var valueFormatterRegistry = CreateValueFormatterRegistry(providerRegistry);
        var iconProviderRegistry = CreateIconProviderRegistry(providerRegistry);

        var model = new ReportModelBuilder(
            metadataProvider: TestMetadataProvider.Instance,
            providerRegistry: providerRegistry,
            iconProviderRegistry: iconProviderRegistry).Build(plan);

        var renderer = new MarkdownRenderer(
            providerRegistry: providerRegistry,
            valueFormatterRegistry: valueFormatterRegistry,
            iconProviderRegistry: iconProviderRegistry);

        var markdown = renderer.Render(model);

        SnapshotTestAssertions.AssertNoEmojiFollowedByRegularSpace(markdown, "known-after-apply-all-scenarios.md");
        SnapshotTestAssertions.AssertMatchesSnapshot("known-after-apply-all-scenarios.md", markdown);
    }

    /// <summary>
    /// Creates a provider registry with the providers required by the known-after-apply scenarios.
    /// </summary>
    /// <param name="principalMapper">Principal mapper used by AzureRM provider summaries.</param>
    /// <returns>Configured provider registry.</returns>
    private static ProviderRegistry CreateProviderRegistry(IPrincipalMapper principalMapper)
    {
        var registry = new ProviderRegistry();
        registry.RegisterProvider(new AzureADModule());
        registry.RegisterProvider(new AzureRMModule(
            largeValueFormat: LargeValueFormat.InlineDiff,
            principalMapper: principalMapper));
        return registry;
    }

    /// <summary>
    /// Registers provider value formatters used by snapshot rendering.
    /// </summary>
    /// <param name="providerRegistry">Provider registry with active modules.</param>
    /// <returns>Configured value formatter registry.</returns>
    private static ValueFormatterRegistry CreateValueFormatterRegistry(ProviderRegistry providerRegistry)
    {
        return providerRegistry.CreateContributionSet().CreateValueFormatterRegistry();
    }

    /// <summary>
    /// Registers provider icon providers used by snapshot rendering.
    /// </summary>
    /// <param name="providerRegistry">Provider registry with active modules.</param>
    /// <returns>Configured icon provider registry.</returns>
    private static IconProviderRegistry CreateIconProviderRegistry(ProviderRegistry providerRegistry)
    {
        return providerRegistry.CreateContributionSet().CreateIconProviderRegistry();
    }
}
