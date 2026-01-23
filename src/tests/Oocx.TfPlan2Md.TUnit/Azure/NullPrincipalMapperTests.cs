using System.Collections.Generic;
using System.Text.Json;
using Oocx.TfPlan2Md.Azure;
using TUnit.Assertions;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Azure;

/// <summary>
/// Verifies null principal mapping behavior and serialization context support.
/// Related feature: docs/features/024-azure-principal-mapping/specification.md.
/// </summary>
public class NullPrincipalMapperTests
{
    /// <summary>
    /// Ensures the null mapper returns principal identifiers unchanged and never resolves names.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Null_mapper_passthroughs_and_returns_null()
    {
        var mapper = new NullPrincipalMapper();
        const string principalId = "12345678-1234-1234-1234-123456789abc";

        await Assert.That(mapper.GetPrincipalName(principalId)).IsEqualTo(principalId);
        await Assert.That(mapper.GetPrincipalName(principalId, "User", "/subscriptions/sub/resourceGroups/rg")).IsEqualTo(principalId);
        await Assert.That(mapper.GetName(principalId)).IsNull();
        await Assert.That(mapper.GetName(principalId, "Group", "/subscriptions/sub/resourceGroups/rg/providers/Microsoft.Resources/resourceGroups/rg"))
            .IsNull();
    }

    /// <summary>
    /// Validates that the source-generated JSON serialization context can round-trip role definition dictionaries.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Azure_role_definition_context_round_trips_dictionary()
    {
        var definitions = new Dictionary<string, string>
        {
            ["reader"] = "Reader",
        };

        var json = JsonSerializer.Serialize(definitions, AzureRoleDefinitionsJsonContext.Default.DictionaryStringString);
        var roundTrip = JsonSerializer.Deserialize(json, AzureRoleDefinitionsJsonContext.Default.DictionaryStringString);

        await Assert.That(roundTrip).IsNotNull();
        await Assert.That(roundTrip).ContainsKey("reader");
        await Assert.That(roundTrip?["reader"]).IsEqualTo("Reader");
    }

    /// <summary>
    /// Ensures the principal info record preserves all provided fields.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Principal_info_exposes_provided_values()
    {
        var info = new PrincipalInfo("Alice", "id-123", "User", "User: Alice (id-123)");

        await Assert.That(info.Name).IsEqualTo("Alice");
        await Assert.That(info.Id).IsEqualTo("id-123");
        await Assert.That(info.Type).IsEqualTo("User");
        await Assert.That(info.FullName).IsEqualTo("User: Alice (id-123)");
    }
}
