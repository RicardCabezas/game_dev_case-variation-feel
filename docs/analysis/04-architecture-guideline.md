# Architecture Guideline

> AI-assisted guideline for future agents working in this repository.
> Based on the existing architecture as observed in
> `docs/analysis/01-codebase-map.md`. Preserve current boundaries unless a
> task explicitly requests an architectural change.

## Architecture at a Glance

```text
MainScene
  ServicesLocator
  UI views
  World is service-instantiated

ServicesLocator
  -> IService implementations
       JoystickInputService
       WeaponsService
       WorldService
       EntitiesService
            -> HeroController
            -> EnemiesController

Controllers/services own state and decisions
  -> typed Action<T> events
  -> MonoBehaviour views mirror state and present prefabs
```

Primary source locations:

- Composition/lifecycle: `Assets/Core/ServicesManager/Scripts/ServicesLocator.cs`, `IService.cs`.
- Entity state/decisions: `Assets/Features/Entities/Scripts/Controllers`, `Models/RuntimeState`, `EntitiesService.cs`.
- Input: `Assets/Features/JoystickInput/Scripts`.
- Weapons/content: `Assets/Features/Weapons/Scripts`.
- World composition: `Assets/Features/World/Scripts`, `Assets/Features/World/View/Local/World.prefab`.
- UI presentation: `Assets/Features/UI/Scripts/View`.

## Ownership Rules

### Gameplay state

Put authoritative state and rules in the existing plain C# services/controllers:

- `HeroController`: hero position, health, death predicate, restart, movement/attack mode, target selection, attack cooldown.
- `EnemiesController`: enemy collection, IDs, spawn cadence/position, chase movement, attack cadence, damage, removal.
- `WeaponsService`: current weapon and weapon switching.
- `JoystickInputService`: current input state and touch/mouse polling.
- `WorldService`: instantiated world lifetime and `WorldView` access.

Views can cache presentation-only values, but must not become a second owner of health, enemy existence, cooldowns, or progression state.

### Presentation

Use `MonoBehaviour` views for Unity-facing work:

- `HeroView`: hero transform/Animator/rotation and current weapon visual.
- `EnemyView`: enemy-facing rotation and future enemy-local presentation.
- `EnemiesContainerView`: enemy ID-to-view mapping and prefab lifetime.
- `HeroContainerView` / `BiomeContainerView`: configured prefab composition.
- `GameOverOverlayView` / `JoystickView`: UI state presentation.

Keep visual consumers subscribed to events instead of making controllers reference Animator, UI, audio, camera, or particle components.

## Adding a New Service

Use this sequence when a task genuinely needs a new long-lived service:

1. Add it under the owning feature and implement `IService`.
2. Return required service types from `GetDependencies()`.
3. Allocate runtime state and start UniTask loops in `Initialize()`.
4. Expose state through properties and typed events.
5. Cancel and dispose loop tokens in `Reset()`.
6. Resolve it with `ServicesLocator.Instance.GetService<T>()` from consumers after `OnAllServicesInitialized`.

`ServicesLocator` discovers concrete services by reflection and initializes them in dependency order. Do not add scene references solely to register a service.

## Adding or Extending Gameplay State

Follow current value-state shape:

- Add a get-only property to the relevant `*State` struct.
- Replace the complete state value when a controller changes it.
- Emit the owning controller's typed event at the state transition.
- Keep event payload sufficient for the consumer; existing payloads use `HeroState`, `EnemyState`, `JoystickState`, `WeaponConfig`, or enemy ID.

Current event boundaries:

| Need | Existing boundary | Limitation |
|---|---|---|
| Hero health/death/restart | `HeroController.OnStateChanged` | Also emitted for movement; no dedicated attack event. |
| Incoming hero hit | `HeroController.TakeHit(int)` | Has damage and old/new health locally; public state event has final state. |
| Enemy lethal death | `EnemiesController.OnEnemyRemoved` | Receives ID only; view is currently destroyed immediately. |
| Enemy spawn | `EnemiesController.OnEnemySpawned` | Receives complete initial `EnemyState`. |
| Enemy movement | `EnemiesController.OnEnemyPositionChanged` | Emitted for chase movement, not attack/damage. |
| Weapon visual replacement | `WeaponsService.OnWeaponChanged` | No current scene caller switches weapon. |

If a required transition has no event, first identify the authoritative method that already performs it. Do not poll state from a view every frame merely to infer a transition.

## Adding Feedback

Attach feedback at the narrowest existing boundary that already has the needed information:

- Player hurt/death: `HeroController.TakeHit` or `OnStateChanged`.
- Enemy hit: `EnemiesController.AttackEnemy`.
- Enemy death: `OnEnemyRemoved`, coordinated with the existing `EnemiesContainerView` identity map.
- Enemy spawn: `OnEnemySpawned`.
- Movement/input response: `OnStateChanged` from `JoystickInputService` or `OnEnemyPositionChanged`.
- Camera target: `HeroContainerView` / `WorldView.Camera`.

Do not assume contact coordinates exist: hero attacks use nearest-enemy search plus `Vector3.Distance` and weapon range; there is no collider, raycast, projectile, or hitbox data path. Do not assume animation is already synchronized: current code drives hero `Speed` only, while attack/damage/death Animator states are present but not code-triggered.

A feedback presenter should follow the existing view pattern:

```text
Start
  -> subscribe to ServicesLocator.OnAllServicesInitialized
  -> resolve service/controller
  -> subscribe to typed event(s)
  -> synchronize initial CurrentState where applicable

OnDestroy
  -> unsubscribe from locator and gameplay events
  -> remove button listeners
  -> destroy runtime-owned presentation objects
```

## Configuration and Prefabs

- Use private serialized fields with public read-only properties for tunables and asset references.
- Use an existing `ScriptableObjectSingleton<T>` config when the value is global and already belongs to a config domain.
- Keep reusable enemy/weapon definitions as separate `EnemyConfig`/`WeaponConfig` assets; aggregate lists belong in `EnemiesConfig`/`WeaponsConfig`.
- Resolve singleton configs through their `Resources` asset names, matching the existing `Resources.Load<T>(typeof(T).Name)` convention.
- Let container views instantiate configured prefabs and set parent/local/world transforms. Retain a map when runtime identity differs from the view object, as `EnemiesContainerView` does.
- When adding a component reference to a prefab view, serialize it on that view and keep the component's responsibility local. `HeroView` uses serialized `Animator` and `weaponSlot`; `WorldView` uses serialized Cinemachine camera.

Verify list-selection behavior before using a new config entry. The repository contains multiple enemy and weapon assets, but current startup/spawn code selects index zero in the active path.

## Assembly and Naming Boundaries

- Place code in the feature assembly that already owns the referenced types.
- Preserve namespaces matching the feature (`Game.GamePlay.Heroes`, `Game.GamePlay.Enemies`, `Game.Weapons`, `Game.World`, `Game.UI`, `Game.JoystickInput`).
- Use suffixes already established: `*Service`, `*Controller`, `*View`, `*Config`, `*State`.
- Use `On...` for events and `Current...` for current service/controller state.
- Avoid introducing a second locator, global static gameplay state, or direct scene searches; no such pattern is used by current feature code.

## Lifecycle and Async Rules

- Long-lived services are created by `ServicesLocator`, not by `new` in views.
- Controllers start their UniTask update loops during service initialization and retain cancellation sources.
- `Reset()` must stop loop work and dispose owned cancellation sources.
- View event subscriptions must be removable even when service initialization has not completed; follow the null guards used by existing views.
- Runtime-instantiated world objects are marked `DontDestroyOnLoad` by `WorldService`; do not add another world lifetime owner.
- Runtime-spawned enemy/weapon objects are owned by their container/view and destroyed there.

## Change Checklist for Future Agents

Before editing:

- Identify which service/controller owns the state transition.
- Check existing events and their payloads before adding polling or direct view coupling.
- Check the feature `.asmdef` and namespace of every referenced type.
- Check whether the required prefab/config already exists.

While editing:

- Keep gameplay decisions in controllers/services and Unity presentation in views.
- Follow existing typed event and cleanup patterns.
- Keep values configurable through the nearest existing config/view serialization pattern.
- Avoid changing scene/prefab/config assets unless the task explicitly requires it.

After editing:

- Verify initialization and reset paths, including cancellation and unsubscription.
- Verify runtime-created objects have one clear owner.
- Verify event payloads are sufficient and emitted exactly at the intended transition.
- Re-check current enemy/weapon list selection instead of assuming every configured asset is active.
- Profile only the changed execution path; existing hot paths are documented in `docs/analysis/02-performance-audit.md`.

## Known Exceptions and Non-Conventions

These observations should not be generalized without confirmation:

- No-dependency services return either `null` (`WeaponsService`, `WorldService`) or `Array.Empty<Type>()` (`JoystickInputService`).
- Some content Animator states exist without runtime drivers: hero attack/damage/death and bee movement/attack/damage/death are present in controller assets, but current scripts do not drive them.
- `EntitiesService.Reset()` returns a default `UniTask` and does not directly reset its controllers; controller cancellation is therefore not reached through `EntitiesService.Reset()` as currently written.
- Config assets contain more entries than the active index-zero selection paths use.
