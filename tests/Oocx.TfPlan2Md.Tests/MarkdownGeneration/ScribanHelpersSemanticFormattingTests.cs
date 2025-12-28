using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ScribanHelpersSemanticFormattingTests
{
    [Fact]
    public void FormatCodeSummary_EncodesHtmlAndWrapsCode()
    {
        var result = ScribanHelpers.FormatCodeSummary("value<>");

        result.Should().Be("<code>value&lt;&gt;</code>");
    }

    [Fact]
    public void FormatAttributeValueTable_BooleanTrue_UsesIconAndCode()
    {
        var result = ScribanHelpers.FormatAttributeValueTable("https_only", "true", null);

        result.Should().Be("`✅ true`");
    }

    [Fact]
    public void FormatAttributeValueTable_AccessDeny_UsesIconAndCode()
    {
        var result = ScribanHelpers.FormatAttributeValueTable("access", "Deny", null);

        result.Should().Be("`⛔ Deny`");
    }

    [Fact]
    public void FormatAttributeValueTable_DirectionInbound_UsesIconAndCode()
    {
        var result = ScribanHelpers.FormatAttributeValueTable("direction", "Inbound", null);

        result.Should().Be("`⬇️ Inbound`");
    }

    [Fact]
    public void FormatAttributeValueTable_ProtocolAny_UsesIconAndCode()
    {
        var result = ScribanHelpers.FormatAttributeValueTable("protocol", "*", null);

        result.Should().Be("`✳️ *`");
    }

    [Fact]
    public void FormatAttributeValueTable_IpValue_UsesNetworkIconInCode()
    {
        var result = ScribanHelpers.FormatAttributeValueTable("source_address_prefix", "10.0.0.0/16", null);

        result.Should().Be("`🌐 10.0.0.0/16`");
    }

    [Fact]
    public void FormatAttributeValueTable_Location_UsesGlobeIconInCode()
    {
        var result = ScribanHelpers.FormatAttributeValueTable("location", "eastus", null);

        result.Should().Be("`🌍 eastus`");
    }

    [Fact]
    public void FormatAttributeValueSummary_BooleanFalse_UsesIconWithoutCode()
    {
        var result = ScribanHelpers.FormatAttributeValueSummary("enabled", "false", null);

        result.Should().Be("❌ false");
    }

    [Fact]
    public void FormatAttributeValueSummary_IpValue_UsesNetworkIconWithHtmlCode()
    {
        var result = ScribanHelpers.FormatAttributeValueSummary("source_address_prefix", "10.1.0.0/16", null);

        result.Should().Be("<code>🌐 10.1.0.0/16</code>");
    }

    [Fact]
    public void FormatAttributeValueSummary_Location_WrapsInParentheses()
    {
        var result = ScribanHelpers.FormatAttributeValueSummary("location", "westeurope", null);

        result.Should().Be("(<code>🌍 westeurope</code>)");
    }

    [Fact]
    public void FormatAttributeValueSummary_DefaultValue_UsesHtmlCode()
    {
        var result = ScribanHelpers.FormatAttributeValueSummary("name", "hub", null);

        result.Should().Be("<code>hub</code>");
    }
}
