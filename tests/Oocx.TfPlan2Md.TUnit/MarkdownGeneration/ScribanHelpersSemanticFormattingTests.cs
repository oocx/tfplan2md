using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ScribanHelpersSemanticFormattingTests
{
    [Test]
    public void FormatCodeSummary_EncodesHtmlAndWrapsCode()
    {
        var result = ScribanHelpers.FormatCodeSummary("value<>");

        result.Should().Be("<code>value&lt;&gt;</code>");
    }

    [Test]
    public void FormatAttributeValueTable_BooleanTrue_UsesIconAndCode()
    {
        var result = ScribanHelpers.FormatAttributeValueTable("https_only", "true", null);

        result.Should().Be("`✅\u00A0true`");
    }

    [Test]
    public void FormatAttributeValueTable_AccessDeny_UsesIconAndCode()
    {
        var result = ScribanHelpers.FormatAttributeValueTable("access", "Deny", null);

        result.Should().Be("`⛔\u00A0Deny`");
    }

    [Test]
    public void FormatAttributeValueTable_DirectionInbound_UsesIconAndCode()
    {
        var result = ScribanHelpers.FormatAttributeValueTable("direction", "Inbound", null);

        result.Should().Be("`⬇️\u00A0Inbound`");
    }

    [Test]
    public void FormatAttributeValueTable_ProtocolAny_UsesIconAndCode()
    {
        var result = ScribanHelpers.FormatAttributeValueTable("protocol", "*", null);

        result.Should().Be("`✳️`");
    }

    [Test]
    public void FormatAttributeValueTable_IpValue_UsesNetworkIconInCode()
    {
        var result = ScribanHelpers.FormatAttributeValueTable("source_address_prefix", "10.0.0.0/16", null);

        result.Should().Be("`🌐\u00A010.0.0.0/16`");
    }

    [Test]
    public void FormatAttributeValueTable_Location_UsesGlobeIconInCode()
    {
        var result = ScribanHelpers.FormatAttributeValueTable("location", "eastus", null);

        result.Should().Be("`🌍\u00A0eastus`");
    }

    [Test]
    public void FormatAttributeValueSummary_BooleanFalse_UsesIconWithoutCode()
    {
        var result = ScribanHelpers.FormatAttributeValueSummary("enabled", "false", null);

        result.Should().Be("❌\u00A0false");
    }

    [Test]
    public void FormatAttributeValueSummary_IpValue_UsesNetworkIconWithHtmlCode()
    {
        var result = ScribanHelpers.FormatAttributeValueSummary("source_address_prefix", "10.1.0.0/16", null);

        result.Should().Be("<code>🌐\u00A010.1.0.0/16</code>");
    }

    [Test]
    public void FormatAttributeValueSummary_Location_WrapsInParentheses()
    {
        var result = ScribanHelpers.FormatAttributeValueSummary("location", "westeurope", null);

        result.Should().Be("<code>🌍\u00A0westeurope</code>");
    }

    [Test]
    public void FormatAttributeValuePlain_IpValue_UsesNonBreakingSpace()
    {
        var result = ScribanHelpers.FormatAttributeValuePlain("source_address_prefix", "10.0.0.0/16", null);

        result.Should().Be("🌐\u00A010.0.0.0/16");
    }

    [Test]
    public void FormatAttributeValueSummary_DefaultValue_UsesHtmlCode()
    {
        var result = ScribanHelpers.FormatAttributeValueSummary("name", "hub", null);

        result.Should().Be("<code>🆔\u00A0hub</code>");
    }
}
