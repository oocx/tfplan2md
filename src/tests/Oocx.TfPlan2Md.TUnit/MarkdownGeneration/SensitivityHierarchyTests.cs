using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Unit tests for <see cref="SensitivityHelper"/> covering hierarchical sensitivity edge cases.
/// </summary>
/// <remarks>
/// Tests the two confirmed edge-case gaps in sensitivity detection:
/// - Root boolean sensitivity (<c>before_sensitive: true</c> / <c>after_sensitive: true</c>)
///   which flattens to <c>{"": "true"}</c> and was never checked.
/// - Top-level array parent sensitivity for keys without dots (e.g., <c>secrets[0]</c>
///   was not linked to parent <c>secrets: true</c>).
/// Related issue: docs/issues/098-sensitive-info-exposure/analysis.md.
/// Test plan coverage: TC-13 through TC-19.
/// </remarks>
public class SensitivityHierarchyTests
{
    #region TC-13: Root boolean after_sensitive masks any attribute

    /// <summary>
    /// TC-13: When <c>after_sensitive</c> is a root boolean <c>true</c> (flattened as <c>{"": "true"}</c>),
    /// any attribute key should be considered sensitive.
    /// </summary>
    [Test]
    public void IsSensitiveAttribute_RootBooleanAfterSensitive_ReturnsTrue()
    {
        // Arrange
        Dictionary<string, string?> beforeSensitive = [];
        Dictionary<string, string?> afterSensitive = new() { [""] = "true" };

        // Act
        var result = SensitivityHelper.IsSensitiveAttribute("api_key", beforeSensitive, afterSensitive);

        // Assert
        result.Should().BeTrue("root boolean after_sensitive: true should mask all attributes");
    }

    #endregion

    #region TC-14: Root boolean before_sensitive masks any attribute

    /// <summary>
    /// TC-14: When <c>before_sensitive</c> is a root boolean <c>true</c> (flattened as <c>{"": "true"}</c>),
    /// any attribute key should be considered sensitive.
    /// </summary>
    [Test]
    public void IsSensitiveAttribute_RootBooleanBeforeSensitive_ReturnsTrue()
    {
        // Arrange
        Dictionary<string, string?> beforeSensitive = new() { [""] = "true" };
        Dictionary<string, string?> afterSensitive = [];

        // Act
        var result = SensitivityHelper.IsSensitiveAttribute("api_key", beforeSensitive, afterSensitive);

        // Assert
        result.Should().BeTrue("root boolean before_sensitive: true should mask all attributes");
    }

    #endregion

    #region TC-15: Top-level array parent sensitivity masks secrets[0]

    /// <summary>
    /// TC-15: When the parent array <c>secrets</c> is marked sensitive, <c>secrets[0]</c> must be masked.
    /// </summary>
    [Test]
    public void IsSensitiveAttribute_TopLevelArrayParent_MasksFirstElement()
    {
        // Arrange
        Dictionary<string, string?> beforeSensitive = [];
        Dictionary<string, string?> afterSensitive = new() { ["secrets"] = "true" };

        // Act
        var result = SensitivityHelper.IsSensitiveAttribute("secrets[0]", beforeSensitive, afterSensitive);

        // Assert
        result.Should().BeTrue("parent array 'secrets' is sensitive, so 'secrets[0]' must be masked");
    }

    #endregion

    #region TC-16: Top-level array parent sensitivity masks secrets[1]

    /// <summary>
    /// TC-16: When the parent array <c>secrets</c> is marked sensitive, <c>secrets[1]</c> must also be masked.
    /// </summary>
    [Test]
    public void IsSensitiveAttribute_TopLevelArrayParent_MasksSecondElement()
    {
        // Arrange
        Dictionary<string, string?> beforeSensitive = [];
        Dictionary<string, string?> afterSensitive = new() { ["secrets"] = "true" };

        // Act
        var result = SensitivityHelper.IsSensitiveAttribute("secrets[1]", beforeSensitive, afterSensitive);

        // Assert
        result.Should().BeTrue("parent array 'secrets' is sensitive, so 'secrets[1]' must be masked");
    }

    #endregion

    #region TC-17: GetHierarchicalPaths emits base name for key[n] without dot

    /// <summary>
    /// TC-17: <c>GetHierarchicalPaths("secrets[0]")</c> must include <c>"secrets"</c> (the array base name).
    /// </summary>
    [Test]
    public void GetHierarchicalPaths_TopLevelArrayKey_IncludesBaseName()
    {
        // Act
        var paths = SensitivityHelper.GetHierarchicalPaths("secrets[0]").ToList();

        // Assert
        paths.Should().Contain("secrets",
            "GetHierarchicalPaths must emit the array base name for top-level keys like 'secrets[0]'");
    }

    #endregion

    #region TC-18: GetHierarchicalPaths regression guard for dotted+indexed paths

    /// <summary>
    /// TC-18: Regression guard — <c>GetHierarchicalPaths("a[0].b[1]")</c> must include all expected
    /// hierarchical segments: <c>"a[0].b[1]"</c>, <c>"a[0].b"</c>, <c>"a[0]"</c>, <c>"a"</c>.
    /// </summary>
    [Test]
    public void GetHierarchicalPaths_DottedIndexedPath_IncludesAllSegments()
    {
        // Act
        var paths = SensitivityHelper.GetHierarchicalPaths("a[0].b[1]").ToList();

        // Assert
        paths.Should().Contain("a[0].b[1]", "must include the full key");
        paths.Should().Contain("a[0].b", "must include parent without trailing index");
        paths.Should().Contain("a[0]", "must include first-level indexed segment");
        paths.Should().Contain("a", "must include the root array name");
    }

    /// <summary>
    /// Verifies that <c>GetHierarchicalPaths</c> does not emit duplicate paths for multi-level indexed keys.
    /// </summary>
    /// <remarks>
    /// Related issue: docs/issues/098-sensitive-info-exposure/code-review.md (Minor M-1).
    /// For <c>"properties.accessPolicies[0].permissions.keys[0]"</c>, the method previously yielded
    /// <c>"properties"</c> multiple times due to incorrect stripping of array indices from middle segments.
    /// </remarks>
    [Test]
    public void GetHierarchicalPaths_MultiLevelIndexedKey_NoDuplicates()
    {
        // Act
        var paths = SensitivityHelper.GetHierarchicalPaths("properties.accessPolicies[0].permissions.keys[0]").ToList();

        // Assert — no duplicates
        paths.Should().OnlyHaveUniqueItems("GetHierarchicalPaths must not emit duplicate paths");

        // Assert — all expected paths present
        paths.Should().Contain("properties.accessPolicies[0].permissions.keys[0]", "full key");
        paths.Should().Contain("properties.accessPolicies[0].permissions.keys", "stripped last index");
        paths.Should().Contain("properties.accessPolicies[0].permissions", "parent of last segment");
        paths.Should().Contain("properties.accessPolicies[0]", "second-level indexed segment");
        paths.Should().Contain("properties.accessPolicies", "stripped second-level index");
        paths.Should().Contain("properties", "root segment");
    }

    #endregion

    #region TC-19: Root boolean sensitivity masks any key pattern

    /// <summary>
    /// TC-19: Root boolean sensitivity (<c>{"": "true"}</c>) must mask arbitrary key patterns:
    /// simple keys, nested dotted keys, and array-indexed keys.
    /// </summary>
    [Test]
    [Arguments("anything")]
    [Arguments("nested.deep.key")]
    [Arguments("arr[0]")]
    public void IsSensitiveAttribute_RootBooleanSensitive_MasksAnyKeyPattern(string key)
    {
        // Arrange
        Dictionary<string, string?> beforeSensitive = [];
        Dictionary<string, string?> afterSensitive = new() { [""] = "true" };

        // Act
        var result = SensitivityHelper.IsSensitiveAttribute(key, beforeSensitive, afterSensitive);

        // Assert
        result.Should().BeTrue(
            $"root boolean after_sensitive should mask key '{key}' regardless of pattern");
    }

    #endregion
}
