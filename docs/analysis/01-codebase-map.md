# Codebase Map

> AI-assisted static analysis performed before implementation.
> This document maps the existing project; it does not represent the
> candidate's final design decisions.

## Executive Summary

- Single-scene, top-down/third-person arena loop: move with a virtual joystick; release it to auto-attack the nearest enemy in range.
- `HeroController` and `EnemiesController` own gameplay state as plain C# controllers; views mirror that state.
- Enemies spawn every 2 seconds around the hero, up to 20, then chase and periodically damage the hero at attack range.
- Hero attacks are immediate controller calls. There is no attack animation trigger, projectile, collider, raycast, or hitbox path.
- Enemy damage is stored in `EnemyState`; lethal damage removes the enemy and destroys its view immediately.
- Hero damage emits `HeroController.OnStateChanged`; health reaching zero makes gameplay loops skip movement/spawn/attack work and shows the game-over overlay.
- There is no win condition, score, XP, loot, currency, or other reward/progression system in the repository.
- Existing visual feedback is limited to hero movement animation, model rotation, spawned weapon/enemy visuals, joystick visuals, and game-over UI.
- `ServicesLocator` discovers `IService` implementations by reflection, orders them by declared dependencies, then broadcasts initialization to scene/prefab views.
- Main likely feel extension seams are state/events at `TakeHit`, enemy removal, spawn, and service initialization; nonlethal enemy damage and hero attack currently have no public event.

## Core Gameplay Flow

Runtime composition:

```text
MainScene
  ServicesManager (ServicesLocator)
  UI (GameOverOverlayView + Joystick prefab)
  Main Camera

WorldService -> instantiate World prefab (DontDestroyOnLoad)
  WorldView + BiomeContainerView + HeroContainerView
  -> Dungeon prefab
  -> Hero prefab (Alice) + camera Follow target
  -> EnemiesContainerView
```

Service startup is `ServicesLocator.Awake` -> reflection discovery -> dependency ordering -> each `IService.Initialize()` -> `OnAllServicesInitialized`. `EntitiesService` creates `HeroController`, initializes it, then creates/initializes `EnemiesController` (`Assets/Features/Entities/Scripts/EntitiesService.cs:18-29`).

Repeated player loop:

```text
Touch/mouse polling
  -> JoystickInputService.HandleInput()
  -> CurrentState + OnStateChanged
  -> HeroController.UpdateLoop()
       active joystick: UpdatePosition()
         -> replace HeroState(position, health, lastAttack)
         -> HeroController.OnStateChanged
       inactive joystick: AttackClosestEnemy()
         -> closest enemy search by Vector3.Distance and weapon range
         -> EnemiesController.AttackEnemy(enemy, weapon damage)
         -> update hero LastAttackTime (no state event)
```

Enemy loop:

```text
EnemiesController.SpawnLoop()
  every EnemiesConfig.SpawnInterval (2s)
  -> choose random point at EnemiesConfig.SpawnRadius (10)
  -> currently always choose Enemies[0]
  -> add EnemyState + OnEnemySpawned
  -> EnemiesContainerView Instantiate(prefab)

EnemiesController.UpdateLoop() every Update
  -> copy enemy IDs
  -> per enemy: distance to hero
       outside AttackRange: move toward hero + OnEnemyPositionChanged
       inside AttackRange and cooldown elapsed: HeroController.TakeHit()
```

Combat/death flow:

```text
HeroController.AttackClosestEnemy
  -> EnemiesController.AttackEnemy
  -> subtract damage from EnemyState
       health > 0: replace dictionary entry; no event
       health <= 0: RemoveEnemy
         -> OnEnemyRemoved(id)
         -> EnemiesContainerView Destroy(enemyView.gameObject)

EnemiesController.UpdateEnemy
  -> HeroController.TakeHit(damage)
  -> clamp health to zero
  -> HeroController.OnStateChanged
       alive: views receive health state, but no health bar exists
       dead: GameOverOverlayView active; JoystickView hides joystick;
             enemy and hero loops skip gameplay work on later iterations
```

Relevant implementation: `HeroController.cs:42-55,71-127`; `EnemiesController.cs:58-84,86-170`; `EnemyState.cs`; `HeroState.cs`.

Progression/success: static evidence shows only failure/restart. `GameOverOverlayView.OnRestartButtonClicked()` clears all enemies, then calls `HeroController.Restart()` (`Assets/Features/UI/Scripts/View/GameOverOverlayView.cs:44-48`). No success state or reward callback exists.

## Important Systems

### `ServicesLocator` / service lifecycle

- Responsibility: discover concrete `IService` types across loaded assemblies, instantiate them, topologically order declared dependencies, initialize them, expose typed lookup, and reset in reverse order.
- Dependencies: `IService` (`Initialize`, `GetDependencies`, `Reset`) and UniTask. Scene object `ServicesManager` hosts it (`Assets/Scenes/MainScene.unity:261-276`).
- Communication: `OnAllServicesInitialized` is an event with late-subscriber invocation when already initialized. Scene/prefab views subscribe in `Start` and unsubscribe in `OnDestroy`.
- Relevant files: `Assets/Core/ServicesManager/Scripts/ServicesLocator.cs`, `IService.cs`.

### Input: `JoystickInputService` / `JoystickView`

- `JoystickInputService` polls the first touch, otherwise mouse button state, in a UniTask `UpdateLoop`. Press creates an active center; drag clamps delta to `JoystickInputConfig.MaxRadius` (100 px); release returns `JoystickState.Inactive`.
- `OnStateChanged` is emitted only when `JoystickState.Equals` differs. `HeroController` treats active input as movement mode and inactive input as attack mode.
- `JoystickView` renders the outer stick at the recorded screen position and offsets the inner stick by normalized movement. Hero death hides the outer stick.
- Files: `Assets/Features/JoystickInput/Scripts/JoystickInputService.cs`, `Models/RuntimeState/JoystickState.cs`, `Config/JoystickInputConfig.cs`; `Assets/Features/UI/Scripts/View/JoystickView.cs`.

### Hero gameplay: `HeroController` / `HeroView`

- `HeroController` owns `HeroState` (position, health, last attack time), movement speed/cooldown decisions, target selection, damage intake, death predicate, and restart state.
- Dependencies are injected from `EntitiesService`: `EnemiesController`, `JoystickInputService`, `WeaponsService`.
- `HeroView` mirrors position from `OnStateChanged`, rotates toward movement in `Update`, sets only Animator `Speed`, and instantiates the current weapon under serialized `weaponSlot`.
- Files: `Assets/Features/Entities/Scripts/Controllers/HeroController.cs`, `Models/RuntimeState/HeroState.cs`, `View/Heroes/HeroView.cs`, `View/Heroes/HeroContainerView.cs`.

### Enemy gameplay: `EnemiesController` / views

- `EnemiesController` owns the `Dictionary<int, EnemyState>`, IDs, spawn cadence/position, chase movement, attack cadence, and enemy damage/removal.
- `EnemyState` contains ID, position, health, `EnemyConfig`, and last attack time. It is replaced as an immutable-style struct value; there is no enemy health event.
- `EnemiesContainerView` maps IDs to instantiated `EnemyView` instances and responds to spawn/remove/position events.
- `EnemyView` independently reads the hero controller every frame and rotates toward the hero. It does not read enemy health, receive damage callbacks, or drive its Animator.
- Files: `Assets/Features/Entities/Scripts/Controllers/EnemiesController.cs`, `Models/RuntimeState/EnemyState.cs`, `View/Enemies/EnemiesContainerView.cs`, `View/Enemies/EnemyView.cs`.

### Configuration and weapons

- `ScriptableObjectSingleton<T>` resolves assets by concrete type name from `Resources`; assets exist for hero, enemies, weapons, world, biome, and joystick configuration.
- `HeroConfig`: Alice prefab, move speed 5, initial health 100.
- `EnemiesConfig`: interval 2, radius 10, cap 20, three enemy config assets. Current runtime spawn code selects only `Enemies[0]` (`BeeNormal`: 50 HP, speed 10, 3s attack cooldown, 10 damage, range 2).
- `WeaponConfig` assets: GreatSword 30 damage / range 3 / cooldown 1.5; CurvedSword 15 / 2 / 0.5; LongSword 50 / 5 / 2. `WeaponsService` starts with `Weapons[0]` and exposes `SwitchWeapon`, but no scene UI calls it.
- Files: `Assets/Core/ScriptableObjectSingleton/Scripts/ScriptableObjectSingleton.cs`; `Assets/Resources/*.asset`; feature `Config` and `WeaponsService` files.

### World and UI

- `WorldService` instantiates `WorldConfig.WorldPrefab` and marks it `DontDestroyOnLoad`. The world prefab contains `WorldView`, `BiomeContainerView`, `HeroContainerView`, a Cinemachine virtual camera, and `EnemiesContainerView`.
- `HeroContainerView` instantiates `HeroConfig.HeroPrefab` and assigns its transform to `WorldView.Camera.Follow`. `BiomeContainerView` instantiates `BiomeConfig.DefaultBiomePrefab`.
- `GameOverOverlayView` starts hidden, listens to hero state, toggles visibility on `IsDead`, and wires the restart `Button` listener in code. Main scene also contains EventSystem and Canvas UI.
- Files: `Assets/Features/World/Scripts/WorldService.cs`, `WorldView.cs`; `Assets/Features/Biomes/Scripts/View/BiomeContainerView.cs`; `Assets/Features/UI/Scripts/View/GameOverOverlayView.cs`; `Assets/Scenes/MainScene.unity`; `Assets/Features/World/View/Local/World.prefab`.

## Existing Feedback Architecture

### Animation and model presentation

- Hero prefab `Alice` has an Animator and `HeroAnimationController`. The controller declares only `Speed`; `HeroView.OnJoystickStateChanged()` sets it to movement magnitude or zero. Idle/run transitions are therefore the only code-driven hero animation path.
- Hero controller asset also contains `damage`, `death`, and `Weak` states backed by `HeroDamage.anim` and `HeroDie.anim`, but no parameters/triggers or code calls target those states. `HeroView.OnHeroStateChanged()` only sets transform position, including on damage/death.
- Bee prefab has an Animator and `BeeAnimationController` with Idle, Move, Attack, Damage, and Die states, but no Animator parameters. `EnemyView` never references the Animator. Static evidence therefore shows these states are not driven by current gameplay code; whether that is legacy content or intentionally unfinished wiring needs runtime inspection.
- Hero/enemy rotation is visual feedback: `HeroView.Update()` rotates toward movement; `EnemyView.Update()` rotates toward hero.

### VFX, particles, audio, camera, damage, health, death, rewards, haptics

- No project audio clips, `AudioSource`, particle/VFX assets, trail components, haptic/vibration calls, damage numbers, health bars, screen shake, or camera impulse hooks were found.
- Camera feedback currently consists of Cinemachine follow/tracking: `HeroContainerView` assigns `World.Camera.Follow`; the world prefab has a virtual camera with Framing Transposer settings. No combat-driven camera event is present.
- Damage feedback is limited to `Debug.Log` in `HeroController.TakeHit()` and `EnemiesController.AttackEnemy()` plus the model animations/assets noted above; no view reaction is triggered.
- Enemy death feedback is immediate removal: `OnEnemyRemoved` -> `Destroy`. Hero death feedback is immediate overlay activation plus joystick hiding. No reward/progression feedback exists.

## Game-Feel Extension Points

| Hook | File/class and available information | Useful feedback boundary |
|---|---|---|
| `HeroController.OnStateChanged` | `HeroController.cs:26`; receives `HeroState` with position, health, `IsDead`; emitted by `TakeHit`, movement, and `Restart` | Health/death UI, hero state reaction, restart presentation. Note: attack timestamp changes do not emit it. |
| `HeroController.TakeHit(int damage)` | `HeroController.cs:42-55`; has incoming damage, old/new health, final death state at one narrow transition | Player hurt response and death response without coupling to enemy iteration. Existing UI already consumes resulting event. |
| `EnemiesController.AttackEnemy(EnemyState enemyState, int damage)` | `EnemiesController.cs:66-84`; has target ID/config, incoming damage, old health, lethal/nonlethal branch | Hit feedback at the authoritative damage boundary. Nonlethal damage currently has no event; `OnEnemyRemoved` covers only lethal outcomes. |
| `EnemiesController.OnEnemyRemoved` | `EnemiesController.cs:16,58-64`; receives enemy ID; container has ID-to-view mapping and destroys view | Death VFX/audio/reaction can follow the same identity boundary as visual removal. Current destruction is immediate. |
| `EnemiesController.OnEnemySpawned` | `EnemiesController.cs:15,99-112`; receives full `EnemyState`, including config/prefab and spawn position | Spawn presentation at creation; container already owns instantiation. |
| `EnemiesController.OnEnemyPositionChanged` | `EnemiesController.cs:17,146-159`; receives ID, position, health, config | Position-aware presentation; emitted for chase movement only, not attacks. |
| `JoystickInputService.OnStateChanged` | `JoystickInputService.cs:11,84-90`; receives active/inactive, center, normalized movement | Input/aim/anticipation feedback. `JoystickView` is the existing subscriber pattern. |
| `WeaponsService.OnWeaponChanged` | `WeaponsService.cs:13,31-40`; receives selected `WeaponConfig` | Weapon presentation; `HeroView` destroys/reinstantiates the weapon prefab here. No current caller switches weapons. |
| `ServicesLocator.OnAllServicesInitialized` | `ServicesLocator.cs:18-26,64-65`; lifecycle boundary after all service dependencies initialize | Safe setup point for independent feedback presenters that need controller references. |

Important absence: there is no public attack-start event, no nonlethal enemy-health event, and no hit position/contact data. The current attack path provides target state and range-based selection, not collision/contact geometry.

## Project Conventions Worth Following

- Feature-oriented folders and assembly definitions: `Core`, `Features/Entities`, `Weapons`, `World`, `Biomes`, `UI`, and `JoystickInput` each separate runtime concerns. This is supported by the matching `.asmdef` files and namespaces.
- Plain controller/service ownership for gameplay; `MonoBehaviour` classes are primarily views/containers. Controllers use injected references, while views acquire services through `ServicesLocator` after the initialization event.
- Tunables/content are serialized in ScriptableObjects with private fields and public read-only properties. Runtime config lookup uses `Resources` assets named after singleton types; content lists use separate `EnemyConfig`/`WeaponConfig` assets.
- State is exposed through immutable-style structs with get-only properties (`HeroState`, `EnemyState`, `JoystickState`) and replaced as a whole when changed.
- State-to-view communication uses `Action<T>` events. Subscribers explicitly unsubscribe in `OnDestroy`; button listeners are added and removed in the same view lifecycle.
- Runtime view composition is prefab-based: containers instantiate configured prefabs, then set parent/transform references. Weapon visuals are child-instantiated under a serialized slot.
- Async service/controller loops use UniTask plus `CancellationTokenSource`; services cancel/dispose tokens in `Reset`, and `ServicesLocator` resets services in reverse initialization order.
- Small inconsistency, not a global rule: services with no dependencies return either `null` (`WeaponsService`, `WorldService`) or `Array.Empty<Type>()` (`JoystickInputService`).
- Isolated wiring gaps visible in content: three enemy configs and three weapon configs exist, but spawn/initialization use list index zero; weapon switching exists as API but has no scene caller.

## Performance-Sensitive Areas

Hand-off only; this is not a full performance audit.

- `JoystickInputService.UpdateLoop` polls Unity input every frame and may emit state events while dragging (`Assets/Features/JoystickInput/Scripts/JoystickInputService.cs:33-90`).
- `HeroController.UpdateLoop` runs every frame. When attack mode is active and cooldown permits, `TryFindClosestEnemy` scans all enemy values and calls `Vector3.Distance` (`HeroController.cs:71-127`).
- `EnemiesController.UpdateLoop` runs every frame and allocates `new List<int>(_enemies.Keys)` before iterating every enemy. Each moving enemy performs distance/normalization, constructs a new state value, and emits a position event (`EnemiesController.cs:123-159`).
- Each spawned `EnemyView` performs per-frame hero lookup/normalization/`Quaternion.Slerp`; worst case follows the configured 20-enemy cap (`EnemyView.cs:24-35`). `HeroView.Update` also runs every frame.
- Runtime `Instantiate`/`Destroy` occurs on enemy spawn/death/restart and weapon switches: `EnemiesContainerView.cs:28-39`, `HeroView.cs:98-116`.
- Combat logs execute on hero hits and enemy hits/deaths (`HeroController.cs:47`, `EnemiesController.cs:72,76`); measure their impact in the intended build/profile configuration.
- No repeated physics queries are present in the gameplay path; range checks are dictionary iteration plus `Vector3.Distance`.

## Things Worth Inspecting Manually

1. Confirm actual runtime Animator state changes for hero/enemy attack, damage, and death; static wiring shows no usable code-driven parameters for them.
2. Verify whether the intended combat feel is deliberately “release joystick = attack”; there is no separate attack input or attack event.
3. Confirm enemy variety expectations: `EnemiesConfig` contains three assets, but `SpawnEnemy()` always selects the first.
4. Confirm weapon switching is intentionally dormant; `WeaponsService.SwitchWeapon()` exists but no scene/UI caller was found.
5. Observe whether enemy views are destroyed before any visible death reaction can play; `OnEnemyRemoved` immediately calls `Destroy`.
6. Check whether movement can leave the dungeon/play area; no bounds or navigation query appears in controller code, and scene NavMesh data is empty.
7. Measure event volume while 20 enemies chase: position events update views every controller frame, while each enemy also rotates independently every frame.
8. Verify restart timing around spawn-loop cadence and stale joystick state; restart clears enemies and resets hero state, but input service state is not reset by `HeroController.Restart()`.
