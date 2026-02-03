# Category Weights Configuration

This document defines the weighting scheme for evaluating code quality findings across different categories.

## Category Weights

Based on the repository's emphasis on correctness and comprehensive testing (as documented in docs/spec.md and the testing-first culture), the following weights are applied:

| Category | Weight | Rationale |
|----------|--------|-----------|
| **Correctness** | 1.0 | Highest priority - functional correctness is critical |
| **Testing** | 0.9 | Strong testing culture with TUnit adoption and 370-test suite |
| **Code Quality** | 0.7 | Important for maintainability but secondary to correctness |
| **Documentation** | 0.6 | XML docs are required, but less critical than functionality |
| **Architecture** | 0.5 | Important for long-term maintainability |
| **Access Modifiers** | 0.5 | CLI tool conventions, not a library |
| **Code Comments** | 0.4 | XML docs covered in Documentation; inline comments less critical |

## Quality Score Scale (0-3)

Each finding is assigned a quality score per model indicating how well the model detected and described the issue:

- **0**: Model missed the finding entirely
- **1**: Model identified the general area but was vague (e.g., "improve code quality" without specifics)
- **2**: Model identified the specific issue with file/line references
- **3**: Model provided actionable recommendation with evidence, examples, or proposed solutions

## Composite Score Calculation

The composite score for each model is calculated as:

**Composite Score = (0.30 × Precision) + (0.25 × Detection Rate) + (0.25 × Weighted Quality Score) + (0.10 × Severity Accuracy) + (0.10 × Coverage Breadth)**

Where:
- **Precision** = Valid findings / Total findings reported
- **Detection Rate** = Unique findings caught / Total unique findings
- **Weighted Quality Score** = Σ(quality_score × category_weight) / max_possible
- **Severity Accuracy** = % of findings correctly classified (Blocker/Major/Minor)
- **Coverage Breadth** = Categories with findings / 7 total categories

## Validation Statuses

Each finding is validated against repository standards:

- ✅ **Valid**: Finding is accurate, verifiable, and aligns with repo standards
- ⚠️ **Partially Valid**: Finding has merit but contains inaccuracies or is overstated
- ❌ **False Positive**: Finding is incorrect, not applicable, or misunderstands the codebase
