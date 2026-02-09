# <Short title>

<1–2 sentences. Be explicit about scope (e.g., “bug fixes only”).>

## ✨ Features

- <Only include if there are real new user-visible features>

## 🐛 Bug fixes

- <Symptom → fix (what was wrong, what changed)>

## 📚 Documentation

- <Only include if user-facing docs changed>

<!-- Optional: Screenshots

Include this section only if you have screenshots.

If you list anything under ✨ Features and it changes rendered output, you should include screenshots.
If no suitable screenshots exist yet, generate them using:

```bash
# For release notes (recommended - single 580×400 screenshot)
scripts/generate-release-screenshots.sh \
  --plan examples/example-demo/plan.json \
  --output-prefix feature-name \
  --output-dir docs/features/NNN-feature-name \
  --selector "details:has(summary:has-text('resource'))"

# For website (full control - all variants)
scripts/generate-screenshot.sh --plan ... --output-prefix ... --selector ...
```

Constraints for release notes:
- Max screenshot size: 580×400
- Use the generated `*-crop*.png` files (not `*-lightbox*` or `*-full*`)
- Focus screenshots using `--selector` / `--target-resource-id` so the image shows the relevant part of the plan

## 📸 Screenshots

> GitHub Releases render Markdown images (`![alt](url)`).
> Prefer stable URLs (e.g., raw.githubusercontent.com with a commit SHA), not links to main.

### Before
![Before](https://...)

### After
![After](https://...)

-->

## 🔗 Commits

> List user-facing commits only (exclude task tracking, internal workflow/agent changes, snapshot-only commits unless they reflect a user-visible output change).

- [`<sha>`](https://github.com/oocx/tfplan2md/commit/<sha>) <subject>

## 🚨 Breaking changes

⚠️ <If any, include migration steps>

## ▶️ Getting started (only if usage changed)

> Include this section only when there are changes to how users run the tool (new flags, changed defaults, new required config, etc.).

```bash
# Example
# tfplan2md plan.json > plan.md
```
