using System;
using System.Collections.Generic;
using System.Linq;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Registry for provider-specific resource model mappers.
/// </summary>
/// <remarks>
/// Providers register <see cref="IResourceModelMapper"/> implementations to enrich
/// ScriptObjects with typed view models for template rendering. This enables provider-specific
/// rendering extensions without creating MarkdownGeneration → Providers dependencies.
/// Related refactoring: Eliminates architecture boundary violations from typed properties on ResourceChangeModel.
/// </remarks>
internal sealed class ResourceModelMapperRegistry
{
    /// <summary>
    /// Stores all registered mappers in registration order.
    /// </summary>
    private readonly List<IResourceModelMapper> _mappers = [];

    /// <summary>
    /// Registers a resource model mapper.
    /// </summary>
    /// <param name="mapper">The mapper to register.</param>
    public void Register(IResourceModelMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        _mappers.Add(mapper);
    }

    /// <summary>
    /// Applies all registered mappers to enrich the ScriptObject for a resource.
    /// </summary>
    /// <param name="resource">The resource change model to map.</param>
    /// <param name="scriptObject">The ScriptObject to enrich with provider-specific properties.</param>
    /// <remarks>
    /// Mappers are applied in registration order. Each mapper's <see cref="IResourceModelMapper.CanMap"/>
    /// is evaluated, and only matching mappers enrich the ScriptObject.
    /// </remarks>
    public void EnrichScriptObject(ResourceChangeModel resource, ScriptObject scriptObject)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(scriptObject);

        foreach (var mapper in _mappers.Where(m => m.CanMap(resource)))
        {
            mapper.EnrichScriptObject(resource, scriptObject);
        }
    }
}
