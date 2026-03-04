using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.RenderTargets;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Unit tests for scenario formatting detection in <see cref="DefaultResourceRenderer"/>.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
public class DefaultResourceRendererScenarioTests
{
    /// <summary>
    /// Verifies root-module AzureRM resources with known-after-apply markers enable known-after-apply formatting.
    /// </summary>
    [Test]
    public void ResolveScenarioFormatting_AzurermRootWithKnownAfterApplyMarker_EnablesKnownAfterApplyFormatting()
    {
        var change = CreateChange(
            resourceType: "azurerm_resource_group",
            moduleAddress: null,
            attributes:
            [
                new AttributeChangeModel { Name = "id", Before = null, After = "(known after apply)" }
            ]);

        var result = DefaultResourceRenderer.ResolveScenarioFormatting(change, CreateContext());

        result.UseKnownAfterApplyFormatting.Should().BeTrue();
        result.UseEphemeralOpenFormatting.Should().BeFalse();
        result.UseOutputsFocusedFormatting.Should().BeFalse();
    }

    /// <summary>
    /// Verifies module-scoped AzureRM resources do not trigger known-after-apply heuristics.
    /// </summary>
    [Test]
    public void ResolveScenarioFormatting_AzurermNestedModuleWithKnownAfterApplyMarker_DoesNotEnableKnownAfterApplyFormatting()
    {
        var change = CreateChange(
            resourceType: "azurerm_resource_group",
            moduleAddress: "module.network",
            attributes:
            [
                new AttributeChangeModel { Name = "id", Before = null, After = "(known after apply)" }
            ]);

        var result = DefaultResourceRenderer.ResolveScenarioFormatting(change, CreateContext());

        result.UseKnownAfterApplyFormatting.Should().BeFalse();
    }

    /// <summary>
    /// Verifies root-module null_resource resources with known-after-apply markers enable known-after-apply formatting.
    /// </summary>
    [Test]
    public void ResolveScenarioFormatting_NullResourceRootWithKnownAfterApplyMarker_EnablesKnownAfterApplyFormatting()
    {
        var change = CreateChange(
            resourceType: "null_resource",
            moduleAddress: null,
            attributes:
            [
                new AttributeChangeModel { Name = "id", Before = null, After = "(known after apply)" }
            ]);

        var result = DefaultResourceRenderer.ResolveScenarioFormatting(change, CreateContext());

        result.UseKnownAfterApplyFormatting.Should().BeTrue();
    }

    /// <summary>
    /// Verifies azuread_group_member with configuration references enables known-after-apply formatting
    /// when any attribute contains the marker.
    /// </summary>
    [Test]
    public void ResolveScenarioFormatting_AzureAdGroupMemberWithConfigurationReference_EnablesKnownAfterApplyFormatting()
    {
        var change = CreateChange(
            resourceType: "azuread_group_member",
            moduleAddress: null,
            attributes:
            [
                new AttributeChangeModel { Name = "group_object_id", Before = null, After = "(known after apply)" },
                new AttributeChangeModel { Name = "member_object_id", Before = null, After = "user-123" }
            ],
            configurationReferences: new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["group_object_id"] = ["var.group_id"]
            });

        var result = DefaultResourceRenderer.ResolveScenarioFormatting(change, CreateContext());

        result.UseKnownAfterApplyFormatting.Should().BeTrue();
    }

    /// <summary>
    /// Verifies azuread_group_member without configuration references requires all attributes
    /// to contain known-after-apply markers.
    /// </summary>
    [Test]
    public void ResolveScenarioFormatting_AzureAdGroupMemberWithoutConfigurationReferences_DoesNotEnableKnownAfterApplyFormatting_WhenAnyAttributeLacksMarker()
    {
        var change = CreateChange(
            resourceType: "azuread_group_member",
            moduleAddress: null,
            attributes:
            [
                new AttributeChangeModel { Name = "group_object_id", Before = null, After = "(known after apply)" },
                new AttributeChangeModel { Name = "member_object_id", Before = null, After = "user-123" }
            ]);

        var result = DefaultResourceRenderer.ResolveScenarioFormatting(change, CreateContext());

        result.UseKnownAfterApplyFormatting.Should().BeFalse();
    }

    /// <summary>
    /// Verifies resources without known-after-apply markers keep known-after-apply formatting disabled.
    /// </summary>
    [Test]
    public void ResolveScenarioFormatting_WithoutKnownAfterApplyMarkers_DoesNotEnableKnownAfterApplyFormatting()
    {
        var change = CreateChange(
            resourceType: "random_id",
            moduleAddress: null,
            attributes:
            [
                new AttributeChangeModel { Name = "hex", Before = "abc", After = "def" }
            ]);

        var result = DefaultResourceRenderer.ResolveScenarioFormatting(change, CreateContext());

        result.UseKnownAfterApplyFormatting.Should().BeFalse();
    }

    /// <summary>
    /// Verifies heuristic detection does not currently auto-enable ephemeral-open formatting.
    /// </summary>
    [Test]
    public void ResolveScenarioFormatting_HeuristicOnly_DoesNotEnableEphemeralOpenFormatting()
    {
        var change = CreateChange(
            resourceType: "ephemeral.vault_kv_secret_v2",
            moduleAddress: null,
            action: "open",
            attributes:
            [
                new AttributeChangeModel { Name = "value", Before = null, After = "(known after apply)" }
            ]);

        var result = DefaultResourceRenderer.ResolveScenarioFormatting(change, CreateContext());

        result.UseEphemeralOpenFormatting.Should().BeFalse();
    }

    /// <summary>
    /// Verifies scenario-context flags override heuristic detection when provided.
    /// </summary>
    [Test]
    public void ResolveScenarioFormatting_ScenarioContextFlags_OverrideHeuristics()
    {
        var change = CreateChange(
            resourceType: "random_id",
            moduleAddress: null,
            attributes:
            [
                new AttributeChangeModel { Name = "hex", Before = "abc", After = "def" }
            ]);

        var context = new ScenarioRenderContext(
            isKnownAfterApplyScenario: true,
            isEphemeralOpenScenario: true,
            isOutputsFocusedReport: true);

        var result = DefaultResourceRenderer.ResolveScenarioFormatting(change, context);

        result.UseKnownAfterApplyFormatting.Should().BeTrue();
        result.UseEphemeralOpenFormatting.Should().BeTrue();
        result.UseOutputsFocusedFormatting.Should().BeTrue();
    }

    /// <summary>
    /// Creates a baseline render context with no scenario overrides.
    /// </summary>
    /// <returns>Render context instance.</returns>
    private static RenderContext CreateContext()
    {
        return new RenderContext(
            showSensitive: false,
            showUnchangedValues: false,
            ignoreAzureIdCaseChanges: true,
            renderTarget: RenderTarget.AzureDevOps,
            detailsDisplayMode: DetailsDisplayMode.Auto);
    }

    /// <summary>
    /// Creates a resource change model for scenario detection tests.
    /// </summary>
    /// <param name="resourceType">Terraform resource type.</param>
    /// <param name="moduleAddress">Optional module address.</param>
    /// <param name="attributes">Attribute changes.</param>
    /// <param name="action">Terraform action.</param>
    /// <param name="configurationReferences">Optional configuration references.</param>
    /// <returns>Constructed resource change model.</returns>
    private static ResourceChangeModel CreateChange(
        string resourceType,
        string? moduleAddress,
        IReadOnlyList<AttributeChangeModel> attributes,
        string action = "create",
        IReadOnlyDictionary<string, IReadOnlyList<string>>? configurationReferences = null)
    {
        return new ResourceChangeModel
        {
            Address = $"{resourceType}.main",
            ModuleAddress = moduleAddress,
            Type = resourceType,
            Name = "main",
            ProviderName = "registry.terraform.io/hashicorp/azurerm",
            Action = action,
            ActionSymbol = "➕",
            AttributeChanges = attributes,
            ConfigurationReferences = configurationReferences
                ?? new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        };
    }

    /// <summary>
    /// Test render context that supplies scenario flags through <see cref="IScenarioRenderContext"/>.
    /// </summary>
    private sealed class ScenarioRenderContext : IRenderContext, IScenarioRenderContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScenarioRenderContext"/> class.
        /// </summary>
        /// <param name="isKnownAfterApplyScenario">Known-after-apply scenario flag.</param>
        /// <param name="isEphemeralOpenScenario">Ephemeral-open scenario flag.</param>
        /// <param name="isOutputsFocusedReport">Outputs-focused scenario flag.</param>
        public ScenarioRenderContext(
            bool isKnownAfterApplyScenario,
            bool isEphemeralOpenScenario,
            bool isOutputsFocusedReport)
        {
            IsKnownAfterApplyScenario = isKnownAfterApplyScenario;
            IsEphemeralOpenScenario = isEphemeralOpenScenario;
            IsOutputsFocusedReport = isOutputsFocusedReport;
        }

        /// <inheritdoc />
        public bool IsKnownAfterApplyScenario { get; }

        /// <inheritdoc />
        public bool IsEphemeralOpenScenario { get; }

        /// <inheritdoc />
        public bool IsOutputsFocusedReport { get; }

        /// <inheritdoc />
        public bool ShowSensitive => false;

        /// <inheritdoc />
        public bool ShowUnchangedValues => false;

        /// <inheritdoc />
        public bool IgnoreAzureIdCaseChanges => true;

        /// <inheritdoc />
        public RenderTarget RenderTarget => RenderTarget.AzureDevOps;

        /// <inheritdoc />
        public DetailsDisplayMode DetailsDisplayMode => DetailsDisplayMode.Auto;

        /// <inheritdoc />
        public ValueFormatterRegistry? ValueFormatterRegistry => null;

        /// <inheritdoc />
        public IconProviderRegistry? IconProviderRegistry => null;
    }
}
