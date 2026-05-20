# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a 2D roguelike game built in Unity, using the **Corgi Engine** (MoreMountains) as the platformer/character physics foundation. The Unity project lives in `RogueLikeProject/`.

## Unity Build & Testing

Open and build through the Unity Editor directly — there is no CLI build script in this repo. The Unity version can be found in `RogueLikeProject/ProjectSettings/ProjectSettings.asset`.

- **Running tests**: Unity Test Framework (`com.unity.test-framework`) is included. Run via *Window > General > Test Runner* in the Editor.
- **Script compilation errors**: After editing scripts, check `read_console` (via UnityMCP) or the Unity Console for compilation errors before continuing.
- **Play mode**: Open any non-CommonScene scene in the Editor and press Play. The game should not be started from `CommonScene` directly (see Scene Architecture below).

## Key Dependencies

Managed via `RogueLikeProject/Packages/manifest.json`:

| Package | Purpose |
|---|---|
| **Corgi Engine** (`MoreMountains.CorgiEngine`) | 2D platformer character/physics engine |
| **UniTask** (`com.cysharp.unitask`) | Async/await without allocations (`UniTask` instead of `Task`) |
| **DOTween** (`Demigiant`) | Tween animations (fade, scroll, etc.) |
| **Odin Inspector** (`Sirenix`) | Enhanced Unity Inspector attributes |
| **Unity Input System** (`com.unity.inputsystem`) | New Input System for keyboard/gamepad |
| **URP** (`com.unity.render-pipelines.universal`) | Universal Render Pipeline |
| **Cinemachine** (`com.unity.cinemachine`) | Camera system |
| **2D Tilemap + Extras** | Tilemap-based level layout |
| **UnityMCP** (`com.coplaydev.unity-mcp`) | MCP bridge for AI tooling |

## Architecture

### Scene Structure

The game uses an **additive multi-scene** pattern:

- `CommonScene` — always loaded additively on startup. Hosts persistent singletons (`InputManager`, `SceneTransitionManager`, `ObjectPoolManager`). **Never start the game from CommonScene** — `CommonSceneChecker` will assert if CommonScene is loaded twice.
- `TitleScene`, `MainScene`, `ResultScene` — gameplay scenes, loaded/unloaded by `SceneTransitionManager`.
- `MinimalLevel` — a minimal test scene used during development.

`CommonSceneBootstrap` (with `RuntimeInitializeOnLoadMethod` currently commented out) is the intended mechanism to auto-load CommonScene from any starting scene.

### Singleton Pattern (`Assets/Scripts/Utility/Core/Singleton.cs`)

Two base classes:
- `SingletonMonoBehaviour<T>` — for MonoBehaviour singletons. **Always call `base.Awake()` and `base.OnDestroy()` when overriding.** Asserts on duplicate instances.
- `Singleton<T>` — for plain C# singletons. Requires explicit `CreateInstance()` / `DeleteInstance()` calls.

In Editor, `SingletonInitializer` resets all `Instance` properties via reflection on domain reload (targets assemblies `GMLib.Runtime` and `Assembly-CSharp-firstpass`).

### Input System (`Assets/Scripts/Manager/InputManager.cs`)

Custom wrapper over Unity's New Input System (`InputSystem_Actions`). Tracks four direction buttons (Up/Down/Left/Right mapped from left stick/WASD), plus stick vectors. Provides four query modes per button:
- `IsTrig(btn)` — pressed this frame
- `IsPress(btn)` — held down
- `IsRelease(btn)` — released this frame
- `IsRepeat(btn)` — configurable repeat rate (first repeat: `_repeatFirstSec`, subsequent: `_repeatAfterSec`)

Button state is stored as bitmasks using `BitUtil`. Detects current device type (`KeyboardMouse` vs `Gamepad`).

### Object Pool (`Assets/Scripts/Utility/Pool/`)

- `ObjectPoolManager` (singleton) — registers pools per prefab, wraps Unity's `ObjectPool<GameObject>`.
- `IPoolable` interface — `OnCreate(prefab)`, `OnSpawn()`, `OnDespawn()`.
- `PoolObjectBase` — abstract MonoBehaviour implementing `IPoolable`. Extend this for pooled GameObjects. To return an object to the pool: `_objectPoolManager?.Release(_prefab, this.gameObject)`.

### Scene Transitions (`Assets/Scripts/Manager/SceneTransitionManager.cs`)

Async scene switching with DOTween fade via `SceneChangeFade`. Transitions disable input during fade, unload the old scene (additive mode), load the new one, then re-enable input. Uses `UniTask` for async control flow.

### Corgi Engine Integration (`Assets/CorgiEngine/`)

The Corgi Engine provides:
- `CorgiController` — physics/collision for 2D characters.
- `Character` + `CharacterAbility` subclasses — modular ability system (jump, run, dash, wall-cling, etc.).
- `LevelManager` — manages player spawn, death, respawn.
- `TilemapLevelGenerator` — procedural tilemap level generation extending `MMTilemapGenerator`.
- AI system — `AIDecision` + `AIAction` components assembled into behaviour trees.

Corgi Engine code lives under `Assets/CorgiEngine/` in its own asmdef (`MoreMountains.CorgiEngine`). Do not modify Corgi Engine files unless necessary; extend via subclassing.

### Namespaces

| Namespace | Location |
|---|---|
| `Manager` | `Assets/Scripts/Manager/` |
| `SceneSystem` | `Assets/Scripts/SceneSystem/` |
| `Utility` / `Utility.Core` / `Utility.Pool` | `Assets/Scripts/Utility/` |
| `GameObj` | `Assets/Scripts/GameObj/` |
| `System` (project-level) | `Assets/Scripts/System/` |
| `MoreMountains.CorgiEngine` | `Assets/CorgiEngine/` |

### UI Utilities

- `AutoScroller` — scroll a `ScrollRect` to keep a selected element visible; supports vertical list and grid layouts.
- `ElementSelector` — grid/list cursor navigation driven by `InputManager` repeat input.
- `AspectRatio` — maintains 16:9 letterbox/pillarbox via camera viewport rect.
