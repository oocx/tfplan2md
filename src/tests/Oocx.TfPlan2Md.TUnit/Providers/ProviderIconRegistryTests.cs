using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzApi;
using Oocx.TfPlan2Md.Providers.AzureAD;
using Oocx.TfPlan2Md.Providers.AzureDevOps;
using Oocx.TfPlan2Md.Providers.AzureRM;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers;

/// <summary>
/// Validates provider icon registration for core semantic attributes.
/// </summary>
public class ProviderIconRegistryTests
{
    /// <summary>
    /// Ensures AzureRM registers subscription icons.
    /// </summary>
    [Test]
    public void AzureRmModule_RegisterIconProviders_RegistersSubscriptionIcon()
    {
        var registry = new IconProviderRegistry();
        var module = new AzureRMModule(LargeValueFormat.SimpleDiff, new NullPrincipalMapper());

        module.RegisterIconProviders(registry);

        var icon = registry.TryGetIcon(new ServiceResolutionContext("azurerm", null, "subscription_id", "0000"));

        icon.Should().Be("🔑");
    }

    /// <summary>
    /// Ensures AzureRM registers access icons.
    /// </summary>
    [Test]
    public void AzureRmModule_RegisterIconProviders_RegistersAccessIcon()
    {
        var registry = new IconProviderRegistry();
        var module = new AzureRMModule(LargeValueFormat.SimpleDiff, new NullPrincipalMapper());

        module.RegisterIconProviders(registry);

        var icon = registry.TryGetIcon(new ServiceResolutionContext("azurerm", null, "access", "Allow"));

        icon.Should().Be("✅");
    }

    /// <summary>
    /// Ensures AzApi registers location icons.
    /// </summary>
    [Test]
    public void AzApiModule_RegisterIconProviders_RegistersLocationIcon()
    {
        var registry = new IconProviderRegistry();
        var module = new AzApiModule();

        module.RegisterIconProviders(registry);

        var icon = registry.TryGetIcon(new ServiceResolutionContext("azapi", null, "location", "westeurope"));

        icon.Should().Be("🌍");
    }

    /// <summary>
    /// Ensures AzApi registers protocol icons.
    /// </summary>
    [Test]
    public void AzApiModule_RegisterIconProviders_RegistersProtocolIcon()
    {
        var registry = new IconProviderRegistry();
        var module = new AzApiModule();

        module.RegisterIconProviders(registry);

        var icon = registry.TryGetIcon(new ServiceResolutionContext("azapi", null, "protocol", "tcp"));

        icon.Should().Be("🔗");
    }

    /// <summary>
    /// Ensures Azure AD registers identity icons.
    /// </summary>
    [Test]
    public void AzureAdModule_RegisterIconProviders_RegistersDisplayNameIcon()
    {
        var registry = new IconProviderRegistry();
        var module = new AzureADModule();

        module.RegisterIconProviders(registry);

        var icon = registry.TryGetIcon(new ServiceResolutionContext("azuread", "azuread_user", "display_name", "User"));

        icon.Should().Be("👤");
    }

    /// <summary>
    /// Ensures Azure DevOps registers change icons.
    /// </summary>
    [Test]
    public void AzureDevOpsModule_RegisterIconProviders_RegistersChangeIcon()
    {
        var registry = new IconProviderRegistry();
        var module = new AzureDevOpsModule(LargeValueFormat.SimpleDiff);

        module.RegisterIconProviders(registry);

        var icon = registry.TryGetIcon(new ServiceResolutionContext("azuredevops", "azuredevops_variable_group", "change", "add"));

        icon.Should().Be("➕");
    }
}
