using System.Collections.Frozen;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Providers.AzureDevOps;
using Scriban;
using Scriban.Runtime;
using TUnit.Core;

namespace Oocx.TfPlan2Md.TUnit.MarkdownGeneration;

/// <summary>
/// Tests for Azure DevOps Scriban helper functions.
/// Related feature: docs/features/085-azdo-principal-mapping/test-cases.md (TC-14, TC-15, TC-16).
/// </summary>
public class ScribanHelpersAzdoTests
{
    /// <summary>
    /// TC-14: Tests that the azdo_user_name helper resolves known user IDs correctly.
    /// </summary>
    [Test]
    public async Task AzdoUserName_KnownUserId_ReturnsFormattedName()
    {
        // Arrange
        var mappings = new Dictionary<string, string>
        {
            ["4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b"] = "John Smith",
        }.ToFrozenDictionary();
        var mapper = new AzdoUserMapper(mappings, null);

        // Create Scriban template context with helper
        var template = Template.Parse("{{ azdo_user_name '4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b' }}");
        var context = new TemplateContext();

        // Register helper (similar to how it's done in Registry)
        var scriptObject = new ScriptObject();
        scriptObject.Import("azdo_user_name", new Func<string, string>(userId => mapper.GetEntityName(userId)));
        context.PushGlobal(scriptObject);

        // Act
        var result = await template.RenderAsync(context);

        // Assert
        result.Should().Be("John Smith [4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b]");
    }

    /// <summary>
    /// TC-14: Tests that the azdo_user_name helper handles unknown user IDs gracefully.
    /// </summary>
    [Test]
    public async Task AzdoUserName_UnknownUserId_ReturnsRawId()
    {
        // Arrange
        var mapper = new AzdoUserMapper(FrozenDictionary<string, string>.Empty, null);

        var template = Template.Parse("{{ azdo_user_name 'unknown-user-id' }}");
        var context = new TemplateContext();
        var scriptObject = new ScriptObject();
        scriptObject.Import("azdo_user_name", new Func<string, string>(userId => mapper.GetEntityName(userId)));
        context.PushGlobal(scriptObject);

        // Act
        var result = await template.RenderAsync(context);

        // Assert
        result.Should().Be("unknown-user-id");
    }

    /// <summary>
    /// TC-15: Tests that the azdo_group_name helper preserves long group descriptors.
    /// </summary>
    [Test]
    public async Task AzdoGroupName_LongDescriptor_ReturnsFormattedNameWithFullDescriptor()
    {
        // Arrange
        var longDescriptor = "vssgp.Uy0xLTktMTU1MTM3NDI0NS0yNzY5MzQwNjk3LTExMDE5ODM1NjMtMzU0Nzk5MjM2MS0zNzAyMTIxNjI4LTEtMTIzNDU2Nzg5MC0xMjM0NTY3ODkwLTEyMzQ1Njc4OTAtMTIzNDU2Nzg5MA";
        var mappings = new Dictionary<string, string>
        {
            [longDescriptor] = "Platform Team",
        }.ToFrozenDictionary();
        var mapper = new AzdoGroupMapper(mappings, null);

        var template = Template.Parse($"{{{{ azdo_group_name '{longDescriptor}' }}}}");
        var context = new TemplateContext();
        var scriptObject = new ScriptObject();
        scriptObject.Import("azdo_group_name", new Func<string, string>(groupId => mapper.GetEntityName(groupId)));
        context.PushGlobal(scriptObject);

        // Act
        var result = await template.RenderAsync(context);

        // Assert
        result.Should().Be($"Platform Team [{longDescriptor}]");
    }

    /// <summary>
    /// TC-15: Tests that the azdo_group_name helper handles unknown descriptors gracefully.
    /// </summary>
    [Test]
    public async Task AzdoGroupName_UnknownDescriptor_ReturnsRawDescriptor()
    {
        // Arrange
        var mapper = new AzdoGroupMapper(FrozenDictionary<string, string>.Empty, null);

        var template = Template.Parse("{{ azdo_group_name 'unknown-descriptor' }}");
        var context = new TemplateContext();
        var scriptObject = new ScriptObject();
        scriptObject.Import("azdo_group_name", new Func<string, string>(groupId => mapper.GetEntityName(groupId)));
        context.PushGlobal(scriptObject);

        // Act
        var result = await template.RenderAsync(context);

        // Assert
        result.Should().Be("unknown-descriptor");
    }

    /// <summary>
    /// TC-16: Tests that the azdo_project_name helper resolves known project IDs correctly.
    /// </summary>
    [Test]
    public async Task AzdoProjectName_KnownProjectId_ReturnsFormattedName()
    {
        // Arrange
        var mappings = new Dictionary<string, string>
        {
            ["8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f"] = "Infrastructure Project",
        }.ToFrozenDictionary();
        var mapper = new AzdoProjectMapper(mappings, null);

        var template = Template.Parse("{{ azdo_project_name '8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f' }}");
        var context = new TemplateContext();
        var scriptObject = new ScriptObject();
        scriptObject.Import("azdo_project_name", new Func<string, string>(projectId => mapper.GetEntityName(projectId)));
        context.PushGlobal(scriptObject);

        // Act
        var result = await template.RenderAsync(context);

        // Assert
        result.Should().Be("Infrastructure Project [8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f]");
    }

    /// <summary>
    /// TC-16: Tests that the azdo_project_name helper handles unknown project IDs gracefully.
    /// </summary>
    [Test]
    public async Task AzdoProjectName_UnknownProjectId_ReturnsRawId()
    {
        // Arrange
        var mapper = new AzdoProjectMapper(FrozenDictionary<string, string>.Empty, null);

        var template = Template.Parse("{{ azdo_project_name 'unknown-project-id' }}");
        var context = new TemplateContext();
        var scriptObject = new ScriptObject();
        scriptObject.Import("azdo_project_name", new Func<string, string>(projectId => mapper.GetEntityName(projectId)));
        context.PushGlobal(scriptObject);

        // Act
        var result = await template.RenderAsync(context);

        // Assert
        result.Should().Be("unknown-project-id");
    }
}
