using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Parsing;

/// <summary>
/// Coverage tests for ReplacePathsConverter behavior.
/// Related feature: docs/features/010-replacement-reasons-and-summaries/specification.md
/// </summary>
public class ReplacePathsConverterTests
{
    [Test]
    public void Read_NullToken_ReturnsNull()
    {
        var options = CreateOptions();

        var result = JsonSerializer.Deserialize<IReadOnlyList<IReadOnlyList<object>>?>("null", options);

        result.Should().BeNull();
    }

    [Test]
    public void Read_NonArray_ThrowsJsonException()
    {
        var options = CreateOptions();

        var action = () => JsonSerializer.Deserialize<IReadOnlyList<IReadOnlyList<object>>?>("{}", options);

        action.Should().Throw<JsonException>();
    }

    [Test]
    public void Read_SkipsNonArrayEntries()
    {
        var options = CreateOptions();
        const string json = "[\"skip\", [\"name\", 1]]";

        var result = JsonSerializer.Deserialize<IReadOnlyList<IReadOnlyList<object>>?>(json, options);

        result.Should().NotBeNull();
        result!.Should().HaveCount(1);
        result[0].Should().ContainInOrder("name", 1);
    }

    [Test]
    public void Read_ParsesCommonSegmentTypes()
    {
        var options = CreateOptions();
        const string json = "[[\"name\", 1, 2147483648, 1.25, true, false, null]]";

        var result = JsonSerializer.Deserialize<IReadOnlyList<IReadOnlyList<object>>?>(json, options);

        result.Should().NotBeNull();
        var segments = result![0];
        segments[0].Should().Be("name");
        segments[1].Should().BeOfType<int>().Which.Should().Be(1);
        segments[2].Should().BeOfType<long>().Which.Should().Be(2147483648L);
        segments[3].Should().BeOfType<double>().Which.Should().Be(1.25d);
        segments[4].Should().BeOfType<bool>().Which.Should().BeTrue();
        segments[5].Should().BeOfType<bool>().Which.Should().BeFalse();
        segments[6].Should().BeOfType<string>().Which.Should().Be(string.Empty);
    }

    [Test]
    public void Write_NullValue_WritesNullLiteral()
    {
        var options = CreateOptions();

        var json = JsonSerializer.Serialize<IReadOnlyList<IReadOnlyList<object>>?>(null, options);

        json.Should().Be("null");
    }

    [Test]
    public void Write_SerializesSegmentsToExpectedJsonTypes()
    {
        var options = CreateOptions();
        var value = new List<IReadOnlyList<object>>
        {
            new List<object>
            {
                "name",
                1,
                2147483648L,
                1.25d,
                2.5m,
                true,
                null!,
                new Version(1, 2, 3)
            }
        };

        var json = JsonSerializer.Serialize<IReadOnlyList<IReadOnlyList<object>>?>(value, options);
        using var document = JsonDocument.Parse(json);
        var segments = document.RootElement[0];

        segments[0].GetString().Should().Be("name");
        segments[1].GetInt32().Should().Be(1);
        segments[2].GetInt64().Should().Be(2147483648L);
        segments[3].GetDouble().Should().Be(1.25d);
        segments[4].GetDecimal().Should().Be(2.5m);
        segments[5].GetBoolean().Should().BeTrue();
        segments[6].ValueKind.Should().Be(JsonValueKind.Null);
        segments[7].GetString().Should().Be("1.2.3");
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new ReplacePathsConverter());
        return options;
    }
}
