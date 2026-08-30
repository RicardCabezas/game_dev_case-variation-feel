---
name: pr-readme-evaluator
description: Review merged GitHub PRs, ask the candidate clarification questions, and update a project README while tracking evaluated PRs in the README itself. Use when README documentation should be kept current from merged PR history.
---

# PR README Evaluator

Use one dedicated subagent for this workflow. The subagent analyzes merged PRs first, returns questions to the user, then edits only after the user answers or explicitly approves. The user opens the eventual PR; never commit, push, or open a PR.

The parent agent must use the collaboration tool to spawn exactly one subagent for each invocation, then use a follow-up message to continue that same subagent after user answers. The parent relays questions and approval; the subagent must not assume a user answer from repository data.

## Required workflow

1. Identify repository root, current branch, default branch, and GitHub remote. Do not switch branches, reset, clean, stash, or otherwise rewrite working-tree state.
2. Record `git status --short`. Preserve all pre-existing modifications. If `README.md` has uncommitted changes, stop and ask whether to continue; never overwrite them silently.
3. Read `AGENTS.md` and current `README.md` if present. Treat an empty or missing README as needing creation. If the repository contains an assignment brief or referenced source document, read it before drafting.
4. Use `gh` to list merged PRs for the repository. Relevant commands include:

   ```bash
   gh pr list --state merged --limit 100 --json number,title,mergeCommit,mergedAt,url,baseRefName
   ```

   Paginate until no further merged PRs remain; do not assume the first 100 is complete. Restrict to this repository and record each PR's number, merge commit, URL, base branch, and merge date. If `gh` is unavailable or unauthenticated, stop and report the exact access problem; do not invent PR data.
5. Parse the README's machine-readable evaluated-PR section. Use this format when creating it:

   ```markdown
   ## Evaluated PRs

   <!-- pr-readme-evaluator:begin -->
   | PR | Merge commit | Evaluated |
   | --- | --- | --- |
   <!-- pr-readme-evaluator:end -->
   ```

   PR number is the stable identity. Do not mark a PR evaluated until its documentation update is actually written. Preserve existing entries. If the section is malformed, repair it conservatively and retain all identifiable entries.
6. Select every merged PR not listed in that section. Treat every PR as relevant; do not filter by path or PR body. PR body is not required. Match by PR number; use merge commit only as an integrity check. A PR already listed is evaluated even if its text appears incomplete; do not duplicate it automatically.
7. Retrieve each unevaluated diff with `gh pr diff <number>`. If unavailable for a merged PR, use its recorded merge commit's first parent locally or through the GitHub API. Never use an unrelated current-branch diff as a substitute.
8. Spawn one subagent with the current README, evaluated-PR list, PR metadata, and each unevaluated PR diff. Tell it to inspect only additional files required to understand those diffs. Do not send the entire repository or unrelated history as context.
9. Subagent Phase 1 is analysis-only. It must:
   - determine README sections affected by each PR;
   - distinguish implemented behavior from plans, comments, and stale analysis;
   - identify missing candidate-owned facts such as intent, time spent, device validation, measurements, and trade-offs;
   - ask one consolidated set of concise questions;
   - make no file edits.
10. Relay subagent questions to the user and wait. If no factual question is needed, still request explicit approval before editing. Do not guess answers. Approval applies only to the listed PRs and current README state.
11. Send the user's answers and approval back to the same subagent for Phase 2. It may edit only `README.md`; it must not edit skill files, source, assets, settings, generated files, or git metadata.
12. Subagent must update the README in first-person candidate voice, explain decisions and trade-offs, and append newly completed PR numbers to the evaluated-PR section. If README was empty/missing, create a complete evaluator-facing README using the assignment brief and verified repository evidence.
13. Mark a PR evaluated only after its related README content is written and the user-approved phase completes. If editing fails halfway, preserve existing entries, report partial completion, and do not mark unfinished PRs.
14. Reread the complete README and inspect `git diff -- README.md`. Verify every claim against code, merged diffs, git history, assignment material, or user-provided facts. Leave `[TODO: confirm]` only for facts the user did not provide and that cannot be verified.
15. Report changed sections, evaluated PR numbers, remaining questions/TODOs, pre-existing worktree changes, and exact files changed. Do not commit, push, create a PR, or claim external publication.

## Diff and evidence rules

- Primary evidence is the merged PR diff. Prefer `gh pr diff <number>`; for a merged PR where that fails, use its recorded merge commit and compare its first parent with the merge commit.
- Current repository state and merged code outrank planning documents. Analysis docs may explain intent but do not prove implementation.
- Never fabricate profiler numbers, test results, screenshots, time spent, device details, or motivation.
- Do not describe an optimization as shipped unless current code contains it.
- Do not rewrite unaffected README sections.
- Preserve user changes and existing README content outside affected sections.
- Keep the evaluated-PR markers machine-readable and stable. Do not reorder or remove historical entries without explicit user approval.
- Make repeated runs idempotent: an already evaluated PR must not produce duplicate sections or duplicate table rows.
- If no unevaluated PRs exist, do not edit README; report that repository is up to date.
- If a PR diff cannot be retrieved or its merge identity is ambiguous, ask the user instead of marking it evaluated.
- If a PR has no README-relevant user-facing or engineering decision, document that judgment only if useful, but still record the PR as evaluated after approval.

## Subagent handoff prompt

Give the spawned subagent this task shape:

```text
Review these merged PR diffs for README documentation.

Phase 1: analysis only. Do not edit files. Read current README and only files needed to understand supplied diffs. Identify affected sections, verified decisions, unsupported claims, and candidate-owned facts missing from context. Ask one concise consolidated question list. Wait for user answers and explicit approval.

Phase 2: after answers and explicit approval, edit only README.md. If README is missing or empty, create it. Write in first person as the candidate. Explain approach, rationale, implementation boundaries, measurements, trade-offs, time, base-project feedback, and AI usage when supported by evidence. Never invent facts. Maintain the machine-readable evaluated-PR section and mark only successfully documented PRs. Do not commit, push, switch branches, reset, commit, push, or open a PR. Preserve unrelated working-tree changes.
```

Keep questions short and answerable. Batch questions across all unevaluated PRs into one user turn. If user does not answer, stop without editing.
