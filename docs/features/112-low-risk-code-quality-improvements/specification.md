# Feature: Low-Risk Code Quality Improvements

## Overview

This feature improves the maintainability of `tfplan2md` through a focused pass of small, low-risk refactorings. The goal is to make the codebase easier to understand, extend, and review without changing CLI behavior, rendered markdown output, supported workflows, or published binary characteristics.

## User Goals

- **Maintainers** can change shared logic in one place instead of updating multiple duplicated implementations.
- **Maintainers** can reason about class responsibilities more easily because overloaded classes and dependency-heavy constructors are reduced or clarified.
- **Contributors** can follow more consistent implementation patterns across the codebase.
- **Reviewers** can validate changes faster because the affected code is simpler, more uniform, and easier to trace.
- **Release owners** can accept the improvements with confidence because the work is intentionally surgical and behavior-preserving.

## Scope

### In Scope

- A bounded set of valuable, low-risk code-quality improvements suitable for a single surgical pull request.
- Improvements that reduce or remove:
  - classes with too many responsibilities
  - constructors with too many parameters when that reflects unclear responsibility boundaries or unnecessary dependency flow
  - duplicate code and repeated implementation patterns
  - code paths that are harder to read or reason about than necessary
  - inconsistent implementations that should follow one shared approach
- Use of modern language features already available in the current project toolchain when they simplify the code without introducing extra framework or package dependencies.
- Refactorings that preserve the repository's existing constraints, including NativeAOT compatibility, Pure DI style, and no increase in user-visible surface area.

### Out of Scope

- New CLI commands, options, flags, or configuration.
- Changes to rendered markdown content or formatting, except for behavior-preserving internal rewrites.
- Broad architectural rewrites, subsystem replacements, or speculative cleanup unrelated to current maintenance pain.
- New framework or package dependencies added for convenience.
- Refactorings that materially increase binary size or deployment complexity.
- Performance work unless it is an incidental benefit of a simplification that is already in scope.

## User Experience

This is an internal, non-functional feature. From the user's perspective:

- `tfplan2md` is invoked the same way as before.
- Existing reports, diagnostics, and documented workflows remain functionally unchanged.
- Any benefit is indirect: future enhancements and fixes become safer and easier to deliver because the underlying code is cleaner and more consistent.

## Success Criteria

- [ ] A prioritized and bounded set of low-risk code-quality changes is selected for implementation.
- [ ] At least one meaningful hotspot involving duplication, excessive responsibility, unnecessary complexity, or implementation inconsistency is reduced or removed.
- [ ] Any class or constructor targeted by this feature has clearer responsibility boundaries or simpler dependency flow after the change.
- [ ] Any modern language features used come from the existing project stack and do not require new framework or package dependencies.
- [ ] The resulting changes preserve user-visible behavior, including CLI usage and rendered markdown output.
- [ ] The resulting code is more consistent with existing project conventions and easier to review than before.
- [ ] The final change set remains small and surgical enough to review, validate, and revert safely if needed.

## Open Questions

1. Should this initial refactoring pass be limited to the main runtime project (`src/Oocx.TfPlan2Md`), or may it also include low-risk improvements in supporting tools and tests when they share the same duplication or inconsistency patterns?
2. Should implementation target a small curated subset of the highest-value findings, or a broader sweep across all qualifying low-risk findings discovered during analysis?
