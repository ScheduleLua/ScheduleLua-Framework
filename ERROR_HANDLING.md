# ScheduleLua Error Handling & Mod Compatibility Guide

This guide details best practices for error handling and ensuring compatibility with other mods in the ScheduleLua framework.

## Error Handling Principles

### 1. Defense in Depth

Implement multiple layers of error protection:

- **Input validation** at API boundaries
- **Try-catch blocks** around external code interactions
- **Null checks** before accessing potentially null objects
- **Boundary validation** for array indices and numeric ranges
- **Graceful degradation** when dependencies are unavailable

### 2. Error Containment

Prevent errors in one script from affecting others:

- **Script isolation**: Run each script in its own context
- **Error boundaries**: Catch and log errors rather than allowing propagation
- **Resource cleanup**: Ensure resources are properly released even during errors
- **State restoration**: Return to a valid state after errors when possible

### 3. Detailed Logging

Provide meaningful error information to assist debugging:

- **Context details**: Include relevant object names and values
- **Stack traces**: Enable optional stack traces for debugging
- **Error categorization**: Distinguish between different error types
- **Log levels**: Use appropriate severity levels (error, warning, info)
- **Error codes**: Consider adding error codes for common issues

## Implementation Patterns

### Script Error Handling

```csharp
// Executing Lua function with error handling
public DynValue CallLuaFunction(string functionName, params object[] args)
{
    try
    {
        DynValue function = _scriptEngine.Globals.Get(functionName);
        
        if (function.Type != DataType.Function)
            return DynValue.Nil;
            
        return _scriptEngine.Call(function, args);
    }
    catch (ScriptRuntimeException ex)
    {
        _logger.Error($"Runtime error in {_name}.{functionName}: {ex.Message}");
        if (_config.ShowStackTraces)
            _logger.Error(ex.DecoratedMessage);
        return DynValue.Nil;
    }
    catch (Exception ex)
    {
        _logger.Error($"Error calling {_name}.{functionName}: {ex.Message}");
        return DynValue.Nil;
    }
}
```

### API Error Handling

```csharp
public static bool AddItemToInventory(string itemName, int amount)
{
    // Input validation
    if (string.IsNullOrEmpty(itemName))
    {
        Log("AddItemToInventory: itemName is null or empty");
        return false;
    }
    
    if (amount <= 0)
    {
        Log("AddItemToInventory: amount must be positive");
        return false;
    }
    
    try
    {
        // Access validation
        if (Player.Local == null || Player.Local.Inventory == null)
        {
            Log("AddItemToInventory: Player inventory is not available");
            return false;
        }
        
        // Game-specific code
        var item = ItemDatabase.GetItemByName(itemName);
        if (item == null)
        {
            Log($"AddItemToInventory: Item '{itemName}' not found in database");
            return false;
        }
        
        return Player.Local.Inventory.AddItem(item, amount);
    }
    catch (Exception ex)
    {
        LogError($"Error in AddItemToInventory: {ex.Message}");
        return false;
    }
}
```

## Error Recovery Strategies

### 1. Retries for Transient Errors

For operations that might fail temporarily (e.g., accessing a resource that's being loaded):

```csharp
private T RetryOperation<T>(Func<T> operation, int maxRetries = 3, float delaySeconds = 0.5f)
{
    int attempts = 0;
    while (attempts < maxRetries)
    {
        try
        {
            return operation();
        }
        catch (Exception ex)
        {
            attempts++;
            if (attempts >= maxRetries)
                throw;
                
            _logger.Warning($"Operation failed, retrying ({attempts}/{maxRetries}): {ex.Message}");
            System.Threading.Thread.Sleep((int)(delaySeconds * 1000));
        }
    }
    
    // This shouldn't be reached due to the throw above, but compiler might complain
    return default(T);
}
```

### 2. Default Values

Return sensible defaults when operations fail:

```csharp
public static Vector3 GetPlayerPosition()
{
    try
    {
        if (Player.Local == null)
            return Vector3.zero;
            
        return Player.Local.transform.position;
    }
    catch
    {
        return Vector3.zero;
    }
}
```

### 3. Feature Detection

Check for feature availability before using:

```csharp
public static bool CanUseFeature(string featureName)
{
    switch (featureName.ToLower())
    {
        case "inventory":
            return Player.Local != null && Player.Local.Inventory != null;
        case "quests":
            return QuestManager.Instance != null;
        // Add more feature checks as needed
        default:
            return false;
    }
}
```

## Cross-Mod Compatibility

### 1. Avoiding Namespace Conflicts

- **Prefix all global functions** with a unique identifier
- **Use a dedicated Lua table** for your API
- **Avoid common function names** that might be used by other mods

Example using a dedicated table:

```csharp
// In C#
luaEngine.Globals["ScheduleLua"] = new Table(luaEngine);
var luaTable = luaEngine.Globals["ScheduleLua"] as Table;

luaTable["GetPlayer"] = (Func<Player>)GetPlayer;
luaTable["Log"] = (Action<string>)Log;

// In Lua
ScheduleLua.Log("Using namespaced API")
local player = ScheduleLua.GetPlayer()
```

### 2. Version Checking

Implement version checking to handle API changes:

```csharp
// In C#
luaEngine.Globals["SCHEDULELUA_VERSION"] = "1.2.0";
luaEngine.Globals["SCHEDULELUA_VERSION_MAJOR"] = 1;
luaEngine.Globals["SCHEDULELUA_VERSION_MINOR"] = 2;
luaEngine.Globals["SCHEDULELUA_VERSION_PATCH"] = 0;

// In Lua
if SCHEDULELUA_VERSION_MAJOR >= 1 and SCHEDULELUA_VERSION_MINOR >= 2 then
    -- Use features from version 1.2+
else
    -- Fallback for older versions
end
```

### 3. API Feature Detection

Allow scripts to check for feature availability:

```csharp
// In C#
luaEngine.Globals["HasFeature"] = (Func<string, bool>)HasFeature;

public static bool HasFeature(string featureName)
{
    var features = new Dictionary<string, bool>
    {
        { "quests", true },
        { "npcs", true },
        { "ui", VERSION >= 1.2 }, // Only in newer versions
        // ...
    };
    
    return features.ContainsKey(featureName.ToLower()) && features[featureName.ToLower()];
}

// In Lua
if HasFeature("ui") then
    -- Use UI features
else
    -- Fallback approach
end
```

### 4. Dependency Management

Allow scripts to declare dependencies:

```csharp
// In Lua
-- At the top of a script
SCRIPT = {
    name = "MyScript",
    version = "1.0.0",
    author = "YourName",
    requires = {
        { mod = "ScheduleLua", minVersion = "1.2.0" },
        { mod = "OtherMod", minVersion = "0.5.0", optional = true }
    }
}

-- In C#, check these requirements before initializing
private bool CheckScriptRequirements(LuaScript script)
{
    try
    {
        Table scriptInfo = script.GetScriptInfo();
        if (scriptInfo == null)
            return true; // No requirements specified
            
        if (!scriptInfo.Keys.Contains("requires"))
            return true; // No specific requirements
            
        Table requires = scriptInfo.Get("requires").Table;
        foreach (DynValue requirement in requires.Values)
        {
            // Check each requirement against loaded mods
            // ...
        }
        
        return true;
    }
    catch
    {
        return true; // In case of error, allow script to run
    }
}
```

## Specific Game Compatibility Issues

### Game API Changes

Handle potential changes in the game's API:

```csharp
// Check if a method exists before using it
private static bool HasMethod(object obj, string methodName)
{
    if (obj == null) return false;
    return obj.GetType().GetMethod(methodName) != null;
}

// Usage example
public static void DoSomethingWithPlayer()
{
    if (Player.Local != null && HasMethod(Player.Local, "NewMethodName"))
    {
        // Use new method
        Player.Local.NewMethodName();
    }
    else if (Player.Local != null && HasMethod(Player.Local, "OldMethodName"))
    {
        // Fall back to old method
        Player.Local.OldMethodName();
    }
    else
    {
        // Alternative implementation
    }
}
```

### Threading and Coroutines

Be careful with threading and Unity coroutines:

```csharp
// Safe coroutine execution
public static void RunCoroutine(IEnumerator routine)
{
    try
    {
        MelonCoroutines.Start(routine);
    }
    catch (Exception ex)
    {
        LogError($"Error starting coroutine: {ex.Message}");
    }
}
```

### MelonLoader Version Compatibility

Account for different MelonLoader versions:

```csharp
public static void EnsureMelonLoaderCompatibility()
{
    // Get MelonLoader version
    Version melonLoaderVersion = typeof(MelonMod).Assembly.GetName().Version;
    
    if (melonLoaderVersion < new Version(0, 5, 0))
    {
        LogError("ScheduleLua requires MelonLoader 0.5.0 or higher");
        // Take appropriate action
    }
    
    // Handle specific version differences
    if (melonLoaderVersion >= new Version(0, 6, 0))
    {
        // Use 0.6.0+ specific APIs
    }
    else
    {
        // Use older APIs
    }
}
```

## Testing for Compatibility

Implement a comprehensive testing strategy:

1. **Unit tests**: Test API functions in isolation
2. **Integration tests**: Test interactions between components
3. **Compatibility matrix**: Test with different game and MelonLoader versions
4. **Stress testing**: Run multiple scripts simultaneously
5. **Error injection**: Deliberately cause errors to test recovery

## Error Reporting for End Users

Implement user-friendly error reporting:

1. **Log file**: Write detailed errors to a dedicated log file
2. **In-game notifications**: Show critical errors to the user
3. **Diagnostics mode**: Allow enabling additional logging
4. **Error collection**: Consider an opt-in error reporting system

## Conclusion

Robust error handling and compatibility practices are essential for a stable modding framework. By implementing these guidelines, you can create a resilient system that can gracefully handle errors and maintain compatibility with other mods and future game updates. 