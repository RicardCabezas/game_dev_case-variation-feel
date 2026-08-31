# Repository Agent Guidelines

These instructions apply to the entire repository.

## Change Scope

- Keep changes minimal and limited to what the task requires unless explicitly asked to make broader changes.
- Avoid unrelated refactors, cleanup, formatting, dependency updates, or asset changes.

## Commits

- Use Conventional Commits: `<type>(<optional-scope>): <description>`.
- Prefer standard types such as `feat`, `fix`, `docs`, `refactor`, `test`, `build`, `ci`, and `chore`.
- Keep each commit focused on one logical change.
- Mark breaking changes with `!` or a `BREAKING CHANGE:` footer.

## Branch Names

- Follow Git Flow branch naming.
- Use lowercase kebab-case after the prefix.
- Use these forms:
  - `feature/<short-description>` for new work branched from `develop`.
  - `bugfix/<short-description>` for non-urgent fixes branched from `develop`.
  - `release/<version>` for release preparation branched from `develop`.
  - `hotfix/<short-description>` for urgent production fixes branched from `main`.
  - `support/<short-description>` for maintenance work that does not fit another branch type.
- Keep `main` production-ready and use `develop` as the integration branch.

## Architecture

- Follow [`docs/codebase-map.md`](docs/codebase-map.md) for current ownership, lifecycle, and presentation boundaries.
- Preserve existing architectural boundaries unless the task explicitly requests an architecture change.
- Keep authoritative gameplay state and decisions in plain C# services/controllers.
- Apply the same separation to UI: UI controllers remain plain C#, expose state through typed events, and do not reference views or Unity presentation components.
- Keep Unity-facing presentation in `MonoBehaviour` views.
- Connect state to presentation through typed events; no controller, including a UI controller, may depend on views, UI components, animation, audio, camera, particles, or other presentation objects.
- Respect existing service initialization, dependency, reset, cancellation, event subscription, and cleanup patterns.
- Place code in the owning feature assembly and preserve established namespaces and naming suffixes.
- Before changing scenes, prefabs, configs, service registration, or state ownership, verify the relevant guidance and existing implementation first.
