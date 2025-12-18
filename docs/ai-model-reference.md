# AI Model Reference for GitHub Copilot Agents

**Last Updated**: December 19, 2025  
**Data Source**: LiveBench 2025-11-25, GitHub Copilot Documentation

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

### Category Definitions

- **Coding**: Code generation, debugging, understanding
- **Reasoning**: Complex problem-solving, logical thinking
- **Math**: Mathematical problem-solving
- **Data Analysis**: Working with structured data
- **Language**: Natural language understanding and generation
- **Instruction Following**: Following specific format/structure requirements

### Top Performers by Category

#### Coding (Critical for: Developer, Code Reviewer)

| Model | Score | Notes |
|-------|-------|-------|
| GPT-5.1 Codex Max | 90.80 | **Best overall** |
| GPT-5.1 Codex | 87.97 | Strong alternative |
| GPT-5 Pro | 87.59 | Solid choice |
| Claude 4.5 Opus Thinking | 84.12 | Expensive (3x) |
| Gemini 3 Pro | 79.89 | Good balance |
| Gemini 3 Flash | 75.40 | Cost-effective |
| Claude Sonnet 4.5 | 45.72 | **Poor for coding** |

#### Reasoning (Critical for: Architect, Code Reviewer)

| Model | Score | Notes |
|-------|-------|-------|
| Claude 4 Sonnet | 80.74 | Best Sonnet |
| Claude Sonnet 4.5 Thinking | 80.36 | Reasoning mode |
| Claude 4.5 Opus Thinking | 79.65 | Premium |
| Claude Sonnet 4.5 | 76.07 | Solid choice |
| GPT-5.2 | 76.07 | Tied with Sonnet |
| Gemini 3 Pro | 74.60 | Good alternative |
| GPT-5.1 Codex Max | 74.98 | Strong all-around |

#### Language (Critical for: Requirements Engineer, Documentation Author)

| Model | Score | Notes |
|-------|-------|-------|
| Gemini 3 Flash | 84.56 | Excellent |
| Gemini 3 Pro | 84.62 | Excellent |
| Claude 4.5 Opus Thinking | 81.26 | Premium |
| Claude 4.1 Opus | 76.75 | Expensive |
| Claude Sonnet 4.5 | 76.00 | Good choice |
| GPT-5.1 Codex Max | 76.06 | Solid |

#### Instruction Following (Critical for: Product Owner, Quality Engineer, Release Manager)

| Model | Score | Notes |
|-------|-------|-------|
| Gemini 3 Flash | 74.86 | **Best value** |
| GPT-5.1 Codex Max | 73.90 | Solid |
| Gemini 3 Pro | 65.85 | Good |
| Claude 4.5 Opus Medium | 28.11 | Poor |
| Claude Sonnet 4.5 | 23.52 | **Very poor** |

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

- **0x (Included)**: GPT-4.1, GPT-5 mini, Raptor mini - Don't count against quota
- **0.25x**: Grok Code Fast 1 - Very cheap (temporary promo)
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
- **Verify model names match exactly** - GitHub Copilot is case-sensitive

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

- **LiveBench**: https://livebench.ai/ - Contamination-free LLM benchmark
- **GitHub Copilot Models**: https://docs.github.com/en/copilot/reference/ai-models/supported-models
- **Model Comparison**: https://docs.github.com/en/copilot/reference/ai-models/model-comparison
- **Premium Requests**: https://docs.github.com/en/copilot/managing-copilot/monitoring-usage-and-entitlements/about-premium-requests
