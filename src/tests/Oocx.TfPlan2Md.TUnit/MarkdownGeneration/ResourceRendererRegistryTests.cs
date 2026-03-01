using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for <see cref="ResourceRendererRegistry"/>.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// Related test plan: docs/features/107-remove-scriban/test-plan.md (TC-RRR-01..03).
/// </summary>
public class ResourceRendererRegistryTests
{
    /// <summary>
    /// Canonical resource type used by registry tests.
    /// </summary>
    private const string ResourceType = "azurerm_resource_group";

    /// <summary>
    /// Verifies that a registered type resolves its renderer.
    /// </summary>
    [Test]
    public void GetRenderer_RegisteredResourceType_ReturnsRegisteredRenderer()
    {
        var registry = new ResourceRendererRegistry();
        var renderer = new TestRenderer(ResourceType);

        registry.Register(renderer);
        var resolved = registry.GetRenderer(ResourceType);

        resolved.Should().BeSameAs(renderer);
    }

    /// <summary>
    /// Verifies that an unregistered type returns null.
    /// </summary>
    [Test]
    public void GetRenderer_UnregisteredResourceType_ReturnsNull()
    {
        var registry = new ResourceRendererRegistry();

        var resolved = registry.GetRenderer("azurerm_storage_account");

        resolved.Should().BeNull();
    }

    /// <summary>
    /// Verifies duplicate registration uses last-writer-wins behavior.
    /// </summary>
    [Test]
    public void Register_DuplicateResourceType_ReplacesExistingRenderer()
    {
        var registry = new ResourceRendererRegistry();
        var first = new TestRenderer(ResourceType);
        var second = new TestRenderer(ResourceType);

        registry.Register(first);
        registry.Register(second);
        var resolved = registry.GetRenderer(ResourceType);

        resolved.Should().BeSameAs(second);
    }

    /// <summary>
    /// Minimal test renderer for registry behavior.
    /// </summary>
    private sealed class TestRenderer(string resourceType) : IResourceRenderer
    {
        /// <summary>
        /// Backing resource type for explicit interface member implementation.
        /// </summary>
        private readonly string _resourceType = resourceType;

        /// <inheritdoc />
        string IResourceRenderer.ResourceType => _resourceType;

        /// <inheritdoc />
        public void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
        {
            _ = writer;
            _ = change;
            _ = context;
        }
    }
}
