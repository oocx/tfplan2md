using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;

namespace Oocx.TfPlan2Md.Tests.Docker;

[TestClass]
public class DockerIntegrationTests
{
    private DockerFixture Fixture => DockerFixture.Instance;
    private const string TestDataPath = "TestData/azurerm-azuredevops-plan.json";

    private static string Escape(string value) => ScribanHelpers.EscapeMarkdown(value);

    private void SkipIfDockerNotAvailable()
    {
        if (!Fixture.IsDockerAvailable)
        {
            Assert.Inconclusive("Docker is not available");
        }
        if (!Fixture.ImageBuilt)
        {
            Assert.Inconclusive("Docker image could not be built");
        }
    }

    [TestMethod]
    public async Task Docker_WithFileInput_ProducesMarkdownOutput()
    {
        SkipIfDockerNotAvailable();

        var (exitCode, stdout, stderr) = await Fixture.RunContainerAsync(TestDataPath);

        exitCode.Should().Be(0);
        stderr.Should().BeEmpty();
        stdout.Should().Contain("# Terraform Plan");
        stdout.Should().Contain("azurerm_resource_group <b><code>main</code></b>");
    }

    [TestMethod]
    public async Task Docker_WithStdinInput_ProducesMarkdownOutput()
    {
        SkipIfDockerNotAvailable();

        var json = await File.ReadAllTextAsync(TestDataPath);
        var (exitCode, stdout, stderr) = await Fixture.RunContainerWithStdinAsync(json);

        exitCode.Should().Be(0);
        stderr.Should().BeEmpty();
        stdout.Should().Contain("# Terraform Plan");
        stdout.Should().Contain("azurerm_resource_group <b><code>main</code></b>");
    }

    [TestMethod]
    public async Task Docker_WithHelpFlag_DisplaysHelp()
    {
        SkipIfDockerNotAvailable();

        var (exitCode, stdout, stderr) = await Fixture.RunContainerWithStdinAsync("", ["--help"]);

        exitCode.Should().Be(0);
        stderr.Should().BeEmpty();
        stdout.Should().Contain("tfplan2md");
        stdout.Should().Contain("--help");
    }

    [TestMethod]
    public async Task Docker_WithVersionFlag_DisplaysVersion()
    {
        SkipIfDockerNotAvailable();

        var (exitCode, stdout, stderr) = await Fixture.RunContainerWithStdinAsync("", ["--version"]);

        exitCode.Should().Be(0);
        stderr.Should().BeEmpty();
        stdout.Should().Contain("tfplan2md");
    }

    [TestMethod]
    public async Task Docker_WithInvalidInput_ReturnsNonZeroExitCode()
    {
        SkipIfDockerNotAvailable();

        var (exitCode, _, stderr) = await Fixture.RunContainerWithStdinAsync("{ invalid json }");

        exitCode.Should().NotBe(0);
        stderr.Should().Contain("Error");
    }

    [TestMethod]
    public async Task Docker_ParsesAllResourceChanges()
    {
        SkipIfDockerNotAvailable();

        var (exitCode, stdout, stderr) = await Fixture.RunContainerAsync(TestDataPath);

        exitCode.Should().Be(0);
        stderr.Should().BeEmpty();
        // Verify expected resources from the test data are in the output (type <b><code>name</code></b> format)
        stdout.Should().Contain("azurerm_resource_group <b><code>main</code></b>")
            .And.Contain("azurerm_key_vault <b><code>main</code></b>")
            .And.Contain("azurerm_virtual_network <b><code>old</code></b>")
            .And.Contain("azuredevops_project <b><code>main</code></b>")
            .And.Contain("azuredevops_git_repository <b><code>main</code></b>");
    }

    [TestMethod]
    public async Task Docker_Includes_ComprehensiveDemoFiles()
    {
        SkipIfDockerNotAvailable();

        var args = new[]
        {
            "/examples/comprehensive-demo/plan.json",
            "--principals",
            "/examples/comprehensive-demo/demo-principals.json",
            "--template",
            "summary"
        };

        var (exitCode, stdout, stderr) = await Fixture.RunContainerAsync(null, args);

        exitCode.Should().Be(0);
        stderr.Should().BeEmpty();
        stdout.Should().Contain("Terraform Plan Summary");
    }
}
