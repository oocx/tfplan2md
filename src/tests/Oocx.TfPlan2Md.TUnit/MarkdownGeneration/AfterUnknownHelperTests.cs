using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Unit tests for <see cref="AfterUnknownHelper"/>.
/// </summary>
/// <remarks>
/// Verifies detection of Terraform <c>after_unknown</c> values for whole resources and
/// flattened attribute keys used by <c>JsonFlattener</c>.
/// Related feature: docs/features/102-known-after-apply-rendering/specification.md.
/// Related test plan: docs/features/102-known-after-apply-rendering/test-plan.md (TC-01 to TC-04).
/// </remarks>
public class AfterUnknownHelperTests
{
    /// <summary>
    /// TC-01: Returns <see langword="true"/> when the whole resource is marked unknown after apply.
    /// </summary>
    [Test]
    public void IsWholeResourceUnknownAfterApply_WhenAfterUnknownIsTrue_ReturnsTrue()
    {
        // Arrange
        var afterUnknown = JsonSerializer.SerializeToElement(true);

        // Act
        var result = AfterUnknownHelper.IsWholeResourceUnknownAfterApply(afterUnknown);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// TC-02: Returns <see langword="false"/> when <c>after_unknown</c> is an object map.
    /// </summary>
    [Test]
    public void IsWholeResourceUnknownAfterApply_WhenAfterUnknownIsObject_ReturnsFalse()
    {
        // Arrange
        var afterUnknown = JsonSerializer.SerializeToElement(new Dictionary<string, object?>
        {
            ["id"] = true,
        });

        // Act
        var result = AfterUnknownHelper.IsWholeResourceUnknownAfterApply(afterUnknown);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// TC-03: Detects unknown attributes for simple, nested, array, and subtree paths.
    /// </summary>
    [Test]
    public void IsAttributeUnknownAfterApply_WhenPathIsMarkedUnknown_ReturnsTrue()
    {
        // Arrange
        var simpleUnknown = JsonSerializer.SerializeToElement(new Dictionary<string, object?>
        {
            ["group_object_id"] = true,
        });

        var nestedUnknown = JsonSerializer.SerializeToElement(new Dictionary<string, object?>
        {
            ["tags"] = new Dictionary<string, object?>
            {
                ["env"] = true,
            },
        });

        var arrayUnknown = JsonSerializer.SerializeToElement(new Dictionary<string, object?>
        {
            ["rules"] = new object?[]
            {
                new Dictionary<string, object?>
                {
                    ["priority"] = true,
                },
            },
        });

        var subtreeUnknown = JsonSerializer.SerializeToElement(new Dictionary<string, object?>
        {
            ["tags"] = true,
        });

        // Act + Assert
        AfterUnknownHelper.IsAttributeUnknownAfterApply(simpleUnknown, "group_object_id").Should().BeTrue();
        AfterUnknownHelper.IsAttributeUnknownAfterApply(nestedUnknown, "tags.env").Should().BeTrue();
        AfterUnknownHelper.IsAttributeUnknownAfterApply(arrayUnknown, "rules[0].priority").Should().BeTrue();
        AfterUnknownHelper.IsAttributeUnknownAfterApply(subtreeUnknown, "tags.env").Should().BeTrue();
    }

    /// <summary>
    /// TC-04: Returns <see langword="false"/> for unmarked, null, and malformed unknown trees.
    /// </summary>
    [Test]
    public void IsAttributeUnknownAfterApply_WhenPathIsNotMarkedUnknown_ReturnsFalse()
    {
        // Arrange
        var afterUnknown = JsonSerializer.SerializeToElement(new Dictionary<string, object?>
        {
            ["id"] = true,
        });

        var malformed = JsonSerializer.SerializeToElement(new Dictionary<string, object?>
        {
            ["rules"] = "not-an-array",
        });

        // Act + Assert
        AfterUnknownHelper.IsAttributeUnknownAfterApply(afterUnknown, "location").Should().BeFalse();
        AfterUnknownHelper.IsAttributeUnknownAfterApply(null, "tags.env").Should().BeFalse();
        AfterUnknownHelper.IsAttributeUnknownAfterApply(malformed, "rules[0].priority").Should().BeFalse();
        AfterUnknownHelper.IsAttributeUnknownAfterApply(afterUnknown, string.Empty).Should().BeFalse();
    }
}
