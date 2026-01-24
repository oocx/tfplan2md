using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Platforms.Azure;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for Azure scope formatting helpers to ensure template-friendly output across scope levels.
/// </summary>
public class ScribanHelpersAzureScopeFormattingTests
{
    [Test]
    public async Task FormatAzureScopeForTable_WhenResourceGroup_ReturnsGroupWithSubscription()
    {
        var scope = new ScopeInfo("rg1", "Microsoft.Resources/resourceGroups", "sub1", "rg1", ScopeLevel.ResourceGroup, "summary", "summary:", "rg1", "details");

        var result = ScribanHelpers.FormatAzureScopeForTable(scope);

        result.Should().Contain("rg1");
        result.Should().Contain("subscription");
        result.Should().Contain("sub1");

        await Task.CompletedTask;
    }

    [Test]
    public async Task FormatAzureScopeForTable_WhenResourceWithResourceGroup_ReturnsResourceWithContext()
    {
        var scope = new ScopeInfo("kv1", "Microsoft.KeyVault/vaults", "sub1", "rg1", ScopeLevel.Resource, "summary", "summary:", "kv1", "details");

        var result = ScribanHelpers.FormatAzureScopeForTable(scope);

        result.Should().Contain("Microsoft.KeyVault/vaults");
        result.Should().Contain("kv1");
        result.Should().Contain("rg1");
        result.Should().Contain("sub1");

        await Task.CompletedTask;
    }

    [Test]
    public async Task FormatAzureScopeForTable_WhenResourceWithoutResourceGroup_UsesSubscriptionOnly()
    {
        var scope = new ScopeInfo("vault", "Microsoft.KeyVault/vaults", "sub-only", string.Empty, ScopeLevel.Resource, "summary", "summary:", "vault", "details");

        var result = ScribanHelpers.FormatAzureScopeForTable(scope);

        result.Should().Contain("vault");
        result.Should().Contain("Microsoft.KeyVault/vaults");
        result.Should().Contain("sub-only");

        await Task.CompletedTask;
    }

    [Test]
    public async Task FormatAzureScopeForTable_WhenSubscription_ReturnsSubscriptionText()
    {
        var scope = new ScopeInfo("sub-name", "subscription", "sub-123", null, ScopeLevel.Subscription, "summary", "summary:", "sub-name", "details");

        var result = ScribanHelpers.FormatAzureScopeForTable(scope);

        result.Should().Contain("subscription");
        result.Should().Contain("sub-123");

        await Task.CompletedTask;
    }

    [Test]
    public async Task FormatAzureScopeForTable_WhenManagementGroup_AppendsLabel()
    {
        var scope = new ScopeInfo("mgmt", "mg", null, null, ScopeLevel.ManagementGroup, "summary", "summary:", "mgmt", "details");

        var result = ScribanHelpers.FormatAzureScopeForTable(scope);

        result.Should().Contain("mgmt");
        result.Should().Contain("Management Group");

        await Task.CompletedTask;
    }

    [Test]
    public async Task FormatAzureScopeForTable_WhenUnknownLevel_ReturnsDetails()
    {
        var scope = new ScopeInfo("name", "type", null, null, ScopeLevel.Unknown, "summary", "summary:", "name", "detail_with_special*_chars");

        var result = ScribanHelpers.FormatAzureScopeForTable(scope);

        result.Should().Be("detail_with_special*_chars");

        await Task.CompletedTask;
    }
}
