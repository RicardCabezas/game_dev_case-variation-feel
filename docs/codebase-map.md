# Current Codebase Map

> Verified static-source map. Update this page when public contracts, service lifecycle, controller state/events, gameplay flow, configuration semantics, or presentation wiring changes. Source code remains authoritative.

## Runtime model

The project is a Unity 2022.3 arena game. Touch or mouse input controls a virtual joystick. While input is active, the hero moves; after input becomes inactive, the hero automatically attacks the nearest tracked enemy inside the equipped weapon range.

```text
ServicesLocator (persistent scene component)
  SettingsService
  JoystickInputService
  WeaponsService
  WorldService
  EntitiesService
    internal HeroController / EnemiesController
    IHeroPresentationSource / IEnemiesPresentationSource
  WavesService
    WaveController
    IWavesPresentationSource
  AutoAttackIndicatorService
    AutoAttackIndicatorController
  HealthBarsService
    HealthBarsCanvasController

Controllers/services own state and decisions
  typed events
MonoBehaviour views own transforms, Animator, UI, prefabs, and materials
```

`ServicesLocator` discovers concrete `IService` implementations by reflection, creates them, initializes declared dependencies first, and raises `OnAllServicesInitialized` after every initialization succeeds. Views subscribe to that event before resolving services. On locator teardown, services reset in reverse initialization order.

## Ownership and lifecycle

| Owner | State and decisions | Main consumers |
| --- | --- | --- |
| `JoystickInputService` | Touch/mouse polling and `JoystickState` | `EntitiesService`, `JoystickView`, auto-attack adapter |
| `WeaponsService` | Equipped weapon durability; weighted pickup scheduling, state, and arena-bounded placement | `EntitiesService`, weapon and hero views |
| `WorldService` | Instantiated persistent `WorldView` lifetime | Hero container |
| `EntitiesService` | Entity loop, lifecycle, combat routing, enemy creation/placement/capacity, restart, presentation sources | Gameplay/UI views and adapters |
| `HeroController` | Internal hero position, health, bounded arena movement/attack mode, target selection, cooldown, read-only presentation events | Exposed only as `IHeroPresentationSource` |
| `EnemiesController` | Internal enemy identities, chase, attacks, damage, removal, read-only presentation events | Exposed only as `IEnemiesPresentationSource` |
| `WavesService` | Wave ticking, entity spawn routing, enemy lifecycle consumption, wave-run restart | `WaveStateView` |
| `WaveController` | Current index, phase, batch order, pending spawns, accepted-spawn retry time, active wave-enemy IDs, completion | Exposed only as `IWavesPresentationSource` |
| `AutoAttackIndicatorController` | Cooldown-indicator visibility and duration | Auto-attack indicator view |
| `HealthBarsCanvasController` | Hero/enemy health-bar state, visibility, and timeout transitions | `HealthBarsCanvasView` |

Controllers and UI controllers are plain C# and must not depend on sibling controllers, gameplay services, reader presentation sources, views, Animator, UI components, camera, audio, particles, or other Unity presentation objects. Services own source subscriptions; views retain publisher references and unsubscribe in `OnDestroy`.

## Gameplay flow

1. `JoystickInputService` emits `OnStateChanged` only when `JoystickState` changes. Input uses the first touch, otherwise mouse input. Drag displacement clamps to `JoystickInputConfig.MaxRadius` screen pixels and becomes a normalized movement vector.
2. One `EntitiesService` Update loop reads input, weapon, scaled time, and delta time. It advances hero movement through `HeroController.Tick`, which clamps X/Z position to the arena bounds, and release cooldown, then separately asks `TryCreateAttackRequest` for an idle, eligible target.
3. A created hero attack request is routed to enemy damage; only accepted current targets confirm hero cooldown and attack presentation. Stale targets consume neither.
4. `WavesService` owns a separate Update loop. It applies shared wave spacing to entities, starts each authored wave after its `StartDelay`, requests one batch-ordered spawn per `SpawnInterval`, and routes the requested enemy type plus current wave cap through `EntitiesService.TrySpawnEnemy`; entities retain IDs, cap enforcement, random placement using each enemy's spawn radius, arena-bound clamping, state insertion, and spawn events.
5. After a wave's pending spawns reach zero, `WaveController` enters clearing and advances only after every enemy it confirmed through entity lifecycle events is removed. Failed entity creation keeps that spawn pending and retries after the current wave interval. Empty or invalid authored entries are skipped; final clear completes the run.
6. `EntitiesService` separately collects eligible enemy attack requests in stable ID order, advances movement and spacing through `EnemiesController.Tick`, then routes attacks. Accepted attacks are confirmed only after hero damage; remaining queued attacks stop after hero death.
7. Nonlethal enemy damage commits replacement state before `OnEnemyHit`. Lethal damage removes authoritative state, publishes self-sufficient hit payload, then removal. Enemy views may keep that removed identity visible for the one-second Bee death clip, but it no longer participates in gameplay or UI state.
8. `WavesService.RestartGame()` first resets wave state to wave zero, then calls `EntitiesService.RestartGame()`, which deactivates joystick, removes enemies normally, resets IDs and hero timing/state, and publishes restart snapshot. Old removal events cannot advance restarted waves.

No projectile, collider, raycast, hitbox, physical contact-point, score, reward, XP, loot, or win-condition path exists in the inspected runtime source.

## Event contracts and presentation

| Producer event | Timing and payload | Presentation consumer |
| --- | --- | --- |
| `ServicesLocator.OnAllServicesInitialized` | All services initialized; no payload | Scene and prefab views resolve services |
| `JoystickInputService.OnStateChanged` | Complete joystick state changes | Joystick view and UI service adapter |
| `IHeroPresentationSource.OnHeroPositionChanged` | Hero movement commit; `Vector3` world position | Hero view and health-bar adapter |
| `IHeroPresentationSource.OnHeroHit` | Accepted incoming damage; self-sufficient `HeroHitResult` with health, position, and lethality | Hero view, health-bar adapter, death UI |
| `IHeroPresentationSource.OnAttackPerformed` / `OnAttackCooldownStarted` | Confirmed attack target / cooldown duration | Hero view and auto-attack indicator adapter |
| `IHeroPresentationSource.OnRestarted` | Hero reset commit; restored `HeroState` | Hero view and UI service adapters |
| `IEnemiesPresentationSource` events | Spawn, movement, self-sufficient hit, confirmed attack, authoritative removal | Enemy views and health-bar adapter |
| `IWavesPresentationSource.OnStateChanged` | Complete `WaveState` replacement after wave start, accepted/rejected spawn, tracked removal, transition, completion, or restart | Wave-state UI view |
| `WeaponsService.OnWeaponChanged` | Successful weapon selection; `WeaponConfig` | Hero view replaces weapon prefab |
| `AutoAttackIndicatorController.OnStateChanged` | Complete indicator state replacement | Indicator view starts or hides fill |
| `HealthBarsCanvasController.OnHealthBarAdded` | New hero state or first visible enemy state; `HealthBarState` | Health-bars canvas view creates or reuses bar |
| `HealthBarsCanvasController.OnHealthBarChanged` | Health/fill/position/visibility replacement; `HealthBarState` | Health-bars canvas view updates bar |
| `HealthBarsCanvasController.OnHealthBarRemoved` | Enemy removal; `HealthBarId` | Health-bars canvas view destroys bar |

`HeroView` and `EnemyView` each use an explicit `HitFlashView` component configured on their prefab. `HeroView` owns transform, rotation, hero Animator, and instantiated weapon presentation. It drives hero `Speed`, `Attack`, and persistent `Death` Boolean parameters; restart clears `Death` and returns the Animator to idle. `EnemyView` owns facing and Bee `IsMoving`, `Attack`, `Damage`, and `Death` presentation. Lethal damage plays Bee `Die`, stops facing updates, and `EnemiesContainerView` destroys that view after the one-second clip; ordinary removal destroys immediately.

`WeaponUsesIndicatorView` displays weapon-use state and selects the sword icon while armed, or the empty-hand icon while unarmed.

## Configuration and content selection

- `ScriptableObjectSingleton<T>` lazily loads a Resources asset named after its concrete type and logs an error if absent.
- `HeroConfig` supplies prefab, initial health, and movement speed.
- `WavesConfig` supplies shared enemy spacing and ordered wave definitions. Each definition supplies first-spawn delay, retry/spawn interval, concurrent-enemy cap, and ordered `EnemyConfig` batches with counts; direct entries select runtime enemy types.
- `EnemyConfig` supplies combat/presentation properties and its own world-unit spawn radius.
- `WeaponConfig` supplies ID, damage, range, cooldown, and weapon view prefab.
- `WeaponsConfig` supplies pickup spawn interval, minimum/maximum radius around supplied center, maximum active pickups, and pickup prefab. `WeaponsService` selects eligible entries by configured spawn chance and clamps pickup X/Z positions to `Constants.World.ArenaLimit`.
- `WorldConfig` and `BiomeConfig` supply prefabs instantiated by their owning service/container.

Configuration assets may contain entries not selected by the current startup or spawn paths. Catalog membership alone does not prove runtime use.

## Documentation maintenance

Update this page with affected static facts only. Keep performance evidence, historical assessment, architectural intent, and proposals separate from current-source claims. Archived initial assessment lives under `docs/analysis/initial_assessment/` and is not maintained as current documentation.
* `Game.Weapons.WeaponsService` is authoritative for equipped durability and spawned pickup state.
