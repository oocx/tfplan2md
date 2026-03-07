using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Stages;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Oocx.TfPlan2Md.TUnit.MarkdownGeneration.Stages;

/// <summary>
/// Tests for <see cref="DisplayFilteringStage"/>.
/// </summary>
public class DisplayFilteringStageTests
{
    private const string TestType = "test_type";
    private const string TestProvider = "provider";

    /// <summary>
    /// Creates a minimal resource change model for testing.
    /// </summary>
    private static ResourceChangeModel CreateResourceModel(
        string address,
        string action,
        string? moduleAddress = "",
        List<AttributeChangeModel>? attributeChanges = null,
        string? importId = null,
        string? movedFromAddress = null,
        bool hasWholeResourceUnknownAfterApply = false)
    {
        return new ResourceChangeModel
        {
            Address = address,
            Type = TestType,
            Name = address,
            ProviderName = TestProvider,
            Action = action,
            ActionSymbol = ActionIcons.Add,
            ModuleAddress = moduleAddress ?? string.Empty,
            AttributeChanges = attributeChanges ?? [],
            ImportId = importId,
            MovedFromAddress = movedFromAddress,
            HasWholeResourceUnknownAfterApply = hasWholeResourceUnknownAfterApply
        };
    }

    /// <summary>
    /// TC-01: Verify that no-op resources are filtered out by default.
    /// </summary>
    [Test]
    public async Task DisplayFilteringStage_Build_FiltersNoOpResources()
    {
        // Arrange
        var stage = new DisplayFilteringStage(ignoreAzureIdCaseChanges: false);
        var changes = new List<ResourceChangeModel>
        {
            CreateResourceModel("r1", "create"),
            CreateResourceModel("r2", "no-op"),
            CreateResourceModel("r3", "update")
        };

        // Act
        var result = stage.Build(changes);

        // Assert
        await Assert.That(result.DisplayChanges.Count).IsEqualTo(2);
        await Assert.That(result.DisplayChanges[0].Address).IsEqualTo("r1");
        await Assert.That(result.DisplayChanges[1].Address).IsEqualTo("r3");
        await Assert.That(result.FilteredResourceCount).IsEqualTo(0);
    }

    /// <summary>
    /// TC-02: Verify that Azure ID case-only update resources are filtered when flag is enabled.
    /// </summary>
    [Test]
    public async Task DisplayFilteringStage_Build_FiltersAzureIdCaseOnlyUpdatesWhenEnabled()
    {
        // Arrange
        var stage = new DisplayFilteringStage(ignoreAzureIdCaseChanges: true);
        var changes = new List<ResourceChangeModel>
        {
            CreateResourceModel("r1", "update", attributeChanges: []), // All attribute changes were suppressed
            CreateResourceModel("r2", "update", attributeChanges: [new AttributeChangeModel { Name = "name" }]) // Has real changes
        };

        // Act
        var result = stage.Build(changes);

        // Assert - r1 should be filtered because it has no remaining attribute changes
        await Assert.That(result.DisplayChanges.Count).IsEqualTo(1);
        await Assert.That(result.DisplayChanges[0].Address).IsEqualTo("r2");
        await Assert.That(result.FilteredResourceCount).IsEqualTo(1);
    }

    /// <summary>
    /// TC-03: Verify that Azure ID case-only filtering is bypassed when flag is disabled.
    /// </summary>
    [Test]
    public async Task DisplayFilteringStage_Build_DoesNotFilterWhenAzureIdCaseChangesDisabled()
    {
        // Arrange
        var stage = new DisplayFilteringStage(ignoreAzureIdCaseChanges: false);
        var changes = new List<ResourceChangeModel>
        {
            CreateResourceModel("r1", "update", attributeChanges: [])
        };

        // Act
        var result = stage.Build(changes);

        // Assert - r1 should NOT be filtered when flag is disabled
        await Assert.That(result.DisplayChanges.Count).IsEqualTo(1);
        await Assert.That(result.DisplayChanges[0].Address).IsEqualTo("r1");
        await Assert.That(result.FilteredResourceCount).IsEqualTo(0);
    }

    /// <summary>
    /// TC-04: Verify that module addresses are normalized (null becomes empty string).
    /// </summary>
    [Test]
    public async Task DisplayFilteringStage_Build_NormalizesModuleAddresses()
    {
        // Arrange
        var stage = new DisplayFilteringStage(ignoreAzureIdCaseChanges: false);
        var changes = new List<ResourceChangeModel>
        {
            CreateResourceModel("r1", "create", moduleAddress: null),
            CreateResourceModel("r2", "create", moduleAddress: "module.child")
        };

        // Act
        var result = stage.Build(changes);

        // Assert
        await Assert.That(result.DisplayChanges.Count).IsEqualTo(2);
        await Assert.That(result.DisplayChanges[0].ModuleAddress).IsEqualTo(string.Empty);
        await Assert.That(result.DisplayChanges[1].ModuleAddress).IsEqualTo("module.child");
    }

    /// <summary>
    /// TC-05: Verify that update resources with import IDs are preserved even when all attributes suppressed.
    /// </summary>
    [Test]
    public async Task DisplayFilteringStage_Build_PreservesUpdatesWithImportIds()
    {
        // Arrange
        var stage = new DisplayFilteringStage(ignoreAzureIdCaseChanges: true);
        var changes = new List<ResourceChangeModel>
        {
            CreateResourceModel("r1", "update", attributeChanges: [], importId: "/subscriptions/abc123/resourceGroups/my-rg")
        };

        // Act
        var result = stage.Build(changes);

        // Assert - r1 should be preserved because it has an ImportId
        await Assert.That(result.DisplayChanges.Count).IsEqualTo(1);
        await Assert.That(result.DisplayChanges[0].Address).IsEqualTo("r1");
        await Assert.That(result.FilteredResourceCount).IsEqualTo(0);
    }

    /// <summary>
    /// TC-06: Verify that update resources with moved-from addresses are preserved.
    /// </summary>
    [Test]
    public async Task DisplayFilteringStage_Build_PreservesUpdatesWithMovedFromAddresses()
    {
        // Arrange
        var stage = new DisplayFilteringStage(ignoreAzureIdCaseChanges: true);
        var changes = new List<ResourceChangeModel>
        {
            CreateResourceModel("r1", "update", attributeChanges: [], movedFromAddress: "old.address")
        };

        // Act
        var result = stage.Build(changes);

        // Assert - r1 should be preserved because it has a MovedFromAddress
        await Assert.That(result.DisplayChanges.Count).IsEqualTo(1);
        await Assert.That(result.DisplayChanges[0].Address).IsEqualTo("r1");
        await Assert.That(result.FilteredResourceCount).IsEqualTo(0);
    }

    /// <summary>
    /// TC-07: Verify that update resources with HasWholeResourceUnknownAfterApply are preserved.
    /// </summary>
    [Test]
    public async Task DisplayFilteringStage_Build_PreservesUpdatesWithUnknownAfterApply()
    {
        // Arrange
        var stage = new DisplayFilteringStage(ignoreAzureIdCaseChanges: true);
        var changes = new List<ResourceChangeModel>
        {
            CreateResourceModel("r1", "update", attributeChanges: [], hasWholeResourceUnknownAfterApply: true)
        };

        // Act
        var result = stage.Build(changes);

        // Assert - r1 should be preserved because it has HasWholeResourceUnknownAfterApply
        await Assert.That(result.DisplayChanges.Count).IsEqualTo(1);
        await Assert.That(result.DisplayChanges[0].Address).IsEqualTo("r1");
        await Assert.That(result.FilteredResourceCount).IsEqualTo(0);
    }
}
