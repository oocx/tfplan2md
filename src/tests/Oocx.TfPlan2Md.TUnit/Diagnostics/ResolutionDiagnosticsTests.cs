using System;
using System.Collections.Generic;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Diagnostics;
using Oocx.TfPlan2Md.Platforms.Azure;
using TUnit.Core;

namespace Oocx.TfPlan2Md.TUnit.Diagnostics;

/// <summary>
/// Tests for recording failed ID resolutions with diagnostic context.
/// Related feature: docs/features/065-tenant-display-mapping/specification.md.
/// </summary>
public class ResolutionDiagnosticsTests
{
    private const string RoleAssignmentAddress = "azurerm_role_assignment.example";
    private const string ResourceGroupAddress = "azurerm_resource_group.example";

    /// <summary>
    /// TC-14: Verifies failed resolutions capture type and resource context.
    /// </summary>
    [Test]
    public void RecordFailedResolutions_CapturesTypeAndContext()
    {
        var diagnosticContext = new DiagnosticContext();
        var principalMapper = new PrincipalMapper(
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            diagnosticContext);
        var entityMapper = new AzureEntityMapper(
            subscriptions: [],
            managementGroups: [],
            tenants: [],
            diagnosticContext: diagnosticContext);

        try
        {
            AzureRoleDefinitionMapper.MergeCustomRoles(Array.Empty<MappingEntry>(), diagnosticContext);

            principalMapper.GetName("principal-1", "User", RoleAssignmentAddress);
            entityMapper.GetSubscriptionDisplayName("sub-1", ResourceGroupAddress);
            entityMapper.GetTenantDisplayName("tenant-1", ResourceGroupAddress);
            AzureRoleDefinitionMapper.GetRoleDefinition("unknown-role", null, RoleAssignmentAddress);

            diagnosticContext.FailedResolutions.Should().ContainSingle(failure =>
                failure.Type == FailedResolutionType.Principal
                && failure.Id == "principal-1"
                && failure.ResourceAddress == RoleAssignmentAddress);
            diagnosticContext.FailedResolutions.Should().ContainSingle(failure =>
                failure.Type == FailedResolutionType.Subscription
                && failure.Id == "sub-1"
                && failure.ResourceAddress == ResourceGroupAddress);
            diagnosticContext.FailedResolutions.Should().ContainSingle(failure =>
                failure.Type == FailedResolutionType.Tenant
                && failure.Id == "tenant-1"
                && failure.ResourceAddress == ResourceGroupAddress);
            diagnosticContext.FailedResolutions.Should().ContainSingle(failure =>
                failure.Type == FailedResolutionType.RoleDefinition
                && failure.Id == "unknown-role"
                && failure.ResourceAddress == RoleAssignmentAddress);
        }
        finally
        {
            AzureRoleDefinitionMapper.MergeCustomRoles(Array.Empty<MappingEntry>(), diagnosticContext: null);
        }
    }
}
