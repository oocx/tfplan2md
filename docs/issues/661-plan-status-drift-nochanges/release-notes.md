# Plan status / drift regressions fixed (Issue 661)

Two regressions in plan status rendering have been fixed. Scope is limited to the plan-status banner and drift-section filtering for no-change plans.

## 🐛 Bug fixes

- **False "Plan is not applyable" warning suppressed for no-change plans.**
  When Terraform produces an effective no-op plan (no resource changes, no drift), the `applyable = false` flag in the plan JSON could trigger a `⛔ Plan is not applyable` warning banner even though the plan posed no real problem. The condition now only fires the banner when the plan is also either errored or genuinely actionable (has changes or drift), matching what practitioners actually need to act on.

- **Drift section no longer emits noise entries for no-op drift.**
  For plans that recorded drift metadata but where every drift entry was a no-op (unchanged resources), the drift section previously rendered empty or no-op blocks. The fix applies the same display-filtering semantics already used for normal resource changes: no-op and fully suppressed drift entries are hidden, keeping the report free of noise for effectively clean baselines.

## 📸 Screenshots

### No false warning — no-change plan renders cleanly

<!-- release-screenshot: selector="h2:has-text('Summary')"; focus="No warning banner shown for no-change plans" -->
![No-change plan: no false non-applyable warning](https://raw.githubusercontent.com/oocx/tfplan2md/v1.45.2/docs/issues/661-plan-status-drift-nochanges/status-no-warning.png)

### Warning correctly shown for actionable non-applyable plans

<!-- release-screenshot: selector="blockquote"; focus="Warning still appears when plan is genuinely non-applyable with changes" -->
![Actionable non-applyable plan: warning still rendered](https://raw.githubusercontent.com/oocx/tfplan2md/v1.45.2/docs/issues/661-plan-status-drift-nochanges/status-with-warning.png)

## 🔗 Commits

- [`f344afb`](https://github.com/oocx/tfplan2md/commit/f344afbb4b6eb837c56032aeb3df83fc23a615fc) fix: suppress non-applyable banner and drift noise for no-change/no-drift baselines
