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
    HeroController
    EnemiesController
  AutoAttackIndicatorService
    AutoAttackIndicatorController

Controllers/services own state and decisions
  typed events
MonoBehaviour views own transforms, Animator, UI, prefabs, and materials
```

`ServicesLocator` discovers concrete `IService` implementations by reflection, creates them, initializes declared dependencies first, and raises `OnAllServicesInitialized` after every initialization succeeds. Views subscribe to that event before resolving services. On locator teardown, services reset in reverse initialization order.

## Ownership and lifecycle

| Owner | State and decisions | Main consumers |
| --- | --- | --- |
| `JoystickInputService` | Touch/mouse polling and `JoystickState` | `HeroController`, `JoystickView`, auto-attack indicator controller |
| `WeaponsService` | Equipped `WeaponConfig`; index-zero startup selection | `HeroController`, `HeroView` |
| `WorldService` | Instantiated persistent `WorldView` lifetime | Hero container |
| `EntitiesService` | Composes hero and enemy controllers | Gameplay/UI views and services |
| `HeroController` | Hero position, health, death, movement/attack mode, target selection, cooldown | Hero, game-over, and indicator presentation |
| `EnemiesController` | Enemy identities, spawn, chase, attacks, damage, removal | Enemy container/view presentation |
| `AutoAttackIndicatorController` | Cooldown-indicator visibility and duration | Auto-attack indicator view |

Controllers and UI controllers are plain C# and must not depend on views, Animator, UI components, camera, audio, particles, or other Unity presentation objects. Views own subscriptions and remove them in `OnDestroy`.

Known lifecycle exception: `EntitiesService.Reset()` currently completes without forwarding reset calls to `HeroController` or `EnemiesController`; source behavior is unchanged.

## Gameplay flow

1. `JoystickInputService` emits `OnStateChanged` only when `JoystickState` changes. Input uses the first touch, otherwise mouse input. Drag displacement clamps to `JoystickInputConfig.MaxRadius` screen pixels and becomes a normalized movement vector.
2. `HeroController` reads that state every Update. Active input moves hero at `HeroConfig.MoveSpeed` world units per second and emits `OnStateChanged` with replacement `HeroState`.
3. Inactive input lets hero find nearest enemy strictly inside current weapon range. A confirmed attack calls `EnemiesController.AttackEnemy`, records timing, emits `OnStateChanged`, emits `OnAttackCooldownStarted` with cooldown seconds, then emits `OnAttackPerformed` with target world position.
4. `EnemiesController` spawns while hero is alive, below `EnemiesConfig.MaxEnemies`, and after `SpawnInterval`. It tries up to 8 positions at `SpawnRadius` around hero that are at least `EnemySpacing` horizontal world units from active enemies; unsuccessful attempts skip spawn. It currently selects `Enemies[0]`.
5. Each active enemy chases hero outside its configured attack range, replacing state and emitting `OnEnemyPositionChanged`. Inside range, elapsed cooldown causes `HeroController.TakeHit`, which emits `OnStateChanged` then `OnHeroHit` with damage, remaining health, and lethality; `EnemiesController` then emits `OnEnemyAttackPerformed` with attacking enemy identity.
6. Enemy damage emits `OnEnemyHit` with ID, damage, remaining health, and lethality before state replacement or removal. Lethal hits then emit `OnEnemyRemoved`; the container immediately destroys the matching view.
7. Hero health clamps at zero. Dead hero stops hero attacks/movement and enemy spawning/updates. `GameOverOverlayView` displays the restart action, which clears enemies then calls `HeroController.Restart`.

No projectile, collider, raycast, hitbox, physical contact-point, score, reward, XP, loot, or win-condition path exists in the inspected runtime source.

## Event contracts and presentation

| Producer event | Timing and payload | Presentation consumer |
| --- | --- | --- |
| `ServicesLocator.OnAllServicesInitialized` | All services initialized; no payload | Scene and prefab views resolve services |
| `JoystickInputService.OnStateChanged` | Complete joystick state changes | Joystick view, hero controller, indicator controller |
| `HeroController.OnStateChanged` | Position, health, cooldown timing, or restart state changes | Hero view, game-over overlay |
| `HeroController.OnAttackPerformed` | Confirmed hero strike; target world position | Hero view faces target and triggers attack |
| `HeroController.OnAttackCooldownStarted` | Cooldown starts; duration seconds | Auto-attack indicator controller |
| `EnemiesController.OnEnemySpawned` | Enemy added; initial `EnemyState` | Enemy container instantiates prefab |
| `EnemiesController.OnEnemyPositionChanged` | Chase movement; replacement `EnemyState` | Enemy view position and move animation |
| `EnemiesController.OnEnemyHit` | Before removal/replacement; `EnemyHitResult` | Nonlethal Bee damage animation and flash |
| `EnemiesController.OnEnemyAttackPerformed` | After hero damage; enemy ID | Enemy view attack animation acknowledgement |
| `EnemiesController.OnEnemyRemoved` | Enemy removed; enemy ID | Enemy container destroys view |
| `WeaponsService.OnWeaponChanged` | Successful weapon selection; `WeaponConfig` | Hero view replaces weapon prefab |
| `AutoAttackIndicatorController.OnStateChanged` | Complete indicator state replacement | Indicator view starts or hides fill |

`HeroView` owns transform, rotation, hero Animator, and instantiated weapon presentation. `EnemyView` owns facing, Bee `IsMoving`, `Attack`, and nonlethal `Damage` presentation. Hero damage/death and Bee death animation states have no current runtime driver.

## Configuration and content selection

- `ScriptableObjectSingleton<T>` lazily loads a Resources asset named after its concrete type and logs an error if absent.
- `HeroConfig` supplies prefab, initial health, and movement speed.
- `EnemiesConfig` supplies spawn interval/radius/cap, minimum horizontal enemy spacing, and enemy catalog. Runtime currently selects catalog index zero.
- `WeaponConfig` supplies ID, damage, range, cooldown, and weapon view prefab. `WeaponsService` starts with catalog index zero; `SwitchWeapon` uses `WeaponsConfig.GetWeaponById`.
- `WorldConfig` and `BiomeConfig` supply prefabs instantiated by their owning service/container.

Configuration assets may contain entries not selected by the current startup or spawn paths. Catalog membership alone does not prove runtime use.

## Documentation maintenance

Update this page with affected static facts only. Keep performance evidence, historical assessment, architectural intent, and proposals separate from current-source claims. Archived initial assessment lives under `docs/analysis/initial_assessment/` and is not maintained as current documentation.
