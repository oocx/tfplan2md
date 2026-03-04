using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Assertions;

namespace Oocx.TfPlan2Md.Tests.Workflows;

/// <summary>
/// Verifies project configuration required for NativeAOT publish compatibility.
/// Related issue: docs/issues/108-binary-builds-failed/analysis.md.
/// </summary>
public class AotPublishIsolationTests
{
    /// <summary>
    /// Ensures the JsonEmbedGenerator analyzer project reference removes PublishAot global property.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Json_embed_generator_reference_removes_publish_aot_global_property()
    {
        var projectPath = Path.Combine(DemoPaths.RepositoryRoot, "src", "Oocx.TfPlan2Md", "Oocx.TfPlan2Md.csproj");
        var document = XDocument.Load(projectPath);

        var projectReference = document
            .Descendants("ProjectReference")
            .FirstOrDefault(node => string.Equals(
                (string?)node.Attribute("Include"),
                "../tools/JsonEmbedGenerator/JsonEmbedGenerator.csproj",
                StringComparison.Ordinal));

        await Assert.That(projectReference).IsNotNull();

        var globalPropertiesToRemove = (string?)projectReference?.Attribute("GlobalPropertiesToRemove");

        await Assert.That(globalPropertiesToRemove).IsNotNull();
        await Assert.That(globalPropertiesToRemove).Contains("PublishAot", StringComparison.Ordinal);
    }

    /// <summary>
    /// Ensures the JsonEmbedGenerator project treats PublishAot as local and forces it off.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Json_embed_generator_project_disables_publish_aot_when_built_as_reference()
    {
        var generatorProjectPath = Path.Combine(DemoPaths.RepositoryRoot, "src", "tools", "JsonEmbedGenerator", "JsonEmbedGenerator.csproj");
        var document = XDocument.Load(generatorProjectPath);

        var root = document.Root;
        await Assert.That(root).IsNotNull();

        var treatAsLocalProperty = (string?)root?.Attribute("TreatAsLocalProperty");
        await Assert.That(treatAsLocalProperty).IsNotNull();
        await Assert.That(treatAsLocalProperty).Contains("PublishAot", StringComparison.Ordinal);

        var publishAotValue = document
            .Descendants("PublishAot")
            .Select(node => node.Value?.Trim())
            .FirstOrDefault(value => !string.IsNullOrEmpty(value));

        await Assert.That(publishAotValue).IsEqualTo("false");
    }
}
