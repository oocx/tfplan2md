using System.Collections.Generic;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests the pattern matching registry infrastructure.
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </summary>
public class PatternMatchingRegistryTests
{
    /// <summary>
    /// Ensures pattern specificity and dimension priority are calculated from non-null patterns.
    /// </summary>
    [Test]
    public void MatchPattern_CalculatesSpecificityAndPriority()
    {
        var pattern = new MatchPattern("^azurerm$", null, null, "^prod$");

        pattern.Specificity.Should().Be(2);
        pattern.DimensionPriority.Should().Be(9);
    }

    /// <summary>
    /// Ensures regex patterns for all dimensions resolve matching services.
    /// </summary>
    [Test]
    public void PatternMatchingRegistry_RegexMatching_ResolvesMatchingServices()
    {
        var registry = new PatternMatchingRegistry<string>();
        registry.Register(new MatchPattern("^azurerm$", "^azurerm_.*$", "^name$", "^prod-.*$"), "match");

        var context = new ServiceResolutionContext("azurerm", "azurerm_resource_group", "name", "prod-core");
        var results = registry.ResolveAll(context);

        results.Should().Equal(new List<string> { "match" });
    }

    /// <summary>
    /// Ensures null patterns act as wildcards for all dimensions.
    /// </summary>
    [Test]
    public void PatternMatchingRegistry_NullPatterns_MatchAll()
    {
        var registry = new PatternMatchingRegistry<string>();
        registry.Register(new MatchPattern(null, null, null, null), "wildcard");

        var context = new ServiceResolutionContext("any", "resource", "attribute", "value");
        var results = registry.ResolveAll(context);

        results.Should().Equal(new List<string> { "wildcard" });
    }

    /// <summary>
    /// Ensures resolution ordering uses specificity, then dimension priority, then registration order.
    /// </summary>
    [Test]
    public void PatternMatchingRegistry_SpecificityResolution_OrdersBySpecificityDimensionAndRegistration()
    {
        var registry = new PatternMatchingRegistry<string>();
        registry.Register(new MatchPattern("^azurerm$", null, null, null), "provider-only");
        registry.Register(new MatchPattern("^azurerm$", null, "^name$", null), "provider-attribute");
        registry.Register(new MatchPattern("^azurerm$", null, null, "^prod$"), "provider-value");
        registry.Register(new MatchPattern("^azurerm$", "^azurerm_resource_group$", "^name$", null), "provider-resource-attribute");
        registry.Register(new MatchPattern("^azurerm$", "^azurerm_resource_group$", "^name$", null), "provider-resource-attribute-2");

        var context = new ServiceResolutionContext("azurerm", "azurerm_resource_group", "name", "prod");
        var results = registry.ResolveAll(context);

        results.Should().Equal(new List<string>
        {
            "provider-resource-attribute",
            "provider-resource-attribute-2",
            "provider-value",
            "provider-attribute",
            "provider-only"
        });
    }
}
