using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Providers.AzureDevOps.Models;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps.Mappers;

/// <summary>
/// Maps azuredevops_build_definition resources to ScriptObject with BuildDefinitionViewModel.
/// Related feature: docs/features/094-build-definition-tables/specification.md.
/// </summary>
internal sealed class BuildDefinitionMapper : IResourceModelMapper
{
    private readonly BuildDefinitionFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildDefinitionMapper"/> class.
    /// </summary>
    /// <param name="factory">The factory for creating BuildDefinitionViewModel instances.</param>
    public BuildDefinitionMapper(BuildDefinitionFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    /// <summary>
    /// Determines whether this mapper applies to the resource.
    /// </summary>
    /// <param name="resource">The resource to evaluate.</param>
    /// <returns><c>true</c> if the resource is azuredevops_build_definition; otherwise, <c>false</c>.</returns>
    public bool CanMap(ResourceChangeModel resource)
    {
        return resource.Type == "azuredevops_build_definition";
    }

    /// <summary>
    /// Enriches the ScriptObject with build_definition property.
    /// </summary>
    /// <param name="resource">The resource change model.</param>
    /// <param name="scriptObject">The ScriptObject to enrich.</param>
    public void EnrichScriptObject(ResourceChangeModel resource, ScriptObject scriptObject)
    {
        if (resource.ResourceChange == null)
        {
            return;
        }

        var viewModel = _factory.CreateViewModel(resource.ResourceChange);
        scriptObject["build_definition"] = MapBuildDefinition(viewModel);
    }

    /// <summary>
    /// Maps a BuildDefinitionViewModel to a ScriptObject.
    /// </summary>
    /// <param name="bd">The view model to map.</param>
    /// <returns>A ScriptObject containing the mapped data.</returns>
    private static ScriptObject MapBuildDefinition(BuildDefinitionViewModel bd)
    {
        var obj = new ScriptObject
        {
            ["name"] = bd.Name,
            ["path"] = bd.Path,
            ["agent_pool_name"] = bd.AgentPoolName,
            ["queue_status"] = bd.QueueStatus
        };

        // Variable changes for update scenarios
        var variableChanges = new ScriptArray();
        foreach (var variable in bd.VariableChanges)
        {
            variableChanges.Add(MapVariableChangeRow(variable));
        }
        obj["variable_changes"] = variableChanges;

        // After variables for create scenarios
        var afterVariables = new ScriptArray();
        foreach (var variable in bd.AfterVariables)
        {
            afterVariables.Add(MapVariableRow(variable));
        }
        obj["after_variables"] = afterVariables;

        // Before variables for delete scenarios
        var beforeVariables = new ScriptArray();
        foreach (var variable in bd.BeforeVariables)
        {
            beforeVariables.Add(MapVariableRow(variable));
        }
        obj["before_variables"] = beforeVariables;

        // CI Triggers
        var afterCiTriggers = new ScriptArray();
        foreach (var trigger in bd.AfterCiTriggers)
        {
            afterCiTriggers.Add(MapCiTriggerRow(trigger));
        }
        obj["after_ci_triggers"] = afterCiTriggers;

        var beforeCiTriggers = new ScriptArray();
        foreach (var trigger in bd.BeforeCiTriggers)
        {
            beforeCiTriggers.Add(MapCiTriggerRow(trigger));
        }
        obj["before_ci_triggers"] = beforeCiTriggers;

        // Pull Request Triggers
        var afterPullRequestTriggers = new ScriptArray();
        foreach (var trigger in bd.AfterPullRequestTriggers)
        {
            afterPullRequestTriggers.Add(MapPullRequestTriggerRow(trigger));
        }
        obj["after_pull_request_triggers"] = afterPullRequestTriggers;

        var beforePullRequestTriggers = new ScriptArray();
        foreach (var trigger in bd.BeforePullRequestTriggers)
        {
            beforePullRequestTriggers.Add(MapPullRequestTriggerRow(trigger));
        }
        obj["before_pull_request_triggers"] = beforePullRequestTriggers;

        // Schedules
        var afterSchedules = new ScriptArray();
        foreach (var schedule in bd.AfterSchedules)
        {
            afterSchedules.Add(MapScheduleRow(schedule));
        }
        obj["after_schedules"] = afterSchedules;

        var beforeSchedules = new ScriptArray();
        foreach (var schedule in bd.BeforeSchedules)
        {
            beforeSchedules.Add(MapScheduleRow(schedule));
        }
        obj["before_schedules"] = beforeSchedules;

        // Repositories
        var afterRepositories = new ScriptArray();
        foreach (var repo in bd.AfterRepositories)
        {
            afterRepositories.Add(MapRepositoryRow(repo));
        }
        obj["after_repositories"] = afterRepositories;

        var beforeRepositories = new ScriptArray();
        foreach (var repo in bd.BeforeRepositories)
        {
            beforeRepositories.Add(MapRepositoryRow(repo));
        }
        obj["before_repositories"] = beforeRepositories;

        // Jobs
        var afterJobs = new ScriptArray();
        foreach (var job in bd.AfterJobs)
        {
            afterJobs.Add(MapJobRow(job));
        }
        obj["after_jobs"] = afterJobs;

        var beforeJobs = new ScriptArray();
        foreach (var job in bd.BeforeJobs)
        {
            beforeJobs.Add(MapJobRow(job));
        }
        obj["before_jobs"] = beforeJobs;

        return obj;
    }

    /// <summary>
    /// Maps a BuildDefinitionVariableChangeRowViewModel to a ScriptObject.
    /// </summary>
    /// <param name="variable">The variable change row view model to map.</param>
    /// <returns>A ScriptObject containing the mapped variable change data.</returns>
    private static ScriptObject MapVariableChangeRow(BuildDefinitionVariableChangeRowViewModel variable)
    {
        return new ScriptObject
        {
            ["change"] = variable.Change,
            ["change_icon"] = variable.ChangeIcon,
            ["name"] = variable.Name,
            ["value"] = variable.Value,
            ["is_secret"] = variable.IsSecret,
            ["allow_override"] = variable.AllowOverride,
            ["is_large_value"] = variable.IsLargeValue
        };
    }

    /// <summary>
    /// Maps a BuildDefinitionVariableRowViewModel to a ScriptObject.
    /// </summary>
    /// <param name="variable">The variable row view model to map.</param>
    /// <returns>A ScriptObject containing the mapped variable data.</returns>
    private static ScriptObject MapVariableRow(BuildDefinitionVariableRowViewModel variable)
    {
        return new ScriptObject
        {
            ["name"] = variable.Name,
            ["value"] = variable.Value,
            ["is_secret"] = variable.IsSecret,
            ["allow_override"] = variable.AllowOverride,
            ["is_large_value"] = variable.IsLargeValue
        };
    }

    /// <summary>
    /// Maps a CiTriggerRowViewModel to a ScriptObject.
    /// </summary>
    /// <param name="trigger">The CI trigger row view model to map.</param>
    /// <returns>A ScriptObject containing the mapped CI trigger data.</returns>
    private static ScriptObject MapCiTriggerRow(CiTriggerRowViewModel trigger)
    {
        return new ScriptObject
        {
            ["use_yaml"] = trigger.UseYaml,
            ["override"] = trigger.Override
        };
    }

    /// <summary>
    /// Maps a PullRequestTriggerRowViewModel to a ScriptObject.
    /// </summary>
    /// <param name="trigger">The pull request trigger row view model to map.</param>
    /// <returns>A ScriptObject containing the mapped pull request trigger data.</returns>
    private static ScriptObject MapPullRequestTriggerRow(PullRequestTriggerRowViewModel trigger)
    {
        return new ScriptObject
        {
            ["use_yaml"] = trigger.UseYaml,
            ["override"] = trigger.Override,
            ["forks_enabled"] = trigger.ForksEnabled,
            ["forks_comment_requirement"] = trigger.ForksCommentRequirement
        };
    }

    /// <summary>
    /// Maps a ScheduleRowViewModel to a ScriptObject.
    /// </summary>
    /// <param name="schedule">The schedule row view model to map.</param>
    /// <returns>A ScriptObject containing the mapped schedule data.</returns>
    private static ScriptObject MapScheduleRow(ScheduleRowViewModel schedule)
    {
        return new ScriptObject
        {
            ["branch_filters"] = schedule.BranchFilters,
            ["days_to_build"] = schedule.DaysToBuild,
            ["schedule_only_with_changes"] = schedule.ScheduleOnlyWithChanges,
            ["start_time"] = schedule.StartTime,
            ["time_zone"] = schedule.TimeZone
        };
    }

    /// <summary>
    /// Maps a RepositoryRowViewModel to a ScriptObject.
    /// </summary>
    /// <param name="repository">The repository row view model to map.</param>
    /// <returns>A ScriptObject containing the mapped repository data.</returns>
    private static ScriptObject MapRepositoryRow(RepositoryRowViewModel repository)
    {
        return new ScriptObject
        {
            ["repo_type"] = repository.RepoType,
            ["repo_id"] = repository.RepoId,
            ["branch_name"] = repository.BranchName,
            ["yml_path"] = repository.YmlPath,
            ["report_build_status"] = repository.ReportBuildStatus,
            ["service_connection_id"] = repository.ServiceConnectionId,
            ["github_enterprise_url"] = repository.GithubEnterpriseUrl
        };
    }

    /// <summary>
    /// Maps a JobRowViewModel to a ScriptObject.
    /// </summary>
    /// <param name="job">The job row view model to map.</param>
    /// <returns>A ScriptObject containing the mapped job data.</returns>
    private static ScriptObject MapJobRow(JobRowViewModel job)
    {
        return new ScriptObject
        {
            ["name"] = job.Name,
            ["condition"] = job.Condition,
            ["timeout_in_minutes"] = job.TimeoutInMinutes
        };
    }
}
