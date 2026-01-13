namespace Oocx.TfPlan2Md.Tests.Docker;

/// <summary>
/// Assembly initialization for all MSTest tests.
/// Initializes shared fixtures.
/// </summary>
[TestClass]
public class AssemblyTestInitializer
{
    [AssemblyInitialize]
    public static void AssemblyInit(TestContext context)
    {
        // Trigger initialization of Docker fixture
        var dockerFixture = DockerFixture.Instance;
        // Trigger initialization of MarkdownLint fixture  
        var lintFixture = MarkdownGeneration.MarkdownLintFixture.Instance;
    }
}
