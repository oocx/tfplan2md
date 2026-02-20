using System;
using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps.Models;

/// <summary>
/// Builds <see cref="BuildDefinitionViewModel"/> instances from Terraform plan data.
/// Related feature: docs/features/094-build-definition-tables/specification.md.
/// </summary>
#pragma warning disable CA1506 // Suppress class coupling - factory orchestrates many view model types
internal static class BuildDefinitionViewModelFactory
#pragma warning restore CA1506
{
    /// <summary>
    /// Creates a view model for the provided build definition change.
    /// </summary>
    /// <param name="change">The resource change containing before/after state.</param>
    /// <param name="providerName">The provider name for semantic formatting.</param>
    /// <param name="largeValueFormat">Preferred large value format for diff rendering.</param>
    /// <returns>Populated <see cref="BuildDefinitionViewModel"/>.</returns>
#pragma warning disable CA1506 // Suppress class coupling - Build method orchestrates many extractors/formatters
    public static BuildDefinitionViewModel Build(ResourceChange change, string providerName, LargeValueFormat largeValueFormat)
#pragma warning restore CA1506
    {
        _ = providerName; // Not used for Azure DevOps build definitions

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
                AfterVariables = BuildDefinitionFormatters.FormatVariableRows(afterVariables),
                AfterCiTriggers = FormatCiTriggerRows(afterCiTriggers),
                AfterPullRequestTriggers = FormatPullRequestTriggerRows(afterPullRequestTriggers),
                AfterSchedules = FormatScheduleRows(afterSchedules),
                AfterRepositories = FormatRepositoryRows(afterRepositories),
                AfterJobs = FormatJobRows(afterJobs)
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
                BeforeVariables = BuildDefinitionFormatters.FormatVariableRows(beforeVariables),
                BeforeCiTriggers = FormatCiTriggerRows(beforeCiTriggers),
                BeforePullRequestTriggers = FormatPullRequestTriggerRows(beforePullRequestTriggers),
                BeforeSchedules = FormatScheduleRows(beforeSchedules),
                BeforeRepositories = FormatRepositoryRows(beforeRepositories),
                BeforeJobs = FormatJobRows(beforeJobs)
            };
        }
        else // update or replace
        {
            // Build variable changes using semantic diffing
            var added = BuildDefinitionChangeBuilders.BuildAdded(afterVariables, beforeVariables);
            var removed = BuildDefinitionChangeBuilders.BuildRemoved(beforeVariables, afterVariables);
            var modified = BuildDefinitionChangeBuilders.BuildModified(beforeVariables, afterVariables, largeValueFormat);
            var unchanged = BuildDefinitionChangeBuilders.BuildUnchanged(beforeVariables, afterVariables);

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
                AfterRepositories = FormatRepositoryRows(afterRepositories),
                BeforeRepositories = FormatRepositoryRows(beforeRepositories),
                AfterJobs = FormatJobRows(afterJobs),
                BeforeJobs = FormatJobRows(beforeJobs)
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
    /// <returns>Formatted repository rows.</returns>
    private static List<RepositoryRowViewModel> FormatRepositoryRows(IReadOnlyList<RepositoryValues> repositories)
    {
        return repositories.Select(BuildDefinitionFormatters.CreateRepositoryRow).ToList();
    }

    /// <summary>
    /// Formats job rows from extracted values.
    /// </summary>
    /// <param name="jobs">Extracted job values.</param>
    /// <returns>Formatted job rows.</returns>
    private static List<JobRowViewModel> FormatJobRows(IReadOnlyList<JobValues> jobs)
    {
        return jobs.Select(BuildDefinitionFormatters.CreateJobRow).ToList();
    }
}
