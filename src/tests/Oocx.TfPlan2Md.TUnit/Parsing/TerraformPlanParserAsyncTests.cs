using System.Text;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Parsing;

/// <summary>
/// Async parsing coverage for Terraform plan parser.
/// </summary>
public class TerraformPlanParserAsyncTests
{
    /// <summary>
    /// Ensures ParseAsync throws when JSON is invalid.
    /// </summary>
    [Test]
    public async Task ParseAsync_InvalidJson_Throws()
    {
        var parser = new TerraformPlanParser();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{ invalid"));

        var action = async () => await parser.ParseAsync(stream);

        await action.Should().ThrowAsync<TerraformPlanParseException>();
    }

    /// <summary>
    /// Ensures ParseAsync throws when JSON deserializes to null.
    /// </summary>
    [Test]
    public async Task ParseAsync_NullJson_Throws()
    {
        var parser = new TerraformPlanParser();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("null"));

        var action = async () => await parser.ParseAsync(stream);

        await action.Should().ThrowAsync<TerraformPlanParseException>();
    }

    /// <summary>
    /// Ensures ParseAsync throws TerraformPlanParseException (not ArgumentNullException) when stream is null.
    /// </summary>
    [Test]
    public async Task ParseAsync_NullStream_ThrowsTerraformPlanParseException()
    {
        var parser = new TerraformPlanParser();
        Stream? nullStream = null;

        var action = async () => await parser.ParseAsync(nullStream!);

        await action.Should().ThrowAsync<TerraformPlanParseException>()
            .WithMessage("*cannot be null*");
    }
}
