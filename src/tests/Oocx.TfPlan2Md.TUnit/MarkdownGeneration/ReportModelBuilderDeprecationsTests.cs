using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Builder tests for the Terraform 1.15+ deprecation pipeline (ADR-004).
/// Verifies referenced-only filtering, source tagging, and unification with the
/// existing SARIF Warnings list.
/// Related feature: docs/features/122-terraform-1-15-support/adr-004-deprecation-warnings-via-existing-pipeline.md (Task 13).
/// </summary>
public class ReportModelBuilderDeprecationsTests
{
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement;

    private static TerraformPlan MakePlan(string configurationJson, IReadOnlyDictionary<string, JsonElement>? variables = null, IReadOnlyDictionary<string, OutputChange>? outputChanges = null)
    {
        return new TerraformPlan(
            "1.2",
            "1.15.0",
            Array.Empty<ResourceChange>(),
            OutputChanges: outputChanges,
            Configuration: Json(configurationJson),
            Variables: variables);
    }

    [Test]
    public void Build_DeprecatedReferencedVariable_EmittedAsPlanDeprecationWarning()
    {
        var plan = MakePlan(
            "{\"root_module\":{\"variables\":{\"old_var\":{\"deprecated\":\"use new_var instead\"}}}}",
            variables: new Dictionary<string, JsonElement> { ["old_var"] = Json("\"value\"") });

        var model = new ReportModelBuilder().Build(plan);

        model.CodeAnalysis.Should().NotBeNull();
        var warning = model.CodeAnalysis!.Warnings.Should().ContainSingle().Subject;
        warning.Source.Should().Be(CodeAnalysisWarningSource.PlanDeprecation);
        warning.SubjectKind.Should().Be("variable");
        warning.SubjectName.Should().Be("old_var");
        warning.Message.Should().Be("use new_var instead");
        warning.FilePath.Should().BeNull();
    }

    [Test]
    public void Build_DeprecatedUnreferencedVariable_Suppressed()
    {
        var plan = MakePlan(
            "{\"root_module\":{\"variables\":{\"unused\":{\"deprecated\":\"do not use\"}}}}");

        var model = new ReportModelBuilder().Build(plan);

        model.CodeAnalysis.Should().BeNull();
    }

    [Test]
    public void Build_DeprecatedReferencedOutput_EmittedAsPlanDeprecationWarning()
    {
        var plan = MakePlan(
            "{\"root_module\":{\"outputs\":{\"old_out\":{\"deprecated\":\"renamed\"}}}}",
            outputChanges: new Dictionary<string, OutputChange>
            {
                ["old_out"] = new OutputChange(["create"], null, "value", null, null, null)
            });

        var model = new ReportModelBuilder().Build(plan);

        model.CodeAnalysis.Should().NotBeNull();
        var warning = model.CodeAnalysis!.Warnings.Should().ContainSingle().Subject;
        warning.SubjectKind.Should().Be("output");
        warning.SubjectName.Should().Be("old_out");
    }
}
