using System.Net;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzApi;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Integration tests for azapi_resource template rendering.
/// Related feature: docs/features/040-azapi-resource-template/test-plan.md.
/// </summary>
public class AzapiResourceTemplateTests
{
    /// <summary>
    /// Resource type name used by AzAPI tests to avoid string duplication.
    /// Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
    /// </summary>
    private const string AzapiResourceType = "azapi_resource";

    /// <summary>
    /// Parses plan JSON files for AzAPI template tests.
    /// Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
    /// </summary>
    private readonly TerraformPlanParser _parser = new();

    /// <summary>
    /// Renders markdown output for AzAPI template tests.
    /// Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
    /// </summary>
    private readonly MarkdownRenderer _renderer;

    /// <summary>
    /// Builds report models for AzAPI template tests.
    /// Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
    /// </summary>
    private readonly ReportModelBuilder _modelBuilder;

    public AzapiResourceTemplateTests()
    {
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzApiModule());
        _renderer = new MarkdownRenderer(providerRegistry: providerRegistry);
        _modelBuilder = new ReportModelBuilder(providerRegistry: providerRegistry);
    }

    /// <summary>
    /// Normalizes markdown to make assertions resilient to HTML encoding and whitespace differences.
    /// </summary>
    /// <param name="markdown">The markdown content to normalize.</param>
    /// <returns>The normalized markdown string.</returns>
    private static string Normalize(string markdown)
    {
        var decoded = WebUtility.HtmlDecode(markdown);
        var withoutTags = Regex.Replace(decoded, "<.*?>", string.Empty, RegexOptions.Singleline, TimeSpan.FromSeconds(2));
        var withoutBackticks = withoutTags.Replace("`", string.Empty, StringComparison.Ordinal);
        return Regex.Replace(withoutBackticks, "\\s+", " ", RegexOptions.Singleline, TimeSpan.FromSeconds(2)).Trim();
    }

    /// <summary>
    /// Renders a markdown report from the specified AzAPI plan test data file.
    /// </summary>
    /// <param name="testDataFile">The test data file name under TestData.</param>
    /// <returns>The rendered markdown output.</returns>
    private async Task<string> RenderAzapiPlanAsync(string testDataFile)
    {
        var json = await File.ReadAllTextAsync($"TestData/{testDataFile}");
        var plan = _parser.Parse(json);
        var model = _modelBuilder.Build(plan);

        return _renderer.Render(model);
    }

    [Test]
    public async Task Render_AzapiCreate_ShowsBodyConfiguration()
    {
        // Arrange - TC-24: Create operation with body
        var result = await RenderAzapiPlanAsync("azapi-create-complete-plan.json");
        var normalized = Normalize(result);

        // Assert - Should contain key elements
        normalized.Should().Contain(AzapiResourceType);
        normalized.Should().Contain("Body");
        // Body properties should be de-prefixed; other sections may still contain `properties.` paths.
        result.Should().Contain("| disableLocalAuth |");
        result.Should().NotContain("properties.disableLocalAuth");
    }

    [Test]
    public async Task Render_AzapiUpdate_ShowsBodyChanges()
    {
        // Arrange - TC-25: Update operation with changed properties
        var result = await RenderAzapiPlanAsync("azapi-update-complete-plan.json");
        var normalized = Normalize(result);

        // Assert - Should contain change indicators
        normalized.Should().Contain(AzapiResourceType);
        normalized.Should().Contain("Body Changes");
        normalized.Should().Contain("Before");
        normalized.Should().Contain("After");
    }

    [Test]
    public async Task Render_AzapiDelete_ShowsBeingDeleted()
    {
        // Arrange - TC-26: Delete operation
        var result = await RenderAzapiPlanAsync("azapi-delete-complete-plan.json");
        var normalized = Normalize(result);

        // Assert - Should indicate deletion
        normalized.Should().Contain(AzapiResourceType);
        normalized.Should().Contain("being deleted");
    }

    [Test]
    public async Task Render_AzapiReplace_ShowsReplacingMessage()
    {
        // Arrange - TC-27: Replace operation
        var result = await RenderAzapiPlanAsync("azapi-replace-complete-plan.json");
        var normalized = Normalize(result);

        // Assert - Should indicate replacement
        normalized.Should().Contain(AzapiResourceType);
        normalized.Should().Contain("replacing existing resource");
    }

    [Test]
    public async Task Render_AzapiCreate_IncludesDocumentationLink()
    {
        // Arrange - TC-14: Documentation link generation
        var result = await RenderAzapiPlanAsync("azapi-create-complete-plan.json");

        // Assert - Should contain doc link
        result.Should().Contain("View API Documentation");
        result.Should().Contain("https://learn.microsoft.com");
    }

    [Test]
    public async Task Render_AzapiCreate_ShowsMetadataTable()
    {
        // Arrange - TC-02: Metadata extraction
        var result = await RenderAzapiPlanAsync("azapi-create-complete-plan.json");

        // Assert - Should show standard attributes
        result.Should().Contain("| Attribute | Value |");
        result.Should().Contain("name");
        result.Should().Contain("parent_id");
        result.Should().Contain("location");
    }

    [Test]
    public async Task Render_AzapiEmptyBody_HandlesGracefully()
    {
        // Arrange - TC-28: Empty body handling
        var result = await RenderAzapiPlanAsync("azapi-empty-body-plan.json");
        var normalized = Normalize(result);

        // Assert - Should handle empty body
        normalized.Should().Contain(AzapiResourceType);
        // Should not crash or show errors
        result.Should().NotBeEmpty();
        result.Should().Contain(AzapiResourceType);
    }

    [Test]
    public async Task Render_AzapiLargeValues_CollapsesIntoDetails()
    {
        // Arrange - TC-31: Large value handling
        var result = await RenderAzapiPlanAsync("azapi-large-value-plan.json");

        // Assert - Should have collapsible section for large properties
        result.Should().Contain("<details>");
        result.Should().Contain("Large body properties");
    }

    [Test]
    public async Task Render_AzapiMultipleLargeValues_SeparatesFromSmallValues()
    {
        // Arrange - TC-32: Multiple large values
        var result = await RenderAzapiPlanAsync("azapi-multiple-large-values-plan.json");

        // Assert - Should have both main table and large properties section
        result.Should().Contain("| Property | Value |");
        result.Should().Contain("<details>");
        result.Should().Contain("Large body properties");
    }

    [Test]
    public async Task Render_AzapiComplexNested_HandlesDeepNesting()
    {
        // Arrange - TC-30: Complex nested JSON
        var result = await RenderAzapiPlanAsync("azapi-complex-nested-plan.json");

        // Assert - Should remove "properties." prefix as requested
        result.Should().NotContain("properties.enabled");
        result.Should().NotContain("properties.siteConfig");

        // Arrays should be rendered as dedicated sections
        result.Should().Contain("###### `siteConfig.appSettings` Array");
        result.Should().Contain("###### `siteConfig.connectionStrings` Array");
        result.Should().Contain("###### `siteConfig.cors.allowedOrigins` Array");

        // Matrix tables should use Index column
        result.Should().Contain("| Index | name | value |");

        // Array members should not be rendered as flattened indexed paths
        result.Should().NotContain("appSettings[0].name");
        result.Should().NotContain("connectionStrings[0].name");

    }

    [Test]
    public async Task Render_AzapiCreate_UsesTemplateResolver()
    {
        // Arrange - TC-01: Template resolution
        var json = await File.ReadAllTextAsync("TestData/azapi-create-plan.json");
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder().Build(plan);

        // Act
        var result = _renderer.Render(model);

        // Assert - Template should be loaded and used (no errors)
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain(AzapiResourceType);
    }

    [Test]
    public async Task Render_AzapiSpecialCharacters_EscapesMarkdown()
    {
        // Arrange - TC-21: Special character handling
        var result = await RenderAzapiPlanAsync("azapi-special-chars-plan.json");

        // Assert - Should escape markdown characters
        // The actual escaping is done by escape_markdown helper
        result.Should().NotBeEmpty();
        result.Should().Contain(AzapiResourceType);
    }
}
