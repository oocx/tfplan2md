using AwesomeAssertions;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Tests.Parsing;

public class TerraformPlanParserReplacePathsTests
{
    [Fact]
    public void Parse_ShouldPopulateReplacePaths()
    {
        // Arrange
        var json = File.ReadAllText("TestData/replace-paths-plan.json");
        var parser = new TerraformPlanParser();

        // Act
        var plan = parser.Parse(json);

        // Assert
        var change = plan.ResourceChanges.Should().ContainSingle().Subject.Change;
        change.ReplacePaths.Should().NotBeNull();
        change.ReplacePaths!.Should().HaveCount(1);
        var path = change.ReplacePaths![0];
        path.Should().ContainInOrder("address_prefixes", 0);
    }
}
