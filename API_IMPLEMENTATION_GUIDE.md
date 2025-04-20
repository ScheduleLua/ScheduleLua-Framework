# ScheduleLua API Implementation Guide

This guide provides guidelines and best practices for implementing Lua API bindings to Schedule I game systems.

## API Design Principles

When implementing new API functions for ScheduleLua, adhere to these principles:

1. **Discoverability**: Functions should be easy to find and logically organized
2. **Consistency**: Follow consistent naming and parameter conventions
3. **Simplicity**: Prefer simple interfaces over complex ones
4. **Safety**: Implement error handling to prevent game crashes
5. **Efficiency**: Optimize performance for functions called frequently
6. **Documentation**: Document all functions thoroughly

## Naming Conventions

- Use `PascalCase` for function names
- Begin with a verb that describes the action (Get, Set, Find, Create, etc.)
- Follow with the subject of the action
- Examples: `GetPlayerHealth`, `SetNPCPosition`, `FindItemByName`

## Function Implementation Pattern

For each API function, follow this pattern:

```csharp
/// <summary>
/// Brief description of what the function does
/// </summary>
/// <param name="paramName">Description of parameter</param>
/// <returns>Description of return value</returns>
public static ReturnType FunctionName(ParameterType paramName)
{
    // 1. Input validation
    if (paramName == null)
    {
        LuaUtility.Log("FunctionName: paramName is null");
        return default(ReturnType); // Or appropriate default value
    }
    
    try
    {
        // 2. Core functionality
        // ...
        
        // 3. Return value
        return result;
    }
    catch (Exception ex)
    {
        // 4. Error handling
        LuaUtility.LogError($"Error in FunctionName: {ex.Message}", ex);
        return default(ReturnType); // Or appropriate default value
    }
}
```

## Type Handling

### Primitive Types

- `int`, `float`, `bool`, and `string` are automatically converted between C# and Lua
- Prefer `float` over `double` for compatibility with Unity

### Unity Types

- Register Unity types with MoonSharp using `UserData.RegisterType<T>()`
- Create helper methods for common operations
- Example:
  ```csharp
  // Register type
  UserData.RegisterType<Vector3>();
  
  // Helper method for Lua
  public static Vector3 CreateVector3(float x, float y, float z)
  {
      return new Vector3(x, y, z);
  }
  ```

### Game-Specific Types

- Register game types with MoonSharp using `UserData.RegisterType<T>()`
- Create simplified wrapper objects when appropriate
- Ensure objects exposed to Lua don't have circular references

## Adding New API Modules

To add a new API module:

1. Create a new C# file in the `API` directory
2. Implement the static API class with appropriate methods
3. Register the methods with the Lua engine in the core initialization:

```csharp
// In LuaManager.cs or similar central registration point
private void RegisterAPI(Script luaEngine)
{
    // Register existing API modules
    CoreAPI.Register(luaEngine);
    PlayerAPI.Register(luaEngine);
    
    // Register your new module
    MyNewAPI.Register(luaEngine);
}
```

4. Document the new API in the README or separate documentation

## Game System Integration Guidelines

### UI System

- Provide functions to create basic UI elements
- Allow registering callbacks for UI events
- Abstract away complex UI hierarchies
- Example functions:
  ```
  CreateWindow(title, width, height)
  AddButton(parent, text, x, y, width, height)
  SetButtonCallback(button, callbackFunction)
  ```

### Game State

- Provide read access to important game state
- Use caution with write access to prevent game corruption
- Consider a permission system for risky operations
- Example functions:
  ```
  GetGameState()
  IsMenuOpen()
  IsPaused()
  ```

### Entities

- Create functions to find, query, and manipulate entities
- Provide filtered entity searches
- Enable event callbacks for entity interactions
- Example functions:
  ```
  FindEntitiesInRadius(position, radius)
  GetEntityType(entity)
  SetEntityBehavior(entity, behavior)
  ```

### World/Map

- Enable safe manipulation of world objects
- Provide spatial query functions
- Abstract complex world data structures
- Example functions:
  ```
  GetTileAt(x, y)
  SetTileProperties(x, y, properties)
  GetHeight(x, y)
  ```

## Performance Considerations

1. **Avoid per-frame overhead**: Minimize allocations in frequently called functions
2. **Caching**: Cache references to frequently accessed objects
3. **Batching**: Combine operations when possible
4. **Throttling**: Rate-limit expensive operations
5. **Profiling**: Use Unity Profiler to identify bottlenecks

## Error Handling and Safety

1. **Validate all inputs**: Check for null references and invalid parameters
2. **Graceful failure**: Return reasonable defaults rather than crashing
3. **Comprehensive logging**: Log errors with helpful context
4. **Prevention**: Prevent operations that could crash the game
5. **Timeout protection**: For long-running operations, consider timeouts

## Event System Integration

1. **Identify key game events** to expose to Lua
2. **Create C# event handlers** that register with game systems
3. **Forward events** to the Lua environment
4. **Implement clean-up logic** to prevent memory leaks

## Testing New API Functions

Before committing new API functions:

1. **Create test scripts** that exercise the new functionality
2. **Test edge cases** including null inputs, invalid parameters
3. **Performance test** frequently called functions
4. **Check error handling** by deliberately causing errors
5. **Verify memory usage** doesn't increase over time

## Documentation Standards

Document each API function with:

1. **Purpose**: What the function does
2. **Parameters**: Description of each parameter
3. **Return value**: What the function returns
4. **Exceptions**: Any exceptions that might be thrown
5. **Example usage**: Simple Lua code example

## Example Implementation

Here's an example of implementing a new API module for interacting with the game's quest system:

```csharp
using MelonLoader;
using MoonSharp.Interpreter;
using ScheduleOne.QuestSystem;

namespace ScheduleLua.API
{
    /// <summary>
    /// Provides quest-related functionality to Lua scripts
    /// </summary>
    public static class QuestAPI
    {
        /// <summary>
        /// Registers quest API functions with the Lua engine
        /// </summary>
        public static void Register(Script luaEngine)
        {
            // Register types
            UserData.RegisterType<Quest>();
            UserData.RegisterType<QuestObjective>();
            
            // Register functions
            luaEngine.Globals["GetActiveQuests"] = (Func<Quest[]>)GetActiveQuests;
            luaEngine.Globals["GetQuestByName"] = (Func<string, Quest>)GetQuestByName;
            luaEngine.Globals["IsQuestComplete"] = (Func<string, bool>)IsQuestComplete;
            luaEngine.Globals["GetQuestProgress"] = (Func<string, float>)GetQuestProgress;
        }
        
        /// <summary>
        /// Gets all currently active quests
        /// </summary>
        /// <returns>Array of active quests</returns>
        public static Quest[] GetActiveQuests()
        {
            try
            {
                if (QuestManager.Instance == null)
                {
                    LuaUtility.LogWarning("GetActiveQuests: QuestManager not available");
                    return new Quest[0];
                }
                
                return QuestManager.Instance.ActiveQuests.ToArray();
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in GetActiveQuests: {ex.Message}");
                return new Quest[0];
            }
        }
        
        /// <summary>
        /// Finds a quest by its name
        /// </summary>
        /// <param name="questName">Name of the quest to find</param>
        /// <returns>Quest object if found, null otherwise</returns>
        public static Quest GetQuestByName(string questName)
        {
            if (string.IsNullOrEmpty(questName))
            {
                LuaUtility.LogWarning("GetQuestByName: questName is null or empty");
                return null;
            }
            
            try
            {
                if (QuestManager.Instance == null)
                {
                    LuaUtility.LogWarning("GetQuestByName: QuestManager not available");
                    return null;
                }
                
                return QuestManager.Instance.GetQuestByName(questName);
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in GetQuestByName: {ex.Message}");
                return null;
            }
        }
        
        // Additional quest API functions...
    }
} 