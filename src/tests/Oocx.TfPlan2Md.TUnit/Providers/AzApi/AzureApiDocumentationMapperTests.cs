using AwesomeAssertions;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzApi;

/// <summary>
/// Unit tests for AzureApiDocumentationMapper.
/// Related feature: docs/features/048-azure-api-doc-mapping/test-plan.md.
/// </summary>
[Category("Unit")]
public class AzureApiDocumentationMapperTests
{
    #region TC-04: Known Resource Type Returns Correct URL

    [Test]
    public async Task GetDocumentationUrl_KnownResourceType_ReturnsCorrectUrl()
    {
        // Arrange - TC-04: Multiple known types from mappings
        var testCases = new[]
        {
            ("Microsoft.Compute/virtualMachines", "https://learn.microsoft.com/rest/api/compute/virtual-machines"),
            ("Microsoft.Storage/storageAccounts", "https://learn.microsoft.com/rest/api/storagerp/storage-accounts"),
            ("Microsoft.Network/virtualNetworks", "https://learn.microsoft.com/rest/api/virtualnetwork/virtual-networks")
        };

        foreach (var (resourceType, expectedUrl) in testCases)
        {
            // Act
            var url = Oocx.TfPlan2Md.Providers.AzApi.AzureApiDocumentationMapper.GetDocumentationUrl(resourceType);

            // Assert
            url.Should().NotBeNull($"Mapping should exist for {resourceType}");
            url.Should().Be(expectedUrl, $"URL should match for {resourceType}");
        }

        await Task.CompletedTask;
    }

    #endregion

    #region TC-05: Unknown Resource Type Returns Null

    [Test]
    public async Task GetDocumentationUrl_UnknownResourceType_ReturnsNull()
    {
        // Arrange - TC-05: Unknown resource types
        var unknownTypes = new[]
        {
            "Microsoft.UnknownService/unknownResource",
            "Microsoft.FakeService/fakeResource@2023-01-01",
            "Microsoft.Invalid/resource"
        };

        foreach (var resourceType in unknownTypes)
        {
            // Act
            var url = Oocx.TfPlan2Md.Providers.AzApi.AzureApiDocumentationMapper.GetDocumentationUrl(resourceType);

            // Assert
            url.Should().BeNull($"Unknown resource type '{resourceType}' should return null");
        }

        await Task.CompletedTask;
    }

    #endregion

    #region TC-06: API Version Suffix is Stripped Before Lookup

    [Test]
    public async Task GetDocumentationUrl_ResourceTypeWithVersion_StripsVersionAndReturnsUrl()
    {
        // Arrange - TC-06: Resource types with API versions
        var testCases = new[]
        {
            ("Microsoft.Compute/virtualMachines@2023-03-01", "Microsoft.Compute/virtualMachines"),
            ("Microsoft.Storage/storageAccounts@2021-06-01", "Microsoft.Storage/storageAccounts"),
            ("Microsoft.Network/virtualNetworks@2020-11-01", "Microsoft.Network/virtualNetworks")
        };

        foreach (var (resourceTypeWithVersion, expectedLookupKey) in testCases)
        {
            // Act
            var urlWithVersion = Oocx.TfPlan2Md.Providers.AzApi.AzureApiDocumentationMapper.GetDocumentationUrl(resourceTypeWithVersion);
            var urlWithoutVersion = Oocx.TfPlan2Md.Providers.AzApi.AzureApiDocumentationMapper.GetDocumentationUrl(expectedLookupKey);

            // Assert
            urlWithVersion.Should().NotBeNull($"Should find mapping for {resourceTypeWithVersion}");
            urlWithVersion.Should().Be(urlWithoutVersion, $"Version suffix should be stripped for {resourceTypeWithVersion}");
        }

        await Task.CompletedTask;
    }

    #endregion

    #region TC-07: Non-Microsoft Providers Return Null

    [Test]
    public async Task GetDocumentationUrl_NonMicrosoftProvider_ReturnsNull()
    {
        // Arrange - TC-07: Non-Microsoft providers
        var nonMicrosoftProviders = new[]
        {
            "HashiCorp.RandomProvider/randomString",
            "Custom.Provider/customResource",
            "Terraform.LocalProvider/localFile",
            "aws_instance"
        };

        foreach (var resourceType in nonMicrosoftProviders)
        {
            // Act
            var url = Oocx.TfPlan2Md.Providers.AzApi.AzureApiDocumentationMapper.GetDocumentationUrl(resourceType);

            // Assert
            url.Should().BeNull($"Non-Microsoft provider '{resourceType}' should return null");
        }

        await Task.CompletedTask;
    }

    #endregion

    #region TC-10: Edge Cases Handled Gracefully

    [Test]
    public async Task GetDocumentationUrl_EdgeCases_ReturnsNull()
    {
        // Arrange - TC-10: Edge cases (null, empty, whitespace, malformed)
        var edgeCases = new[]
        {
            null,
            "",
            "   ",
            "invalid-format",
            "no-slash-format",
            "@version-only",
            "Microsoft.Only"
        };

        foreach (var resourceType in edgeCases)
        {
            // Act
            var url = Oocx.TfPlan2Md.Providers.AzApi.AzureApiDocumentationMapper.GetDocumentationUrl(resourceType);

            // Assert
            url.Should().BeNull($"Edge case '{resourceType ?? "(null)"}' should return null");
        }

        await Task.CompletedTask;
    }

    #endregion

    #region TC-20: Nested Resource Types Resolve Individually

    [Test]
    public async Task GetDocumentationUrl_NestedResourceTypes_ResolvesIndividually()
    {
        // Arrange - TC-20: Nested resources have separate mappings
        var parentType = "Microsoft.Storage/storageAccounts";
        var childType = "Microsoft.Storage/storageAccounts/blobServices";
        var grandchildType = "Microsoft.Storage/storageAccounts/blobServices/containers";

        // Act
        var parentUrl = Oocx.TfPlan2Md.Providers.AzApi.AzureApiDocumentationMapper.GetDocumentationUrl(parentType);
        var childUrl = Oocx.TfPlan2Md.Providers.AzApi.AzureApiDocumentationMapper.GetDocumentationUrl(childType);
        var grandchildUrl = Oocx.TfPlan2Md.Providers.AzApi.AzureApiDocumentationMapper.GetDocumentationUrl(grandchildType);

        // Assert
        parentUrl.Should().NotBeNull("Parent resource should have mapping");
        childUrl.Should().NotBeNull("Child resource should have mapping");
        grandchildUrl.Should().NotBeNull("Grandchild resource should have mapping");

        parentUrl.Should().NotBe(childUrl, "Parent and child should have different URLs");
        childUrl.Should().NotBe(grandchildUrl, "Child and grandchild should have different URLs");

        await Task.CompletedTask;
    }

    #endregion

    #region TC-17: Case-Insensitive Lookup

    [Test]
    public async Task GetDocumentationUrl_CaseVariations_ReturnsSameUrl()
    {
        // Arrange - Case variations of the same resource type
        var variations = new[]
        {
            "Microsoft.Compute/virtualMachines",
            "microsoft.compute/virtualmachines",
            "MICROSOFT.COMPUTE/VIRTUALMACHINES",
            "Microsoft.compute/VirtualMachines"
        };

        // Act & Assert
        string? firstUrl = null;
        foreach (var variation in variations)
        {
            var url = Oocx.TfPlan2Md.Providers.AzApi.AzureApiDocumentationMapper.GetDocumentationUrl(variation);

            if (firstUrl is null)
            {
                firstUrl = url;
                url.Should().NotBeNull("First variation should have mapping");
            }
            else
            {
                url.Should().Be(firstUrl, $"Case variation '{variation}' should return same URL as first variation");
            }
        }

        await Task.CompletedTask;
    }

    #endregion

    #region TC-01 & TC-02: JSON Structure and Coverage

    [Test]
    public async Task Mappings_LoadedSuccessfully_ContainsExpectedEntries()
    {
        // Arrange - TC-01 & TC-02: Verify mappings loaded and contain expected entries
        var expectedTypes = new[]
        {
            "Microsoft.Compute/virtualMachines",
            "Microsoft.Storage/storageAccounts",
            "Microsoft.Network/virtualNetworks",
            "Microsoft.KeyVault/vaults"
        };

        foreach (var resourceType in expectedTypes)
        {
            // Act
            var url = Oocx.TfPlan2Md.Providers.AzApi.AzureApiDocumentationMapper.GetDocumentationUrl(resourceType);

            // Assert
            url.Should().NotBeNull($"Common resource type '{resourceType}' should have mapping");
            url.Should().StartWith("https://learn.microsoft.com/rest/api/", $"URL for '{resourceType}' should be from Microsoft Learn");
        }

        await Task.CompletedTask;
    }

    #endregion

    #region TC-03: Mapping URLs Follow Expected Patterns

    [Test]
    public async Task Mappings_AllUrls_FollowExpectedPattern()
    {
        // Arrange - TC-03: Sample of resource types to verify URL pattern
        var sampleTypes = new[]
        {
            "Microsoft.Compute/virtualMachines",
            "Microsoft.Storage/storageAccounts",
            "Microsoft.Network/virtualNetworks",
            "Microsoft.Web/sites",
            "Microsoft.Sql/servers"
        };

        foreach (var resourceType in sampleTypes)
        {
            // Act
            var url = Oocx.TfPlan2Md.Providers.AzApi.AzureApiDocumentationMapper.GetDocumentationUrl(resourceType);

            // Assert
            if (url is not null) // Only check if mapping exists
            {
                url.Should().StartWith("https://learn.microsoft.com/rest/api/", $"URL for '{resourceType}' should start with correct prefix");
                url.Should().NotContain("?", $"URL for '{resourceType}' should not contain query parameters");
            }
        }

        await Task.CompletedTask;
    }

    #endregion
}
