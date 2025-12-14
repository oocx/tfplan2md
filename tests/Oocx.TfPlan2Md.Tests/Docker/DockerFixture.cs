using System.Diagnostics;

namespace Oocx.TfPlan2Md.Tests.Docker;

public class DockerFixture : IAsyncLifetime
{
    public const string ImageName = "tfplan2md-test";
    public const string ImageTag = "latest";
    public string FullImageName => $"{ImageName}:{ImageTag}";
    public bool IsDockerAvailable { get; private set; }
    public bool ImageBuilt { get; private set; }

    public async Task InitializeAsync()
    {
        IsDockerAvailable = await CheckDockerAvailableAsync();
        if (!IsDockerAvailable)
            return;

        ImageBuilt = await BuildDockerImageAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private static async Task<bool> CheckDockerAvailableAsync()
    {
        try
        {
            var psi = new ProcessStartInfo("docker", "version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            if (process == null)
                return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> BuildDockerImageAsync()
    {
        var repoRoot = FindRepositoryRoot();
        if (repoRoot == null)
            return false;

        var psi = new ProcessStartInfo("docker", $"build -t {FullImageName} .")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        if (process == null)
            return false;

        await process.WaitForExitAsync();
        return process.ExitCode == 0;
    }

    private static string? FindRepositoryRoot()
    {
        var directory = Directory.GetCurrentDirectory();
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory, "Dockerfile")))
                return directory;
            directory = Directory.GetParent(directory)?.FullName;
        }
        return null;
    }

    public async Task<(int ExitCode, string StdOut, string StdErr)> RunContainerAsync(
        string? inputFile = null,
        string[]? args = null)
    {
        var arguments = new List<string> { "run", "--rm", "-i" };

        if (inputFile != null)
        {
            var absolutePath = Path.GetFullPath(inputFile);
            var fileName = Path.GetFileName(inputFile);
            arguments.Add("-v");
            arguments.Add($"{absolutePath}:/data/{fileName}:ro");
        }

        arguments.Add(FullImageName);

        if (inputFile != null)
        {
            arguments.Add($"/data/{Path.GetFileName(inputFile)}");
        }

        if (args != null)
        {
            arguments.AddRange(args);
        }

        var psi = new ProcessStartInfo("docker", string.Join(" ", arguments))
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        if (process == null)
            return (-1, "", "Failed to start docker process");

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, stdout, stderr);
    }

    public async Task<(int ExitCode, string StdOut, string StdErr)> RunContainerWithStdinAsync(
        string input,
        string[]? args = null)
    {
        var arguments = new List<string> { "run", "--rm", "-i", FullImageName };

        if (args != null)
        {
            arguments.AddRange(args);
        }

        var psi = new ProcessStartInfo("docker", string.Join(" ", arguments))
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        if (process == null)
            return (-1, "", "Failed to start docker process");

        await process.StandardInput.WriteAsync(input);
        process.StandardInput.Close();

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, stdout, stderr);
    }
}
