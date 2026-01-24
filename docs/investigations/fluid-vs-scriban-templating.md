# Investigation: Fluid vs Scriban Templating Library Replacement

## Status

Investigation Complete — **Recommendation: Do Not Replace**

## Summary

This investigation evaluates whether replacing Scriban with [Fluid](https://github.com/sebastienros/fluid) would provide meaningful benefits for the tfplan2md project. After comprehensive analysis, the recommendation is to **continue using Scriban**.

## Investigation Context

### Questions Addressed

1. What benefits would we gain from replacing Scriban with Fluid (e.g., reduced binary size)?
2. Is Fluid a good replacement that covers all Scriban features currently used?
3. How could we perform the replacement?

### Current State

- **Template engine**: Scriban 6.5.2
- **Publish mode**: NativeAOT with full trimming
- **Image size**: 14.7 MB (FROM scratch)
- **AOT compatibility**: Achieved via `AotScriptObjectMapper` (explicit property mapping)
- **Template syntax**: Scriban native syntax (not Liquid-compatible)

## Analysis

### 1. Package Size Comparison

| Library | NuGet Package Size | Notes |
|---------|-------------------|-------|
| Scriban 6.5.2 | ~507 KB | Includes scripting engine |
| Fluid.Core 2.31.0 | ~245 KB | Liquid-only, smaller scope |

**Delta**: ~260 KB smaller NuGet package for Fluid.

However, in the context of NativeAOT + trimming:
- Final binary size impact depends on what gets trimmed
- Current image is already 14.7 MB (89.6% reduction achieved)
- The ~260 KB difference becomes negligible (<2% of final binary)

### 2. Feature Compatibility Assessment

#### Scriban Features Currently Used

| Feature | Usage in tfplan2md | Fluid Equivalent |
|---------|-------------------|------------------|
| **Native Scriban syntax** | `{{ for item in array }}...{{ end }}` | ❌ Uses Liquid syntax: `{% for item in array %}...{% endfor %}` |
| **ScriptObject** | `AotScriptObjectMapper` creates nested ScriptObjects | ✅ TemplateContext with models |
| **ScriptArray** | Dynamic array building for templates | ✅ Native .NET collections |
| **Custom helper functions** | 20+ registered via `scriptObject.Import()` | ✅ Filters via `options.Filters.AddFilter()` |
| **ITemplateLoader** | `ScribanTemplateLoader` for includes | ✅ IFileProvider for includes |
| **include directive** | `{{ include "_header.sbn" }}` | ✅ `{% include "header" %}` |
| **Whitespace control** | `{{~ expr ~}}` | ✅ `{%- expr -%}` |
| **Inline expressions** | `{{ x = 10 }}`, `{{ if condition }}` | ⚠️ Different: `{% assign x = 10 %}`, `{% if condition %}` |
| **Null coalescing** | `{{ value ?? default }}` | ❌ Not directly supported |
| **Ternary operator** | `{{ condition ? "yes" : "no" }}` | ❌ Not directly supported |
| **Array filter expressions** | `{{ items | array.filter @(do; ret condition; end) }}` | ❌ No inline lambdas |
| **Built-in functions** | `string.starts_with`, `date.to_string` | ✅ Similar filters available |

#### Templates Analysis

Examined template files reveal heavy use of Scriban-specific features:

1. **`default.sbn`**: Uses `{{ include (resolve_template change.type) }}` for dynamic includes
2. **`azapi/resource.sbn`**: Uses `{{ change.attribute_changes | array.filter @(do; ret !string.starts_with $0.name "body."; end) }}`
3. **`_header.sbn`**: Uses `{{ report_title ?? default_report_title ?? "Terraform Plan Report" }}`

These patterns would require significant rewrites for Liquid/Fluid compatibility.

### 3. AOT/NativeAOT Compatibility

| Aspect | Scriban | Fluid |
|--------|---------|-------|
| Reflection-free core | ✅ (with explicit mapping) | ✅ (allow-list model) |
| NativeAOT tested | ✅ (production proven) | ⚠️ (community reports, not verified) |
| Trimming safe | ✅ (TrimmerRootDescriptor.xml) | ⚠️ (would need similar setup) |
| Current implementation | Working | Would require new implementation |

Both libraries can work with NativeAOT, but:
- Scriban compatibility has been proven and tested in this codebase
- Fluid would require new trimmer root configuration and testing
- No guarantee of smaller trimmed binary size

### 4. Migration Effort Assessment

#### Required Changes for Fluid Migration

1. **Template syntax rewrite** (15+ template files):
   - Change all `{{ expr }}` to Liquid-style `{% statement %}` / `{{ output }}`
   - Rewrite array filter expressions (no inline lambdas)
   - Replace null coalescing with explicit conditionals
   - Replace ternary operators

2. **Helper registration refactoring**:
   - Convert 20+ helper functions to Fluid filter format
   - Signature change: `Func<...>` → `ValueTask<FluidValue> Filter(...)`

3. **Template loader rewrite**:
   - Replace `ITemplateLoader` with `IFileProvider` implementation
   - Update resource loading strategy

4. **AOT compatibility work**:
   - Create new trimmer configuration
   - Test all templates under NativeAOT
   - Verify no runtime reflection issues

5. **Test updates**:
   - Update snapshot tests for new output
   - Verify all template scenarios

**Estimated effort**: 3-5 days of development + testing

### 5. Risk Analysis

#### Risks of Migration

| Risk | Severity | Mitigation |
|------|----------|------------|
| Behavioral differences in rendering | High | Extensive snapshot testing |
| AOT regression | High | Full AOT build/test cycle |
| Template syntax errors | Medium | Manual review of all templates |
| Helper function parity | Medium | Integration testing |
| User-facing template changes | Low | Custom templates would break |

#### Risks of Staying with Scriban

| Risk | Severity | Notes |
|------|----------|-------|
| Library abandonment | Very Low | Actively maintained (last release: recent) |
| Security vulnerabilities | Low | No known issues, good security model |
| Feature limitations | None | All needed features available |

## Benefits Summary

### Potential Benefits of Fluid

1. **Smaller NuGet package**: ~250 KB savings (negligible after trimming)
2. **Strict Liquid compatibility**: Not a requirement for this project
3. **Allow-list security model**: Already have explicit mapping with AotScriptObjectMapper

### Current Benefits of Scriban

1. **Proven NativeAOT compatibility**: Production-tested
2. **Rich syntax**: Ternary, null coalescing, inline lambdas
3. **No migration needed**: Zero effort to maintain status quo
4. **Custom template support**: Users can extend with Scriban syntax
5. **Performance**: Both are fast, no measurable difference for use case

## Cost-Benefit Analysis

| Factor | Scriban (Current) | Fluid (Migration) |
|--------|-------------------|-------------------|
| Binary size | Current | ~0-2% smaller (~260 KB, unverified) |
| Development cost | 0 days | 3-5 days |
| Risk | None | Medium-High |
| Feature parity | 100% | ~85% (some features need workarounds) |
| Maintenance | Known/stable | New learning curve |
| User impact | None | Breaking for custom templates |

## Conclusion

**Recommendation: Do not replace Scriban with Fluid.**

The investigation reveals that:

1. **Marginal size benefit**: The ~260 KB NuGet difference becomes negligible after NativeAOT trimming. The current 14.7 MB image is already optimized.

2. **Significant migration effort**: 3-5 days of work with non-trivial risks, including potential AOT regression.

3. **Feature regression**: Scriban's rich syntax (ternary, null coalescing, inline lambdas) would need workarounds in Liquid.

4. **No pressing need**: Scriban is actively maintained, has proven NativeAOT compatibility, and meets all project requirements.

5. **User impact**: Custom template users would need to rewrite their templates.

The only scenario where this migration would make sense is if:
- Scriban becomes unmaintained AND
- A security vulnerability is discovered AND
- Significant size reduction is achievable (verified)

None of these conditions currently apply.

## Alternative Recommendations

If binary size optimization remains a goal, consider:

1. **Profile trimming**: Use ILLink analysis to identify additional trimming opportunities
2. **Template optimization**: Reduce template complexity to minimize helper code
3. **Dependency audit**: Review other dependencies for size reduction opportunities

## References

- [Scriban GitHub](https://github.com/scriban/scriban)
- [Fluid GitHub](https://github.com/sebastienros/fluid)
- [ADR-001: Scriban Templating](../adr-001-scriban-templating.md)
- [Feature 037: AOT Trimmed Image](../features/037-aot-trimmed-image/architecture.md)
