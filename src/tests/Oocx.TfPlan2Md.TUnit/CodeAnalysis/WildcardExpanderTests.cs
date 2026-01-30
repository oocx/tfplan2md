
using System.IO;
using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.CodeAnalysis;

public class WildcardExpanderTests
{
    [Test]
    public void Expand_SinglePattern_ReturnsMatchingFiles()
    {
        // Arrange
        var testDir = Path.Combine(GetTempRoot(), Path.GetRandomFileName());
        Directory.CreateDirectory(testDir);
        var file1 = Path.Combine(testDir, "a.sarif");
        var file2 = Path.Combine(testDir, "b.sarif");
        File.WriteAllText(file1, "{}");
        File.WriteAllText(file2, "{}");

        try
        {
            // Act
            var files = WildcardExpander.Expand(new[] { Path.Combine(testDir, "*.sarif") });

            // Assert
            files.Should().Contain(file1);
            files.Should().Contain(file2);
        }
        finally
        {
            File.Delete(file1);
            File.Delete(file2);
            Directory.Delete(testDir);
        }
    }

    [Test]
    public void Expand_MultiplePatterns_ReturnsAllMatches()
    {
        // Arrange
        var testDir = Path.Combine(GetTempRoot(), Path.GetRandomFileName());
        Directory.CreateDirectory(testDir);
        var file1 = Path.Combine(testDir, "a.sarif");
        var file2 = Path.Combine(testDir, "b.sarif");
        var file3 = Path.Combine(testDir, "c.txt");
        File.WriteAllText(file1, "{}");
        File.WriteAllText(file2, "{}");
        File.WriteAllText(file3, "test");

        try
        {
            // Act
            var files = WildcardExpander.Expand(new[] { Path.Combine(testDir, "*.sarif"), Path.Combine(testDir, "*.txt") });

            // Assert
            files.Should().Contain(file1);
            files.Should().Contain(file2);
            files.Should().Contain(file3);
        }
        finally
        {
            File.Delete(file1);
            File.Delete(file2);
            File.Delete(file3);
            Directory.Delete(testDir);
        }
    }

    /// <summary>
    /// Verifies recursive glob patterns search nested directories.
    /// </summary>
    [Test]
    public void Expand_RecursivePattern_ReturnsNestedFiles()
    {
        var testDir = Path.Combine(GetTempRoot(), Path.GetRandomFileName());
        var nestedDir = Path.Combine(testDir, "nested");
        Directory.CreateDirectory(nestedDir);
        var file1 = Path.Combine(testDir, "root.sarif");
        var file2 = Path.Combine(nestedDir, "nested.sarif");
        File.WriteAllText(file1, "{}");
        File.WriteAllText(file2, "{}");

        try
        {
            var files = WildcardExpander.Expand(new[] { Path.Combine(testDir, "**", "*.sarif") });

            files.Should().Contain(file1);
            files.Should().Contain(file2);
        }
        finally
        {
            File.Delete(file1);
            File.Delete(file2);
            Directory.Delete(nestedDir);
            Directory.Delete(testDir);
        }
    }

    /// <summary>
    /// Gets a unique temporary directory under the repository .tmp directory.
    /// </summary>
    /// <returns>The absolute directory path.</returns>
    private static string GetTempRoot()
    {
        var tempRoot = Path.Combine(GetRepoRoot(), ".tmp", "wildcard-expander-tests");
        Directory.CreateDirectory(tempRoot);
        return tempRoot;
    }

    /// <summary>
    /// Resolves the repository root to keep file IO inside the workspace.
    /// </summary>
    /// <returns>Absolute path to the repo root.</returns>
    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
