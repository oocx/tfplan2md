using System.Linq;
using System.Reflection;
using AwesomeAssertions;
using NetArchTest.Rules;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using TUnit.Core;

namespace Oocx.TfPlan2Md.TUnit.Providers;

/// <summary>
/// Structural guards for the provider contribution model and role-resolution cleanup.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
public class ProviderContributionStructureTests
{
    [Test]
    public void ProviderRegistration_UsesExplicitStaticTypes()
    {
        var result = Types.InAssembly(typeof(TerraformPlan).Assembly)
            .That()
            .HaveNameMatching("ProviderRegistry|ProviderContributionSet|CompositionRoot")
            .ShouldNot()
            .HaveDependencyOnAny("System.Reflection", "System.Runtime.Loader")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypes ?? []));
    }

    [Test]
    public void ProviderModuleContractType_IsRemovedFromProductionAssembly()
    {
        typeof(TerraformPlan).Assembly
            .GetType("Oocx.TfPlan2Md.MarkdownGeneration.Services.IProviderModule")
            .Should()
            .BeNull();
    }

    [Test]
    public void ProviderAndRoleResolutionTypes_HaveNoMutableStaticFields()
    {
        var inspectedTypes = new[]
        {
            typeof(Oocx.TfPlan2Md.Providers.AzApi.AzApiModule),
            typeof(Oocx.TfPlan2Md.Providers.AzureAD.AzureADModule),
            typeof(Oocx.TfPlan2Md.Providers.AzureDevOps.AzureDevOpsModule),
            typeof(Oocx.TfPlan2Md.Providers.AzureRM.AzureRMModule),
            typeof(AzureRoleDefinitionResolver)
        };

#pragma warning disable S3011 // Reflection is intentional here for a structural regression guard.
        var mutableStaticFields = inspectedTypes
            .SelectMany(type => type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field => !field.IsInitOnly)
                .Select(field => $"{type.Name}.{field.Name}"))
            .ToList();
#pragma warning restore S3011

        mutableStaticFields.Should().BeEmpty();
    }
}
