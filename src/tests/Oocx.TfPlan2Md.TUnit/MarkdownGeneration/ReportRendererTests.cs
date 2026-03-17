using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.RenderTargets;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for the pure C# report rendering pipeline.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// Related test plan: docs/features/107-remove-scriban/test-plan.md (TC-RR, TC-HR, TC-SR, TC-DR).
/// </summary>
public class ReportRendererTests
{
    /// <summary>
    /// Verifies empty report renders header and summary without resource sections.
    /// </summary>
    [Test]
    public void Render_EmptyModel_ContainsOnlyHeaderAndSummary()
    {
        var model = CreateModel(moduleChanges: []);
        var renderer = new ReportRenderer();

        var markdown = renderer.Render(model, CreateContext());

        markdown.Should().Contain("# Terraform Plan Report");
        markdown.Should().Contain("## Summary");
        markdown.Should().Contain("No changes");
        markdown.Should().NotContain("## Resource Changes");
    }

    /// <summary>
    /// Verifies root-module resources render with root module heading.
    /// </summary>
    [Test]
    public void Render_RootModule_ModuleHeadingInOutput()
    {
        var model = CreateModel(moduleChanges:
        [
            new ModuleChangeGroup
            {
                ModuleAddress = string.Empty,
                Changes = [CreateChange("azurerm_resource_group")]
            }
        ]);

        var renderer = new ReportRenderer();
        var markdown = renderer.Render(model, CreateContext());

        markdown.Should().Contain("### 📦\u00A0Module: root");
    }

    /// <summary>
    /// Verifies named module resources render module heading.
    /// </summary>
    [Test]
    public void Render_NamedModule_ModuleHeadingInOutput()
    {
        var model = CreateModel(moduleChanges:
        [
            new ModuleChangeGroup
            {
                ModuleAddress = "module.network",
                Changes = [CreateChange("azurerm_resource_group")]
            }
        ]);

        var renderer = new ReportRenderer();
        var markdown = renderer.Render(model, CreateContext());

        markdown.Should().Contain("### 📦\u00A0Module: `module.network`");
    }

    /// <summary>
    /// Verifies refactoring section appears when operations exist.
    /// </summary>
    [Test]
    public void Render_ModelWithRefactoringOperations_RefactoringSectionPresent()
    {
        var model = CreateModel(
            moduleChanges: [],
            operations:
            [
                new RefactoringOperationModel
                {
                    Operation = "Import",
                    Address = "azurerm_resource_group.main",
                    ResourceType = "azurerm_resource_group",
                    ResourceName = "main",
                    Details = "/subscriptions/x/resourceGroups/rg-main",
                    Status = "Ready",
                    IsAlreadyApplied = false
                }
            ]);

        var renderer = new ReportRenderer();
        var markdown = renderer.Render(model, CreateContext());

        markdown.Should().Contain("## Refactoring Summary");
        markdown.Should().Contain("📥\u00A0Import");
    }

    /// <summary>
    /// Verifies global outputs section appears when outputs exist.
    /// </summary>
    [Test]
    public void Render_ModelWithGlobalOutputs_OutputsSectionPresent()
    {
        var model = CreateModel(
            moduleChanges: [],
            globalOutputs:
            [
                new OutputChangeModel
                {
                    Name = "principal_id",
                    Description = "Principal Id",
                    IsSensitive = false,
                    Action = "update",
                    ActionSymbol = "🔄",
                    ProviderName = "registry.terraform.io/hashicorp/azurerm",
                    Value = "abc",
                    IsComputed = false,
                    IsMasked = false,
                    ModuleAddress = string.Empty,
                    IsLargeOutputValue = false,
                    ReferencedAttributeName = "principal_id"
                }
            ]);

        var renderer = new ReportRenderer();
        var markdown = renderer.Render(model, CreateContext());

        markdown.Should().Contain("## 📤\u00A0Outputs");
        markdown.Should().Contain("`principal_id`");
    }

    /// <summary>
    /// Verifies unregistered resource types use default resource renderer.
    /// </summary>
    [Test]
    public void Render_UnknownResourceType_FallsBackToDefaultResourceRenderer()
    {
        var model = CreateModel(moduleChanges:
        [
            new ModuleChangeGroup
            {
                ModuleAddress = string.Empty,
                Changes =
                [
                    new ResourceChangeModel
                    {
                        Address = "unknown_resource.main",
                        Type = "unknown_resource",
                        Name = "main",
                        ProviderName = "registry.terraform.io/hashicorp/random",
                        Action = "create",
                        ActionSymbol = "➕",
                        AttributeChanges =
                        [
                            new AttributeChangeModel
                            {
                                Name = "name",
                                After = "value"
                            }
                        ],
                        SummaryHtml = "➕\u00A0unknown_resource `main`"
                    }
                ]
            }
        ]);

        var renderer = new ReportRenderer();
        var markdown = renderer.Render(model, CreateContext());

        markdown.Should().Contain("<details");
        markdown.Should().Contain("| Attribute | Value |");
        markdown.Should().Contain("unknown_resource");
    }

    /// <summary>
    /// Verifies that a sensitive AND large output shows "(sensitive value)" in the table cell,
    /// not "_(see below)_".
    /// Related issue: docs/issues/fix-sensitive-large-value-rendering/analysis.md (Bug 1A).
    /// </summary>
    [Test]
    public void Render_SensitiveLargeOutput_TableCellShowsSensitiveValue()
    {
        // A value long enough to be classified as large (> 80 chars compact) but also sensitive.
        var largeSecret = new string('x', 90);
        var model = CreateModel(
            moduleChanges: [],
            globalOutputs:
            [
                new OutputChangeModel
                {
                    Name = "jwt_token",
                    Description = "A large sensitive value",
                    IsSensitive = true,
                    Action = "create",
                    ActionSymbol = "➕",
                    ProviderName = "registry.terraform.io/hashicorp/azurerm",
                    Value = largeSecret,
                    IsComputed = false,
                    IsMasked = true,
                    ModuleAddress = string.Empty,
                    IsLargeOutputValue = true,
                    ReferencedAttributeName = "jwt_token"
                }
            ]);

        var renderer = new ReportRenderer();
        var markdown = renderer.Render(model, CreateContext());

        markdown.Should().Contain("(sensitive value)");
        markdown.Should().NotContain("_(see below)_");
    }

    /// <summary>
    /// Verifies that a sensitive AND large output does not render the raw value in a below-table block.
    /// Related issue: docs/issues/fix-sensitive-large-value-rendering/analysis.md (Bug 1B).
    /// </summary>
    [Test]
    public void Render_SensitiveLargeOutput_BelowTableBlockOmitted()
    {
        // A value long enough to be classified as large (> 80 chars compact) but also sensitive.
        var largeSecret = new string('s', 90);
        var model = CreateModel(
            moduleChanges: [],
            globalOutputs:
            [
                new OutputChangeModel
                {
                    Name = "access_token",
                    Description = "A large sensitive value",
                    IsSensitive = true,
                    Action = "create",
                    ActionSymbol = "➕",
                    ProviderName = "registry.terraform.io/hashicorp/azurerm",
                    Value = largeSecret,
                    IsComputed = false,
                    IsMasked = true,
                    ModuleAddress = string.Empty,
                    IsLargeOutputValue = true,
                    ReferencedAttributeName = "access_token"
                }
            ]);

        var renderer = new ReportRenderer();
        var markdown = renderer.Render(model, CreateContext());

        // The raw secret must not appear anywhere in the rendered markdown.
        markdown.Should().NotContain(largeSecret);
        // No code block should be emitted for this output.
        markdown.Should().NotContain("```json");
    }

    /// <summary>
    /// Verifies that a large JSON array output is rendered as pretty-printed JSON in the below-table block.
    /// Related issue: docs/issues/fix-sensitive-large-value-rendering/analysis.md (Bug 2).
    /// </summary>
    [Test]
    public void Render_LargeJsonArrayOutput_BelowTableIsPrettyPrinted()
    {
        // Build a JsonElement representing a large array value.
        var json = """[{"principal":"user@example.com","role":"Contributor"},{"principal":"sp@tenant.io","role":"Reader"}]""";
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var jsonElement = doc.RootElement.Clone();

        var model = CreateModel(
            moduleChanges: [],
            globalOutputs:
            [
                new OutputChangeModel
                {
                    Name = "role_assignments",
                    Description = "Role assignments",
                    IsSensitive = false,
                    Action = "create",
                    ActionSymbol = "➕",
                    ProviderName = "registry.terraform.io/hashicorp/azurerm",
                    Value = jsonElement,
                    IsComputed = false,
                    IsMasked = false,
                    ModuleAddress = string.Empty,
                    IsLargeOutputValue = true,
                    ReferencedAttributeName = "role_assignments"
                }
            ]);

        var renderer = new ReportRenderer();
        var markdown = renderer.Render(model, CreateContext());

        // Pretty-printed JSON must contain line breaks (indented output).
        markdown.Should().Contain("```json");
        markdown.Should().Contain("\n  {");
    }

    /// <summary>
    /// Creates render context for report renderer tests.
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
    /// Creates minimal report model for renderer tests.
    /// </summary>
    /// <param name="moduleChanges">Module changes to include.</param>
    /// <param name="operations">Optional refactoring operations.</param>
    /// <param name="globalOutputs">Optional global outputs.</param>
    /// <returns>Report model.</returns>
    private static ReportModel CreateModel(
        IReadOnlyList<ModuleChangeGroup> moduleChanges,
        IReadOnlyList<RefactoringOperationModel>? operations = null,
        IReadOnlyList<OutputChangeModel>? globalOutputs = null)
    {
        return new ReportModel
        {
            TerraformVersion = "1.10.5",
            FormatVersion = "1.2",
            TfPlan2MdVersion = "1.0.0",
            CommitHash = "abcdef1",
            GeneratedAtUtc = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            HideMetadata = false,
            Timestamp = null,
            ReportTitle = null,
            Changes = moduleChanges.SelectMany(group => group.Changes).ToList(),
            ModuleChanges = moduleChanges,
            Summary = new SummaryModel
            {
                ToAdd = new ActionSummary(0, []),
                ToChange = new ActionSummary(0, []),
                ToDestroy = new ActionSummary(0, []),
                ToReplace = new ActionSummary(0, []),
                NoOp = new ActionSummary(0, []),
                Total = 0
            },
            CodeAnalysis = null,
            ShowUnchangedValues = false,
            IgnoreAzureIdCaseChanges = true,
            ShowSensitive = false,
            RenderTarget = RenderTarget.AzureDevOps,
            DetailsDisplayMode = DetailsDisplayMode.Auto,
            RefactoringOperations = operations ?? [],
            GlobalOutputs = globalOutputs ?? []
        };
    }

    /// <summary>
    /// Creates a basic resource change model for tests.
    /// </summary>
    /// <param name="type">Terraform resource type.</param>
    /// <returns>Resource change model.</returns>
    private static ResourceChangeModel CreateChange(string type)
    {
        return new ResourceChangeModel
        {
            Address = $"{type}.main",
            Type = type,
            Name = "main",
            ProviderName = "registry.terraform.io/hashicorp/azurerm",
            Action = "create",
            ActionSymbol = "➕",
            AttributeChanges = [],
            SummaryHtml = $"➕\u00A0{type} `main`"
        };
    }
}
