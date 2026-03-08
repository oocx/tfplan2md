# Feature: Low-Risk Code Quality Improvements

## Overview

This feature improves the maintainability of `tfplan2md` through a focused pass of small, low-risk refactorings. This work is explicitly a separate new refactoring pass: any previous refactoring in the repository is background context only and must not constrain what may be selected in this effort. The goal is to make the codebase easier to understand, extend, and review without changing CLI behavior, rendered markdown output, supported workflows, or published binary characteristics.

## User Goals

- **Maintainers** can change shared logic in one place instead of updating multiple duplicated implementations.
- **Maintainers** can reason about class responsibilities more easily because overloaded classes and dependency-heavy constructors are reduced or clarified.
- **Contributors** can follow more consistent implementation patterns across the codebase.
- **Reviewers** can validate changes faster because the affected code is simpler, more uniform, and easier to trace.
- **Release owners** can accept the improvements with confidence because the work is intentionally surgical and behavior-preserving.

## Scope

### In Scope

- A bounded set of valuable, low-risk code-quality improvements suitable for a single surgical pull request.
- A fresh evaluation of current code-quality opportunities based on the present codebase, without treating earlier refactoring decisions as scope boundaries for this pass.
- Improvements that reduce or remove:
  - classes with too many responsibilities
  - constructors with too many parameters when that reflects unclear responsibility boundaries or unnecessary dependency flow
  - duplicate code and repeated implementation patterns
  - code paths that are harder to read or reason about than necessary
  - inconsistent implementations that should follow one shared approach
- Use of modern language features already available in the current project toolchain when they simplify the code without introducing extra framework or package dependencies.
- Refactorings that preserve the repository's existing constraints, including NativeAOT compatibility, Pure DI style, and no increase in user-visible surface area.
- Low-risk improvements in the main runtime project and, when clearly justified by the same duplicated or inconsistent pattern, closely related tests or supporting project code.

### Out of Scope

- New CLI commands, options, flags, or configuration.
- Changes to rendered markdown content or formatting, except for behavior-preserving internal rewrites.
- Broad architectural rewrites, subsystem replacements, or speculative cleanup unrelated to current maintenance pain.
- Reopening or redesigning earlier refactorings solely for the sake of revisiting history rather than improving currently identifiable code-quality hotspots.
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
- [ ] The selected work is based on a fresh pass over current code-quality hotspots rather than being constrained by a previous refactoring pass.
- [ ] At least one meaningful hotspot involving duplication, excessive responsibility, unnecessary complexity, or implementation inconsistency is reduced or removed.
- [ ] Any class or constructor targeted by this feature has clearer responsibility boundaries or simpler dependency flow after the change.
- [ ] Any modern language features used come from the existing project stack and do not require new framework or package dependencies.
- [ ] The resulting changes preserve user-visible behavior, including CLI usage and rendered markdown output.
- [ ] The resulting code is more consistent with existing project conventions and easier to review than before.
- [ ] The final change set remains small and surgical enough to review, validate, and revert safely if needed.

## Open Questions

None. The current requirements assume a low-risk, small curated subset of the highest-value findings, with supporting tests or related project code included only when they are part of the same behavior-preserving simplification.
