using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureRM;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Integration tests for the <c>--ignore-case-changes</c> feature in <see cref="ReportModelBuilder"/>.
/// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
/// </summary>
/// <remarks>
/// These tests use <c>azurerm-case-only-ids-plan.json</c> which contains:
/// - <c>azurerm_role_assignment.casing_only</c>: two attributes that differ only in Azure ID casing.
/// - <c>azurerm_role_assignment.mixed_changes</c>: one Azure ID casing change + one genuine change.
/// - <c>azurerm_key_vault.null_before</c>: null before value.
/// - <c>azurerm_key_vault.null_after</c>: null after value.
/// - <c>azurerm_key_vault.numeric_change</c>: numeric attribute change.
/// - <c>azurerm_key_vault.unchanged</c>: ordinal-equal value.
/// - <c>azurerm_role_assignment.display_name_casing</c>: non-Azure-ID casing change.
/// - <c>random_string.non_azurerm</c>: Azure-ID-shaped value in a non-azurerm provider.
/// </remarks>
[Category("Unit")]
public class ReportModelBuilderIgnoreCaseChangesTests
{
    private readonly TerraformPlanParser _parser = new();
    private readonly string _planJson = File.ReadAllText("TestData/azurerm-case-only-ids-plan.json");

    /// <summary>
    /// Creates a <see cref="ReportModelBuilder"/> with the AzureRM filter registry wired up.
    /// </summary>
    /// <param name="ignoreCaseChanges">Whether to enable the case-insensitive filter.</param>
    /// <param name="showUnchangedValues">Whether to include unchanged values.</param>
    private static ReportModelBuilder CreateBuilder(
        bool ignoreCaseChanges = false,
        bool showUnchangedValues = false)
    {
        var filterRegistry = new AttributeChangeFilterRegistry();
        var azureRmModule = new AzureRMModule(
            largeValueFormat: LargeValueFormat.InlineDiff,
            principalMapper: new NullPrincipalMapper());
        azureRmModule.RegisterAttributeChangeFilters(filterRegistry);

        return new ReportModelBuilder(
            ignoreCaseChanges: ignoreCaseChanges,
            showUnchangedValues: showUnchangedValues,
            attributeChangeFilterRegistry: filterRegistry);
    }

    // -------------------------------------------------------------------------
    // TC-01: Flag absent → casing-only rows are shown (no regression).
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-01: ignoreCaseChanges: false → Azure ID casing-only rows are present in AttributeChanges.
    /// </summary>
    [Test]
    public async Task Build_IgnoreCaseChangesFalse_IncludesCasingOnlyRows()
    {
        // Arrange
        var plan = _parser.Parse(_planJson);
        var builder = CreateBuilder(ignoreCaseChanges: false);

        // Act
        var model = builder.Build(plan);

        // Assert
        var resource = model.Changes.First(c => c.Address == "azurerm_role_assignment.casing_only");
        var attributeNames = resource.AttributeChanges.Select(a => a.Name).ToList();

        attributeNames.Should().Contain("scope",
            "the casing-only row should be shown when flag is absent");
        attributeNames.Should().Contain("role_definition_id",
            "the casing-only row should be shown when flag is absent");

        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // TC-02: Flag active + all-casing resource → resource is suppressed entirely.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-02: ignoreCaseChanges: true on resource with only Azure ID casing differences → resource not rendered.
    /// </summary>
    [Test]
    public async Task Build_IgnoreCaseChangesTrue_AllAzureIdCasingOnly_ResourceSuppressed()
    {
        // Arrange
        var plan = _parser.Parse(_planJson);
        var builder = CreateBuilder(ignoreCaseChanges: true);

        // Act
        var model = builder.Build(plan);

        // Assert
        var resource = model.Changes.FirstOrDefault(c => c.Address == "azurerm_role_assignment.casing_only");
        resource.Should().BeNull(
            "a resource whose only changes are Azure ID casing differences should be completely suppressed from display");

        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // TC-03: Mixed changes → only genuine change retained.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-03: ignoreCaseChanges: true on mixed resource → display_name present, scope absent.
    /// </summary>
    [Test]
    public async Task Build_IgnoreCaseChangesTrue_MixedChanges_RetainsGenuineChanges()
    {
        // Arrange
        var plan = _parser.Parse(_planJson);
        var builder = CreateBuilder(ignoreCaseChanges: true);

        // Act
        var model = builder.Build(plan);

        // Assert
        var resource = model.Changes.First(c => c.Address == "azurerm_role_assignment.mixed_changes");
        var attributeNames = resource.AttributeChanges.Select(a => a.Name).ToList();

        attributeNames.Should().Contain("display_name",
            "genuine content change should be retained");
        attributeNames.Should().NotContain("scope",
            "Azure ID casing-only change should be suppressed");

        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // TC-04: Null before value → row shown (not suppressed).
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-04: Null before value → tenant_id row is present even with flag active.
    /// </summary>
    [Test]
    public async Task Build_IgnoreCaseChangesTrue_NullBeforeValue_RowIsShown()
    {
        // Arrange
        var plan = _parser.Parse(_planJson);
        var builder = CreateBuilder(ignoreCaseChanges: true);

        // Act
        var model = builder.Build(plan);

        // Assert
        var resource = model.Changes.First(c => c.Address == "azurerm_key_vault.null_before");
        var attributeNames = resource.AttributeChanges.Select(a => a.Name).ToList();

        attributeNames.Should().Contain("tenant_id",
            "null before value should not be suppressed by the case filter");

        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // TC-05: Null after value → row shown.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-05: Null after value → tenant_id row is present.
    /// </summary>
    [Test]
    public async Task Build_IgnoreCaseChangesTrue_NullAfterValue_RowIsShown()
    {
        // Arrange
        var plan = _parser.Parse(_planJson);
        var builder = CreateBuilder(ignoreCaseChanges: true);

        // Act
        var model = builder.Build(plan);

        // Assert
        var resource = model.Changes.First(c => c.Address == "azurerm_key_vault.null_after");
        var attributeNames = resource.AttributeChanges.Select(a => a.Name).ToList();

        attributeNames.Should().Contain("tenant_id",
            "null after value should not be suppressed by the case filter");

        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // TC-06: Numeric attribute change → row shown.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-06: Numeric attribute change → soft_delete_retention_days row is present.
    /// </summary>
    [Test]
    public async Task Build_IgnoreCaseChangesTrue_NumericAttributeChange_RowIsShown()
    {
        // Arrange
        var plan = _parser.Parse(_planJson);
        var builder = CreateBuilder(ignoreCaseChanges: true);

        // Act
        var model = builder.Build(plan);

        // Assert
        var resource = model.Changes.First(c => c.Address == "azurerm_key_vault.numeric_change");
        var attributeNames = resource.AttributeChanges.Select(a => a.Name).ToList();

        attributeNames.Should().Contain("soft_delete_retention_days",
            "numeric changes should never be suppressed by the case filter");

        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // TC-07: --ignore-case-changes + --show-unchanged-values interaction.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-07: ignoreCaseChanges: true AND showUnchangedValues: true → Azure ID casing rows absent;
    /// ordinal-equal rows present; genuine changes present.
    /// </summary>
    [Test]
    public async Task Build_IgnoreCaseChangesTrue_AndShowUnchangedValues_CasingRowsStillSuppressed()
    {
        // Arrange
        var plan = _parser.Parse(_planJson);
        var builder = CreateBuilder(ignoreCaseChanges: true, showUnchangedValues: true);

        // Act
        var model = builder.Build(plan);

        // Assert: casing-only resource is suppressed entirely (not rendered at all)
        var casingOnly = model.Changes.FirstOrDefault(c => c.Address == "azurerm_role_assignment.casing_only");
        casingOnly.Should().BeNull(
            "resource with only Azure ID casing changes must be suppressed from display even with --show-unchanged-values active");

        // Assert: genuine change is retained
        var mixed = model.Changes.First(c => c.Address == "azurerm_role_assignment.mixed_changes");
        mixed.AttributeChanges.Select(a => a.Name).Should().Contain("display_name",
            "genuine content changes must be visible");

        // Assert: ordinal-equal rows are shown (showUnchangedValues: true)
        var unchanged = model.Changes.First(c => c.Address == "azurerm_key_vault.unchanged");
        unchanged.AttributeChanges.Select(a => a.Name).Should().Contain("name",
            "ordinal-equal rows should be shown when --show-unchanged-values is active");

        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // TC-11: Model.IgnoreCaseChanges reflects flag value.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-11: Default build (no flag) → model.IgnoreCaseChanges is false.
    /// </summary>
    [Test]
    public async Task Build_Default_IgnoreCaseChangesFalseInModel()
    {
        // Arrange
        var plan = _parser.Parse(_planJson);
        var builder = new ReportModelBuilder(); // no ignoreCaseChanges arg

        // Act
        var model = builder.Build(plan);

        // Assert
        model.IgnoreCaseChanges.Should().BeFalse(
            "default builder should have IgnoreCaseChanges = false");

        await Task.CompletedTask;
    }

    /// <summary>
    /// TC-12: ignoreCaseChanges: true → model.IgnoreCaseChanges is true.
    /// </summary>
    [Test]
    public async Task Build_WithIgnoreCaseChangesTrue_ModelReflectsFlag()
    {
        // Arrange
        var plan = _parser.Parse(_planJson);
        var builder = CreateBuilder(ignoreCaseChanges: true);

        // Act
        var model = builder.Build(plan);

        // Assert
        model.IgnoreCaseChanges.Should().BeTrue(
            "model should reflect the flag value set in the builder");

        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // TC-13: Ordinal-equal value absent when showUnchangedValues: false.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-13: Ordinal-equal value in azurerm_key_vault.unchanged absent when showUnchangedValues: false.
    /// </summary>
    [Test]
    public async Task Build_IgnoreCaseChangesTrue_OrdinallyEqualValues_BehavesLikeUnchanged()
    {
        // Arrange
        var plan = _parser.Parse(_planJson);
        var builder = CreateBuilder(ignoreCaseChanges: true, showUnchangedValues: false);

        // Act
        var model = builder.Build(plan);

        // Assert
        var resource = model.Changes.FirstOrDefault(c => c.Address == "azurerm_key_vault.unchanged");
        // The resource may appear as no-op and be filtered out of displayChanges,
        // or if it appears its attribute changes should be empty.
        if (resource is not null)
        {
            resource.AttributeChanges.Select(a => a.Name).Should().NotContain("name",
                "ordinal-equal values should be hidden when showUnchangedValues is false");
        }

        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // TC-14: Scriban variable ignore_case_changes is true when flag active.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-14: Scriban template {{ ignore_case_changes }} renders "true" when flag is active.
    /// </summary>
    [Test]
    public async Task Render_IgnoreCaseChangesTrue_ScribanVariableIsTrue()
    {
        // Arrange
        var plan = _parser.Parse(_planJson);
        var builder = CreateBuilder(ignoreCaseChanges: true);
        var model = builder.Build(plan);

        // Act: map model to Scriban script object and check the key
        var scriptObject = AotScriptObjectMapper.MapReportModel(model);

        // Assert
        scriptObject["ignore_case_changes"].Should().Be(true,
            "the Scriban variable 'ignore_case_changes' must be true when flag is active");

        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // TC-15: Non-Azure-ID string casing change is NOT suppressed.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-15: display_name "MyApp" → "myapp" (non-Azure-ID, casing-only) → row is shown.
    /// </summary>
    [Test]
    public async Task Build_IgnoreCaseChangesTrue_NonAzureIdStringCasingChange_RowIsShown()
    {
        // Arrange
        var plan = _parser.Parse(_planJson);
        var builder = CreateBuilder(ignoreCaseChanges: true);

        // Act
        var model = builder.Build(plan);

        // Assert
        var resource = model.Changes.First(c => c.Address == "azurerm_role_assignment.display_name_casing");
        resource.AttributeChanges.Select(a => a.Name).Should().Contain("display_name",
            "non-Azure-ID casing changes must NOT be suppressed");

        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // TC-16: Non-azurerm provider rows are NOT filtered.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-16: random_string.non_azurerm has Azure-ID-shaped values but a non-azurerm provider → row shown.
    /// </summary>
    [Test]
    public async Task Build_IgnoreCaseChangesTrue_NonAzureRmProvider_RowIsShown()
    {
        // Arrange
        var plan = _parser.Parse(_planJson);
        var builder = CreateBuilder(ignoreCaseChanges: true);

        // Act
        var model = builder.Build(plan);

        // Assert
        var resource = model.Changes.First(c => c.Address == "random_string.non_azurerm");
        resource.AttributeChanges.Select(a => a.Name).Should().Contain("result",
            "Azure-ID-shaped values in a non-azurerm provider must NOT be suppressed");

        await Task.CompletedTask;
    }
}
