using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text.Json;
using Oocx.TfPlan2Md.Diagnostics;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Parses Azure mapping file content into structured mapping results.
/// </summary>
/// <remarks>
/// Related feature: docs/features/063-azure-display-enhancements/specification.md.
/// </remarks>
internal static class AzureMappingFileParser
{
    /// <summary>
    /// Attempts to parse mapping file content in nested or flat formats.
    /// </summary>
    /// <param name="content">The JSON file content.</param>
    /// <param name="diagnosticContext">Optional diagnostic context for tracking load results.</param>
    /// <returns>The parsed mapping file result, or null when parsing fails.</returns>
    public static AzureMappingFileResult? TryParse(string content, DiagnosticContext? diagnosticContext)
    {
        var nested = TryParseNested(content, diagnosticContext);
        if (nested is not null)
        {
            return nested;
        }

        return TryParseFlat(content, diagnosticContext);
    }

    /// <summary>
    /// Attempts to parse the mapping file in nested format.
    /// </summary>
    /// <param name="content">The JSON file content.</param>
    /// <param name="diagnosticContext">Optional diagnostic context for tracking load results.</param>
    /// <returns>The parsed mapping file result when nested parsing succeeds; otherwise null.</returns>
    private static AzureMappingFileResult? TryParseNested(string content, DiagnosticContext? diagnosticContext)
    {
        try
        {
            var nestedMapping = JsonSerializer.Deserialize(content, TfPlanJsonContext.Default.PrincipalMappingFile);
            if (nestedMapping == null)
            {
                return null;
            }

            if (!HasNestedSections(nestedMapping))
            {
                return null;
            }

            var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            AddPrincipalSection(names, types, nestedMapping.Users, "User");
            AddPrincipalSection(names, types, nestedMapping.Groups, "Group");
            AddPrincipalSection(names, types, nestedMapping.ServicePrincipals, "ServicePrincipal");

            RecordNestedDiagnostics(diagnosticContext, nestedMapping);

            return new AzureMappingFileResult(
                names.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),
                types.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),
                nestedMapping.Subscriptions ?? new List<MappingEntry>(),
                nestedMapping.ManagementGroups ?? new List<MappingEntry>(),
                nestedMapping.Tenants ?? new List<MappingEntry>(),
                nestedMapping.Roles ?? new List<MappingEntry>());
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to parse the mapping file in flat format.
    /// </summary>
    /// <param name="content">The JSON file content.</param>
    /// <param name="diagnosticContext">Optional diagnostic context for tracking load results.</param>
    /// <returns>The parsed mapping file result when flat parsing succeeds; otherwise null.</returns>
    private static AzureMappingFileResult? TryParseFlat(string content, DiagnosticContext? diagnosticContext)
    {
        var parsed = JsonSerializer.Deserialize(content, TfPlanJsonContext.Default.DictionaryStringString);
        if (parsed == null)
        {
            return null;
        }

        if (diagnosticContext != null)
        {
            diagnosticContext.PrincipalMappingLoadedSuccessfully = true;
            diagnosticContext.PrincipalTypeCount["principals"] = parsed.Count;
        }

        return new AzureMappingFileResult(
            parsed.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),
            FrozenDictionary<string, string>.Empty,
            Array.Empty<MappingEntry>(),
            Array.Empty<MappingEntry>(),
            Array.Empty<MappingEntry>(),
            Array.Empty<MappingEntry>());
    }

    /// <summary>
    /// Determines whether the nested mapping file contains any supported sections.
    /// </summary>
    /// <param name="nestedMapping">The parsed mapping file.</param>
    /// <returns><c>true</c> when at least one section is present; otherwise <c>false</c>.</returns>
    private static bool HasNestedSections(PrincipalMappingFile nestedMapping)
    {
        return nestedMapping.Users != null ||
               nestedMapping.Groups != null ||
               nestedMapping.ServicePrincipals != null ||
               nestedMapping.Subscriptions != null ||
               nestedMapping.ManagementGroups != null ||
               nestedMapping.Tenants != null ||
               nestedMapping.Roles != null;
    }

    /// <summary>
    /// Adds principal mappings for a specific section and captures type metadata.
    /// </summary>
    /// <param name="names">The destination dictionary for principal names.</param>
    /// <param name="types">The destination dictionary for principal types.</param>
    /// <param name="section">The source section of principal mappings.</param>
    /// <param name="typeLabel">The label to record for the principal type.</param>
    private static void AddPrincipalSection(
        Dictionary<string, string> names,
        Dictionary<string, string> types,
        IReadOnlyDictionary<string, string>? section,
        string typeLabel)
    {
        if (section == null)
        {
            return;
        }

        foreach (var (id, name) in section)
        {
            names[id] = name;
            types[id] = typeLabel;
        }
    }

    /// <summary>
    /// Records diagnostics for successfully parsed nested mapping files.
    /// </summary>
    /// <param name="diagnosticContext">Optional diagnostic context to update.</param>
    /// <param name="nestedMapping">The parsed mapping file.</param>
    private static void RecordNestedDiagnostics(DiagnosticContext? diagnosticContext, PrincipalMappingFile nestedMapping)
    {
        if (diagnosticContext == null)
        {
            return;
        }

        diagnosticContext.PrincipalMappingLoadedSuccessfully = true;

        AddPrincipalTypeCount(diagnosticContext, "users", nestedMapping.Users?.Count);
        AddPrincipalTypeCount(diagnosticContext, "groups", nestedMapping.Groups?.Count);
        AddPrincipalTypeCount(diagnosticContext, "servicePrincipals", nestedMapping.ServicePrincipals?.Count);

        diagnosticContext.SubscriptionCount = nestedMapping.Subscriptions?.Count ?? 0;
        diagnosticContext.ManagementGroupCount = nestedMapping.ManagementGroups?.Count ?? 0;
        diagnosticContext.TenantCount = nestedMapping.Tenants?.Count ?? 0;
        diagnosticContext.RoleCount = nestedMapping.Roles?.Count ?? 0;
    }

    /// <summary>
    /// Records principal type counts only when the section is present.
    /// </summary>
    /// <param name="diagnosticContext">The diagnostic context to update.</param>
    /// <param name="key">The principal type key.</param>
    /// <param name="count">The count of principals in the section.</param>
    private static void AddPrincipalTypeCount(DiagnosticContext diagnosticContext, string key, int? count)
    {
        if (!count.HasValue || count.Value == 0)
        {
            return;
        }

        diagnosticContext.PrincipalTypeCount[key] = count.Value;
    }
}
