namespace Oocx.TfPlan2Md.Tests.Docker;

[Collection(nameof(DockerCollection))]
public class DockerIntegrationTests
{
    private readonly DockerFixture _fixture;
    private const string TestDataPath = "TestData/azurerm-azuredevops-plan.json";

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

        Assert.Equal(0, exitCode);
        Assert.Contains("# Terraform Plan", stdout);
        Assert.Contains("azurerm_resource_group.main", stdout);
    }

    [SkippableFact]
    public async Task Docker_WithStdinInput_ProducesMarkdownOutput()
    {
        SkipIfDockerNotAvailable();

        var json = await File.ReadAllTextAsync(TestDataPath);
        var (exitCode, stdout, stderr) = await _fixture.RunContainerWithStdinAsync(json);

        Assert.Equal(0, exitCode);
        Assert.Contains("# Terraform Plan", stdout);
        Assert.Contains("azurerm_resource_group.main", stdout);
    }

    [SkippableFact]
    public async Task Docker_WithHelpFlag_DisplaysHelp()
    {
        SkipIfDockerNotAvailable();

        var (exitCode, stdout, stderr) = await _fixture.RunContainerWithStdinAsync("", ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("tfplan2md", stdout);
        Assert.Contains("--help", stdout);
    }

    [SkippableFact]
    public async Task Docker_WithVersionFlag_DisplaysVersion()
    {
        SkipIfDockerNotAvailable();

        var (exitCode, stdout, stderr) = await _fixture.RunContainerWithStdinAsync("", ["--version"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("tfplan2md", stdout);
    }

    [SkippableFact]
    public async Task Docker_WithInvalidInput_ReturnsNonZeroExitCode()
    {
        SkipIfDockerNotAvailable();

        var (exitCode, stdout, stderr) = await _fixture.RunContainerWithStdinAsync("{ invalid json }");

        Assert.NotEqual(0, exitCode);
        Assert.Contains("Error", stderr);
    }

    [SkippableFact]
    public async Task Docker_ParsesAllResourceChanges()
    {
        SkipIfDockerNotAvailable();

        var (exitCode, stdout, stderr) = await _fixture.RunContainerAsync(TestDataPath);

        Assert.Equal(0, exitCode);
        // Verify expected resources from the test data are in the output
        Assert.Contains("azurerm_resource_group.main", stdout);
        Assert.Contains("azurerm_key_vault.main", stdout);
        Assert.Contains("azurerm_virtual_network.old", stdout);
        Assert.Contains("azuredevops_project.main", stdout);
        Assert.Contains("azuredevops_git_repository.main", stdout);
    }
}
