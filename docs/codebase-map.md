# Current Codebase Map

> Verified static-source map. Update this page when public contracts, service lifecycle, controller state/events, gameplay flow, configuration semantics, or presentation wiring changes. Source code remains authoritative.

## Runtime model

The project is a Unity 2022.3 arena game. Touch or mouse input controls virtual joystick. Normal active input moves hero; second press inside configured double-tap window opens dash-aiming joystick. Valid dash release consumes equipped weapon, moves hero instantly along bounded path, and damages enemies intersecting its capsule. After normal input becomes inactive, hero automatically attacks nearest tracked enemy inside equipped weapon range.

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
  BiomesService
    BiomeController
    IBiomePresentationSource
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
| `JoystickInputService` | Touch/mouse polling, normal/dash-aiming `JoystickState`, valid dash-release event | `EntitiesService`, `JoystickView`, auto-attack adapter |
| `WeaponsService` | Equipped weapon durability including valid-dash destruction; weighted pickup scheduling, state, and arena-bounded placement | `EntitiesService`, weapon and hero views |
| `WorldService` | Instantiated persistent `WorldView` lifetime and camera presentation | Hero container |
| `EntitiesService` | Entity loop, pending-dash routing, lifecycle, combat routing, enemy creation/placement/capacity, restart, presentation sources | Gameplay/UI views and adapters |
| `HeroController` | Internal hero position, health, bounded arena movement/dash/attack mode, target selection, cooldown, read-only presentation events | Exposed only as `IHeroPresentationSource` |
| `EnemiesController` | Internal enemy identities, dash-capsule query, chase, attacks, damage, removal, read-only presentation events | Exposed only as `IEnemiesPresentationSource` |
| `WavesService` | Wave ticking, entity spawn routing, enemy lifecycle consumption, wave-run restart | `WaveStateView` |
| `WaveController` | Current index, phase, batch order, pending spawns, accepted-spawn retry time, active wave-enemy IDs, completion | Exposed only as `IWavesPresentationSource` |
| `BiomesService` / `BiomeController` | Wave-indexed active biome presentation selection and restart state | `BiomeContainerView` through `IBiomePresentationSource` |
| `AutoAttackIndicatorController` | Cooldown-indicator visibility and duration | Auto-attack indicator view |
| `HealthBarsCanvasController` | Hero/enemy health-bar state, visibility, and timeout transitions | `HealthBarsCanvasView` |
| `GameEndAnalyticsService` | Per-run accepted damage, confirmed weapon attacks, and committed dash counts observed from hero events; resets with hero restart | Game-end overlay |

Controllers and UI controllers are plain C# and must not depend on sibling controllers, gameplay services, reader presentation sources, views, Animator, UI components, camera, audio, particles, or other Unity presentation objects. Services own source subscriptions; views retain publisher references and unsubscribe in `OnDestroy`.

`Assets/Core/Constants/Scripts/Game.Constants.asmdef` contains shared gameplay constants. `Game.Entities` and `Game.Weapons` both reference it. `Game.World` references `Game.Entities` for hero/camera presentation; entity gameplay does not depend on world presentation.

## Gameplay flow

1. `JoystickInputService` emits `OnStateChanged` only when `JoystickState` changes. Input uses first touch, otherwise mouse input. Drag displacement clamps to `JoystickInputConfig.MaxRadius` screen pixels and becomes normalized vector. Short primary release arms second press inside `SecondTapWindow` as generic `Secondary` input; valid release emits `OnSecondaryInputReleased`. `EntitiesService` interprets secondary input as dash request, so dash gameplay remains outside joystick service.
2. One `EntitiesService` Update loop reads input, weapon, scaled time, and delta time. It advances hero movement through `HeroController.Tick`, which clamps X/Z position to arena bounds and release cooldown. It resolves one pending dash before hero autoattack and enemy attack collection: armed, living hero commits bounded endpoint, kills enemies from X/Z capsule query through normal damage routing, destroys weapon, and skips autoattack that frame. Unarmed or invalid requests do nothing.
3. A created hero attack request is routed to enemy damage; only accepted current targets confirm hero cooldown and attack presentation. Stale targets consume neither.
4. `WavesService` owns a separate Update loop. It applies shared wave spacing to entities, starts each authored wave after its `StartDelay`, requests one batch-ordered spawn per `SpawnInterval`, and routes the requested enemy type plus current wave cap through `EntitiesService.TrySpawnEnemy`; entities retain IDs, cap enforcement, random placement using each enemy's spawn radius, arena-bound clamping, state insertion, and spawn events.
5. After a wave's pending spawns reach zero, `WaveController` enters clearing and advances only after every enemy it confirmed through entity lifecycle events is removed. Failed entity creation keeps that spawn pending and retries after the current wave interval. Empty or invalid authored entries are skipped; final clear completes the run.
6. `BiomesService` depends on `WavesService` and consumes wave snapshots. It changes the arena only when a new wave enters spawning; clearing snapshots retain the previous biome. Current mapping is Dungeon (wave 0), Water (wave 1), and Fire (wave 2).
7. `EntitiesService` separately collects eligible enemy attack requests in stable ID order, advances movement and spacing through `EnemiesController.Tick`, then routes attacks. Accepted attacks are confirmed only after hero damage; remaining queued attacks stop after hero death.
8. Nonlethal enemy damage commits replacement state before `OnEnemyHit`. Lethal damage removes authoritative state, publishes self-sufficient hit payload, then removal. Enemy views may keep that removed identity visible for the one-second Bee death clip, but it no longer participates in gameplay or UI state.
9. `WavesService.RestartGame()` resets wave state to wave zero before entities clear old enemies; `BiomesService` observes that snapshot and restores Dungeon. Entities then deactivate joystick, remove enemies normally, reset IDs and hero timing/state, and publish restart snapshot. Old removal events cannot advance restarted waves.

No projectile, collider, raycast, hitbox, physical contact-point, score, reward, XP, or loot path exists in the inspected runtime source. A completed wave run is presented as a game win.

## Event contracts and presentation

| Producer event | Timing and payload | Presentation consumer |
| --- | --- | --- |
| `ServicesLocator.OnAllServicesInitialized` | All services initialized; no payload | Scene and prefab views resolve services |
| `JoystickInputService.OnStateChanged` | Complete joystick state changes | Joystick view and UI service adapter |
| `IHeroPresentationSource.OnHeroPositionChanged` | Hero movement commit; `Vector3` world position | Hero view and health-bar adapter |
| `IHeroPresentationSource.OnHeroHit` | Accepted incoming damage; self-sufficient `HeroHitResult` with health, position, and lethality | Hero view, health-bar adapter, death UI |
| `IHeroPresentationSource.OnAttackPerformed` / `OnAttackCooldownStarted` | Confirmed attack target / cooldown duration | Hero view and auto-attack indicator adapter |
| `IHeroPresentationSource.OnDashPerformed` | Authoritative `HeroDashRequest` after endpoint position commit | Hero view trail and attack presentation |
| `IHeroPresentationSource.OnRestarted` | Hero reset commit; restored `HeroState` | Hero view and UI service adapters |
| `IEnemiesPresentationSource` events | Spawn, movement, self-sufficient hit, confirmed attack, authoritative removal | Enemy views and health-bar adapter |
| `IWavesPresentationSource.OnStateChanged` | Complete `WaveState` replacement after wave start, accepted/rejected spawn, tracked removal, transition, completion, or restart | Wave-state UI view |
| `WeaponsService.OnWeaponChanged` | Successful weapon selection; `WeaponConfig` | Hero view replaces weapon prefab |
| `WeaponsService.OnEquippedWeaponDestroyed` | Confirmed attack depletes, or valid dash destroys, an armed weapon after state becomes unarmed | Broken-weapon animation view replays its UI clip |
| `AutoAttackIndicatorController.OnStateChanged` | Complete indicator state replacement | Indicator view starts or hides fill |
| `HealthBarsCanvasController.OnHealthBarAdded` | New hero state or first visible enemy state; `HealthBarState` | Health-bars canvas view creates or reuses bar |
| `HealthBarsCanvasController.OnHealthBarChanged` | Health/fill/position/visibility replacement; `HealthBarState` | Health-bars canvas view updates bar |
| `HealthBarsCanvasController.OnHealthBarRemoved` | Enemy removal; `HealthBarId` | Health-bars canvas view destroys bar |
| `GameEndAnalyticsService` | Observes hero hit, confirmed attack, dash, and restart events; exposes current run totals | Game-end overlay reads totals when waves complete |

`HeroView` and `EnemyView` each use an explicit `HitFlashView` component configured on their prefab. `WorldView` subscribes to hero hit events for `CameraShakeView`, and dash events for `CameraZoomView`. `HeroView` owns transform, rotation, hero Animator, instantiated weapon presentation, and prefab-assigned wide fading dash trail configured through serialized renderer values. It drives hero `Speed`, `Attack`, and persistent `Death` Boolean parameters; only normal joystick input drives `Speed` and rotation. Dash path comes from `OnDashPerformed`, while root position remains authoritative and instant. Restart clears `Death` and returns Animator to idle. `EnemyView` owns facing and Bee `IsMoving`, `Attack`, `Damage`, and `Death` presentation. Lethal damage plays Bee `Die`, stops facing updates, and `EnemiesContainerView` destroys that view after one-second clip; ordinary removal destroys immediately.

`WeaponUsesIndicatorView` displays weapon-use state and selects the sword icon while armed, or the empty-hand icon while unarmed.
`BrokenWeaponAnimationView` keeps `BrokenWeaponAnimationConainer` hidden except while replaying its authored legacy animation after equipped weapon durability reaches zero.
`GameEndOverlayView` owns game-end presentation and restart request wiring. It shows `GAME OVER` on lethal hero damage and `GAME WON` with run statistics after all waves complete.

## Configuration and content selection

- `ScriptableObjectSingleton<T>` lazily loads a Resources asset named after its concrete type and logs an error if absent.
- `HeroConfig` supplies prefab, initial health, movement speed, dash distance, and dash hit radius.
- `JoystickInputConfig` supplies joystick radius, primary-to-secondary tap window, valid secondary-release magnitude, and secondary-input tint.
- `WavesConfig` supplies shared enemy spacing and ordered wave definitions. Each definition supplies first-spawn delay, retry/spawn interval, concurrent-enemy cap, and ordered `EnemyConfig` batches with counts; direct entries select runtime enemy types.
- `EnemyConfig` supplies combat/presentation properties and its own world-unit spawn radius.
- `WeaponConfig` supplies ID, damage, range, cooldown, weapon view prefab, and config-owned pickup Quad color. `WeaponPickupView` owns pickup color application through a material property block plus root spin/levitation presentation; its collider follows that root lift.
- `WeaponsConfig` supplies pickup spawn interval, minimum/maximum radius around supplied center, maximum active pickups, and pickup prefab. `WeaponsService` selects eligible entries by configured spawn chance and clamps pickup X/Z positions to `Constants.World.ArenaLimit`.
- `WorldConfig` supplies the persistent world prefab. `BiomeConfig` supplies wave-indexed biome arena prefabs and optional skybox materials; `BiomesService` owns selection and `BiomeContainerView` owns instantiation.

Configuration assets may contain entries not selected by the current startup or spawn paths. Catalog membership alone does not prove runtime use.

## Documentation maintenance

Update this page with affected static facts only. Keep performance evidence, historical assessment, architectural intent, and proposals separate from current-source claims. Archived initial assessment lives under `docs/analysis/initial_assessment/` and is not maintained as current documentation.
* `Game.Weapons.WeaponsService` is authoritative for equipped durability and spawned pickup state.
