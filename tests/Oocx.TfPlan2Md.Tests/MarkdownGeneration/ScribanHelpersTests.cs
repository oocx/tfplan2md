using System.Globalization;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Azure;
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
        added.Should().NotBeNull();
        added.Should().ContainSingle();
        var addedItem = added[0] as ScriptObject;
        addedItem?["name"].Should().Be("rule2");
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
        removed.Should().NotBeNull();
        removed.Should().ContainSingle();
        var removedItem = removed[0] as ScriptObject;
        removedItem?["name"].Should().Be("rule2");
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
        modified.Should().NotBeNull();
        modified.Should().ContainSingle();

        var modifiedItem = modified[0] as ScriptObject;
        modifiedItem.Should().NotBeNull();

        var before = modifiedItem["before"] as ScriptObject;
        var after = modifiedItem["after"] as ScriptObject;

        Convert.ToInt64(before?["port"], CultureInfo.InvariantCulture).Should().Be(80L);
        Convert.ToInt64(after?["port"], CultureInfo.InvariantCulture).Should().Be(8080L);
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
        unchanged.Should().NotBeNull();
        unchanged.Should().ContainSingle();
        var unchangedItem = unchanged[0] as ScriptObject;
        unchangedItem?["name"].Should().Be("rule1");

        ((ScriptArray)result["added"]!).Should().BeEmpty();
        ((ScriptArray)result["removed"]!).Should().BeEmpty();
        ((ScriptArray)result["modified"]!).Should().BeEmpty();
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
        ((ScriptArray)result["added"]!).Should().ContainSingle();
        ((ScriptArray)result["removed"]!).Should().ContainSingle();
        ((ScriptArray)result["modified"]!).Should().ContainSingle();
        ((ScriptArray)result["unchanged"]!).Should().ContainSingle();
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
        ((ScriptArray)result["added"]!).Should().ContainSingle();
        ((ScriptArray)result["removed"]!).Should().BeEmpty();
        ((ScriptArray)result["modified"]!).Should().BeEmpty();
        ((ScriptArray)result["unchanged"]!).Should().BeEmpty();
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
        ((ScriptArray)result["added"]!).Should().BeEmpty();
        ((ScriptArray)result["removed"]!).Should().ContainSingle();
        ((ScriptArray)result["modified"]!).Should().BeEmpty();
        ((ScriptArray)result["unchanged"]!).Should().BeEmpty();
    }

    [Fact]
    public void FormatValue_AzureResourceId_FormatsWithScopeParser()
    {
        const string providerName = "registry.terraform.io/hashicorp/azurerm";
        const string value = "/subscriptions/sub-id/resourceGroups/my-rg/providers/Microsoft.KeyVault/vaults/my-kv";

        var result = ScribanHelpers.FormatValue(value, providerName);

        result.Should().Be("Key Vault **my-kv** in resource group **my-rg** of subscription **sub-id**");
    }

    [Fact]
    public void FormatValue_NonAzureId_ReturnsBacktickedValue()
    {
        const string providerName = "registry.terraform.io/hashicorp/azurerm";
        const string value = "standard-value";

        var result = ScribanHelpers.FormatValue(value, providerName);

        result.Should().Be("`standard-value`");
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
        ((ScriptArray)result["added"]!).Should().ContainSingle();
        ((ScriptArray)result["removed"]!).Should().BeEmpty();
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
        ((ScriptArray)result["added"]!).Should().BeEmpty();
        ((ScriptArray)result["removed"]!).Should().ContainSingle();
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
        Action act = () => ScribanHelpers.DiffArray(beforeJson, afterJson, "name");
        act.Should().Throw<ScribanHelperException>()
            .Which.Message.Should().Contain("missing required key property 'name'").And.Contain("index 0").And.Contain("'after'");
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
        modified.Should().NotBeNull();
        modified.Should().ContainSingle();
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
        modified.Should().NotBeNull();
        modified.Should().ContainSingle();
    }

    [Fact]
    public void RegisterHelpers_AddsDiffArrayFunction()
    {
        // Arrange
        var scriptObject = new ScriptObject();

        // Act
        ScribanHelpers.RegisterHelpers(scriptObject, new NullMapper(), LargeValueFormat.InlineDiff);

        // Assert
        scriptObject.ContainsKey("diff_array").Should().BeTrue();
    }

    private sealed class NullMapper : IPrincipalMapper
    {
        public string GetPrincipalName(string principalId)
        {
            return principalId;
        }

        public string? GetName(string principalId)
        {
            return null;
        }
    }
}
