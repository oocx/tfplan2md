using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Unit tests for <see cref="AttributeChangeFilterRegistry"/>.
/// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
/// </summary>
[Category("Unit")]
public class AttributeChangeFilterRegistryTests
{
    private static readonly AttributeChangeFilterContext AnyContext = new(
        ProviderName: "test",
        AttributeName: "attr",
        BeforeValue: "before",
        AfterValue: "after");

    // -------------------------------------------------------------------------
    // TC-22: An empty registry never suppresses.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-22: No filters registered → ShouldSuppress returns false for any context.
    /// </summary>
    [Test]
    public async Task ShouldSuppress_EmptyRegistry_ReturnsFalse()
    {
        // Arrange
        var registry = new AttributeChangeFilterRegistry();

        // Act
        var result = registry.ShouldSuppress(AnyContext);

        // Assert
        result.Should().BeFalse("an empty registry must never suppress any row");
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // TC-23: One true-returning filter causes the registry to return true.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-23: One stub returning false + one stub returning true → registry returns true (OR semantics).
    /// </summary>
    [Test]
    public async Task ShouldSuppress_OneFilterReturnsTrue_ReturnsTrue()
    {
        // Arrange
        var registry = new AttributeChangeFilterRegistry();
        registry.Register(new StubFilter(returns: false));
        registry.Register(new StubFilter(returns: true));

        // Act
        var result = registry.ShouldSuppress(AnyContext);

        // Assert
        result.Should().BeTrue("at least one filter returns true, so the registry should return true");
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // TC-24: All false-returning filters cause the registry to return false.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-24: Two stubs both returning false → registry returns false.
    /// </summary>
    [Test]
    public async Task ShouldSuppress_AllFiltersReturnFalse_ReturnsFalse()
    {
        // Arrange
        var registry = new AttributeChangeFilterRegistry();
        registry.Register(new StubFilter(returns: false));
        registry.Register(new StubFilter(returns: false));

        // Act
        var result = registry.ShouldSuppress(AnyContext);

        // Assert
        result.Should().BeFalse("all filters return false, so the registry should return false");
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // Test stub
    // -------------------------------------------------------------------------

    /// <summary>
    /// Minimal test stub for <see cref="IAttributeChangeFilter"/>.
    /// </summary>
    private sealed class StubFilter(bool returns) : IAttributeChangeFilter
    {
        /// <inheritdoc />
        public bool ShouldSuppress(AttributeChangeFilterContext context) => returns;
    }
}
