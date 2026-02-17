# Issue: Decimal Number Incorrectly Rendered with IP Icon

## Problem Description

The attribute `min_capacity` of `azurerm_mssql_database` with value `0.5` is incorrectly rendered with the IP address icon (🌐 0.5). This is wrong because `0.5` is a decimal number representing database capacity (in vCores), not an IP address.

## Steps to Reproduce

1. Create a Terraform plan with `azurerm_mssql_database` resource
2. Set `min_capacity = 0.5` (valid vCore value for serverless SQL databases)
3. Generate markdown report with tfplan2md
4. Observe: value is rendered as `🌐 0.5` instead of plain `0.5`

## Expected Behavior

Only complete IPv4 addresses (a.b.c.d format, optionally with /24 CIDR mask) and complete IPv6 addresses should receive the 🌐 icon. Decimal numbers with dots (like 0.5, 1.25, 3.14) must NOT be treated as IP addresses.

## Actual Behavior

The value `0.5` is being rendered with the IP icon: `🌐 0.5`

## Root Cause Analysis

### Affected Components

**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.Identity.cs`  
**Method:** `IsIpAddressOrCidr(string value)` at lines 326-339

```csharp
private static bool IsIpAddressOrCidr(string value)
{
    if (!value.Contains('.', StringComparison.Ordinal))
    {
        return false;
    }

    if (IPAddress.TryParse(value, out _))  // ← BUG IS HERE
    {
        return true;
    }

    return Regex.IsMatch(value, "^([0-9]{1,3}\\.){3}[0-9]{1,3}/[0-9]{1,2}$", 
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));
}
```

**Invocation sites:**
- `SemanticFormatting.cs` line 142: Used in `FormatAttributeValuePlain()`
- `SemanticFormatting.Registry.cs` line 282: Used in fallback formatting

### What's Broken

**.NET IPAddress.TryParse accepts shortened IPv4 notation:**
- `IPAddress.TryParse("0.5", out _)` returns `true` and interprets it as `0.0.0.5`
- `IPAddress.TryParse("1.2", out _)` returns `true` and interprets it as `1.0.0.2`
- `IPAddress.TryParse("1.2.3", out _)` returns `true` and interprets it as `1.2.0.3`

This legacy behavior is defined in .NET's IPAddress class to support ancient IPv4 shorthand notation (like `127.1` for `127.0.0.1`). While technically valid according to historical IPv4 standards, Terraform never uses this notation - all IP addresses in Terraform are full dotted-quad (a.b.c.d) or IPv6 format.

The current regex check on line 338 only validates CIDR blocks (a.b.c.d/mask), but the IPAddress.TryParse check happens first and catches shortened formats.

### Why It Happened

The original implementation relied on .NET's `IPAddress.TryParse()` to validate IP addresses, which was reasonable for typical IP addresses. However, it didn't account for:
1. .NET's legacy support for shortened IPv4 notation
2. Terraform attribute values that are decimal numbers (database vCores, cost multipliers, scaling factors)

## Suggested Fix Approach

**Smallest Safe Code Change:**

Replace the `IPAddress.TryParse` check with explicit validation for full IPv4 and IPv6 formats:

1. **For IPv4**: Require exactly 4 octets (a.b.c.d) - check that value has exactly 3 dots and all 4 parts are valid 0-255 numbers
2. **For IPv6**: Keep using `IPAddress.TryParse` but only for values containing `:` (colon)
3. **For CIDR**: Keep existing regex validation for IPv4 CIDR blocks (a.b.c.d/mask)

**Implementation strategy:**
```csharp
private static bool IsIpAddressOrCidr(string value)
{
    if (!value.Contains('.', StringComparison.Ordinal) && !value.Contains(':', StringComparison.Ordinal))
    {
        return false;
    }

    // IPv6 check (contains colon)
    if (value.Contains(':', StringComparison.Ordinal))
    {
        return IPAddress.TryParse(value, out var addr) && addr.AddressFamily == AddressFamily.InterNetworkV6;
    }

    // IPv4 CIDR check (a.b.c.d/mask)
    if (value.Contains('/', StringComparison.Ordinal))
    {
        return Regex.IsMatch(value, "^([0-9]{1,3}\\.){3}[0-9]{1,3}/[0-9]{1,2}$", 
            RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));
    }

    // IPv4 full dotted-quad check (a.b.c.d) - must have exactly 3 dots
    var parts = value.Split('.');
    if (parts.Length != 4)
    {
        return false;
    }

    // Validate each octet is 0-255
    foreach (var part in parts)
    {
        if (!int.TryParse(part, out var octet) || octet < 0 || octet > 255)
        {
            return false;
        }
    }

    return true;
}
```

**Why this is safer than relying on IPAddress.TryParse:**
- Explicitly rejects 1-part, 2-part, and 3-part dotted notation
- Only accepts standard Terraform IP formats
- Still validates IPv6 correctly
- Keeps CIDR validation unchanged
- Minimal performance impact (simple string operations)

## Related Tests

### Existing Tests to Verify Still Pass

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersSemanticFormattingTests.cs`

Existing tests that should continue to pass:
- Line 159: `FormatAttributeValueTable_IpValue_UsesNetworkIconInCode` - tests `10.0.0.0/16` CIDR
- Line 207: `FormatAttributeValueSummary_IpValue_UsesNetworkIconWithHtmlCode` - tests `10.1.0.0/16` CIDR
- Line 279: `FormatAttributeValuePlain_IpValue_UsesNonBreakingSpace` - tests `10.0.0.0/16` CIDR

### New Test Coverage Needed

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersSemanticFormattingTests.cs`

Add these test cases to verify the fix:

```csharp
[Test]
public void FormatAttributeValueTable_DecimalNumber_DoesNotUseIpIcon()
{
    // Test case from bug report
    var result = FormatAttributeValueTable("min_capacity", "0.5", null);
    
    result.Should().Be("`0.5`");
}

[Test]
public void FormatAttributeValuePlain_DecimalNumber_DoesNotUseIpIcon()
{
    // Verify plain formatting also correct
    var result = FormatAttributeValuePlain("min_capacity", "0.5", null);
    
    result.Should().Be("0.5");
}

[Test]
public void FormatAttributeValueTable_FullIpv4_UsesIpIcon()
{
    // Ensure full IPv4 still works
    var result = FormatAttributeValueTable("source_ip", "192.168.1.1", null);
    
    result.Should().Be("`🌐\u00A0192.168.1.1`");
}

[Test]
public void FormatAttributeValueTable_Ipv6_UsesIpIcon()
{
    // Ensure IPv6 still works
    var result = FormatAttributeValueTable("source_ip", "2001:db8::1", null);
    
    result.Should().Be("`🌐\u00A02001:db8::1`");
}

[Test]
[Arguments("1.2")]      // 2-part shorthand
[Arguments("1.2.3")]    // 3-part shorthand
[Arguments("1.25")]     // decimal with 2 digits after dot
[Arguments("3.14159")]  // pi-like decimal
public void FormatAttributeValuePlain_ShortenedOrDecimal_DoesNotUseIpIcon(string value)
{
    // Verify various shortened and decimal formats are rejected
    var result = FormatAttributeValuePlain("value", value, null);
    
    result.Should().Be(value); // No icon, just the raw value
}

[Test]
[Arguments("10.0.0.1")]
[Arguments("192.168.1.1")]
[Arguments("255.255.255.255")]
[Arguments("0.0.0.0")]
public void FormatAttributeValuePlain_FullIpv4Addresses_UseIpIcon(string ipAddress)
{
    // Verify standard IPv4 addresses get icon
    var result = FormatAttributeValuePlain("ip", ipAddress, null);
    
    result.Should().Be($"🌐\u00A0{ipAddress}");
}
```

**Assertions Summary:**
- ✅ Decimal numbers (0.5, 1.25, 3.14159) → NO icon, plain value
- ✅ Shortened IPv4 (1.2, 1.2.3) → NO icon, plain value
- ✅ Full IPv4 (192.168.1.1, 10.0.0.1) → IP icon (🌐)
- ✅ IPv6 (2001:db8::1) → IP icon (🌐)
- ✅ CIDR (10.0.0.0/16) → IP icon (🌐)

## Additional Context

**Related Features:**
- Feature: `docs/features/017-visual-report-enhancements/specification.md` - Original semantic icons feature
- Specification states: "IP Addresses and CIDR Blocks" should use 🌐 icon

**Azure SQL Database Context:**
- `azurerm_mssql_database` resource has `min_capacity` attribute
- Valid values: 0.5, 0.75, 1.0, 1.25, etc. (vCore units for serverless tier)
- These are decimal numbers, not IP addresses

**Performance Note:**
- The proposed fix avoids regex for simple IPv4 checks, using string split and int parsing
- This is actually faster than IPAddress.TryParse for rejecting invalid formats
- IPv6 still uses IPAddress.TryParse but only when colon is present (rare case)
