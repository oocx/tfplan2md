using System.Text.RegularExpressions;
using AwesomeAssertions;
using TUnit.Core;

namespace Oocx.TfPlan2Md.TUnit.Architecture;

/// <summary>
/// Documentation regression guards for the active provider-architecture guidance.
/// Related feature: docs/features/110-refactoring-opportunities/test-plan.md.
/// </summary>
[Category("Unit")]
public class DocumentationAlignmentTests
{
    [Test]
    public async Task DocumentationFiles_DoNotReferToIProviderModuleAsActiveContract()
    {
        var activeDocs = new[]
        {
            GetRepositoryFilePath("docs", "features.md"),
            GetRepositoryFilePath("docs", "architecture.md")
        };

        foreach (var path in activeDocs)
        {
            var content = await File.ReadAllTextAsync(path);
            content.Should().NotContain("IProviderModule", because: $"{Path.GetFileName(path)} should describe the current provider contract");
        }
    }

    [Test]
    public async Task Adr006_DescribesIProvider_NotIProviderModule()
    {
        var path = GetRepositoryFilePath("docs", "adr-006-dependency-injection.md");
        var content = await File.ReadAllTextAsync(path);

        content.Should().Contain("IProvider", because: "ADR-006 should describe the current provider contract");
        content.Should().NotContain("implement `IProviderModule`", because: "ADR-006 should not instruct contributors to use the deleted contract");
    }

    [Test]
    public async Task ActiveDocumentationFiles_DoNotInstructImplementingIProviderModule()
    {
        var activeDocs = new[]
        {
            GetRepositoryFilePath("docs", "features.md"),
            GetRepositoryFilePath("docs", "adr-006-dependency-injection.md"),
            GetRepositoryFilePath("docs", "architecture.md"),
            GetRepositoryFilePath("CONTRIBUTING.md"),
            GetRepositoryFilePath("README.md")
        };

        var pattern = new Regex(
            "implement.*IProviderModule|IProviderModule.*extend",
            RegexOptions.IgnoreCase | RegexOptions.Singleline,
            TimeSpan.FromSeconds(1));

        foreach (var path in activeDocs)
        {
            var content = await File.ReadAllTextAsync(path);
            pattern.IsMatch(content).Should().BeFalse(because: $"{Path.GetFileName(path)} should not instruct contributors to use the deleted provider contract");
        }
    }

    private static string GetRepositoryFilePath(params string[] segments)
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../..", Path.Combine(segments)));
    }
}
