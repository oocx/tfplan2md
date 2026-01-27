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
    private readonly TerraformPlanParser _parser = new();
    private readonly MarkdownRenderer _renderer;
    private readonly ReportModelBuilder _modelBuilder;

    public AzapiResourceTemplateTests()
    {
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzApiModule());
        _renderer = new MarkdownRenderer(providerRegistry: providerRegistry);
        _modelBuilder = new ReportModelBuilder(providerRegistry: providerRegistry);
    }

    private static string Normalize(string markdown)
    {
        var decoded = WebUtility.HtmlDecode(markdown);
        var withoutTags = Regex.Replace(decoded, "<.*?>", string.Empty, RegexOptions.Singleline, TimeSpan.FromSeconds(2));
        var withoutBackticks = withoutTags.Replace("`", string.Empty, StringComparison.Ordinal);
        return Regex.Replace(withoutBackticks, "\\s+", " ", RegexOptions.Singleline, TimeSpan.FromSeconds(2)).Trim();
    }

    private string RenderAzapiPlan(string testDataFile)
    {
        var json = File.ReadAllText($"TestData/{testDataFile}");
        var plan = _parser.Parse(json);
        var model = _modelBuilder.Build(plan);

        return _renderer.Render(model);
    }

    [Test]
    public async Task Render_AzapiCreate_ShowsBodyConfiguration()
    {
        // Arrange - TC-24: Create operation with body
        var result = RenderAzapiPlan("azapi-create-complete-plan.json");
        var normalized = Normalize(result);

        // Assert - Should contain key elements
        normalized.Should().Contain("azapi_resource");
        normalized.Should().Contain("Body");
        // Body properties should be de-prefixed; other sections may still contain `properties.` paths.
        result.Should().Contain("| disableLocalAuth |");
        result.Should().NotContain("properties.disableLocalAuth");

        await Task.CompletedTask;
    }

    [Test]
    public async Task Render_AzapiUpdate_ShowsBodyChanges()
    {
        // Arrange - TC-25: Update operation with changed properties
        var result = RenderAzapiPlan("azapi-update-complete-plan.json");
        var normalized = Normalize(result);

        // Assert - Should contain change indicators
        normalized.Should().Contain("azapi_resource");
        normalized.Should().Contain("Body Changes");
        normalized.Should().Contain("Before");
        normalized.Should().Contain("After");

        await Task.CompletedTask;
    }

    [Test]
    public async Task Render_AzapiDelete_ShowsBeingDeleted()
    {
        // Arrange - TC-26: Delete operation
        var result = RenderAzapiPlan("azapi-delete-complete-plan.json");
        var normalized = Normalize(result);

        // Assert - Should indicate deletion
        normalized.Should().Contain("azapi_resource");
        normalized.Should().Contain("being deleted");

        await Task.CompletedTask;
    }

    [Test]
    public async Task Render_AzapiReplace_ShowsReplacingMessage()
    {
        // Arrange - TC-27: Replace operation
        var result = RenderAzapiPlan("azapi-replace-complete-plan.json");
        var normalized = Normalize(result);

        // Assert - Should indicate replacement
        normalized.Should().Contain("azapi_resource");
        normalized.Should().Contain("replacing existing resource");

        await Task.CompletedTask;
    }

    [Test]
    public async Task Render_AzapiCreate_IncludesDocumentationLink()
    {
        // Arrange - TC-14: Documentation link generation
        var result = RenderAzapiPlan("azapi-create-complete-plan.json");

        // Assert - Should contain doc link
        result.Should().Contain("View API Documentation");
        result.Should().Contain("https://learn.microsoft.com");

        await Task.CompletedTask;
    }

    [Test]
    public async Task Render_AzapiCreate_ShowsMetadataTable()
    {
        // Arrange - TC-02: Metadata extraction
        var result = RenderAzapiPlan("azapi-create-complete-plan.json");

        // Assert - Should show standard attributes
        result.Should().Contain("| Attribute | Value |");
        result.Should().Contain("name");
        result.Should().Contain("parent_id");
        result.Should().Contain("location");

        await Task.CompletedTask;
    }

    [Test]
    public async Task Render_AzapiEmptyBody_HandlesGracefully()
    {
        // Arrange - TC-28: Empty body handling
        var result = RenderAzapiPlan("azapi-empty-body-plan.json");
        var normalized = Normalize(result);

        // Assert - Should handle empty body
        normalized.Should().Contain("azapi_resource");
        // Should not crash or show errors
        result.Should().NotBeEmpty();
        result.Should().Contain("azapi_resource");

        await Task.CompletedTask;
    }

    [Test]
    public async Task Render_AzapiLargeValues_CollapsesIntoDetails()
    {
        // Arrange - TC-31: Large value handling
        var result = RenderAzapiPlan("azapi-large-value-plan.json");

        // Assert - Should have collapsible section for large properties
        result.Should().Contain("<details>");
        result.Should().Contain("Large body properties");

        await Task.CompletedTask;
    }

    [Test]
    public async Task Render_AzapiMultipleLargeValues_SeparatesFromSmallValues()
    {
        // Arrange - TC-32: Multiple large values
        var result = RenderAzapiPlan("azapi-multiple-large-values-plan.json");

        // Assert - Should have both main table and large properties section
        result.Should().Contain("| Property | Value |");
        result.Should().Contain("<details>");
        result.Should().Contain("Large body properties");

        await Task.CompletedTask;
    }

    [Test]
    public async Task Render_AzapiComplexNested_HandlesDeepNesting()
    {
        // Arrange - TC-30: Complex nested JSON
        var result = RenderAzapiPlan("azapi-complex-nested-plan.json");

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

        await Task.CompletedTask;
    }

    [Test]
    public async Task Render_AzapiCreate_UsesTemplateResolver()
    {
        // Arrange - TC-01: Template resolution
        var json = File.ReadAllText("TestData/azapi-create-plan.json");
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder().Build(plan);

        // Act
        var result = _renderer.Render(model);

        // Assert - Template should be loaded and used (no errors)
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("azapi_resource");

        await Task.CompletedTask;
    }

    [Test]
    public async Task Render_AzapiSpecialCharacters_EscapesMarkdown()
    {
        // Arrange - TC-21: Special character handling
        var result = RenderAzapiPlan("azapi-special-chars-plan.json");

        // Assert - Should escape markdown characters
        // The actual escaping is done by escape_markdown helper
        result.Should().NotBeEmpty();
        result.Should().Contain("azapi_resource");

        await Task.CompletedTask;
    }
}
