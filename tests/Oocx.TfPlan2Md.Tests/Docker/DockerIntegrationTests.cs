using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;

namespace Oocx.TfPlan2Md.Tests.Docker;

[Collection(nameof(DockerCollection))]
public class DockerIntegrationTests
{
    private readonly DockerFixture _fixture;
    private const string TestDataPath = "TestData/azurerm-azuredevops-plan.json";

    private static string Escape(string value) => ScribanHelpers.EscapeMarkdown(value);

    public DockerIntegrationTests(DockerFixture fixture)
    {
        _fixture = fixture;
    }

    private void SkipIfDockerNotAvailable()
    {
        Xunit.Skip.If(!_fixture.IsDockerAvailable, "Docker is not available");
        Xunit.Skip.If(!_fixture.ImageBuilt, "Docker image could not be built");
    }

    [SkippableFact]
    public async Task Docker_WithFileInput_ProducesMarkdownOutput()
    {
        SkipIfDockerNotAvailable();

        var (exitCode, stdout, stderr) = await _fixture.RunContainerAsync(TestDataPath);

        exitCode.Should().Be(0);
        stderr.Should().BeEmpty();
        stdout.Should().Contain("# Terraform Plan");
        stdout.Should().Contain(Escape("azurerm_resource_group.main"));
    }

    [SkippableFact]
    public async Task Docker_WithStdinInput_ProducesMarkdownOutput()
    {
        SkipIfDockerNotAvailable();

        var json = await File.ReadAllTextAsync(TestDataPath);
        var (exitCode, stdout, stderr) = await _fixture.RunContainerWithStdinAsync(json);

        exitCode.Should().Be(0);
        stderr.Should().BeEmpty();
        stdout.Should().Contain("# Terraform Plan");
        stdout.Should().Contain(Escape("azurerm_resource_group.main"));
    }

    [SkippableFact]
    public async Task Docker_WithHelpFlag_DisplaysHelp()
    {
        SkipIfDockerNotAvailable();

        var (exitCode, stdout, stderr) = await _fixture.RunContainerWithStdinAsync("", ["--help"]);

        exitCode.Should().Be(0);
        stderr.Should().BeEmpty();
        stdout.Should().Contain("tfplan2md");
        stdout.Should().Contain("--help");
    }

    [SkippableFact]
    public async Task Docker_WithVersionFlag_DisplaysVersion()
    {
        SkipIfDockerNotAvailable();

        var (exitCode, stdout, stderr) = await _fixture.RunContainerWithStdinAsync("", ["--version"]);

        exitCode.Should().Be(0);
        stderr.Should().BeEmpty();
        stdout.Should().Contain("tfplan2md");
    }

    [SkippableFact]
    public async Task Docker_WithInvalidInput_ReturnsNonZeroExitCode()
    {
        SkipIfDockerNotAvailable();

        var (exitCode, _, stderr) = await _fixture.RunContainerWithStdinAsync("{ invalid json }");

        exitCode.Should().NotBe(0);
        stderr.Should().Contain("Error");
    }

    [SkippableFact]
    public async Task Docker_ParsesAllResourceChanges()
    {
        SkipIfDockerNotAvailable();

        var (exitCode, stdout, stderr) = await _fixture.RunContainerAsync(TestDataPath);

        exitCode.Should().Be(0);
        stderr.Should().BeEmpty();
        // Verify expected resources from the test data are in the output
        stdout.Should().Contain(Escape("azurerm_resource_group.main"))
            .And.Contain(Escape("azurerm_key_vault.main"))
            .And.Contain(Escape("azurerm_virtual_network.old"))
            .And.Contain(Escape("azuredevops_project.main"))
            .And.Contain(Escape("azuredevops_git_repository.main"));
    }

    [SkippableFact]
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

        var (exitCode, stdout, stderr) = await _fixture.RunContainerAsync(null, args);

        exitCode.Should().Be(0);
        stderr.Should().BeEmpty();
        stdout.Should().Contain("Terraform Plan Summary");
    }
}
