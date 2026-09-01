# Game Feel Home Assignment

## Initial Considerations

I treated this as an inherited project: understand the game first, then make small changes with clear value. The main goals were better combat feedback, stable performance, and code that still follows the existing architecture.

The project uses Unity `2022.3.62f2`. The assignment allowed up to eight hours. Generated API documentation and the current codebase map are available on [GitHub Pages](https://ricardcabezas.github.io/game_dev_case-variation-feel/).

## Implemented Work

| Area | What I implemented | Time spent |
| --- | --- | ---: |
| Analysis | Reviewed architecture, game feel, and performance; created a prioritized plan | `~1.5 h` |
| Attack flow | Added clear attack timing, a manual attack window, button state, target validation, and attack animation | `~1 h` for initial attack feedback; later work `[TODO]` |
| Enemy feedback | Added movement, attack, damage animation, and red hit flash | `~1 h` for hit feedback; remaining work `[TODO]` |
| Health feedback | Added hero and enemy health bars, smoothing, visibility rules, and hero hit feedback | `[TODO: add by day]` |
| Enemy readability | Added bee spacing and a small opaque disk under each bee to avoid stacking and improve grounding | `[TODO: add by day]` |
| Performance | Added a 60 FPS target, removed recurring combat logs, reused update buffers, and profiled the bee ground marker | `[TODO: add by day]` |
| Controller boundaries | Moved entity orchestration into `EntitiesService` and exposed read-only presentation contracts | `~1 h` |
| Shadow mesh optimization | Replaced the transparent textured quad with an opaque 16-triangle disk after profiling | `~30 min` |
| Code quality and docs | Added architecture rules, XML documentation, Doxygen, GitHub Pages automation, and a documentation-only README pass | `[TODO: add earlier work]`; README pass `~30 min` |

Time for later work is not fully recorded yet. I will add it by day before claiming a verified total against the eight-hour limit.

## AI Usage

I used Codex because it is the coding agent I know best. There was no feature-specific reason for this choice; the same workflow could have used another agent such as Claude. I did not add separate AI planning tools because I wanted to keep the test simple and use the available time on implementation and review.

### Workflow

- I used Git worktrees so several agents could work in parallel from one repository clone without sharing the same checkout.
- For a longer project, I would likely use two or three separate clones or workspaces to reduce contention further.
- I used specialized agents for focused tasks. During the first project review, separate agents analyzed performance, architecture, and game feedback. A later mediator agent compared their reports and produced one prioritized proposal.
- I created reusable skills for repetitive work, including PR-based README evaluation and documentation maintenance.

### Delegation and Responsibility

I delegated most implementation work to AI agents. My role was to:

- Write prompts and define scope.
- Decide which ideas to implement.
- Review code and agent reports.
- Request corrections or follow-up changes.
- Play the game, test builds, and provide profiler data.
- Create project guidelines so agents followed the architecture and workflow I wanted.

I treated agent output as a draft, not as final work. 

## Trade-offs and Future Work

- I reused existing animations and assets instead of building a new content system.
- Damage stays immediate. Animations explain accepted gameplay actions; they do not control damage timing.
- Lethal enemies are removed immediately, so there is no delayed death animation.
- Bee spacing uses a simple two-pass pair solver. It works for the 20-enemy limit but does not scale to large swarms.
- I chose a low-poly opaque disk over the transparent blob after profiling. The intended final setup disables realtime bee shadows, but `BeeNormal.prefab` currently still enables shadow casting on the bee renderer. `[TODO: confirm prefab shadow flag]`
- Pooling was not the right optimization at the current 20-enemy scale: profiling pointed to GPU-bound rendering, not spawn or destruction lifecycle cost.
- The boundary refactor deliberately centralized orchestration. Manual playthroughs were practical for this small project, but I would add unit tests for attack ordering, restart, and teardown as the project scales.
- PR #20 also changed the configured bee speed from `10` to `1`. `[TODO: confirm intended gameplay tuning]`
- Hero health is currently `10,000` from testing and needs final tuning.
- With more time, I would add profiler screenshots, instanced disk rendering, and final balance values.

## Performance

Profiling used a Pixel 7 Android Development Build. I used Profile Analyzer for median frame data and the Frame Debugger to inspect triangle counts, rendering, and compute/render batches. Initial captures used 90 FPS, the device limit, so frame waiting would not hide CPU/GPU work. The shipped target is 60 FPS.

At the configured scale of 20 moving bees, the median frame time was `11.08 ms`. This was within the 90 FPS profiling budget and comfortably within the shipped 60 FPS target, so normal gameplay performance appeared healthy.

Scaling to 200 bees exposed a GPU-bound rendering problem. The useful target was per-bee rendering cost, not object creation, so pooling would not address the measured bottleneck.

| 200-bee configuration | Median frame time |
| --- | ---: |
| Realtime bee shadows disabled, no ground marker | `17.11 ms` |
| Realtime bee shadows enabled | `19.55 ms` |
| Transparent blob enabled | `19.87 ms` |
| Realtime bee shadows disabled, opaque disk enabled | `18.43 ms` |

The transparent blob began with the premise that replacing realtime shadows would save roughly half the shadow-casting triangle cost. Profiling showed that its transparency cost hurt performance more than expected. I therefore replaced it with a small opaque 16-triangle disk and chose to disable realtime bee shadows. The disk costs `1.32 ms` over having no ground marker, but remains cheaper than either realtime shadows or the transparent blob while preserving visual grounding. The current bee prefab still has shadow casting enabled, so that flag needs confirmation before I describe this intended setup as shipped.

Recurring combat logs also consumed a large portion of frame time during stress profiling. I removed those recurring logs, reused enemy-update buffers, and avoided one chase-distance square root.

`[TODO: add profiler and Frame Debugger screenshots]`

## Game Feedback Decisions

I focused on making actions easy to read:

- Attack timing, full-window state, and button text show when an attack is possible or requires movement.
- Hero and bee animations show movement, attacks, and damage.
- Red flashes and health bars make damage clear.
- Bee spacing prevents enemies from looking like one stacked model.
- Opaque low-poly disks keep bees visually connected to the ground without transparent blending.

I kept immediate damage and the existing combat authority. Presentation reacts to gameplay events; it does not decide gameplay results.

## Code Quality

I kept the existing gameplay/presentation separation, but I did perform a substantial boundary refactor. `EntitiesService` now owns update order, spawning, attack routing, restart, and teardown. Hero and enemy controllers are internal, expose read-only presentation interfaces, and no longer orchestrate one another. UI services adapt those events into presentation-only controller contracts.

The refactor also made attack processing deterministic by enemy ID and made restart and teardown cleanup explicit. Gameplay was intended to remain unchanged. Because the project is small, I validated regressions through manual playthroughs; as it scales, I would add unit tests around attack ordering, restart, and cleanup rather than rely on manual coverage.

I also added architecture guidance, a maintained codebase map, XML API comments, and generated documentation. PR #19 was documentation-only: it consolidated these decisions, measurements, AI workflow, and base-project feedback in this README.

## Base Project Feedback

### Good

- Clear separation between gameplay logic and Unity presentation.
- Data-driven hero, enemy, weapon, and world configuration.
- Useful animations and assets already existed, even when not wired into gameplay.
- Service initialization and typed events gave new features clear integration points.

### Could Be Better

- Combat had little feedback, so hits and attack timing were hard to read.
- Bees could overlap and look like one broken model.
- Runtime content selection still uses the first weapon and enemy entry.
- Some recurring logs were expensive in large stress tests.

### Improvements for Scaling

- Replace pairwise bee separation with spatial partitioning if enemy counts grow.
- Add pooling only after profiling shows spawn or destruction cost at normal scale.
- Replace index-zero content selection with an explicit selection or factory flow.
- Add automated gameplay tests for attack timing, damage, death, and restart flows.

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
| #11 | `dcc47e15110aef82d8a13c92aa32f772967c1d76` | Yes |
| #12 | `0e0805dd618da9d7ab22abf3f76f7e15ad8259e6` | Yes |
| #13 | `26319f4ba95df1d1719d2f785052fb4bde73afc9` | Yes |
| #14 | `05e96a34f71dbfb3d521ce7f9b6b55cbdf211f2c` | Yes |
| #15 | `096ae31aac069a0fcb80fd42b0909e7d85f84274` | Yes |
| #16 | `8e00662e0386d504a6fbff4ec9421709e0d9e041` | Yes |
| #17 | `7ae6b677d24eaae82609b909ed6f6cdece027fa7` | Yes |
| #18 | `e9bc902a2f6174c3855fb639a24b05c088cd1f54` | Yes |
| #19 | `e89498533d66b6038171b6554d3290ee5aa8d5c9` | Yes |
| #20 | `182224ad1548e88fb0769cac8241293b9928ce8a` | Yes |
<!-- pr-readme-evaluator:end -->
