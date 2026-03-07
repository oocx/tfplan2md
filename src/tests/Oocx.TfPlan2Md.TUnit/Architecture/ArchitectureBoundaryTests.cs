using NetArchTest.Rules;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Assertions.Exceptions;
using TUnit.Core;

namespace Oocx.TfPlan2Md.TUnit.Architecture;

/// <summary>
/// Architecture tests that enforce layer boundaries and dependency rules.
/// See docs/architecture-rules.md for rule documentation.
/// Related ADR: docs/adr-007-architecture-boundary-enforcement.md
/// </summary>
public class ArchitectureBoundaryTests
{
    private const string ParsingNamespace = "Oocx.TfPlan2Md.Parsing";
    private const string MarkdownGenerationNamespace = "Oocx.TfPlan2Md.MarkdownGeneration";
    private const string ProvidersNamespace = "Oocx.TfPlan2Md.Providers";

    // === LAYER DEPENDENCY RULES (FORBIDDEN) ===

    /// <summary>
    /// Verifies that the Parsing layer does not depend on MarkdownGeneration.
    /// Parsing is a core domain layer and should remain independent of rendering concerns.
    /// </summary>
    [Test]
    public void Parsing_ShouldNotDependOn_MarkdownGeneration()
    {
        var result = Types.InAssembly(typeof(TerraformPlan).Assembly)
            .That().ResideInNamespace(ParsingNamespace)
            .ShouldNot().HaveDependencyOn(MarkdownGenerationNamespace)
            .GetResult();

        if (!result.IsSuccessful)
        {
            throw new AssertionException(CreateViolationMessage(
                "Parsing layer must not depend on MarkdownGeneration",
                "Parsing is a core domain layer and should not know about rendering concerns. This prevents circular dependencies and maintains clean separation between parsing and rendering.",
                result.FailingTypes));
        }
    }

    /// <summary>
    /// Verifies that the Parsing layer does not depend on CLI.
    /// Core domain should be independent of user interface concerns.
    /// </summary>
    [Test]
    public void Parsing_ShouldNotDependOn_CLI()
    {
        var result = Types.InAssembly(typeof(TerraformPlan).Assembly)
            .That().ResideInNamespace(ParsingNamespace)
            .ShouldNot().HaveDependencyOn("Oocx.TfPlan2Md.CLI")
            .GetResult();

        if (!result.IsSuccessful)
        {
            throw new AssertionException(CreateViolationMessage(
                "Parsing layer must not depend on CLI",
                "Core domain layer should be independent of user interface concerns, allowing parsing logic to be reused in different contexts (CLI, API, library).",
                result.FailingTypes));
        }
    }

    /// <summary>
    /// Verifies that the Parsing layer does not depend on Providers.
    /// Core parsing logic should be provider-agnostic.
    /// </summary>
    [Test]
    public void Parsing_ShouldNotDependOn_Providers()
    {
        var result = Types.InAssembly(typeof(TerraformPlan).Assembly)
            .That().ResideInNamespace(ParsingNamespace)
            .ShouldNot().HaveDependencyOn(ProvidersNamespace)
            .GetResult();

        if (!result.IsSuccessful)
        {
            throw new AssertionException(CreateViolationMessage(
                "Parsing layer must not depend on Providers",
                "Core parsing logic should be provider-agnostic. Provider-specific handling happens in the Providers layer, which depends on Parsing (not the reverse).",
                result.FailingTypes));
        }
    }

    /// <summary>
    /// Verifies that the Parsing layer does not depend on Platforms.
    /// Core domain should be independent of platform metadata.
    /// Known exemption: TfPlanJsonContext (JSON source generation limitation).
    /// </summary>
    [Test]
    public void Parsing_ShouldNotDependOn_Platforms()
    {
        var result = Types.InAssembly(typeof(TerraformPlan).Assembly)
            .That().ResideInNamespace(ParsingNamespace)
            .And().DoNotHaveNameMatching("TfPlanJsonContext") // Exempt: JSON source generation requires all types in one context (Issue #TBD)
            .ShouldNot().HaveDependencyOn("Oocx.TfPlan2Md.Platforms")
            .GetResult();

        if (!result.IsSuccessful)
        {
            throw new AssertionException(CreateViolationMessage(
                "Parsing layer must not depend on Platforms",
                "Core domain layer should be independent of platform-specific metadata concerns. Exemption: TfPlanJsonContext uses Platforms types for JSON source generation (System.Text.Json limitation).",
                result.FailingTypes));
        }
    }

    /// <summary>
    /// Verifies that the MarkdownGeneration layer does not depend on Providers.
    /// General rendering logic should not depend on specific providers.
    /// </summary>
    [Test]
    public void MarkdownGeneration_ShouldNotDependOn_Providers()
    {
        var result = Types.InAssembly(typeof(TerraformPlan).Assembly)
            .That().ResideInNamespace(MarkdownGenerationNamespace)
            .ShouldNot().HaveDependencyOn(ProvidersNamespace)
            .GetResult();

        if (!result.IsSuccessful)
        {
            throw new AssertionException(CreateViolationMessage(
                "MarkdownGeneration layer must not depend on Providers",
                "General rendering logic should not depend on specific providers. Provider-specific rendering should happen in the Providers layer.",
                result.FailingTypes));
        }
    }

    /// <summary>
    /// Verifies that the CodeAnalysis layer does not depend on MarkdownGeneration.
    /// Static analysis should be independent of rendering concerns.
    /// </summary>
    [Test]
    public void CodeAnalysis_ShouldNotDependOn_MarkdownGeneration()
    {
        var result = Types.InAssembly(typeof(TerraformPlan).Assembly)
            .That().ResideInNamespace("Oocx.TfPlan2Md.CodeAnalysis")
            .ShouldNot().HaveDependencyOn(MarkdownGenerationNamespace)
            .GetResult();

        if (!result.IsSuccessful)
        {
            throw new AssertionException(CreateViolationMessage(
                "CodeAnalysis layer must not depend on MarkdownGeneration",
                "Static analysis results should be independent of rendering concerns, allowing analysis to be used in different contexts.",
                result.FailingTypes));
        }
    }

    /// <summary>
    /// Verifies that the Diagnostics layer has no dependencies on domain layers.
    /// Diagnostics is a cross-cutting concern that should remain independent.
    /// </summary>
    [Test]
    public void Diagnostics_ShouldNotDependOn_AnyLayer()
    {
        var result = Types.InAssembly(typeof(TerraformPlan).Assembly)
            .That().ResideInNamespace("Oocx.TfPlan2Md.Diagnostics")
            .ShouldNot().HaveDependencyOnAny(
                "Oocx.TfPlan2Md.CLI",
                "Oocx.TfPlan2Md.Parsing",
                MarkdownGenerationNamespace,
                "Oocx.TfPlan2Md.Providers",
                "Oocx.TfPlan2Md.Platforms",
                "Oocx.TfPlan2Md.CodeAnalysis")
            .GetResult();

        if (!result.IsSuccessful)
        {
            throw new AssertionException(CreateViolationMessage(
                "Diagnostics layer must not depend on any domain layer",
                "Cross-cutting concerns like diagnostics should not depend on domain layers, ensuring they can be used anywhere without circular dependencies.",
                result.FailingTypes));
        }
    }

    // === LAYER DEPENDENCY RULES (ALLOWED - DOCUMENTATION) ===

    /// <summary>
    /// Documents that the CLI layer depends on RenderTargets for option types.
    /// The top-level orchestration (ProgramEntry) lives in the root namespace
    /// and wires together CLI, Parsing, and MarkdownGeneration.
    /// </summary>
    [Test]
    public void CLI_CanDependOn_RenderTargets()
    {
        // CLI types (CliParser, HelpTextProvider) depend on RenderTargets for
        // RenderTarget enum used in parsed options. Verify at least one CLI type
        // has this dependency (existential check, not universal).
        var result = Types.InAssembly(typeof(TerraformPlan).Assembly)
            .That().ResideInNamespace("Oocx.TfPlan2Md.CLI")
            .ShouldNot().HaveDependencyOn("Oocx.TfPlan2Md.RenderTargets")
            .GetResult();

        // If ShouldNot succeeds, NO CLI types depend on RenderTargets — that's wrong
        if (result.IsSuccessful)
        {
            throw new AssertionException(
                "CLI should depend on RenderTargets (for option types). If this test fails, CLI may have lost its dependency on RenderTargets.");
        }
    }

    /// <summary>
    /// Documents that the MarkdownGeneration layer is allowed to depend on Parsing.
    /// Rendering logic needs access to parsed domain models.
    /// </summary>
    [Test]
    public void MarkdownGeneration_CanDependOn_Parsing()
    {
        // This test documents that MarkdownGeneration SHOULD depend on Parsing.
        // Verify at least one type depends on Parsing (existential check).
        var result = Types.InAssembly(typeof(TerraformPlan).Assembly)
            .That().ResideInNamespace(MarkdownGenerationNamespace)
            .ShouldNot().HaveDependencyOn(ParsingNamespace)
            .GetResult();

        // If ShouldNot succeeds, NO types depend on Parsing — that's wrong
        if (result.IsSuccessful)
        {
            throw new AssertionException(
                "MarkdownGeneration should depend on Parsing (rendering needs parsed data). If this test fails, the architecture may be incorrect.");
        }
    }

    /// <summary>
    /// Documents that the Platforms layer is allowed to depend on MarkdownGeneration.
    /// Platform-specific rendering needs access to general rendering infrastructure.
    /// </summary>
    [Test]
    public void Platforms_CanDependOn_MarkdownGeneration()
    {
        // This test documents that Platforms can depend on MarkdownGeneration.
        // Verify at least one type depends on MarkdownGeneration (existential check).
        var result = Types.InAssembly(typeof(TerraformPlan).Assembly)
            .That().ResideInNamespace("Oocx.TfPlan2Md.Platforms")
            .ShouldNot().HaveDependencyOn(MarkdownGenerationNamespace)
            .GetResult();

        // If ShouldNot succeeds, NO types depend on MarkdownGeneration — that's wrong
        if (result.IsSuccessful)
        {
            throw new AssertionException(
                "Platforms should depend on MarkdownGeneration (platform-specific rendering uses general infrastructure). If this test fails, the architecture may need review.");
        }
    }

    /// <summary>
    /// Documents that the Providers layer is allowed to depend on Parsing and MarkdownGeneration.
    /// Provider-specific rendering extends base rendering capabilities.
    /// </summary>
    [Test]
    public void Providers_CanDependOn_ParsingAndMarkdownGeneration()
    {
        // This test documents that Providers SHOULD depend on both Parsing and MarkdownGeneration.
        // Verify at least one type depends on each (existential check).
        var parsingResult = Types.InAssembly(typeof(TerraformPlan).Assembly)
            .That().ResideInNamespace(ProvidersNamespace)
            .ShouldNot().HaveDependencyOn(ParsingNamespace)
            .GetResult();

        var mdResult = Types.InAssembly(typeof(TerraformPlan).Assembly)
            .That().ResideInNamespace(ProvidersNamespace)
            .ShouldNot().HaveDependencyOn(MarkdownGenerationNamespace)
            .GetResult();

        // If ShouldNot succeeds for either, NO types have the expected dependency
        if (parsingResult.IsSuccessful || mdResult.IsSuccessful)
        {
            throw new AssertionException(
                "Providers should depend on both Parsing and MarkdownGeneration (provider-specific templates extend base rendering). If this test fails, the architecture may be incorrect.");
        }
    }

    /// <summary>
    /// Verifies that architecture boundary exemptions do not increase without an explicit update.
    /// </summary>
    [Test]
    public async Task Architecture_RefactoringDoesNotIncreaseExemptionCount()
    {
        var sourcePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../Architecture/ArchitectureBoundaryTests.cs"));
        var source = await File.ReadAllTextAsync(sourcePath);
        var exemptionCount = source
            .Split('\n')
            .Count(line => line.Contains("// Exempt:", StringComparison.Ordinal)
                && !line.Contains("source", StringComparison.Ordinal));

        await Assert.That(exemptionCount).IsEqualTo(1);
    }

    // === NAMING CONVENTION RULES ===

    /// <summary>
    /// Verifies that all exception classes end with "Exception" suffix.
    /// Standard .NET naming convention for exception types.
    /// </summary>
    [Test]
    public void Exceptions_ShouldHave_ExceptionSuffix()
    {
#pragma warning disable MA0074 // NetArchTest.Rules doesn't support StringComparison parameter
        var result = Types.InAssembly(typeof(TerraformPlan).Assembly)
            .That().Inherit(typeof(Exception))
            .And().ResideInNamespace("Oocx.TfPlan2Md")
            .Should().HaveNameEndingWith("Exception")
            .GetResult();
#pragma warning restore MA0074

        if (!result.IsSuccessful)
        {
            var violations = result.FailingTypes?.Select(t => t.FullName) ?? [];
            throw new AssertionException(
                $"All exception classes must end with 'Exception' suffix (.NET naming convention). Violations: {string.Join(", ", violations)}");
        }
    }

    /// <summary>
    /// Verifies that all test classes end with "Tests" suffix.
    /// Project naming convention for test discoverability.
    /// </summary>
    [Test]
    public void Tests_ShouldHave_TestsSuffix()
    {
#pragma warning disable MA0074 // NetArchTest.Rules doesn't support StringComparison parameter
        var result = Types.InAssembly(typeof(TerraformPlan).Assembly)
            .That().ResideInNamespace("Oocx.TfPlan2Md.TUnit")
            .And().AreClasses()
            .And().DoNotHaveNameMatching("AssemblyInfo")
            .And().DoNotHaveNameMatching(".*Helper")
            .And().DoNotHaveNameMatching(".*Util")
            .And().DoNotHaveNameMatching(".*EntryPoint")          // Exclude test platform entry point
            .And().DoNotHaveNameMatching(".*Extensions")          // Exclude extension registration classes
            .And().DoNotHaveNameMatching(".*Fixture")             // Exclude test fixtures (setup/teardown classes)
            .And().DoNotResideInNamespace("Oocx.TfPlan2Md.TUnit.Assertions")
            .And().DoNotResideInNamespace("Oocx.TfPlan2Md.TUnit.TestData")
            .Should().HaveNameEndingWith("Tests")
            .GetResult();
#pragma warning restore MA0074

        if (!result.IsSuccessful)
        {
            var violations = result.FailingTypes?.Select(t => t.FullName) ?? [];
            throw new AssertionException(
                $"All test classes should end with 'Tests' suffix (project naming convention). Violations: {string.Join(", ", violations)}");
        }
    }

    /// <summary>
    /// Verifies that all interface names start with "I" prefix.
    /// Standard .NET naming convention for interface types.
    /// </summary>
    [Test]
    public void Interfaces_ShouldHave_IPrefix()
    {
#pragma warning disable MA0074 // NetArchTest.Rules doesn't support StringComparison parameter
        var result = Types.InAssembly(typeof(TerraformPlan).Assembly)
            .That().AreInterfaces()
            .And().ResideInNamespace("Oocx.TfPlan2Md")
            .Should().HaveNameStartingWith("I")
            .GetResult();
#pragma warning restore MA0074

        if (!result.IsSuccessful)
        {
            var violations = result.FailingTypes?.Select(t => t.FullName) ?? [];
            throw new AssertionException(
                $"All interface names must start with 'I' prefix (.NET naming convention). Violations: {string.Join(", ", violations)}");
        }
    }

    /// <summary>
    /// Creates a formatted violation message with all required components.
    /// </summary>
    /// <param name="rule">The architectural rule that was violated.</param>
    /// <param name="rationale">Why this rule exists (architectural principle).</param>
    /// <param name="failingTypes">List of types that violate the rule.</param>
    /// <returns>Formatted error message with rule, rationale, violations, guidance link, and ADR reference.</returns>
    private static string CreateViolationMessage(string rule, string rationale, IEnumerable<Type>? failingTypes)
    {
        var violations = failingTypes?.Any() == true
            ? string.Join("\n  - ", failingTypes.Select(t => t.FullName))
            : "(none)";

        return $@"
Architecture Violation: {rule}

Rationale: {rationale}

Violations found in:
  - {violations}

See docs/architecture-rules.md for guidance on architectural boundaries.
Related ADR: docs/adr-007-architecture-boundary-enforcement.md
";
    }
}
