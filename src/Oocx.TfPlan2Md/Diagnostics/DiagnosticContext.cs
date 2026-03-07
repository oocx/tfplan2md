using System.Collections.Generic;

namespace Oocx.TfPlan2Md.Diagnostics;

/// <summary>
/// Collects diagnostic information during tfplan2md execution.
/// Related feature: docs/features/038-debug-output/.
/// </summary>
/// <remarks>
/// <para>
/// This class accumulates diagnostic state from various components during processing:
/// <list type="bullet">
/// <item><description>Principal mapping status (load success/failure, type counts, failed resolutions)</description></item>
/// <item><description>Template resolution decisions (which templates were used for each resource type)</description></item>
/// </list>
/// </para>
/// <para>
/// The diagnostic context is optional and only created when the --debug flag is enabled.
/// Components check for null before recording diagnostics, ensuring no performance impact
/// when debug mode is disabled.
/// </para>
/// <para>
/// Thread safety: This class is not thread-safe. It should only be accessed from a single
/// thread during report generation.
/// </para>
/// </remarks>
internal sealed class DiagnosticContext : IDiagnosticSink
{
    private readonly Dictionary<string, int> _principalTypeCount = new(StringComparer.Ordinal);
    private readonly List<FailedResolution> _failedResolutions = [];
    private readonly List<TemplateResolution> _templateResolutions = [];

    /// <summary>
    /// Gets a value indicating whether a principal mapping file was provided via CLI.
    /// </summary>
    public bool PrincipalMappingFileProvided { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the principal mapping file loaded successfully.
    /// </summary>
    /// <remarks>
    /// This is only meaningful when <see cref="PrincipalMappingFileProvided"/> is true.
    /// If the file was provided but failed to load (e.g., file not found, invalid JSON),
    /// this will be false.
    /// </remarks>
    public bool PrincipalMappingLoadedSuccessfully { get; private set; }

    /// <summary>
    /// Gets the path to the principal mapping file that was provided.
    /// </summary>
    /// <value>
    /// The file path if provided, otherwise null.
    /// </value>
    public string? PrincipalMappingFilePath { get; private set; }

    /// <summary>
    /// Gets whether the principal mapping file exists at the specified path.
    /// </summary>
    /// <remarks>
    /// This is only set when <see cref="PrincipalMappingLoadedSuccessfully"/> is false.
    /// It helps distinguish between file-not-found errors and other loading errors.
    /// Related to issue 042: Enhanced principal loading debug context.
    /// </remarks>
    public bool? PrincipalMappingFileExists { get; private set; }

    /// <summary>
    /// Gets whether the parent directory of the principal mapping file exists.
    /// </summary>
    /// <remarks>
    /// This is only set when <see cref="PrincipalMappingLoadedSuccessfully"/> is false.
    /// It helps diagnose Docker volume mount issues where the mount point doesn't exist.
    /// Related to issue 042: Enhanced principal loading debug context.
    /// </remarks>
    public bool? PrincipalMappingDirectoryExists { get; private set; }

    /// <summary>
    /// Gets the type of error that occurred when loading the principal mapping file.
    /// </summary>
    /// <remarks>
    /// This is only set when <see cref="PrincipalMappingLoadedSuccessfully"/> is false.
    /// The error type determines what troubleshooting guidance is shown to the user.
    /// Related to issue 042: Enhanced principal loading debug context.
    /// </remarks>
    public PrincipalLoadError? PrincipalMappingErrorType { get; private set; }

    /// <summary>
    /// Gets a user-friendly error message describing what went wrong.
    /// </summary>
    /// <remarks>
    /// This is only set when <see cref="PrincipalMappingLoadedSuccessfully"/> is false.
    /// The message should be clear and actionable, not just the raw exception message.
    /// Related to issue 042: Enhanced principal loading debug context.
    /// </remarks>
    public string? PrincipalMappingErrorMessage { get; private set; }

    /// <summary>
    /// Gets additional technical details about the error.
    /// </summary>
    /// <remarks>
    /// This is only set when <see cref="PrincipalMappingLoadedSuccessfully"/> is false.
    /// May include line/column numbers for JSON parse errors, exception details, etc.
    /// Related to issue 042: Enhanced principal loading debug context.
    /// </remarks>
    public string? PrincipalMappingErrorDetails { get; private set; }

    /// <summary>
    /// Gets the count of principals by type (e.g., "users", "groups", "service principals").
    /// </summary>
    /// <remarks>
    /// The dictionary key is the principal type name, and the value is the count of that type.
    /// Type detection is based on naming conventions or metadata in the mapping file.
    /// </remarks>
    public IReadOnlyDictionary<string, int> PrincipalTypeCount => _principalTypeCount;

    /// <summary>
    /// Gets the number of subscription mappings loaded from the mapping file.
    /// </summary>
    public int SubscriptionCount { get; private set; }

    /// <summary>
    /// Gets the number of management group mappings loaded from the mapping file.
    /// </summary>
    public int ManagementGroupCount { get; private set; }

    /// <summary>
    /// Gets the number of tenant mappings loaded from the mapping file.
    /// </summary>
    public int TenantCount { get; private set; }

    /// <summary>
    /// Gets the number of custom role mappings loaded from the mapping file.
    /// </summary>
    public int RoleCount { get; private set; }

    /// <summary>
    /// Gets the number of Azure DevOps user mappings loaded from the mapping file.
    /// </summary>
    /// <remarks>
    /// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
    /// </remarks>
    public int AzdoUserCount { get; private set; }

    /// <summary>
    /// Gets the number of Azure DevOps group mappings loaded from the mapping file.
    /// </summary>
    /// <remarks>
    /// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
    /// </remarks>
    public int AzdoGroupCount { get; private set; }

    /// <summary>
    /// Gets the number of Azure DevOps project mappings loaded from the mapping file.
    /// </summary>
    /// <remarks>
    /// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
    /// </remarks>
    public int AzdoProjectCount { get; private set; }

    /// <summary>
    /// Gets the number of Azure DevOps repository mappings loaded from the mapping file.
    /// </summary>
    /// <remarks>
    /// Related feature: docs/features/096-azdo-repo-mapping-and-icons/specification.md.
    /// </remarks>
    public int AzdoRepositoryCount { get; private set; }

    /// <summary>
    /// Gets the list of IDs that failed to resolve, along with the resource that referenced them.
    /// </summary>
    /// <remarks>
    /// Each entry captures a failed lookup with context about where it was referenced.
    /// The same ID may appear multiple times if referenced by different resources.
    /// </remarks>
    public IReadOnlyList<FailedResolution> FailedResolutions => _failedResolutions;

    /// <summary>
    /// Gets the list of template resolution decisions made during rendering.
    /// </summary>
    /// <remarks>
    /// Each entry records which rendering path was selected for a specific resource type.
    /// This helps users understand whether a built-in template mode, a resource-specific
    /// renderer, or the default renderer was used.
    /// </remarks>
    public IReadOnlyList<TemplateResolution> TemplateResolutions => _templateResolutions;

    /// <summary>
    /// Records that a principal mapping file was explicitly supplied.
    /// </summary>
    /// <param name="mappingFilePath">The user-supplied mapping file path.</param>
    public void RecordPrincipalMappingFileProvided(string mappingFilePath)
    {
        PrincipalMappingFileProvided = true;
        PrincipalMappingFilePath = mappingFilePath;
    }

    /// <summary>
    /// Records file-system status for the principal mapping input.
    /// </summary>
    /// <param name="fileExists">Whether the mapping file exists.</param>
    /// <param name="directoryExists">Whether the mapping file's parent directory exists.</param>
    public void RecordPrincipalMappingPathStatus(bool fileExists, bool directoryExists)
    {
        PrincipalMappingFileExists = fileExists;
        PrincipalMappingDirectoryExists = directoryExists;
    }

    /// <summary>
    /// Records a successful principal mapping load.
    /// </summary>
    public void RecordPrincipalMappingLoadedSuccessfully()
    {
        PrincipalMappingLoadedSuccessfully = true;
        PrincipalMappingErrorType = null;
        PrincipalMappingErrorMessage = null;
        PrincipalMappingErrorDetails = null;
    }

    /// <summary>
    /// Records a failed principal mapping load.
    /// </summary>
    /// <param name="errorType">The category of load error.</param>
    /// <param name="message">The user-facing error message.</param>
    /// <param name="details">Detailed technical error information.</param>
    public void RecordPrincipalMappingLoadFailure(
        PrincipalLoadError errorType,
        string message,
        string details)
    {
        PrincipalMappingLoadedSuccessfully = false;
        PrincipalMappingErrorType = errorType;
        PrincipalMappingErrorMessage = message;
        PrincipalMappingErrorDetails = details;
    }

    /// <summary>
    /// Records a principal type count.
    /// </summary>
    /// <param name="principalType">The principal type label.</param>
    /// <param name="count">The number of items for that type.</param>
    public void RecordPrincipalTypeCount(string principalType, int count)
    {
        if (count <= 0)
        {
            return;
        }

        _principalTypeCount[principalType] = count;
    }

    /// <summary>
    /// Records principal mapping entity counts.
    /// </summary>
    /// <param name="subscriptionCount">The number of mapped subscriptions.</param>
    /// <param name="managementGroupCount">The number of mapped management groups.</param>
    /// <param name="tenantCount">The number of mapped tenants.</param>
    /// <param name="roleCount">The number of mapped custom roles.</param>
    /// <param name="azdoUserCount">The number of mapped Azure DevOps users.</param>
    /// <param name="azdoGroupCount">The number of mapped Azure DevOps groups.</param>
    /// <param name="azdoProjectCount">The number of mapped Azure DevOps projects.</param>
    /// <param name="azdoRepositoryCount">The number of mapped Azure DevOps repositories.</param>
    public void RecordPrincipalEntityCounts(
        int subscriptionCount,
        int managementGroupCount,
        int tenantCount,
        int roleCount,
        int azdoUserCount,
        int azdoGroupCount,
        int azdoProjectCount,
        int azdoRepositoryCount)
    {
        SubscriptionCount = subscriptionCount;
        ManagementGroupCount = managementGroupCount;
        TenantCount = tenantCount;
        RoleCount = roleCount;
        AzdoUserCount = azdoUserCount;
        AzdoGroupCount = azdoGroupCount;
        AzdoProjectCount = azdoProjectCount;
        AzdoRepositoryCount = azdoRepositoryCount;
    }

    /// <summary>
    /// Records a failed resolution event.
    /// </summary>
    /// <param name="failure">The failed resolution to append.</param>
    public void RecordFailedResolution(FailedResolution failure)
    {
        _failedResolutions.Add(failure);
    }

    /// <summary>
    /// Records a template resolution event.
    /// </summary>
    /// <param name="resolution">The template resolution that occurred.</param>
    public void RecordTemplateResolution(TemplateResolution resolution)
    {
        _templateResolutions.Add(resolution);
    }

    /// <summary>
    /// Creates an immutable snapshot of the current diagnostic state.
    /// </summary>
    /// <returns>A formatter-ready snapshot of the current diagnostics.</returns>
    public DiagnosticReport CreateSnapshot()
    {
        return new DiagnosticReport(
            PrincipalMappingFileProvided,
            PrincipalMappingLoadedSuccessfully,
            PrincipalMappingFilePath,
            PrincipalMappingFileExists,
            PrincipalMappingDirectoryExists,
            PrincipalMappingErrorType,
            PrincipalMappingErrorMessage,
            PrincipalMappingErrorDetails,
            new Dictionary<string, int>(_principalTypeCount, StringComparer.Ordinal),
            SubscriptionCount,
            ManagementGroupCount,
            TenantCount,
            RoleCount,
            AzdoUserCount,
            AzdoGroupCount,
            AzdoProjectCount,
            AzdoRepositoryCount,
            _failedResolutions.ToArray(),
            _templateResolutions.ToArray());
    }
}
