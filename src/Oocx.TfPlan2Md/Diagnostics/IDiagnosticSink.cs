namespace Oocx.TfPlan2Md.Diagnostics;

/// <summary>
/// Records typed diagnostic events and state during a single application run.
/// Related feature: docs/features/110-refactoring-opportunities/tasks.md.
/// </summary>
internal interface IDiagnosticSink
{
    /// <summary>
    /// Records that the user supplied a principal mapping file path.
    /// </summary>
    /// <param name="mappingFilePath">The supplied mapping file path.</param>
    void RecordPrincipalMappingFileProvided(string mappingFilePath);

    /// <summary>
    /// Records whether the mapping file and parent directory exist.
    /// </summary>
    /// <param name="fileExists">Whether the file exists.</param>
    /// <param name="directoryExists">Whether the parent directory exists.</param>
    void RecordPrincipalMappingPathStatus(bool fileExists, bool directoryExists);

    /// <summary>
    /// Records that the principal mapping file was loaded successfully.
    /// </summary>
    void RecordPrincipalMappingLoadedSuccessfully();

    /// <summary>
    /// Records that loading the principal mapping file failed.
    /// </summary>
    /// <param name="errorType">The category of load failure.</param>
    /// <param name="message">The user-facing message.</param>
    /// <param name="details">Detailed technical context.</param>
    void RecordPrincipalMappingLoadFailure(PrincipalLoadError errorType, string message, string details);

    /// <summary>
    /// Records a principal type count.
    /// </summary>
    /// <param name="principalType">The principal type label.</param>
    /// <param name="count">The number of principals of that type.</param>
    void RecordPrincipalTypeCount(string principalType, int count);

    /// <summary>
    /// Records entity counts loaded from the principal mapping file.
    /// </summary>
    /// <param name="subscriptionCount">The mapped subscription count.</param>
    /// <param name="managementGroupCount">The mapped management group count.</param>
    /// <param name="tenantCount">The mapped tenant count.</param>
    /// <param name="roleCount">The mapped custom role count.</param>
    /// <param name="azdoUserCount">The mapped Azure DevOps user count.</param>
    /// <param name="azdoGroupCount">The mapped Azure DevOps group count.</param>
    /// <param name="azdoProjectCount">The mapped Azure DevOps project count.</param>
    /// <param name="azdoRepositoryCount">The mapped Azure DevOps repository count.</param>
    void RecordPrincipalEntityCounts(
        int subscriptionCount,
        int managementGroupCount,
        int tenantCount,
        int roleCount,
        int azdoUserCount,
        int azdoGroupCount,
        int azdoProjectCount,
        int azdoRepositoryCount);

    /// <summary>
    /// Records a failed resolution event.
    /// </summary>
    /// <param name="failure">The failed resolution.</param>
    void RecordFailedResolution(FailedResolution failure);

    /// <summary>
    /// Records a template resolution event.
    /// </summary>
    /// <param name="resolution">The template resolution decision.</param>
    void RecordTemplateResolution(TemplateResolution resolution);
}
