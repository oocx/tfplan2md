# Issue: GitHub Code Scanning Findings

## Problem Description

This investigation focused specifically on the GitHub **code scanning** findings for `oocx/tfplan2md`.

Direct access to the repository's code scanning alerts was **not available** in this environment:

- `github-mcp-server-list_code_scanning_alerts` returned `403 Resource not accessible by integration`

Because the exact alert list could not be queried, this analysis identifies the **most likely current CodeQL findings** from the codebase itself, prioritizing findings with strong evidence and minimal-fix paths.

## Most Likely Findings

### 1. Command injection in Docker test fixture

**Confidence:** High  
**Likely alert type:** CodeQL command-line / process argument injection  

**Impacted file:**
- `src/tests/Oocx.TfPlan2Md.TUnit/Docker/DockerFixture.cs#L191-L197`
- `src/tests/Oocx.TfPlan2Md.TUnit/Docker/DockerFixture.cs#L228-L234`

**Evidence:**

Both container-launch paths build a `docker` command by flattening a user-influenced argument list into a single string:

```csharp
var psi = new ProcessStartInfo("docker", string.Join(" ", arguments))
```

The `arguments` list includes caller-supplied `args` from:

- `RunContainerAsync(..., string[]? args = null, ...)`
- `RunContainerWithStdinAsync(..., string[]? args = null, ...)`

This is exactly the pattern CodeQL flags in C#: constructing process arguments with string concatenation instead of `ProcessStartInfo.ArgumentList`.

**Root cause:**

The fixture treats process arguments as a pre-escaped shell string instead of as discrete tokens. Even with `UseShellExecute = false`, CodeQL treats this as unsafe because spaces/quoting are interpreted by the target process invocation boundary and the code bypasses the safe `ArgumentList` API.

---

### 2. Command injection in markdownlint Docker fixture

**Confidence:** High  
**Likely alert type:** CodeQL command-line / process argument injection  

**Impacted file:**
- `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownLintFixture.cs#L117-L125`

**Evidence:**

The markdownlint fixture uses the same unsafe pattern:

```csharp
var arguments = $"run --rm -i {MarkdownLintImage} --stdin";
var psi = new ProcessStartInfo("docker", arguments)
```

This is lower risk than `DockerFixture` because the current interpolated value is constant, but it still matches the insecure process-construction pattern and is a likely CodeQL finding.

**Root cause:**

Arguments are composed as a single string instead of added via `ArgumentList`, so the fixture relies on manual argument formatting instead of the framework's tokenized API.

---

### 3. Path traversal in wildcard expansion for SARIF inputs

**Confidence:** Medium  
**Likely alert type:** CodeQL path injection / path traversal

**Impacted files:**
- `src/Oocx.TfPlan2Md/CodeAnalysis/WildcardExpander.cs#L44-L58`
- `src/Oocx.TfPlan2Md/CodeAnalysis/WildcardExpander.cs#L70-L81`
- call site: `src/Oocx.TfPlan2Md/CodeAnalysis/CodeAnalysisLoader.cs#L43`

**Evidence:**

`WildcardExpander` turns CLI-supplied `--code-analysis-results` patterns into filesystem enumeration roots:

```csharp
var root = ResolveRecursiveRoot(pattern);
foreach (var file in Directory.EnumerateFiles(root, filePattern, SearchOption.AllDirectories))
```

For recursive patterns, the root is derived directly from the raw pattern:

```csharp
var rootCandidate = pattern[..recursiveIndex].TrimEnd(...);
return string.IsNullOrWhiteSpace(rootCandidate)
    ? Directory.GetCurrentDirectory()
    : rootCandidate;
```

No validation rejects `..` traversal segments before the code enumerates files.

**Root cause:**

The code canonicalizes matched file paths only **after** enumeration (`Path.GetFullPath(file)`), but it does not canonicalize and validate the enumeration root **before** using it. That leaves a likely CodeQL data flow from untrusted CLI pattern input to filesystem traversal.

## Minimal Fix Approach

### Fix 1: Tokenize Docker process arguments

Update the affected test fixtures to build `ProcessStartInfo` like this:

- construct with `new ProcessStartInfo { FileName = "docker", ... }`
- add each argument through `psi.ArgumentList.Add(...)`
- stop using `string.Join(" ", arguments)` and interpolated argument strings

**Smallest required code changes:**
- `src/tests/Oocx.TfPlan2Md.TUnit/Docker/DockerFixture.cs`
- `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownLintFixture.cs`

### Fix 2: Validate wildcard roots before directory enumeration

Add a small validation/canonicalization step in `WildcardExpander` so directory enumeration never runs on a root containing path traversal outside the intended resolved location.

The smallest likely acceptable change is:

- resolve the root to a full path before enumeration
- reject patterns whose directory portion contains parent traversal outside the resolved intended root (for example `..` path segments that escape the base path)
- use the validated full path for `Directory.EnumerateFiles`

**Smallest required code changes:**
- `src/Oocx.TfPlan2Md/CodeAnalysis/WildcardExpander.cs`

## Targeted Tests To Add or Update

The repository uses **TUnit** in `src/tests/Oocx.TfPlan2Md.TUnit/`. The smallest targeted tests consistent with existing conventions are:

### 1. Add a focused WildcardExpander security test

**Update:** `src/tests/Oocx.TfPlan2Md.TUnit/CodeAnalysis/WildcardExpanderTests.cs`

Add one test that proves traversal-style patterns are rejected (or otherwise safely normalized, depending on implementation choice), for example:

- `Expand_RecursivePatternWithParentTraversal_ThrowsArgumentException`

This is the only production-code security test clearly needed.

### 2. Add unit tests for safe Docker argument construction

Because the current Docker tests are integration tests that actually launch containers, the minimal targeted test approach is to extract a small internal helper that creates `ProcessStartInfo`, then verify it uses `ArgumentList`.

**Suggested new tests:**
- `src/tests/Oocx.TfPlan2Md.TUnit/Docker/DockerFixtureSecurityTests.cs`
- `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownLintFixtureTests.cs`

**Suggested assertions:**
- the resulting `ProcessStartInfo.ArgumentList` contains each token separately
- arguments containing spaces remain a single token
- no helper falls back to concatenated `Arguments` strings

If the Developer keeps the fix inline and does not extract a helper, the fallback is to rely on existing Docker integration tests plus a smaller regression test around the new helper logic wherever it ends up.

## Findings Ruled Lower Priority

Other file reads/writes in `ProgramEntry.cs` also use user-supplied paths, but those are part of the CLI's explicit contract (`input file`, `output file`) and are a weaker match for the current task than the three findings above. I did not find equally strong evidence of additional current CodeQL alerts beyond these areas.

## Technical Writer Involvement

**Not needed** unless the Developer changes CLI behavior for wildcard patterns in a user-visible way (for example, rejecting previously accepted `--code-analysis-results` patterns). If the fix stays internal and only hardens unsafe path/process handling, Technical Writer involvement can be skipped.
