using System.Collections.Generic;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureRM;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzureRM;

/// <summary>
/// Tests for formatting principal identifiers using mapping data.
/// </summary>
public class PrincipalIdFormatterTests
{
    /// <summary>
    /// Verifies mapped principal ids render as display names.
    /// </summary>
    [Test]
    public void TryFormat_WhenPrincipalMapped_ReturnsDisplayName()
    {
        var principalMapper = new PrincipalMapper(
            new Dictionary<string, string> { ["user-123"] = "Jane Doe" },
            new Dictionary<string, string>());
        var formatter = new PrincipalIdFormatter(principalMapper);
        var context = new ServiceResolutionContext("azurerm", null, "principal_id", "user-123");

        var formatted = formatter.TryFormat(context);

        formatted.Should().Be("`👤\u00a0Jane Doe (user-123)`");
    }

    /// <summary>
    /// Verifies unmapped principal ids return null to allow fallback rendering.
    /// </summary>
    [Test]
    public void TryFormat_WhenPrincipalUnmapped_ReturnsNull()
    {
        var principalMapper = new PrincipalMapper(
            new Dictionary<string, string>(),
            new Dictionary<string, string>());
        var formatter = new PrincipalIdFormatter(principalMapper);
        var context = new ServiceResolutionContext("azurerm", null, "principal_id", "user-999");

        var formatted = formatter.TryFormat(context);

        formatted.Should().BeNull();
    }
}
