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
/// Tests for <see cref="BuildDefinitionViewModelFactory"/>.
/// Verifies factory correctly builds ViewModels from Terraform plan data,
/// including semantic diffing, secret masking, and large value detection.
/// Related feature: docs/features/094-build-definition-tables/specification.md.
/// </summary>
public class BuildDefinitionViewModelFactoryTests
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
            name = "test-pipeline",
            path = "\\MyPipeline",
            agent_pool_name = "Default",
            variable = new object[]
            {
                new { name = "BUILD_CONFIGURATION", value = "Release", is_secret = false, allow_override = true, secret_value = "" },
                new { name = "BUILD_PLATFORM", value = "Any CPU", is_secret = false, allow_override = false, secret_value = "" },
                new { name = "TIMEOUT", value = "30", is_secret = false, allow_override = (bool?)null, secret_value = "" }
            }
        });

        // Act
        var viewModel = BuildDefinitionViewModelFactory.Build(changeJson, ProviderName, DefaultFormat);

        // Assert
        viewModel.Name.Should().Be("test-pipeline");
        viewModel.Path.Should().Be("\\MyPipeline");
        viewModel.AgentPoolName.Should().Be("Default");
        viewModel.AfterVariables.Should().HaveCount(3);
        viewModel.VariableChanges.Should().BeEmpty();
        viewModel.BeforeVariables.Should().BeEmpty();

        var var1 = viewModel.AfterVariables[0];
        var1.Name.Should().Be("`BUILD_CONFIGURATION`");
        var1.Value.Should().Be("`Release`");
        var1.IsSecret.Should().Be("`false`");
        var1.AllowOverride.Should().Be("`true`");
        var1.IsLargeValue.Should().BeFalse();

        var var2 = viewModel.AfterVariables[1];
        var2.Name.Should().Be("`BUILD_PLATFORM`");
        var2.Value.Should().Be("`Any CPU`");
        var2.IsSecret.Should().Be("`false`");
        var2.AllowOverride.Should().Be("`false`");

        var var3 = viewModel.AfterVariables[2];
        var3.Name.Should().Be("`TIMEOUT`");
        var3.Value.Should().Be("`30`");
        var3.IsSecret.Should().Be("`false`");
        var3.AllowOverride.Should().Be("-");
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
            name = "test-pipeline",
            variable = new object[]
            {
                new { name = "API_KEY", value = "", secret_value = "super-secret-123", is_secret = true, allow_override = true },
                new { name = "DB_PASSWORD", value = "", secret_value = "p@ssw0rd", is_secret = true, allow_override = false }
            }
        });

        // Act
        var viewModel = BuildDefinitionViewModelFactory.Build(changeJson, ProviderName, DefaultFormat);

        // Assert
        viewModel.AfterVariables.Should().HaveCount(2);

        var secret1 = viewModel.AfterVariables[0];
        secret1.Name.Should().Be("`API_KEY`");
        secret1.Value.Should().Be("`(sensitive / hidden)`");
        secret1.IsSecret.Should().Be("`true`");
        secret1.AllowOverride.Should().Be("`true`");
        secret1.IsLargeValue.Should().BeFalse();

        var secret2 = viewModel.AfterVariables[1];
        secret2.Name.Should().Be("`DB_PASSWORD`");
        secret2.Value.Should().Be("`(sensitive / hidden)`");
        secret2.IsSecret.Should().Be("`true`");
        secret2.AllowOverride.Should().Be("`false`");
    }

    #endregion

    #region TC-03: Delete Operation

    /// <summary>
    /// TC-03: Verifies delete operation populates BeforeVariables.
    /// </summary>
    [Test]
    public void Build_Delete_PopulatesBeforeVariables()
    {
        // Arrange
        var changeJson = CreateResourceChange("delete", new
        {
            name = "test-pipeline",
            variable = new object[]
            {
                new { name = "ENV", value = "prod", is_secret = false, allow_override = true, secret_value = "" },
                new { name = "REGION", value = "eastus", is_secret = false, allow_override = false, secret_value = "" }
            }
        }, null);

        // Act
        var viewModel = BuildDefinitionViewModelFactory.Build(changeJson, ProviderName, DefaultFormat);

        // Assert
        viewModel.BeforeVariables.Should().HaveCount(2);
        viewModel.AfterVariables.Should().BeEmpty();
        viewModel.VariableChanges.Should().BeEmpty();

        var var1 = viewModel.BeforeVariables[0];
        var1.Name.Should().Be("`ENV`");
        var1.Value.Should().Be("`prod`");
        var1.IsSecret.Should().Be("`false`");
        var1.AllowOverride.Should().Be("`true`");

        var var2 = viewModel.BeforeVariables[1];
        var2.Name.Should().Be("`REGION`");
        var2.Value.Should().Be("`eastus`");
    }

    #endregion

    #region TC-04: Secret Variable in Delete - Values Masked

    /// <summary>
    /// TC-04: Verifies secret variables remain masked in delete operations.
    /// </summary>
    [Test]
    public void Build_DeleteWithSecretVariables_MasksValues()
    {
        // Arrange
        var changeJson = CreateResourceChange("delete", new
        {
            name = "test-pipeline",
            variable = new object[]
            {
                new { name = "SECRET_TOKEN", value = "", secret_value = "secret123", is_secret = true, allow_override = true }
            }
        }, null);

        // Act
        var viewModel = BuildDefinitionViewModelFactory.Build(changeJson, ProviderName, DefaultFormat);

        // Assert
        viewModel.BeforeVariables.Should().HaveCount(1);
        var secret = viewModel.BeforeVariables[0];
        secret.Name.Should().Be("`SECRET_TOKEN`");
        secret.Value.Should().Be("`(sensitive / hidden)`");
        secret.IsSecret.Should().Be("`true`");
    }

    #endregion

    #region TC-05: Update - Variable Changes

    /// <summary>
    /// TC-05: Verifies update operation uses semantic diffing for variables.
    /// </summary>
    [Test]
    public void Build_UpdateWithVariableChanges_UsesSemanticDiffing()
    {
        // Arrange
        var changeJson = CreateResourceChange("update", new
        {
            name = "test-pipeline",
            variable = new object[]
            {
                new { name = "ENV", value = "dev", is_secret = false, allow_override = true, secret_value = "" }
            }
        }, new
        {
            name = "test-pipeline",
            variable = new object[]
            {
                new { name = "ENV", value = "prod", is_secret = false, allow_override = true, secret_value = "" }
            }
        });

        // Act
        var viewModel = BuildDefinitionViewModelFactory.Build(changeJson, ProviderName, DefaultFormat);

        // Assert
        viewModel.VariableChanges.Should().HaveCount(1);
        viewModel.AfterVariables.Should().BeEmpty();
        viewModel.BeforeVariables.Should().BeEmpty();

        var change = viewModel.VariableChanges[0];
        change.Name.Should().Be("`ENV`");
        change.Change.Should().Be("update");
        change.ChangeIcon.Should().Be("🔄");
        // Value diff is rendered as HTML for inline-diff format
        // Don't check for exact strings due to HTML highlighting - just check it has diff markers
        change.Value.Should().Contain("d"); // Contains parts of both "dev" and "prod"
        change.Value.Should().Contain("-"); // Contains diff markers
        change.Value.Should().Contain("+");
    }

    #endregion

    #region TC-06: Update - Added Variables

    /// <summary>
    /// TC-06: Verifies added variables are correctly categorized.
    /// </summary>
    [Test]
    public void Build_UpdateWithAddedVariables_CategorizesAsAdded()
    {
        // Arrange
        var changeJson = CreateResourceChange("update", new
        {
            name = "test-pipeline",
            variable = new object[]
            {
                new { name = "OLD_VAR", value = "old", is_secret = false, allow_override = true, secret_value = "" }
            }
        }, new
        {
            name = "test-pipeline",
            variable = new object[]
            {
                new { name = "OLD_VAR", value = "old", is_secret = false, allow_override = true, secret_value = "" },
                new { name = "NEW_VAR", value = "new", is_secret = false, allow_override = false, secret_value = "" }
            }
        });

        // Act
        var viewModel = BuildDefinitionViewModelFactory.Build(changeJson, ProviderName, DefaultFormat);

        // Assert
        viewModel.VariableChanges.Should().HaveCount(2);

        var added = viewModel.VariableChanges.FirstOrDefault(v => v.Name == "`NEW_VAR`");
        added.Should().NotBeNull();
        added!.Change.Should().Be("add");
        added.ChangeIcon.Should().Be("➕");
        added.Value.Should().Be("`new`");
    }

    #endregion

    #region TC-07: Update - Modified Variables with Before/After Diffs

    /// <summary>
    /// TC-07: Verifies modified variables show before/after diffs with prefixes.
    /// </summary>
    [Test]
    public void Build_UpdateWithModifiedVariables_ShowsBeforeAfterDiffs()
    {
        // Arrange
        var changeJson = CreateResourceChange("update", new
        {
            name = "test-pipeline",
            variable = new object[]
            {
                new { name = "CONFIG", value = "debug", is_secret = false, allow_override = true, secret_value = "" }
            }
        }, new
        {
            name = "test-pipeline",
            variable = new object[]
            {
                new { name = "CONFIG", value = "release", is_secret = false, allow_override = false, secret_value = "" }
            }
        });

        // Act
        var viewModel = BuildDefinitionViewModelFactory.Build(changeJson, ProviderName, DefaultFormat);

        // Assert
        var modified = viewModel.VariableChanges[0];
        modified.Change.Should().Be("update");
        modified.ChangeIcon.Should().Be("🔄");

        // Value changed - both values are non-secret so should show diff
        // Don't check for exact strings due to HTML highlighting - just check key parts
        modified.Value.Should().Contain("e"); // Common letter in both debug/release
        modified.Value.Should().Contain("-"); // Diff marker
        modified.Value.Should().Contain("+");

        // IsSecret unchanged
        modified.IsSecret.Should().Be("`false`");

        // AllowOverride changed - contains HTML-formatted diff
        modified.AllowOverride.Should().Contain("e"); // Common letter in true/false
        modified.AllowOverride.Should().Contain("-");
        modified.AllowOverride.Should().Contain("+");
    }

    #endregion

    #region TC-08: Update - Removed Variables

    /// <summary>
    /// TC-08: Verifies removed variables are correctly categorized.
    /// </summary>
    [Test]
    public void Build_UpdateWithRemovedVariables_CategorizesAsRemoved()
    {
        // Arrange
        var changeJson = CreateResourceChange("update", new
        {
            name = "test-pipeline",
            variable = new object[]
            {
                new { name = "OLD_VAR", value = "old", is_secret = false, allow_override = true, secret_value = "" },
                new { name = "REMOVED_VAR", value = "removed", is_secret = false, allow_override = false, secret_value = "" }
            }
        }, new
        {
            name = "test-pipeline",
            variable = new object[]
            {
                new { name = "OLD_VAR", value = "old", is_secret = false, allow_override = true, secret_value = "" }
            }
        });

        // Act
        var viewModel = BuildDefinitionViewModelFactory.Build(changeJson, ProviderName, DefaultFormat);

        // Assert
        var removed = viewModel.VariableChanges.FirstOrDefault(v => v.Name == "`REMOVED_VAR`");
        removed.Should().NotBeNull();
        removed!.Change.Should().Be("remove");
        removed.ChangeIcon.Should().Be("❌");
        removed.Value.Should().Be("`removed`");
    }

    #endregion

    #region TC-09: Update - Unchanged Variables

    /// <summary>
    /// TC-09: Verifies unchanged variables show single value (no diff).
    /// </summary>
    [Test]
    public void Build_UpdateWithUnchangedVariables_ShowsSingleValue()
    {
        // Arrange
        var changeJson = CreateResourceChange("update", new
        {
            name = "test-pipeline",
            variable = new object[]
            {
                new { name = "UNCHANGED", value = "same", is_secret = false, allow_override = true, secret_value = "" }
            }
        }, new
        {
            name = "test-pipeline",
            variable = new object[]
            {
                new { name = "UNCHANGED", value = "same", is_secret = false, allow_override = true, secret_value = "" }
            }
        });

        // Act
        var viewModel = BuildDefinitionViewModelFactory.Build(changeJson, ProviderName, DefaultFormat);

        // Assert
        var unchanged = viewModel.VariableChanges[0];
        unchanged.Change.Should().Be("unchanged");
        unchanged.ChangeIcon.Should().Be("⏺️");
        unchanged.Name.Should().Be("`UNCHANGED`");
        unchanged.Value.Should().Be("`same`");
        unchanged.IsSecret.Should().Be("`false`");
        unchanged.AllowOverride.Should().Be("`true`");
    }

    #endregion

    #region TC-10: Large Variable Values

    /// <summary>
    /// TC-10: Verifies large variable values are flagged (only for non-secret variables).
    /// </summary>
    [Test]
    public void Build_LargeVariableValues_FlagsIsLargeValue()
    {
        // Arrange
        var largeValue = new string('x', 150); // >100 chars
        var changeJson = CreateResourceChange("create", null, new
        {
            name = "test-pipeline",
            variable = new object[]
            {
                new { name = "LARGE_VAR", value = largeValue, is_secret = false, allow_override = true, secret_value = "" },
                new { name = "SMALL_VAR", value = "small", is_secret = false, allow_override = true, secret_value = "" }
            }
        });

        // Act
        var viewModel = BuildDefinitionViewModelFactory.Build(changeJson, ProviderName, DefaultFormat);

        // Assert
        viewModel.AfterVariables.Should().HaveCount(2);
        viewModel.AfterVariables[0].IsLargeValue.Should().BeTrue();
        viewModel.AfterVariables[1].IsLargeValue.Should().BeFalse();
    }

    #endregion

    #region TC-11: Nested Blocks - CI Trigger, Repository, etc.

    /// <summary>
    /// TC-11: Verifies extraction of all nested blocks (CI trigger, repository, etc.).
    /// </summary>
    [Test]
    public void Build_NestedBlocks_ExtractsAllBlocks()
    {
        // Arrange
        var branchFilter = new[] { "main", "develop" };
        var prBranchFilter = new[] { "main" };
        var includeFilter = new[] { "main" };
        var daysToBuild = new[] { "Mon", "Wed", "Fri" };

        var changeJson = CreateResourceChange("create", null, new
        {
            name = "test-pipeline",
            variable = Array.Empty<object>(),
            ci_trigger = new object[]
            {
                new
                {
                    use_yaml = true,
                    @override = branchFilter
                }
            },
            repository = new object[]
            {
                new
                {
                    repo_type = "TfsGit",
                    repo_id = "12345678-1234-1234-1234-123456789012",
                    branch_name = "refs/heads/main",
                    yml_path = "azure-pipelines.yml",
                    report_build_status = true,
                    service_connection_id = "",
                    github_enterprise_url = ""
                }
            },
            pull_request_trigger = new object[]
            {
                new
                {
                    use_yaml = false,
                    @override = prBranchFilter,
                    forks = new
                    {
                        enabled = true,
                        share_secrets = false
                    },
                    comment_required = "CommunityMembers"
                }
            },
            schedules = new object[]
            {
                new
                {
                    branch_filter = new object[] { new { include = includeFilter } },
                    days_to_build = daysToBuild,
                    schedule_only_with_changes = true,
                    start_hours = 9,
                    start_minutes = 30,
                    time_zone = "(UTC) Coordinated Universal Time"
                }
            }
        });

        // Act
        var viewModel = BuildDefinitionViewModelFactory.Build(changeJson, ProviderName, DefaultFormat);

        // Assert
        viewModel.AfterCiTriggers.Should().HaveCount(1);
        viewModel.AfterCiTriggers[0].UseYaml.Should().Be("`true`");
        viewModel.AfterCiTriggers[0].Override.Should().Contain("main");
        viewModel.AfterCiTriggers[0].Override.Should().Contain("develop");

        viewModel.AfterRepositories.Should().HaveCount(1);
        viewModel.AfterRepositories[0].RepoType.Should().Be("`TfsGit`");
        viewModel.AfterRepositories[0].YmlPath.Should().Be("`azure-pipelines.yml`");

        viewModel.AfterPullRequestTriggers.Should().HaveCount(1);
        viewModel.AfterPullRequestTriggers[0].UseYaml.Should().Be("`false`");

        viewModel.AfterSchedules.Should().HaveCount(1);
        viewModel.AfterSchedules[0].DaysToBuild.Should().Contain("Mon");
    }

    #endregion

    #region TC-12: Empty/Null Attribute Values

    /// <summary>
    /// TC-12: Verifies null/empty attributes are displayed as "-".
    /// </summary>
    [Test]
    public void Build_EmptyOrNullAttributes_DisplaysAsDash()
    {
        // Arrange
        var changeJson = CreateResourceChange("create", null, new
        {
            name = "test-pipeline",
            path = (string?)null,
            agent_pool_name = "",
            variable = new object[]
            {
                new { name = "VAR1", value = "val1", is_secret = false, allow_override = (bool?)null, secret_value = "" }
            }
        });

        // Act
        var viewModel = BuildDefinitionViewModelFactory.Build(changeJson, ProviderName, DefaultFormat);

        // Assert
        viewModel.Path.Should().BeNull();
        viewModel.AgentPoolName.Should().Be("");
        viewModel.AfterVariables[0].AllowOverride.Should().Be("-");
    }

    #endregion

    #region TC-13: Conditional Rendering - Empty Collections

    /// <summary>
    /// TC-13: Verifies empty nested block arrays result in empty view model lists.
    /// </summary>
    [Test]
    public void Build_EmptyNestedBlocks_ResultsInEmptyLists()
    {
        // Arrange
        var changeJson = CreateResourceChange("create", null, new
        {
            name = "test-pipeline",
            variable = new object[]
            {
                new { name = "VAR1", value = "val1", is_secret = false, allow_override = true, secret_value = "" }
            },
            ci_trigger = Array.Empty<object>(),
            repository = Array.Empty<object>(),
            pull_request_trigger = Array.Empty<object>(),
            schedules = Array.Empty<object>()
        });

        // Act
        var viewModel = BuildDefinitionViewModelFactory.Build(changeJson, ProviderName, DefaultFormat);

        // Assert
        viewModel.AfterVariables.Should().HaveCount(1);
        viewModel.AfterCiTriggers.Should().BeEmpty();
        viewModel.AfterRepositories.Should().BeEmpty();
        viewModel.AfterPullRequestTriggers.Should().BeEmpty();
        viewModel.AfterSchedules.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

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
            Address: "azuredevops_build_definition.test",
            ModuleAddress: null,
            Mode: "managed",
            Type: "azuredevops_build_definition",
            Name: "test",
            ProviderName: "azuredevops",
            Change: change
        );
    }

    #endregion
}
