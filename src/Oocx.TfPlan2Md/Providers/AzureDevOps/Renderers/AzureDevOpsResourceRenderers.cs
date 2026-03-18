using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.Providers.AzureDevOps.Models;
using Oocx.TfPlan2Md.RenderTargets;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps.Renderers;

/// <summary>
/// Base class for Azure DevOps resource renderers that currently delegate to the default renderer.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal class AzureDevOpsDelegatingRenderer(string resourceType) : IResourceRenderer
{
    /// <summary>
    /// Default fallback renderer.
    /// </summary>
    private readonly DefaultResourceRenderer _defaultRenderer = new();

    /// <inheritdoc />
    public string ResourceType { get; } = resourceType;

    /// <inheritdoc />
    public virtual void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        _defaultRenderer.Render(writer, change, context);
    }
}

/// <summary>
/// Renders <c>azuredevops_variable_group</c> resources using structured tables.
/// Masks secret variable values to prevent sensitive data exposure in reports.
/// Related feature: docs/features/039-azdo-variable-group-template/specification.md.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal sealed class VariableGroupRenderer : AzureDevOpsDelegatingRenderer
{
    /// <summary>
    /// Shared details block style matching all other structured renderers.
    /// </summary>
    private const string DetailsStyle = " style=\"margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;\"";

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableGroupRenderer"/> class.
    /// </summary>
    public VariableGroupRenderer()
        : base("azuredevops_variable_group")
    {
    }

    /// <inheritdoc />
    public override void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        // Fall back to default renderer when raw parsing data is unavailable.
        if (change.ResourceChange is null)
        {
            base.Render(writer, change, context);
            return;
        }

        var largeValueFormat = ReportModelBuilder.ConvertRenderTargetToLargeValueFormat(context.RenderTarget);
        var viewModel = VariableGroupViewModelFactory.Build(change.ResourceChange, change.ProviderName, largeValueFormat);

        var detailsTag = context.DetailsDisplayMode switch
        {
            DetailsDisplayMode.Open => "<details open",
            DetailsDisplayMode.Closed => "<details",
            _ => change.CodeAnalysisFindings.Count > 0 ? "<details open" : "<details"
        };

        var summary = change.SummaryHtml
            ?? $"{change.ActionSymbol}\u00A0{MarkdownHelpers.EscapeMarkdown(change.Type)} <b>{MarkdownHelpers.FormatCodeSummary(change.Name)}</b>";

        writer.Raw(detailsTag + DetailsStyle + ">\n");
        writer.Raw("<summary>");
        writer.Raw(summary);
        writer.Raw("</summary>\n");
        writer.Raw("<br>\n\n");

        if (!string.IsNullOrWhiteSpace(viewModel.Name))
        {
            writer.Paragraph($"**Variable Group:** <code>{MarkdownHelpers.EscapeMarkdown(viewModel.Name)}</code>");
        }

        writer.BlankLine();

        if (!string.IsNullOrWhiteSpace(viewModel.Description))
        {
            writer.Paragraph($"**Description:** <code>{MarkdownHelpers.EscapeMarkdown(viewModel.Description)}</code>");
        }

        if (viewModel.KeyVaultBlocks.Count > 0)
        {
            writer.Heading("Key Vault Integration", 4);
            writer.BlankLine();
            writer.TableHeader("Name", "Service Endpoint ID", "Search Depth");
            foreach (var kv in viewModel.KeyVaultBlocks)
            {
                writer.TableRow([kv.Name, kv.ServiceEndpointId, kv.SearchDepth]);
            }

            writer.BlankLine();
        }

        // Render variables using the appropriate table format based on the action.
        if (viewModel.VariableChanges.Count > 0)
        {
            RenderVariableChangesTable(writer, viewModel.VariableChanges);
        }
        else if (viewModel.AfterVariables.Count > 0)
        {
            RenderVariablesTable(writer, viewModel.AfterVariables, heading: "Variables");
        }
        else if (viewModel.BeforeVariables.Count > 0)
        {
            RenderVariablesTable(writer, viewModel.BeforeVariables, heading: "Variables (being deleted)");
        }

        writer.DetailsClose();
        writer.BlankLine();
    }

    /// <summary>
    /// Renders a create or delete variable table with no change column.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="variables">Variable rows to render.</param>
    /// <param name="heading">Section heading text.</param>
    private static void RenderVariablesTable(MarkdownWriter writer, IReadOnlyList<VariableRowViewModel> variables, string heading)
    {
        writer.Heading(heading, 4);
        writer.BlankLine();
        writer.Raw("| Name | Value | Enabled | Content Type | Expires |\n");
        writer.Raw("| ---- | ----- | ------- | ------------ | ------- |\n");
        foreach (var v in variables)
        {
            writer.TableRow([v.Name, v.Value, v.Enabled, v.ContentType, v.Expires]);
        }

        writer.BlankLine();
    }

    /// <summary>
    /// Renders an update/replace variable change table with a change indicator column.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="changes">Variable change rows to render.</param>
    private static void RenderVariableChangesTable(MarkdownWriter writer, IReadOnlyList<VariableChangeRowViewModel> changes)
    {
        writer.Heading("Variables", 4);
        writer.BlankLine();
        writer.Raw("| Change | Name | Value | Enabled | Content Type | Expires |\n");
        writer.Raw("| ------ | ---- | ----- | ------- | ------------ | ------- |\n");
        foreach (var vc in changes)
        {
            writer.TableRow([vc.ChangeIcon, vc.Name, vc.Value, vc.Enabled, vc.ContentType, vc.Expires]);
        }

        writer.BlankLine();
    }
}

/// <summary>
/// Renders <c>azuredevops_build_definition</c> resources using structured tables.
/// Reads variable data directly from before/after JSON via <see cref="BuildDefinitionViewModelFactory"/>,
/// ensuring that only the <c>value</c>/<c>secret_value</c> field is masked for secret variables.
/// This fixes the bug where the <see cref="DefaultResourceRenderer"/> would propagate sensitivity
/// hierarchically and mark all variable attributes (name, is_secret, allow_override) as "(sensitive)".
/// Related feature: docs/features/094-build-definition-tables/specification.md.
/// Related issue: docs/issues/118-build-definition-variable-rendering/analysis.md.
/// </summary>
[SuppressMessage("Design", "CA1506:Avoid excessive class coupling", Justification = "Renderer orchestrates multiple view model sections and table formats by design.")]
internal sealed class BuildDefinitionRenderer : AzureDevOpsDelegatingRenderer
{
    /// <summary>
    /// Shared details block style matching all other structured renderers.
    /// </summary>
    private const string DetailsStyle = " style=\"margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;\"";

    /// <summary>
    /// Optional mapper for resolving repository display names.
    /// Related feature: docs/features/096-azdo-repo-mapping-and-icons/specification.md.
    /// </summary>
    private readonly AzdoRepositoryMapper? _repositoryMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildDefinitionRenderer"/> class.
    /// </summary>
    /// <param name="repositoryMapper">Optional mapper for Azure DevOps repository display names.</param>
    public BuildDefinitionRenderer(AzdoRepositoryMapper? repositoryMapper = null)
        : base("azuredevops_build_definition")
    {
        _repositoryMapper = repositoryMapper;
    }

    /// <inheritdoc />
    public override void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        // Fall back to default renderer when raw parsing data is unavailable.
        if (change.ResourceChange is null)
        {
            base.Render(writer, change, context);
            return;
        }

        var largeValueFormat = ReportModelBuilder.ConvertRenderTargetToLargeValueFormat(context.RenderTarget);
        var viewModel = BuildDefinitionViewModelFactory.Build(change.ResourceChange, change.ProviderName, largeValueFormat, _repositoryMapper);

        RenderHeader(writer, change, context, viewModel);
        RenderVariableSection(writer, viewModel);
        RenderSupplementarySections(writer, viewModel);

        writer.DetailsClose();
        writer.BlankLine();
    }

    /// <summary>
    /// Renders the details/summary header block and build definition metadata fields.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="change">The resource change model.</param>
    /// <param name="context">The render context.</param>
    /// <param name="viewModel">The populated view model.</param>
    private static void RenderHeader(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context, BuildDefinitionViewModel viewModel)
    {
        var detailsTag = context.DetailsDisplayMode switch
        {
            DetailsDisplayMode.Open => "<details open",
            DetailsDisplayMode.Closed => "<details",
            _ => change.CodeAnalysisFindings.Count > 0 ? "<details open" : "<details"
        };

        var summary = change.SummaryHtml
            ?? $"{change.ActionSymbol}\u00A0{MarkdownHelpers.EscapeMarkdown(change.Type)} <b>{MarkdownHelpers.FormatCodeSummary(change.Name)}</b>";

        writer.Raw(detailsTag + DetailsStyle + ">\n");
        writer.Raw("<summary>");
        writer.Raw(summary);
        writer.Raw("</summary>\n");
        writer.Raw("<br>\n\n");

        if (!string.IsNullOrWhiteSpace(viewModel.Name))
        {
            writer.Paragraph($"**Build Definition:** <code>{MarkdownHelpers.EscapeMarkdown(viewModel.Name)}</code>");
        }

        if (!string.IsNullOrWhiteSpace(viewModel.Path))
        {
            writer.Paragraph($"**Path:** <code>{MarkdownHelpers.EscapeMarkdown(viewModel.Path)}</code>");
        }

        if (!string.IsNullOrWhiteSpace(viewModel.AgentPoolName))
        {
            writer.Paragraph($"**Agent Pool:** <code>{MarkdownHelpers.EscapeMarkdown(viewModel.AgentPoolName)}</code>");
        }

        if (!string.IsNullOrWhiteSpace(viewModel.QueueStatus))
        {
            writer.Paragraph($"**Queue Status:** <code>{MarkdownHelpers.EscapeMarkdown(viewModel.QueueStatus)}</code>");
        }

        writer.BlankLine();
    }

    /// <summary>
    /// Renders the variable section using the appropriate table format based on the action type.
    /// For update/replace operations, shows a change-delta table; for create/delete, shows a flat table.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="viewModel">The populated view model.</param>
    private static void RenderVariableSection(MarkdownWriter writer, BuildDefinitionViewModel viewModel)
    {
        if (viewModel.VariableChanges.Count > 0)
        {
            RenderVariableChangesTable(writer, viewModel.VariableChanges);
        }
        else if (viewModel.AfterVariables.Count > 0)
        {
            RenderVariablesTable(writer, viewModel.AfterVariables, heading: "Variables");
        }
        else if (viewModel.BeforeVariables.Count > 0)
        {
            RenderVariablesTable(writer, viewModel.BeforeVariables, heading: "Variables (being deleted)");
        }
    }

    /// <summary>
    /// Renders optional supplementary sections: CI triggers, PR triggers, schedules, repository, and jobs.
    /// Each section only appears if the view model contains data for it.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="viewModel">The populated view model.</param>
    private static void RenderSupplementarySections(MarkdownWriter writer, BuildDefinitionViewModel viewModel)
    {
        // Render CI triggers.
        if (viewModel.AfterCiTriggers.Count > 0)
        {
            RenderCiTriggersTable(writer, viewModel.AfterCiTriggers, heading: "CI Triggers");
        }
        else if (viewModel.BeforeCiTriggers.Count > 0)
        {
            RenderCiTriggersTable(writer, viewModel.BeforeCiTriggers, heading: "CI Triggers (being deleted)");
        }

        // Render PR triggers.
        if (viewModel.AfterPullRequestTriggers.Count > 0)
        {
            RenderPullRequestTriggersTable(writer, viewModel.AfterPullRequestTriggers, heading: "Pull Request Triggers");
        }
        else if (viewModel.BeforePullRequestTriggers.Count > 0)
        {
            RenderPullRequestTriggersTable(writer, viewModel.BeforePullRequestTriggers, heading: "Pull Request Triggers (being deleted)");
        }

        // Render schedules.
        if (viewModel.AfterSchedules.Count > 0)
        {
            RenderSchedulesTable(writer, viewModel.AfterSchedules, heading: "Schedules");
        }
        else if (viewModel.BeforeSchedules.Count > 0)
        {
            RenderSchedulesTable(writer, viewModel.BeforeSchedules, heading: "Schedules (being deleted)");
        }

        // Render repository configuration.
        if (viewModel.AfterRepositories.Count > 0)
        {
            RenderRepositoriesTable(writer, viewModel.AfterRepositories, heading: "Repository");
        }
        else if (viewModel.BeforeRepositories.Count > 0)
        {
            RenderRepositoriesTable(writer, viewModel.BeforeRepositories, heading: "Repository (being deleted)");
        }

        // Render jobs (typically empty for YAML pipelines).
        if (viewModel.AfterJobs.Count > 0)
        {
            RenderJobsTable(writer, viewModel.AfterJobs, heading: "Jobs");
        }
        else if (viewModel.BeforeJobs.Count > 0)
        {
            RenderJobsTable(writer, viewModel.BeforeJobs, heading: "Jobs (being deleted)");
        }
    }

    /// <summary>
    /// Renders a create or delete variable table with columns: Name, Value, Secret, Allow Override.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="variables">Variable rows to render.</param>
    /// <param name="heading">Section heading text.</param>
    private static void RenderVariablesTable(MarkdownWriter writer, IReadOnlyList<BuildDefinitionVariableRowViewModel> variables, string heading)
    {
        writer.Heading(heading, 4);
        writer.BlankLine();
        writer.Raw("| Name | Value | Secret | Allow Override |\n");
        writer.Raw("| ---- | ----- | ------ | -------------- |\n");
        foreach (var v in variables)
        {
            writer.TableRow([v.Name, v.Value, v.IsSecret, v.AllowOverride]);
        }

        writer.BlankLine();
    }

    /// <summary>
    /// Renders an update/replace variable change table with columns: Change, Name, Value, Secret, Allow Override.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="changes">Variable change rows to render.</param>
    private static void RenderVariableChangesTable(MarkdownWriter writer, IReadOnlyList<BuildDefinitionVariableChangeRowViewModel> changes)
    {
        writer.Heading("Variables", 4);
        writer.BlankLine();
        writer.Raw("| Change | Name | Value | Secret | Allow Override |\n");
        writer.Raw("| ------ | ---- | ----- | ------ | -------------- |\n");
        foreach (var vc in changes)
        {
            writer.TableRow([vc.ChangeIcon, vc.Name, vc.Value, vc.IsSecret, vc.AllowOverride]);
        }

        writer.BlankLine();
    }

    /// <summary>
    /// Renders a CI triggers table.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="triggers">CI trigger rows to render.</param>
    /// <param name="heading">Section heading text.</param>
    private static void RenderCiTriggersTable(MarkdownWriter writer, IReadOnlyList<CiTriggerRowViewModel> triggers, string heading)
    {
        writer.Heading(heading, 4);
        writer.BlankLine();
        writer.Raw("| Use YAML | Override Branch Filters |\n");
        writer.Raw("| -------- | ----------------------- |\n");
        foreach (var t in triggers)
        {
            writer.TableRow([t.UseYaml, t.Override]);
        }

        writer.BlankLine();
    }

    /// <summary>
    /// Renders a pull request triggers table.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="triggers">Pull request trigger rows to render.</param>
    /// <param name="heading">Section heading text.</param>
    private static void RenderPullRequestTriggersTable(MarkdownWriter writer, IReadOnlyList<PullRequestTriggerRowViewModel> triggers, string heading)
    {
        writer.Heading(heading, 4);
        writer.BlankLine();
        writer.Raw("| Use YAML | Override Branch Filters | Forks Enabled | Forks Comment Requirement |\n");
        writer.Raw("| -------- | ----------------------- | ------------- | ------------------------- |\n");
        foreach (var t in triggers)
        {
            writer.TableRow([t.UseYaml, t.Override, t.ForksEnabled, t.ForksCommentRequirement]);
        }

        writer.BlankLine();
    }

    /// <summary>
    /// Renders a schedules table.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="schedules">Schedule rows to render.</param>
    /// <param name="heading">Section heading text.</param>
    private static void RenderSchedulesTable(MarkdownWriter writer, IReadOnlyList<ScheduleRowViewModel> schedules, string heading)
    {
        writer.Heading(heading, 4);
        writer.BlankLine();
        writer.Raw("| Branch Filters | Days | Only With Changes | Start Time | Time Zone |\n");
        writer.Raw("| -------------- | ---- | ----------------- | ---------- | --------- |\n");
        foreach (var s in schedules)
        {
            writer.TableRow([s.BranchFilters, s.DaysToBuild, s.ScheduleOnlyWithChanges, s.StartTime, s.TimeZone]);
        }

        writer.BlankLine();
    }

    /// <summary>
    /// Renders a repository configuration table.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="repositories">Repository rows to render.</param>
    /// <param name="heading">Section heading text.</param>
    private static void RenderRepositoriesTable(MarkdownWriter writer, IReadOnlyList<RepositoryRowViewModel> repositories, string heading)
    {
        writer.Heading(heading, 4);
        writer.BlankLine();
        writer.Raw("| Type | Repo ID | Branch | YAML Path | Report Status | Service Connection |\n");
        writer.Raw("| ---- | ------- | ------ | --------- | ------------- | ------------------ |\n");
        foreach (var r in repositories)
        {
            writer.TableRow([r.RepoType, r.RepoId, r.BranchName, r.YmlPath, r.ReportBuildStatus, r.ServiceConnectionId]);
        }

        writer.BlankLine();
    }

    /// <summary>
    /// Renders a jobs table.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="jobs">Job rows to render.</param>
    /// <param name="heading">Section heading text.</param>
    private static void RenderJobsTable(MarkdownWriter writer, IReadOnlyList<JobRowViewModel> jobs, string heading)
    {
        writer.Heading(heading, 4);
        writer.BlankLine();
        writer.Raw("| Name | Condition | Timeout (min) |\n");
        writer.Raw("| ---- | --------- | ------------- |\n");
        foreach (var j in jobs)
        {
            writer.TableRow([j.Name, j.Condition, j.TimeoutInMinutes]);
        }

        writer.BlankLine();
    }
}

