using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for inline diff rendering in parent-child resource tables.
/// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
/// </summary>
/// <remarks>
/// Inline diffs appear in child resource tables (e.g., subnet address prefixes, NSG rule ports).
/// These diffs must use plain markdown format (- old / + new) NOT HTML.
/// GitHub and Azure DevOps markdown renderers handle coloring automatically.
/// </remarks>
[Category("Unit")]
public class ParentChildInlineDiffTests
{
    /// <summary>
    /// Verifies that FormatDiff with "inline-diff" format produces rich HTML with character-level diffs.
    /// </summary>
    [Test]
    public void FormatDiff_InlineDiff_ProducesRichHtmlWithCharacterLevelDiffs()
    {
        // Arrange
        var before = "10.200.2.0/24";
        var after = "10.200.2.0/23";

        // Act
        var result = ScribanHelpers.FormatDiff(before, after, "inline-diff");

        // Assert - Should contain rich HTML with styling
        result.Should().Contain("<code style=\"display:block; white-space:normal; padding:0; margin:0;\">");
        result.Should().Contain("<span style=");
        result.Should().Contain("background-color:");
        result.Should().Contain("border-left:");

        // Should contain diff markers within spans
        result.Should().Contain("- 10.200.2.0/2");
        result.Should().Contain("+ 10.200.2.0/2");

        // Should contain character-level highlighting
        result.Should().Contain("#ffc0c0"); // Red highlight for removed char
        result.Should().Contain("#acf2bd"); // Green highlight for added char
        result.Should().Contain("<br>"); // Line separator
    }

    /// <summary>
    /// Verifies that FormatDiff with "inline-diff" uses - and + prefixes for changes.
    /// </summary>
    [Test]
    public void FormatDiff_InlineDiff_UsesPrefixesForChanges()
    {
        // Arrange
        var before = "old value";
        var after = "new value";

        // Act
        var result = ScribanHelpers.FormatDiff(before, after, "inline-diff");

        // Assert - Should use styled spans with +/- prefixes
        result.Should().Contain("<code style=\"display:block;");
        result.Should().Contain("- "); // Minus prefix
        result.Should().Contain("+ "); // Plus prefix
        result.Should().Contain("old"); // Contains before text
        result.Should().Contain("new"); // Contains after text

        // Should have character-level highlighting
        result.Should().Contain("background-color: #ffc0c0"); // Red for removed
        result.Should().Contain("background-color: #acf2bd"); // Green for added
    }

    /// <summary>
    /// Verifies that simple-diff format also produces plain markdown.
    /// </summary>
    [Test]
    public void FormatDiff_SimpleDiff_ProducesPlainMarkdownWithoutHtmlStyles()
    {
        // Arrange
        var before = "VirtualAppliance";
        var after = "VnetLocal";

        // Act
        var result = ScribanHelpers.FormatDiff(before, after, "simple-diff");

        // Assert - Should NOT contain HTML style attributes
        result.Should().NotContain("<span style=");
        result.Should().NotContain("background-color:");
        result.Should().NotContain("border-left:");

        // Should contain markdown diff markers
        result.Should().Contain("- ");
        result.Should().Contain("+ ");
        result.Should().Contain("<br>"); // newline is used for line breaks in tables
    }

    /// <summary>
    /// Verifies that diffs work correctly for VNet subnet address prefixes.
    /// </summary>
    [Test]
    public void FormatDiff_InlineDiff_VNetSubnetAddressPrefixes()
    {
        // Arrange - Subnet address prefix changing from /24 to /23
        var before = "10.200.2.0/24";
        var after = "10.200.2.0/23";

        // Act
        var result = ScribanHelpers.FormatDiff(before, after, "inline-diff");

        // Assert - Should contain rich HTML with character-level highlighting
        result.Should().Contain("<code style=\"display:block;");
        result.Should().Contain("<span style=");
        result.Should().Contain("background-color:");
        result.Should().Contain("10.200.2.0/2"); // Common prefix

        // Should highlight changed character (4 vs 3)
        result.Should().Contain("background-color: #ffc0c0"); // Red for "4"
        result.Should().Contain("background-color: #acf2bd"); // Green for "3"
    }

    /// <summary>
    /// Verifies that diffs work correctly for route table next hop types.
    /// </summary>
    [Test]
    public void FormatDiff_InlineDiff_RouteTableNextHopType()
    {
        // Arrange - Route next hop type changing from VirtualAppliance to VnetLocal
        var before = "VirtualAppliance";
        var after = "VnetLocal";

        // Act
        var result = ScribanHelpers.FormatDiff(before, after, "inline-diff");

        // Assert - Should contain rich HTML with character-level highlighting
        result.Should().Contain("<code style=\"display:block;");
        result.Should().Contain("<span style=");
        result.Should().Contain("background-color:");

        // Should show both values with character-level diffs
        result.Should().Contain("- V"); // Start of before value
        result.Should().Contain("+ V"); // Start of after value
        result.Should().Contain("Appliance"); // Unique to before
        result.Should().Contain("Loc"); // Part of "Local" in after

        // Should highlight changed portions
        result.Should().Contain("background-color: #ffc0c0"); // Red for removed chars
        result.Should().Contain("background-color: #acf2bd"); // Green for added chars
    }

    /// <summary>
    /// Verifies that diffs work correctly for NSG rule source addresses with emoji formatting.
    /// </summary>
    [Test]
    public void FormatDiff_InlineDiff_NsgRuleSourceAddresses()
    {
        // Arrange - NSG rule source changing from single IP to multiple IPs
        var before = "🌐 10.1.1.5";
        var after = "🌐 10.1.1.5, 🌐 10.1.1.6";

        // Act
        var result = ScribanHelpers.FormatDiff(before, after, "inline-diff");

        // Assert - Should contain rich HTML with character-level highlighting
        result.Should().Contain("<code style=\"display:block;");
        result.Should().Contain("<span style=");
        result.Should().Contain("10.1.1.5"); // First IP
        result.Should().Contain("10.1.1.6"); // Second IP
        result.Should().Contain("🌐"); // Emoji preserved

        // Should highlight the addition (second IP with emoji)
        result.Should().Contain("background-color: #acf2bd"); // Green for added content
    }

    /// <summary>
    /// Verifies that diffs work correctly for NSG rule destination ports.
    /// </summary>
    [Test]
    public void FormatDiff_InlineDiff_NsgRuleDestinationPorts()
    {
        // Arrange - NSG rule ports changing from single port to multiple ports
        var before = "🔌 8443";
        var after = "🔌 8443, 🔌 9443";

        // Act
        var result = ScribanHelpers.FormatDiff(before, after, "inline-diff");

        // Assert - Should contain rich HTML with character-level highlighting
        result.Should().Contain("<code style=\"display:block;");
        result.Should().Contain("<span style=");
        result.Should().Contain("border-left:");
        result.Should().Contain("8443"); // First port
        result.Should().Contain("9443"); // Second port
        result.Should().Contain("🔌"); // Emoji preserved

        // Should highlight the addition (second port)
        result.Should().Contain("background-color: #acf2bd"); // Green for added content
    }

    /// <summary>
    /// Verifies that diffs work correctly for DNS record values.
    /// </summary>
    [Test]
    public void FormatDiff_InlineDiff_DnsRecordValue()
    {
        // Arrange - DNS A record IP address changing
        var before = "🌐 10.1.1.10";
        var after = "🌐 10.1.1.20";

        // Act
        var result = ScribanHelpers.FormatDiff(before, after, "inline-diff");

        // Assert - Should contain rich HTML with character-level highlighting
        result.Should().Contain("<code style=\"display:block;");
        result.Should().Contain("<span style=");
        result.Should().Contain("10.1.1."); // Common prefix
        result.Should().Contain("🌐"); // Emoji preserved

        // Should highlight changed character (1 vs 2)
        result.Should().Contain("background-color: #ffc0c0"); // Red for "1"
        result.Should().Contain("background-color: #acf2bd"); // Green for "2"
    }

    /// <summary>
    /// Verifies that diffs handle null/empty before values correctly.
    /// </summary>
    [Test]
    public void FormatDiff_InlineDiff_NullBeforeValue()
    {
        // Arrange - Adding a new value (before is null)
        string? before = null;
        var after = "new-nsg-id";

        // Act
        var result = ScribanHelpers.FormatDiff(before, after, "inline-diff");

        // Assert
        result.Should().NotContain("<span style=");
        result.Should().Contain("new-nsg-id");
    }

    /// <summary>
    /// Verifies that diffs handle null/empty after values correctly.
    /// </summary>
    [Test]
    public void FormatDiff_InlineDiff_NullAfterValue()
    {
        // Arrange - Removing a value (after is null)
        var before = "old-nsg-id";
        string? after = null;

        // Act
        var result = ScribanHelpers.FormatDiff(before, after, "inline-diff");

        // Assert
        result.Should().NotContain("<span style=");
        result.Should().Contain("old-nsg-id");
    }

    /// <summary>
    /// Verifies that diffs handle identical before/after values correctly.
    /// </summary>
    [Test]
    public void FormatDiff_InlineDiff_IdenticalValues()
    {
        // Arrange - No change
        var before = "unchanged-value";
        var after = "unchanged-value";

        // Act
        var result = ScribanHelpers.FormatDiff(before, after, "inline-diff");

        // Assert - Should show value without diff markers
        result.Should().NotContain("- ");
        result.Should().NotContain("+ ");
        result.Should().Contain("unchanged-value");
    }

    /// <summary>
    /// Verifies that diffs are table-compatible (no newlines, proper HTML escaping).
    /// </summary>
    [Test]
    public void FormatDiff_InlineDiff_IsTableCompatible()
    {
        // Arrange - Multi-word value change
        var before = "Value with spaces";
        var after = "Different value with spaces";

        // Act
        var result = ScribanHelpers.FormatDiff(before, after, "inline-diff");

        // Assert - Should be table-compatible with rich HTML
        result.Should().NotContain("\n"); // No raw newlines
        result.Should().Contain("<br>"); // Uses HTML line breaks
        result.Should().Contain("<code style=\"display:block;"); // Block-level code tag

        // Should contain rich HTML styling (table-safe HTML is allowed)
        result.Should().Contain("<span style=");
        result.Should().Contain("background-color:");

        // Should contain both values (parts that aren't split by highlighting)
        result.Should().Contain("alue with spaces"); // Common suffix
        result.Should().Contain("Different"); // Unique to after
    }
}
