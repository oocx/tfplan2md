# Copilot Instructions

This document provides generic guidelines for AI agents.

For project-specific instructions, refer to the `docs/spec.md` file in the repository.

## Coding Pattern Preferences

- Always prefer simple solutions
- Avoid adding code for features not explicitly requested
- Avoid overengineering solutions
- Avoid duplication of code whenever possible, which means checking for other areas of the codebase that might already have similar code and functionality
- You are careful to only make changes that are requested or you are confident are well understood and related to the change being requested
- When fixing an issue or bug, do not introduce a new pattern or technology without first exhausting all options for the existing implementation. And if you finally do this, make sure to remove the old implementation afterwards so we don't have duplicate logic
- Keep the codebase very clean and organized
- Avoid having files over 200–300 lines of code. Refactor at that point.

## Coding Workflow Preferences

- Focus on the areas of code relevant to the task
- Do not touch code that is unrelated to the task
- Write thorough tests for all major functionality
- Avoid making major changes to the patterns and architecture of how a feature works, after it has shown to work well, unless explicitly instructed
- Always think about what other methods and areas of code might be affected by code changes
- Only make changes and give answers with high confidence. "I don't know" is an acceptable answer when you are not sure about something
- When asking the user for more information, focus on one topic at a time
- When you have follow-up questions, ask them one at a time, and wait for the answer before asking the next question. **Never list multiple questions or considerations at once, even in plans or summaries.**
- Always ensure that you have up-to-date information. Use web search if you think that your information might be outdated (example: new versions of frameworks, libraries, tools, etc.)

## Documentation
- Ensure that you follow instructions from the `docs/spec.md` and other documentation files in the /docs folder
- Ensure that all documentation files are updated with any code changes
- Ensure that all documentation files are consistent with each, do not contradict each other and do not contain outdated information
- When writing documentation, ensure that it is clear, concise, and easy to understand
- Ensure that the implementation and the documentation are aligned with each other