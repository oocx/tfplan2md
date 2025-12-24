# AI Model Reference for GitHub Copilot Agents

**Last Updated**: December 24, 2025  
**Data Source**: LiveBench 2025-12-23, GitHub Copilot Documentation

This document provides reference data for selecting AI models when creating or modifying custom GitHub Copilot agents. It includes performance benchmarks, availability, and cost information to support data-driven model selection.

## Purpose

When designing agents, select models based on:
1. **Task requirements** - What cognitive abilities does the agent need?
2. **Performance benchmarks** - How well does each model perform on relevant tasks?
3. **Availability** - Is the model available in GitHub Copilot Pro for VS Code?
4. **Cost efficiency** - What is the premium request multiplier?

## Available Models in GitHub Copilot Pro (VS Code)

Source: [GitHub Copilot Supported Models](https://docs.github.com/en/copilot/reference/ai-models/supported-models)

### OpenAI Models

| Model | Status | Premium Multiplier | Notes |
|-------|--------|-------------------|-------|
| GPT-4.1 | GA | 0x (included) | Baseline model |
| GPT-5 | GA | 1x | General purpose |
| GPT-5 mini | GA | 0x (included) | Fast, lightweight |
| GPT-5.1 | GA | 1x | Improved reasoning |
| GPT-5.2 | GA | 1x | Latest general model |
| GPT-5-Codex | GA | 1x | Specialized for code |
| GPT-5.1-Codex | GA | 1x | Improved code model |
| GPT-5.1-Codex-Mini | Public Preview | 0.33x | Fast coding |
| GPT-5.1-Codex-Max | GA | 1x | **Best for coding tasks** |

### Anthropic Models

| Model | Status | Premium Multiplier | Notes |
|-------|--------|-------------------|-------|
| Claude Haiku 4.5 | GA | 0.33x | Fast, lightweight |
| Claude Sonnet 4 | GA | 1x | Balanced performance |
| Claude Sonnet 4.5 | GA | 1x | Strong language/reasoning |
| Claude Opus 4.1 | GA | 10x | Very expensive |
| Claude Opus 4.5 | GA | 3x | Premium reasoning |

### Google Models

| Model | Status | Premium Multiplier | Notes |
|-------|--------|-------------------|-------|
| Gemini 2.5 Pro | GA | 1x | Balanced multimodal |
| Gemini 3 Flash | Public Preview | 0.33x | **Fast, cost-effective** |
| Gemini 3 Pro | Public Preview | 1x | Strong reasoning |

### Other Models

| Model | Status | Premium Multiplier | Notes |
|-------|--------|-------------------|-------|
| Grok Code Fast 1 | GA | 0.25x | Complimentary (temporary) |
| Raptor mini | Public Preview | 0x (included) | Fine-tuned GPT-5 mini |

## LiveBench Performance by Category

Source: [LiveBench](https://livebench.ai/) - 2025-11-25 release

**Important:** LiveBench model names often differ from GitHub Copilot's model IDs.

- **LiveBench Model Name**: How the model appears in benchmark results.
- **Copilot Model ID**: The exact string to use in agent frontmatter (`model:`).

### Category Definitions

- **Coding**: Code generation, debugging, understanding
- **Reasoning**: Complex problem-solving, logical thinking
- **Math**: Mathematical problem-solving
- **Data Analysis**: Working with structured data
- **Language**: Natural language understanding and generation
- **Instruction Following**: Following specific format/structure requirements

### Top Performers by Category

#### Coding (Critical for: Developer, Code Reviewer)

| LiveBench Model Name | Copilot Model ID | Score | Notes |
|---------------------|-----------------|-------|-------|
| GPT-5.1 Codex Max | GPT-5.1-Codex-Max | 84.60 | **Best overall** |
| GPT-5.2 | GPT-5.2 | 83.21 | Strong general model |
| GPT-5.1 Codex | GPT-5.1-Codex | 81.98 | Strong alternative |
| GPT-5.1 | GPT-5.1 | 78.79 | Strong general model |
| Gemini 3 Pro | Gemini 3 Pro (Preview) | 77.42 | Good balance |
| Gemini 3 Flash | Gemini 3 Flash (Preview) | 74.55 | Cost-effective |
| Gemini 2.5 Pro | Gemini 2.5 Pro | 70.81 | Balanced |
| GPT-5 mini | GPT-5 mini | 68.32 | **Best free model** |
| GPT-5.1 Codex Mini | GPT-5.1-Codex-Mini | 64.71 | Specialized but weaker |
| Grok Code Fast | Grok Code Fast 1 | 42.30 | Poor performance |
| Claude Haiku 4.5 | Claude Haiku 4.5 | 33.94 | **Avoid for coding** |
| Claude Sonnet 4.5 | Claude Sonnet 4.5 | 42.29 | **Poor for coding** |

#### Reasoning (Critical for: Architect, Code Reviewer)
| Claude Sonnet 4.5 | 76.07 | Strong reasoning |
| GPT-5.2 | GPT-5.2 | 76.07 | Strong general model |
| Gemini 2.5 Pro | Gemini 2.5 Pro | 75.69 | Good reasoning |
| GPT-5.1 Codex Max | GPT-5.1-Codex-Max | 74.98 | Strong all-around |
| Gemini 3 Pro | Gemini 3 Pro (Preview) | 74.60 | Good alternative |
| GPT-5.1 | GPT-5.1 | 72.49 | Decent reasoning |
| Claude Haiku 4.5 | Claude Haiku 4.5 | 72.17 | Good for size |
| GPT-5.1 Codex Mini | GPT-5.1-Codex-Mini | 69.93 | Acceptable |
| GPT-5 mini | GPT-5 mini | 68.20 | **Great value** |
| Grok Code Fast | Grok Code Fast 1 | 64.44 | Weak| Premium |
| GPT-5.2 | GPT-5.2 | 76.07 | Strong general model |
| Claude SoPro | Gemini 3 Pro (Preview) | 84.62 | Excellent |
| Gemini 3 Flash | Gemini 3 Flash (Preview) | 84.56 | Excellent |
| GPT-5.2 | GPT-5.2 | 79.81 | Strong general model |
| GPT-5.1 | GPT-5.1 | 79.26 | Strong language |
| Claude Sonnet 4.5 | Claude Sonnet 4.5 | 76.00 | Good choice |
| GPT-5.1 Codex Max | GPT-5.1-Codex-Max | 76.06 | Solid |
| GPT-5 mini | GPT-5 mini | 75.52 | **Excellent for free** |
| Gemini 2.5 Pro | Gemini 2.5 Pro | 75.50 | Good |
| GPT-5.1 Codex Mini | GPT-5.1-Codex-Mini | 63.01 | Weak |
| Claude Haiku 4.5 | Claude Haiku 4.5 | 57.05 | Weak |
| Grok Code Fast | Grok Code Fast 1 | 48.56 | Poores |
|---------------------|-----------------|-------|-------|
| Gemini 3 Flash | Gemini 3 Flash (Preview) | 84.56 | Excellent |
| Gemini 3 Pro | Gemini 3 Pro (Preview) | 84.62 | Excellent |
| Claude 4.5 Opus Thinking | N/A (benchmark-only variant) | 81.26 | Premium |
| GPT-5.2 | GPT-5.2 | 79.81 | Strong general model |
| Claude 4.1 Opus | Claude Opus 4.1 | 76.75 | Expensive |
| Claude Sonnet 4.5 | Claude Sonnet 4.5 | 76.00 | Good choice |
| GPT-5.1 Codex Max | GPT-5.1-Codex-Max | 76.06 | Solid |
 mini | GPT-5 mini | 65.27 | **Excellent free option** |
| GPT-5.1 | GPT-5.1 | 63.90 | Decent |
| GPT-5.2 | GPT-5.2 | 61.77 | Acceptable |
| GPT-5.1 Codex Mini | GPT-5.1-Codex-Mini | 59.02 | Mediocre |
| Gemini 2.5 Pro | Gemini 2.5 Pro | 33.07 | Poor |
| Grok Code Fast | Grok Code Fast 1 | 22.27 | Very poor |
| Claude Haiku 4.5 | Claude Haiku 4.5 | 17.75 | **Avoid**eer, Release Manager)

| LiveBench Model Name | Copilot Model ID | Score | Notes |
|---------------------|-----------------|-------|-------|
| Gemini 3 Flash | Gemini 3 Flash (Preview) | 74.86 | **Best value** |
| GPT-5.1 Codex Max | GPT-5.1-Codex-Max | 73.90 | Solid |
| Gemini 3 Pro | Gemini 3 Pro (Preview) | 65.85 | Good |
| GPT-5.2 | GPT-5.2 | 61.77 | Acceptable |
| Claude 4.5 Opus Medium | N/A (benchmark-only variant) | 28.11 | Poor |
| Claude Sonnet 4.5 | Claude Sonnet 4.5 | 23.52 | **Very poor** |

**Critical Finding**: Claude Sonnet 4.5 (non-thinking) has very poor Instruction Following performance (23.52), making it unsuitable for agents that need to follow templates or structured formats strictly (Product Owner, Quality Engineer).

## Model Selection Guidelines

### By Agent Type

| Agent Type | Primary Needs | Recommended Models | Rationale |
|------------|--------------|-------------------|-----------|
| **Developer** | Code generation, debugging | GPT-5.1 Codex Max | Top coding score (90.80) |
| **Architect** | Complex reasoning, analysis | Gemini 3 Pro, Claude Sonnet 4 | Balanced reasoning + coding |
| **Code Reviewer** | Code understanding + reasoning | Claude Sonnet 4.5, GPT-5.1 Codex Max | Deep analysis + code understanding |
| **Quality Engineer** | Instruction following + structure | Gemini 3 Pro, Gemini 3 Flash | Strong instruction following (65-75) |
| **Product Owner** | Instruction following + language | Gemini 3 Flash, Gemini 3 Pro | Best instruction following + cost |
| **Requirements Engineer** | Language + reasoning | Claude Sonnet 4.5 | Strong language (76.00) + reasoning (76.07) |
| **Documentation Author** | Language + writing | Claude Sonnet 4.5 | Excellent language skills |
| **Release Manager** | Instruction following + checklists | Gemini 3 Flash | Cost-effective + good instruction following |

### Cost Considerations

**Premium Request Multipliers** (for Copilot Pro monthly allowance):
PT-5 mini**: 0x multiplier (Free), surprisingly strong performance (beats paid models in IF/Coding).
- **Gemini 3 Flash**: 0.33x multiplier, strong across multiple categories.
- **GPT-5.1 Codex Max**: 1x multiplier, best coding performance.
- **Claude Sonnet 4.5**: 1x multiplier, strong language/reasoning (but poor instruction following).
- **0.33x**: Claude Haiku 4.5, Gemini 3 Flash, GPT-5.1-Codex-Mini - Cost-effective
- **1x**: Most standard models - Normal cost
- **3x**: Claude Opus 4.5 - Premium
- **10x**: Claude Opus 4.1 - Very expensive

**Best Value Models**:
- **Gemini 3 Flash**: 0.33x multiplier, strong across multiple categories
- **GPT-5.1 Codex Max**: 1x multiplier, best coding performance
- **Claude Sonnet 4.5**: 1x multiplier, strong language/reasoning (but poor instruction following)

## Model Selection Process

When selecting a model for an agent:

1. **Identify task requirements**
   - What is the agent's primary responsibility?
   - What cognitive abilities are most critical?

2. **Check category-specific benchmarks**
   - Look up performance in relevant LiveBench categories (see tables above)
   - Compare top performers in that category

3. **Consider cost vs performance**
   - Is this a high-frequency agent? (favor lower multipliers)
   - Is accuracy critical? (favor higher-performing models)
   - Balance cost with capability needs

4. **Verify availability**
   - Confirm model is available in GitHub Copilot Pro for VS Code
   - Check if model is GA or Public Preview

5. **Document rationale**
   - Include reasoning in agent definition or commit message
   - Reference specific benchmark scores when relevant

## Common Pitfalls

### ❌ Don't Do

- **Don't use overall average scores** - Always check task-specific categories
- **Don't assume Claude is best for everything** - Claude Sonnet 4.5 has poor instruction following
- **Don't ignore cost** - 10x multiplier models consume quota very quickly
- **Don't use preview models for critical paths** - Unless you need cutting-edge features
- **Don't rely on outdated information** - Models evolve; update this document regularly

### ✅ Do

- **Use GPT-5.1 Codex Max for coding** - Clear leader in coding benchmarks
- **Use Gemini models for instruction following** - Significantly better than Claude
- **Use Claude Sonnet for language/reasoning** - Strong in these categories
- **Consider Gemini 3 Flash for high-volume** - 0.33x multiplier with good performance
- **Verify model IDs match exactly** - Use the **Copilot Model ID** string (case-sensitive)

## Update Process

This document should be updated when:
- New models are released by OpenAI, Anthropic, or Google
- LiveBench releases new benchmark data (typically monthly)
- GitHub Copilot changes model availability or pricing
- Agent performance issues suggest model reassessment is needed

**To update this document**:
1. Fetch latest data from [LiveBench](https://livebench.ai/)
2. Check [GitHub Copilot Supported Models](https://docs.github.com/en/copilot/reference/ai-models/supported-models)
3. Update tables with new data
4. Update "Last Updated" date at top
5. Review and adjust agent recommendations if needed

## References

- **LiveBench**: [https://livebench.ai/](https://livebench.ai/) - A challenging, contamination-free LLM benchmark that regularly releases new questions with objective ground-truth answers across categories like Coding, Reasoning, and Instruction Following.
- **GitHub Copilot Supported Models**: [https://docs.github.com/en/copilot/reference/ai-models/supported-models](https://docs.github.com/en/copilot/reference/ai-models/supported-models) - Official list of all AI models available in GitHub Copilot, including their release status, plan availability, and premium request multipliers.
- **Model Comparison Guide**: [https://docs.github.com/en/copilot/reference/ai-models/model-comparison](https://docs.github.com/en/copilot/reference/ai-models/model-comparison) - A task-based guide for picking the best model for general-purpose coding, deep reasoning, fast help, or visual tasks.
- **Comparing Models by Task**: [https://docs.github.com/en/copilot/tutorials/compare-ai-models](https://docs.github.com/en/copilot/tutorials/compare-ai-models) - A tutorial with real-world examples, sample prompts, and responses showing how different models handle specific developer tasks.
- **About Premium Requests**: [https://docs.github.com/en/copilot/managing-copilot/monitoring-usage-and-entitlements/about-premium-requests](https://docs.github.com/en/copilot/managing-copilot/monitoring-usage-and-entitlements/about-premium-requests) - Detailed information on how premium requests are calculated, consumed via model multipliers, and managed across different Copilot plans.

## Independent LiveBench mappings (actionable evidence)

The table below maps Copilot model IDs from this document to the closest independent LiveBench leaderboard variant (search links provided). Use these LiveBench rows as the primary independent evidence when updating model recommendations. Where a clear LiveBench variant was not found, the model is flagged as "No independent scores found" or "Ambiguous".

- **GPT-5.1-Codex-Max**: LiveBench variant "GPT-5.1 Codex Max" — https://livebench.ai/?search=GPT-5.1%20Codex%20Max (strong coding + instruction-following scores; actionable)
- **GPT-5 family (GPT-5, GPT-5.1, GPT-5.2, GPT-5 Pro, GPT-5 mini)**: LiveBench variants "GPT-5.2 High", "GPT-5 Pro", "GPT-5.1 High", "GPT-5 Mini High" — https://livebench.ai/?search=GPT-5 (independent per-category scores available; verify exact Copilot ID → LiveBench variant mapping before changing recommendations)
- **GPT-5-Codex / GPT-5.1-Codex / GPT-5.1-Codex-Mini**: LiveBench variants include "GPT-5.1 Codex" and "GPT-5.1 Codex Max" — https://livebench.ai/?search=GPT-5.1%20Codex (Codex and Codex Max: actionable; Codex-Mini: limited independent runs)
- **Claude Haiku 4.5**: LiveBench variant "Claude Haiku 4.5" — https://livebench.ai/?search=Claude%20Haiku%204.5 (independent per-category scores available)
- **Claude Opus 4.1 / Opus 4.5**: LiveBench variants "Claude Opus" / "Claude Opus 4.1/4.5" — https://livebench.ai/?search=Claude%20Opus (actionable; map effort/thinking modes to Copilot IDs)
- **Gemini 2.5 Pro**: LiveBench variant "Gemini 2.5 Pro" — https://livebench.ai/?search=Gemini%202.5%20Pro (independent scores available)
- **Raptor mini**: No independent LiveBench / HELM rows found for the exact "Raptor mini" ID — https://livebench.ai/?search=Raptor%20mini (flagged: No independent scores found; recommend vendor confirmation)
- **GPT-4.1**: Ambiguous mapping on LiveBench (no unambiguous row labelled exactly "GPT-4.1"); related GPT-4 variant data exists but treat as "Ambiguous" — https://livebench.ai/?search=GPT-4.1 (recommend caution and cross-checking before using as primary evidence)

Notes:
- Primary independent source: LiveBench (https://livebench.ai/) and its Hugging Face datasets (https://huggingface.co/collections/livebench/livebench-67eaef9bb68b45b17a197a98).
- "Effort" / "Thinking" / "High" tags in LiveBench affect scores — always record the exact LiveBench variant (including effort) when citing scores.
- For models flagged as "No independent scores found" or "Ambiguous", document that gap in agent guidance and recommend vendor/model-card confirmation or targeted evaluation.
