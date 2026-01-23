using System.Diagnostics;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Workflows;

/// <summary>
/// Tests for the extract-changelog.sh script that extracts version-specific sections from CHANGELOG.md.
/// </summary>
public class ChangelogExtractionTests
{
    private static readonly Lazy<string> RepoRoot = new(FindRepoRoot);
    private static readonly Lazy<bool> BashAvailable = new(CheckBashAvailable);

    /// <summary>
    /// Verifies that the script extracts only the current version section when no previous release is specified.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task Extracts_only_current_version_when_no_previous_release_exists()
    {
        if (!BashAvailable.Value)
        {
            return; // Skip if bash not available
        }

        var output = await RunExtraction("changelog-full.md", "0.12.0", null);

        var expected = """
<a name="0.12.0"></a>
## [0.12.0] - 2025-12-18

### ✨ Features
* feature twelve

### 🐛 Bug Fixes
* fix twelve
""";

        await Assert.That(Normalize(output)).IsEqualTo(Normalize(expected));
    }

    /// <summary>
    /// Verifies that the script extracts only the current version section when the previous release is consecutive (e.g., 0.11.0 -> 0.12.0).
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task Extracts_only_current_version_when_previous_is_consecutive()
    {
        if (!BashAvailable.Value)
        {
            return;
        }

        var output = await RunExtraction("changelog-full.md", "0.12.0", "0.11.0");

        var expected = """
<a name="0.12.0"></a>
## [0.12.0] - 2025-12-18

### ✨ Features
* feature twelve

### 🐛 Bug Fixes
* fix twelve
""";

        await Assert.That(Normalize(output)).IsEqualTo(Normalize(expected));
    }

    /// <summary>
    /// Verifies that the script extracts cumulative sections when versions were skipped (e.g., 0.9.0 -> 0.12.0 includes 0.10.0, 0.11.0).
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task Extracts_cumulative_sections_when_versions_were_skipped()
    {
        if (!BashAvailable.Value)
        {
            return;
        }

        var output = await RunExtraction("changelog-full.md", "0.12.0", "0.9.0");

        var expected = """
<a name="0.12.0"></a>
## [0.12.0] - 2025-12-18

### ✨ Features
* feature twelve

### 🐛 Bug Fixes
* fix twelve

<a name="0.11.0"></a>
## [0.11.0] - 2025-12-17

### ✨ Features
* feature eleven

### 🐛 Bug Fixes
* fix eleven

<a name="0.10.0"></a>
## [0.10.0] - 2025-12-16

### ✨ Features
* feature ten

### 📚 Documentation
* docs ten
""";

        await Assert.That(Normalize(output)).IsEqualTo(Normalize(expected));
    }

    /// <summary>
    /// Verifies that the script preserves complex markdown formatting (nested lists, code blocks, links).
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task Preserves_complex_markdown_formatting()
    {
        if (!BashAvailable.Value)
        {
            return;
        }

        var output = await RunExtraction("changelog-complex.md", "0.3.0", null);

        var expected = """
<a name="0.3.0"></a>
## [0.3.0] - 2025-12-10

### ✨ Features
* feature three
* nested list:
  - item one
  - item two
    - sub item

### 🧪 Examples
```
code block line 1
code block line 2
```

### 🔗 Links
* [example](https://example.com/path)
""";

        await Assert.That(Normalize(output)).IsEqualTo(Normalize(expected));
    }

    /// <summary>
    /// Verifies that the script handles versions with or without 'v' prefix consistently.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task Handles_versions_with_or_without_v_prefix_consistently()
    {
        if (!BashAvailable.Value)
        {
            return;
        }

        var withPrefix = await RunExtraction("changelog-full.md", "v0.12.0", "v0.9.0");
        var withoutPrefix = await RunExtraction("changelog-full.md", "0.12.0", "0.9.0");

        await Assert.That(Normalize(withPrefix)).IsEqualTo(Normalize(withoutPrefix));
    }

    /// <summary>
    /// Verifies that the script produces identical output when run multiple times with the same inputs (idempotent).
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task Is_idempotent_for_same_inputs()
    {
        if (!BashAvailable.Value)
        {
            return;
        }

        var first = await RunExtraction("changelog-full.md", "0.12.0", "0.9.0");
        var second = await RunExtraction("changelog-full.md", "0.12.0", "0.9.0");

        await Assert.That(Normalize(first)).IsEqualTo(Normalize(second));
    }

    /// <summary>
    /// Verifies that the script extracts all sections until the end of the changelog when the last version is not found.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task Extracts_until_end_when_last_version_not_found()
    {
        if (!BashAvailable.Value)
        {
            return;
        }

        var output = await RunExtraction("changelog-full.md", "0.12.0", "0.5.0");

        var expected = """
<a name="0.12.0"></a>
## [0.12.0] - 2025-12-18

### ✨ Features
* feature twelve

### 🐛 Bug Fixes
* fix twelve

<a name="0.11.0"></a>
## [0.11.0] - 2025-12-17

### ✨ Features
* feature eleven

### 🐛 Bug Fixes
* fix eleven

<a name="0.10.0"></a>
## [0.10.0] - 2025-12-16

### ✨ Features
* feature ten

### 📚 Documentation
* docs ten

<a name="0.9.0"></a>
## [0.9.0] - 2025-12-15

### ✨ Features
* feature nine

<a name="0.8.0"></a>
## [0.8.0] - 2025-12-14

### ✨ Features
* feature eight
""";

        await Assert.That(Normalize(output)).IsEqualTo(Normalize(expected));
    }

    /// <summary>
    /// Verifies that the script returns empty output when the current version is not found in the changelog.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task Returns_empty_output_when_current_version_not_found()
    {
        if (!BashAvailable.Value)
        {
            return;
        }

        var output = await RunExtraction("changelog-full.md", "1.0.0", "0.9.0");

        await Assert.That(Normalize(output)).IsEqualTo(string.Empty);
    }

    /// <summary>
    /// Verifies that the script works correctly with POSIX-compliant awk implementation.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task Works_with_posix_awk()
    {
        if (!BashAvailable.Value)
        {
            return;
        }

        var output = await RunExtraction("changelog-full.md", "0.12.0", "0.11.0", usePosixAwk: true);

        var expected = """
<a name="0.12.0"></a>
## [0.12.0] - 2025-12-18

### ✨ Features
* feature twelve

### 🐛 Bug Fixes
* fix twelve
""";

        await Assert.That(Normalize(output)).IsEqualTo(Normalize(expected));
    }

    /// <summary>
    /// Verifies that the script can extract prerelease versions (e.g., 1.0.0-alpha.3).
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task Extracts_prerelease_version()
    {
        if (!BashAvailable.Value)
        {
            return;
        }

        var output = await RunExtraction("changelog-prerelease.md", "1.0.0-alpha.3", null);

        var expected = """
<a name="1.0.0-alpha.3"></a>
## [1.0.0-alpha.3] - 2025-12-20

### ✨ Features
* feature alpha three

### 🐛 Bug Fixes
* fix alpha three
""";

        await Assert.That(Normalize(output)).IsEqualTo(Normalize(expected));
    }

    /// <summary>
    /// Verifies that the script can extract a range of prerelease versions (e.g., 1.0.0-alpha.3 to 1.0.0-alpha.1).
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task Extracts_prerelease_version_range()
    {
        if (!BashAvailable.Value)
        {
            return;
        }

        var output = await RunExtraction("changelog-prerelease.md", "1.0.0-alpha.3", "1.0.0-alpha.1");

        var expected = """
<a name="1.0.0-alpha.3"></a>
## [1.0.0-alpha.3] - 2025-12-20

### ✨ Features
* feature alpha three

### 🐛 Bug Fixes
* fix alpha three

<a name="1.0.0-alpha.2"></a>
## [1.0.0-alpha.2] - 2025-12-19

### ✨ Features
* feature alpha two
""";

        await Assert.That(Normalize(output)).IsEqualTo(Normalize(expected));
    }

    private static async Task<string> RunExtraction(string changelogFile, string currentVersion, string? lastVersion, bool usePosixAwk = false)
    {
        var scriptPath = Path.Combine(RepoRoot.Value, "scripts", "extract-changelog.sh");
        var changelogPath = Path.Combine(RepoRoot.Value, "src", "tests", "Oocx.TfPlan2Md.TUnit", "TestData", changelogFile);

        var arguments = new List<string> { scriptPath, changelogPath, currentVersion };
        if (lastVersion is not null)
        {
            arguments.Add(lastVersion);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "bash",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = RepoRoot.Value
        };

        if (usePosixAwk)
        {
            startInfo.Environment["POSIXLY_CORRECT"] = "1";
        }

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)!;
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Process exited with code {process.ExitCode}. stderr: {stderr}");
        }

        await Assert.That(process.ExitCode).IsEqualTo(0);

        return stdout;
    }

    private static string Normalize(string value) => value.ReplaceLineEndings("\n").TrimEnd();

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "src", "tfplan2md.slnx")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new InvalidOperationException("Repository root not found");
    }

    private static bool CheckBashAvailable()
    {
        if (OperatingSystem.IsWindows())
        {
            return false;
        }
        return File.Exists("/bin/bash") || File.Exists("/usr/bin/bash");
    }
}
