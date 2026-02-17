using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using TUnit.Core;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ScribanHelpersSemanticFormattingTests
{
    [Test]
    public void FormatCodeSummary_EncodesHtmlAndWrapsCode()
    {
        var result = FormatCodeSummary("value<>");

        result.Should().Be("<code>value&lt;&gt;</code>");
    }

    [Test]
    public void FormatCodeTable_EncodesMarkdownAndWrapsCode()
    {
        var result = FormatCodeTable("value|`");

        result.Should().Be("`value\\|\\``");
    }

    [Test]
    public void FormatCodeTable_WithEmptyValue_ReturnsEmpty()
    {
        var result = FormatCodeTable(string.Empty);

        result.Should().Be(string.Empty);
    }

    [Test]
    public void FormatAttributeValueTable_BooleanTrue_UsesIconAndCode()
    {
        var result = FormatAttributeValueTable("https_only", "true", null);

        result.Should().Be("`✅\u00A0true`");
    }

    [Test]
    public void FormatAttributeValueTable_AccessDeny_UsesIconAndCode()
    {
        var result = FormatAttributeValueTable("access", "Deny", null);

        result.Should().Be("`⛔\u00A0Deny`");
    }

    [Test]
    public void FormatAttributeValueTable_AccessAllow_UsesIconAndCode()
    {
        var result = FormatAttributeValueTable("action", "Allow", null);

        result.Should().Be("`✅\u00A0Allow`");
    }

    [Test]
    public void FormatAttributeValueTable_DirectionInbound_UsesIconAndCode()
    {
        var result = FormatAttributeValueTable("direction", "Inbound", null);

        result.Should().Be("`⬇️\u00A0Inbound`");
    }

    [Test]
    public void FormatAttributeValueSummary_DirectionOutbound_UsesIconWithoutCode()
    {
        var result = FormatAttributeValueSummary("direction", "Outbound", null);

        result.Should().Be("⬆️\u00A0Outbound");
    }

    [Test]
    public void FormatAttributeValueTable_ProtocolValues_UseExpectedIcons()
    {
        var cases = new Dictionary<string, string>
        {
            ["tcp"] = "`🔗\u00A0TCP`",
            ["udp"] = "`📨\u00A0UDP`",
            ["icmp"] = "`📡\u00A0ICMP`",
            ["*"] = "`✳️`"
        };

        foreach (var entry in cases)
        {
            var result = FormatAttributeValueTable("protocol", entry.Key, null);
            result.Should().Be(entry.Value);
        }
    }

    [Test]
    public void FormatAttributeValueTable_PortValues_UsePlugIcon()
    {
        var cases = new Dictionary<string, string>
        {
            ["443"] = "`🔌\u00A0443`",
            ["80-443"] = "`🔌\u00A080-443`",
            ["*"] = "`✳️`"
        };

        foreach (var entry in cases)
        {
            var result = FormatAttributeValueTable("destination_port_range", entry.Key, null);
            result.Should().Be(entry.Value);
        }
    }

    [Test]
    public void FormatAttributeValueTable_PrincipalTypes_UseExpectedIcons()
    {
        var cases = new Dictionary<string, string>
        {
            ["User"] = "`👤\u00A0User`",
            ["Group"] = "`👥\u00A0Group`",
            ["ServicePrincipal"] = "`💻\u00A0ServicePrincipal`"
        };

        foreach (var entry in cases)
        {
            var result = FormatAttributeValueTable("principal_type", entry.Key, null);
            result.Should().Be(entry.Value);
        }
    }

    [Test]
    public void FormatAttributeValueTable_RoleDefinition_UsesShieldIcon()
    {
        var result = FormatAttributeValueTable("role_definition_name", "Contributor", null);

        result.Should().Be("`🛡️\u00A0Contributor`");
    }

    [Test]
    public void FormatAttributeValueTable_ResourceGroupName_UsesFolderIcon()
    {
        var result = FormatAttributeValueTable("resource_group_name", "rg-app", null);

        result.Should().Be("`📁\u00A0rg-app`");
    }

    [Test]
    public void FormatAttributeValuePlain_ResourceGroupName_UsesFolderIcon()
    {
        var result = FormatAttributeValuePlain("resource_group_name", "rg-app", null);

        result.Should().Be("📁\u00A0rg-app");
    }

    [Test]
    public void FormatAttributeValueTable_ProtocolAny_UsesIconAndCode()
    {
        var result = FormatAttributeValueTable("protocol", "*", null);

        result.Should().Be("`✳️`");
    }

    [Test]
    public void FormatAttributeValueTable_IpValue_UsesNetworkIconInCode()
    {
        var result = FormatAttributeValueTable("source_address_prefix", "10.0.0.0/16", null);

        result.Should().Be("`🌐\u00A010.0.0.0/16`");
    }

    [Test]
    public void FormatAttributeValueTable_Location_UsesGlobeIconInCode()
    {
        var result = FormatAttributeValueTable("location", "eastus", null);

        result.Should().Be("`🌍\u00A0eastus`");
    }

    [Test]
    public void FormatAttributeValueTable_UserPrincipalName_UsesIdIcon()
    {
        var result = FormatAttributeValueTable("user_principal_name", "jane.doe@contoso.com", null);

        result.Should().Be("`🆔\u00A0jane.doe@contoso.com`");
    }

    [Test]
    public void FormatAttributeValueTable_Mail_UsesEmailIcon()
    {
        var result = FormatAttributeValueTable("mail", "jane.doe@contoso.com", null);

        result.Should().Be("`📧\u00A0jane.doe@contoso.com`");
    }

    [Test]
    public void FormatAttributeValueTable_UserEmailAddress_UsesEmailIcon()
    {
        var result = FormatAttributeValueTable("user_email_address", "contractor@external.com", null);

        result.Should().Be("`📧\u00A0contractor@external.com`");
    }

    [Test]
    public void FormatAttributeValueSummary_BooleanFalse_UsesIconWithoutCode()
    {
        var result = FormatAttributeValueSummary("enabled", "false", null);

        result.Should().Be("❌\u00A0false");
    }

    [Test]
    public void FormatAttributeValueSummary_IpValue_UsesNetworkIconWithHtmlCode()
    {
        var result = FormatAttributeValueSummary("source_address_prefix", "10.1.0.0/16", null);

        result.Should().Be("<code>🌐\u00A010.1.0.0/16</code>");
    }

    [Test]
    public void FormatAttributeValueSummary_Location_WrapsInParentheses()
    {
        var result = FormatAttributeValueSummary("location", "westeurope", null);

        result.Should().Be("<code>🌍\u00A0westeurope</code>");
    }

    [Test]
    public void FormatAttributeValueSummary_UserPrincipalName_UsesIdIcon()
    {
        var result = FormatAttributeValueSummary("user_principal_name", "jane.doe@contoso.com", null);

        result.Should().Be("<code>🆔\u00A0jane.doe@contoso.com</code>");
    }

    [Test]
    public void FormatAttributeValueTable_SubscriptionId_UsesKeyEmojiAndCode()
    {
        var result = FormatAttributeValueTable("subscription_id", "00000000-0000-0000-0000-000000000000", null);

        result.Should().Be("`🔑\u00A000000000-0000-0000-0000-000000000000`");
    }

    [Test]
    public void FormatAttributeValueSummary_SubscriptionId_UsesKeyEmoji()
    {
        var result = FormatAttributeValueSummary("subscription_id", "00000000-0000-0000-0000-000000000000", null);

        result.Should().Be("<code>🔑\u00A000000000-0000-0000-0000-000000000000</code>");
    }

    [Test]
    public void FormatAttributeValuePlain_SubscriptionId_UsesKeyEmoji()
    {
        var result = FormatAttributeValuePlain("subscription_id", "00000000-0000-0000-0000-000000000000", null);

        result.Should().Be("🔑\u00A000000000-0000-0000-0000-000000000000");
    }

    [Test]
    public void FormatAttributeValueTable_SubscriptionName_UsesKeyEmojiAndCode()
    {
        var result = FormatAttributeValueTable("subscription", "Production", null);

        result.Should().Be("`🔑\u00A0Production`");
    }

    [Test]
    public void FormatAttributeValueSummary_SubscriptionName_UsesKeyEmoji()
    {
        var result = FormatAttributeValueSummary("subscription", "Production", null);

        result.Should().Be("<code>🔑\u00A0Production</code>");
    }

    [Test]
    public void FormatAttributeValuePlain_SubscriptionName_UsesKeyEmoji()
    {
        var result = FormatAttributeValuePlain("subscription", "Production", null);

        result.Should().Be("🔑\u00A0Production");
    }

    [Test]
    public void FormatAttributeValuePlain_IpValue_UsesNonBreakingSpace()
    {
        var result = FormatAttributeValuePlain("source_address_prefix", "10.0.0.0/16", null);

        result.Should().Be("🌐\u00A010.0.0.0/16");
    }

    [Test]
    public void FormatAttributeValueSummary_DefaultValue_UsesHtmlCode()
    {
        var result = FormatAttributeValueSummary("name", "hub", null);

        result.Should().Be("<code>🆔\u00A0hub</code>");
    }

    [Test]
    public void FormatIconValueSummary_UsesNonBreakingSpaceAndCode()
    {
        var result = FormatIconValueSummary("👤 Jane Doe");

        result.Should().Be("<code>👤\u00A0Jane Doe</code>");
    }

    [Test]
    public void FormatIconValueTable_UsesNonBreakingSpaceAndCode()
    {
        var result = FormatIconValueTable("👤 Jane Doe");

        result.Should().Be("`👤\u00A0Jane Doe`");
    }

    [Test]
    public void FormatAttributeValueTable_DecimalNumber_DoesNotUseIpIcon()
    {
        var result = FormatAttributeValueTable("min_capacity", "0.5", null);

        result.Should().Be("`0.5`");
    }

    [Test]
    public void FormatAttributeValueTable_MultiDecimalNumber_DoesNotUseIpIcon()
    {
        var result = FormatAttributeValueTable("max_size_gb", "1.5", null);

        result.Should().Be("`1.5`");
    }

    [Test]
    public void FormatAttributeValueTable_ValidIpv4Address_UsesNetworkIcon()
    {
        var result = FormatAttributeValueTable("ip_address", "192.168.1.1", null);

        result.Should().Be("`🌐\u00A0192.168.1.1`");
    }

    [Test]
    public void FormatAttributeValueTable_ValidIpv4Cidr_UsesNetworkIcon()
    {
        var result = FormatAttributeValueTable("address_prefix", "10.0.0.0/24", null);

        result.Should().Be("`🌐\u00A010.0.0.0/24`");
    }

    [Test]
    public void FormatAttributeValueTable_ValidIpv6Address_UsesNetworkIcon()
    {
        var result = FormatAttributeValueTable("ipv6_address", "2001:0db8:85a3::8a2e:0370:7334", null);

        result.Should().Be("`🌐\u00A02001:0db8:85a3::8a2e:0370:7334`");
    }

    [Test]
    public void FormatAttributeValueTable_IpLikeButInvalidValues_DoNotUseNetworkIcon()
    {
        var cases = new Dictionary<string, string>
        {
            ["a.b.c.d"] = "`a.b.c.d`",
            ["256.256.256.256"] = "`256.256.256.256`",
            ["1.2.3.4.5"] = "`1.2.3.4.5`"
        };

        foreach (var entry in cases)
        {
            var result = FormatAttributeValueTable("source_address_prefix", entry.Key, null);
            result.Should().Be(entry.Value);
        }
    }
}
