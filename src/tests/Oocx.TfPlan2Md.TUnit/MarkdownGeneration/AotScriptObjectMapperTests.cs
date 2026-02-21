using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.TUnit.MarkdownGeneration;

/// <summary>
/// Tests for <see cref="AotScriptObjectMapper"/> sensitivity mapping into the Scriban template context.
/// Related issue: docs/issues/098-sensitive-info-exposure/analysis.md.
/// </summary>
[Category("Unit")]
public class AotScriptObjectMapperTests
{
    /// <summary>
    /// TC-08: <c>MapResourceChange</c> must populate <c>before_sensitive</c> in the Scriban context
    /// so that provider templates can use it for sensitivity checks.
    /// </summary>
    [Test]
    public async Task MapResourceChange_WithSensitiveChange_MapsBeforeSensitiveToContext()
    {
        // Arrange
        var sensitivityJson = JsonDocument.Parse("""{"password": true}""").RootElement;

        var change = CreateMinimalChangeModel(beforeSensitive: sensitivityJson);

        // Act
        var result = AotScriptObjectMapper.MapResourceChangeWithFormat(
            change,
            RenderTargets.RenderTarget.GitHub);

        // Assert
        var beforeSensitive = result["before_sensitive"];
        beforeSensitive.Should().NotBeNull("before_sensitive should be mapped into the Scriban context");

        var sensitiveObj = beforeSensitive as ScriptObject;
        sensitiveObj.Should().NotBeNull("before_sensitive should be a ScriptObject");
        sensitiveObj!["password"].Should().Be(true);

        await Task.CompletedTask;
    }

    /// <summary>
    /// TC-09: <c>MapResourceChange</c> must populate <c>after_sensitive</c> in the Scriban context
    /// so that provider templates can use it for sensitivity checks.
    /// </summary>
    [Test]
    public async Task MapResourceChange_WithSensitiveChange_MapsAfterSensitiveToContext()
    {
        // Arrange
        var sensitivityJson = JsonDocument.Parse("""{"api_key": true, "connection_string": true}""").RootElement;

        var change = CreateMinimalChangeModel(afterSensitive: sensitivityJson);

        // Act
        var result = AotScriptObjectMapper.MapResourceChangeWithFormat(
            change,
            RenderTargets.RenderTarget.GitHub);

        // Assert
        var afterSensitive = result["after_sensitive"];
        afterSensitive.Should().NotBeNull("after_sensitive should be mapped into the Scriban context");

        var sensitiveObj = afterSensitive as ScriptObject;
        sensitiveObj.Should().NotBeNull("after_sensitive should be a ScriptObject");
        sensitiveObj!["api_key"].Should().Be(true);
        sensitiveObj["connection_string"].Should().Be(true);

        await Task.CompletedTask;
    }

    /// <summary>
    /// TC-10: When <c>showSensitive = false</c>, the <c>after_json</c> exposed to templates must have
    /// sensitive leaf values replaced with <c>(sensitive)</c>. Non-sensitive values remain untouched.
    /// </summary>
    [Test]
    public async Task MapResourceChange_WithSensitiveValues_MasksJsonInContext_WhenShowSensitiveFalse()
    {
        // Arrange
        var afterJson = JsonDocument.Parse("""{"name": "test", "password": "secret123"}""").RootElement;
        var afterSensitive = JsonDocument.Parse("""{"password": true}""").RootElement;

        var change = CreateMinimalChangeModel(
            afterJson: afterJson,
            afterSensitive: afterSensitive);

        // Act — map with showSensitive = false (default security posture)
        // Currently MapResourceChangeWithFormat does not accept showSensitive,
        // so we test through MapReportModel which carries ShowSensitive on the model.
        // Task 10 will add showSensitive threading to the mapper.
        var model = CreateMinimalReportModel(change, showSensitive: false);
        var scriptObject = AotScriptObjectMapper.MapReportModel(model);

        // Navigate to the first change's after_json
        var changes = scriptObject["changes"] as ScriptArray;
        changes.Should().NotBeNull();
        changes!.Count.Should().BeGreaterThan(0);

        var firstChange = changes[0] as ScriptObject;
        firstChange.Should().NotBeNull();

        var afterJsonObj = firstChange!["after_json"] as ScriptObject;
        afterJsonObj.Should().NotBeNull("after_json should be a ScriptObject");

        // Assert — sensitive values must be masked
        afterJsonObj!["password"].Should().Be("(sensitive)",
            "sensitive leaf 'password' should be masked when showSensitive is false");

        // Assert — non-sensitive values must NOT be masked
        afterJsonObj["name"].Should().Be("test",
            "non-sensitive leaf 'name' should retain its original value");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates a minimal <see cref="ResourceChangeModel"/> for testing mapper behavior.
    /// </summary>
    private static ResourceChangeModel CreateMinimalChangeModel(
        JsonElement? beforeJson = null,
        JsonElement? afterJson = null,
        JsonElement? beforeSensitive = null,
        JsonElement? afterSensitive = null)
    {
        return new ResourceChangeModel
        {
            Address = "test_resource.example",
            Type = "test_resource",
            Name = "example",
            ProviderName = "registry.terraform.io/hashicorp/test",
            Action = "update",
            ActionSymbol = "~",
            AttributeChanges = [],
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            BeforeSensitive = beforeSensitive,
            AfterSensitive = afterSensitive,
        };
    }

    /// <summary>
    /// Creates a minimal <see cref="ReportModel"/> wrapping a single change for testing through the full mapper path.
    /// </summary>
    private static ReportModel CreateMinimalReportModel(ResourceChangeModel change, bool showSensitive)
    {
        return new ReportModel
        {
            TerraformVersion = "1.0.0",
            FormatVersion = "1.2",
            TfPlan2MdVersion = "test",
            CommitHash = "abc123",
            HideMetadata = false,
            Timestamp = "",
            ReportTitle = "Test",
            ShowUnchangedValues = false,
            ShowSensitive = showSensitive,
            RenderTarget = RenderTargets.RenderTarget.GitHub,
            DetailsDisplayMode = RenderTargets.DetailsDisplayMode.Auto,
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            Summary = new SummaryModel
            {
                ToAdd = new ActionSummary(0, []),
                ToChange = new ActionSummary(1, []),
                ToDestroy = new ActionSummary(0, []),
                ToReplace = new ActionSummary(0, []),
                NoOp = new ActionSummary(0, []),
                Total = 1,
            },
            Changes = [change],
            ModuleChanges = [],
            RefactoringOperations = [],
        };
    }
}
