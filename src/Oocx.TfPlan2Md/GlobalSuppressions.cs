using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.",
    Scope = "type",
    Target = "~T:Oocx.TfPlan2Md.Program")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.",
    Scope = "member",
    Target = "~M:Oocx.TfPlan2Md.Program.<Main>$")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.",
    Scope = "type",
    Target = "~T:Oocx.TfPlan2Md.Platforms.Azure.PrincipalMapper")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Baseline for docs/features/047-provider-code-separation/ - JSON source generation context.",
    Scope = "type",
    Target = "~T:Oocx.TfPlan2Md.Platforms.Azure.AzureRoleDefinitionsJsonContext")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "JSON source generation context - coupling is from generated code. Related feature: docs/features/116-azuread-app-role-assignment/.",
    Scope = "type",
    Target = "~T:Oocx.TfPlan2Md.Platforms.Azure.MicrosoftGraphAppRolesJsonContext")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1502:Avoid excessive complexity",
    Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.",
    Scope = "member",
    Target = "~M:Oocx.TfPlan2Md.Platforms.Azure.PrincipalMapper.LoadMappings(System.String," +
             "Oocx.TfPlan2Md.Diagnostics.DiagnosticContext)")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.",
    Scope = "member",
    Target = "~M:Oocx.TfPlan2Md.Platforms.Azure.PrincipalMapper.LoadMappings(System.String," +
             "Oocx.TfPlan2Md.Diagnostics.DiagnosticContext)")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1502:Avoid excessive complexity",
    Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.",
    Scope = "member",
    Target = "~M:Oocx.TfPlan2Md.CLI.CliParser.Parse(System.String[])")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1502:Avoid excessive complexity",
    Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.",
    Scope = "member",
    Target = "~M:Oocx.TfPlan2Md.Diagnostics.DiagnosticMarkdownFormatter.Format(Oocx.TfPlan2Md.Diagnostics.DiagnosticReport)")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.",
    Scope = "type",
    Target = "~T:Oocx.TfPlan2Md.Providers.AzureRM.Models.FirewallNetworkRuleCollectionViewModelFactory")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.",
    Scope = "type",
    Target = "~T:Oocx.TfPlan2Md.MarkdownGeneration.MarkdownRenderer")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/. Coupling reduced from 50 to 38 types via extracted helpers.",
    Scope = "type",
    Target = "~T:Oocx.TfPlan2Md.MarkdownGeneration.ReportModelBuilder")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.",
    Scope = "type",
    Target = "~T:Oocx.TfPlan2Md.MarkdownGeneration.Summaries.ResourceSummaryBuilder")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.",
    Scope = "type",
    Target = "~T:Oocx.TfPlan2Md.Providers.AzureRM.Models.NetworkSecurityGroupViewModelFactory")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.",
    Scope = "type",
    Target = "~T:Oocx.TfPlan2Md.Providers.AzureRM.Models.RoleAssignmentViewModelFactory")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1502:Avoid excessive complexity",
    Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.",
    Scope = "member",
    Target = "~M:Oocx.TfPlan2Md.Providers.AzureRM.Models.RoleAssignmentViewModelFactory.FormatRoleValue(" +
             "System.String,System.Text.Json.JsonElement," +
             "Oocx.TfPlan2Md.Platforms.Azure.ScopeInfo,Oocx.TfPlan2Md.Platforms.Azure.RoleInfo," +
             "Oocx.TfPlan2Md.Platforms.Azure.PrincipalInfo)")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.",
    Scope = "type",
    Target = "~T:Oocx.TfPlan2Md.MarkdownGeneration.MarkdownHelpers")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1502:Avoid excessive complexity",
    Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.",
    Scope = "member",
    Target = "~M:Oocx.TfPlan2Md.MarkdownGeneration.MarkdownHelpers.FormatAttributeValue(" +
             "System.String,System.String,System.String," +
             "Oocx.TfPlan2Md.MarkdownGeneration.MarkdownHelpers+ValueFormatContext)")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1502:Avoid excessive complexity",
    Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.",
    Scope = "member",
    Target = "~M:Oocx.TfPlan2Md.MarkdownGeneration.Summaries.ResourceSummaryBuilder.BuildCreateSummary(" +
             "Oocx.TfPlan2Md.MarkdownGeneration.ResourceChangeModel)")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.",
    Scope = "type",
    Target = "~T:Oocx.TfPlan2Md.Parsing.TfPlanJsonContext")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Partial class summary builder aggregates multiple resource type dispatches.",
    Scope = "type",
    Target = "~T:Oocx.TfPlan2Md.Providers.AzureAD.Models.AzureAdSummaryBuilder")]
