using System.Collections;
using System.Reflection;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Diagnostics;
using TUnit.Core;

namespace Oocx.TfPlan2Md.TUnit.Diagnostics;

/// <summary>
/// Tests focused on diagnostic collection semantics and snapshot immutability.
/// Related feature: docs/features/110-refactoring-opportunities/test-plan.md.
/// </summary>
[Category("Unit")]
public class DiagnosticContextCollectionTests
{
    [Test]
    public void DiagnosticSink_Append_RecordsEventWithoutExposingMutableCollection()
    {
        var context = new DiagnosticContext();
#pragma warning disable CA1859 // The test intentionally exercises the interface boundary.
        IDiagnosticSink sink = context;
#pragma warning restore CA1859
        sink.RecordPrincipalMappingFileProvided("principals.json");
        sink.RecordPrincipalMappingPathStatus(fileExists: true, directoryExists: true);
        sink.RecordPrincipalMappingLoadedSuccessfully();
        sink.RecordFailedResolution(new FailedResolution(
            FailedResolutionType.Principal,
            "principal-1",
            "azurerm_role_assignment.example",
            "not found in mapping file"));
        sink.RecordTemplateResolution(new TemplateResolution("azurerm_resource_group", "Default renderer"));

        var snapshot = context.CreateSnapshot();

        snapshot.PrincipalMappingFileProvided.Should().BeTrue();
        snapshot.PrincipalMappingLoadedSuccessfully.Should().BeTrue();
        snapshot.FailedResolutions.Should().ContainSingle();
        snapshot.TemplateResolutions.Should().ContainSingle();

        var mutablePublicCollections = typeof(DiagnosticContext)
            .GetMembers(BindingFlags.Instance | BindingFlags.Public)
            .Select(member => member switch
            {
                PropertyInfo property when IsMutableCollectionType(property.PropertyType)
                    && property.SetMethod is not null => property.Name,
                FieldInfo field when IsMutableCollectionType(field.FieldType) => field.Name,
                _ => null
            })
            .Where(name => name is not null)
            .ToList();

        mutablePublicCollections.Should().BeEmpty();
    }

    [Test]
    public async Task Format_WithAzdoEntities_IncludesCounts()
    {
        var context = CreateSuccessfulPrincipalMappingContext();
        context.RecordPrincipalEntityCounts(0, 0, 0, 0, 2, 3, 1, 0);

        var markdown = Render(context);

        await Assert.That(markdown).Contains("2 azdo users");
        await Assert.That(markdown).Contains("3 azdo groups");
        await Assert.That(markdown).Contains("1 azdo project");
    }

    [Test]
    public async Task Format_WithSingleAzdoEntity_UsesSingularForm()
    {
        var context = CreateSuccessfulPrincipalMappingContext();
        context.RecordPrincipalEntityCounts(0, 0, 0, 0, 1, 1, 1, 0);

        var markdown = Render(context);

        await Assert.That(markdown).Contains("1 azdo user");
        await Assert.That(markdown).Contains("1 azdo group");
        await Assert.That(markdown).Contains("1 azdo project");
        await Assert.That(markdown).DoesNotContain("1 azdo users");
        await Assert.That(markdown).DoesNotContain("1 azdo groups");
        await Assert.That(markdown).DoesNotContain("1 azdo projects");
    }

    [Test]
    public async Task Format_WithZeroAzdoEntities_OmitsAzdoCounts()
    {
        var markdown = Render(CreateSuccessfulPrincipalMappingContext());

        await Assert.That(markdown).DoesNotContain("azdo user");
        await Assert.That(markdown).DoesNotContain("azdo group");
        await Assert.That(markdown).DoesNotContain("azdo project");
    }

    [Test]
    public void CreateSnapshot_ReturnsImmutableCopiesOfRecordedCollections()
    {
        var context = CreateSuccessfulPrincipalMappingContext();
        context.RecordPrincipalTypeCount("users", 2);
        context.RecordFailedResolution(new FailedResolution(
            FailedResolutionType.Principal,
            "principal-1",
            "azurerm_role_assignment.example",
            "not found in mapping file"));
        context.RecordTemplateResolution(new TemplateResolution("azurerm_virtual_network", "Default template"));

        var snapshot = context.CreateSnapshot();
        context.RecordPrincipalTypeCount("groups", 1);
        context.RecordFailedResolution(new FailedResolution(
            FailedResolutionType.RoleDefinition,
            "role-1",
            "azurerm_role_assignment.reader",
            "not found"));
        context.RecordTemplateResolution(new TemplateResolution("azurerm_resource_group", "Default renderer"));

        snapshot.PrincipalTypeCount.Should().ContainKey("users");
        snapshot.PrincipalTypeCount.Should().NotContainKey("groups");
        snapshot.FailedResolutions.Should().HaveCount(1);
        snapshot.TemplateResolutions.Should().HaveCount(1);
    }

    private static DiagnosticContext CreateSuccessfulPrincipalMappingContext()
    {
        var context = new DiagnosticContext();
        context.RecordPrincipalMappingFileProvided("principals.json");
        context.RecordPrincipalMappingLoadedSuccessfully();
        return context;
    }

    private static string Render(DiagnosticContext context)
    {
        return DiagnosticMarkdownFormatter.Format(context.CreateSnapshot());
    }

    private static bool IsMutableCollectionType(Type type)
    {
        if (type == typeof(string))
        {
            return false;
        }

        return typeof(IList).IsAssignableFrom(type)
            || typeof(IDictionary).IsAssignableFrom(type)
            || (type.IsGenericType && type.GetGenericTypeDefinition() is var genericType
                && (genericType == typeof(List<>) || genericType == typeof(Dictionary<,>)));
    }
}
