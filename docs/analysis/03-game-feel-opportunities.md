# Game-Feel Opportunity Analysis

> AI-assisted static analysis used to identify potential feedback
> opportunities. Final game-feel decisions are made through manual
> playtesting and candidate judgment.

## Scope and core interactions

Static analysis identifies one active combat loop: touch/hold to move, release to stop moving and automatically strike nearest enemy in weapon range. `HeroController.UpdateLoop()` chooses movement while joystick is active, otherwise calls `AttackClosestEnemy()` every frame; weapon cooldown limits confirmed strikes. Enemies spawn every two seconds up to 20 (`EnemiesController.SpawnLoop()`), pursue hero, then directly call `HeroController.TakeHit()` at their cooldown.

`WeaponsConfig.asset` and `EnemiesConfig.asset` list multiple items, but current runtime selects index `0` in both `WeaponsService.Initialize()` and `EnemiesController.SpawnEnemy()`: GreatSword and BeeNormal. Thus movement, automatic attacks, enemy pursuit, enemy hits, player damage, death, and restart are highest-value recurring interactions. No current critical-hit or reward/progression event exists.

## GF-01 — Make automatic strikes readable

### Current behavior

`HeroController.AttackClosestEnemy()` immediately calls `EnemiesController.AttackEnemy()` once closest target is in range and cooldown permits. It updates `LastAttackTime`, but does not emit an attack event or update a view. `HeroView` only drives animator `Speed`; `HeroAnimationController.controller` includes `HeroAttack.anim`, but no attack parameter or runtime call reaches it.

### Opportunity

Player receives no visible start, cadence, or facing cue for most frequent offensive action. Because attack begins only after releasing joystick, it can be hard to connect player intent, target selection, and damage moment.

### Expected player-facing effect

Better attack responsiveness, target clarity, perceived weapon weight, and readable cooldown cadence.

### Implementation point

`HeroController.AttackClosestEnemy()`; `HeroView`; existing `HeroAttack.anim` / `HeroAnimationController.controller`.

### Smallest viable implementation

Expose one confirmed-strike notification from `AttackClosestEnemy()` containing target position. `HeroView` rotates/faces target and plays existing attack clip once per notification. Keep damage timing unchanged for first test.

### Tunable parameters

Animation start offset, transition duration, playback speed, target-facing turn speed, and whether clip begins before or exactly on current damage frame.

### Expected game-feel impact

High

### Implementation effort

S

### Performance risk

Low. Animator state changes occur only at weapon cooldown frequency.

### Relationships

Reinforces GF-02, GF-06, and GF-07. GF-07 camera motion should follow confirmed impact, not every animation start. Strong attack motion can make a separate target marker less necessary.

## GF-02 — Show enemy damage and remaining durability

### Current behavior

`EnemiesController.AttackEnemy()` subtracts health and writes updated `EnemyState` for non-lethal hits. It emits no damage/state event; `EnemiesContainerView` only handles spawn, removal, and position events. Bee `Damage` clip exists in `BeeAnimationController.controller`, but controller has no animator parameters and `EnemyView` never accesses animator. Enemy health is not rendered.

### Opportunity

Non-lethal hits leave no observable target reaction or health change. Repeated GreatSword hits can look identical to misses or delayed simulation.

### Expected player-facing effect

Clear hit confirmation, kill progress, and better distinction between one-hit and multi-hit targets.

### Implementation point

`EnemiesController.AttackEnemy()` non-lethal branch; `EnemiesContainerView`; `EnemyView`; existing Bee `Damage` clip.

### Smallest viable implementation

Emit a non-lethal damage event with enemy ID, damage, and health fraction. Use it for one brief target-local reaction: existing Damage animation or a material/scale flash. Add a small world-space health fill only while recently damaged.

### Tunable parameters

Reaction duration, flash color/intensity, health-bar linger time, height/scale, interpolation speed, and distance/UI culling threshold.

### Expected game-feel impact

High

### Implementation effort

M

### Performance risk

Low to Medium. Per-enemy canvases or material instances can add cost with the configured 20-enemy cap; pool and hide inactive indicators if used.

### Relationships

Reinforces GF-01 and GF-06. Complements GF-03 by making lethal result legible. Avoid long health-bar persistence plus large floating numbers; both communicate same information.

## GF-03 — Preserve lethal-hit confirmation before removal

### Current behavior

On lethal damage, `EnemiesController.AttackEnemy()` calls `RemoveEnemy()` in same call stack. `EnemiesContainerView.OnEnemyRemoved()` destroys `EnemyView` immediately. Bee `Die` clip exists but cannot be reached by runtime controller wiring.

### Opportunity

Enemy disappears at moment health crosses zero. This gives no death reaction, no clear distinction from despawn, and no punctuation for final hit.

### Expected player-facing effect

More satisfying kill confirmation and clearer completion of each encounter, without adding reward mechanics.

### Implementation point

Lethal branch of `EnemiesController.AttackEnemy()`; `RemoveEnemy()`; `EnemiesContainerView.OnEnemyRemoved()`; existing Bee `Die` clip.

### Smallest viable implementation

Send a lethal-hit event, play existing Die clip or one short local dissolve/scale-down, then remove view after its brief presentation. Gameplay targeting must exclude target immediately, as it already does.

### Tunable parameters

Presentation delay, clip transition, fade/scale curve, corpse visibility, and whether a single final-hit accent is used.

### Expected game-feel impact

Medium

### Implementation effort

M

### Performance risk

Low. Temporary retained views increase concurrent renderers only for death-presentation duration.

### Relationships

Reinforces GF-01, GF-02, and GF-07. A large death VFX plus heavy camera impulse may be excessive for every BeeNormal kill at 1.5-second GreatSword cadence.

## GF-04 — Telegraph enemy attacks and acknowledge player hits

### Current behavior

`EnemiesController.UpdateEnemy()` directly calls `HeroController.TakeHit()` when within `AttackRange` and cooldown elapsed. No attack event reaches `EnemyView`; Bee `Attack` clip is unreachable. `TakeHit()` changes `HeroState` and logs health; `HeroView.OnHeroStateChanged()` only sets position. Hero Damage and Die clips are present but unused.

### Opportunity

Enemy contact damage has no visible wind-up, strike, or player-hit response. Player may only infer threat after game over.

### Expected player-facing effect

Better threat readability, fairness, and cause/effect when crowding enemies.

### Implementation point

`EnemiesController.UpdateEnemy()` attack branch; `HeroController.TakeHit()`; `EnemyView`; `HeroView`; existing Bee Attack and Hero Damage clips.

### Smallest viable implementation

At current attack moment, notify view to play one enemy attack reaction and one brief hero damage response; preserve existing damage timing. A later candidate can test a visual wind-up before existing damage only if manual playtest finds attacks unreadable.

### Tunable parameters

Enemy attack animation offset/speed, wind-up length if tested, hero flash/recoil duration, stacking/cooldown for player-hit feedback, and interruption rules.

### Expected game-feel impact

High

### Implementation effort

M

### Performance risk

Low. Main risk is overlapping animations when several enemies attack same frame, not frame cost.

### Relationships

Reinforces GF-05 and GF-06. If visual wind-up is added, keep it distinct from GF-08 chase animation. Do not pair a full-screen flash, camera shake, and loud sound for every multi-enemy hit.

## GF-05 — Surface hero health and danger state

### Current behavior

Hero health begins at `HeroConfig.InitialHealth` (100 default in `HeroConfig.cs`) and changes through `HeroController.TakeHit()`. `OnStateChanged` already reaches `GameOverOverlayView` and `HeroView`, but no gameplay health UI subscribes. Only zero health is visibly acknowledged through `GameOverOverlayView`.

### Opportunity

Damage accumulation and low-health urgency are hidden until terminal state. This weakens defensive decision-making in a chase loop.

### Expected player-facing effect

Improved survivability awareness, readable danger escalation, and stronger state-transition clarity.

### Implementation point

`HeroController.OnStateChanged`; a component under existing scene `UI` canvas; `GameOverOverlayView` can remain terminal-only.

### Smallest viable implementation

Add one unobtrusive health bar that interpolates from `HeroState.Health` and has a short damage pulse. Do not add numbers, critical states, or new resource systems.

### Tunable parameters

Screen position/size, smoothing rate, damage-pulse intensity/duration, low-health threshold, and low-health signal frequency.

### Expected game-feel impact

High

### Implementation effort

S

### Performance risk

Low. One canvas element updates only on hero state changes.

### Relationships

Reinforces GF-04 and GF-09. Avoid duplicating normal-hit damage with persistent screen-edge treatment; reserve stronger warning for low health or death.

## GF-06 — Add restrained impact sound hierarchy

### Current behavior

Repository contains no audio clips and no `AudioSource` references. Attack, enemy damage/death, enemy attack, hero hit, game-over, and restart all resolve without audio.

### Opportunity

Combat events lack a non-visual confirmation channel. This is especially notable when camera framing or enemy density hides contact.

### Expected player-facing effect

Stronger hit certainty, perceived material impact, and clearer separation of player success, player damage, death, and restart.

### Implementation point

Confirmed-strike path in `HeroController.AttackClosestEnemy()`, enemy damage/lethal branches in `EnemiesController.AttackEnemy()`, `HeroController.TakeHit()`, and `GameOverOverlayView.OnRestartButtonClicked()`.

### Smallest viable implementation

Use one short attack-contact sound, one muted enemy-hit/death variant, and one clearly different hero-damage sound. Prioritize these over ambient layers or continuous effects.

### Tunable parameters

Volume groups, cooldown/concurrency cap, random pitch range, variant weights, spatial blend, max audible distance, and ducking priority for hero damage/death.

### Expected game-feel impact

High

### Implementation effort

S

### Performance risk

Low to Medium. Frequent attacks and up to 20 enemies can cause voice overlap; cap simultaneous one-shots and rate-limit duplicate sounds.

### Relationships

Reinforces GF-01 through GF-04 and GF-09. Audio should carry event hierarchy, not duplicate every visual. Pitched variants are useful for repeated normal hits, while death/game-over should remain stable and distinct.

## GF-07 — Use camera and screen-space accents selectively

### Current behavior

`WorldView` exposes a `CinemachineVirtualCamera` which follows hero via `HeroContainerView`. Its prefab contains follow framing/damping, but no impulse source/listener is configured. No screen-space combat feedback or particles exist.

### Opportunity

Existing camera can support impact punctuation, but nothing differentiates ordinary contact from lethal/player-damage events. Adding feedback indiscriminately would conflict with frequent automatic strikes.

### Expected player-facing effect

More perceived weight for selected events while retaining readable movement and target tracking.

### Implementation point

`WorldView.Camera`; confirmed lethal hit in `EnemiesController.AttackEnemy()`; `HeroController.TakeHit()`; `GameOverOverlayView` death transition.

### Smallest viable implementation

Prototype one subtle camera or screen-space accent for a single higher-priority event, such as player damage or enemy lethal hit. Do not trigger it on every normal attack initially.

### Tunable parameters

Amplitude, duration, frequency, direction, event cooldown, event priority, and reduced-motion/disable option.

### Expected game-feel impact

Medium

### Implementation effort

S

### Performance risk

Low. Main risk is visual fatigue and loss of camera readability rather than CPU/GPU cost.

### Relationships

Reinforces GF-03, GF-04, and GF-09. May be redundant with a strong visual death reaction plus loud sound. Never combine full-strength impulse with each normal-hit effect from GF-01/GF-02.

## GF-08 — Reflect enemy pursuit state in animation

### Current behavior

`EnemiesController.UpdateEnemy()` emits `OnEnemyPositionChanged` while BeeNormal moves toward hero. `EnemyView` turns toward hero every frame, but does not drive animator. Bee controller defaults to Idle and has Move, Attack, Damage, and Die states without usable runtime parameters/transitions.

### Opportunity

Enemies slide through world state while appearing idle. This weakens approach pressure and makes attack range less legible.

### Expected player-facing effect

Clearer enemy intent, motion energy, and enemy-to-attack transition.

### Implementation point

`EnemiesController.OnEnemyPositionChanged`; `EnemyView`; existing Bee Move and Idle clips/controller.

### Smallest viable implementation

Drive idle/move presentation from received position updates or controller movement state, leaving gameplay positions and speed unchanged.

### Tunable parameters

Animation transition duration, move playback multiplier, start/stop threshold, and delay before returning idle near attack range.

### Expected game-feel impact

Medium

### Implementation effort

S

### Performance risk

Low. Animator changes are bounded by spawned enemies; avoid forcing state changes every frame.

### Relationships

Reinforces GF-04. Does not replace attack telegraph: pursuit motion and attack intent should be separate. Too much blending can obscure specific Attack/Damage reactions.

## GF-09 — Punctuate death and restart transition

### Current behavior

`GameOverOverlayView` activates a dark background, “GAME OVER” text, and stock color-tint Restart button when `HeroState.IsDead`. Restart calls `EnemiesController.ClearAllEnemies()` then `HeroController.Restart()` immediately. No hero death animation, transition, audio, or restart acknowledgement is wired.

### Opportunity

Terminal state is visible, but hero death cause and restart completion have little transition feedback. Immediate enemy removal can read as abrupt reset rather than deliberate recovery.

### Expected player-facing effect

Clearer failure punctuation, more satisfying recovery, and more responsive restart confirmation.

### Implementation point

`HeroController.TakeHit()` lethal path; `GameOverOverlayView.OnHeroStateChanged()` and `OnRestartButtonClicked()`; existing Hero Die clip and current overlay/Button.

### Smallest viable implementation

Play one death presentation before/with overlay, then give Restart one short pressed/transition acknowledgement while preserving current reset behavior. Keep Game Over overlay as existing state authority.

### Tunable parameters

Overlay fade duration, death-to-overlay delay, text/button scale/fade, restart acknowledgement duration, and input lock timing.

### Expected game-feel impact

Medium

### Implementation effort

S

### Performance risk

Low. UI animation and one character animation only.

### Relationships

Reinforces GF-04, GF-05, GF-06, and GF-07. Avoid making restart slower than necessary; long death presentation can frustrate repeat attempts.

# Current Feedback-Chain Map

## Movement with virtual joystick

Input — **GOOD.** `JoystickInputService.HandleInput()` samples first touch/mouse each Update; press creates state, release deactivates it.

Anticipation — **NOT APPLICABLE.** Movement begins from continuous input.

Action — **GOOD.** `HeroController.UpdatePosition()` updates state every frame while input is active; `HeroView` moves transform, turns with `Quaternion.Slerp`, and sets `Speed`.

Contact — **NOT APPLICABLE.** No movement collision or traversal interaction is implemented.

Feedback — **GOOD.** `JoystickView` displays outer/inner sticks and `HeroAnimationController.controller` transitions Idle/Run from `Speed`. Camera follows hero through `HeroContainerView` and `WorldView.Camera`.

State transition — **WEAK.** Releasing input silently changes from movement to automatic attack eligibility in `HeroController.UpdateLoop()`.

## Automatic closest-enemy attack

Input — **WEAK.** Attack is implicit when joystick becomes inactive, not a discrete input.

Anticipation — **MISSING.** No target selection, facing, wind-up, or attack state is sent to views.

Action — **MISSING.** `AttackClosestEnemy()` calls damage directly; Hero Attack animation is not driven.

Contact — **GOOD in game state / WEAK in presentation.** Range check and cooldown are explicit in `TryFindClosestEnemy()` and weapon config, but only `Debug.Log` marks result.

Damage — **WEAK.** Enemy health mutates in `AttackEnemy()` with no visual/UI notification.

Reaction — **MISSING.** Existing Bee Damage state has no runtime hook.

Death — **WEAK.** Enemy is immediately removed/destroyed; existing Die state has no runtime hook.

Reward — **NOT APPLICABLE.** No reward, loot, score, or progression event exists.

## Enemy pursuit and attack

Spawn — **WEAK.** `SpawnLoop()` creates BeeNormal on cadence, but no presentation event beyond prefab appearing.

Anticipation — **WEAK.** `EnemyView` faces hero and model position updates, but Bee Move/Attack animations are not driven.

Action — **MISSING.** In-range attack calls `TakeHit()` directly without visible strike.

Contact / damage — **GOOD in game state / MISSING in presentation.** Cooldown and damage values exist in `EnemyConfig`; no player hit response is produced.

Reaction — **MISSING.** Hero Damage clip is unused.

Death / reward — **NOT APPLICABLE.** Enemy attacks do not directly produce a separate state other than hero health reduction.

## Hero damage, death, and restart

Input — **NOT APPLICABLE.** Enemy attack drives damage.

Damage — **GOOD in game state / MISSING in presentation.** `HeroController.TakeHit()` clamps health and emits `OnStateChanged`; no health display or visual response consumes it.

Death — **GOOD for UI / WEAK for character.** `GameOverOverlayView` shows dark overlay, text, and restart; Hero Die clip is unused.

Reward / recovery — **WEAK.** Restart button has standard Unity color transition; `ClearAllEnemies()` and `Restart()` occur immediately without acknowledgement.

# High-Impact Feedback Clusters

## Attack Legibility Package

- GF-01
- GF-02
- GF-06

## Enemy Threat Package

- GF-04
- GF-05
- GF-08

## Kill Punctuation Package

- GF-03
- GF-06
- GF-07

## Failure / Recovery Package

- GF-04
- GF-05
- GF-09

# Potential Over-Juicing Risks

- Automatic GreatSword strikes occur every 1.5 seconds against current BeeNormal. Full attack clip, hit flash, floating health UI, particles, camera impulse, and multiple sounds on each hit would turn basic cadence into visual/audio churn.
- Up to 20 enemies can pursue and attack. Simultaneous hero-hit reactions, Bee attack clips, particles, and one-shots need cooldowns and concurrency caps; otherwise source attribution becomes unclear.
- Persistent health bars plus floating damage numbers both solve health progress. Showing both on every hit can cover small Bee silhouettes and reduce kill readability.
- Camera motion is strongest when reserved for rare/high-priority events. Shake on normal strikes plus enemy hits will make continuous chase movement feel unstable.
- Long death presentations or overlay delays may improve punctuation but can make repeated restart loops feel slow. Preserve fast restart responsiveness.
- Randomized pitch helps repeated normal hits, but excessive spread weakens consistency of player damage, death, and restart feedback.

# Questions for My Manual Playtest

1. When releasing joystick near a BeeNormal, can I immediately tell hero started an automatic GreatSword attack, which Bee was targeted, and when next strike becomes ready?
2. After first non-lethal GreatSword hit, can I tell it connected and estimate how many more hits BeeNormal needs without reading console output?
3. When a BeeNormal reaches its 2-unit attack range, do I notice its attack before health loss or only discover threat at Game Over?
4. While several enemies chase, can I track remaining hero safety well enough to make movement decisions before zero health?
5. Does current Game Over and Restart sequence feel adequately clear and fast, or does immediate enemy disappearance/restart lack useful confirmation?
