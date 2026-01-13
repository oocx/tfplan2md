using System.Globalization;
using System.Reflection;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

[TestClass]
public class ReportModelBuilderMetadataTests
{
    private readonly TerraformPlanParser _parser = new();

    [TestMethod]
    public void Build_UsesInjectedMetadataProvider()
    {
        var plan = _parser.Parse(File.ReadAllText("TestData/minimal-plan.json"));
        var metadata = new ReportMetadata("9.9.9", "abcdef0", DateTimeOffset.Parse("2026-01-01T12:30:00Z", CultureInfo.InvariantCulture));
        var provider = new FakeMetadataProvider(metadata);

        var builder = new ReportModelBuilder(metadataProvider: provider);

        var model = builder.Build(plan);

        model.TfPlan2MdVersion.Should().Be("9.9.9");
        model.CommitHash.Should().Be("abcdef0");
        model.GeneratedAtUtc.Should().Be(metadata.GeneratedAtUtc);
        model.HideMetadata.Should().BeFalse();
    }

    [TestMethod]
    public void AssemblyMetadataProvider_UsesAssemblyInformationalVersion()
    {
        var assembly = typeof(ReportModelBuilder).Assembly;
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var expectedVersion = string.IsNullOrWhiteSpace(informationalVersion)
            ? "unknown"
            : informationalVersion.Split('+')[0].Trim();

        var provider = new AssemblyMetadataProvider(assembly);

        var metadata = provider.GetMetadata();

        metadata.Version.Should().Be(expectedVersion);
        metadata.CommitHash.Should().NotBeNullOrWhiteSpace();
    }

    private sealed class FakeMetadataProvider : IMetadataProvider
    {
        private readonly ReportMetadata _metadata;

        public FakeMetadataProvider(ReportMetadata metadata)
        {
            _metadata = metadata;
        }

        public ReportMetadata GetMetadata() => _metadata;
    }
}
