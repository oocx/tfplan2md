
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
        var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
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
        var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
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
}
