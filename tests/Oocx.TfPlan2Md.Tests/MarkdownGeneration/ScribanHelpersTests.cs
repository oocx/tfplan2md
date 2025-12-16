using System.Globalization;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ScribanHelpersTests
{
    [Fact]
    public void DiffArray_WithAddedItems_ReturnsAddedCollection()
    {
        // Arrange
        var beforeJson = JsonDocument.Parse("""
            [
                {"name": "rule1", "port": 80}
            ]
            """).RootElement;

        var afterJson = JsonDocument.Parse("""
            [
                {"name": "rule1", "port": 80},
                {"name": "rule2", "port": 443}
            ]
            """).RootElement;

        // Act
        var result = ScribanHelpers.DiffArray(beforeJson, afterJson, "name");

        // Assert
        var added = result["added"] as ScriptArray;
        Assert.NotNull(added);
        Assert.Single(added);
        var addedItem = added[0] as ScriptObject;
        Assert.Equal("rule2", addedItem?["name"]);
    }

    [Fact]
    public void DiffArray_WithRemovedItems_ReturnsRemovedCollection()
    {
        // Arrange
        var beforeJson = JsonDocument.Parse("""
            [
                {"name": "rule1", "port": 80},
                {"name": "rule2", "port": 443}
            ]
            """).RootElement;

        var afterJson = JsonDocument.Parse("""
            [
                {"name": "rule1", "port": 80}
            ]
            """).RootElement;

        // Act
        var result = ScribanHelpers.DiffArray(beforeJson, afterJson, "name");

        // Assert
        var removed = result["removed"] as ScriptArray;
        Assert.NotNull(removed);
        Assert.Single(removed);
        var removedItem = removed[0] as ScriptObject;
        Assert.Equal("rule2", removedItem?["name"]);
    }

    [Fact]
    public void DiffArray_WithModifiedItems_ReturnsModifiedCollectionWithBeforeAndAfter()
    {
        // Arrange
        var beforeJson = JsonDocument.Parse("""
            [
                {"name": "rule1", "port": 80}
            ]
            """).RootElement;

        var afterJson = JsonDocument.Parse("""
            [
                {"name": "rule1", "port": 8080}
            ]
            """).RootElement;

        // Act
        var result = ScribanHelpers.DiffArray(beforeJson, afterJson, "name");

        // Assert
        var modified = result["modified"] as ScriptArray;
        Assert.NotNull(modified);
        Assert.Single(modified);

        var modifiedItem = modified[0] as ScriptObject;
        Assert.NotNull(modifiedItem);

        var before = modifiedItem["before"] as ScriptObject;
        var after = modifiedItem["after"] as ScriptObject;

        Assert.Equal(80L, Convert.ToInt64(before?["port"], CultureInfo.InvariantCulture));
        Assert.Equal(8080L, Convert.ToInt64(after?["port"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void DiffArray_WithUnchangedItems_ReturnsUnchangedCollection()
    {
        // Arrange
        var beforeJson = JsonDocument.Parse("""
            [
                {"name": "rule1", "port": 80}
            ]
            """).RootElement;

        var afterJson = JsonDocument.Parse("""
            [
                {"name": "rule1", "port": 80}
            ]
            """).RootElement;

        // Act
        var result = ScribanHelpers.DiffArray(beforeJson, afterJson, "name");

        // Assert
        var unchanged = result["unchanged"] as ScriptArray;
        Assert.NotNull(unchanged);
        Assert.Single(unchanged);
        var unchangedItem = unchanged[0] as ScriptObject;
        Assert.Equal("rule1", unchangedItem?["name"]);

        Assert.Empty((ScriptArray)result["added"]!);
        Assert.Empty((ScriptArray)result["removed"]!);
        Assert.Empty((ScriptArray)result["modified"]!);
    }

    [Fact]
    public void DiffArray_WithMixedChanges_ReturnsAllCategories()
    {
        // Arrange
        var beforeJson = JsonDocument.Parse("""
            [
                {"name": "unchanged", "value": 1},
                {"name": "modified", "value": 2},
                {"name": "removed", "value": 3}
            ]
            """).RootElement;

        var afterJson = JsonDocument.Parse("""
            [
                {"name": "unchanged", "value": 1},
                {"name": "modified", "value": 20},
                {"name": "added", "value": 4}
            ]
            """).RootElement;

        // Act
        var result = ScribanHelpers.DiffArray(beforeJson, afterJson, "name");

        // Assert
        Assert.Single((ScriptArray)result["added"]!);
        Assert.Single((ScriptArray)result["removed"]!);
        Assert.Single((ScriptArray)result["modified"]!);
        Assert.Single((ScriptArray)result["unchanged"]!);
    }

    [Fact]
    public void DiffArray_WithEmptyBeforeArray_ReturnsAllAsAdded()
    {
        // Arrange
        var beforeJson = JsonDocument.Parse("[]").RootElement;
        var afterJson = JsonDocument.Parse("""
            [
                {"name": "rule1", "port": 80}
            ]
            """).RootElement;

        // Act
        var result = ScribanHelpers.DiffArray(beforeJson, afterJson, "name");

        // Assert
        Assert.Single((ScriptArray)result["added"]!);
        Assert.Empty((ScriptArray)result["removed"]!);
        Assert.Empty((ScriptArray)result["modified"]!);
        Assert.Empty((ScriptArray)result["unchanged"]!);
    }

    [Fact]
    public void DiffArray_WithEmptyAfterArray_ReturnsAllAsRemoved()
    {
        // Arrange
        var beforeJson = JsonDocument.Parse("""
            [
                {"name": "rule1", "port": 80}
            ]
            """).RootElement;
        var afterJson = JsonDocument.Parse("[]").RootElement;

        // Act
        var result = ScribanHelpers.DiffArray(beforeJson, afterJson, "name");

        // Assert
        Assert.Empty((ScriptArray)result["added"]!);
        Assert.Single((ScriptArray)result["removed"]!);
        Assert.Empty((ScriptArray)result["modified"]!);
        Assert.Empty((ScriptArray)result["unchanged"]!);
    }

    [Fact]
    public void DiffArray_WithNullBeforeArray_ReturnsAllAsAdded()
    {
        // Arrange
        var afterJson = JsonDocument.Parse("""
            [
                {"name": "rule1", "port": 80}
            ]
            """).RootElement;

        // Act
        var result = ScribanHelpers.DiffArray(null, afterJson, "name");

        // Assert
        Assert.Single((ScriptArray)result["added"]!);
        Assert.Empty((ScriptArray)result["removed"]!);
    }

    [Fact]
    public void DiffArray_WithNullAfterArray_ReturnsAllAsRemoved()
    {
        // Arrange
        var beforeJson = JsonDocument.Parse("""
            [
                {"name": "rule1", "port": 80}
            ]
            """).RootElement;

        // Act
        var result = ScribanHelpers.DiffArray(beforeJson, null, "name");

        // Assert
        Assert.Empty((ScriptArray)result["added"]!);
        Assert.Single((ScriptArray)result["removed"]!);
    }

    [Fact]
    public void DiffArray_WithMissingKeyProperty_ThrowsScribanHelperException()
    {
        // Arrange
        var beforeJson = JsonDocument.Parse("""
            [
                {"name": "rule1", "port": 80}
            ]
            """).RootElement;

        var afterJson = JsonDocument.Parse("""
            [
                {"port": 80}
            ]
            """).RootElement;

        // Act & Assert
        var ex = Assert.Throws<ScribanHelperException>(() =>
            ScribanHelpers.DiffArray(beforeJson, afterJson, "name"));

        Assert.Contains("missing required key property 'name'", ex.Message);
        Assert.Contains("index 0", ex.Message);
        Assert.Contains("'after'", ex.Message);
    }

    [Fact]
    public void DiffArray_WithNestedArrays_ComparesCorrectly()
    {
        // Arrange
        var beforeJson = JsonDocument.Parse("""
            [
                {"name": "rule1", "ports": [80, 443]}
            ]
            """).RootElement;

        var afterJson = JsonDocument.Parse("""
            [
                {"name": "rule1", "ports": [80, 443, 8080]}
            ]
            """).RootElement;

        // Act
        var result = ScribanHelpers.DiffArray(beforeJson, afterJson, "name");

        // Assert
        var modified = result["modified"] as ScriptArray;
        Assert.NotNull(modified);
        Assert.Single(modified);
    }

    [Fact]
    public void DiffArray_WithNestedObjects_ComparesCorrectly()
    {
        // Arrange
        var beforeJson = JsonDocument.Parse("""
            [
                {"name": "rule1", "config": {"enabled": true}}
            ]
            """).RootElement;

        var afterJson = JsonDocument.Parse("""
            [
                {"name": "rule1", "config": {"enabled": false}}
            ]
            """).RootElement;

        // Act
        var result = ScribanHelpers.DiffArray(beforeJson, afterJson, "name");

        // Assert
        var modified = result["modified"] as ScriptArray;
        Assert.NotNull(modified);
        Assert.Single(modified);
    }

    [Fact]
    public void RegisterHelpers_AddsDiffArrayFunction()
    {
        // Arrange
        var scriptObject = new ScriptObject();

        // Act
        ScribanHelpers.RegisterHelpers(scriptObject);

        // Assert
        Assert.True(scriptObject.ContainsKey("diff_array"));
    }
}
