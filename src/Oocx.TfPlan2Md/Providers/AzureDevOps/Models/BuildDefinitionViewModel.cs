using System;
using System.Collections.Generic;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps.Models;

/// <summary>
/// Provides precomputed data for azuredevops_build_definition template.
/// Related feature: docs/features/094-build-definition-tables/specification.md.
/// </summary>
public sealed class BuildDefinitionViewModel
{
    /// <summary>
    /// Gets the build definition name derived from the after/before state.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the build definition path within the project.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Gets the agent pool name used by this build definition.
    /// </summary>
    public string? AgentPoolName { get; init; }

    /// <summary>
    /// Gets the queue status (enabled/disabled/paused).
    /// </summary>
    public string? QueueStatus { get; init; }

    /// <summary>
    /// Gets the variable changes for update scenarios (added, modified, removed, unchanged).
    /// Variables are semantically matched by name attribute.
    /// </summary>
    public IReadOnlyList<BuildDefinitionVariableChangeRowViewModel> VariableChanges { get; init; } = Array.Empty<BuildDefinitionVariableChangeRowViewModel>();

    /// <summary>
    /// Gets the variables after the change, used for create operations.
    /// </summary>
    public IReadOnlyList<BuildDefinitionVariableRowViewModel> AfterVariables { get; init; } = Array.Empty<BuildDefinitionVariableRowViewModel>();

    /// <summary>
    /// Gets the variables before the change, used for delete operations.
    /// </summary>
    public IReadOnlyList<BuildDefinitionVariableRowViewModel> BeforeVariables { get; init; } = Array.Empty<BuildDefinitionVariableRowViewModel>();

    /// <summary>
    /// Gets the CI trigger configuration after the change.
    /// </summary>
    public IReadOnlyList<CiTriggerRowViewModel> AfterCiTriggers { get; init; } = Array.Empty<CiTriggerRowViewModel>();

    /// <summary>
    /// Gets the CI trigger configuration before the change.
    /// </summary>
    public IReadOnlyList<CiTriggerRowViewModel> BeforeCiTriggers { get; init; } = Array.Empty<CiTriggerRowViewModel>();

    /// <summary>
    /// Gets the pull request trigger configuration after the change.
    /// </summary>
    public IReadOnlyList<PullRequestTriggerRowViewModel> AfterPullRequestTriggers { get; init; } = Array.Empty<PullRequestTriggerRowViewModel>();

    /// <summary>
    /// Gets the pull request trigger configuration before the change.
    /// </summary>
    public IReadOnlyList<PullRequestTriggerRowViewModel> BeforePullRequestTriggers { get; init; } = Array.Empty<PullRequestTriggerRowViewModel>();

    /// <summary>
    /// Gets the schedule configuration after the change.
    /// </summary>
    public IReadOnlyList<ScheduleRowViewModel> AfterSchedules { get; init; } = Array.Empty<ScheduleRowViewModel>();

    /// <summary>
    /// Gets the schedule configuration before the change.
    /// </summary>
    public IReadOnlyList<ScheduleRowViewModel> BeforeSchedules { get; init; } = Array.Empty<ScheduleRowViewModel>();

    /// <summary>
    /// Gets the repository configuration after the change.
    /// </summary>
    public IReadOnlyList<RepositoryRowViewModel> AfterRepositories { get; init; } = Array.Empty<RepositoryRowViewModel>();

    /// <summary>
    /// Gets the repository configuration before the change.
    /// </summary>
    public IReadOnlyList<RepositoryRowViewModel> BeforeRepositories { get; init; } = Array.Empty<RepositoryRowViewModel>();

    /// <summary>
    /// Gets the jobs configuration after the change (typically empty for YAML pipelines).
    /// </summary>
    public IReadOnlyList<JobRowViewModel> AfterJobs { get; init; } = Array.Empty<JobRowViewModel>();

    /// <summary>
    /// Gets the jobs configuration before the change (typically empty for YAML pipelines).
    /// </summary>
    public IReadOnlyList<JobRowViewModel> BeforeJobs { get; init; } = Array.Empty<JobRowViewModel>();
}

/// <summary>
/// Represents a variable row that includes a change indicator for update tables.
/// Related feature: docs/features/094-build-definition-tables/specification.md.
/// </summary>
public sealed class BuildDefinitionVariableChangeRowViewModel
{
    /// <summary>
    /// Gets the change label used to resolve icons (add/update/remove/unchanged).
    /// </summary>
    public required string Change { get; init; }

    /// <summary>
    /// Gets the icon representing the change type.
    /// </summary>
    public required string ChangeIcon { get; init; }

    /// <summary>
    /// Gets the formatted variable name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the formatted value or diff. For secret variables, shows "(sensitive / hidden)".
    /// For changed attributes, shows before/after with - and + prefixes.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Gets the formatted is_secret status or diff.
    /// </summary>
    public required string IsSecret { get; init; }

    /// <summary>
    /// Gets the formatted allow_override status or diff.
    /// </summary>
    public required string AllowOverride { get; init; }

    /// <summary>
    /// Gets a value indicating whether this variable has a large value (>100 chars or multi-line).
    /// Large values are moved to a separate collapsible section. Only applies to non-secret variables.
    /// </summary>
    public bool IsLargeValue { get; init; }
}

/// <summary>
/// Represents a variable row used for create/delete tables.
/// Related feature: docs/features/094-build-definition-tables/specification.md.
/// </summary>
public sealed class BuildDefinitionVariableRowViewModel
{
    /// <summary>
    /// Gets the formatted variable name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the formatted value. For secret variables, shows "(sensitive / hidden)".
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Gets the formatted is_secret status.
    /// </summary>
    public required string IsSecret { get; init; }

    /// <summary>
    /// Gets the formatted allow_override status.
    /// </summary>
    public required string AllowOverride { get; init; }

    /// <summary>
    /// Gets a value indicating whether this variable has a large value (>100 chars or multi-line).
    /// Large values are moved to a separate collapsible section. Only applies to non-secret variables.
    /// </summary>
    public bool IsLargeValue { get; init; }
}

/// <summary>
/// Represents a CI trigger block configuration.
/// Related feature: docs/features/094-build-definition-tables/specification.md.
/// </summary>
public sealed class CiTriggerRowViewModel
{
    /// <summary>
    /// Gets the formatted use_yaml value.
    /// </summary>
    public required string UseYaml { get; init; }

    /// <summary>
    /// Gets the formatted override branch filters (comma-separated list or "-").
    /// </summary>
    public required string Override { get; init; }
}

/// <summary>
/// Represents a pull request trigger block configuration.
/// Related feature: docs/features/094-build-definition-tables/specification.md.
/// </summary>
public sealed class PullRequestTriggerRowViewModel
{
    /// <summary>
    /// Gets the formatted use_yaml value.
    /// </summary>
    public required string UseYaml { get; init; }

    /// <summary>
    /// Gets the formatted override branch filters (comma-separated list or "-").
    /// </summary>
    public required string Override { get; init; }

    /// <summary>
    /// Gets the formatted forks enabled status.
    /// </summary>
    public required string ForksEnabled { get; init; }

    /// <summary>
    /// Gets the formatted forks comment requirement.
    /// </summary>
    public required string ForksCommentRequirement { get; init; }
}

/// <summary>
/// Represents a schedule block configuration.
/// Related feature: docs/features/094-build-definition-tables/specification.md.
/// </summary>
public sealed class ScheduleRowViewModel
{
    /// <summary>
    /// Gets the formatted branch filters (comma-separated list or "-").
    /// </summary>
    public required string BranchFilters { get; init; }

    /// <summary>
    /// Gets the formatted days to build (comma-separated list).
    /// </summary>
    public required string DaysToBuild { get; init; }

    /// <summary>
    /// Gets the formatted schedule_only_with_changes status.
    /// </summary>
    public required string ScheduleOnlyWithChanges { get; init; }

    /// <summary>
    /// Gets the formatted start time (HH:MM format).
    /// </summary>
    public required string StartTime { get; init; }

    /// <summary>
    /// Gets the formatted time zone.
    /// </summary>
    public required string TimeZone { get; init; }
}

/// <summary>
/// Represents a repository block configuration.
/// Related feature: docs/features/094-build-definition-tables/specification.md.
/// </summary>
public sealed class RepositoryRowViewModel
{
    /// <summary>
    /// Gets the formatted repository type (TfsGit, GitHub, etc.).
    /// </summary>
    public required string RepoType { get; init; }

    /// <summary>
    /// Gets the formatted repository ID (GUID or identifier).
    /// </summary>
    public required string RepoId { get; init; }

    /// <summary>
    /// Gets the formatted branch name (e.g., refs/heads/master).
    /// </summary>
    public required string BranchName { get; init; }

    /// <summary>
    /// Gets the formatted YAML path (e.g., azure-pipelines.yml).
    /// </summary>
    public required string YmlPath { get; init; }

    /// <summary>
    /// Gets the formatted report_build_status value.
    /// </summary>
    public required string ReportBuildStatus { get; init; }

    /// <summary>
    /// Gets the formatted service connection ID (GUID or "-" if not used).
    /// </summary>
    public required string ServiceConnectionId { get; init; }

    /// <summary>
    /// Gets the formatted GitHub enterprise URL ("-" if not applicable).
    /// </summary>
    public required string GithubEnterpriseUrl { get; init; }
}

/// <summary>
/// Represents a job block configuration.
/// Related feature: docs/features/094-build-definition-tables/specification.md.
/// </summary>
public sealed class JobRowViewModel
{
    /// <summary>
    /// Gets the formatted job name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the formatted condition (e.g., succeeded(), always(), etc.).
    /// </summary>
    public required string Condition { get; init; }

    /// <summary>
    /// Gets the formatted timeout in minutes.
    /// </summary>
    public required string TimeoutInMinutes { get; init; }
}
