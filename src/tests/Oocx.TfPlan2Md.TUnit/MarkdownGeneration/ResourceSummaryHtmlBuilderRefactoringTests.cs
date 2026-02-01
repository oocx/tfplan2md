using System.Collections.Generic;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ResourceSummaryHtmlBuilderRefactoringTests
{
    private const string NonBreakingSpace = "\u00A0";

    [Test]
    public void BuildSummaryHtml_Import_IncludesImportedAnnotation()
    {
        // Arrange
        var model = new ResourceChangeModel
        {
            Address = "azurerm_resource_group.existing",
            ModuleAddress = string.Empty,
            Type = "azurerm_resource_group",
            Name = "existing",
            ProviderName = "provider",
            Action = "create",
            ActionSymbol = "➕",
            AttributeChanges = [],
            AfterJson = new Dictionary<string, object?>(),
            ImportId = "rg-existing"
        };

        // Act
        var summary = ResourceSummaryHtmlBuilder.BuildSummaryHtml(model);

        // Assert
        summary.Should().Contain($"📥{NonBreakingSpace}<i>Imported</i>");
    }

    [Test]
    public void BuildSummaryHtml_Move_IncludesMovedFromAnnotation()
    {
        // Arrange
        var movedFrom = "module.old.azurerm_virtual_network.hub";
        var model = new ResourceChangeModel
        {
            Address = "azurerm_virtual_network.hub",
            ModuleAddress = string.Empty,
            Type = "azurerm_virtual_network",
            Name = "hub",
            ProviderName = "provider",
            Action = "create",
            ActionSymbol = "➕",
            AttributeChanges = [],
            AfterJson = new Dictionary<string, object?>(),
            MovedFromAddress = movedFrom
        };

        // Act
        var summary = ResourceSummaryHtmlBuilder.BuildSummaryHtml(model);

        // Assert
        summary.Should().Contain($"🔀{NonBreakingSpace}<i>Moved from</i> <code>{movedFrom}</code>");
    }

    [Test]
    public void BuildSummaryHtml_AlreadyApplied_AddsWarningSuffix()
    {
        // Arrange
        var model = new ResourceChangeModel
        {
            Address = "azurerm_storage_account.legacy",
            ModuleAddress = string.Empty,
            Type = "azurerm_storage_account",
            Name = "legacy",
            ProviderName = "provider",
            Action = "no-op",
            ActionSymbol = " ",
            AttributeChanges = [],
            AfterJson = new Dictionary<string, object?>(),
            ImportId = "legacy-import",
            IsRefactoringAlreadyApplied = true
        };

        // Act
        var summary = ResourceSummaryHtmlBuilder.BuildSummaryHtml(model);

        // Assert
        summary.Should().Contain($"(⚠️{NonBreakingSpace}<i>already applied</i>)");
    }

    [Test]
    public void BuildSummaryHtml_RefactoringIcons_UseNonBreakingSpaces()
    {
        // Arrange
        var model = new ResourceChangeModel
        {
            Address = "azurerm_virtual_network.hub",
            ModuleAddress = string.Empty,
            Type = "azurerm_virtual_network",
            Name = "hub",
            ProviderName = "provider",
            Action = "no-op",
            ActionSymbol = " ",
            AttributeChanges = [],
            AfterJson = new Dictionary<string, object?>(),
            ImportId = "import-id",
            MovedFromAddress = "module.old.azurerm_virtual_network.hub",
            IsRefactoringAlreadyApplied = true
        };

        // Act
        var summary = ResourceSummaryHtmlBuilder.BuildSummaryHtml(model);

        // Assert
        summary.Should().Contain($"📥{NonBreakingSpace}<i>Imported</i>")
            .And.Contain($"🔀{NonBreakingSpace}<i>Moved from</i>")
            .And.Contain($"⚠️{NonBreakingSpace}<i>already applied</i>");
    }
}
