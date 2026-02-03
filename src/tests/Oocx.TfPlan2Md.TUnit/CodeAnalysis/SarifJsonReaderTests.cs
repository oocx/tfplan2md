using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.CodeAnalysis;

/// <summary>
/// Tests for SARIF JSON primitive parsing helpers.
/// Related feature: docs/features/056-static-analysis-integration/test-plan.md.
/// </summary>
public class SarifJsonReaderTests
{
    /// <summary>
    /// Verifies missing or non-string properties return null.
    /// </summary>
    [Test]
    public void GetString_MissingOrNonString_ReturnsNull()
    {
        using var document = JsonDocument.Parse("{\"value\":123}");
        var root = document.RootElement;

        SarifJsonReader.GetString(root, "missing").Should().BeNull();
        SarifJsonReader.GetString(root, "value").Should().BeNull();
    }

    /// <summary>
    /// Verifies numeric values are parsed from numbers and strings.
    /// </summary>
    [Test]
    public void GetOptionalDouble_ParsesNumberOrString()
    {
        using var document = JsonDocument.Parse("{\"number\":1.5,\"text\":\"2.5\"}");
        var root = document.RootElement;

        SarifJsonReader.GetOptionalDouble(root, "number").Should().Be(1.5);
        SarifJsonReader.GetOptionalDouble(root, "text").Should().Be(2.5);
    }

    /// <summary>
    /// Verifies property bag parsing returns null when properties are absent.
    /// </summary>
    [Test]
    public void GetOptionalDoubleFromProperties_MissingProperties_ReturnsNull()
    {
        using var document = JsonDocument.Parse("{\"level\":\"error\"}");
        var root = document.RootElement;

        SarifJsonReader.GetOptionalDoubleFromProperties(root, "rank").Should().BeNull();
    }

    /// <summary>
    /// Verifies integer values are parsed from numbers and strings.
    /// </summary>
    [Test]
    public void GetOptionalInt_ParsesNumberOrString()
    {
        using var document = JsonDocument.Parse("{\"number\":3,\"text\":\"7\"}");
        var root = document.RootElement;

        SarifJsonReader.GetOptionalInt(root, "number").Should().Be(3);
        SarifJsonReader.GetOptionalInt(root, "text").Should().Be(7);
    }
}
