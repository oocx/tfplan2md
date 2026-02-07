using System.IO;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzureAD;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Snapshot tests covering Azure AD rendering scenarios.
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </summary>
public class AzureAdSnapshotTests
{
    /// <summary>
    /// Parses Terraform plan JSON files for Azure AD snapshots.
    /// Related feature: docs/features/061-extensible-provider-registry/specification.md.
    /// </summary>
    private readonly TerraformPlanParser _parser = new();

    /// <summary>
    /// Verifies the Azure AD snapshot output matches the approved baseline.
    /// Related feature: docs/features/061-extensible-provider-registry/specification.md.
    /// </summary>
    [Test]
    public void Snapshot_AzureAd_Comprehensive_MatchesBaseline()
    {
        AssertAzureAdSnapshot("azuread-snapshot-plan.json", "azuread-snapshot.md");
    }

    /// <summary>
    /// Renders a markdown report from an Azure AD plan test data file.
    /// Related feature: docs/features/061-extensible-provider-registry/specification.md.
    /// </summary>
    /// <param name="testDataFile">The test data file name under TestData.</param>
    /// <returns>The rendered markdown output.</returns>
    private string RenderAzureAdPlan(string testDataFile)
    {
        var json = File.ReadAllText(Path.Combine("TestData", testDataFile));
        var plan = _parser.Parse(json);
        var principalMapper = new PrincipalMapper(DemoPaths.AzureAdPrincipalMappingPath);
        var providerRegistry = CreateProviderRegistry();
        var valueFormatterRegistry = CreateValueFormatterRegistry(providerRegistry);
        var iconProviderRegistry = CreateIconProviderRegistry(providerRegistry);
        var model = new ReportModelBuilder(
            principalMapper: principalMapper,
            metadataProvider: TestMetadataProvider.Instance,
            providerRegistry: providerRegistry,
            iconProviderRegistry: iconProviderRegistry).Build(plan);
        var renderer = new MarkdownRenderer(
            principalMapper: principalMapper,
            providerRegistry: providerRegistry,
            valueFormatterRegistry: valueFormatterRegistry,
            iconProviderRegistry: iconProviderRegistry);

        return renderer.Render(model);
    }

    /// <summary>
    /// Asserts the rendered output matches the stored snapshot.
    /// Related feature: docs/features/061-extensible-provider-registry/specification.md.
    /// </summary>
    /// <param name="testDataFile">The test data file name under TestData.</param>
    /// <param name="snapshotName">The snapshot file name under TestData/Snapshots.</param>
    private void AssertAzureAdSnapshot(string testDataFile, string snapshotName)
    {
        var markdown = RenderAzureAdPlan(testDataFile);

        SnapshotTestAssertions.AssertNoEmojiFollowedByRegularSpace(markdown, snapshotName);
        SnapshotTestAssertions.AssertMatchesSnapshot(snapshotName, markdown);
    }

    /// <summary>
    /// Creates a provider registry that includes Azure AD support.
    /// Related feature: docs/features/061-extensible-provider-registry/specification.md.
    /// </summary>
    /// <returns>The configured provider registry.</returns>
    private static ProviderRegistry CreateProviderRegistry()
    {
        var registry = new ProviderRegistry();
        registry.RegisterProvider(new AzureADModule());
        return registry;
    }

    /// <summary>
    /// Registers Azure AD value formatters for snapshot rendering.
    /// Related feature: docs/features/061-extensible-provider-registry/specification.md.
    /// </summary>
    /// <param name="providerRegistry">The registry containing provider modules.</param>
    /// <returns>The configured value formatter registry.</returns>
    private static ValueFormatterRegistry CreateValueFormatterRegistry(ProviderRegistry providerRegistry)
    {
        var registry = new ValueFormatterRegistry();
        providerRegistry.RegisterAllValueFormatters(registry);
        return registry;
    }

    /// <summary>
    /// Registers Azure AD icon providers for snapshot rendering.
    /// Related feature: docs/features/061-extensible-provider-registry/specification.md.
    /// </summary>
    /// <param name="providerRegistry">The registry containing provider modules.</param>
    /// <returns>The configured icon provider registry.</returns>
    private static IconProviderRegistry CreateIconProviderRegistry(ProviderRegistry providerRegistry)
    {
        var registry = new IconProviderRegistry();
        providerRegistry.RegisterAllIconProviders(registry);
        return registry;
    }
}
