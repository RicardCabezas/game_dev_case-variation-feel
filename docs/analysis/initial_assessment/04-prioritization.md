# Assignment Scope Prioritization

> This document combines AI-assisted analysis with candidate playtesting and
> runtime profiling. It is a proposal; final scope decisions remain with the
> candidate.

## 1. Inputs

- **Codebase:** Small arena loop. Joystick movement; release triggers closest-
  enemy auto-attack. Controllers own state; views consume events. Combat lacks
  attack, damage, death, health, and enemy-attack feedback. Movement and
  start/Game Over/Restart already communicate state.
- **Performance:** 20 moving bees, current cap, hold 11.08 ms median over 299
  frames at explicit 90-FPS testing. 200 bees reach 19.87 ms median; 2,000
  moving bees reach 94.57 ms. Large-swarm work is outside current scope.
- **Confirmed optimization:** Removing one recurring hero-hit `Debug.Log` cut
  matched 2,000-bee stationary median from 71.77 to 36.09 ms. Normal-cap A/B is
  still required.
- **Unverified concern:** `new List<int>(_enemies.Keys)` likely allocates each
  frame, but no runtime call stack proves normal-cap impact.
- **Lifecycle evidence:** One spawn frame allocates 9.7 KB GC; one death frame
  allocates 19.3 KB. After multiple bee lifecycles, memory totals remain stable:
  no retained-enemy growth or leak is shown. Simultaneous-death cost is untested.
- **Game feel:** Strongest clusters are attack legibility and incoming-threat
  readability.
- **Candidate playtest:** Attack start/targeting, enemy hit/death, incoming
  attack, hero damage, and health feedback feel poor. Pursuit is tolerable.
  Start/end/restart feel acceptable.
- **Candidate decisions:** Keep auto-attack and immediate damage; use existing
  enemy Damage clip; always show hero health; target 60 FPS; preserve health
  feedback when cutting; exclude audio and biome-selection work.
- **Profiling evidence:** `docs/profiling/baseline/` is absent. Runtime numbers
  come from inspectable target-device captures summarized in
  `02-performance-audit.md`. Raw Unity profiler captures were not parsed
  directly.

## 2. Findings I Would Reject or Defer

| ID | Origin | Decision | Reason |
| --- | --- | --- | --- |
| PERF-02 | Performance audit | Reject | Only unsupported 200/2,000-bee scales miss budget. Current 20-bee cap is healthy. |
| PERF-03 | Performance audit | Defer | Allocation is likely but unmeasured; changing iteration risks unsafe removal. |
| Lifecycle/pooling note | Performance audit | Reject | Spawn/death allocate, but repeated churn shows no retained-memory growth or leak. No measured hitch justifies pooling at 20 enemies. |
| Opaque Texture note | Performance audit | Defer | `CopyColor` disappeared in an instrumented sample, but release magnitude and visual safety remain unconfirmed. |
| GF-03 | Game-feel audit | Nice to have | Death reaction matters, but lifecycle/Animator work has lower impact than normal-hit clarity and health. |
| GF-06 | Game-feel audit | Reject | Candidate excludes audio for time. |
| GF-07 | Game-feel audit | Defer | Camera accents risk fatigue and are secondary to animation/UI feedback. |
| GF-08 | Game-feel audit | Defer | Candidate finds pursuit tolerable. |
| GF-09 | Game-feel audit | Reject | Candidate finds start/end/restart acceptable. |

Also defer DOTS, broad rendering/model/shadow work, progression, loot,
score, damage numbers, critical hits, and biome selection. None supports this
eight-hour combat-readability slice.

## 3. Game-Feel Ranking

Scores: 5 is strongest. `Ease` and `Safety` score cheap/low-risk work higher.

| Rank | ID | Opportunity | Impact | Frequency | Cohesion | Ease | Tuning | Safety |
| ---: | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | GF-01 | Readable auto-attack | 5 | 5 | 5 | 4 | 5 | 4 |
| 2 | GF-05 | Always-visible hero health | 5 | 4 | 5 | 4 | 4 | 4 |
| 3 | GF-04 | Enemy attack and hero-hit response | 5 | 4 | 5 | 3 | 5 | 3 |
| 4 | GF-02 | Enemy damage reaction | 5 | 5 | 5 | 3 | 5 | 4 |
| 5 | GF-03 | Lethal-hit confirmation | 4 | 3 | 4 | 3 | 5 | 3 |
| 6 | GF-07 | Camera accent | 3 | 3 | 3 | 4 | 5 | 3 |
| 7 | GF-08 | Pursuit animation | 2 | 5 | 3 | 4 | 3 | 4 |
| 8 | GF-09 | Death/restart polish | 2 | 1 | 2 | 4 | 4 | 4 |
| 9 | GF-06 | Audio hierarchy | 4 | 5 | 5 | 2 | 5 | 3 |

GF-06 ranks last for assignment suitability because candidate excluded it.

## 4. Performance Ranking

| ID | Classification | Judgment |
| --- | --- | --- |
| PERF-01 | **CONFIRMED AND WORTH ADDRESSING** | Combat logs have measured stress cost; change is small and supports clean profiling. Validate at 20 bees. |
| TARGET-60 | **GAME-FEEL ENABLER** | Candidate chose 60 FPS. Default was 30; 20 bees already hold 11.08 ms median under 90-FPS testing. Validate 16.67 ms pacing and thermals. |
| PERF-02 | **CONFIRMED BUT NOT WORTH ASSIGNMENT TIME** | Only 200/2,000-bee runs fail; product cap is 20. |
| Lifecycle/pooling note | **CONFIRMED BUT NOT WORTH ASSIGNMENT TIME** | Spawn/death frames allocate 9.7/19.3 KB, but normal repeated churn shows no retained growth. Measure a hitch before pooling. |
| PERF-03 | **UNVERIFIED / DEFER** | Key-copy allocation lacks attributed runtime impact. |

Selected feedback remains event-driven and bounded at 20 enemies. No separate
simulation/render rewrite is required.

## 5. Recommended Scope

Five MUST items. Estimated implementation: 3.75–4.75 hours. Remaining time is
reserved for playtesting, tuning, profiling, debugging, cleanup, docs, and
contingency.

### MUST DO

#### COMBAT-01 — Readable auto-attack

- **Why / player impact:** Core action has no visible start or target. Emit a
  confirmed-strike event; face target and play existing hero Attack clip.
- **Technical value:** Typed controller-to-view event; presentation stays out of
  gameplay authority.
- **Time:** 50–65 minutes.
- **Dependencies:** Current target selection, `HeroView`, hero Animator.
- **Risk:** Animation/movement transition conflict. Keep damage immediate.
- **Manual validation:** Release near/far targets, switch targets, repeat cooldown,
  move/attack transition, no-target case.
- **Measurement:** Compare hero Animator time, GC Alloc, and 20-bee frame P50/P99.

#### COMBAT-02 — Enemy hit reaction

- **Why / player impact:** Hits resemble misses. Emit enemy ID, damage result,
  and lethal state; play existing enemy Damage clip.
- **Technical value:** Authoritative damage event using existing ID-to-view map.
- **Time:** 50–65 minutes.
- **Dependencies:** Event convention from COMBAT-01; enemy Animator.
- **Risk:** Animator interruption. Use brief scale/material fallback only if clip
  cannot be wired inside budget.
- **Manual validation:** Repeated hits, multiple targets, movement, lethal hit.
- **Measurement:** Check recurring allocation, Animator cost, batches, SetPass,
  and 20-bee frame P50/P99.

#### COMBAT-03 — Incoming damage and health

- **Why / player impact:** Enemy attacks and accumulated damage are invisible.
  Add rate-limited hero hit response and always-visible health bar. Add enemy
  attack animation only if time survives.
- **Technical value:** State-bound UI, event-driven presentation, multi-attacker
  conflict handling.
- **Time:** 75–100 minutes.
- **Dependencies:** Existing hero state and enemy attack boundary.
- **Risk:** Reaction restart spam and Canvas rebuilds.
- **Manual validation:** One/many attackers, moving damage, lethal hit, restart,
  full-to-zero health.
- **Measurement:** UI rebuild/layout calls, UI batches, GC Alloc, Animator time,
  and worst synchronized-hit frame.

#### PERF-01 — Gate recurring combat logs

- **Why / player impact:** Only small optimization with strong runtime evidence;
  removes profiler/console noise and possible combat spikes.
- **Technical value:** Measured optimization, not speculative cleanup.
- **Time:** 15 minutes code; 40–60 minutes matched A/B.
- **Dependencies:** Capture before changing logs; keep feedback changes separate.
- **Risk:** Reduced debugging visibility. Preserve errors and approved dev logs.
- **Manual validation:** Combat, death, Game Over, and restart remain identical.
- **Measurement:** Log/stack-trace calls and time, GC Alloc, collections, combat
  marker time, frame P50/P99, and worst combat frame.

#### TARGET-60 — Deliberate Android frame-rate policy

- **Why / player impact:** Candidate wants 60 FPS; default was 30 and temporary
  profiling override is 90. Use intentional 60-FPS startup policy.
- **Technical value:** Explicit product behavior backed by device evidence.
- **Time:** 15 minutes code; 40–50 minutes matched A/B.
- **Dependencies:** Remove temporary 90-FPS override; use same device/build.
- **Risk:** Battery, heat, and GPU load.
- **Manual validation:** Motion/input, pause/resume, Game Over/restart, longer play.
- **Measurement:** Effective target, frame P50/P99, missed frames, CPU excluding
  waits, reliable GPU time, temperature, and battery trend.

### NICE TO HAVE

- **GF-03:** Keep dead enemy out of gameplay immediately, but allow brief view-
  only death reaction before destruction. Budget: 30–45 minutes. Re-profile
  simultaneous deaths if promoted; do not add pooling without a measured hitch.

### DEFER

Audio, camera shake, pursuit polish, start/Game Over/restart polish, biome work,
Opaque Texture optimization, large-swarm work, key-copy rewrite, pooling, and
new progression systems. Pooling requires a measured lifecycle hitch, especially
for simultaneous deaths; current memory evidence does not justify it.

## 6. Cohesion Check

```text
Auto-target -> visible strike -> enemy reaction
Enemy attack -> hero reaction -> persistent health consequence
60 FPS -> clearer motion and response
Log gating -> clean before/after combat evidence
```

Everything improves cause/effect in existing combat. No new mechanic, content
system, or sensory channel.

## 7. Suggested Execution Order

Maximum: 8 hours.

1. **Manual baseline, 15–20m:** Record poor combat feedback.
2. **TARGET-60, 40–50m:** Matched default-30/explicit-60 capture; remove 90-FPS
   test override.
3. **PERF-01, 40–60m:** Matched logs-on/off capture at 60 FPS.
4. **COMBAT-01, 50–65m:** Implement, playtest, tune.
5. **COMBAT-02, 50–65m:** Implement, playtest, profile guardrail.
6. **COMBAT-03, 75–100m:** Implement, test crowd damage and restart.
7. **Integrated test/tuning, 35–50m:** Remove competing/restarting effects;
   record after video.
8. **Final profile/cleanup/docs, 50–70m:** Repeat guardrails; inspect events,
   allocations, and diff.

Safe parallel AI work after APIs stabilize: profiler metric tabulation, final
diff review, and README measurement-table drafting. Candidate owns playtesting,
tuning, animation choice, device validation, and cuts. Do not concurrently edit
hero attack/hero damage or enemy damage/enemy attack systems. Never change code
during an A/B capture.

## 8. Measurement Plan

### TARGET-60

- **BEFORE:** Same device/build, warmed fixed 20-moving-bee route, default mobile
  target, 300 clean frames. Record effective target, P50/P99, missed frames, CPU
  excluding waits, reliable GPU time, temperature, and battery trend.
- **AFTER:** Exact scenario and metrics at explicit 60 FPS. Success: stable
  16.67 ms pacing without unacceptable heat, battery, input, or visual impact.

### PERF-01

- **BEFORE:** Warmed fixed-camera 20-bee combat, logs enabled, comparable hit
  count, 300 frames. CPU Hierarchy/Timeline and Profile Analyzer with GC Alloc.
- **AFTER:** Exact scenario with only recurring combat logs gated. Success: log
  descendants disappear/fall, gameplay stays identical, affected frames improve.

### COMBAT-01/02 guardrail

- **BEFORE:** Twenty enemies; repeat move/release/attack sequence. Record frame
  P50/P99, GC Alloc, Animator time, batches, and SetPass.
- **AFTER:** Exact scenario after feedback. Success: readable reactions, no
  recurring allocation or unexplained rendering growth.

### COMBAT-03 guardrail

- **BEFORE:** Thirty seconds joystick movement with 20 enemies, then synchronized
  hits and Game Over/restart. Record Canvas rebuild/layout, UI batches, GC Alloc,
  Animator time, P50/P99, and worst hit frame.
- **AFTER:** Exact scenario after health/hit feedback. Success: bounded UI work,
  no recurring layout/GC, no continuously restarting hero reaction.

## 9. Cut Line

### First thing to cut

Enemy attack animation inside COMBAT-03. Keep always-visible health and hero hit
response.

### Second thing to cut

Enemy Damage-clip integration complexity. Keep same damage event and use one
simple bounded flash/scale response.

### Minimum Viable Strong Submission

1. TARGET-60 with matched pacing evidence.
2. COMBAT-01 visible auto-attack.
3. COMBAT-02 simplest enemy hit response.
4. COMBAT-03 always-visible health plus rate-limited hero response.
5. PERF-01 measured log A/B.
6. Before/after video, final 20-enemy guardrail, cleanup, and concise docs.

This preserves candidate's health priority while demonstrating game-feel
judgment, Unity engineering, performance awareness, and measurement.

## 10. Questions Requiring Candidate Decision

Resolved:

1. Keep auto-attack; improve feedback.
2. Keep damage immediate.
3. Prioritize existing enemy Damage clip.
4. Keep hero health always visible.
5. Preserve health feedback when cutting.
6. Target 60 FPS.
7. Exclude audio, biome work, pursuit polish, and start/end/restart polish.

No blocking question remains. Final approval and implementation-time cuts remain
candidate decisions.
