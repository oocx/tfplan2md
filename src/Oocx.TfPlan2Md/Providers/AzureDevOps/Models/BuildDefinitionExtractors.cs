using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps.Models;

/// <summary>
/// Extracts build definition data from Terraform JSON state for Azure DevOps build definitions.
/// </summary>
/// <remarks>
/// Follows the pattern from VariableGroupExtractors to improve maintainability.
/// Related feature: docs/features/094-build-definition-tables/specification.md.
/// </remarks>
internal static class BuildDefinitionExtractors
{
    /// <summary>
    /// Extracts the build definition name from the provided state JSON.
    /// </summary>
    /// <param name="state">Terraform state object from the plan.</param>
    /// <returns>Name value when present; otherwise null.</returns>
    public static string? ExtractName(object? state)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return element.TryGetProperty("name", out var nameProperty) && nameProperty.ValueKind == JsonValueKind.String
            ? nameProperty.GetString()
            : null;
    }

    /// <summary>
    /// Extracts the build definition path from the provided state JSON.
    /// </summary>
    /// <param name="state">Terraform state object from the plan.</param>
    /// <returns>Path value when present; otherwise null.</returns>
    public static string? ExtractPath(object? state)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return element.TryGetProperty("path", out var pathProperty) && pathProperty.ValueKind == JsonValueKind.String
            ? pathProperty.GetString()
            : null;
    }

    /// <summary>
    /// Extracts the agent pool name from the provided state JSON.
    /// </summary>
    /// <param name="state">Terraform state object from the plan.</param>
    /// <returns>Agent pool name when present; otherwise null.</returns>
    public static string? ExtractAgentPoolName(object? state)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return element.TryGetProperty("agent_pool_name", out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    /// <summary>
    /// Extracts the queue status from the provided state JSON.
    /// </summary>
    /// <param name="state">Terraform state object from the plan.</param>
    /// <returns>Queue status when present; otherwise null.</returns>
    public static string? ExtractQueueStatus(object? state)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return element.TryGetProperty("queue_status", out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    /// <summary>
    /// Extracts variables from the provided state JSON.
    /// </summary>
    /// <param name="state">Terraform state object containing variable array.</param>
    /// <returns>Collection of extracted variable values.</returns>
    public static IReadOnlyList<BuildDefinitionVariableValues> ExtractVariables(object? state)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<BuildDefinitionVariableValues>();
        }

        var variables = new List<BuildDefinitionVariableValues>();

        // Extract variables from the 'variable' array
        if (element.TryGetProperty("variable", out var varsElement) && varsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var varElement in varsElement.EnumerateArray())
            {
                if (varElement.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var name = GetString(varElement, "name");
                var value = GetString(varElement, "value");
                var isSecret = GetNullableBool(varElement, "is_secret");
                var allowOverride = GetNullableBool(varElement, "allow_override");

                variables.Add(new BuildDefinitionVariableValues(
                    name,
                    value,
                    isSecret ?? false,
                    allowOverride));
            }
        }

        return variables;
    }

    /// <summary>
    /// Extracts CI trigger blocks from the provided state JSON.
    /// </summary>
    /// <param name="state">Terraform state object from the plan.</param>
    /// <returns>Collection of CI trigger block values.</returns>
    public static IReadOnlyList<CiTriggerValues> ExtractCiTriggers(object? state)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<CiTriggerValues>();
        }

        if (!element.TryGetProperty("ci_trigger", out var ciElement) || ciElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<CiTriggerValues>();
        }

        var triggers = new List<CiTriggerValues>();
        foreach (var triggerElement in ciElement.EnumerateArray())
        {
            if (triggerElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var useYaml = GetNullableBool(triggerElement, "use_yaml");
            var overrideFilters = GetStringArray(triggerElement, "override");

            triggers.Add(new CiTriggerValues(useYaml, overrideFilters));
        }

        return triggers;
    }

    /// <summary>
    /// Extracts pull request trigger blocks from the provided state JSON.
    /// </summary>
    /// <param name="state">Terraform state object from the plan.</param>
    /// <returns>Collection of pull request trigger block values.</returns>
    public static IReadOnlyList<PullRequestTriggerValues> ExtractPullRequestTriggers(object? state)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<PullRequestTriggerValues>();
        }

        if (!element.TryGetProperty("pull_request_trigger", out var prElement) || prElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<PullRequestTriggerValues>();
        }

        var triggers = new List<PullRequestTriggerValues>();
        foreach (var triggerElement in prElement.EnumerateArray())
        {
            if (triggerElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var useYaml = GetNullableBool(triggerElement, "use_yaml");
            var overrideFilters = GetStringArray(triggerElement, "override");

            // Extract forks settings
            bool? forksEnabled = null;
            string? forksCommentRequirement = null;

            if (triggerElement.TryGetProperty("forks", out var forksElement) && forksElement.ValueKind == JsonValueKind.Object)
            {
                forksEnabled = GetNullableBool(forksElement, "enabled");
                forksCommentRequirement = GetString(forksElement, "share_secrets");
            }

            triggers.Add(new PullRequestTriggerValues(useYaml, overrideFilters, forksEnabled, forksCommentRequirement));
        }

        return triggers;
    }

    /// <summary>
    /// Extracts schedule blocks from the provided state JSON.
    /// </summary>
    /// <param name="state">Terraform state object from the plan.</param>
    /// <returns>Collection of schedule block values.</returns>
    public static IReadOnlyList<ScheduleValues> ExtractSchedules(object? state)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<ScheduleValues>();
        }

        if (!element.TryGetProperty("schedules", out var schedElement) || schedElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<ScheduleValues>();
        }

        var schedules = new List<ScheduleValues>();
        foreach (var scheduleElement in schedElement.EnumerateArray())
        {
            if (scheduleElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var branchFilters = GetStringArray(scheduleElement, "branch_filter");
            var daysToBuild = GetStringArray(scheduleElement, "days_to_build");
            var scheduleOnlyWithChanges = GetNullableBool(scheduleElement, "schedule_only_with_changes");
            var startHours = GetNullableInt(scheduleElement, "start_hours");
            var startMinutes = GetNullableInt(scheduleElement, "start_minutes");
            var timeZone = GetString(scheduleElement, "time_zone");

            schedules.Add(new ScheduleValues(
                branchFilters,
                daysToBuild,
                scheduleOnlyWithChanges,
                startHours,
                startMinutes,
                timeZone));
        }

        return schedules;
    }

    /// <summary>
    /// Extracts repository blocks from the provided state JSON.
    /// </summary>
    /// <param name="state">Terraform state object from the plan.</param>
    /// <returns>Collection of repository block values.</returns>
    public static IReadOnlyList<RepositoryValues> ExtractRepositories(object? state)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<RepositoryValues>();
        }

        if (!element.TryGetProperty("repository", out var repoElement) || repoElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<RepositoryValues>();
        }

        var repositories = new List<RepositoryValues>();
        foreach (var repositoryElement in repoElement.EnumerateArray())
        {
            if (repositoryElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var repoType = GetString(repositoryElement, "repo_type");
            var repoId = GetString(repositoryElement, "repo_id");
            var branchName = GetString(repositoryElement, "branch_name");
            var ymlPath = GetString(repositoryElement, "yml_path");
            var reportBuildStatus = GetNullableBool(repositoryElement, "report_build_status");
            var serviceConnectionId = GetString(repositoryElement, "service_connection_id");
            var githubEnterpriseUrl = GetString(repositoryElement, "github_enterprise_url");

            repositories.Add(new RepositoryValues(
                repoType,
                repoId,
                branchName,
                ymlPath,
                reportBuildStatus,
                serviceConnectionId,
                githubEnterpriseUrl));
        }

        return repositories;
    }

    /// <summary>
    /// Extracts job blocks from the provided state JSON.
    /// </summary>
    /// <param name="state">Terraform state object from the plan.</param>
    /// <returns>Collection of job block values.</returns>
    public static IReadOnlyList<JobValues> ExtractJobs(object? state)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<JobValues>();
        }

        if (!element.TryGetProperty("jobs", out var jobsElement) || jobsElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<JobValues>();
        }

        var jobs = new List<JobValues>();
        foreach (var jobElement in jobsElement.EnumerateArray())
        {
            if (jobElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = GetString(jobElement, "name");
            var condition = GetString(jobElement, "condition");
            var timeoutInMinutes = GetNullableInt(jobElement, "timeout_in_minutes");

            jobs.Add(new JobValues(name, condition, timeoutInMinutes));
        }

        return jobs;
    }

    /// <summary>
    /// Gets a string property from a JSON element.
    /// </summary>
    /// <param name="element">The JSON element.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The string value or empty string.</returns>
    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    /// <summary>
    /// Gets a nullable boolean property from a JSON element.
    /// </summary>
    /// <param name="element">The JSON element.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The boolean value or null.</returns>
    private static bool? GetNullableBool(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            if (property.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (property.ValueKind == JsonValueKind.False)
            {
                return false;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets a nullable integer property from a JSON element.
    /// </summary>
    /// <param name="element">The JSON element.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The integer value or null.</returns>
    private static int? GetNullableInt(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number)
        {
            return property.GetInt32();
        }

        return null;
    }

    /// <summary>
    /// Gets a string array property from a JSON element.
    /// </summary>
    /// <param name="element">The JSON element.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The string array or empty array.</returns>
    private static IReadOnlyList<string> GetStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        var items = new List<string>();
        foreach (var item in property.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var value = item.GetString();
                if (!string.IsNullOrEmpty(value))
                {
                    items.Add(value);
                }
            }
        }

        return items;
    }
}

/// <summary>
/// Internal record holding extracted variable values from Terraform state.
/// </summary>
internal record BuildDefinitionVariableValues(
    string Name,
    string? Value,
    bool IsSecret,
    bool? AllowOverride);

/// <summary>
/// Internal record holding extracted CI trigger values from Terraform state.
/// </summary>
internal record CiTriggerValues(
    bool? UseYaml,
    IReadOnlyList<string> Override);

/// <summary>
/// Internal record holding extracted pull request trigger values from Terraform state.
/// </summary>
internal record PullRequestTriggerValues(
    bool? UseYaml,
    IReadOnlyList<string> Override,
    bool? ForksEnabled,
    string? ForksCommentRequirement);

/// <summary>
/// Internal record holding extracted schedule values from Terraform state.
/// </summary>
internal record ScheduleValues(
    IReadOnlyList<string> BranchFilters,
    IReadOnlyList<string> DaysToBuild,
    bool? ScheduleOnlyWithChanges,
    int? StartHours,
    int? StartMinutes,
    string? TimeZone);

/// <summary>
/// Internal record holding extracted repository values from Terraform state.
/// </summary>
internal record RepositoryValues(
    string? RepoType,
    string? RepoId,
    string? BranchName,
    string? YmlPath,
    bool? ReportBuildStatus,
    string? ServiceConnectionId,
    string? GithubEnterpriseUrl);

/// <summary>
/// Internal record holding extracted job values from Terraform state.
/// </summary>
internal record JobValues(
    string? Name,
    string? Condition,
    int? TimeoutInMinutes);
