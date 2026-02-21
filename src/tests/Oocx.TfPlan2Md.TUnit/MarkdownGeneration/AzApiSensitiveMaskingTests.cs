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
/// Assertion-based tests verifying that AzApi body rendering masks sensitive values correctly.
/// </summary>
/// <remarks>
/// These tests use direct string assertions (not snapshot baselines) to catch the current
/// broken behavior where sensitive body values are rendered in plaintext.
/// Test plan coverage: TC-01 through TC-05, TC-21.
/// Related issue: docs/issues/098-sensitive-info-exposure/analysis.md.
/// </remarks>
public class AzApiSensitiveMaskingTests
{
    /// <summary>
    /// Parses Terraform plan JSON files for AzAPI tests.
    /// </summary>
    private readonly TerraformPlanParser _parser = new();

    #region TC-01: AzApi create body masks individual sensitive field

    /// <summary>
    /// TC-01: When rendering an <c>azapi_resource</c> create plan where
    /// <c>after_sensitive.body.properties.administratorLoginPassword = true</c>,
    /// the password must be replaced with <c>(sensitive)</c>.
    /// </summary>
    [Test]
    public void RenderAzapiCreate_WithSensitiveBodyProperty_MasksValue()
    {
        // Arrange & Act
        var markdown = RenderAzapiPlan("azapi-sensitive-plan.json");

        // Assert: plaintext password must NOT appear
        markdown.Should().NotContain("P@ssw0rd123!",
            "sensitive body property 'administratorLoginPassword' must be masked in create rendering");

        // Assert: masked placeholder must appear in the body section
        markdown.Should().Contain("(sensitive)",
            "masked placeholder must appear for sensitive body properties");
    }

    #endregion

    #region TC-02: AzApi create body masks when entire body is sensitive

    /// <summary>
    /// TC-02: When <c>after_sensitive.body = true</c>, every property in the Body table must be masked.
    /// </summary>
    [Test]
    public void RenderAzapiCreate_WithAllBodySensitive_MasksAllProperties()
    {
        // Arrange & Act
        var markdown = RenderAzapiPlan("azapi-body-sensitive-plan.json");

        // Assert: no plaintext body values should appear
        markdown.Should().NotContain("12345678-1234-1234-1234-123456789012",
            "tenantId must be masked when entire body is sensitive");
        markdown.Should().NotContain("standard",
            "sku.name must be masked when entire body is sensitive");

        // Assert: masked placeholder present
        markdown.Should().Contain("(sensitive)",
            "masked placeholder must appear for all body properties when body is fully sensitive");
    }

    #endregion

    #region TC-03: AzApi delete body masks sensitive field

    /// <summary>
    /// TC-03: For delete actions the rendered value comes from <c>change.before.body</c>.
    /// If <c>before_sensitive</c> marks a property sensitive, it must be masked.
    /// </summary>
    [Test]
    public void RenderAzapiDelete_WithSensitiveBodyProperty_MasksValue()
    {
        // Arrange & Act
        var markdown = RenderAzapiPlan("azapi-delete-sensitive-plan.json");

        // Assert: the secret value must NOT appear
        markdown.Should().NotContain("actual-secret",
            "sensitive body property 'clientSecret' must be masked in delete rendering");

        // Assert: masked placeholder present
        markdown.Should().Contain("(sensitive)",
            "masked placeholder must appear for sensitive body properties in delete");
    }

    #endregion

    #region TC-04: AzApi replace body masks sensitive field

    /// <summary>
    /// TC-04: Replace actions produce both delete (before) and create (after) sections.
    /// Both must have sensitive values masked.
    /// </summary>
    [Test]
    public void RenderAzapiReplace_WithSensitiveBodyProperty_MasksValue()
    {
        // Arrange & Act
        var markdown = RenderAzapiPlan("azapi-replace-sensitive-plan.json");

        // Assert: neither old nor new secret should appear
        markdown.Should().NotContain("old-secret",
            "sensitive body property must be masked in replace before section");
        markdown.Should().NotContain("new-secret",
            "sensitive body property must be masked in replace after section");

        // Assert: masked placeholder present
        markdown.Should().Contain("(sensitive)",
            "masked placeholder must appear for sensitive body properties in replace");
    }

    #endregion

    #region TC-05: --show-sensitive reveals plaintext in AzApi create body

    /// <summary>
    /// TC-05: When <c>--show-sensitive</c> is enabled, sensitive body values should be rendered as-is.
    /// </summary>
    [Test]
    public void RenderAzapiCreate_ShowSensitive_RevealsValue()
    {
        // Arrange & Act
        var markdown = RenderAzapiPlan("azapi-sensitive-plan.json", showSensitive: true);

        // Assert: plaintext password must appear when show-sensitive is enabled
        markdown.Should().Contain("P@ssw0rd123!",
            "sensitive body values must be visible when --show-sensitive is enabled");
    }

    #endregion

    #region TC-21: Non-sensitive values are not masked

    /// <summary>
    /// TC-21: Non-sensitive body properties must still be visible when rendering a plan
    /// that contains a mix of sensitive and non-sensitive body values.
    /// </summary>
    [Test]
    public void RenderAzapiCreate_NonSensitiveValues_StillVisible()
    {
        // Arrange & Act
        var markdown = RenderAzapiPlan("azapi-sensitive-plan.json");

        // Assert: non-sensitive values must still appear
        markdown.Should().Contain("sqladmin",
            "non-sensitive property 'administratorLogin' must not be masked");
        markdown.Should().Contain("12.0",
            "non-sensitive property 'version' must not be masked");
        markdown.Should().Contain("Enabled",
            "non-sensitive property 'publicNetworkAccess' must not be masked");
    }

    #endregion

    #region TC-06: AzApi update body masks sensitive changed field

    /// <summary>
    /// TC-06: When rendering an <c>azapi_resource</c> update where
    /// <c>after_sensitive.body.properties.clientSecret = true</c>,
    /// the before and after secret values must both be replaced with <c>(sensitive)</c>.
    /// </summary>
    [Test]
    public void RenderAzapiUpdate_WithSensitiveBodyProperty_MasksValue()
    {
        // Arrange & Act
        var markdown = RenderAzapiPlan("azapi-update-sensitive-plan.json");

        // Assert: neither old nor new secret values must appear
        markdown.Should().NotContain("old-secret-value",
            "sensitive body property 'clientSecret' before value must be masked in update rendering");
        markdown.Should().NotContain("new-secret-value",
            "sensitive body property 'clientSecret' after value must be masked in update rendering");

        // Assert: masked placeholder must appear
        markdown.Should().Contain("(sensitive)",
            "masked placeholder must appear for sensitive body properties in update");

        // Assert: non-sensitive changed value must still appear
        markdown.Should().Contain("new-object-id",
            "non-sensitive property 'objectId' must still be visible in update rendering");
    }

    #endregion

    #region TC-07: AzApi update body shows sensitive values with --show-sensitive

    /// <summary>
    /// TC-07: When <c>--show-sensitive</c> is enabled, sensitive update body values should be rendered as-is.
    /// </summary>
    [Test]
    public void RenderAzapiUpdate_ShowSensitive_RevealsValue()
    {
        // Arrange & Act
        var markdown = RenderAzapiPlan("azapi-update-sensitive-plan.json", showSensitive: true);

        // Assert: secret values must appear when show-sensitive is enabled
        markdown.Should().Contain("new-secret-value",
            "sensitive body values must be visible when --show-sensitive is enabled in update mode");
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Renders a markdown report from an AzAPI plan test data file.
    /// </summary>
    /// <param name="testDataFile">The test data file name under TestData.</param>
    /// <param name="showSensitive">Whether to enable show-sensitive mode.</param>
    /// <returns>The rendered markdown output.</returns>
    private string RenderAzapiPlan(string testDataFile, bool showSensitive = false)
    {
        var json = File.ReadAllText(Path.Combine("TestData", testDataFile));
        var plan = _parser.Parse(json);
        var providerRegistry = CreateProviderRegistry();
        var model = new ReportModelBuilder(
            showSensitive: showSensitive,
            metadataProvider: TestMetadataProvider.Instance,
            providerRegistry: providerRegistry).Build(plan);
        var renderer = new MarkdownRenderer(providerRegistry: providerRegistry);

        return renderer.Render(model);
    }

    /// <summary>
    /// Creates a provider registry that includes AzAPI support.
    /// </summary>
    /// <returns>The configured provider registry.</returns>
    private static ProviderRegistry CreateProviderRegistry()
    {
        var registry = new ProviderRegistry();
        registry.RegisterProvider(new AzApiModule());
        return registry;
    }

    #endregion
}
