using AwesomeAssertions;
using Oocx.TfPlan2Md.Tests.MarkdownGeneration;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class MarkdownLintFixtureTests
{
    [Test]
    public void CreateDockerProcessStartInfo_UsesTokenizedArgumentList()
    {
        var psi = MarkdownLintFixture.CreateDockerProcessStartInfo(
            ["run", "--rm", "-i", "davidanson/markdownlint-cli2:v0.20.0", "--stdin", "notes with spaces.md"],
            redirectStandardInput: true);

        psi.FileName.Should().Be("docker");
        psi.ArgumentList.Should().Equal("run", "--rm", "-i", "davidanson/markdownlint-cli2:v0.20.0", "--stdin", "notes with spaces.md");
        psi.Arguments.Should().BeEmpty();
        psi.RedirectStandardInput.Should().BeTrue();
        psi.RedirectStandardOutput.Should().BeTrue();
        psi.RedirectStandardError.Should().BeTrue();
        psi.UseShellExecute.Should().BeFalse();
    }
}
