using System.Linq;
using AwesomeAssertions;
using NetArchTest.Rules;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Architecture;

/// <summary>
/// Architectural guards for Feature 122 (Terraform 1.14/1.15 plan-JSON support).
/// Asserts that the action-invocation rendering pipeline contains no provider-specific
/// renderers, in line with the hard-stop rule documented in the developer agent and
/// docs/features/122-terraform-1-15-support/architecture.md (Task 16).
/// </summary>
public class Feature122ArchitectureTests
{
    [Test]
    public void NoProviderSpecificActionRenderer_ExistsInProductionAssembly()
    {
        var assembly = typeof(TerraformPlan).Assembly;

        var offending = Types.InAssembly(assembly)
            .That()
            .ResideInNamespaceStartingWith("Oocx.TfPlan2Md.Providers")
            .And()
            .HaveNameEndingWith("ActionRenderer", System.StringComparison.Ordinal)
            .GetTypes()
            .ToList();

        offending.Should().BeEmpty(
            "actions must be rendered by the generic ActionInvocationSectionRenderer; provider-specific action renderers violate the architecture (see docs/features/122-terraform-1-15-support/architecture.md).");
    }

    [Test]
    public void GenericActionInvocationSectionRenderer_LivesInRenderingNamespace()
    {
        typeof(ActionInvocationSectionRenderer).Namespace
            .Should().Be("Oocx.TfPlan2Md.MarkdownGeneration.Rendering");
    }
}
