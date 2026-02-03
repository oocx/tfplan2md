using System;
using System.Linq;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Providers.AzureDevOps.Models;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for <see cref="VariableGroupViewModelFactory"/>.
/// Verifies factory correctly builds ViewModels from Terraform plan data,
/// including semantic diffing, secret masking, and large value detection.
/// Related feature: docs/features/039-azdo-variable-group-template/specification.md.
/// </summary>
public class VariableGroupViewModelFactoryTests
{
    private const string ProviderName = "azuredevops";
    private const LargeValueFormat DefaultFormat = LargeValueFormat.InlineDiff;

    #region TC-01: Create Operation - Regular Variables

    /// <summary>
    /// TC-01: Verifies that regular variables are correctly formatted for create operations.
    /// </summary>
    [Test]
    public void Build_CreateWithRegularVariables_FormatsCorrectly()
    {
        // Arrange
        var changeJson = CreateResourceChange("create", null, new
        {
            name = "test-group",
            description = "Test variable group",
            variable = new object[]
            {
                new { name = "APP_VERSION", value = "1.0.0", enabled = false, content_type = "", expires = "" },
                new { name = "ENVIRONMENT", value = "production", enabled = true, content_type = "text/plain", expires = "" },
                new { name = "TIMEOUT", value = "30", enabled = (bool?)null, content_type = (string?)null, expires = "2024-12-31" }
            },
            secret_variable = Array.Empty<object>()
        });

        // Act
        var viewModel = VariableGroupViewModelFactory.Build(changeJson, ProviderName, DefaultFormat);

        // Assert
        viewModel.Name.Should().Be("test-group");
        viewModel.Description.Should().Be("Test variable group");
        viewModel.AfterVariables.Should().HaveCount(3);
        viewModel.VariableChanges.Should().BeEmpty();
        viewModel.BeforeVariables.Should().BeEmpty();

        var var1 = viewModel.AfterVariables[0];
        var1.Name.Should().Be("`APP_VERSION`");
        var1.Value.Should().Be("`1.0.0`");
        var1.Enabled.Should().Be("`false`");
        var1.ContentType.Should().Be("-");
        var1.Expires.Should().Be("-");
        var1.IsLargeValue.Should().BeFalse();

        var var2 = viewModel.AfterVariables[1];
        var2.Name.Should().Be("`ENVIRONMENT`");
        var2.Value.Should().Be("`production`");
        var2.Enabled.Should().Be("`true`");
        var2.ContentType.Should().Be("`text/plain`");
        var2.Expires.Should().Be("-");

        var var3 = viewModel.AfterVariables[2];
        var3.Name.Should().Be("`TIMEOUT`");
        var3.Value.Should().Be("`30`");
        var3.Enabled.Should().Be("-");
        var3.ContentType.Should().Be("-");
        var3.Expires.Should().Be("`2024-12-31`");
    }

    #endregion

    #region TC-02: Create Operation - Secret Variables

    /// <summary>
    /// TC-02: Verifies that secret variables mask values while preserving metadata.
    /// </summary>
    [Test]
    public void Build_CreateWithSecretVariables_MasksValues()
    {
        // Arrange
        var changeJson = CreateResourceChange("create", null, new
        {
            name = "test-group",
            variable = Array.Empty<object>(),
            secret_variable = new object[]
            {
                new { name = "API_KEY", value = "super-secret-key", enabled = true, content_type = "", expires = "" },
                new { name = "DB_PASSWORD", value = "p@ssw0rd123", enabled = (bool?)null, content_type = "password", expires = "" }
            }
        });

        // Act
        var viewModel = VariableGroupViewModelFactory.Build(changeJson, ProviderName, DefaultFormat);

        // Assert
        viewModel.AfterVariables.Should().HaveCount(2);

        var secret1 = viewModel.AfterVariables[0];
        secret1.Name.Should().Be("`API_KEY`");
        secret1.Value.Should().Be("`(sensitive / hidden)`");
        secret1.Enabled.Should().Be("`true`");
        secret1.ContentType.Should().Be("-");
        secret1.Expires.Should().Be("-");
        secret1.IsLargeValue.Should().BeFalse();

        var secret2 = viewModel.AfterVariables[1];
        secret2.Name.Should().Be("`DB_PASSWORD`");
        secret2.Value.Should().Be("`(sensitive / hidden)`");
        secret2.Enabled.Should().Be("-");
        secret2.ContentType.Should().Be("`password`");
    }

    #endregion

    #region TC-03: Create Operation - Mixed Variables

    /// <summary>
    /// TC-03: Verifies that regular and secret variables are merged into unified collection.
    /// </summary>
    [Test]
    public void Build_CreateWithMixedVariables_MergesCorrectly()
    {
        // Arrange
        var changeJson = CreateResourceChange("create", null, new
        {
            name = "test-group",
            variable = new object[]
            {
                new { name = "ENV", value = "prod", enabled = (bool?)null, content_type = "", expires = "" },
                new { name = "REGION", value = "eastus", enabled = (bool?)null, content_type = "", expires = "" }
            },
            secret_variable = new object[]
            {
                new { name = "API_KEY", value = "secret1", enabled = (bool?)null, content_type = "", expires = "" },
                new { name = "TOKEN", value = "secret2", enabled = (bool?)null, content_type = "", expires = "" }
            }
        });

        // Act
        var viewModel = VariableGroupViewModelFactory.Build(changeJson, ProviderName, DefaultFormat);

        // Assert
        viewModel.AfterVariables.Should().HaveCount(4);

        // Regular variables come first
        viewModel.AfterVariables[0].Name.Should().Be("`API_KEY`"); // Alphabetically sorted
        viewModel.AfterVariables[0].Value.Should().Be("`(sensitive / hidden)`");

        viewModel.AfterVariables[1].Name.Should().Be("`ENV`");
        viewModel.AfterVariables[1].Value.Should().Be("`prod`");

        viewModel.AfterVariables[2].Name.Should().Be("`REGION`");
        viewModel.AfterVariables[2].Value.Should().Be("`eastus`");

        viewModel.AfterVariables[3].Name.Should().Be("`TOKEN`");
        viewModel.AfterVariables[3].Value.Should().Be("`(sensitive / hidden)`");
    }

    #endregion

    #region TC-06: Update Operation - Added Variables

    /// <summary>
    /// TC-06: Verifies that variables present only in after state are categorized as Added.
    /// </summary>
    [Test]
    public void Build_UpdateWithAddedVariable_ShowsAddedIndicator()
    {
        // Arrange
        var changeJson = CreateResourceChange("update", new
        {
            name = "test-group",
            variable = new object[]
            {
                new { name = "VAR_A", value = "value-a", enabled = (bool?)null, content_type = "", expires = "" }
            },
            secret_variable = Array.Empty<object>()
        }, new
        {
            name = "test-group",
            variable = new object[]
            {
                new { name = "VAR_A", value = "value-a", enabled = (bool?)null, content_type = "", expires = "" },
                new { name = "VAR_B", value = "value-b", enabled = true, content_type = "", expires = "" }
            },
            secret_variable = Array.Empty<object>()
        });

        // Act
        var viewModel = VariableGroupViewModelFactory.Build(changeJson, ProviderName, DefaultFormat);

        // Assert
        viewModel.VariableChanges.Should().HaveCount(2);

        var addedVar = viewModel.VariableChanges.FirstOrDefault(v => v.Name == "`VAR_B`");
        addedVar.Should().NotBeNull();
        addedVar!.Change.Should().Be("➕");
        addedVar.Value.Should().Be("`value-b`");
        addedVar.Enabled.Should().Be("`true`");
    }

    #endregion

    #region TC-07: Update Operation - Null Attribute Transition

    /// <summary>
    /// TC-07: Verifies that when an attribute changes from a value to null, the diff shows a proper placeholder.
    /// This tests the critical bug found in UAT where enabled: false → null showed empty "+ " text.
    /// </summary>
    [Test]
    public void Build_UpdateWithNullTransition_ShowsPlaceholderForNullAfter()
    {
        // Arrange
        var changeJson = CreateResourceChange("update", new
        {
            name = "test-group",
            variable = new object[]
            {
                new { name = "APP_VERSION", value = "1.0.0", enabled = false, content_type = "text/plain", expires = "" }
            },
            secret_variable = new object[]
            {
                new { name = "SECRET_KEY", value = "supersecret", enabled = false, content_type = "", expires = "" }
            }
        }, new
        {
            name = "test-group",
            variable = new object[]
            {
                new { name = "APP_VERSION", value = "1.0.0", enabled = (bool?)null, content_type = (string?)null, expires = "" }
            },
            secret_variable = new object[]
            {
                new { name = "SECRET_KEY", value = "supersecret", enabled = (bool?)null, content_type = "", expires = "" }
            }
        });

        // Act
        var viewModel = VariableGroupViewModelFactory.Build(changeJson, ProviderName, DefaultFormat);

        // Assert
        viewModel.VariableChanges.Should().HaveCount(2);

        // Find APP_VERSION (regular variable)
        var appVersion = viewModel.VariableChanges.FirstOrDefault(v => v.Name == "`APP_VERSION`");
        appVersion.Should().NotBeNull();
        appVersion!.Change.Should().Be("🔄");

        // Value is unchanged - shows as single value
        appVersion.Value.Should().Contain("1.0.0");

        // Critical: Enabled should show diff with placeholder for null
        // Should NOT be empty after the "+" sign
        appVersion.Enabled.Should().NotBeEmpty();
        appVersion.Enabled.Should().Contain("false"); // Contains before value
        appVersion.Enabled.Should().Contain("+"); // Contains diff indicator
        // After the "+", should show "-" placeholder for null
        appVersion.Enabled.Should().Contain("+ ");
        appVersion.Enabled.Should().Contain("-"); // Placeholder for null in after state

        // ContentType should also show null transition
        appVersion.ContentType.Should().NotBeEmpty();
        appVersion.ContentType.Should().Contain("text/plain");
        appVersion.ContentType.Should().Contain("+");
        appVersion.ContentType.Should().Contain("-"); // Placeholder for null

        // Find SECRET_KEY (secret variable)
        var secretKey = viewModel.VariableChanges.FirstOrDefault(v => v.Name == "`SECRET_KEY`");
        secretKey.Should().NotBeNull();
        secretKey!.Change.Should().Be("🔄");
        secretKey.Value.Should().Be("`(sensitive / hidden)`"); // Secrets always masked

        // Enabled should show diff with placeholder for null
        secretKey.Enabled.Should().NotBeEmpty();
        secretKey.Enabled.Should().Contain("false");
        secretKey.Enabled.Should().Contain("+");
        secretKey.Enabled.Should().Contain("-"); // Placeholder for null
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a ResourceChange object with the specified action and states.
    /// </summary>
    private static ResourceChange CreateResourceChange(string action, object? before, object? after)
    {
        var beforeJson = before != null ? JsonSerializer.SerializeToElement(before) : (JsonElement?)null;
        var afterJson = after != null ? JsonSerializer.SerializeToElement(after) : (JsonElement?)null;

        var actions = action == "update" ? new[] { "update" } : new[] { action };

        var change = new Change(
            actions: actions,
            before: beforeJson,
            after: afterJson,
            afterUnknown: null,
            beforeSensitive: null,
            afterSensitive: null
        );

        return new ResourceChange(
            Address: "azuredevops_variable_group.test",
            ModuleAddress: null,
            Mode: "managed",
            Type: "azuredevops_variable_group",
            Name: "test",
            ProviderName: "azuredevops",
            Change: change
        );
    }

    #endregion
}
