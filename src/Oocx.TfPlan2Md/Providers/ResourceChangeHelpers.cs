using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Providers;

/// <summary>
/// Shared helpers for resource change processing used across provider factory implementations.
/// </summary>
internal static class ResourceChangeHelpers
{
    /// <summary>
    /// Resolves the JSON state object to use for summary generation based on the action.
    /// For delete actions the <c>Before</c> state is used; all other actions use <c>After</c>.
    /// Falls back to whichever side is non-null when the preferred side is null.
    /// </summary>
    /// <param name="resourceChange">The resource change data.</param>
    /// <param name="action">The normalized Terraform action (e.g. "create", "delete", "update").</param>
    /// <returns>The active state object to use for summary generation.</returns>
    internal static object? ResolveActiveState(ResourceChange resourceChange, string action)
    {
        var state = string.Equals(action, "delete", StringComparison.Ordinal) ? resourceChange.Change.Before : resourceChange.Change.After;
        return state ?? resourceChange.Change.After ?? resourceChange.Change.Before;
    }
}
