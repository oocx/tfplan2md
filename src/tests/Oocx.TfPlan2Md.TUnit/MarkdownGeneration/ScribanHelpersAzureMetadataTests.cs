using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Scriban.Runtime;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests Azure metadata helpers exposed to Scriban templates.
/// </summary>
public class ScribanHelpersAzureMetadataTests
{
    /// <summary>
    /// Ensures scope info includes expected parsed fields.
    /// </summary>
    [Test]
    public async Task GetScopeInfo_ReturnsParsedFields()
    {
        var method = GetHelperMethod("GetScopeInfo");
        var scope = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg/providers/Microsoft.KeyVault/vaults/kv1";

        var result = (ScriptObject?)method.Invoke(null, new object?[] { scope });

        result.Should().NotBeNull();
        result!["name"].Should().Be("kv1");
        result["type"].Should().Be("Key Vault");
        result["subscription_id"].Should().Be("00000000-0000-0000-0000-000000000000");
        result["resource_group"].Should().Be("rg");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Ensures role info includes mapped role names when IDs are known.
    /// </summary>
    [Test]
    public async Task GetRoleInfo_ReturnsMappedRoleData()
    {
        var method = GetHelperMethod("GetRoleInfo");
        var roleId = "/subscriptions/00000000-0000-0000-0000-000000000000/providers/Microsoft.Authorization/roleDefinitions/acdd72a7-3385-48ef-bd42-f606fba81ae7";

        var result = (ScriptObject?)method.Invoke(null, new object?[] { roleId, null });

        result.Should().NotBeNull();
        result!["name"].Should().Be("Reader");
        result["id"].Should().Be("acdd72a7-3385-48ef-bd42-f606fba81ae7");
        result["full_name"].Should().BeOfType<string>().Which.Should().Contain("Reader");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets a private Scriban helper method for reflection-based tests.
    /// </summary>
    /// <param name="name">Method name.</param>
    /// <returns>The reflection method info.</returns>
    [SuppressMessage("Major Code Smell", "S3011", Justification = "Testing private Scriban helpers via reflection.")]
    private static MethodInfo GetHelperMethod(string name)
    {
        var method = typeof(ScribanHelpers).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        return method!;
    }
}
