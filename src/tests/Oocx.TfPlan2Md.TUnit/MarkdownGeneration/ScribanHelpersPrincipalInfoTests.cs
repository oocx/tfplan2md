using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Platforms.Azure;
using Scriban.Runtime;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests principal helper formatting branches for Azure templates.
/// </summary>
public class ScribanHelpersPrincipalInfoTests
{
    /// <summary>
    /// Ensures null principal IDs resolve to an empty string.
    /// </summary>
    [Test]
    public async Task ResolvePrincipalName_WithNullId_ReturnsEmpty()
    {
        var resolver = GetHelperMethod("ResolvePrincipalName");

        var result = (string?)resolver.Invoke(null, new object?[] { null, "User", new StubPrincipalMapper(null), null });

        result.Should().BeEmpty();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Ensures missing principal names fall back to the raw ID.
    /// </summary>
    [Test]
    public async Task ResolvePrincipalName_WithMissingName_ReturnsId()
    {
        var resolver = GetHelperMethod("ResolvePrincipalName");

        var result = (string?)resolver.Invoke(null, new object?[] { "abc", "User", new StubPrincipalMapper(null), null });

        result.Should().Be("abc");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Ensures resolved principal names include the ID when available.
    /// </summary>
    [Test]
    public async Task ResolvePrincipalName_WithResolvedName_ReturnsFormatted()
    {
        var resolver = GetHelperMethod("ResolvePrincipalName");

        var result = (string?)resolver.Invoke(null, new object?[] { "abc", "User", new StubPrincipalMapper("Jane"), null });

        result.Should().Be("Jane [abc]");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Ensures principal info includes full name with type and ID.
    /// </summary>
    [Test]
    public async Task GetPrincipalInfo_IncludesFullName()
    {
        var method = GetHelperMethod("GetPrincipalInfo");

        var result = (ScriptObject?)method.Invoke(null, new object?[] { "abc", "Group", new StubPrincipalMapper("Team"), "addr" });

        result.Should().NotBeNull();
        result!["full_name"].Should().Be("Team (Group) [abc]");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Ensures principal type lookup returns the resolved type when available.
    /// </summary>
    [Test]
    public async Task TryGetPrincipalType_WithResolvedType_ReturnsTypeInfo()
    {
        var method = GetHelperMethod("TryGetPrincipalType");

        var result = (ScriptObject?)method.Invoke(null, new object?[] { "abc", new StubPrincipalTypeMapper(true, "User") });

        result.Should().NotBeNull();
        result!["found"].Should().Be(true);
        result!["type"].Should().Be("User");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets a private Scriban helper method for reflection-based tests.
    /// </summary>
    /// <param name="name">Method name.</param>
    /// <returns>The reflection method info.</returns>
    [SuppressMessage("Major Code Smell", "S3011", Justification = "Testing private Scriban helpers via reflection.")]
    private static MethodInfo GetHelperMethod(string name)
    {
        var method = typeof(ScribanHelpers).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        return method!;
    }

    /// <summary>
    /// Stub principal mapper for deterministic tests.
    /// </summary>
    private sealed class StubPrincipalMapper(string? name) : IPrincipalMapper
    {
        public string GetPrincipalName(string principalId) => principalId;

        public string? GetName(string principalId) => name;
    }

    /// <summary>
    /// Stub principal mapper that controls type resolution outcomes.
    /// </summary>
    private sealed class StubPrincipalTypeMapper(bool found, string? type) : IPrincipalMapper
    {
        public string GetPrincipalName(string principalId) => principalId;

        public string? GetName(string principalId) => null;

        public bool TryGetPrincipalType(string principalId, out string? principalType)
        {
            principalType = type;
            return found;
        }
    }
}
