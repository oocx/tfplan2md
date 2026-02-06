using System;
using System.Text.RegularExpressions;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Defines regex-based matching rules for provider, resource, attribute, and value dimensions.
/// </summary>
/// <remarks>
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </remarks>
internal sealed class MatchPattern
{
    /// <summary>
    /// Weight used for provider name specificity.
    /// </summary>
    private const int ProviderPriority = 1;
    /// <summary>
    /// Weight used for resource type specificity.
    /// </summary>
    private const int ResourceTypePriority = 2;
    /// <summary>
    /// Weight used for attribute name specificity.
    /// </summary>
    private const int AttributeNamePriority = 4;
    /// <summary>
    /// Weight used for value specificity.
    /// </summary>
    private const int ValuePriority = 8;

    /// <summary>
    /// Timeout used to guard regex evaluations in pattern matching.
    /// </summary>
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="MatchPattern"/> class.
    /// </summary>
    /// <param name="providerPattern">Regex for provider names or null to match all.</param>
    /// <param name="resourceTypePattern">Regex for resource types or null to match all.</param>
    /// <param name="attributeNamePattern">Regex for attribute names or null to match all.</param>
    /// <param name="valuePattern">Regex for attribute values or null to match all.</param>
    public MatchPattern(
        string? providerPattern,
        string? resourceTypePattern,
        string? attributeNamePattern,
        string? valuePattern)
    {
        ProviderPattern = CompilePattern(providerPattern, "provider");
        ResourceTypePattern = CompilePattern(resourceTypePattern, "resource type");
        AttributeNamePattern = CompilePattern(attributeNamePattern, "attribute name");
        ValuePattern = CompilePattern(valuePattern, "value");

        Specificity = CountSpecificity();
        DimensionPriority = CalculateDimensionPriority();
    }

    /// <summary>
    /// Gets the compiled provider regex, or null when matching all providers.
    /// </summary>
    /// <value>The provider regex or null for wildcard matching.</value>
    public Regex? ProviderPattern { get; }

    /// <summary>
    /// Gets the compiled resource type regex, or null when matching all resource types.
    /// </summary>
    /// <value>The resource type regex or null for wildcard matching.</value>
    public Regex? ResourceTypePattern { get; }

    /// <summary>
    /// Gets the compiled attribute name regex, or null when matching all attributes.
    /// </summary>
    /// <value>The attribute name regex or null for wildcard matching.</value>
    public Regex? AttributeNamePattern { get; }

    /// <summary>
    /// Gets the compiled value regex, or null when matching all values.
    /// </summary>
    /// <value>The value regex or null for wildcard matching.</value>
    public Regex? ValuePattern { get; }

    /// <summary>
    /// Gets the specificity score based on the number of non-null patterns.
    /// </summary>
    /// <value>The count of non-null patterns across all dimensions.</value>
    public int Specificity { get; }

    /// <summary>
    /// Gets the dimension priority score used for tie-breaking between patterns.
    /// </summary>
    /// <value>The weighted priority score for non-null patterns.</value>
    public int DimensionPriority { get; }

    /// <summary>
    /// Determines whether the pattern matches the provided resolution context.
    /// </summary>
    /// <param name="context">The resolution context to evaluate.</param>
    /// <returns>True when all non-null patterns match; otherwise, false.</returns>
    public bool IsMatch(ServiceResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Matches(ProviderPattern, context.ProviderName)
            && Matches(ResourceTypePattern, context.ResourceType)
            && Matches(AttributeNamePattern, context.AttributeName)
            && Matches(ValuePattern, context.Value);
    }

    /// <summary>
    /// Compiles a regex pattern, throwing a registration exception when invalid.
    /// </summary>
    /// <param name="pattern">The regex pattern or null to disable matching.</param>
    /// <param name="patternName">The friendly name of the pattern for error messages.</param>
    /// <returns>The compiled regex or null when no pattern is supplied.</returns>
    private static Regex? CompilePattern(string? pattern, string patternName)
    {
        if (pattern is null)
        {
            return null;
        }

        try
        {
            return new Regex(
                pattern,
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
                RegexTimeout);
        }
        catch (ArgumentException ex)
        {
            throw new ServiceRegistrationException($"Invalid {patternName} regex pattern: '{pattern}'.", ex);
        }
    }

    /// <summary>
    /// Evaluates whether a value matches a regex pattern, honoring wildcard behavior.
    /// </summary>
    /// <param name="pattern">The compiled regex pattern or null for wildcard.</param>
    /// <param name="value">The value to match.</param>
    /// <returns>True when the value matches the pattern or the pattern is null.</returns>
    private static bool Matches(Regex? pattern, string? value)
    {
        if (pattern is null)
        {
            return true;
        }

        if (value is null)
        {
            return false;
        }

        return pattern.IsMatch(value);
    }

    /// <summary>
    /// Counts the number of non-null patterns to determine specificity.
    /// </summary>
    /// <returns>The specificity score.</returns>
    private int CountSpecificity()
    {
        var count = 0;
        if (ProviderPattern is not null)
        {
            count++;
        }

        if (ResourceTypePattern is not null)
        {
            count++;
        }

        if (AttributeNamePattern is not null)
        {
            count++;
        }

        if (ValuePattern is not null)
        {
            count++;
        }

        return count;
    }

    /// <summary>
    /// Calculates the dimension priority score used for tie-breaking.
    /// </summary>
    /// <returns>The dimension priority score.</returns>
    private int CalculateDimensionPriority()
    {
        var priority = 0;
        if (ProviderPattern is not null)
        {
            priority += ProviderPriority;
        }

        if (ResourceTypePattern is not null)
        {
            priority += ResourceTypePriority;
        }

        if (AttributeNamePattern is not null)
        {
            priority += AttributeNamePriority;
        }

        if (ValuePattern is not null)
        {
            priority += ValuePriority;
        }

        return priority;
    }
}
