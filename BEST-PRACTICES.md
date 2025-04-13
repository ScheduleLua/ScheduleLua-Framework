# ScheduleLua Best Practices

This document outlines best practices for developing Lua scripts with ScheduleLua, organized by topic area.

## Script Structure and Organization

### Script Lifecycle

- **Implement key lifecycle functions** in every script:
  - `Initialize()` - For setup when script is first loaded
  - `Update()` - Called every frame (use cautiously)
  - `OnPlayerReady()` - Called when player is fully initialized
  - `OnSceneLoaded(sceneName)` - Called when a scene loads
  - `Shutdown()` - For cleanup when script is unloaded

- **Return true from Initialize()** to indicate successful initialization:
  ```lua
  function Initialize()
      Log("Script initialized!")
      return true
  end
  ```

- **Always clean up resources** in `Shutdown()`:
  ```lua
  function Shutdown()
      UnregisterAllCommands()
      Log("Script shutdown complete")
  end
  ```

### Variable Scope

- **Prefer local variables** over global variables when possible
  ```lua
  -- Good: Scoped to file
  local playerLastPosition = nil
  
  -- Avoid: Global scope can cause conflicts
  playerLastPosition = nil
  ```

- **Declare global state at file scope** for tracking between function calls
  ```lua
  -- Global tracking variables
  local isUIVisible = false
  local mainWindowId = nil
  local lastCheckedTime = 0
  ```

## Performance Optimization

### Update Function

- **Be extremely careful with operations in `Update()`** as it runs every frame
  ```lua
  function Update()
      -- BAD: Expensive operations every frame
      Log("Current player position: " .. GetPlayerPosition().x)
      
      -- GOOD: Occasional checks with conditions
      if math.random(1, 600) == 1 then
          -- This will run approximately once every 10 seconds (at 60 FPS)
          Log("Current player position: " .. GetPlayerPosition().x)
      end
  end
  ```

- **Use time-based intervals** for periodic operations:
  ```lua
  function Update()
      -- Only perform checks once per in-game minute
      local gameTime = GetGameTime()
      if gameTime ~= lastCheckedTime then
          lastCheckedTime = gameTime
          -- Your periodic code here
      end
  end
  ```

### Resource Management

- **Cache values** that are accessed frequently:
  ```lua
  -- Get once and reuse
  local playerRegion = GetPlayerRegion()
  if playerRegion then
      -- Use playerRegion multiple times
  end
  ```

- **Limit string concatenation in loops**:
  ```lua
  -- BAD: Creates many temporary strings
  local result = ""
  for i = 1, 1000 do
      result = result .. "Item " .. i .. ", "
  end
  
  -- BETTER: Use table concatenation
  local parts = {}
  for i = 1, 1000 do
      parts[i] = "Item " .. i
  end
  local result = table.concat(parts, ", ")
  ```

## Error Handling

### Null Checks

- **Always check for nil** before accessing properties:
  ```lua
  local pos = GetPlayerPosition()
  if pos then
      Log("Position: " .. pos.x .. ", " .. pos.y .. ", " .. pos.z)
  else
      Log("Could not get player position")
  end
  ```

### Function Call Protection

- **Use pcall** for functions that might throw errors:
  ```lua
  local success, result = pcall(function()
      return SomeFunctionThatMightFail()
  end)
  
  if success then
      -- Use result
  else
      LogError("Failed: " .. tostring(result))
  end
  ```

## Console Commands

### Registration

- **Register commands** in `OnConsoleReady()`:
  ```lua
  function OnConsoleReady()
      RegisterCommand(
          "commandname",
          "Command description",
          "commandname arg1 arg2",
          function(args)
              -- Command implementation
          end
      )
  end
  ```

### Command Design

- **Choose unique command names** that don't conflict with game commands
- **Provide clear descriptions** that explain what the command does
- **Include usage examples** in the command registration
- **Handle missing arguments** gracefully:
  ```lua
  RegisterCommand("greet", "Greets someone", "greet John", function(args)
      local name = args[1] or "stranger"
      Log("Hello, " .. name .. "!")
  end)
  ```

### Cleanup

- **Unregister commands** in `Shutdown()`:
  ```lua
  function Shutdown()
      -- Unregister specific commands
      UnregisterCommand("commandname")
      
      -- Or unregister all commands from this script
      UnregisterAllCommands()
  end
  ```

## Event System

### Event Handlers

- **Implement event handlers** for game events relevant to your script:
  ```lua
  function OnPlayerHealthChanged(newHealth)
      if newHealth < 30 then
          Log("Player health is low!")
      end
  end
  
  function OnGameDayChanged(day)
      Log("Day changed to: " .. day)
  end
  ```

### Custom Events

- **Create custom event handlers** for script-specific events:
  ```lua
  function OnPlayerMovedSignificantly()
      local currentRegion = GetPlayerRegion()
      
      if currentRegion ~= playerLastRegion then
          Log("Player changed region to " .. currentRegion)
          playerLastRegion = currentRegion
      end
  end
  ```

## UI Guidelines

### Window Management

- **Track window states** with local variables:
  ```lua
  local mainWindowId = nil
  local isUIVisible = false
  ```

- **Create reusable functions** for UI operations:
  ```lua
  local function CreateUI()
      -- UI creation code
  end
  
  local function ToggleUI()
      if isUIVisible then
          ShowWindow(mainWindowId, false)
      else
          ShowWindow(mainWindowId, true)
      end
      isUIVisible = not isUIVisible
  end
  ```

### Control Placement

- **Position controls consistently** with appropriate spacing:
  ```lua
  -- Create a button with clear positioning
  local buttonId = AddButton(windowId, "btn_id", "Button Text", callback)
  SetControlPosition(buttonId, 50, 120)  -- x, y position
  SetControlSize(buttonId, 300, 40)      -- width, height
  ```

### Notification Usage

- **Use notifications sparingly** to avoid overwhelming the player:
  ```lua
  -- Good: Important information
  ShowNotification("Quest completed!")
  
  -- Avoid: Excessive notifications
  -- ShowNotification("Player moved")  -- Too frequent
  ```

## Game Interaction

### Player Interaction

- **Check player state** before performing operations:
  ```lua
  local playerState = GetPlayerState()
  if playerState and playerState.isAlive then
      -- Perform actions for living player
  end
  ```

### NPC Interaction

- **Verify NPC existence** before accessing properties:
  ```lua
  local npcsInRegion = GetNPCsInRegion(regionName) or {}
  for i, npc in pairs(npcsInRegion) do
      local npcObj = FindNPC(npc.id)
      if npcObj then
          local pos = GetNPCPosition(npcObj)
          -- Use position
      end
  end
  ```

### Game Time Usage

- **Format game time** for readability:
  ```lua
  local currentTime = GetGameTime()
  local formattedTime = FormatGameTime(currentTime)
  Log("Current time: " .. formattedTime)
  ```

- **Check time conditionally** for time-based events:
  ```lua
  if IsNightTime() then
      -- Run night-specific code
  end
  ```

## Script Interoperability

### Modular Design

- **Create helper functions** for reusable functionality:
  ```lua
  local function ShowTemporaryDialogue(title, text, delay)
      delay = delay or 5.0  -- Default to 5 seconds
      
      ShowDialogue(title, text)
      Wait(delay, function()
          CloseDialogue()
      end)
  end
  ```

### Function Naming

- **Use descriptive function names** with consistent naming patterns:
  ```lua
  -- Good: Clear purpose
  function GetPlayerInventoryWeight()
  
  -- Avoid: Unclear purpose
  function Process()  -- Too vague
  ```

## Debugging and Logging

### Log Levels

- **Use appropriate log levels** for different message types:
  ```lua
  Log("Normal information message")
  LogWarning("Warning: Low energy detected")
  LogError("Error: Failed to load resource")
  ```

### Logging Practices

- **Log critical lifecycle events** for easier debugging:
  ```lua
  function Initialize()
      Log("Script initialized!")
      return true
  end
  
  function Shutdown()
      Log("Script shutdown")
  end
  ```

- **Include context in log messages**:
  ```lua
  -- Good: Context included
  Log("Player health changed from " .. oldHealth .. " to " .. newHealth)
  
  -- Avoid: No context
  Log("Health changed")  -- Not enough information
  ```

## Memory Management

### Table Reuse

- **Reuse tables** instead of creating new ones repeatedly:
  ```lua
  -- BAD: Creates new table every time
  function Update()
      local result = {}
      -- Fill table
      return result
  end
  
  -- GOOD: Reuse existing table
  local resultTable = {}
  function Update()
      -- Clear table
      for k in pairs(resultTable) do resultTable[k] = nil end
      -- Fill table
      return resultTable
  end
  ```

### Clear References

- **Set unused references to nil** to help garbage collection:
  ```lua
  local hugeData = LoadLargeData()
  -- Use hugeData
  hugeData = nil  -- Clear reference when done
  ```

## Configuration and Settings

### Defaults

- **Provide default values** for configurable settings:
  ```lua
  local function ShowTemporaryDialogue(title, text, delay)
      delay = delay or 5.0  -- Default to 5 seconds
      -- Function implementation
  end
  ```

### Script Settings

- **Document configuration variables** at the top of your script:
  ```lua
  -- Configuration
  local CONFIG = {
      HEAL_THRESHOLD = 30,      -- Health level to trigger healing
      CHECK_INTERVAL = 5,       -- Seconds between checks
      DEBUG_MODE = false        -- Enable extra logging
  }
  ```

## Code Readability

### Comments

- **Comment your code** appropriately:
  ```lua
  -- Global variables for tracking state
  local playerLastPosition = nil
  local playerLastRegion = nil
  
  -- Check if player has moved significantly (more than 5 units)
  local currentPos = GetPlayerPosition()
  if playerLastPosition then
      local distance = Vector3Distance(currentPos, playerLastPosition)
      if distance > 5 then
          -- Player has moved significantly, update tracking
          playerLastPosition = currentPos
          OnPlayerMovedSignificantly()
      end
  end
  ```

### Consistent Formatting

- **Use consistent indentation** (typically 4 spaces or tabs)
- **Add blank lines** between logical sections of code
- **Align related code** for readability:
  ```lua
  -- Good: Clear alignment
  local isEnabled   = true
  local playerName  = "John"
  local healthValue = 100
  ```
