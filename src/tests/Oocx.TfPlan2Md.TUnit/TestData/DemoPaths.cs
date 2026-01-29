using System;
using System.IO;

namespace Oocx.TfPlan2Md.Tests.TestData;

/// <summary>
/// Provides paths to test data files used in integration tests.
/// </summary>
public static class DemoPaths
{
    private static string? _repositoryRoot;

    /// <summary>
    /// Gets the absolute path to the repository root directory.
    /// </summary>
    public static string RepositoryRoot => _repositoryRoot ??= FindRepositoryRoot();

    /// <summary>
    /// Gets the path to the comprehensive demo plan.json file.
    /// </summary>
    public static string DemoPlanPath => Path.Combine(RepositoryRoot, "examples", "comprehensive-demo", "plan.json");

    /// <summary>
    /// Gets the path to the demo principals JSON file used for Azure role assignments.
    /// </summary>
    public static string DemoPrincipalsPath => Path.Combine(RepositoryRoot, "examples", "comprehensive-demo", "demo-principals.json");

    /// <summary>
    /// Gets the path to the role assignments test plan JSON file.
    /// </summary>
    public static string RoleAssignmentsPlanPath => Path.Combine(RepositoryRoot, "src", "tests", "Oocx.TfPlan2Md.TUnit", "TestData", "role-assignments.json");

    /// <summary>
    /// Gets the path to the Azure DevOps variable groups test plan JSON file.
    /// </summary>
    public static string AzureDevOpsVariableGroupPlanPath => Path.Combine(RepositoryRoot, "src", "tests", "Oocx.TfPlan2Md.TUnit", "TestData", "azuredevops-variable-groups.json");

    /// <summary>
    /// Gets the path to the Azure AD group member test plan JSON file.
    /// </summary>
    public static string AzureAdGroupMemberPlanPath => Path.Combine(RepositoryRoot, "src", "tests", "Oocx.TfPlan2Md.TUnit", "TestData", "azuread-group-member-plan.json");

    /// <summary>
    /// Gets the path to the Azure AD group test plan JSON file.
    /// </summary>
    public static string AzureAdGroupPlanPath => Path.Combine(RepositoryRoot, "src", "tests", "Oocx.TfPlan2Md.TUnit", "TestData", "azuread-group-plan.json");

    /// <summary>
    /// Gets the path to the Azure AD user test plan JSON file.
    /// </summary>
    public static string AzureAdUserPlanPath => Path.Combine(RepositoryRoot, "src", "tests", "Oocx.TfPlan2Md.TUnit", "TestData", "azuread-user-plan.json");

    /// <summary>
    /// Gets the path to the Azure AD invitation test plan JSON file.
    /// </summary>
    public static string AzureAdInvitationPlanPath => Path.Combine(RepositoryRoot, "src", "tests", "Oocx.TfPlan2Md.TUnit", "TestData", "azuread-invitation-plan.json");

    /// <summary>
    /// Gets the path to the Azure AD service principal test plan JSON file.
    /// </summary>
    public static string AzureAdServicePrincipalPlanPath => Path.Combine(RepositoryRoot, "src", "tests", "Oocx.TfPlan2Md.TUnit", "TestData", "azuread-service-principal-plan.json");

    /// <summary>
    /// Gets the path to the Azure AD group without members test plan JSON file.
    /// </summary>
    public static string AzureAdGroupWithoutMembersPlanPath => Path.Combine(RepositoryRoot, "src", "tests", "Oocx.TfPlan2Md.TUnit", "TestData", "azuread-group-without-members-plan.json");

    /// <summary>
    /// Gets the path to the Azure AD principal mapping file for tests.
    /// </summary>
    public static string AzureAdPrincipalMappingPath => Path.Combine(RepositoryRoot, "src", "tests", "Oocx.TfPlan2Md.TUnit", "TestData", "principal-mapping-azuread.json");

    private static string FindRepositoryRoot()
    {
        var directory = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "src", "tfplan2md.slnx")))
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new InvalidOperationException("Could not locate repository root (src/tfplan2md.slnx not found)");
    }
}
