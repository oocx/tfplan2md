using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Platforms.Azure;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Platforms.Azure;

/// <summary>
/// Tests for the run-scoped Azure role definition resolver.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
public class AzureRoleDefinitionResolverTests
{
    /// <summary>
    /// Verifies null IDs fall back to the provided role name.
    /// </summary>
    [Test]
    public void GetRoleDefinition_NullId_UsesFallbackName()
    {
        var resolver = new AzureRoleDefinitionResolver(Array.Empty<MappingEntry>());

        var info = resolver.GetRoleDefinition(null, "Custom Role");

        info.Name.Should().Be("Custom Role");
        info.Id.Should().BeEmpty();
        info.FullName.Should().Be("Custom Role");
    }

    /// <summary>
    /// Verifies known role IDs are mapped to display names.
    /// </summary>
    [Test]
    public void GetRoleDefinition_KnownId_UsesMappedName()
    {
        var roleId = "/subscriptions/sub-one/providers/Microsoft.Authorization/roleDefinitions/acdd72a7-3385-48ef-bd42-f606fba81ae7";
        var resolver = new AzureRoleDefinitionResolver(Array.Empty<MappingEntry>());

        var info = resolver.GetRoleDefinition(roleId, null);

        info.Name.Should().Be("Reader");
        info.Id.Should().Be("acdd72a7-3385-48ef-bd42-f606fba81ae7");
        info.FullName.Should().Be("Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7)");
    }

    /// <summary>
    /// Verifies the built-in Owner role resolves correctly.
    /// </summary>
    [Test]
    public void GetRoleDefinition_BuiltInOwnerGuid_UsesMappedName()
    {
        var roleId = "/subscriptions/sub-one/providers/Microsoft.Authorization/roleDefinitions/8e3af657-a8ff-443c-a75c-2fe8c4bcb635";
        var resolver = new AzureRoleDefinitionResolver(Array.Empty<MappingEntry>());

        var info = resolver.GetRoleDefinition(roleId, null);

        info.Name.Should().Be("Owner");
        info.Id.Should().Be("8e3af657-a8ff-443c-a75c-2fe8c4bcb635");
        info.FullName.Should().Be("Owner (8e3af657-a8ff-443c-a75c-2fe8c4bcb635)");
    }

    /// <summary>
    /// Verifies built-in role definitions remain identical across resolver instances.
    /// </summary>
    [Test]
    public void GetRoleDefinition_BuiltInRoles_AreImmutableAcrossInstances()
    {
        const string builtInRoleId = "acdd72a7-3385-48ef-bd42-f606fba81ae7";
        var resolverWithoutCustomRoles = new AzureRoleDefinitionResolver(Array.Empty<MappingEntry>());
        var resolverWithCustomRoles = new AzureRoleDefinitionResolver(
        [
            new MappingEntry("11111111-1111-1111-1111-111111111111", "Custom Role")
        ]);

        var first = resolverWithoutCustomRoles.GetRoleDefinition(builtInRoleId, null);
        var second = resolverWithCustomRoles.GetRoleDefinition(builtInRoleId, null);

        first.Name.Should().Be("Reader");
        second.Name.Should().Be(first.Name);
        second.FullName.Should().Be(first.FullName);
        second.Id.Should().Be(first.Id);
    }

    /// <summary>
    /// Verifies custom role mappings are resolved when provided.
    /// </summary>
    [Test]
    public void GetRoleDefinition_CustomRoleGuid_UsesMappedName()
    {
        var roles = new List<MappingEntry>
        {
            new("11111111-1111-1111-1111-111111111111", "Custom Role")
        };
        var resolver = new AzureRoleDefinitionResolver(roles);

        var info = resolver.GetRoleDefinition("11111111-1111-1111-1111-111111111111", null);

        info.Name.Should().Be("Custom Role");
        info.Id.Should().Be("11111111-1111-1111-1111-111111111111");
        info.FullName.Should().Be("Custom Role (11111111-1111-1111-1111-111111111111)");
    }

    /// <summary>
    /// Verifies custom role mappings override built-in roles.
    /// </summary>
    [Test]
    public void GetRoleDefinition_BuiltInRole_IsOverriddenByCustomMapping()
    {
        var roles = new List<MappingEntry>
        {
            new("8e3af657-a8ff-443c-a75c-2fe8c4bcb635", "Full Owner")
        };
        var resolver = new AzureRoleDefinitionResolver(roles);

        var info = resolver.GetRoleDefinition("8e3af657-a8ff-443c-a75c-2fe8c4bcb635", null);

        info.Name.Should().Be("Full Owner");
        info.Id.Should().Be("8e3af657-a8ff-443c-a75c-2fe8c4bcb635");
        info.FullName.Should().Be("Full Owner (8e3af657-a8ff-443c-a75c-2fe8c4bcb635)");
    }

    /// <summary>
    /// Verifies unknown IDs fall back to the raw value.
    /// </summary>
    [Test]
    public void GetRoleDefinition_UnknownId_UsesRawValue()
    {
        var resolver = new AzureRoleDefinitionResolver(Array.Empty<MappingEntry>());

        var info = resolver.GetRoleDefinition("unknown-role", null);

        info.Name.Should().Be("unknown-role");
        info.Id.Should().Be("unknown-role");
        info.FullName.Should().Be("unknown-role");
    }

    /// <summary>
    /// Verifies custom role mappings are scoped to the resolver instance that owns them.
    /// </summary>
    [Test]
    public void GetRoleDefinition_CustomRoles_AreScopedPerResolverInstance()
    {
        var customResolver = new AzureRoleDefinitionResolver(
        [
            new MappingEntry("8e3af657-a8ff-443c-a75c-2fe8c4bcb635", "Full Owner")
        ]);
        var builtInResolver = new AzureRoleDefinitionResolver(Array.Empty<MappingEntry>());

        var customInfo = customResolver.GetRoleDefinition("8e3af657-a8ff-443c-a75c-2fe8c4bcb635", null);
        var builtInInfo = builtInResolver.GetRoleDefinition("8e3af657-a8ff-443c-a75c-2fe8c4bcb635", null);

        customInfo.Name.Should().Be("Full Owner");
        builtInInfo.Name.Should().Be("Owner");
    }

    /// <summary>
    /// Verifies the resolver type does not hold mutable static state.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Major Code Smell",
        "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields",
        Justification = "This structural regression test intentionally inspects static field metadata.")]
    [Test]
    public void AzureRoleDefinitionResolver_HasNoMutableStaticFields()
    {
        var mutableStaticFields = typeof(AzureRoleDefinitionResolver)
            .GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(field => !field.IsInitOnly)
            .ToList();

        mutableStaticFields.Should().BeEmpty();
    }
}
