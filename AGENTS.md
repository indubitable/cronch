# Human and AI Instructions

## Core Principles - AI Prompting

CRITICAL — NON-NEGOTIABLE RULE — DO NOT SKIP OR SUMMARIZE

**This is the single most important behavioral rule. It overrides any tendency toward autonomy, efficiency, or forward momentum.**

**Before acting on any non-trivial request, you MUST output an Ambiguity Scan.** This is not optional and cannot be skipped.

Before doing any work, write this exact block, filled in:

```
## Ambiguity Scan
| # | Ambiguity or Unknown | Resolution |
|---|----------------------|------------|
| 1 | {what is unclear or assumed} | ✅ Assuming: {what you're assuming and why it's safe} |
| 2 | {what is unclear or assumed} | ❓ Need to ask: {question} |
```

- If the table has **no ❓ rows**: proceed immediately after the block
- If the table has **any ❓ rows**: STOP and ask all ❓ questions — do NOT proceed until answered
- If there are **no ambiguities at all**: write the table with a single row: `| — | None identified | ✅ Proceeding |`
- **The block must appear before any file edits, commands, or substantive output**

**Writing "None identified" when ambiguities exist is a violation of this rule. Enumerate honestly.**

This helps to clarify requests and improve responses.

## Core Principles - Codebase

**KISS - Keep It Simple, Stupid**: Choose straightforward solutions over clever ones. Minimize abstractions. Write clear code first.

**Readability and Maintainability First**: Self-documenting code with minimal comments. Short methods with clear flow. Early returns over deep nesting.

**Best Practices Always**: Follow modern .NET conventions and existing patterns. Security first. Async/await properly. Log errors with context. For tests and examples, use IANA-approved example domains.

**Testability**: Write unit tests for all business logic, naming them with the "Should" pattern and no underscores. Use dependency injection for easy mocking.

**Service Layer Owns Business Logic**: Page models should be thin — delegate orchestration (validate, persist, reload) to service methods. Don't create trivial pass-through wrappers; call the appropriate service directly. Don't extract one-liner helper methods when inlining is clearer and equally testable via mocking.

**Follow Existing Patterns**: Always search for and follow existing patterns in the codebase before creating new approaches. Check existing tests, services, and pages to maintain consistency.

**No Extra Docs**: Don't create new documentation files unless explicitly asked. Code is the documentation.

## What NOT to Do

- ❌ Don't create documentation files unless explicitly asked
- ❌ Don't add new architectural patterns
- ❌ Don't add unnecessary abstractions or interfaces
- ❌ Don't use client-side JS frameworks
- ❌ Don't write verbose comments
- ❌ Don't add dependencies without strong justification
- ❌ Don't deviate from established patterns unless necessary

## Workflow

**Making Changes**: Read existing code first → Follow existing patterns → Run tests

**Simple, readable code beats clever code. Follow established patterns. When in doubt, choose the straightforward solution.**
