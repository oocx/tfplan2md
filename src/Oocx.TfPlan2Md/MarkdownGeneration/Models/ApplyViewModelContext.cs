using System.Collections.Generic;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Carries all contextual data needed by <see cref="IResourceViewModelFactory.ApplyViewModel"/>.
/// </summary>
/// <remarks>
/// Replaces the six individual parameters previously threaded into every factory implementation.
/// Each factory unpacks only the fields it actually needs, eliminating
/// <c>_ = principalMapper;</c> and <c>_ = iconProviderRegistry;</c> discard statements.
/// Related feature: docs/features/111-code-simplification/specification.md (Finding 2.5).
/// </remarks>
/// <param name="Model">The resource change model to populate.</param>
/// <param name="ResourceChange">The resource change data from the Terraform plan.</param>
/// <param name="Action">The determined action for this resource (create, update, delete, replace).</param>
/// <param name="AttributeChanges">Pre-computed attribute changes for the resource.</param>
/// <param name="PrincipalMapper">Mapper used for Azure principal resolution.</param>
/// <param name="IconProviderRegistry">Optional registry of icon providers for summary rendering.</param>
internal sealed record ApplyViewModelContext(
    ResourceChangeModel Model,
    ResourceChange ResourceChange,
    string Action,
    IReadOnlyList<AttributeChangeModel> AttributeChanges,
    IPrincipalMapper PrincipalMapper,
    IconProviderRegistry? IconProviderRegistry);
