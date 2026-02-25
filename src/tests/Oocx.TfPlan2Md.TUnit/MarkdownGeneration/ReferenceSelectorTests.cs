using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Unit tests for <see cref="ReferenceSelector"/>.
/// </summary>
/// <remarks>
/// Related feature: docs/features/102-known-after-apply-rendering/specification.md.
/// Related test plan: docs/features/102-known-after-apply-rendering/test-plan.md (TC-05 to TC-11).
/// </remarks>
public class ReferenceSelectorTests
{
    private const string EachValueGroupObjectId = "each.value.group_object_id";
    private const string EachValue = "each.value";

    /// <summary>
    /// TC-05: Selects static resource reference with highest priority.
    /// </summary>
    [Test]
    public void SelectBestReference_StaticResourceReference_ReturnsTypeDotName()
    {
        // Arrange
        IReadOnlyList<string> references =
        [
            "azuread_group.platform_engineers.object_id",
            "azuread_group.platform_engineers",
        ];

        // Act
        var selected = ReferenceSelector.SelectBestReference(references);

        // Assert
        selected.Should().Be("azuread_group.platform_engineers");
    }

    /// <summary>
    /// TC-06: Selects each.value attribute reference when no static resource reference exists.
    /// </summary>
    [Test]
    public void SelectBestReference_EachValueAttributeRef_WhenNoStaticRef()
    {
        // Arrange
        IReadOnlyList<string> references = [EachValueGroupObjectId, EachValue];

        // Act
        var selected = ReferenceSelector.SelectBestReference(references);

        // Assert
        selected.Should().Be(EachValueGroupObjectId);
    }

    /// <summary>
    /// TC-07: Selects variable reference when no higher-priority references exist.
    /// </summary>
    [Test]
    public void SelectBestReference_VarReference_WhenNoStaticOrEachRef()
    {
        // Arrange
        IReadOnlyList<string> references = ["count.index", "var.users"];

        // Act
        var selected = ReferenceSelector.SelectBestReference(references);

        // Assert
        selected.Should().Be("var.users");
    }

    /// <summary>
    /// TC-08: Selects local reference when no higher-priority references exist.
    /// </summary>
    [Test]
    public void SelectBestReference_LocalReference_WhenNoHigherPriority()
    {
        // Arrange
        IReadOnlyList<string> references = ["local.tenant_prefix"];

        // Act
        var selected = ReferenceSelector.SelectBestReference(references);

        // Assert
        selected.Should().Be("local.tenant_prefix");
    }

    /// <summary>
    /// TC-09: Returns null when only useless meta-references are present.
    /// </summary>
    [Test]
    public void SelectBestReference_UselessMetaReferences_ReturnsNull()
    {
        // Arrange
        IReadOnlyList<string> references = ["each.key", EachValue, "count.index", "self"];

        // Act
        var selected = ReferenceSelector.SelectBestReference(references);

        // Assert
        selected.Should().BeNull();
    }

    /// <summary>
    /// TC-10: Strips attribute suffix from static references.
    /// </summary>
    [Test]
    public void SelectBestReference_ThreePartStaticRef_StripsAttributeSuffix()
    {
        // Arrange
        IReadOnlyList<string> references = ["azuread_group.admins.object_id"];

        // Act
        var selected = ReferenceSelector.SelectBestReference(references);

        // Assert
        selected.Should().Be("azuread_group.admins");
    }

    /// <summary>
    /// TC-11: SelectResourceLevelReference returns only static resource references.
    /// </summary>
    [Test]
    public void SelectResourceLevelReference_OnlyStaticRefs_NoEachOrVar()
    {
        // Act + Assert
        ReferenceSelector.SelectResourceLevelReference(["azuread_group.admins.object_id", "azuread_group.admins"])
            .Should().Be("azuread_group.admins");

        ReferenceSelector.SelectResourceLevelReference([EachValueGroupObjectId, EachValue])
            .Should().BeNull();

        ReferenceSelector.SelectResourceLevelReference(["var.tenant_id"])
            .Should().BeNull();

        ReferenceSelector.SelectResourceLevelReference(["module.identity.azuread_user.admin.object_id", "module.identity.azuread_user.admin"])
            .Should().Be("module.identity.azuread_user.admin");
    }
}
