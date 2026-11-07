# CrowRx.Singleton

A lightweight Singleton utility library for **Native C# Objects** (non-`UnityEngine.Object` types) in .NET and Unity.

## Features

- **Generic Singleton**: Inherit from `Native<TInstance>` for lazy singleton creation.
- **Native Objects**: Designed for standard C# classes, rather than `MonoBehaviour` or `ScriptableObject`.
- **Lifecycle Hooks**: Override `OnInit()` and `OnRelease()`; call the static `ReleaseInstance()` method for cleanup.
- **Synchronized Creation and Release**: Creation and release share a lock for each closed generic type. Access to an already published instance uses `Volatile.Read` without acquiring the lock.
- **Safe Publication**: The instance is published only after `OnInit()` completes successfully.

## Requirements

- .NET Standard 2.1 compatible environment

## Installation

Install via NuGet.

## Usage

### 1. Define your Singleton

Inherit from `Native<TInstance>` and override the protected lifecycle hooks. The base class already implements `IInstance`, which is currently an empty marker interface.

A parameterless constructor is required. It can be private to prevent direct construction.

```csharp
using CrowRx.Singleton;

public sealed class MySystem : Native<MySystem>
{
    private MySystem() { }

    protected override void OnInit()
    {
        // Initialize this object before it is published.
    }

    protected override void OnRelease()
    {
        // Clean up this object's resources.
    }

    public void DoSomething()
    {
        // Application logic; add synchronization here if needed.
    }
}
```

### 2. Access the Instance

```csharp
// Creates and initializes the singleton on first access.
MySystem.Instance.DoSomething();

// Checks whether an initialized instance is currently published.
bool isReady = MySystem.IsValid;
```

Concurrent callers wait for creation to complete when no instance is published. Once initialization succeeds, subsequent accesses return the published instance without acquiring the lifecycle lock.

`IsValid` does not create an instance. It is `false` before creation, during initialization or release, after failed initialization, and after release. It is a snapshot, not a guarantee that the instance will remain available.

### 3. Release the Instance

```csharp
// First ensure all other users have stopped using the instance.
MySystem.ReleaseInstance();
```

Release removes the published reference before calling `OnRelease()`. Calling `ReleaseInstance()` when no instance exists does nothing. A later access to `Instance` creates a new object.

## Lifecycle and Threading Precautions

- **Coordinate release with all users.** The lifecycle lock serializes creation and release, but does not protect references already returned to callers. Stop and coordinate all concurrent use and acquisition before releasing; otherwise another thread can use an object whose resources are being cleaned up. `IsValid` cannot prevent this race.
- **Synchronize your own methods and state.** Safe publication does not make the instance's mutable data or application methods thread-safe.
- **Use `this` inside lifecycle hooks.** Accessing the same singleton's `Instance` from its constructor, `OnInit()`, or `OnRelease()` throws `InvalidOperationException` on the same thread. Calling its `ReleaseInstance()` during initialization or release also throws. These checks include indirect calls through other objects.
- **Avoid blocking dependencies inside lifecycle callbacks.** Constructors and lifecycle hooks run while holding the lifecycle lock. Waiting for another thread that needs the same singleton can deadlock. Cyclic dependencies between different singleton types can also deadlock when initialized or released concurrently.
- **Handle partial initialization cleanup.** If construction or `OnInit()` throws, no instance is published, and a later access can retry. `OnRelease()` is not automatically called after failed initialization; initialization code must clean up any resources it acquired before failing.
- **Release exceptions propagate.** If `OnRelease()` throws, the published reference remains cleared and the release flag is reset. Resource cleanup may still be incomplete, so the hook should handle that appropriately.
- **Callbacks run on the calling thread.** Unity API calls that require the main thread must be made from that thread, including first access and release when their hooks use those APIs.

## Unity Lifecycle

`Native<TInstance>` does not register Unity Editor callbacks or automatically release its instance when leaving Play Mode. Arrange for `ReleaseInstance()` to be called by your application's shutdown or Play Mode lifecycle integration, after other users have stopped. This is especially relevant when Domain Reload is disabled and static state can survive between Play Mode sessions.

## Migration

The public instance methods `Init()` and `Release()` have been removed from `Native<TInstance>` and `IInstance`.

- Move initialization code into `protected override void OnInit()` and trigger initialization through `MySystem.Instance`.
- Move cleanup code into `protected override void OnRelease()` and replace instance `Release()` calls with `MySystem.ReleaseInstance()`.
- `IInstance` no longer provides lifecycle methods; update callers that previously invoked them through the interface.
