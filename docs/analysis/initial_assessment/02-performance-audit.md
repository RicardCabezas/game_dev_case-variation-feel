# Performance Investigation

> AI-assisted performance investigation combining static code analysis
> with runtime profiling evidence where available. Static findings are
> not treated as measured performance problems unless validated using
> runtime data.

## Evidence used

Reviewed gameplay code, prefabs, URP/quality settings, and project settings. Runtime evidence was provided by the candidate from target-device Unity Profiler, Profile Analyzer, GPU Usage/Hierarchy, and Frame Debugger captures.

Raw Unity `.data` / `.pdata` captures were not used for timing claims because they are proprietary binary formats that cannot be safely decoded here. GPU recording warns about measurement overhead; GPU-Hierarchy numbers identify contributors and bottleneck direction, but are not release-frame-time predictions.

Default gameplay configuration caps enemies at 20. Android project code does not set `Application.targetFrameRate`; the observed initial 30 FPS cadence was Unity mobile's default target, not a measured rendering limit. Tests below use an explicit 90 FPS target.

Candidate-provided target-device Memory module captures show one spawn frame (9.7 KB GC) and one death frame (19.3 KB GC). By the death capture, multiple bees had already completed their lifecycle. Resident/allocated summary and displayed asset-category totals remain unchanged, supporting no accumulated retained-enemy memory or leak under repeated normal churn. This does not measure the single-frame cost of many simultaneous deaths.

## Tested cases

| Case | Runtime result | Conclusion |
| --- | --- | --- |
| **20 moving bees** | 299 frames: 11.08 ms median, 10.93–11.21 ms IQR, 12.12 ms max | Stable near 90 FPS. Current configured cap is healthy on tested device. |
| **200 moving bees** | 299 frames: 19.87 ms median, 18.76–21.03 ms IQR, 26.84 ms max | About 50 FPS; misses 90 FPS budget. GPU/render work is limiting direction. |
| **2,000 stationary bees** | 36.09 ms median after hit-log removal; 71.77 ms before | Hit logging accounted for a 49.7% median-frame reduction. Stress-only result. |
| **2,000 moving bees** | 94.57 ms median; GPU Hierarchy selected 197.84 ms GPU / 80.76 ms CPU | Extreme swarm is both GPU- and CPU-limited. Not representative of configured gameplay. |

## PERF-01 — High-frequency combat logging creates avoidable combat cost

**Status:** CONFIRMED

**Severity:** Medium

**Confidence:** High

### Location

`EnemiesController.AttackEnemy`; `HeroController.TakeHit`.

### Evidence

**Static evidence**

Both recurring combat methods call interpolated `Debug.Log`; hit/death paths add further logs.

**Runtime evidence**

In 2,000-bee stress, `TakeHit` and nested log/stack-trace markers were about 40 ms when present. Removing only `HeroController.TakeHit`'s hit log reduced median frame time from 71.77 ms to 36.09 ms (49.7%).

### Expected execution frequency

Every hero attack or enemy hit. Frequency rises sharply when many enemies attack together.

### Potential runtime impact

CPU spikes, stack-trace work, managed allocations, Console/profiler traffic, and possible GC.

### Profiler validation

At normal 20-bee combat, compare 300 frames with/without development-only combat logs. Record call count, `Debug.Log` total time, GC Alloc, P50/P99 frame time, and worst combat frame. Confirm if log removal improves comparable combat frames; reject as a release issue if logs are absent/negligible in the player build.

### Potential fix

Gate high-frequency combat logs behind an explicit development-only logging mechanism. Do not remove telemetry or errors without approval.

### Estimated active implementation effort

S

### Regression risk

Low — reduced debugging visibility only.

## PERF-02 — Moving swarms above 20 bees exceed the 90-FPS budget

**Status:** CONFIRMED

**Severity:** Medium

**Confidence:** High

### Location

`EnemiesController.UpdateEnemy`; `EnemyView.Update`; `BeeNormal` prefab (`Animator`, `SkinnedMeshRenderer`, shadows).

### Evidence

**Static evidence**

Each live enemy is moved, state-updated, and emits position changes. Each view also rotates every frame. Bees are animated, skinned, and cast/receive shadows. Default cap is 20.

**Runtime evidence**

20 moving bees hold 11.08 ms median. At 200 moving bees, median rises to 19.87 ms. CPU profile spends 9.10 ms in present wait, while GPU Hierarchy shows 58.52 ms GPU versus 21.90 ms CPU in an instrumented selected frame. It records 407 batches, 366k triangles, 257k vertices, and 4.79 ms in `UpdateAllSkinnedMeshes` across 400 draws.

At 2,000 moving bees, frame median reaches 94.57 ms; selected GPU Hierarchy is 197.84 ms GPU versus 80.76 ms CPU. Movement increases animation/job work and render/present waits. GPU instrumentation inflates exact timing but supports GPU as the limiting direction above the normal cap.

### Expected execution frequency

Per live enemy, every frame while the hero moves and bees follow.

### Potential runtime impact

Above the 20-enemy cap: GPU rendering/skinning, render/present waits, animation/jobs, and main-thread simulation. No current 20-bee bottleneck is measured.

### Profiler validation

Capture 300-frame moving runs at 20, 50, 100, 150, and 200 bees after warm-up. Record P50/P99 frame time, CPU work excluding waits, batches, triangles, skinned-mesh work, and GPU time. Confirm the product-supported count from its target-frame budget; use Android GPU trace if count above 20 becomes product scope.

### Potential fix

No change needed for the current cap. If larger swarms are intended: first keep combat logs gated, then reduce update/animation/render participation for distant or off-screen enemies based on a measured count budget.

### Estimated active implementation effort

M

### Regression risk

Medium — changes can affect enemy responsiveness, readability, or shadows.

## PERF-03 — Per-frame enemy-key copy may allocate managed memory

**Status:** LIKELY

**Severity:** Low

**Confidence:** High

### Location

`Assets/Features/Entities/Scripts/Controllers/EnemiesController.cs`, `UpdateLoop`.

### Evidence

**Static evidence**

The update loop creates `new List<int>(_enemies.Keys)` every frame before updating enemies.

**Runtime evidence**

Not yet attributed. Captures show small per-frame GC allocation in some gameplay frames, but no call-stack evidence ties it to this list copy.

### Expected execution frequency

Once per gameplay frame while enemies exist; allocation size grows with active enemy count.

### Potential runtime impact

Managed allocation and later GC. No measured normal-cap frame-time effect claimed.

### Profiler validation

At 20 moving bees, inspect `EnemiesController.UpdateLoop` in CPU Hierarchy with GC Alloc and call stacks. Record allocation/call, collection count, and total time. Confirm only if this call allocates repeatedly and correlates with GC or frame-time cost; reject if insignificant on device.

### Potential fix

Reuse an ID buffer, or avoid the copy only if safe against collection mutation during update.

### Estimated active implementation effort

S

### Regression risk

Medium — removal during enumeration must remain safe.

# Top Profiling Experiments

1. **Normal-cap confidence run — PERF-01, PERF-02.** 20 moving bees, 300 frames, target 90, with GPU recording disabled. Record P50/P99, CPU excluding waits, GC Alloc, thermals, and battery. Expected signal: retain stable ~11.1 ms pacing.

2. **Swarm threshold sweep — PERF-02.** Moving 20/50/100/150/200 bees, same camera/device/build. Record P50/P99, batches, geometry, skinning, and present wait. Expected signal: define supported swarm count for 60 and 90 FPS.

3. **GPU trace for a product swarm count — PERF-02.** If more than 20 bees are planned, capture Android GPU trace at the intended count. Record GPU active time, queue depth, skinned-mesh cost, opaque/shadow work, and present timing. Expected signal: distinguish real GPU saturation from Unity GPU-profiler overhead.

# Rendering / Frame Debugger Checks

- Current normal-cap scene uses a small number of SetPass calls; draw-call reduction alone is not the priority.
- At 200+ bees, inspect per-bee opaque and shadow events, `UpdateAllSkinnedMeshes`, triangle/vertex growth, and material/shader variants.
- Keep Opaque Texture disabled only if visual audit passes. Its removal eliminated `CopyColor` and improved an instrumented no-bee GPU sample, but needs clean platform-trace confirmation for release magnitude.

# Performance Constraints for Added Game Feel

- **Normal cap:** 20 moving bees sustain ~90 FPS on tested device. Use this as the baseline, not the 200/2,000 stress results.
- **VFX/particles:** bound concurrent transparent effects; profile them during 20-bee movement before shipping. Do not pool by default; validate churn first.
- **Audio:** keep hit/attack audio bounded and avoid per-hit logging. Establish voice/DSP baseline before adding layered swarm audio.
- **Camera/UI:** profile camera shake, damage numbers, and overlays alongside moving 20 bees. Check Canvas rebuilds and transparent overdraw.
- **Large swarms:** 200 bees already misses 90 FPS; 2,000 is far beyond current content budget. Treat higher counts as a separate feature with its own CPU/GPU budget.

# Low-Priority / Rejected Static Concerns

- `Resources.Load<T>` singleton loading is cached/startup-only.
- Service discovery and dependency ordering are startup work.
- Weapon instantiate/destroy is startup/switch-only, not recurring combat churn.
- Individual spawn/death frames allocate 9.7 KB / 19.3 KB GC. After multiple completed bee lifecycles, no retained-memory growth is shown. Pooling is not justified at the current cap without a measured lifecycle hitch, particularly from simultaneous deaths.
- No evidence supports broad `foreach`/LINQ/GetComponent/pooling/DOTS rewrites.
- No physics query, particle, or audio bottleneck is measured.
