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
| Enemy readability | Added bee spacing and blob shadows to avoid stacking and improve grounding | `[TODO: add by day]` |
| Performance | Added a 60 FPS target, removed recurring combat logs, reused update buffers, and reduced realtime shadows | `[TODO: add by day]` |
| Code quality and docs | Added architecture rules, XML documentation, Doxygen, and GitHub Pages automation | `[TODO: add by day]` |

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
- Blob shadows are cheaper but less accurate than realtime mesh shadows.
- Hero health is currently `10,000` from testing and needs final tuning.
- With more time, I would add device validation, profiler screenshots, blob-shadow before/after captures, and final balance values.

## Performance

Profiling used a Pixel 7 Android Development Build. Initial captures used 90 FPS, the device limit, so frame waiting would not hide CPU/GPU work. The shipped target is 60 FPS.

| Scenario | Median frame time |
| --- | ---: |
| 20 moving bees | `11.08 ms` |
| 200 moving bees | `19.87 ms` |
| 2,000 moving bees | `94.57 ms` |
| 2,000 stationary bees, hit log enabled / removed | `71.77 ms` / `36.09 ms` |

The normal 20-enemy case did not justify pooling or a swarm rewrite. Later changes removed recurring combat logs, reused enemy-update buffers, avoided one chase-distance square root, and replaced realtime bee shadows with blob shadows. The observed triangle count was roughly halved, but exact capture context and before/after frame measurements are still `[TODO]`.

## Game Feedback Decisions

I focused on making actions easy to read:

- Attack timing, full-window state, and button text show when an attack is possible or requires movement.
- Hero and bee animations show movement, attacks, and damage.
- Red flashes and health bars make damage clear.
- Bee spacing prevents enemies from looking like one stacked model.
- Blob shadows keep bees visually connected to the ground.

I kept immediate damage and the existing combat authority. Presentation reacts to gameplay events; it does not decide gameplay results.

## Code Quality

I avoided a broad rewrite because the existing structure was good enough for this scope. Plain C# controllers and services already owned game state, while `MonoBehaviour` views owned animation, UI, materials, and prefabs. Typed events connected both sides.

Changing that architecture would have added risk without improving the player experience. I kept most existing code and added focused controllers, states, events, and views where needed. I also added architecture guidance, a maintained codebase map, XML API comments, and generated documentation.

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
- `EntitiesService.Reset()` does not forward reset calls to its controllers.
- Some recurring logs were expensive in large stress tests.

### Improvements for Scaling

- Replace pairwise bee separation with spatial partitioning if enemy counts grow.
- Add pooling only after profiling shows spawn or destruction cost at normal scale.
- Replace index-zero content selection with an explicit selection or factory flow.
- Make controller reset ownership complete and test service teardown.
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
<!-- pr-readme-evaluator:end -->
