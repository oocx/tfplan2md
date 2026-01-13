using TUnit.Core;
using Oocx.TfPlan2Md.MarkdownGeneration;

namespace Oocx.TfPlan2Md.TUnit.Docker;

[NotInParallel("Docker")]
public class DockerIntegrationTests
{
    private readonly DockerFixture _fixture = DockerFixture.Instance;
    private const string TestDataPath = "TestData/azurerm-azuredevops-plan.json";

    private static string Escape(string value) => ScribanHelpers.EscapeMarkdown(value);

    private async Task<bool> SkipIfDockerNotAvailableAsync()
    {
        await DockerFixture.EnsureInitializedAsync();
        return !_fixture.IsDockerAvailable || !_fixture.ImageBuilt;
    }

    [Test]
    public async Task Docker_WithFileInput_ProducesMarkdownOutput()
    {
        if (await SkipIfDockerNotAvailableAsync())
        {
            // Skip if Docker not available
            return;
        }

        var (exitCode, stdout, stderr) = await _fixture.RunContainerAsync(TestDataPath);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(stderr).IsEmpty();
        await Assert.That(stdout).Contains("# Terraform Plan");
        await Assert.That(stdout).Contains("azurerm_resource_group <b><code>main</code></b>");
    }

    [Test]
    public async Task Docker_WithStdinInput_ProducesMarkdownOutput()
    {
        if (await SkipIfDockerNotAvailableAsync())
        {
            // Skip if Docker not available
            return;
        }

        var json = await File.ReadAllTextAsync(TestDataPath);
        var (exitCode, stdout, stderr) = await _fixture.RunContainerWithStdinAsync(json);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(stderr).IsEmpty();
        await Assert.That(stdout).Contains("# Terraform Plan");
        await Assert.That(stdout).Contains("azurerm_resource_group <b><code>main</code></b>");
    }

    [Test]
    public async Task Docker_WithHelpFlag_DisplaysHelp()
    {
        if (await SkipIfDockerNotAvailableAsync())
        {
            // Skip if Docker not available
            return;
        }

        var (exitCode, stdout, stderr) = await _fixture.RunContainerWithStdinAsync("", ["--help"]);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(stderr).IsEmpty();
        await Assert.That(stdout).Contains("tfplan2md");
        await Assert.That(stdout).Contains("--help");
    }

    [Test]
    public async Task Docker_WithVersionFlag_DisplaysVersion()
    {
        if (await SkipIfDockerNotAvailableAsync())
        {
            // Skip if Docker not available
            return;
        }

        var (exitCode, stdout, stderr) = await _fixture.RunContainerWithStdinAsync("", ["--version"]);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(stderr).IsEmpty();
        await Assert.That(stdout).Contains("tfplan2md");
    }

    [Test]
    public async Task Docker_WithInvalidInput_ReturnsNonZeroExitCode()
    {
        if (await SkipIfDockerNotAvailableAsync())
        {
            // Skip if Docker not available
            return;
        }

        var (exitCode, _, stderr) = await _fixture.RunContainerWithStdinAsync("{ invalid json }");

        await Assert.That(exitCode).IsNotEqualTo(0);
        await Assert.That(stderr).Contains("Error");
    }

    [Test]
    public async Task Docker_ParsesAllResourceChanges()
    {
        if (await SkipIfDockerNotAvailableAsync())
        {
            // Skip if Docker not available
            return;
        }

        var (exitCode, stdout, stderr) = await _fixture.RunContainerAsync(TestDataPath);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(stderr).IsEmpty();
        // Verify expected resources from the test data are in the output (type <b><code>name</code></b> format)
        await Assert.That(stdout).Contains("azurerm_resource_group <b><code>main</code></b>");
        await Assert.That(stdout).Contains("azurerm_key_vault <b><code>main</code></b>");
        await Assert.That(stdout).Contains("azurerm_virtual_network <b><code>old</code></b>");
        await Assert.That(stdout).Contains("azuredevops_project <b><code>main</code></b>");
        await Assert.That(stdout).Contains("azuredevops_git_repository <b><code>main</code></b>");
    }

    [Test]
    public async Task Docker_Includes_ComprehensiveDemoFiles()
    {
        if (await SkipIfDockerNotAvailableAsync())
        {
            // Skip if Docker not available
            return;
        }

        var args = new[]
        {
            "/examples/comprehensive-demo/plan.json",
            "--principals",
            "/examples/comprehensive-demo/demo-principals.json",
            "--template",
            "summary"
        };

        var (exitCode, stdout, stderr) = await _fixture.RunContainerAsync(null, args);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(stderr).IsEmpty();
        await Assert.That(stdout).Contains("Terraform Plan Summary");
    }
}
