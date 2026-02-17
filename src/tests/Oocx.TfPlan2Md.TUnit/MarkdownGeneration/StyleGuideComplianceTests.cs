using System.IO;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests that validate markdown output complies with the style guide requirements.
/// Related specification: docs/report-style-guide.md.
/// Related issue: docs/issues/086-style-guide-compliance-fixes/issue-analysis.md.
/// </summary>
public class StyleGuideComplianceTests
{
    /// <summary>
    /// Gets all generated artifact and example markdown files for compliance testing.
    /// </summary>
    /// <returns>Collection of file paths to validate.</returns>
    private static List<string> GetAllMarkdownArtifacts()
    {
        var files = new List<string>();

        // Tests run from bin/Debug/net10.0/, so we need to go up to the repo root
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));

        // Add artifacts directory files
        var artifactsPath = Path.Combine(repoRoot, "artifacts");
        if (Directory.Exists(artifactsPath))
        {
            files.AddRange(Directory.GetFiles(artifactsPath, "*.md", SearchOption.TopDirectoryOnly));
        }

        // Add example markdown files
        var examplesPath = Path.Combine(repoRoot, "examples");
        if (Directory.Exists(examplesPath))
        {
            foreach (var exampleDir in Directory.GetDirectories(examplesPath))
            {
                files.AddRange(Directory.GetFiles(exampleDir, "*.md", SearchOption.TopDirectoryOnly));
            }
        }

        return files;
    }

    /// <summary>
    /// TC-086-1: Verifies wrench icon in changed attribute summaries has a space before it.
    /// Violation example: "2🔧 account_replication_type"
    /// Compliant example: "2 🔧 account_replication_type"
    /// Style guide requirement (line 59): Changed attribute summary format is `&lt;count&gt; 🔧 &lt;attributes&gt;` with non-breaking space.
    /// Related issue: docs/issues/086-style-guide-compliance-fixes/issue-analysis.md (Violation 1).
    /// </summary>
    [Test]
    public void Test_WrenchIcon_HasNonBreakingSpace()
    {
        // Pattern: digit immediately followed by wrench emoji (no space)
        var violationPattern = new Regex(@"\d🔧", RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

        var violations = new List<string>();

        foreach (var file in GetAllMarkdownArtifacts())
        {
            var content = File.ReadAllText(file);
            var matches = violationPattern.Matches(content);

            if (matches.Count > 0)
            {
                var fileName = Path.GetFileName(file);
                violations.Add($"{fileName}: {matches.Count} occurrence(s)");
            }
        }

        if (violations.Count > 0)
        {
            Assert.Fail(
                "Style Guide Violation: Wrench icon missing space\n\n" +
                "The wrench icon (🔧) must be preceded by a non-breaking space (U+00A0).\n" +
                "Expected format: '2\\u00A0🔧 attribute_name'\n" +
                "Current format:  '2🔧 attribute_name'\n\n" +
                "Files with violations:\n" +
                string.Join("\n", violations.Select(v => $"  - {v}")));
        }
    }

    /// <summary>
    /// TC-086-2: Verifies AzAPI resource names are not empty in summaries.
    /// Violation example: "&lt;summary&gt;➕ azapi_resource &lt;b&gt;&lt;/b&gt; — &lt;code&gt;🆔 myAccount&lt;/code&gt;&lt;/summary&gt;"
    /// Compliant example: "&lt;summary&gt;➕ azapi_resource &lt;b&gt;&lt;code&gt;myAccount&lt;/code&gt;&lt;/b&gt; — &lt;code&gt;🆔 myAccount&lt;/code&gt;&lt;/summary&gt;"
    /// Style guide requirement (line 54): Resource Name must be bold + code-formatted.
    /// Related issue: docs/issues/086-style-guide-compliance-fixes/issue-analysis.md (Violation 2).
    /// </summary>
    [Test]
    public void Test_AzApiResourceNames_NotEmpty()
    {
        // Pattern: empty bold tags in summaries
        var violationPattern = new Regex(@"<summary>[^<]*<b></b>", RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

        var violations = new List<string>();

        foreach (var file in GetAllMarkdownArtifacts())
        {
            var content = File.ReadAllText(file);
            var matches = violationPattern.Matches(content);

            if (matches.Count > 0)
            {
                var fileName = Path.GetFileName(file);
                violations.Add($"{fileName}: {matches.Count} occurrence(s)");
            }
        }

        if (violations.Count > 0)
        {
            Assert.Fail(
                "Style Guide Violation: Empty AzAPI resource names\n\n" +
                "AzAPI resources must display either a friendly name or fall back to the Terraform resource name.\n" +
                "Empty <b></b> tags are not acceptable.\n\n" +
                "Files with violations:\n" +
                string.Join("\n", violations.Select(v => $"  - {v}")));
        }
    }

    /// <summary>
    /// TC-086-3: Verifies tags headers include the 🏷️ icon.
    /// Violation example: "**Tags:**"
    /// Compliant example: "**🏷️ Tags:**"
    /// Style guide requirement (line 98): Tags must use format `**🏷️ Tags:** `key: value``
    /// Related issue: docs/issues/086-style-guide-compliance-fixes/issue-analysis.md (Violation 3).
    /// </summary>
    [Test]
    public void Test_TagsHeader_HasIcon()
    {
        // Pattern: Tags header without the tag icon
        // Must handle both standalone and inline variants
        var violationPattern = new Regex(@"\*\*Tags:\*\*(?!\s*🏷️)", RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

        var violations = new List<string>();

        foreach (var file in GetAllMarkdownArtifacts())
        {
            var content = File.ReadAllText(file);
            var matches = violationPattern.Matches(content);

            if (matches.Count > 0)
            {
                var fileName = Path.GetFileName(file);
                violations.Add($"{fileName}: {matches.Count} occurrence(s)");
            }
        }

        if (violations.Count > 0)
        {
            Assert.Fail(
                "Style Guide Violation: Tags header missing icon\n\n" +
                "Tags headers must include the 🏷️ icon.\n" +
                "Expected format: '**🏷️ Tags:**'\n" +
                "Current format:  '**Tags:**'\n\n" +
                "Files with violations:\n" +
                string.Join("\n", violations.Select(v => $"  - {v}")));
        }
    }

    /// <summary>
    /// TC-086-4: Verifies no H3 headings are inside &lt;details&gt; blocks.
    /// Violation example: &lt;details&gt; containing "### ➕ azapi_resource.complex_app"
    /// Compliant: Resources inside &lt;details&gt; should not use H3 headings
    /// Style guide requirement (line 160): Maintains proper heading hierarchy (H3 for modules, no H4 for resources).
    /// Related issue: docs/issues/086-style-guide-compliance-fixes/issue-analysis.md (Violation 4).
    /// </summary>
    [Test]
    public void Test_NoH3HeadingsInDetails()
    {
        // Pattern: <details> block containing ### heading (but not ####, #####, etc.)
        // Use multiline matching to find details blocks with H3
        // Key: ^### matches line-start with exactly 3 hashes, [^#] ensures 4th char is not a hash
        var violationPattern = new Regex(
            @"<details[^>]*>(?:(?!<\/details>).)*^###[^#]",
            RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));

        var violations = new List<string>();

        foreach (var file in GetAllMarkdownArtifacts())
        {
            var content = File.ReadAllText(file);
            var matches = violationPattern.Matches(content);

            if (matches.Count > 0)
            {
                var fileName = Path.GetFileName(file);
                violations.Add($"{fileName}: {matches.Count} occurrence(s)");
            }
        }

        if (violations.Count > 0)
        {
            Assert.Fail(
                "Style Guide Violation: H3 headings inside <details> blocks\n\n" +
                "Resource details should not contain H3 (###) headings.\n" +
                "H3 headings are reserved for module-level sections.\n\n" +
                "Files with violations:\n" +
                string.Join("\n", violations.Select(v => $"  - {v}")) +
                "\n\nThese files may be obsolete and not regenerated by generate-demo-artifacts.sh.");
        }
    }

    /// <summary>
    /// TC-086-5: Verifies module headers include the 📦 icon.
    /// Violation example: "### Module: `module.network`"
    /// Compliant example: "### 📦 Module: `module.network`"
    /// Style guide requirement (lines 168, 180): Module headers use the 📦 icon with non-breaking space.
    /// Related issue: docs/issues/086-style-guide-compliance-fixes/issue-analysis.md (Violation 5).
    /// </summary>
    [Test]
    public void Test_ModuleHeaders_HavePackageIcon()
    {
        // Pattern: Module header without package icon
        var violationPattern = new Regex(@"^###\s+Module:", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

        var violations = new List<string>();

        foreach (var file in GetAllMarkdownArtifacts())
        {
            var content = File.ReadAllText(file);
            var matches = violationPattern.Matches(content);

            if (matches.Count > 0)
            {
                var fileName = Path.GetFileName(file);
                violations.Add($"{fileName}: {matches.Count} occurrence(s)");
            }
        }

        if (violations.Count > 0)
        {
            Assert.Fail(
                "Style Guide Violation: Module headers missing package icon\n\n" +
                "Module headers must include the 📦 icon with a non-breaking space.\n" +
                "Expected format: '### 📦 Module: `module.network`'\n" +
                "Current format:  '### Module: `module.network`'\n\n" +
                "Files with violations:\n" +
                string.Join("\n", violations.Select(v => $"  - {v}")));
        }
    }

    /// <summary>
    /// TC-086-6: Verifies attribute names in tables are plain text, not wrapped in backticks.
    /// Violation example: "| `location` | `eastus` |"
    /// Compliant example: "| location | `eastus` |"
    /// Style guide requirement (line 18): Attribute Names (Keys) must be plain text.
    /// Related issue: docs/issues/086-style-guide-compliance-fixes/issue-analysis.md (Violation 6).
    /// </summary>
    [Test]
    public void Test_AttributeNamesNotInBackticks()
    {
        // Pattern: table row with attribute name in backticks in first column
        // This is tricky to detect perfectly, so we look for common attribute patterns
        var violations = new List<string>();

        foreach (var file in GetAllMarkdownArtifacts())
        {
            var content = File.ReadAllText(file);

            // Split into lines and check table rows
            var lines = content.Split('\n');
            var violationCount = 0;

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                // Skip table headers and separator lines
                if (line.StartsWith('|') && !line.Contains("---", StringComparison.Ordinal))
                {
                    // Check if this looks like an attribute table (key-value pair)
                    // First column backticked identifier, second column backticked value
                    var match = Regex.Match(
                        line,
                        @"^\|\s*`([a-z_][a-z0-9_]*)`\s*\|\s*`[^`]+`\s*\|",
                        RegexOptions.None,
                        TimeSpan.FromSeconds(1));

                    if (match.Success)
                    {
                        // Likely an attribute name in backticks - this is a violation
                        violationCount++;
                    }
                }
            }

            if (violationCount > 0)
            {
                var fileName = Path.GetFileName(file);
                violations.Add($"{fileName}: {violationCount} occurrence(s)");
            }
        }

        if (violations.Count > 0)
        {
            Assert.Fail(
                "Style Guide Violation: Attribute names wrapped in backticks\n\n" +
                "Attribute names (table keys) must be plain text, not code-formatted.\n" +
                "Expected format: '| location | `eastus` |'\n" +
                "Current format:  '| `location` | `eastus` |'\n\n" +
                "Files with violations:\n" +
                string.Join("\n", violations.Select(v => $"  - {v}")));
        }
    }
}
