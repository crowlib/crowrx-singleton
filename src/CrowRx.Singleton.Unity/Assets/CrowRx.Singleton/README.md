# CrowRx.Singleton

A singleton utility library for pure C# objects and Unity components. `IInstance` is a marker interface; it does not declare lifecycle methods.

## Key Features

- **Generic Singleton**: Provides a concise way to implement the singleton pattern for standard C# classes.
- **Native & MonoBehaviour Support**: Supports not only Plain Old CLR Objects (POCO) but also `MonoBehaviour`-based scene objects and persistent `DontDestroyOnLoad` objects.
- **Lifecycle Hooks**: Override `OnInit()` and `OnRelease()` for managed singletons, and `OnStart()` for scene components. Use `ReleaseInstance()` for explicit cleanup.
- **Existing Components**: `ComponentContainer<TComponent>` caches a reference without creating or managing the component.
- **Static Cleanup**: Unity singleton fields use `[AutoStaticsCleanup]`. This does not provide automatic cleanup for `Native<T>`.

## Requirements

- **Unity**: 6000.3 or newer, as declared in `package.json`
- **.NET Standard**: 2.1 compatible environment
- **Dependencies**:
    - [R3](https://github.com/Cysharp/R3) (`com.cysharp.r3`)
    - [CrowRx](https://github.com/newhory/crowrx) (`com.crowlib.crowrx`)

## Installation

### Unity Package Manager (Git URL)

`https://github.com/newhory/crowrx-singleton.git?path=src/CrowRx.Singleton.Unity/Assets/CrowRx.Singleton`

## Usage

### 1. Plain C# Class Singleton (Native)

Use this for pure C# objects that do not inherit from `MonoBehaviour`.

```csharp
using CrowRx.Singleton;

public sealed class MyManager : Native<MyManager>
{
    private MyManager() { }

    protected override void OnInit()
    {
        // Automatically called when the instance is first created
    }

    protected override void OnRelease()
    {
        // Release owned resources
    }
}

var manager = MyManager.Instance;
MyManager.ReleaseInstance();
```

`Native<T>` creates the object through a parameterless constructor and publishes it after `OnInit()` succeeds. Creation and release share a lock per type; access to a published instance uses `Volatile.Read`. Coordinate all concurrent use before release. Do not wait inside lifecycle hooks for another thread that needs the same singleton. Cleanup is explicit,
including when leaving Unity Play Mode.

### 2. Scene-based MonoBehaviour Singleton (SceneObject)

Use this for `MonoBehaviour` singletons that are dependent on a specific scene.

```csharp
using CrowRx.Singleton;

public class PlayerController : SceneObject<PlayerController>
{
    protected override void OnInit()
    {
        // Initialization logic
    }

    protected override void OnRelease()
    {
        // Release logic
    }

    protected override void OnStart()
    {
        // Start logic for the initialized, active instance
    }
}
```

Place the component in a scene. Initialization runs in `Awake()` or on the first `Instance` access, and the reference is published only after initialization succeeds. Inactive objects can also be initialized through `Instance`.

When searching, a single result is selected regardless of active state. With multiple results, the first `isActiveAndEnabled` component is preferred; multiple eligible components produce an error log. If none are eligible, the first result is selected. A valid cached reference is retained when its active state changes.

If `Start()` runs before the object becomes the current instance, the component is temporarily disabled and `OnStart()` is deferred. A successful retry restores the temporary disable before `OnInit()`, preserving any enabled state chosen by that hook. Deferred `OnStart()` runs once the instance is active and enabled, and does not repeat merely because it is
reinitialized or re-enabled.

Inherit from `SceneObject<T>` rather than `SceneObjectBase`. The latter's constructor prevents direct inheritance from another assembly.

### 3. Global MonoBehaviour Singleton (StaticSceneObject)

Uses the same selection and lifecycle logic as `SceneObject<T>`, adding `DontDestroyOnLoad` after successful initialization. It does not create missing objects.

```csharp
using CrowRx.Singleton;

public class GlobalAudioManager : StaticSceneObject<GlobalAudioManager>
{
    // ...
}
```

Place the component on a root GameObject. It must remain a root after `OnInit()`. The selected GameObject and its children persist together. Other instances placed in later scenes are not automatically destroyed or merged.

### 4. Component Container (ComponentContainer)

Useful when you want to use an existing general `Component` as a singleton.

```csharp
using CrowRx.Singleton;
using UnityEngine;

public sealed class CameraConsumer
{
    public Camera GetCamera() => ComponentContainer<Camera>.Instance;
}
```

`TComponent` only needs to derive from `Component`; it does not need to implement `IInstance`. Searches include inactive objects. With multiple results, `Behaviour` components are eligible when `isActiveAndEnabled`; other components are eligible when their GameObject is `activeInHierarchy`. The first eligible result is selected, multiple eligible results
produce an error log, and the first result is used if none are eligible.

The container does not create, initialize, enable, release, or persist components. A cached reference is retained until the component is destroyed; the next access then searches again. Missing components cause `InvalidOperationException`. `StaticComponentContainer<T>` has been removed; the object's owner should explicitly manage persistence.

## Instance Access and Validation

```csharp
// Create the Native instance on first access
var instance = MyManager.Instance;

// Check the published reference without creating or searching
if (MyManager.IsValid)
{
    // ...
}
```

## Lifecycle and Failure Rules

- Use Unity component APIs on the Unity main thread. `Native<T>` synchronization does not make Unity API calls or application state thread-safe.
- Inside `OnInit()` and `OnRelease()`, use `this`. Accessing the same singleton's `Instance` or `ReleaseInstance()` during initialization or release throws `InvalidOperationException`. `OnStart()` may access `Instance`.
- `SceneObject<T>` logs search and initialization failures and returns `null` when no instance was published. `Awake()` also logs initialization failures. `Native<T>` propagates creation and initialization exceptions, and `ComponentContainer<T>` throws when no component exists.
- Failed initialization can be retried. `OnRelease()` is not automatically called for failed initialization, so hooks must clean up partially acquired resources themselves. Persistent objects must satisfy the root requirement both before and after `OnInit()`.
- `ReleaseInstance()` clears the published reference before `OnRelease()`, and release exceptions propagate. Calling it without a reference does nothing. For scene objects, release does not destroy or disable the GameObject; later access can reinitialize it.
- Destruction of the current scene instance normally invokes release. An object destroyed without ever having been active may not receive `OnDestroy()`: cleanup is deferred until the next `Instance` access, a replacement's `Awake()`, or explicit `ReleaseInstance()`. In that case, `OnRelease()` must avoid accessing properties or methods of the destroyed Unity
  object and clean up retained managed resources instead. Release explicitly before destruction when immediate cleanup is required.
- Unity component caches use Unity's object validity check so destroyed objects are not returned as live instances. `IsValid` checks the cache without searching, initializing, or performing deferred cleanup.
- `Native<T>` has no Unity Editor exit subscription. Explicitly coordinate and release its resources as part of application shutdown or the Play Mode lifecycle.