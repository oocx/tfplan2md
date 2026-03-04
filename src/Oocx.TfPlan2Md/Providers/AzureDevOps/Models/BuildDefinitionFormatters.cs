using System;
using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration;
using static Oocx.TfPlan2Md.MarkdownGeneration.MarkdownHelpers;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps.Models;

/// <summary>
/// Formats build definition values and change rows for Azure DevOps build definition rendering.
/// </summary>
/// <remarks>
/// Follows the pattern from VariableGroupFormatters to improve maintainability.
/// Related feature: docs/features/094-build-definition-tables/specification.md.
/// </remarks>
#pragma warning disable CA1506 // Suppress class coupling - formatters need many view model types
internal static class BuildDefinitionFormatters
#pragma warning restore CA1506
{
    /// <summary>
    /// Change label used for added variables.
    /// </summary>
    private const string AddedChange = "add";

    /// <summary>
    /// Change label used for removed variables.
    /// </summary>
    private const string RemovedChange = "remove";

    /// <summary>
    /// Change label used for unchanged variables.
    /// </summary>
    private const string UnchangedChange = "unchanged";

    /// <summary>
    /// Change label used for modified variables.
    /// </summary>
    private const string ModifiedChange = "update";

    /// <summary>
    /// Formats variable values for create/delete tables.
    /// </summary>
    /// <param name="variables">Raw variable values.</param>
    /// <param name="providerName">The Terraform provider name for semantic formatting.</param>
    /// <returns>Formatted variable rows.</returns>
    public static List<BuildDefinitionVariableRowViewModel> FormatVariableRows(
        IReadOnlyList<BuildDefinitionVariableValues> variables,
        string? providerName)
    {
        return variables
            .OrderBy(variable => variable.Name, StringComparer.Ordinal)
            .Select(variable => new BuildDefinitionVariableRowViewModel
            {
                Name = FormatAttributeValueTable("name", variable.Name, providerName),
                Value = FormatVariableValue(variable),
                IsSecret = FormatBoolean(variable.IsSecret),
                AllowOverride = FormatBoolean(variable.AllowOverride),
                IsLargeValue = IsLargeValue(variable)
            })
            .ToList();
    }

    /// <summary>
    /// Creates a formatted row for an added variable.
    /// </summary>
    /// <param name="variable">Variable values from the after state.</param>
    /// <param name="providerName">The Terraform provider name for semantic formatting.</param>
    /// <returns>Formatted change row.</returns>
    public static BuildDefinitionVariableChangeRowViewModel CreateAddedRow(BuildDefinitionVariableValues variable, string? providerName)
    {
        return new BuildDefinitionVariableChangeRowViewModel
        {
            Change = AddedChange,
            ChangeIcon = ActionIcons.Add,
            Name = FormatAttributeValueTable("name", variable.Name, providerName),
            Value = FormatVariableValue(variable),
            IsSecret = FormatBoolean(variable.IsSecret),
            AllowOverride = FormatBoolean(variable.AllowOverride),
            IsLargeValue = IsLargeValue(variable)
        };
    }

    /// <summary>
    /// Creates a formatted row for a removed variable.
    /// </summary>
    /// <param name="variable">Variable values from the before state.</param>
    /// <param name="providerName">The Terraform provider name for semantic formatting.</param>
    /// <returns>Formatted change row.</returns>
    public static BuildDefinitionVariableChangeRowViewModel CreateRemovedRow(BuildDefinitionVariableValues variable, string? providerName)
    {
        return new BuildDefinitionVariableChangeRowViewModel
        {
            Change = RemovedChange,
            ChangeIcon = ActionIcons.Delete,
            Name = FormatAttributeValueTable("name", variable.Name, providerName),
            Value = FormatVariableValue(variable),
            IsSecret = FormatBoolean(variable.IsSecret),
            AllowOverride = FormatBoolean(variable.AllowOverride),
            IsLargeValue = IsLargeValue(variable)
        };
    }

    /// <summary>
    /// Creates a formatted row for an unchanged variable.
    /// </summary>
    /// <param name="variable">Variable values.</param>
    /// <param name="providerName">The Terraform provider name for semantic formatting.</param>
    /// <returns>Formatted change row.</returns>
    public static BuildDefinitionVariableChangeRowViewModel CreateUnchangedRow(BuildDefinitionVariableValues variable, string? providerName)
    {
        return new BuildDefinitionVariableChangeRowViewModel
        {
            Change = UnchangedChange,
            ChangeIcon = ActionIcons.Unchanged,
            Name = FormatAttributeValueTable("name", variable.Name, providerName),
            Value = FormatVariableValue(variable),
            IsSecret = FormatBoolean(variable.IsSecret),
            AllowOverride = FormatBoolean(variable.AllowOverride),
            IsLargeValue = IsLargeValue(variable)
        };
    }

    /// <summary>
    /// Creates a formatted diff row for a modified variable.
    /// </summary>
    /// <param name="before">Variable values before the change.</param>
    /// <param name="after">Variable values after the change.</param>
    /// <param name="largeValueFormat">Preferred diff format.</param>
    /// <param name="providerName">The Terraform provider name for semantic formatting.</param>
    /// <returns>Formatted diff row.</returns>
    public static BuildDefinitionVariableChangeRowViewModel CreateDiffRow(
        BuildDefinitionVariableValues before,
        BuildDefinitionVariableValues after,
        LargeValueFormat largeValueFormat,
        string? providerName)
    {
        var format = largeValueFormat.ToString();

        // SECURITY: For secret variables, always show masked value (no diff)
        // If is_secret changes to true, we still mask the value
        var valueDisplay = (before.IsSecret || after.IsSecret)
            ? "`(sensitive / hidden)`"
            : FormatVariableValueDiff(before, after, format);

        return new BuildDefinitionVariableChangeRowViewModel
        {
            Change = ModifiedChange,
            ChangeIcon = ActionIcons.Update,
            Name = FormatAttributeValueTable("name", after.Name, providerName),
            Value = valueDisplay,
            IsSecret = FormatBooleanDiff(before.IsSecret, after.IsSecret, format),
            AllowOverride = FormatBooleanDiff(before.AllowOverride, after.AllowOverride, format),
            IsLargeValue = IsLargeValue(after)
        };
    }

    /// <summary>
    /// Formats a variable value, showing "(sensitive / hidden)" for secrets.
    /// SECURITY: This method ensures secret values are NEVER displayed.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>Formatted value.</returns>
    private static string FormatVariableValue(BuildDefinitionVariableValues variable)
    {
        // SECURITY: Always mask secret variables
        if (variable.IsSecret)
        {
            return "`(sensitive / hidden)`";
        }

        if (string.IsNullOrEmpty(variable.Value))
        {
            return "-";
        }

        return $"`{EscapeMarkdown(variable.Value)}`";
    }

    /// <summary>
    /// Formats a variable value diff for non-secret variables.
    /// </summary>
    /// <param name="before">Before variable.</param>
    /// <param name="after">After variable.</param>
    /// <param name="format">Diff format.</param>
    /// <returns>Formatted diff or single value.</returns>
    private static string FormatVariableValueDiff(
        BuildDefinitionVariableValues before,
        BuildDefinitionVariableValues after,
        string format)
    {
        var beforeStr = string.IsNullOrEmpty(before.Value) ? "-" : before.Value;
        var afterStr = string.IsNullOrEmpty(after.Value) ? "-" : after.Value;

        if (beforeStr == afterStr)
        {
            return FormatVariableValue(after);
        }

        return FormatDiff(beforeStr, afterStr, format);
    }

    /// <summary>
    /// Formats a boolean value or displays dash for null.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <returns>Formatted string.</returns>
    private static string FormatBoolean(bool? value)
    {
        if (value == null)
        {
            return "-";
        }

        var icon = value.Value ? "✅" : "❌";
        var text = value.Value ? "true" : "false";
        return $"`{icon}{NonBreakingSpace}{text}`";
    }

    /// <summary>
    /// Formats a boolean diff.
    /// </summary>
    /// <param name="before">Before value.</param>
    /// <param name="after">After value.</param>
    /// <param name="format">Diff format.</param>
    /// <returns>Formatted diff or single value.</returns>
    private static string FormatBooleanDiff(bool? before, bool? after, string format)
    {
        var beforeStr = ConvertBoolToString(before);
        var afterStr = ConvertBoolToString(after);

        if (beforeStr == afterStr)
        {
            return FormatBoolean(after);
        }

        return FormatDiff(beforeStr, afterStr, format);
    }

    /// <summary>
    /// Converts a nullable boolean to its string representation for display.
    /// </summary>
    /// <param name="value">Boolean value to convert.</param>
    /// <returns>"true", "false", or "-" for null.</returns>
    private static string ConvertBoolToString(bool? value)
    {
        if (value == null)
        {
            return "-";
        }

        return value.Value ? "true" : "false";
    }

    /// <summary>
    /// Formats an optional string value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Formatted string or dash.</returns>
    private static string FormatOptionalString(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "-";
        }

        return $"`{EscapeMarkdown(value)}`";
    }

    /// <summary>
    /// Formats a list of branch filters or other string arrays.
    /// </summary>
    /// <param name="filters">The list of filters.</param>
    /// <returns>Formatted comma-separated list or dash.</returns>
    private static string FormatStringList(IReadOnlyList<string>? filters)
    {
        if (filters == null || filters.Count == 0)
        {
            return "-";
        }

        return string.Join(", ", filters.Select(f => $"`{EscapeMarkdown(f)}`"));
    }

    /// <summary>
    /// Formats a time from hours and minutes.
    /// </summary>
    /// <param name="hours">The hours value.</param>
    /// <param name="minutes">The minutes value.</param>
    /// <returns>Formatted time as HH:MM or dash.</returns>
    private static string FormatTime(int? hours, int? minutes)
    {
        if (hours == null || minutes == null)
        {
            return "-";
        }

        return $"`{hours.Value:D2}:{minutes.Value:D2}`";
    }

    /// <summary>
    /// Determines if a variable value should be treated as large.
    /// Secret variables are never large (value is masked).
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>True if large; otherwise false.</returns>
    private static bool IsLargeValue(BuildDefinitionVariableValues variable)
    {
        // SECURITY: Secret variables are never large (value is masked)
        if (variable.IsSecret)
        {
            return false;
        }

        return MarkdownHelpers.IsLargeValue(variable.Value, null);
    }

    /// <summary>
    /// Creates a formatted row for a CI trigger block.
    /// </summary>
    /// <param name="trigger">CI trigger values.</param>
    /// <returns>Formatted CI trigger row.</returns>
    public static CiTriggerRowViewModel CreateCiTriggerRow(CiTriggerValues trigger)
    {
        return new CiTriggerRowViewModel
        {
            UseYaml = FormatBoolean(trigger.UseYaml),
            Override = FormatStringList(trigger.Override)
        };
    }

    /// <summary>
    /// Creates a formatted row for a pull request trigger block.
    /// </summary>
    /// <param name="trigger">Pull request trigger values.</param>
    /// <returns>Formatted pull request trigger row.</returns>
    public static PullRequestTriggerRowViewModel CreatePullRequestTriggerRow(PullRequestTriggerValues trigger)
    {
        return new PullRequestTriggerRowViewModel
        {
            UseYaml = FormatBoolean(trigger.UseYaml),
            Override = FormatStringList(trigger.Override),
            ForksEnabled = FormatBoolean(trigger.ForksEnabled),
            ForksCommentRequirement = FormatOptionalString(trigger.ForksCommentRequirement)
        };
    }

    /// <summary>
    /// Creates a formatted row for a schedule block.
    /// </summary>
    /// <param name="schedule">Schedule values.</param>
    /// <returns>Formatted schedule row.</returns>
    public static ScheduleRowViewModel CreateScheduleRow(ScheduleValues schedule)
    {
        return new ScheduleRowViewModel
        {
            BranchFilters = FormatStringList(schedule.BranchFilters),
            DaysToBuild = FormatStringList(schedule.DaysToBuild),
            ScheduleOnlyWithChanges = FormatBoolean(schedule.ScheduleOnlyWithChanges),
            StartTime = FormatTime(schedule.StartHours, schedule.StartMinutes),
            TimeZone = FormatOptionalString(schedule.TimeZone)
        };
    }

    /// <summary>
    /// Creates a formatted row for a repository block.
    /// </summary>
    /// <param name="repository">Repository values.</param>
    /// <param name="repositoryMapper">Optional mapper for resolving repository display names.</param>
    /// <returns>Formatted repository row.</returns>
    public static RepositoryRowViewModel CreateRepositoryRow(RepositoryValues repository, AzdoRepositoryMapper? repositoryMapper = null)
    {
        string formattedRepoId;
        if (string.IsNullOrEmpty(repository.RepoId))
        {
            formattedRepoId = "-";
        }
        else if (repositoryMapper != null)
        {
            formattedRepoId = FormatIconValueTable(repositoryMapper.GetEntityName(repository.RepoId));
        }
        else
        {
            formattedRepoId = FormatIconValueTable($"🗃️\u00A0{repository.RepoId}");
        }

        return new RepositoryRowViewModel
        {
            RepoType = FormatOptionalString(repository.RepoType),
            RepoId = formattedRepoId,
            BranchName = string.IsNullOrEmpty(repository.BranchName)
                ? "-"
                : FormatIconValueTable($"⎇\u00A0{repository.BranchName}"),
            YmlPath = FormatOptionalString(repository.YmlPath),
            ReportBuildStatus = FormatBoolean(repository.ReportBuildStatus),
            ServiceConnectionId = FormatOptionalString(repository.ServiceConnectionId),
            GithubEnterpriseUrl = FormatOptionalString(repository.GithubEnterpriseUrl)
        };
    }

    /// <summary>
    /// Creates a formatted row for a job block.
    /// </summary>
    /// <param name="job">Job values.</param>
    /// <param name="providerName">The Terraform provider name for semantic formatting.</param>
    /// <returns>Formatted job row.</returns>
    public static JobRowViewModel CreateJobRow(JobValues job, string? providerName)
    {
        return new JobRowViewModel
        {
            Name = string.IsNullOrEmpty(job.Name) ? "-" : FormatAttributeValueTable("name", job.Name, providerName),
            Condition = FormatOptionalString(job.Condition),
            TimeoutInMinutes = job.TimeoutInMinutes.HasValue
                ? $"`{job.TimeoutInMinutes.Value}`"
                : "-"
        };
    }
}
