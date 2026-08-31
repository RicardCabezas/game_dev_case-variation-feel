---
name: maintain-project-docs
description: Maintain XML API comments and verified project documentation when Unity contracts, lifecycle, gameplay flow, configuration, presentation wiring, or documentation claims change.
---

# Maintain Project Docs

Use when a change affects public C# contracts, service lifecycle or dependencies, controller state/events, gameplay flow, configuration semantics, scene/prefab presentation wiring, or an existing documentation claim.

1. Read `AGENTS.md`, `docs/codebase-map.md`, changed source, and current diff.
2. Treat current source as authoritative. Update affected public XML comments: responsibility, ownership, lifecycle, event timing/payload, units, ranges, invariants, and failure behavior where relevant.
3. Update affected verified static facts in `docs/codebase-map.md`. Do not edit `docs/analysis/initial_assessment/`; it is an initial-assessment archive. Keep architectural intent, performance measurements, and suggested future work separate from current-source claims.
4. Check public-surface coverage in touched source. Use `<inheritdoc/>` only where `IService` contract fully explains lifecycle method.
5. Run `git diff --check`. If documentation workflow results are available, inspect its job; do not claim a remote build passed without evidence.
6. Report changed contracts/pages and unverified build or runtime state. Do not change gameplay to make documentation easier.

For controller event changes, trace producer, event timing, payload consumers, state replacement/removal order, and view subscriptions before changing conceptual claims.
