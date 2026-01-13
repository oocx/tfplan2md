using System.Diagnostics;
using AwesomeAssertions;

namespace Oocx.TfPlan2Md.Tests.Workflows;

[TestClass]
public class ChangelogExtractionTests
{
    private static readonly Lazy<string> RepoRoot = new(FindRepoRoot);

    [TestMethod]
    public void Extracts_only_current_version_when_no_previous_release_exists()
    {
        SkipIfBashUnavailable();

        var output = RunExtraction("changelog-full.md", "0.12.0", null);

        var expected = """
<a name="0.12.0"></a>
## [0.12.0] - 2025-12-18

### ✨ Features
* feature twelve

### 🐛 Bug Fixes
* fix twelve
""";

        Normalize(output).Should().Be(Normalize(expected));
    }

    [TestMethod]
    public void Extracts_only_current_version_when_previous_is_consecutive()
    {
        SkipIfBashUnavailable();

        var output = RunExtraction("changelog-full.md", "0.12.0", "0.11.0");

        var expected = """
<a name="0.12.0"></a>
## [0.12.0] - 2025-12-18

### ✨ Features
* feature twelve

### 🐛 Bug Fixes
* fix twelve
""";

        Normalize(output).Should().Be(Normalize(expected));
    }

    [TestMethod]
    public void Extracts_cumulative_sections_when_versions_were_skipped()
    {
        SkipIfBashUnavailable();

        var output = RunExtraction("changelog-full.md", "0.12.0", "0.9.0");

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

        Normalize(output).Should().Be(Normalize(expected));
    }

    [TestMethod]
    public void Preserves_complex_markdown_formatting()
    {
        SkipIfBashUnavailable();

        var output = RunExtraction("changelog-complex.md", "0.3.0", null);

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

        Normalize(output).Should().Be(Normalize(expected));
    }

    [TestMethod]
    public void Handles_versions_with_or_without_v_prefix_consistently()
    {
        SkipIfBashUnavailable();

        var withPrefix = RunExtraction("changelog-full.md", "v0.12.0", "v0.9.0");
        var withoutPrefix = RunExtraction("changelog-full.md", "0.12.0", "0.9.0");

        Normalize(withPrefix).Should().Be(Normalize(withoutPrefix));
    }

    [TestMethod]
    public void Is_idempotent_for_same_inputs()
    {
        SkipIfBashUnavailable();

        var first = RunExtraction("changelog-full.md", "0.12.0", "0.9.0");
        var second = RunExtraction("changelog-full.md", "0.12.0", "0.9.0");

        Normalize(first).Should().Be(Normalize(second));
    }

    [TestMethod]
    public void Extracts_until_end_when_last_version_not_found()
    {
        SkipIfBashUnavailable();

        var output = RunExtraction("changelog-full.md", "0.12.0", "0.5.0");

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

        Normalize(output).Should().Be(Normalize(expected));
    }

    [TestMethod]
    public void Returns_empty_output_when_current_version_not_found()
    {
        SkipIfBashUnavailable();

        var output = RunExtraction("changelog-full.md", "1.0.0", "0.9.0");

        Normalize(output).Should().Be(string.Empty);
    }

    [TestMethod]
    public void Works_with_posix_awk()
    {
        SkipIfBashUnavailable();

        var output = RunExtraction("changelog-full.md", "0.12.0", "0.11.0", usePosixAwk: true);

        var expected = """
<a name="0.12.0"></a>
## [0.12.0] - 2025-12-18

### ✨ Features
* feature twelve

### 🐛 Bug Fixes
* fix twelve
""";

        Normalize(output).Should().Be(Normalize(expected));
    }

    private static string RunExtraction(string changelogFile, string currentVersion, string? lastVersion, bool usePosixAwk = false)
    {
        var scriptPath = Path.Combine(RepoRoot.Value, "scripts", "extract-changelog.sh");
        var changelogPath = Path.Combine(RepoRoot.Value, "tests", "Oocx.TfPlan2Md.Tests", "TestData", changelogFile);

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
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        process.ExitCode.Should().Be(0, $"stderr: {stderr}");

        return stdout;
    }

    private static string Normalize(string value) => value.ReplaceLineEndings("\n").TrimEnd();

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "tfplan2md.slnx")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new InvalidOperationException("Repository root not found");
    }

    private static void SkipIfBashUnavailable()
    {
        Skip.If(OperatingSystem.IsWindows(), "Bash is required to run the release script tests.");
        var bashExists = File.Exists("/bin/bash") || File.Exists("/usr/bin/bash");
        Skip.IfNot(bashExists, "Bash is required to run the release script tests.");
    }
}
