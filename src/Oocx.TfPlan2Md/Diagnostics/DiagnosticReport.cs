using System.Collections.Generic;

namespace Oocx.TfPlan2Md.Diagnostics;

/// <summary>
/// Immutable snapshot of diagnostics collected during a single application run.
/// Related feature: docs/features/110-refactoring-opportunities/tasks.md.
/// </summary>
internal sealed record DiagnosticReport(
    bool PrincipalMappingFileProvided,
    bool PrincipalMappingLoadedSuccessfully,
    string? PrincipalMappingFilePath,
    bool? PrincipalMappingFileExists,
    bool? PrincipalMappingDirectoryExists,
    PrincipalLoadError? PrincipalMappingErrorType,
    string? PrincipalMappingErrorMessage,
    string? PrincipalMappingErrorDetails,
    IReadOnlyDictionary<string, int> PrincipalTypeCount,
    int SubscriptionCount,
    int ManagementGroupCount,
    int TenantCount,
    int RoleCount,
    int AzdoUserCount,
    int AzdoGroupCount,
    int AzdoProjectCount,
    int AzdoRepositoryCount,
    IReadOnlyList<FailedResolution> FailedResolutions,
    IReadOnlyList<TemplateResolution> TemplateResolutions);
