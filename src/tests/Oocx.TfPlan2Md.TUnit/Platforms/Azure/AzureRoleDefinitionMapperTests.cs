using System.Collections.Generic;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Platforms.Azure;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Platforms.Azure;

/// <summary>
/// Tests for mapping Azure role definition identifiers.
/// Related feature: docs/features/025-azure-role-definition-mapping/specification.md.
/// </summary>
public class AzureRoleDefinitionMapperTests
{
    /// <summary>
    /// Verifies null IDs fall back to the provided role name.
    /// </summary>
    [Test]
    public void GetRoleDefinition_NullId_UsesFallbackName()
    {
        var info = AzureRoleDefinitionMapper.GetRoleDefinition(null, "Custom Role");

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

        var info = AzureRoleDefinitionMapper.GetRoleDefinition(roleId, null);

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

        var info = AzureRoleDefinitionMapper.GetRoleDefinition(roleId, null);

        info.Name.Should().Be("Owner");
        info.Id.Should().Be("8e3af657-a8ff-443c-a75c-2fe8c4bcb635");
        info.FullName.Should().Be("Owner (8e3af657-a8ff-443c-a75c-2fe8c4bcb635)");
    }

    /// <summary>
    /// Verifies custom role mappings are resolved when provided.
    /// </summary>
    [Test]
    public void MergeCustomRoles_CustomRoleGuid_UsesMappedName()
    {
        var roles = new List<MappingEntry>
        {
            new("11111111-1111-1111-1111-111111111111", "Custom Role")
        };

        try
        {
            AzureRoleDefinitionMapper.MergeCustomRoles(roles);

            var info = AzureRoleDefinitionMapper.GetRoleDefinition("11111111-1111-1111-1111-111111111111", null);

            info.Name.Should().Be("Custom Role");
            info.Id.Should().Be("11111111-1111-1111-1111-111111111111");
            info.FullName.Should().Be("Custom Role (11111111-1111-1111-1111-111111111111)");
        }
        finally
        {
            AzureRoleDefinitionMapper.MergeCustomRoles(Array.Empty<MappingEntry>());
        }
    }

    /// <summary>
    /// Verifies custom role mappings override built-in roles.
    /// </summary>
    [Test]
    public void MergeCustomRoles_BuiltInRole_IsOverriddenByCustomMapping()
    {
        var roles = new List<MappingEntry>
        {
            new("8e3af657-a8ff-443c-a75c-2fe8c4bcb635", "Full Owner")
        };

        try
        {
            AzureRoleDefinitionMapper.MergeCustomRoles(roles);

            var info = AzureRoleDefinitionMapper.GetRoleDefinition("8e3af657-a8ff-443c-a75c-2fe8c4bcb635", null);

            info.Name.Should().Be("Full Owner");
            info.Id.Should().Be("8e3af657-a8ff-443c-a75c-2fe8c4bcb635");
            info.FullName.Should().Be("Full Owner (8e3af657-a8ff-443c-a75c-2fe8c4bcb635)");
        }
        finally
        {
            AzureRoleDefinitionMapper.MergeCustomRoles(Array.Empty<MappingEntry>());
        }
    }

    /// <summary>
    /// Verifies unknown IDs fall back to the raw value.
    /// </summary>
    [Test]
    public void GetRoleDefinition_UnknownId_UsesRawValue()
    {
        var info = AzureRoleDefinitionMapper.GetRoleDefinition("unknown-role", null);

        info.Name.Should().Be("unknown-role");
        info.Id.Should().Be("unknown-role");
        info.FullName.Should().Be("unknown-role");
    }
}
