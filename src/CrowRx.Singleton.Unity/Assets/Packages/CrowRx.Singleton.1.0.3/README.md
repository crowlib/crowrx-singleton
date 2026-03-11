# CrowRx.Singleton

A lightweight, interface-driven Singleton utility library for **Native C# Objects** (non-`UnityEngine.Object` types) in .NET and Unity.

## Features
- **Generic Singleton**: Provide a simple way to implement the singleton pattern for standard C# classes.
- **Native Object Only**: Specifically designed for POCOs (Plain Old CLR Objects), not `MonoBehaviour` or `ScriptableObject`.
- **Lifecycle Management**: Supports `Init()` and `Release()` methods through the `IInstance` interface.
- **Unity Editor Integration**: When running in the Unity Editor, it automatically hooks into the `playModeStateChanged` event to call `Release()` and clear the static instance when exiting Play Mode.

## Requirements
- .NET Standard 2.1 compatible environment
- (Optional) Unity 6.0 or newer for automatic Editor lifecycle management

## Installation
Install via NuGet.

## Usage

### 1. Define your Singleton
Inherit from `Native<T>` and implement the `IInstance` interface.

```csharp
using CrowRx.Singleton;

public class MySystem : Native<MySystem>, IInstance
{
    public void Init()
    {
        // Called automatically when the instance is first created
    }

    public void Release()
    {
        // Cleanup logic
    }
}
```

### 2. Access the Instance
The instance is lazily initialized on the first access to the `Instance` property.

```csharp
// Access the singleton
MySystem.Instance.DoSomething();

// Check if the instance is currently active
if (MySystem.IsValid)
{
    // ...
}
```

## Unity Editor Specific Behavior
While this library works in any .NET environment, it includes specialized behavior for the **Unity Editor**:
- When `Instance` is first accessed in the Editor, it registers a callback to `UnityEditor.EditorApplication.playModeStateChanged`.
- When you **exit Play Mode**, `Release()` is automatically called on the instance, and the static reference is set to `null`.
- This ensures that your singleton state does not persist between Play Mode sessions, preventing common memory leaks and state bugs in Editor tools.