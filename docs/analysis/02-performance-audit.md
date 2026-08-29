# Performance Investigation

> AI-assisted performance investigation combining static code analysis
> with runtime profiling evidence where available. Static findings are
> not treated as measured performance problems unless validated using
> runtime data.

## Scope and evidence limits

Reviewed runtime gameplay code, scene/prefab YAML, URP settings, project settings, and Addressables configuration. `docs/profiling/` is absent; no Profiler capture, Profile Analyzer export, GPU capture, Memory Profiler summary, CSV, build report, or `Editor.log` was available. Binary profiler files were not present.

Therefore this audit has **no CONFIRMED findings**. Runtime measurements must decide whether any item below merits work. Default gameplay configuration caps enemies at 20 and spawns one every two seconds.

## PERF-01 — Per-frame enemy-key collection allocation

**Status:** LIKELY

**Severity:** Medium

**Confidence:** High

### Location

`Assets/Features/Entities/Scripts/Controllers/EnemiesController.cs`, `UpdateLoop` (lines 123–143), specifically `new List<int>(_enemies.Keys)` on line 133.

### Evidence

**Static evidence**

Every non-dead gameplay frame creates a `List<int>` from dictionary keys before updating enemies. This allocates list storage and copies current keys even when enemy count is zero. It avoids collection modification during enumeration, but code does not show a need to remove enemies inside this loop.

**Runtime evidence**

Not yet measured. No profiling artifacts supplied.

### Expected execution frequency

Once per `PlayerLoopTiming.Update` while hero lives: typically once per rendered gameplay frame. At default cap, copied key count reaches 20.

### Potential runtime impact

Recurring managed allocation and later GC work; possibly CPU for list creation/copy. No measured frame or GC impact claimed.

### Profiler validation

Gameplay scenario: profile development build for 60 seconds after reaching 20 live enemies; repeat with zero enemies and unchanged camera/input.

Profiler module/tool: CPU Usage in Timeline or Hierarchy with **GC Alloc** column; Memory module for managed-heap/GC collection correlation. Keep Deep Profile off for first capture.

Marker/call to inspect: `Game.GamePlay.Enemies.EnemiesController.UpdateLoop`; expand managed call tree around `new List<int>(_enemies.Keys)` if visible.

Metric to record: GC Alloc per frame, allocation call count, GC collection occurrence, and self/total CPU time for loop.

CONFIRM if this call produces repeatable per-frame managed allocation that grows with active enemy count and correlates with unwanted GC or frame-time cost. REJECT as actionable if allocation is absent/insignificant in target build and no GC or frame-time signal follows.

### Potential fix

Reuse a private ID buffer, or enumerate safely without snapshotting if mutation is structurally impossible during update. Preserve safe removal behavior; do not change before capture confirms cost.

### Estimated active implementation effort

S

### Regression risk

Medium — event handlers or future update logic could mutate enemy collection during enumeration.

## PERF-02 — Enemy simulation, view rotation, animation scale together per frame

**Status:** HYPOTHESIS

**Severity:** Low

**Confidence:** Medium

### Location

`Assets/Features/Entities/Scripts/Controllers/EnemiesController.cs`, `UpdateEnemy` (lines 146–170); `Assets/Features/Entities/Scripts/View/Enemies/EnemyView.cs`, `Update` (lines 24–36); enemy prefab `Assets/Features/Entities/View/Enemies/Local/BeeNormal/BeeNormal.prefab` (`Animator` and `SkinnedMeshRenderer`).

### Evidence

**Static evidence**

For each live enemy, controller code calculates distance and movement, creates updated value-state, updates dictionary, and invokes a position event. Each matching view sets transform position. Independently, each `EnemyView.Update` normalizes a direction and runs `Quaternion.LookRotation` plus `Quaternion.Slerp` each frame. Default cap is 20. Bee prefab also has enabled Animator, skinned renderer, shadow casting, shadow receiving, and reflection-probe usage.

**Runtime evidence**

Not yet measured. No CPU, animation, Render Thread, or GPU capture supplied.

### Expected execution frequency

Controller work runs once per live enemy per Update; view rotation also runs once per instantiated enemy per MonoBehaviour Update. At configured cap this is up to 20 simulation passes plus 20 view-rotation passes per frame, while hero is alive.

### Potential runtime impact

Main-thread CPU, animation CPU, render-thread skinning/shadow submission, and GPU. Static code cannot establish bottleneck or material cost.

### Profiler validation

Gameplay scenario: capture two comparable 30-second development-build runs: zero enemies, then 20 enemies chasing hero in camera view. Keep hero still after enemies converge; repeat with camera aimed away from enemies.

Profiler module/tool: CPU Usage Timeline plus Rendering and GPU Usage modules where platform supports GPU profiling. Use Profile Analyzer to compare captures.

Marker/call to inspect: `EnemiesController.UpdateLoop`, `EnemyView.Update`, `BehaviourUpdate`, animation/Animator markers, `RenderLoop.Draw`, shadow-rendering markers, and Render Thread time.

Metric to record: frame time, main-thread and Render Thread time, self/total CPU for listed calls, draw-call count, batches, triangles, and GPU frame time.

CONFIRM if full-cap capture shows material, repeatable scaling in those markers or frame time versus zero-enemy baseline. REJECT as actionable if full-cap cost remains small and flat on target hardware.

### Potential fix

Only after evidence: consolidate redundant facing/movement work, lower animation update rate for off-screen/distant enemies, or tune shadows/skin visibility. Choose based on dominant measured marker; do not apply all changes together.

### Estimated active implementation effort

M

### Regression risk

Medium — enemy facing, animation timing, and visibility behavior affect game feel.

## PERF-03 — Spawn/death lifecycle may create intermittent frame spikes

**Status:** HYPOTHESIS

**Severity:** Low

**Confidence:** Medium

### Location

`Assets/Features/Entities/Scripts/Controllers/EnemiesController.cs`, `SpawnLoop`/`SpawnEnemy` (lines 86–112) and `RemoveEnemy` (lines 58–64); `Assets/Features/Entities/Scripts/View/Enemies/EnemiesContainerView.cs`, `OnEnemySpawned`/`OnEnemyRemoved` (lines 28–41).

### Evidence

**Static evidence**

Enemy spawn event instantiates a full enemy prefab; removal destroys it. Spawn interval is two seconds and no reuse/pool exists. Each instantiated BeeNormal includes an Animator, SkinnedMeshRenderer, transform hierarchy, and `EnemyView`. Current configuration limits concurrent enemies to 20.

**Runtime evidence**

Not yet measured. No spawn/destroy timing, GC, or hitch capture supplied.

### Expected execution frequency

One spawn attempt every two seconds while below cap. Destroy frequency depends on combat and restart behavior; sustained fast kills can keep lifecycle events recurring, but code/config does not establish enough churn to justify pooling now.

### Potential runtime impact

Short main-thread CPU and managed/native allocation spikes at spawn/destroy; possible animation/renderer setup cost. No sustained-cost claim.

### Profiler validation

Gameplay scenario: development build, repeatedly kill enemies for two minutes so each spawn is removed before next cap; separately press restart after reaching 20 enemies.

Profiler module/tool: CPU Usage Timeline with GC Alloc, Memory module, and Profile Analyzer frame-spike comparison.

Marker/call to inspect: `Object.Instantiate`, `Object.Destroy`, `EnemiesContainerView.OnEnemySpawned`, `OnEnemyRemoved`, Animator initialization, and GC collections.

Metric to record: worst and median frame time around each lifecycle event, GC allocation on those frames, object count before/after repeated cycle, and persistent memory growth.

CONFIRM if lifecycle frames repeatedly create visible spikes or retained-object/memory growth versus steady combat. REJECT if per-event costs remain within normal frame variance and memory returns to baseline.

### Potential fix

If confirmed, pool only enemy views, with explicit reset for transform, animator state, subscriptions, and health/state binding. Otherwise retain current simpler lifecycle.

### Estimated active implementation effort

M

### Regression risk

Medium — stale state, duplicate event subscriptions, and Animator reset defects are common pool regressions.

## PERF-04 — URP opaque-texture copy needs GPU verification

**Status:** HYPOTHESIS

**Severity:** Low

**Confidence:** Medium

### Location

`Assets/Settings/UniversalRP-Settings.asset`: `m_RequireOpaqueTexture: 1`; `Assets/Settings/ForwardRenderer.asset`: no renderer features. `Assets/Scenes/MainScene.unity`: one main camera, MSAA enabled, post-processing disabled.

### Evidence

**Static evidence**

URP requests an opaque texture. On relevant platform/path this can introduce color-copy/bandwidth work. Project is configured for Android and defaults to 1920×1080. Static settings do not prove that a `Copy Color` pass executes on target device, nor that its cost is meaningful. Scene/prefab scan found no existing particle systems or post-processing effects.

**Runtime evidence**

Not yet measured. No Frame Debugger or GPU Profiler capture supplied.

### Expected execution frequency

Potentially once per camera frame if URP produces opaque texture for active renderer/platform. Exact behavior must be observed in target build.

### Potential runtime impact

GPU and memory-bandwidth cost, especially at high resolution; not a claimed rendering bottleneck.

### Profiler validation

Gameplay scenario: full-cap enemy scene, joystick active, Game Over overlay hidden; capture same camera view on target Android device and Editor for diagnosis only.

Profiler module/tool: Frame Debugger, GPU Usage profiler or platform GPU tool, and Game View Stats.

Marker/call to inspect: `Copy Color`/opaque-texture pass, camera color target transitions, GPU duration, render passes, and render-target bandwidth indicators where available.

Metric to record: existence of copy pass, its GPU time, total GPU frame time, render-pass count, resolution, and draw/batch counts.

CONFIRM if active target capture contains copy pass with repeatable meaningful GPU time. REJECT if pass is absent or its cost is insignificant. Only then assess whether any material actually samples scene color before changing setting.

### Potential fix

Disable opaque texture only if capture confirms cost and material/frame-debugger audit proves scene-color sampling unnecessary.

### Estimated active implementation effort

S

### Regression risk

Medium — shaders or future screen-space feedback can require opaque texture.

# Top Profiling Experiments

1. **Full-cap enemy CPU/GC baseline — PERF-01, PERF-02.** Let 20 enemies spawn, keep them pursuing hero for 60 seconds, then repeat at zero enemies. Use development build CPU Usage Timeline/Hierarchy and Memory modules. Record frame time, `GC Alloc` per frame, GC collections, `EnemiesController.UpdateLoop`, `EnemyView.Update`, Animator, and Render Thread self/total time. Expected signal: recurring allocation from PERF-01 and count-correlated CPU from PERF-02, if either matters.

2. **Lifecycle spike test — PERF-03.** Kill every spawned enemy for two minutes; then reach 20 and press Restart. Use Timeline, Memory, and Profile Analyzer. Record worst/median spawn/destroy frame time, allocations, GC, object count, and retained memory after cycle. Expected signal: repeatable `Instantiate`/`Destroy` spikes or memory growth, not isolated Editor noise.

3. **Target-device GPU/frame pass capture — PERF-02, PERF-04.** On Android hardware, show 20 visible enemies with joystick active. Capture GPU Usage plus Frame Debugger. Record GPU frame time, `Copy Color` presence/cost, shadow passes, batches, draw calls, triangles, and Render Thread time. Expected signal: distinguish CPU enemy work from GPU/shadow/bandwidth limitation.

4. **UI interaction baseline — game-feel guardrail.** Hold and move joystick for 30 seconds; repeat with Game Over overlay shown. Use CPU Usage/UI markers, Frame Debugger, and Game View Stats. Record Canvas rebuild/batch activity, UI batches, and frame time. Expected signal: whether current single-Canvas UI leaves headroom for damage numbers or hit overlays.

# Rendering / Frame Debugger Checks

- Capture active scene at zero and 20 visible BeeNormal enemies. Compare opaque, shadow, transparent/UI pass count, batches, material switches, and triangles. BeeNormal is skinned, casts/receives shadows, and uses reflection probes; measure their actual contribution before tuning.
- Inspect whether `Copy Color`/opaque texture exists. If it does, inspect exact GPU duration and any material sampling scene color before considering setting change.
- Capture joystick inactive and held/moving. Verify whether moving its RectTransforms causes Canvas rebuilds or only transform updates; current UI is one scene Canvas with two joystick images plus Game Over elements.
- Add no rendering verdict from Game View Stats alone. Use it to orient capture, then validate with Frame Debugger/GPU timing.

# Performance Constraints for Added Game Feel

- **VFX/particles:** none exist today, so no current baseline cost. Add bounded concurrent effects and profile 20-enemy combat first. Prioritize transparent-overdraw and GPU-pass checks over draw-call count alone. Reuse/pool only if lifecycle capture confirms spikes at intended effect rate.
- **Audio:** no active `AudioSource` found in scene/prefabs and no runtime audio data exists. Establish target-device Audio module baseline before frequent hit/attack sounds; measure voice count, DSP load, and allocations during sustained combat.
- **Camera effects:** project uses Cinemachine follow camera. Measure after adding impulse/shake/extensions in full-cap combat; keep feedback event-driven, not a new per-enemy camera update loop.
- **UI feedback:** joystick already updates while held inside main Canvas. Add damage numbers/hit overlays only with Canvas rebuild, UI batch, and transparent-overdraw capture. Bound simultaneous feedback and test Game Over plus joystick-active state.
- **Gameplay simulation:** treat 20 active animated/skinned enemies as current measurement scenario. Increase enemy cap or add per-enemy feedback only after repeating PERF-01/PERF-02 capture at intended maximum.

# Low-Priority / Rejected Static Concerns

- `Resources.Load<T>` in `ScriptableObjectSingleton<T>` is cached after first access; startup-only configuration load, not recurring gameplay path.
- Reflection service discovery and dependency ordering run during `ServicesLocator.Awake`; no evidence of repeat use in play loop.
- `GetWeaponById` and `GetEnemyById` build dictionaries only once; no frequent caller found.
- Hero weapon instantiate/destroy occurs on startup or explicit weapon switch; no code shows frequent switching.
- No LINQ, physics raycasts/overlaps, `GetComponent`, hierarchy search, particle-system, or active audio call appears in production gameplay code. Do not invent concerns absent evidence.
- `Debug.Log` exists on attacks/hits, but configured weapon cooldown is 1.5 seconds and no capture establishes log overhead on target. Include it only if combat profile shows logging-related cost.
- `foreach` usage over small config/maps is not promoted; replacing it with `for` lacks evidence of value.
- Addressables package/settings exist, but game code loads current content through `Resources`/prefab references and no build layout or runtime loading trace exists. No loading/memory finding can be supported.
