using System;
using System.Collections.Generic;
using System.Linq;

namespace Oocx.TfPlan2Md.CodeAnalysis;

/// <summary>
/// Maps SARIF logical locations to Terraform resource addresses and attributes.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
internal static class ResourceMapper
{
    /// <summary>
    /// Maps a SARIF finding to one or more resource-scoped findings.
    /// </summary>
    /// <param name="finding">The SARIF finding to map.</param>
    /// <param name="severity">The derived severity for the finding.</param>
    /// <returns>The list of mapped findings.</returns>
    internal static IReadOnlyList<CodeAnalysisMappedFinding> MapFinding(CodeAnalysisFinding finding, CodeAnalysisSeverity severity)
    {
        if (finding.Locations.Count == 0)
        {
            return [CreateUnmappedFinding(finding, severity)];
        }

        return finding.Locations
            .Select(location => MapLocation(finding, severity, location))
            .ToList();
    }

    /// <summary>
    /// Maps a single SARIF logical location to a resource-scoped finding.
    /// </summary>
    /// <param name="finding">The source SARIF finding.</param>
    /// <param name="severity">The derived severity.</param>
    /// <param name="location">The logical location entry.</param>
    /// <returns>The mapped finding.</returns>
    private static CodeAnalysisMappedFinding MapLocation(
        CodeAnalysisFinding finding,
        CodeAnalysisSeverity severity,
        CodeAnalysisLocation location)
    {
        if (location.FullyQualifiedName is null)
        {
            return CreateUnmappedFinding(finding, severity);
        }

        if (TryMapLogicalLocation(location.FullyQualifiedName, out var mappedLocation))
        {
            return new CodeAnalysisMappedFinding
            {
                Source = finding,
                Severity = severity,
                ResourceAddress = mappedLocation.ResourceAddress,
                ModuleAddress = mappedLocation.ModuleAddress,
                AttributePath = mappedLocation.AttributePath
            };
        }

        return CreateUnmappedFinding(finding, severity);
    }

    /// <summary>
    /// Attempts to map a logical location string to a Terraform resource address and attribute path.
    /// </summary>
    /// <param name="fullyQualifiedName">The SARIF logical location fully qualified name.</param>
    /// <param name="location">The mapped resource location, when available.</param>
    /// <returns><c>true</c> when a resource address was mapped; otherwise <c>false</c>.</returns>
    internal static bool TryMapLogicalLocation(string fullyQualifiedName, out CodeAnalysisResourceLocation location)
    {
        location = new CodeAnalysisResourceLocation { ResourceAddress = fullyQualifiedName };

        if (string.IsNullOrWhiteSpace(fullyQualifiedName))
        {
            return false;
        }

        var tokens = fullyQualifiedName.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 2)
        {
            return false;
        }

        for (var start = 0; start < tokens.Length - 1; start++)
        {
            if (IsModuleToken(tokens[start]))
            {
                if (TryParseModuleAddress(tokens, start, out var parsed))
                {
                    location = parsed;
                    return true;
                }

                continue;
            }

            if (!IsResourceTypeToken(tokens[start]))
            {
                continue;
            }

            location = BuildLocation(tokens, start, start + 2, null);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates an unmapped finding instance for unmatched locations.
    /// </summary>
    /// <param name="finding">The source SARIF finding.</param>
    /// <param name="severity">The derived severity.</param>
    /// <returns>An unmapped finding entry.</returns>
    private static CodeAnalysisMappedFinding CreateUnmappedFinding(CodeAnalysisFinding finding, CodeAnalysisSeverity severity)
    {
        return new CodeAnalysisMappedFinding
        {
            Source = finding,
            Severity = severity,
            ResourceAddress = null,
            ModuleAddress = null,
            AttributePath = null
        };
    }

    /// <summary>
    /// Determines whether a token represents the module prefix in a Terraform address.
    /// </summary>
    /// <param name="token">The token to evaluate.</param>
    /// <returns><c>true</c> when the token is <c>module</c>; otherwise <c>false</c>.</returns>
    private static bool IsModuleToken(string token)
    {
        return token.Equals("module", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether a token represents a Terraform resource type segment.
    /// </summary>
    /// <param name="token">The token to evaluate.</param>
    /// <returns><c>true</c> when the token matches expected resource type patterns.</returns>
    private static bool IsResourceTypeToken(string token)
    {
        return token.Contains('_', StringComparison.Ordinal);
    }

    /// <summary>
    /// Attempts to parse a module-prefixed resource address.
    /// </summary>
    /// <param name="tokens">The tokenized fully qualified name.</param>
    /// <param name="start">The index where the module prefix starts.</param>
    /// <param name="location">The parsed resource location when successful.</param>
    /// <returns><c>true</c> when a module-prefixed resource address was parsed.</returns>
    private static bool TryParseModuleAddress(string[] tokens, int start, out CodeAnalysisResourceLocation location)
    {
        var index = start;
        while (index + 1 < tokens.Length && IsModuleToken(tokens[index]))
        {
            index += 2;
        }

        if (index + 1 >= tokens.Length)
        {
            location = new CodeAnalysisResourceLocation { ResourceAddress = string.Empty };
            return false;
        }

        if (!IsResourceTypeToken(tokens[index]))
        {
            location = new CodeAnalysisResourceLocation { ResourceAddress = string.Empty };
            return false;
        }

        var moduleAddress = string.Join('.', tokens[start..index]);
        location = BuildLocation(tokens, start, index + 2, moduleAddress);
        return true;
    }

    /// <summary>
    /// Builds a resource location from tokenized segments.
    /// </summary>
    /// <param name="tokens">The tokenized fully qualified name.</param>
    /// <param name="resourceStart">The start index of the resource address.</param>
    /// <param name="resourceEnd">The end index (exclusive) of the resource address.</param>
    /// <param name="moduleAddress">The module address when available.</param>
    /// <returns>The mapped resource location.</returns>
    private static CodeAnalysisResourceLocation BuildLocation(string[] tokens, int resourceStart, int resourceEnd, string? moduleAddress)
    {
        var resourceAddress = string.Join('.', tokens[resourceStart..resourceEnd]);
        var attributePath = resourceEnd < tokens.Length
            ? string.Join('.', tokens[resourceEnd..])
            : null;

        return new CodeAnalysisResourceLocation
        {
            ResourceAddress = resourceAddress,
            AttributePath = string.IsNullOrWhiteSpace(attributePath) ? null : attributePath,
            ModuleAddress = moduleAddress
        };
    }
}
