using System.IO;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzApi;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Integration tests for azapi_update_resource template rendering.
/// Related feature: docs/features/095-azapi-update-resource-grouping/specification.md.
/// </summary>
public class AzapiUpdateResourceTemplateTests
{
    /// <summary>
    /// The expected resource type name for azapi_update_resource.
    /// </summary>
    private const string AzapiUpdateResourceType = "azapi_update_resource";

    /// <summary>
    /// Parses Terraform plan JSON files.
    /// </summary>
    private readonly TerraformPlanParser _parser = new();

    /// <summary>
    /// Renders markdown reports with AzAPI provider support.
    /// </summary>
    private readonly MarkdownRenderer _renderer;

    /// <summary>
    /// Builds report models with AzAPI provider support.
    /// </summary>
    private readonly ReportModelBuilder _modelBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzapiUpdateResourceTemplateTests"/> class.
    /// Configures the provider registry with AzAPI support.
    /// </summary>
    public AzapiUpdateResourceTemplateTests()
    {
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzApiModule());
        _renderer = new MarkdownRenderer(providerRegistry: providerRegistry);
        _modelBuilder = new ReportModelBuilder(
            metadataProvider: TestMetadataProvider.Instance,
            providerRegistry: providerRegistry);
    }

    /// <summary>
    /// Renders a markdown report from a plan JSON file.
    /// </summary>
    /// <param name="testDataFile">The test data file name under TestData.</param>
    /// <returns>The rendered markdown output.</returns>
    private async Task<string> RenderPlanAsync(string testDataFile)
    {
        var json = await File.ReadAllTextAsync($"TestData/{testDataFile}");
        var plan = _parser.Parse(json);
        var model = _modelBuilder.Build(plan);
        return _renderer.Render(model);
    }

    /// <summary>
    /// Verifies that azapi_update_resource update action shows body changes section.
    /// </summary>
    [Test]
    public async Task Render_AzapiUpdateResource_Update_ShowsBodyChanges()
    {
        var result = await RenderPlanAsync("azapi-update-resource-update-plan.json");

        result.Should().Contain(AzapiUpdateResourceType);
        result.Should().Contain("Body Changes");
        result.Should().Contain("Before");
        result.Should().Contain("After");
    }

    /// <summary>
    /// Verifies that azapi_update_resource displays the resource_id attribute.
    /// </summary>
    [Test]
    public async Task Render_AzapiUpdateResource_Update_ShowsResourceId()
    {
        var result = await RenderPlanAsync("azapi-update-resource-update-plan.json");

        result.Should().Contain("resource_id");
        result.Should().Contain("myAccount");
    }

    /// <summary>
    /// Verifies that azapi_update_resource includes Azure API documentation link.
    /// </summary>
    [Test]
    public async Task Render_AzapiUpdateResource_Update_ShowsDocumentationLink()
    {
        var result = await RenderPlanAsync("azapi-update-resource-update-plan.json");

        result.Should().Contain("View API Documentation");
        result.Should().Contain("https://learn.microsoft.com");
    }

    /// <summary>
    /// Verifies that azapi_update_resource groups encryption attributes correctly.
    /// The encryption prefix group should be rendered as a separate section.
    /// </summary>
    [Test]
    public async Task Render_AzapiUpdateResource_Update_GroupsEncryptionAttributes()
    {
        var result = await RenderPlanAsync("azapi-update-resource-update-plan.json");

        // Encryption prefix group should be rendered as a section
        result.Should().Contain("###### `encryption`");
        // Properties should not have "encryption." prefix in the table
        result.Should().NotContain("| encryption.keySource |");
    }

    /// <summary>
    /// Verifies that azapi_update_resource delete action shows "being deleted" message.
    /// </summary>
    [Test]
    public async Task Render_AzapiUpdateResource_Delete_ShowsBeingDeleted()
    {
        var result = await RenderPlanAsync("azapi-update-resource-delete-plan.json");

        result.Should().Contain(AzapiUpdateResourceType);
        result.Should().Contain("being deleted");
    }
}
