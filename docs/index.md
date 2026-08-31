# Game Feel Home Assignment Documentation

## Project summary

This Unity 2022.3 project is a small arena game: touch or mouse input drives a virtual joystick; releasing input lets the hero automatically attack the nearest enemy in weapon range. Plain C# services and controllers own gameplay and UI decisions. `MonoBehaviour` views subscribe to typed events and perform Unity presentation work.

## Start here

- [Current codebase map](codebase-map.md) — maintained verified static source map, runtime flow, ownership, and event contracts.
- [Initial assessment archive](analysis/initial_assessment/01-codebase-map.md) — original codebase map, performance audit, architecture guideline, opportunities, and prioritization; historical only.
- [API reference](annotated.html) — generated public C# API, source links, search, and Graphviz relationship diagrams.

## Gameplay and service flow

`ServicesLocator` discovers `IService` implementations, initializes declared dependencies, then raises `OnAllServicesInitialized`. `EntitiesService` composes `HeroController` and `EnemiesController`; `JoystickInputService` owns input; `WeaponsService` owns equipped weapon; `WorldService` owns world lifetime.

While joystick input is active, `HeroController` moves. When input becomes inactive, it finds nearest enemy inside weapon range, applies damage through `EnemiesController`, starts weapon cooldown, and emits an attack event for presentation. Enemies spawn around hero, chase until attack range, then damage hero on their own cooldown. Views mirror these transitions through typed events.

## Evidence status

Source code is authoritative for current static behavior and public contracts. The current codebase map records verified source facts. Initial-assessment pages are preserved historical analysis; performance numbers are capture-specific measurements, and proposals are not evidence that behavior exists.
