# Architecture Boundary Enforcement with Tests

This release adds automated enforcement of architectural layer boundaries using NetArchTest.Rules. The tfplan2md codebase now has 14 automated tests that prevent unintended coupling between layers (CLI, Parsing, MarkdownGeneration, Platforms, Providers) and verify naming conventions. Architecture tests run automatically in CI and block PRs that violate architectural rules.

## ✨ Features

- **14 Architecture Tests**: Automated enforcement of layer dependency rules and naming conventions
  - 7 forbidden dependency rules (e.g., Parsing cannot depend on MarkdownGeneration)
  - 4 allowed dependency rules documented as tests (e.g., CLI can depend on all layers)
  - 3 naming convention rules (Exception suffix, Tests suffix, Interface prefix)
- **NetArchTest.Rules Integration**: Added NetArchTest.Rules 1.3.2 for framework-agnostic architecture testing
- **Comprehensive Documentation**: New `docs/architecture-rules.md` (390 lines) explaining all architectural layers and rules
- **CI Integration**: Tests run automatically in PR validation workflow, blocking merge on architectural violations
- **Clear Error Messages**: Failed tests provide actionable guidance with links to documentation
- **Known Violations Documented**: 8 files with known violations are exempted with justification comments

## 📚 Documentation

- **New:** `docs/architecture-rules.md` - Comprehensive guide to architectural layers, dependency rules, and naming conventions
- **Updated:** `docs/architecture.md` - Added ADR-007 reference and architecture enforcement to quality requirements
- **Updated:** `docs/testing-strategy.md` - Added "Architecture Tests" section with developer workflow
- **Updated:** `docs/features.md` - Added Feature 066 entry
- **Updated:** `CONTRIBUTING.md` - Added architecture rules guidance for contributors
- **Updated:** `docs/spec.md` - Added architecture enforcement to code quality section
- **New:** `docs/adr-007-architecture-boundary-enforcement.md` - ADR documenting library selection rationale

## 🔗 Commits

> Infrastructure and testing commits for architecture boundary enforcement feature.

- [`40074a7`](https://github.com/oocx/tfplan2md/commit/40074a7) test: add NetArchTest.Rules package and create architecture test file structure
- [`f795dbf`](https://github.com/oocx/tfplan2md/commit/f795dbf) test: implement Parsing layer dependency tests
- [`5367a20`](https://github.com/oocx/tfplan2md/commit/5367a20) test: implement all architecture boundary tests with exemptions
- [`4d37f47`](https://github.com/oocx/tfplan2md/commit/4d37f47) docs: create comprehensive architecture rules documentation
- [`9d4035b`](https://github.com/oocx/tfplan2md/commit/9d4035b) docs: update global documentation for architecture boundary enforcement

## 🚨 Breaking changes

None. This is an internal infrastructure feature that adds architecture enforcement without changing user-facing behavior.

## ▶️ For Developers

### Running Architecture Tests

Architecture tests run automatically with the standard test command:

```bash
# Run all tests including architecture tests
dotnet test --solution src/tfplan2md.slnx

# Run only architecture tests
dotnet test --filter "FullyQualifiedName~Architecture"
```

### When Architecture Tests Fail

If an architecture test fails, it means your code violates an architectural boundary rule:

1. **Read the error message** - Shows which layer violated which rule
2. **Consult `docs/architecture-rules.md`** - Explains the rule and rationale
3. **Refactor to respect boundaries** - Move code to appropriate layer or adjust design
4. **If rule is incorrect** - Discuss with maintainers about updating the rule

### Key Architectural Rules

- **Parsing layer** cannot depend on MarkdownGeneration (separation of parsing from rendering)
- **Platforms layer** should not depend on MarkdownGeneration (metadata only, not rendering)
- **MarkdownGeneration layer** should not depend on Providers (providers depend on generation, not vice versa)
- **Exceptions must end with "Exception" suffix**
- **Test classes must end with "Tests" or "Test" suffix**
- **Interfaces must start with "I" prefix**

See `docs/architecture-rules.md` for complete rule list and detailed explanations.

## Performance

- **Execution Time:** 2.5 seconds for all 14 tests (analyzing 904 types)
- **Target:** <10 seconds
- **Result:** 75% faster than target ✅

## Implementation Notes

### Library Selection: NetArchTest.Rules vs ArchUnitNET

NetArchTest.Rules (1.3.2) was chosen over ArchUnitNET based on:
- **Framework Agnostic:** Works with TUnit without adapters
- **Simplicity:** Easy to write, read, and maintain tests
- **Sufficient Capabilities:** Covers all 14 rules needed
- **Performance:** Lightweight with fast execution

See `docs/adr-007-architecture-boundary-enforcement.md` for detailed rationale.

### Known Violations (Exempted)

8 files have documented exemptions for known violations:

1. **Parsing → Platforms (1 file):** `TfPlanJsonContext.cs` - System.Text.Json source generation limitation
2. **Platforms → MarkdownGeneration (4 files):** Value formatters should move to MarkdownGeneration layer (future refactoring)
3. **MarkdownGeneration → Providers (3 files):** AOT script mapping should use provider self-registration (future refactoring)

All exemptions include inline justification comments and are documented in `docs/architecture-rules.md`.

## Related Issues

This feature addresses findings from multi-model analysis:
- #312 - Multi-model analysis findings (M-2: Architecture boundary enforcement)
- #313 - Multi-model analysis findings
- #314 - Multi-model analysis findings  
- #319 - Multi-model analysis findings

## Testing

- ✅ All 14 architecture tests pass
- ✅ Tests execute in 2.5 seconds (target: <10s)
- ✅ Manual meta-testing validation completed
- ✅ CI integration verified
- ✅ CodeQL security scan: 0 alerts
- ✅ Markdownlint: 0 errors

## Next Steps

Future work to resolve exempted violations:
- Refactor value formatters from Platforms to MarkdownGeneration (4 files)
- Refactor AOT script mapping to use provider self-registration (3 files)

These refactorings are tracked as technical debt but do not block this release.
