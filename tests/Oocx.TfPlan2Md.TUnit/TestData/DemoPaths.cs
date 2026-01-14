using System;
using System.IO;

namespace Oocx.TfPlan2Md.Tests.TestData;

public static class DemoPaths
{
    private static string? _repositoryRoot;

    public static string RepositoryRoot => _repositoryRoot ??= FindRepositoryRoot();

    public static string DemoPlanPath => Path.Combine(RepositoryRoot, "examples", "comprehensive-demo", "plan.json");

    public static string DemoPrincipalsPath => Path.Combine(RepositoryRoot, "examples", "comprehensive-demo", "demo-principals.json");

    public static string RoleAssignmentsPlanPath => Path.Combine(RepositoryRoot, "tests", "Oocx.TfPlan2Md.TUnit", "TestData", "role-assignments.json");

    private static string FindRepositoryRoot()
    {
        var directory = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "tfplan2md.slnx")))
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new InvalidOperationException("Could not locate repository root (tfplan2md.slnx not found)");
    }
}
