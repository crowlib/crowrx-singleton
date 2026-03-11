# CrowRx.Singleton

A lightweight, interface-driven singleton utility library for .NET and Unity. It provides a structured way to manage singletons ranging from pure C# objects (Native) to MonoBehaviour-based scene objects.

## Key Features

- **Generic Singleton**: Provides a concise way to implement the singleton pattern for standard C# classes.
- **Native & MonoBehaviour Support**: Supports not only Plain Old CLR Objects (POCO) but also `MonoBehaviour`-based scene objects and persistent `DontDestroyOnLoad` objects.
- **Lifecycle Management**: Consistent `Init()` and `Release()` lifecycle management through the `IInstance` interface.
- **Unity Editor Integration**: Automatically releases instances and clears static references when exiting Play Mode in the Unity Editor to prevent memory leaks and state contamination.
- **R3 Integration**: Supports reactive lifecycle management via R3 through `ComponentContainer`.

## Requirements

- **Unity**: 6000.3 (Unity 6.0) or newer
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

public class MyManager : Native<MyManager>, IInstance
{
    public void Init()
    {
        // Automatically called when the instance is first created
    }

    public void Release()
    {
        // Cleanup logic (automatically called when exiting Play Mode in Editor)
    }
}
```

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
}
```

### 3. Global MonoBehaviour Singleton (StaticSceneObject)
A singleton that persists across scene changes using `DontDestroyOnLoad`. It automatically creates the component if it doesn't exist in the scene.

```csharp
using CrowRx.Singleton;

public class GlobalAudioManager : StaticSceneObject<GlobalAudioManager>
{
    // ...
}
```

### 4. Component Container (ComponentContainer)
Useful when you want to use an existing general `Component` as a singleton.

```csharp
// Finds the instance in the scene and provides it as a singleton
var manager = ComponentContainer<MyExistingComponent>.Instance;

// When DontDestroyOnLoad and automatic creation are required
var staticManager = StaticComponentContainer<MyExistingComponent>.Instance;
```

## Instance Access and Validation

```csharp
// Access the instance (performs lazy initialization on first access)
var instance = MyManager.Instance;

// Check if the current instance is valid (already created)
if (MyManager.IsValid)
{
    // ...
}
```

## Editor Specific Behavior
- `Native<T>` instances subscribe to the `playModeStateChanged` event in the Unity Editor.
- When exiting Play Mode, `Release()` is automatically called, and the reference is set to `null`.
- This prevents bugs caused by static variables retaining data during editor tools usage or repetitive testing cycles.
