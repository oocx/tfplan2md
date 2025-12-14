# Project specification

## Project Overview
`tfplan2md` is a CLI tool that converts Terraform plan JSON files into human-readable markdown reports. It is built using modern .NET 10 and C# 13 features, emphasizing clean architecture, testability, and maintainability.

The goal of this tool is to help DevOps and infrastructure teams easily review Terraform plans by generating concise markdown summaries of proposed changes. The summaries must be customizable via template files, and provide a default template out of the box.

## Project Organization
- Use namespaces to oganize the code. The root namespace is `Oocx.TfPlan2Md`
- Use a single project for the CLI tool; use separate projects for tests
- Organize files by feature (e.g., `Parsing`, `MarkdownGeneration`, `CLI`), not by type (e.g., `Models`, `Services`)
- Place all documentation in the /docs folder, except for the README.md at the root
- Key architecture decisions must be documented in separate files per decision. Place those files in /docs/adr-nnn-title.
- The testing strategy is described in /docs/testing-strategy.md
- Features of tfplan2md (from a user perspective) are described in /docs/features.md
