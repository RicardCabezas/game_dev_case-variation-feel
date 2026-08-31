# Game Feel Home Assignment

## Overview

I treated this project as an inherited live codebase: understand its existing loop and architecture first, then improve combat readability with small, reviewable changes. The core mechanic remains unchanged: the player moves with the joystick and attacks automatically after movement stops. My work in the evaluated PRs focuses on making that automatic combat easier to read through attack, cooldown, movement, hit, and enemy-attack feedback.

The project uses Unity `2022.3.62f2`. The assignment allowed a maximum of eight hours and emphasized game feel, frame-budget awareness, measured optimization, and code quality.

## Documentation

Open generated documentation at [GitHub Pages](https://ricardcabezas.github.io/game_dev_case-variation-feel/).

Documentation updates after changes reach `main`.

## Approach and Rationale

I first analyzed the project structure, feedback gaps, and likely performance-sensitive paths. I also played the game and reviewed Android Development Builds while collecting profiler data. This produced a codebase map, performance investigation, game-feel opportunity analysis, architecture guideline, and prioritized implementation proposal.

The project already contained useful but unwired content, including hero and bee animations. Under the time constraint, I chose to reuse nearby existing assets instead of creating a broader content system. I also kept auto-attack and immediate damage intact. The goal was clearer cause and effect, not a new combat mechanic.

The existing architecture was sufficient for this small test. I avoided a broad refactor and followed its main boundary:

- Plain C# controllers and services own gameplay or UI state and decisions.
- Typed events carry authoritative state changes to presentation.
- `MonoBehaviour` views own Animator, material, prefab, and UI work.
- Existing service initialization and cleanup patterns remain in use.

This let me contribute quickly while keeping the changes reasonably scalable and consistent with the base project.

## Implemented Feedback

### Readable auto-attack

I added explicit feedback around the existing automatic strike:

- `HeroController` records the next allowed attack time and emits typed attack and cooldown events.
- When movement stops, a radial UI fill communicates the cooldown before the next automatic attack.
- On a successful attack, the hero faces the selected target and plays the existing attack animation.
- Damage still resolves immediately; the animation communicates an action that has already been accepted by gameplay logic.

I kept UI state in a plain `AutoAttackIndicatorController`, exposed it through `AutoAttackIndicatorState`, and left fill animation and helper-text visibility in `AutoAttackIndicatorView`.

### Bee movement and attack feedback

I added a small Animator contract for the existing bee controller: `IsMoving`, `Damage`, and `Attack`. Position updates now route through `EnemyView.SetPosition`, which drives movement animation. When an enemy performs its authoritative attack, `EnemiesController` emits the enemy ID; the container resolves the matching view and plays the attack animation.

The enemy attack event is emitted after hero damage is applied. It is therefore hit acknowledgement, not a pre-hit telegraph.

### Bee hit reaction

I added `EnemyHitResult` so the damage boundary can report enemy ID, damage, remaining health, and whether the hit is lethal. `EnemiesController` now resolves the current dictionary state before applying damage and preserves the enemy attack timestamp when it stores nonlethal health.

For a nonlethal hit, the bee plays its existing damage animation and receives a bounded `0.1 s` red flash. The flash uses `MaterialPropertyBlock`, avoiding per-hit material instantiation. The view owns this presentation work; the gameplay controller does not reference Animator or renderer components.

Lethal hits intentionally retain the base lifecycle: the enemy leaves gameplay and its view is removed immediately. This evaluated implementation does not add a death-animation delay.

## Performance Investigation

I profiled on a Google Pixel 7 running an Android Development Build. I used an explicit 90 FPS target for the reported frame-time experiments and added Unity Memory Profiler `1.1.12` and Profile Analyzer `1.2.4` to support investigation.

| Scenario | Result | Interpretation |
| --- | ---: | --- |
| 20 moving bees, 299 frames | `11.08 ms` median | Current configured cap remained near the explicit 90 FPS budget. |
| 200 moving bees | `19.87 ms` median | Larger unsupported swarm missed the 90 FPS budget. |
| 2,000 moving bees | `94.57 ms` median | Extreme stress case exposed substantial rendering and simulation cost. |
| 2,000 stationary bees, hero-hit log enabled | `71.77 ms` median | Stress-only baseline for recurring logging experiment. |
| 2,000 stationary bees, hero-hit log removed | `36.09 ms` median | Removing one recurring log reduced this matched stress median by about `49.7%`. |
| One spawn frame | `9.7 KB` GC allocation | Single captured lifecycle frame; not evidence of a normal-scale hitch. |
| One death frame | `19.3 KB` GC allocation | No retained growth was observed after repeated normal enemy churn. |

These results separate assignment-scale behavior from stress behavior. The configured 20-enemy case did not justify a swarm rewrite. The logging experiment demonstrated a large stress-case cost, but log removal was not shipped in the evaluated PRs and I do not present it as a delivered optimization.

I found no major optimization that was both measured and worthwhile at the assignment's scale. The other notable concern, rendering too many animated bees, occurred beyond the supported enemy cap. I chose not to spend the remaining scope on speculative iteration changes, pooling, or large-swarm rendering work.

### Profiler screenshots

[TODO: embed confirmed profiler screenshots]

No screenshots are included yet. The numeric results above come from the confirmed Pixel 7 captures summarized during the analysis phase.

## Measurements and Validation Boundaries

The performance table records the confirmed Editor/Android investigation supplied during development. I personally played the game, reviewed the project and reports, and provided build/profiler data and game-experience feedback.

I have not documented additional runtime test coverage for each feedback PR because no broader verified test record was supplied for this README pass. The implementation evidence confirms the code and asset wiring, but it does not by itself prove device behavior or visual quality.

## Trade-offs and Further Work

- I reused existing animations and nearby assets to maximize feedback delivered within the time limit.
- I preserved auto-attack and immediate damage instead of changing combat timing or authority.
- I kept lethal removal immediate, so lethal hits do not receive the nonlethal damage flash or a delayed death presentation.
- I kept the base controller/view structure rather than pursuing an idealized architecture rewrite.
- I did not optimize for unsupported 200- or 2,000-enemy stress cases.
- I did not ship the stress-tested log removal. If performance work continues, I would first validate logging at the normal 20-enemy cap and only change it with a comparable before/after capture.
- I would only consider pooling or enemy-iteration/rendering changes after profiling shows a normal-scale lifecycle hitch or frame-budget problem.

## Time Spent

| Work item | Approximate active time |
| --- | ---: |
| Initial profiling, code-structure analysis, feedback analysis, and prioritization | `~1.5 h` |
| Readable auto-attack and cooldown feedback (PR #2) | `~1 h` |
| Bee hit feedback (PR #7) | `~1 h` |
| Bee Animator contract, movement wiring, and attack feedback (PRs #4, #5, #8) | `[TODO: confirm exact breakdown]` |
| Architecture guardrails (PR #3) | `[TODO: confirm]` |
| README evaluator skill (PR #10) | Ran in background while I reviewed other agents' work; exact active time was not recorded. |

## AI Usage

I used three agents in parallel for the initial profiling review, existing-code-structure analysis, and missing-feedback analysis while I manually played and reviewed the project. I supplied performance observations from the Editor and Android Development Builds plus my own game-experience feedback. Another agent reviewed the reports and generated a prioritization proposal, which I reviewed and edited before implementation decisions were accepted.

I remained responsible for playing the game, reviewing the code and reports, supplying profiler/build evidence, choosing scope, and editing the prioritization. The README evaluator also ran in the background while I reviewed other agent work.

During development, an agent-generated change broke movement after missing an existing architectural constraint. I caught that issue during review. It motivated adding `AGENTS.md` and strengthening the architecture guideline so later work would preserve controller/view ownership, feature boundaries, and lifecycle patterns. This was a useful reminder that AI output still requires candidate review and runtime validation.

## Base Project Feedback

[TODO: confirm candidate feedback on the base project]

## Evaluated PRs

<!-- pr-readme-evaluator:begin -->
| PR | Merge commit | Evaluated |
| --- | --- | --- |
| #1 | `84eea760789859575bf9f8146d5feac0956a8fe0` | Yes |
| #2 | `9784d53c279a3813128cc565e5ab5b349094f340` | Yes |
| #3 | `4a98f95637b3e4527447c555b5d28e0be5f61f71` | Yes |
| #4 | `d61808d55b152dc27f06eab6aff623dbf26735b4` | Yes |
| #5 | `dce1ffe1b0566e3d47108b632d8e4b35f70f39ee` | Yes |
| #7 | `823906575a7d9be18f9835558dba3c0c7cf27787` | Yes |
| #8 | `56fa812ac0689fb8d383cbb2ef3b3dd408de6c0a` | Yes |
| #10 | `4026aaa6d89b06ea9d6ec6e475d0045f11160625` | Yes |
<!-- pr-readme-evaluator:end -->
