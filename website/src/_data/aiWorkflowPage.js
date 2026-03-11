module.exports = {
  hero: {
    title: "AI Development",
    highlightText: "Workflow",
    subtitle: "tfplan2md is developed using a multi-agent AI workflow where specialized agents handle different phases of the development lifecycle."
  },
  overview: [
    "This project uses an agent-based workflow for feature development, inspired by best practices from GitHub Copilot agents and modern software engineering principles. Each agent is a specialized AI assistant with a clear responsibility in the development process.",
    "The workflow is coordinated by a human Maintainer who manages handoffs between agents and provides clarifications as needed. Agents produce artifacts as markdown files in the repository, creating a traceable development history."
  ],
  benefits: [
    { icon: "🎯", title: "Clear Responsibilities", description: "Each agent has a single, well-defined role in the workflow" },
    { icon: "📝", title: "Traceable Artifacts", description: "All decisions and changes are documented in markdown files" },
    { icon: "🔄", title: "Consistent Process", description: "Standardized workflow ensures quality and completeness" },
    { icon: "🤖", title: "AI-Powered", description: "Leverages GitHub Copilot's multi-model capabilities" }
  ],
  diagram: {
    title: "Workflow Diagram",
    description: "The diagram below shows the complete agent workflow from requirements to release. Each agent produces artifacts that are consumed by the next agent in the sequence.",
    note: "Agents produce and consume artifacts. Solid arrows show artifact creation and consumption. Dashed arrows indicate rework/feedback loops."
  },
  agents: [
    { emoji: "📋", title: "Requirements Engineer", description: "Gathers and clarifies requirements for new features" },
    { emoji: "🔍", title: "Issue Analyst", description: "Investigates bugs and technical issues" },
    { emoji: "🏗️", title: "Architect", description: "Designs solutions and documents decisions" },
    { emoji: "✅", title: "Quality Engineer", description: "Defines test plans and acceptance criteria" },
    { emoji: "💻", title: "Developer", description: "Implements features and tests" },
    { emoji: "📝", title: "Technical Writer", description: "Updates and maintains documentation" },
    { emoji: "👀", title: "Code Reviewer", description: "Reviews code quality and standards" },
    { emoji: "🧪", title: "UAT Tester", description: "Validates user-facing features" },
    { emoji: "🚀", title: "Release Manager", description: "Prepares and executes releases" },
    { emoji: "🔄", title: "Retrospective", description: "Identifies improvement opportunities" },
    { emoji: "⚙️", title: "Workflow Engineer", description: "Improves the workflow itself" },
    { emoji: "🎨", title: "Web Designer", description: "Maintains the project website" }
  ],
  processSteps: [
    { number: "1", title: "Entry Point", description: "The Maintainer identifies a need (new feature, bug fix, or workflow improvement) and starts with the appropriate entry agent." },
    { number: "2", title: "Agent Collaboration", description: "Each agent produces artifacts (markdown documents) that serve as inputs for the next agent in the workflow." },
    { number: "3", title: "Traceability", description: "All decisions, requirements, and changes are documented in versioned artifact files in the repository." },
    { number: "4", title: "Quality Gates", description: "Code Reviewer and UAT Tester validate changes before Release Manager creates the pull request." },
    { number: "5", title: "Continuous Improvement", description: "Retrospective analyzes the process and provides feedback to the Workflow Engineer for improvements." }
  ],
  executionModes: [
    {
      title: "🖥️ Local Mode (VS Code)",
      items: [
        "Interactive development with Maintainer",
        "Design decisions and debugging",
        "Full tool access (edit, execute, preview)",
        "Best for complex tasks requiring guidance"
      ]
    },
    {
      title: "☁️ Cloud Mode (GitHub)",
      items: [
        "Automated execution from GitHub issues",
        "Well-scoped batch updates",
        "Creates pull requests autonomously",
        "Best for routine, well-defined tasks"
      ]
    }
  ],
  ctas: [
    { href: "https://github.com/oocx/tfplan2md/blob/main/docs/agents.md", label: "📖 Read Full Documentation (agents.md)", variant: "primary", external: true },
    { href: "contributing.html", label: "🤝 Contributing Guide", variant: "secondary" }
  ]
};