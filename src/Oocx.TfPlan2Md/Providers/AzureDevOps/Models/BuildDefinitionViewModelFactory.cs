using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps.Models;

/// <summary>
/// Builds <see cref="BuildDefinitionViewModel"/> instances from Terraform plan data.
/// Related feature: docs/features/094-build-definition-tables/specification.md.
/// </summary>
[SuppressMessage("Design", "CA1506:Avoid excessive class coupling", Justification = "Factory orchestrates many view model types, extractors, and formatters by design.")]
internal static class BuildDefinitionViewModelFactory
{
    /// <summary>
    /// Creates a view model for the provided build definition change.
    /// </summary>
    /// <param name="change">The resource change containing before/after state.</param>
    /// <param name="providerName">The provider name for semantic formatting.</param>
    /// <param name="largeValueFormat">Preferred large value format for diff rendering.</param>
    /// <param name="repositoryMapper">Optional mapper for resolving repository display names.</param>
    /// <returns>Populated <see cref="BuildDefinitionViewModel"/>.</returns>
    public static BuildDefinitionViewModel Build(ResourceChange change, string providerName, LargeValueFormat largeValueFormat, AzdoRepositoryMapper? repositoryMapper = null)
    {
        // Extract metadata
        var name = BuildDefinitionExtractors.ExtractName(change.Change.After)
            ?? BuildDefinitionExtractors.ExtractName(change.Change.Before);
        var path = BuildDefinitionExtractors.ExtractPath(change.Change.After)
            ?? BuildDefinitionExtractors.ExtractPath(change.Change.Before);
        var agentPoolName = BuildDefinitionExtractors.ExtractAgentPoolName(change.Change.After)
            ?? BuildDefinitionExtractors.ExtractAgentPoolName(change.Change.Before);
        var queueStatus = BuildDefinitionExtractors.ExtractQueueStatus(change.Change.After)
            ?? BuildDefinitionExtractors.ExtractQueueStatus(change.Change.Before);

        // Extract variables
        var beforeVariables = BuildDefinitionExtractors.ExtractVariables(change.Change.Before);
        var afterVariables = BuildDefinitionExtractors.ExtractVariables(change.Change.After);

        // Extract other blocks
        var beforeCiTriggers = BuildDefinitionExtractors.ExtractCiTriggers(change.Change.Before);
        var afterCiTriggers = BuildDefinitionExtractors.ExtractCiTriggers(change.Change.After);

        var beforePullRequestTriggers = BuildDefinitionExtractors.ExtractPullRequestTriggers(change.Change.Before);
        var afterPullRequestTriggers = BuildDefinitionExtractors.ExtractPullRequestTriggers(change.Change.After);

        var beforeSchedules = BuildDefinitionExtractors.ExtractSchedules(change.Change.Before);
        var afterSchedules = BuildDefinitionExtractors.ExtractSchedules(change.Change.After);

        var beforeRepositories = BuildDefinitionExtractors.ExtractRepositories(change.Change.Before);
        var afterRepositories = BuildDefinitionExtractors.ExtractRepositories(change.Change.After);

        var beforeJobs = BuildDefinitionExtractors.ExtractJobs(change.Change.Before);
        var afterJobs = BuildDefinitionExtractors.ExtractJobs(change.Change.After);

        // Determine action type (create, update, delete)
        var actions = change.Change.Actions ?? Array.Empty<string>();
        var isCreate = actions.Contains("create") && !actions.Contains("delete");
        var isDelete = actions.Contains("delete") && !actions.Contains("create");

        if (isCreate)
        {
            return new BuildDefinitionViewModel
            {
                Name = name,
                Path = path,
                AgentPoolName = agentPoolName,
                QueueStatus = queueStatus,
                AfterVariables = BuildDefinitionFormatters.FormatVariableRows(afterVariables, providerName),
                AfterCiTriggers = FormatCiTriggerRows(afterCiTriggers),
                AfterPullRequestTriggers = FormatPullRequestTriggerRows(afterPullRequestTriggers),
                AfterSchedules = FormatScheduleRows(afterSchedules),
                AfterRepositories = FormatRepositoryRows(afterRepositories, repositoryMapper),
                AfterJobs = FormatJobRows(afterJobs, providerName)
            };
        }
        else if (isDelete)
        {
            return new BuildDefinitionViewModel
            {
                Name = name,
                Path = path,
                AgentPoolName = agentPoolName,
                QueueStatus = queueStatus,
                BeforeVariables = BuildDefinitionFormatters.FormatVariableRows(beforeVariables, providerName),
                BeforeCiTriggers = FormatCiTriggerRows(beforeCiTriggers),
                BeforePullRequestTriggers = FormatPullRequestTriggerRows(beforePullRequestTriggers),
                BeforeSchedules = FormatScheduleRows(beforeSchedules),
                BeforeRepositories = FormatRepositoryRows(beforeRepositories, repositoryMapper),
                BeforeJobs = FormatJobRows(beforeJobs, providerName)
            };
        }
        else // update or replace
        {
            // Build variable changes using semantic diffing
            var added = BuildDefinitionChangeBuilders.BuildAdded(afterVariables, beforeVariables, providerName);
            var removed = BuildDefinitionChangeBuilders.BuildRemoved(beforeVariables, afterVariables, providerName);
            var modified = BuildDefinitionChangeBuilders.BuildModified(beforeVariables, afterVariables, largeValueFormat, providerName);
            var unchanged = BuildDefinitionChangeBuilders.BuildUnchanged(beforeVariables, afterVariables, providerName);

            var variableChanges = new List<BuildDefinitionVariableChangeRowViewModel>();
            variableChanges.AddRange(added);
            variableChanges.AddRange(modified);
            variableChanges.AddRange(removed);
            variableChanges.AddRange(unchanged);

            return new BuildDefinitionViewModel
            {
                Name = name,
                Path = path,
                AgentPoolName = agentPoolName,
                QueueStatus = queueStatus,
                VariableChanges = variableChanges,
                AfterCiTriggers = FormatCiTriggerRows(afterCiTriggers),
                BeforeCiTriggers = FormatCiTriggerRows(beforeCiTriggers),
                AfterPullRequestTriggers = FormatPullRequestTriggerRows(afterPullRequestTriggers),
                BeforePullRequestTriggers = FormatPullRequestTriggerRows(beforePullRequestTriggers),
                AfterSchedules = FormatScheduleRows(afterSchedules),
                BeforeSchedules = FormatScheduleRows(beforeSchedules),
                AfterRepositories = FormatRepositoryRows(afterRepositories, repositoryMapper),
                BeforeRepositories = FormatRepositoryRows(beforeRepositories, repositoryMapper),
                AfterJobs = FormatJobRows(afterJobs, providerName),
                BeforeJobs = FormatJobRows(beforeJobs, providerName)
            };
        }
    }

    /// <summary>
    /// Formats CI trigger rows from extracted values.
    /// </summary>
    /// <param name="triggers">Extracted CI trigger values.</param>
    /// <returns>Formatted CI trigger rows.</returns>
    private static List<CiTriggerRowViewModel> FormatCiTriggerRows(IReadOnlyList<CiTriggerValues> triggers)
    {
        return triggers.Select(BuildDefinitionFormatters.CreateCiTriggerRow).ToList();
    }

    /// <summary>
    /// Formats pull request trigger rows from extracted values.
    /// </summary>
    /// <param name="triggers">Extracted pull request trigger values.</param>
    /// <returns>Formatted pull request trigger rows.</returns>
    private static List<PullRequestTriggerRowViewModel> FormatPullRequestTriggerRows(IReadOnlyList<PullRequestTriggerValues> triggers)
    {
        return triggers.Select(BuildDefinitionFormatters.CreatePullRequestTriggerRow).ToList();
    }

    /// <summary>
    /// Formats schedule rows from extracted values.
    /// </summary>
    /// <param name="schedules">Extracted schedule values.</param>
    /// <returns>Formatted schedule rows.</returns>
    private static List<ScheduleRowViewModel> FormatScheduleRows(IReadOnlyList<ScheduleValues> schedules)
    {
        return schedules.Select(BuildDefinitionFormatters.CreateScheduleRow).ToList();
    }

    /// <summary>
    /// Formats repository rows from extracted values.
    /// </summary>
    /// <param name="repositories">Extracted repository values.</param>
    /// <param name="repositoryMapper">Optional mapper for resolving repository display names.</param>
    /// <returns>Formatted repository rows.</returns>
    private static List<RepositoryRowViewModel> FormatRepositoryRows(IReadOnlyList<RepositoryValues> repositories, AzdoRepositoryMapper? repositoryMapper)
    {
        return repositories.Select(r => BuildDefinitionFormatters.CreateRepositoryRow(r, repositoryMapper)).ToList();
    }

    /// <summary>
    /// Formats job rows from extracted values.
    /// </summary>
    /// <param name="jobs">Extracted job values.</param>
    /// <param name="providerName">The Terraform provider name for semantic formatting.</param>
    /// <returns>Formatted job rows.</returns>
    private static List<JobRowViewModel> FormatJobRows(IReadOnlyList<JobValues> jobs, string? providerName)
    {
        return jobs.Select(job => BuildDefinitionFormatters.CreateJobRow(job, providerName)).ToList();
    }
}
