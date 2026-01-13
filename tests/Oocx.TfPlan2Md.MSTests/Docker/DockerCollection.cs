namespace Oocx.TfPlan2Md.Tests.Docker;

/// <summary>
/// Docker test initialization - MSTest doesn't need collection definitions like xUnit.
/// The DockerFixture.Instance provides shared state across all Docker tests.
/// </summary>
[TestClass]
public class DockerTestsInitializer
{
    [AssemblyInitialize]
    public static void AssemblyInit(TestContext context)
    {
        // Trigger initialization of Docker fixture
        _ = DockerFixture.Instance;
    }
}
