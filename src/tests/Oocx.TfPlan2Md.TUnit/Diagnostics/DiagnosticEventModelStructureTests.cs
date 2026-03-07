using System.Reflection;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Diagnostics;
using TUnit.Core;

namespace Oocx.TfPlan2Md.TUnit.Diagnostics;

/// <summary>
/// Structural guards for diagnostic event and formatting boundaries.
/// Related feature: docs/features/110-refactoring-opportunities/test-plan.md.
/// </summary>
[Category("Unit")]
public class DiagnosticEventModelStructureTests
{
    [Test]
    public void DiagnosticEventTypes_DoNotContainMarkdownGenerationLogic()
    {
        var diagnosticModelTypes = new[]
        {
            typeof(FailedResolution),
            typeof(TemplateResolution),
            typeof(DiagnosticReport)
        };

#pragma warning disable S3011 // Reflection is intentional for structural regression coverage.
        var markdownMethods = diagnosticModelTypes
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(method => method.ReturnType == typeof(string)
                    && (method.Name.Contains("Markdown", StringComparison.OrdinalIgnoreCase)
                        || method.Name.Contains("Render", StringComparison.OrdinalIgnoreCase))))
            .Select(method => $"{method.DeclaringType?.Name}.{method.Name}")
            .ToList();
#pragma warning restore S3011

        markdownMethods.Should().BeEmpty();
    }

    [Test]
    public async Task ProgramEntry_DebugMode_UsesFormatterForDebugSection()
    {
#pragma warning disable S3011 // Reflection is intentional for structural regression coverage.
        typeof(DiagnosticContext)
            .GetMethod("GenerateMarkdownSection", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Should()
            .BeNull();
#pragma warning restore S3011

        var programEntrySource = await File.ReadAllTextAsync(GetRepositoryFilePath("src", "Oocx.TfPlan2Md", "ProgramEntry.cs"));

        programEntrySource.Should().Contain("DiagnosticMarkdownFormatter.Format");
        programEntrySource.Should().NotContain("GenerateMarkdownSection", because: "ProgramEntry should assemble debug output via the dedicated formatter");
    }

    private static string GetRepositoryFilePath(params string[] segments)
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../..", Path.Combine(segments)));
    }
}
