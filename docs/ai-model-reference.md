# AI Model Reference for GitHub Copilot Agents

**Last Updated**: December 27, 2025  
**Data Sources**: 
- LiveBench 2025-12-23
- SWE-bench Verified (via mini-SWE-agent)
- Artificial Analysis (Performance & Speed)
- GitHub Copilot Documentation
- Google Cloud Status

This document provides reference data for selecting AI models when creating or modifying custom GitHub Copilot agents. It includes performance benchmarks, availability, cost information, speed/latency metrics, and reliability data to support data-driven model selection.

## Purpose

When designing agents, select models based on:
1. **Task requirements** - What cognitive abilities does the agent need?
2. **Performance benchmarks** - How well does each model perform on relevant tasks?
3. **Availability** - Is the model available in GitHub Copilot Pro for VS Code?
4. **Cost efficiency** - What is the premium request multiplier?
5. **Context window** - Can the model handle your expected input size?
6. **Multimodal support** - Does the agent need vision/image processing capabilities?
7. **Speed & latency** - How quickly does the model need to respond?

## Available Models in GitHub Copilot Pro (VS Code)

Source: [GitHub Copilot Supported Models](https://docs.github.com/en/copilot/reference/ai-models/supported-models)

### OpenAI Models

| Model | Status | Premium Multiplier | Context Window | Multimodal | Notes |
|-------|--------|-------------------|----------------|------------|-------|
| GPT-4.1 | GA | 0x (included) | 1M | Vision | Baseline model |
| GPT-5 | GA | 1x | 400K | - | General purpose |
| GPT-5 mini | GA | 0x (included) | 400K | Vision | Fast, lightweight |
| GPT-5.1 | GA | 1x | 400K | - | Improved reasoning |
| GPT-5.2 | GA | 1x | 400K | - | Latest general model |
| GPT-5-Codex | GA | 1x | 400K | - | Specialized for code |
| GPT-5.1-Codex | GA | 1x | 400K | - | Improved code model |
| GPT-5.1-Codex-Mini | Public Preview | 0.33x | 400K | - | Fast coding |
| GPT-5.1-Codex-Max | GA | 1x | 400K | - | **Best for coding tasks** |

### Anthropic Models

| Model | Status | Premium Multiplier | Context Window | Multimodal | Notes |
|-------|--------|-------------------|----------------|------------|-------|
| Claude Haiku 4.5 | GA | 0.33x | 200K | - | Fast, lightweight |
| Claude Sonnet 4 | GA | 1x | 1M | Vision | Balanced performance |
| Claude Sonnet 4.5 | GA | 1x | 1M | - | Strong language/reasoning |
| Claude Opus 4.1 | GA | 10x | 200K | Vision | Very expensive |
| Claude Opus 4.5 | GA | 3x | 200K | Vision | Premium reasoning |

### Google Models

| Model | Status | Premium Multiplier | Context Window | Multimodal | Notes |
|-------|--------|-------------------|----------------|------------|-------|
| Gemini 2.5 Pro | GA | 1x | 1M | Vision | Balanced multimodal |
| Gemini 3 Flash | Public Preview | 0.33x | 1M | - | **Fast, cost-effective** |
| Gemini 3 Pro | Public Preview | 1x | 1M | - | Strong reasoning |

### Other Models

| Model | Status | Premium Multiplier | Context Window | Multimodal | Notes |
|-------|--------|-------------------|----------------|------------|-------|
| Grok Code Fast 1 | GA | 0.25x | 256K | - | Complimentary (temporary) |
| Raptor mini | Public Preview | 0x (included) | 400K | - | Fine-tuned GPT-5 mini |

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

**Critical Finding**: Claude Sonnet 4.5 (non-thinking) has very poor Instruction Following performance (23.52), making it unsuitable for agents that need to follow templates or structured formats strictly (Task Planner, Quality Engineer).

## SWE-bench Verified Performance (Software Engineering)

Source: [SWE-bench](https://www.swebench.com/) - December 2025  
Environment: mini-SWE-agent (minimal bash environment for fair model comparison)

SWE-bench evaluates models on **real-world software engineering tasks** - resolving actual GitHub issues from open-source projects. The metric shown is % Resolved (percentage of issues successfully fixed).

### Top Performers (% Resolved out of 500 tasks)

| Model | % Resolved | Cost per Task | Notes |
|-------|------------|---------------|-------|
| Claude 4.5 Opus medium | 74.40% | $0.72 | **Best overall SWE performance** |
| Gemini 3 Pro Preview | 74.20% | $0.46 | **Best value for SWE tasks** |
| GPT-5.2 (high reasoning) | 71.80% | $0.52 | Strong reasoning mode |
| Claude 4.5 Sonnet | 70.60% | $0.56 | Good balanced performance |
| GPT-5.2 (standard) | 69.00% | $0.27 | **Best cost per SWE task** |
| Claude 4 Opus | 67.60% | $1.13 | Expensive |
| GPT-5.1-Codex (medium) | 66.00% | $0.59 | Good coding specialist |
| GPT-5.1 (medium reasoning) | 66.00% | $0.31 | Good value |
| GPT-5 (medium reasoning) | 65.00% | $0.28 | Solid performance |
| Kimi K2 Thinking | 63.40% | $0.44 | Good alternative |
| Minimax M2 | 61.00% | $0.43 | Emerging option |
| DeepSeek V3.2 Reasoner | 60.00% | $0.03 | **Extremely cost-effective** |
| GPT-5 mini (medium) | 59.80% | $0.04 | **Amazing value** |
| Gemini 2.5 Pro | 53.60% | $0.29 | Weaker on real SWE tasks |

**Key Insights**:
- **Gemini 3 Pro Preview** excels at real-world software engineering (74.20%) at half the cost of Claude Opus
- **GPT-5.2 standard** offers excellent cost/performance ratio for coding agents ($0.27 per task, 69% success)
- **Gemini 2.5 Pro performs significantly worse** on SWE-bench (53.60%) compared to Gemini 3 Pro Preview (74.20%)
- **DeepSeek V3.2** and **GPT-5 mini** provide exceptional budget options for high-volume coding tasks

## Model Speed & Latency

Source: [Artificial Analysis](https://artificialanalysis.ai/) - December 2025

### Output Speed (Tokens per Second)

Speed matters for interactive agents where users wait for responses.

| Model | Output Speed (t/s) | Performance Tier |
|-------|-------------------|------------------|
| gpt-oss-120B | 392 t/s | ⚡ Ultra Fast |
| Gemini 3 Flash | 221 t/s | ⚡ Very Fast |
| GPT-5.1-Codex | 287 t/s | ⚡ Very Fast |
| Gemini 3 Pro Preview | 147 t/s | 🚀 Fast |
| GPT-5.1 | 115 t/s | 🚀 Fast |
| GPT-5.2 | 114 t/s | 🚀 Fast |
| Claude 4.5 Sonnet | 70 t/s | 🐢 Moderate |
| Claude Opus 4.5 | 44 t/s | 🐢 Slow |
| Kimi K2 Thinking | 93 t/s | 🚀 Fast |
| DeepSeek V3.2 | 30 t/s | 🐢 Slow |

### Time to First Token (Latency)

Lower latency means faster initial response - critical for real-time interactions.

| Model | Latency (seconds) | Performance Tier |
|-------|-------------------|------------------|
| Gemini 3 Flash | 0.5s | ⚡ Instant |
| GPT-5.1-Codex | 0.9s | ⚡ Very Fast |
| Gemini 3 Pro Preview | 30.9s | 🐢 Slow (thinking) |
| GPT-5.1 | 25.9s | 🐢 Slow (processing) |
| GPT-5.2 | 32.3s | 🐢 Slow (processing) |
| Claude 4.5 Sonnet | 1.9s | 🚀 Fast |
| Claude Opus 4.5 | 1.4s | 🚀 Fast |
| DeepSeek V3.2 | 1.5s | 🚀 Fast |

**Key Insights**:
- **Gemini 3 Flash** offers best speed/latency combination for interactive use
- **GPT-5.1-Codex** provides excellent speed for code generation
- **Large reasoning models** (GPT-5.2, Gemini 3 Pro) have high latency due to thinking time
- **Claude models** balance decent speed with low latency for interactive agents

## Artificial Analysis Intelligence Index

Source: [Artificial Analysis](https://artificialanalysis.ai/) - December 2025

Comprehensive benchmark across 10 evaluations (MMLU-Pro, GPQA Diamond, LiveCodeBench, SciCode, AIME 2025, IFBench, AA-LCR, Terminal-Bench, Humanity's Last Exam, τ²-Bench).

### Top Overall Intelligence Scores

| Model | Intelligence Index | Cost to Run Suite | Price per 1M Tokens |
|-------|-------------------|-------------------|---------------------|
| Gemini 3 Pro Preview (high) | 72.85 | $1,201 | $4.50 |
| GPT-5.2 (xhigh) | 72.59 | $1,294 | $4.81 |
| Gemini 3 Flash | 71.27 | $524 | $1.13 |
| Claude Opus 4.5 | 69.77 | $1,498 | $10.00 |
| GPT-5.1 (high) | 69.71 | $859 | $3.44 |
| GPT-5.1-Codex (high) | 67.11 | $697 | $3.44 |
| DeepSeek V3.2 | 66.19 | $382 | $0.28 |
| Kimi K2 Thinking | 66.79 | $380 | $0.90 |
| GPT-5 mini | 63.93 | N/A | $0.69 |

### Hallucination Rates (AA-Omniscience Index)

Lower scores = fewer hallucinations (more reliable)

| Model | Omniscience Score | Reliability Tier |
|-------|------------------|------------------|
| Gemini 3 Flash (reasoning) | 13.0 | ✅ Excellent |
| Gemini 3 Pro Preview (high) | 12.9 | ✅ Excellent |
| Claude Opus 4.5 (thinking) | 10.2 | ✅ Excellent |
| GPT-5.1 (high) | 2.2 | ✅ Very Good |
| Grok 4 | 1.0 | ✅ Very Good |
| Gemini 3 Flash (standard) | -0.9 | ⚠️ Good |
| Claude 4.5 Sonnet (thinking) | -2.1 | ⚠️ Good |
| GPT-5.2 (xhigh) | -4.3 | ⚠️ Good |
| Claude Opus 4.5 (standard) | -6.5 | ⚠️ Acceptable |
| GPT-5.1-Codex (high) | -7.0 | ⚠️ Acceptable |

**Key Insights**:
- **Reasoning/thinking modes** significantly reduce hallucinations
- **Gemini models** show excellent reliability across configurations
- **Higher intelligence** doesn't always mean lower hallucinations

## Reliability & Availability Data

### Google Cloud Status (December 27, 2025)

Source: [Google Cloud Status](https://status.cloud.google.com/)

**Gemini Enterprise**: ✅ Available (Global)  
**Gemini Code Assist**: ✅ Available (Global)  
**Vertex Gemini API**: ✅ Available (All regions)

**Status**: All Google AI services operational. No systemic issues identified with Gemini 3 Pro Preview or Gemini 3 Flash.

### Community Reliability Reports

**Methodology**: Searched GitHub community discussions, Reddit r/github, and HackerNews for reliability issues.

**Findings**:
- **No widespread Gemini 3 Pro issues** found in community discussions
- Some isolated reports of VSCode Copilot freezing (Nov 2024) - not model-specific
- General rate limit discussions for Copilot Pro - affects all models when quota exceeded
- **Conclusion**: No evidence of Gemini 3 Pro reliability problems compared to other models

## Context Window Considerations

Context window size determines how much conversation history and documentation the model can process in a single request.

### Minimum Requirements by Agent Type

| Agent Type | Minimum Context | Recommended Context | Rationale |
|------------|----------------|---------------------|------------|
| **Developer** | 128K | 200K+ | Large files + tests + related code |
| **Code Reviewer** | 128K | 200K+ | Multiple files in PR + context |
| **Architect** | 200K | 1M+ | System design docs + multiple specs |
| **Task Planner** | 128K | 200K | Full issue + related docs |
| **Technical Writer** | 128K | 200K+ | Extensive documentation reading |
| **Requirements Engineer** | 200K | 1M+ | Multiple specification documents |
| **Quality Engineer** | 128K | 200K | Test plans + code coverage |
| **Release Manager** | 128K | 200K | Release notes + changelog |
| **Workflow Engineer** | 200K | 1M+ | All agent files + workflow docs |

**Key Insights**:
- **All GitHub Copilot models** support at least 128K context (handles ~95K tokens of typical content)
- **200K+ models** (Claude Sonnet, Gemini) provide comfortable buffer for complex tasks
- **1M+ models** (Gemini, Claude Sonnet 4) ideal for workflow reviews and comprehensive documentation
- **Context overflow** happens when prompt + history + response exceeds window size

**Project-Specific Data** (from terminal output):
- Large file + tests: ~75K tokens (ScribanHelpers.cs 1094 lines + tests 1125 lines)
- Comprehensive feature: ~120K tokens (spec + implementation + tests)
- Full workflow review: ~150-180K tokens (all agent files + documentation)

## Model Selection Guidelines

### By Agent Type

| Agent Type | Primary Needs | Recommended Models | Rationale |
|------------|--------------|-------------------|-----------|
| **Issue Analyst** | Reasoning + debugging | GPT-5.2, Claude Sonnet 4.5 | Reliable reasoning/debugging (Gemini 3 Pro currently suspended) |
| **Developer** | Code generation, real-world SWE | GPT-5.1 Codex Max, GPT-5.2 | Strong coding + SWE performance (Gemini 3 Pro currently suspended) |
| **Architect** | Complex reasoning, analysis | GPT-5.2, Claude Sonnet 4.5 | Balanced intelligence + reliability (Gemini 3 Pro currently suspended) |
| **Code Reviewer** | Code understanding + reasoning | Claude Sonnet 4.5, Gemini 3 Flash | Strong analysis + different perspective from Developer |
| **Quality Engineer** | Instruction following + structure | Gemini 3 Flash, GPT-5.1 Codex Max | Strong instruction following + reliable execution |
| **Task Planner** | Instruction following + language | Gemini 3 Flash, GPT-5 mini | Best instruction following + cost + speed |
| **Requirements Engineer** | Language + reasoning | Claude Sonnet 4.5, Gemini 3 Flash | Strong language + reasoning (⭐ User: Excellent performance) |
| **Technical Writer** | Language + writing | Claude Sonnet 4.5, Gemini 3 Flash | Excellent language + low latency |
| **Release Manager** | Instruction following + checklists | Gemini 3 Flash | Cost-effective + speed + instruction following |

**Model Selection Priorities by Agent**:

- **Interactive Agents** (chat-focused): Prioritize **speed + low latency** → Gemini 3 Flash, Claude Sonnet 4.5
- **Coding Agents** (generate/review code): Prioritize **SWE-bench scores** → ⚠️ **Gemini 3 Pro Preview suspended due to reliability issues**, GPT-5.2, GPT-5.1 Codex Max
- **High-Frequency Agents** (run often): Prioritize **cost efficiency** → Gemini 3 Flash (0.33x), GPT-5 mini (0x)
- **Template-Based Agents** (strict format): Prioritize **instruction following** → Gemini models, **avoid Claude Sonnet 4.5**

**⚠️ IMPORTANT - Model Diversity for Code Review**: Code Reviewer agent should use a **different model** than Developer agent to provide diverse perspectives and catch issues the original model might miss.

### Cost Considerations

**Premium Request Multipliers** (for Copilot Pro monthly allowance):
- **0x**: GPT-5 mini, GPT-4.1, Raptor mini - Included in base allowance
- **0.25x**: Grok Code Fast 1 - Complimentary (temporary)
- **0.33x**: Claude Haiku 4.5, Gemini 3 Flash, GPT-5.1-Codex-Mini - Very cost-effective
- **1x**: Most standard models (GPT-5, GPT-5.1, GPT-5.2, Gemini 2.5 Pro, Claude Sonnet 4.5, etc.) - Normal cost
- **3x**: Claude Opus 4.5 - Premium
- **10x**: Claude Opus 4.1 - Very expensive

**Best Value Models** (Performance per Dollar):

| Use Case | Best Value Model | Rationale |
|----------|-----------------|-----------|
| **Software Engineering** | Gemini 3 Pro Preview (⚠️ suspended) | 74.20% SWE-bench @ $0.46/task (vs Claude Opus $0.72) — currently suspended due to reliability |
| **General Coding** | GPT-5.2 standard | 69.00% SWE-bench @ $0.27/task - best cost/performance |
| **High-Volume Coding** | GPT-5 mini (0x), DeepSeek V3.2 | 59.80% @ $0.04/task, 60% @ $0.03/task |
| **Interactive Agents** | Gemini 3 Flash (0.33x) | 221 t/s output + 0.5s latency + strong intelligence (71.27) |
| **Instruction Following** | Gemini 3 Flash (0.33x) | Score 74.86, cost-effective, fast |
| **Language Tasks** | Claude Sonnet 4.5 (1x) | Strong language (76.00) + decent speed |
| **Balanced All-Round** | Gemini 3 Pro Preview (1x) | Best SWE, strong intelligence, reasonable speed |

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

- **Don't use overall average scores** - Always check task-specific categories (SWE-bench, LiveBench categories)
- **Don't assume Claude is best for everything** - Claude Sonnet 4.5 has poor instruction following (23.52)
- **Don't assume Gemini 2.5 Pro is better than 3.0** - Gemini 3 Pro Preview significantly outperforms on SWE-bench (74.20% vs 53.60%)
- **⚠️ Don't use Gemini 3 Pro Preview until stability improves** - Real-world usage shows frequent crashes with cryptic errors and multi-minute unavailability periods (Dec 2025)
- **Don't use the same model for Developer and Code Reviewer** - Use different models to get diverse perspectives
- **Don't ignore speed/latency** - 30s latency models frustrate users in interactive scenarios
- **Don't ignore cost** - 10x multiplier models consume quota very quickly
- **Don't use preview models for critical paths** - Unless you need cutting-edge features
- **Don't rely on outdated information** - Models evolve; check multiple benchmarks (LiveBench + SWE-bench)
- **Don't underestimate context needs** - 128K might be insufficient for complex multi-file tasks
- **Don't ignore multimodal needs** - If your agent needs vision, verify model supports it

### ✅ Do

- **Prefer GPT-5.2 for real-world SWE while Gemini 3 Pro Preview is suspended** - Gemini 3 Pro has top SWE-bench (74.20%) but current reliability issues outweigh the benchmark advantage
- **Use GPT-5.2 standard for cost-effective coding** - 69% SWE-bench @ $0.27/task
- **Use Gemini models for instruction following** - Significantly better than Claude (74.86 vs 23.52)
- **Use Gemini 3 Flash for interactive agents** - 221 t/s + 0.5s latency + 0.33x cost
- **Use Claude Sonnet for language/reasoning tasks** - Strong in these categories (avoid for templates)
- **Consider DeepSeek V3.2 for high-volume** - 60% SWE-bench @ $0.03/task (20x cheaper than GPT-5.2)
- **Verify model IDs match exactly** - Use the **Copilot Model ID** string (case-sensitive)
- **Check multiple benchmarks** - LiveBench (synthetic) + SWE-bench (real-world) + Artificial Analysis (speed)
- **Size your context window appropriately** - Use 200K+ for complex tasks, 1M+ for comprehensive work
- **Check vision support explicitly** - Not all models support multimodal input

## Real-World Performance Data (Retrospective Analysis)

This section captures actual performance data from completed feature development cycles, providing evidence-based insights into model effectiveness beyond synthetic benchmarks.

**Data Sources**: Retrospective reports from 4 completed features/issues:
- Custom Report Title (Dec 2025) - 36 requests, 1h 22m
- Consistent Value Formatting (Dec 2025) - ~4h 45m
- Azure Resource ID Formatting (Dec 2026) - 1 day, ~50 turns
- CI Deployment Fix (Dec 2025) - 21 requests, 1h 44m

### Model Performance Findings

#### ✅ Excellent Performers

**Claude Sonnet 4.5** (Requirements Engineer)
- **User Rating**: ⭐⭐⭐⭐⭐ "Excellent performance" across all features
- **Strengths**: Complex reasoning, architecture decisions, clear specification, proactive questioning
- **Real-World Success**: Zero issues reported despite poor instruction following benchmark (23.52)
- **Conclusion**: Benchmark weakness doesn't manifest in requirements work; keep assignment

**GPT-5.1 Codex Max** (Developer)
- **Performance**: Strong code generation, well-tested implementations
- **Strengths**: 317 passing tests (Custom Report Title), clean implementation patterns
- **Benchmark Alignment**: Matches LiveBench #1 Coding score (84.60)
- **Conclusion**: Excellent choice for Developer role

**Gemini 3 Flash** (Task Planner, Release Manager)
- **Performance**: Fast iterations, cost-effective, good instruction following
- **Use Cases**: Quick tasks, simple operations, structured output
- **Benchmark Alignment**: Excellent instruction following (74.86)
- **Conclusion**: Optimal for high-frequency, template-based agents

#### ⚠️ Mixed Results

**GPT-5.2** (UAT Tester, Code Reviewer, Issue Analyst)
- **Strengths**: Strong coding capabilities, thorough verification
- **Weaknesses**: "Subjectively slow" (user), stopped mid-task multiple times, latency/context issues
- **Failure Pattern**: All tool failures in CI Fix retrospective (14 failures, 100% from GPT-5.2)
- **Conclusion**: Good for coding but consider faster alternatives for interactive/UAT work

**Gemini 3 Pro** (Retrospective, Architect, Technical Writer)
- **Performance**: Good results when working, appropriate for structured output
- **Strengths**: Planning, documentation, analysis tasks
- **Benchmark Alignment**: Strong SWE-bench (74.20%), good instruction following (65.85)
- **Conclusion**: Currently suspended - see Critical Issues below

#### ❌ Critical Issues

**🚨 Gemini 3 Pro Preview - RELIABILITY PROBLEMS (Dec 2025)**
- **User Report**: "Often crashed with cryptic error messages"
- **Availability**: "Unavailable for several minutes after crash, retries did not fix"
- **Retrospective Evidence**: "Multiple internal failures requiring retries" (CI Fix retrospective)
- **Impact**: User forced to switch models mid-feature to continue work
- **Status**: ⚠️ **SUSPENDED until stability improves** despite excellent benchmarks
- **Alternatives**: GPT-5.2, Claude Sonnet 4.5, or wait for GA release

**Retrospective Agent - POOR RESULTS**
- **Current Model**: Gemini 3 Pro Preview (1x cost, 1M context)
- **User Rating**: Multiple retrospectives rated agent ⭐-⭐⭐⭐ (1-3 stars)
- **Issues**:
  - "Unable to provide critical analysis" (CI Fix retro)
  - "Gave unjustifiably high ratings" (CI Fix retro)
  - "Failed to cover all agents" (CI Fix retro)
  - "Required terminal access to be re-enabled" (Custom Report retro)
  - "Initial ratings were too lenient" (Custom Report retro)
- **Possible Causes**: Poor instruction following (65.85 vs Flash 74.86), or Gemini 3 Pro instability
- **Recommendation**: Switch to **Gemini 3 Flash** (better instruction following 74.86, more stable) or **Claude Sonnet 4.5** (strong analysis despite IF weakness)

### Agent-Specific Insights

#### Developer Agent
- **Current Model**: GPT-5.1 Codex Max
- **Evidence**: "Strong diagnosis and fix implementation" (CI Fix)
- **Issue**: "High usage of flash/gpt-5.2" instead of assigned model
- **Conclusion**: Assignment correct, but enforce stricter model adherence

#### Code Reviewer Agent
- **Current Model**: GPT-5.2
- **Performance**: ⭐⭐⭐⭐ "Thorough verification, all checks passed"
- **Issue**: "Suggested PR creation (boundary violation)"
- **Diversity Requirement**: User requests **different model than Developer** for diverse perspective
- **Recommendation**: Switch to **Claude Sonnet 4.5** (strong reasoning 76.07) or **Gemini 3 Flash** (fast, different vendor)

#### UAT Tester Agent
- **Current Model**: GPT-5.2
- **Issues**: "Stopped mid-task multiple times" (Custom Report), "GPT-5.2 latency/context issues"
- **Rating**: ⭐⭐⭐ across retrospectives
- **Recommendation**: Consider **Gemini 3 Flash** (faster, 0.5s latency vs 32.3s, better instruction following 74.86 vs 61.77)

#### Task Planner Agent  
- **Current Model**: Gemini 3 Flash
- **Issue**: ⭐⭐ "Struggled to stop after creating the plan, repeatedly attempted to start implementation"
- **Root Cause**: Instruction following issue or prompt problem
- **Model Performance**: Flash has excellent instruction following (74.86)
- **Conclusion**: Keep Gemini 3 Flash, improve agent instructions

### Key Learnings

1. **Benchmarks ≠ Real-World**: Claude Sonnet 4.5 excels despite poor instruction following score (23.52)
2. **Reliability Trumps Performance**: Gemini 3 Pro's excellent benchmarks (74.20% SWE) negated by crashes
3. **Model Diversity Critical**: Code Reviewer needs different model than Developer for varied perspectives
4. **Speed Matters**: GPT-5.2's 32.3s latency caused "subjectively slow" perception and mid-task stops
5. **Context of Use**: Same model (GPT-5.2) performs well in some roles (Code Reviewer ⭐⭐⭐⭐) but poorly in others (UAT ⭐⭐⭐, failures in CI work)

### Recommended Model Changes

| Agent | Current | Recommended | Rationale |
|-------|---------|-------------|----------|
| Requirements Engineer | Claude Sonnet 4.5 | ✅ **Keep** | User: "Excellent performance" |
| Developer | GPT-5.1 Codex Max | ✅ **Keep** | Strong real-world coding results |
| Code Reviewer | GPT-5.2 | 🔄 **Switch to Claude Sonnet 4.5** | Diversity from Developer + strong reasoning |
| UAT Tester | GPT-5.2 | 🔄 **Consider Gemini 3 Flash** | Faster (0.5s vs 32.3s), better IF (74.86 vs 61.77) |
| Retrospective | Gemini 3 Pro | 🔄 **Switch to Gemini 3 Flash** | More stable + better IF (74.86 vs 65.85) |
| Architect | Gemini 3 Pro | ⚠️ **Suspend - Use GPT-5.2** | Reliability issues |
| Workflow Engineer | Gemini 3 Pro | ⚠️ **Suspend - Use GPT-5.2** | Reliability issues |
| Technical Writer | Gemini 3 Pro | ⚠️ **Suspend - Use Claude Sonnet 4.5** | Reliability issues + excellent language (84.62) |
| Task Planner | Gemini 3 Flash | ✅ **Keep** | Good performance, improve instructions |
| Release Manager | Gemini 3 Flash | ✅ **Keep** | Cost-effective, appropriate for checklist work |

**Rationale Summary**:
- ✅ Keep what works: Claude Sonnet 4.5 (Req Engineer), GPT-5.1 Codex Max (Developer), Gemini 3 Flash (Task/Release)
- 🔄 Diversify Code Reviewer: Switch to Claude Sonnet 4.5 (different from Developer, strong reasoning)
- 🔄 Fix Retrospective: Switch to Gemini 3 Flash (more stable, better instruction following)
- ⚠️ Suspend Gemini 3 Pro: Replace in all roles until reliability improves
- 🔄 Optimize UAT: Consider Gemini 3 Flash (faster, better instruction following)

## Update Process

This document should be updated when:
- New models are released by OpenAI, Anthropic, or Google
- LiveBench releases new benchmark data (typically monthly)
- SWE-bench updates leaderboard with new models or results
- Artificial Analysis publishes new performance measurements
- GitHub Copilot changes model availability or pricing
- Agent performance issues suggest model reassessment is needed
- Community reports widespread reliability issues with specific models

**To update this document**:
1. **Benchmark Data**: 
   - Fetch latest from [LiveBench](https://livebench.ai/) (synthetic benchmarks)
   - Check [SWE-bench Verified](https://www.swebench.com/bash-only.html) (real-world coding)
   - Review [Artificial Analysis](https://artificialanalysis.ai/) (speed, latency, hallucinations)
2. **Availability & Cost**: Check [GitHub Copilot Supported Models](https://docs.github.com/en/copilot/reference/ai-models/supported-models)
3. **Reliability**: Check [Google Cloud Status](https://status.cloud.google.com/) and community discussions
4. **Update Tables**: Refresh all performance tables with new data, noting which benchmark each score comes from
5. **Update Metadata**: Change "Last Updated" date and "Data Sources" list at top
6. **Review Recommendations**: Adjust agent-specific recommendations if new data shows significant changes

## References

### Primary Benchmarks

- **LiveBench**: [https://livebench.ai/](https://livebench.ai/) - A challenging, contamination-free LLM benchmark that regularly releases new questions with objective ground-truth answers across categories like Coding, Reasoning, and Instruction Following.
- **SWE-bench**: [https://www.swebench.com/](https://www.swebench.com/) - Real-world software engineering benchmark using actual GitHub issues. The "Bash Only" leaderboard uses mini-SWE-agent for fair model-to-model comparison.
- **Artificial Analysis**: [https://artificialanalysis.ai/](https://artificialanalysis.ai/) - Independent AI model performance analysis covering intelligence, speed, latency, cost, and hallucination rates across 10+ evaluations.

### GitHub Copilot Documentation

- **GitHub Copilot Supported Models**: [https://docs.github.com/en/copilot/reference/ai-models/supported-models](https://docs.github.com/en/copilot/reference/ai-models/supported-models) - Official list of all AI models available in GitHub Copilot, including their release status, plan availability, and premium request multipliers.
- **Model Comparison Guide**: [https://docs.github.com/en/copilot/reference/ai-models/model-comparison](https://docs.github.com/en/copilot/reference/ai-models/model-comparison) - A task-based guide for picking the best model for general-purpose coding, deep reasoning, fast help, or visual tasks.
- **Comparing Models by Task**: [https://docs.github.com/en/copilot/tutorials/compare-ai-models](https://docs.github.com/en/copilot/tutorials/compare-ai-models) - A tutorial with real-world examples, sample prompts, and responses showing how different models handle specific developer tasks.
- **About Premium Requests**: [https://docs.github.com/en/copilot/managing-copilot/monitoring-usage-and-entitlements/about-premium-requests](https://docs.github.com/en/copilot/managing-copilot/monitoring-usage-and-entitlements/about-premium-requests) - Detailed information on how premium requests are calculated, consumed via model multipliers, and managed across different Copilot plans.

### Reliability & Status

- **Google Cloud Status**: [https://status.cloud.google.com/](https://status.cloud.google.com/) - Real-time status of Google Cloud services including Gemini Enterprise and Vertex AI APIs.
- **GitHub Community**: [https://github.com/orgs/community/discussions](https://github.com/orgs/community/discussions) - User-reported issues and discussions about GitHub Copilot and model reliability.

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
