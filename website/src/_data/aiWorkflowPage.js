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
    description: "Read Feature, Bug fix, and Website from left to right, with numbered stages running from top to bottom in each lane. The separate Workflow improvement lane starts only when the Maintainer requests it.",
    note: "Arrows show the forward sequence. Architecture approval applies only when options compete; UAT is skipped when output does not change. Feature and bug-fix retrospectives follow the completed, verified release."
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
    { emoji: "🚀", title: "Release Manager", description: "Merges approved work, publishes the release, and verifies its artifacts" },
    { emoji: "🔄", title: "Retrospective", description: "Reviews feature and bug-fix work after the release is complete" },
    { emoji: "⚙️", title: "Workflow Engineer", description: "Improves the workflow when the Maintainer requests separate maintenance work" },
    { emoji: "🎨", title: "Web Designer", description: "Maintains the project website" }
  ],
  processSteps: [
    { number: "1", title: "Entry Point", description: "The Maintainer starts feature, bug-fix, or website delivery through its entry role. Workflow improvement is a separate process started only on an explicit Maintainer request." },
    { number: "2", title: "Agent Collaboration", description: "Each agent produces artifacts (markdown documents) that serve as inputs for the next agent in the workflow." },
    { number: "3", title: "Traceability", description: "All decisions, requirements, and changes are documented in versioned artifact files in the repository." },
    { number: "4", title: "Quality Gates", description: "Code Reviewer and, when user-visible output changes, UAT Tester validate delivery work before Release Manager creates the pull request." },
    { number: "5", title: "Post-release Learning", description: "After a feature or bug-fix release is published and verified, Retrospective records evidence-based findings for the Maintainer. Those findings do not start workflow changes automatically." }
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
    { href: "https://github.com/oocx/tfplan2md/blob/main/docs/workflow.md", label: "📖 Read Full Documentation (workflow.md)", variant: "primary", external: true },
    { href: "contributing.html", label: "🤝 Contributing Guide", variant: "secondary" }
  ]
};
