using System;
using System.Diagnostics;
using TUnit.Core;

namespace Oocx.TfPlan2Md.TUnit.Docker;

/// <summary>
/// Docker fixture for TUnit tests. Manages Docker image build and provides methods to run containers.
/// </summary>
public class DockerFixture
{
    private static readonly Lazy<DockerFixture> LazyInstance = new(() => new DockerFixture());
    private static readonly Task<bool> InitTask = LazyInstance.Value.InitializeAsync();
    private static readonly bool DebugEnabled = string.Equals(Environment.GetEnvironmentVariable("TUNIT_DOCKER_DEBUG"), "1", StringComparison.Ordinal);

    public const string ImageName = "tfplan2md-test";
    public const string ImageTag = "latest";
    public string FullImageName => $"{ImageName}:{ImageTag}";
    public bool IsDockerAvailable { get; private set; }
    public bool ImageBuilt { get; private set; }

    public static DockerFixture Instance => LazyInstance.Value;

    private DockerFixture()
    {
    }

    public static async Task<bool> EnsureInitializedAsync()
    {
        Log("EnsureInitializedAsync invoked");
        return await InitTask;
    }

    private async Task<bool> InitializeAsync()
    {
        Log("InitializeAsync starting");
        IsDockerAvailable = await CheckDockerAvailableAsync();
        if (!IsDockerAvailable)
        {
            Log("Docker not available; skipping build");
            return false;
        }

        ImageBuilt = await BuildDockerImageAsync();
        Log($"Docker image build completed; success={ImageBuilt}");
        return ImageBuilt;
    }

    private static async Task<bool> CheckDockerAvailableAsync()
    {
        Log("Checking docker availability via 'docker version'");
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
            {
                Log("Failed to start docker version process");
                return false;
            }

            await process.WaitForExitAsync();
            Log($"docker version exit code: {process.ExitCode}");
            return process.ExitCode == 0;
        }
        catch
        {
            Log("Exception while running docker version");
            return false;
        }
    }

    private async Task<bool> BuildDockerImageAsync()
    {
        // Check if image already exists (built by scripts/prepare-test-image.sh)
        if (await CheckImageExistsAsync())
        {
            Log($"Docker image {FullImageName} already exists; skipping build");
            return true;
        }

        var repoRoot = FindRepositoryRoot();
        if (repoRoot == null)
        {
            Log("Repository root not found; cannot build image");
            return false;
        }

        Log($"Building docker image {FullImageName} from {repoRoot}");
        var psi = new ProcessStartInfo("docker", $"build -t {FullImageName} .")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            Log("Failed to start docker build process");
            return false;
        }

        await process.WaitForExitAsync();
        Log($"docker build exit code: {process.ExitCode}");
        return process.ExitCode == 0;
    }

    private static async Task<bool> CheckImageExistsAsync()
    {
        Log($"Checking if docker image {Instance.FullImageName} exists");
        try
        {
            var psi = new ProcessStartInfo("docker", $"image inspect {Instance.FullImageName}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                Log("Failed to start docker image inspect process");
                return false;
            }

            await process.WaitForExitAsync();
            var exists = process.ExitCode == 0;
            Log($"docker image exists: {exists}");
            return exists;
        }
        catch
        {
            Log("Exception while checking if docker image exists");
            return false;
        }
    }

    private static string? FindRepositoryRoot()
    {
        var directory = Directory.GetCurrentDirectory();
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory, "Dockerfile")))
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }
        return null;
    }

    public async Task<(int ExitCode, string StdOut, string StdErr)> RunContainerAsync(
        string? inputFile = null,
        string[]? args = null,
        CancellationToken cancellationToken = default)
    {
        Log("RunContainerAsync invoked");
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

        Log($"Running docker with args: {string.Join(' ', arguments)}");
        var psi = new ProcessStartInfo("docker", string.Join(" ", arguments))
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            Log("Failed to start docker run process");
            return (-1, "", "Failed to start docker process");
        }

        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        Log($"docker run exit code: {process.ExitCode}");

        return (process.ExitCode, stdout, stderr);
    }

    public async Task<(int ExitCode, string StdOut, string StdErr)> RunContainerWithStdinAsync(
        string input,
        string[]? args = null,
        CancellationToken cancellationToken = default)
    {
        Log("RunContainerWithStdinAsync invoked");
        var arguments = new List<string> { "run", "--rm", "-i", FullImageName };

        if (args != null)
        {
            arguments.AddRange(args);
        }

        Log($"Running docker (stdin) with args: {string.Join(' ', arguments)}");
        var psi = new ProcessStartInfo("docker", string.Join(" ", arguments))
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            Log("Failed to start docker run (stdin) process");
            return (-1, "", "Failed to start docker process");
        }

        await process.StandardInput.WriteAsync(input.AsMemory(), cancellationToken);
        process.StandardInput.Close();

        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        Log($"docker run (stdin) exit code: {process.ExitCode}");

        return (process.ExitCode, stdout, stderr);
    }

    private static void Log(string message)
    {
        if (!DebugEnabled)
        {
            return;
        }

        Console.Error.WriteLine($"[DockerFixture] {message}");
    }
}
