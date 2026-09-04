using AwesomeAssertions;
using TUnit.Core;

namespace Oocx.TfPlan2Md.TUnit.Docker;

public class DockerFixtureSecurityTests
{
    [Test]
    public void CreateDockerProcessStartInfo_UsesTokenizedArgumentList()
    {
        var psi = DockerFixture.CreateDockerProcessStartInfo(
            ["run", "--rm", "-i", "tfplan2md-test:latest", "--output", "file name.md"],
            redirectStandardInput: true);

        psi.FileName.Should().Be("docker");
        psi.ArgumentList.Should().Equal("run", "--rm", "-i", "tfplan2md-test:latest", "--output", "file name.md");
        psi.Arguments.Should().BeEmpty();
        psi.RedirectStandardInput.Should().BeTrue();
        psi.RedirectStandardOutput.Should().BeTrue();
        psi.RedirectStandardError.Should().BeTrue();
        psi.UseShellExecute.Should().BeFalse();
    }
}
