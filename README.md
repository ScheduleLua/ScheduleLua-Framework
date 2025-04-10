# ScheduleLua

A Lua modding framework for Schedule I that aims to expose the game's functionality to Lua scripts, enabling custom gameplay mechanics, automation, and new features.

## Table of Contents

- [ScheduleLua](#schedulelua)
  - [Table of Contents](#table-of-contents)
  - [Overview](#overview)
  - [Features](#features)
  - [Installation](#installation)
  - [Getting Started](#getting-started)
    - [Creating Your First Script](#creating-your-first-script)
    - [Script Lifecycle](#script-lifecycle)
  - [Configuration](#configuration)
  - [API Reference](#api-reference)
    - [Logging Functions](#logging-functions)
    - [GameObject Functions](#gameobject-functions)
    - [Player Functions](#player-functions)
    - [Inventory Functions](#inventory-functions)
    - [Time Functions](#time-functions)
    - [NPC Functions](#npc-functions)
    - [Map Functions](#map-functions)
    - [Helper Functions](#helper-functions)
  - [API Implementation Checklist](#api-implementation-checklist)
    - [Player System](#player-system)
      - [Basic Player Information](#basic-player-information)
      - [Player Stats](#player-stats)
      - [Player Movement](#player-movement)
    - [NPC System](#npc-system)
      - [Basic NPC Information](#basic-npc-information)
      - [NPC Management](#npc-management)
    - [Inventory System](#inventory-system)
      - [Basic Inventory](#basic-inventory)
      - [Advanced Inventory](#advanced-inventory)
    - [Time System](#time-system)
      - [Basic Time](#basic-time)
      - [Time Events](#time-events)
    - [Map/World System](#mapworld-system)
      - [Regions](#regions)
      - [World Interaction](#world-interaction)
    - [UI System](#ui-system)
      - [Basic UI](#basic-ui)
      - [Advanced UI](#advanced-ui)
    - [Quest System](#quest-system)
      - [Basic Quests](#basic-quests)
      - [Advanced Quests](#advanced-quests)
    - [Economy System](#economy-system)
      - [Basic Economy](#basic-economy)
      - [Advanced Economy](#advanced-economy)
    - [Weather System](#weather-system)
      - [Basic Weather](#basic-weather)
      - [Advanced Weather](#advanced-weather)
    - [Modding Tools](#modding-tools)
      - [Development Tools](#development-tools)
      - [Configuration](#configuration-1)
    - [Event System](#event-system)
      - [Basic Events](#basic-events)
      - [Advanced Events](#advanced-events)
    - [Utility Functions](#utility-functions)
      - [Basic Utilities](#basic-utilities)
      - [Advanced Utilities](#advanced-utilities)
  - [Events](#events)
  - [Example Script](#example-script)
  - [Contributing](#contributing)
    - [Roadmap \& Planned Features](#roadmap--planned-features)
  - [License](#license)
  - [Acknowledgments](#acknowledgments)

## Overview

ScheduleLua is a MelonLoader mod that integrates the MoonSharp Lua interpreter with Schedule I, providing an easy to learn, flexible scripting environment. The framework exposes core game systems through a Lua API, allowing modders to create custom gameplay experiences without direct C# coding.

## Features

- **Robust Lua Environment**: Built on MoonSharp for .NET integration
- **Hot Reloading**: Edit scripts while the game is running for rapid development
- **Event System**: Subscribe to game events like day changes, player status updates, etc.
- **ScheduleOne API**: Access to player, NPCs, and more
- **Error Handling**: Detailed error reporting and script isolation
- **Mod Configuration**: Configurable settings via MelonPreferences

## Installation

1. Install [MelonLoader](https://melonwiki.xyz/#/?id=automated-installation) for Schedule I
2. Download the latest `ScheduleLua.dll` release from [GitHub Releases](https://github.com/yourusername/ScheduleLua/releases)
3. Place the DLL in your `Mods` folder (typically `Schedule I/Mods/`)
4. Launch the game

On first launch, a `UserData/ScheduleLua/Scripts` folder will be created with an example script.

## Getting Started

### Creating Your First Script

Create a new `.lua` file in the `UserData/ScheduleLua/Scripts` directory:

```lua
Log("Script loading...")

function Initialize()
    Log("Hello from Schedule I!")
end

function OnSceneLoaded(sceneName)
    Log("Scene loaded: " .. sceneName)
end

function OnPlayerReady()
    Log("Player is now ready!")
    
    RunMainExample()
end

function RunMainExample()
    Log("Running main example code...")
    
    -- Get player state information
    local playerState = GetPlayerState()
    if playerState then
        local playerName = playerState.playerName or "Unknown"
        local health = playerState.health or 0
        local maxHealth = playerState.maxHealth or 100
        local isAlive = playerState.isAlive or false
        
        Log("Player name: " .. playerName)
        Log("Player health: " .. health .. "/" .. maxHealth)
        Log("Player is alive: " .. tostring(isAlive))
        
        if playerState.isSprinting then
            Log("Player is sprinting")
        end
        
        -- Get position from the state table
        local posTable = playerState.position
        if posTable then
            Log("Player position from state: X=" .. posTable.x .. ", Y=" .. posTable.y .. ", Z=" .. posTable.z)
        end
    else
        Log("Player not found or not initialized")
    end
    
    -- Get player position directly
    local pos = GetPlayerPosition()
    if pos then
        Log("Player position: X=" .. pos.x .. ", Y=" .. pos.y .. ", Z=" .. pos.z)
    else
        Log("Could not get player position")
    end
    
    -- Get player region
    local region = GetPlayerRegion()
    Log("Player is in region: " .. (region or "Unknown"))
    
    -- Get all NPCs in the region
    if region then
        local npcsInRegion = GetNPCsInRegion(region)
        Log("NPCs in the same region as player: " .. #npcsInRegion)
        
        for i, npc in pairs(npcsInRegion) do
            Log("- " .. npc.fullName)
        end
    end
end

function Update()
    -- This function is called every frame
    -- Be careful with performance - avoid expensive operations here
    
    -- For example, to run code only occasionally:
    if math.random(1, 600) == 1 then
        -- This will run approximately once every 10 seconds (at 60 FPS)
        -- Your periodic code here
    end
end
```

### Script Lifecycle

1. **Loading**: Scripts are loaded when the game starts or when modified (hot reload)
2. **Initialization**: The `Initialize()` function is called if it exists
3. **Update**: The `Update()` function is called every frame if it exists
4. **Events**: Event handlers are called when the corresponding game events occur
5. **Command Handling**: Scripts can process commands via `HandleCommand()`

## Configuration

Edit settings in `UserData/MelonPreferences.cfg`:

```
[ScheduleLua]
EnableHotReload = true
LogScriptErrors = true
```

## API Reference

### Logging Functions

| Function              | Description                           |
| --------------------- | ------------------------------------- |
| `Log(message)`        | Logs a normal message to the console  |
| `LogWarning(message)` | Logs a warning message to the console |
| `LogError(message)`   | Logs an error message to the console  |

### GameObject Functions

| Function                           | Description                       |
| ---------------------------------- | --------------------------------- |
| `FindGameObject(name)`             | Finds a GameObject by name        |
| `GetPosition(gameObject)`          | Gets the position of a GameObject |
| `SetPosition(gameObject, x, y, z)` | Sets the position of a GameObject |

### Player Functions

| Function                       | Description                                                                                  |
| ------------------------------ | -------------------------------------------------------------------------------------------- |
| `GetPlayer()`                  | Gets the local player object                                                                 |
| `GetPlayerState()`             | Gets a table containing player status, health, energy, position, and other state information |
| `GetPlayerPosition()`          | Gets the position of the player as a Vector3 or table with x, y, z coordinates               |
| `SetPlayerPosition(x, y, z)`   | Sets the position of the player                                                              |
| `TeleportPlayer(x, y, z)`      | Teleports the player to the specified coordinates                                            |
| `GetPlayerMoney()`             | Gets the player's current money                                                              |
| `AddPlayerMoney(amount)`       | Adds money to the player                                                                     |
| `GetPlayerEnergy()`            | Gets the player's current energy                                                             |
| `SetPlayerEnergy(amount)`      | Sets the player's energy                                                                     |
| `GetPlayerHealth()`            | Gets the player's current health                                                             |
| `SetPlayerHealth(amount)`      | Sets the player's health                                                                     |
| `GetPlayerRegion()`            | Gets the name of the region the player is currently in                                       |
| `IsPlayerInRegion(regionName)` | Checks if the player is in the specified region                                              |

### Inventory Functions

| Function                                    | Description                                 |
| ------------------------------------------- | ------------------------------------------- |
| `GetInventorySlotCount()`                   | Gets the number of inventory slots          |
| `GetInventoryItemAt(slotIndex)`             | Gets the item name at the specified slot    |
| `AddItemToInventory(itemName, amount)`      | Adds an item to the player's inventory      |
| `RemoveItemFromInventory(itemName, amount)` | Removes an item from the player's inventory |

### Time Functions

| Function                    | Description                        |
| --------------------------- | ---------------------------------- |
| `GetGameTime()`             | Gets the current game time         |
| `GetGameDay()`              | Gets the current day as a string   |
| `GetGameDayInt()`           | Gets the current day as an integer |
| `IsNightTime()`             | Returns true if it's night time    |
| `FormatGameTime(timeValue)` | Formats a time value as a string   |

### NPC Functions

| Function                       | Description                                           |
| ------------------------------ | ----------------------------------------------------- |
| `FindNPC(npcName)`             | Finds an NPC by name                                  |
| `GetNPC(npcId)`                | Gets detailed information about an NPC by ID          |
| `GetNPCPosition(npc)`          | Gets the position of an NPC                           |
| `SetNPCPosition(npc, x, y, z)` | Sets the position of an NPC                           |
| `GetNPCRegion(npcId)`          | Gets the current region of an NPC                     |
| `GetNPCsInRegion(region)`      | Gets all NPCs in a specific region                    |
| `GetAllNPCs()`                 | Gets information about all NPCs in the game           |
| `GetAllNPCRegions()`           | Gets a list of all regions that NPCs are currently in |
| `IsNPCInRegion(npcId, region)` | Checks if an NPC is currently in a specific region    |

### Map Functions

| Function             | Description                              |
| -------------------- | ---------------------------------------- |
| `GetAllMapRegions()` | Gets a list of all available map regions |

### Helper Functions

| Function                  | Description                                  |
| ------------------------- | -------------------------------------------- |
| `Vector3(x, y, z)`        | Creates a new Vector3                        |
| `Vector3Distance(v1, v2)` | Calculates the distance between two Vector3s |

## API Implementation Checklist

This checklist tracks the current state of the ScheduleLua API implementation. Use this to understand what features are available and what's planned for future updates. This checklist is subject to changes and there will be more things added once more bindings get finished.

### Player System

#### Basic Player Information
- [x] Get player state (health, energy, position, etc.)
- [x] Get player position
- [x] Get player region
- [x] Check if player is in specific region
- [x] Get player name
- [x] Get player movement state (sprinting, crouching, etc.)

#### Player Stats
- [x] Get/set player health
- [x] Get/set player energy
- [x] Get/set player money
- [x] Monitor health changes
- [x] Monitor energy changes
- [x] Monitor money changes

#### Player Movement
- [x] Teleport player
- [x] Set player position
- [x] Get player movement speed
- [x] Check if player is grounded
- [ ] Force player movement
- [ ] Set player movement speed
- [ ] Apply forces to player

### NPC System

#### Basic NPC Information
- [x] Find NPC by ID
- [x] Get NPC position
- [x] Get NPC region
- [x] Get NPC name
- [x] Get NPC state (conscious, moving, etc.)
- [ ] Get NPC schedule
- [ ] Get NPC relationships

#### NPC Management
- [x] Get all NPCs in region
- [x] Get all NPCs in game
- [x] Check if NPC is in region
- [ ] Spawn NPC
- [ ] Remove NPC
- [ ] Modify NPC behavior
- [ ] Set NPC schedule

### Inventory System

#### Basic Inventory
- [ ] Get inventory slot count
- [ ] Get item at slot
- [ ] Add item to inventory
- [ ] Remove item from inventory
- [ ] Get item properties
- [ ] Use item
- [ ] Drop item
- [ ] Sort inventory

#### Advanced Inventory
- [ ] Get item durability
- [ ] Repair item
- [ ] Combine items
- [ ] Split item stack
- [ ] Get item weight
- [ ] Check inventory weight limit

### Time System

#### Basic Time
- [x] Get current game time
- [x] Get current day
- [x] Check if night time
- [x] Format time values
- [ ] Set game time
- [ ] Set game day
- [ ] Pause time
- [ ] Speed up time

#### Time Events
- [ ] Day change events
- [ ] Time change events
- [ ] Sleep start/end events
- [ ] Weather change events
- [ ] Season change events

### Map/World System

#### Regions
- [x] Get all map regions
- [x] Get player region
- [x] Get NPC region
- [ ] Create custom region
- [ ] Modify region properties
- [ ] Get region weather
- [ ] Get region time

#### World Interaction
- [ ] Spawn objects
- [ ] Remove objects
- [ ] Modify terrain
- [ ] Create buildings
- [ ] Modify environment
- [ ] Create custom locations

### UI System

#### Basic UI
- [ ] Create custom UI elements
- [ ] Show/hide UI elements
- [ ] Modify UI layout
- [ ] Create custom HUD elements
- [ ] Show notifications
- [ ] Create menus

#### Advanced UI
- [ ] Create custom windows
- [ ] Handle UI input
- [ ] Create custom buttons
- [ ] Create custom sliders
- [ ] Create custom text fields
- [ ] Create custom images

### Quest System

#### Basic Quests
- [ ] Create quests
- [ ] Start quests
- [ ] Complete quests
- [ ] Get quest status
- [ ] Get quest objectives
- [ ] Modify quest progress

#### Advanced Quests
- [ ] Create branching quests
- [ ] Create timed quests
- [ ] Create repeatable quests
- [ ] Create quest chains
- [ ] Create quest rewards
- [ ] Create quest conditions

### Economy System

#### Basic Economy
- [ ] Get player money
- [ ] Add/remove player money
- [ ] Get shop inventory
- [ ] Buy/sell items
- [ ] Get item prices
- [ ] Modify item prices

#### Advanced Economy
- [ ] Create shops
- [ ] Modify shop inventory
- [ ] Create trading system
- [ ] Create economy events
- [ ] Create market fluctuations
- [ ] Create custom currencies

### Weather System

#### Basic Weather
- [ ] Get current weather
- [ ] Set weather
- [ ] Get weather forecast
- [ ] Create weather effects
- [ ] Modify weather intensity
- [ ] Create custom weather

#### Advanced Weather
- [ ] Create weather patterns
- [ ] Create weather zones
- [ ] Create weather events
- [ ] Modify weather duration
- [ ] Create weather transitions
- [ ] Create weather effects

### Modding Tools

#### Development Tools
- [x] Hot reloading
- [x] Error logging
- [x] Script isolation
- [ ] Debug console
- [ ] Performance monitoring
- [ ] Script profiling

#### Configuration
- [x] Mod preferences
- [x] Script settings
- [ ] Save/load configuration
- [ ] Create custom settings
- [ ] Modify game settings
- [ ] Create presets

### Event System

#### Basic Events
- [x] Player events
- [x] Time events
- [x] Scene events
- [ ] Command events
- [ ] UI events
- [ ] Inventory events

#### Advanced Events
- [ ] Custom events
- [ ] Event conditions
- [ ] Event chains
- [ ] Event priorities
- [ ] Event cancellation
- [ ] Event scheduling

### Utility Functions

#### Basic Utilities
- [x] Logging
- [x] Vector operations
- [x] Table operations
- [ ] String operations
- [ ] Math operations
- [ ] Time operations

#### Advanced Utilities
- [ ] File operations
- [ ] Network operations
- [ ] Data persistence
- [ ] Random generation
- [ ] Path finding
- [ ] Collision detection

## Events

Your scripts can define these functions to handle game events:

| Event                                  | Parameters                       | Description                                        |
| -------------------------------------- | -------------------------------- | -------------------------------------------------- |
| `Initialize()`                         | None                             | Called when a script is first loaded               |
| `OnSceneLoaded(sceneName)`             | sceneName: string                | Called when a Unity scene is loaded                |
| `OnPlayerReady()`                      | None                             | Called when the player is fully initialized        |
| `OnCommand(command)`                   | command: string                  | Called when a command is processed                 |
| `OnDayChanged(dayName)`                | dayName: string                  | Called when the day changes                        |
| `OnTimeChanged(time)`                  | time: number                     | Called when the hour changes                       |
| `OnSleepStart()`                       | None                             | Called when the player starts sleeping             |
| `OnSleepEnd()`                         | None                             | Called when the player wakes up                    |
| `OnPlayerMoneyChanged(amount)`         | amount: number                   | Called when the player's money changes             |
| `OnPlayerHealthChanged(health)`        | health: number                   | Called when the player's health changes            |
| `OnPlayerEnergyChanged(energy)`        | energy: number                   | Called when the player's energy changes            |
| `OnItemAdded(itemName, amount)`        | itemName: string, amount: number | Called when an item is added to inventory          |
| `OnItemRemoved(itemName, amount)`      | itemName: string, amount: number | Called when an item is removed from inventory      |
| `OnPlayerMovedSignificantly(position)` | position: Vector3                | Called when the player moves significantly         |
| `OnNPCInteraction(npcName)`            | npcName: string                  | Called when the player interacts with an NPC       |
| `Update()`                             | None                             | Called every frame for continuous script execution |

## Example Script

```lua
-- ScheduleLua Example Script
-- This script demonstrates the API available for modding ScheduleOne

-- Print a header for our script
Log("Example script loaded!")

-- Flag to track if we've run the main example code
local hasRunMainExample = false

-- This function gets called whenever a scene is loaded
function OnSceneLoaded(sceneName)
    Log("Scene loaded: " .. sceneName)
end

-- This function gets called once when the player is fully loaded and ready
function OnPlayerReady()
    Log("Player is now ready!")
    
    if not hasRunMainExample then
        hasRunMainExample = true
        RunMainExample()
    end
end

-- Function to run all the example code that requires the Main scene
function RunMainExample()
    Log("Running main example code...")
    
    -------------------------
    -- Player API Examples --
    -------------------------
    
    -- Get the player's full state (position, health, movement info, etc)
    local playerState = GetPlayerState()
    if playerState then
        -- Safely access player properties with nil checks
        local playerName = playerState.playerName or "Unknown"
        local health = playerState.health or 0
        local maxHealth = playerState.maxHealth or 100
        local isAlive = playerState.isAlive or false
        
        Log("Player name: " .. playerName)
        Log("Player health: " .. health .. "/" .. maxHealth)
        Log("Player is alive: " .. tostring(isAlive))
        
        -- Check if player is sprinting
        if playerState.isSprinting then
            Log("Player is sprinting")
        end
    else
        Log("Player not found or not initialized")
    end
    
    -- Get just the player's position
    local pos = GetPlayerPosition()
    if pos then
        Log("Player position: X=" .. pos.x .. ", Y=" .. pos.y .. ", Z=" .. pos.z)
    else
        Log("Could not get player position")
    end
    
    -- Get the player's current region
    local region = GetPlayerRegion()
    Log("Player is in region: " .. (region or "Unknown"))
    
    -- Example of setting player health (commented out for safety)
    -- SetPlayerHealth(100) -- Sets player health to 100
    
    -- Example of teleporting the player (commented out for safety)
    -- TeleportPlayer(100, 10, 100) -- Teleports player to X=100, Y=10, Z=100
    
    -----------------------
    -- NPC API Examples --
    -----------------------
    
    -- Get all NPCs in the game
    local allNPCs = GetAllNPCs()
    if allNPCs then
        Log("Found " .. #allNPCs .. " NPCs in the game")
        
        -- Loop through the first 3 NPCs (to avoid spam)
        local maxToShow = math.min(3, #allNPCs)
        for i=1, maxToShow do
            local npc = allNPCs[i]
            Log("NPC " .. i .. ": " .. npc.fullName .. " (ID: " .. npc.id .. ")")
        end
    end
    
    -- Get all NPC regions to see what regions exist
    local allRegions = GetAllNPCRegions()
    if allRegions then
        local regionsList = ""
        for i=1, #allRegions do
            if i > 1 then
                regionsList = regionsList .. ", "
            end
            regionsList = regionsList .. allRegions[i]
        end
        Log("Available NPC regions: " .. regionsList)
        
        -- Try to get NPCs in the first detected region
        if #allRegions > 0 then
            local firstRegion = allRegions[1]
            local npcsInRegion = GetNPCsInRegion(firstRegion)
            Log("Found " .. #npcsInRegion .. " NPCs in " .. firstRegion)
        end
    end
    
    -- Try several common region names
    local regionsToTry = {"Town", "Downtown", "City", "Village", "Central", "Main"}
    for _, regionName in ipairs(regionsToTry) do
        local npcsInRegion = GetNPCsInRegion(regionName)
        if npcsInRegion and #npcsInRegion > 0 then
            Log("Found " .. #npcsInRegion .. " NPCs in " .. regionName)
        end
    end
    
    -- Get a specific NPC by ID (using "npc_mayor" as an example)
    local exampleNpcId = "npc_mayor"
    local npc = GetNPC(exampleNpcId)
    if npc then
        Log("Found NPC: " .. npc.fullName)
        Log("NPC position: X=" .. npc.position.x .. ", Y=" .. npc.position.y .. ", Z=" .. npc.position.z)
        Log("NPC is in region: " .. npc.region)
        Log("NPC is conscious: " .. tostring(npc.isConscious))
    else
        Log("Could not find NPC with ID: " .. exampleNpcId)
        
        -- Show first NPC ID as a fallback
        if allNPCs and #allNPCs > 0 then
            Log("Try using this NPC ID instead: " .. allNPCs[1].id)
        end
    end
    
    -- Check if an NPC is in a specific region
    local isInRegion = IsNPCInRegion(exampleNpcId, "Town")
    Log("Is " .. exampleNpcId .. " in Town? " .. tostring(isInRegion))
    
    -- Let the system know this script ran successfully
    Log("Example script completed successfully!")
end

-- Example of a simple gameplay function that uses the API
function IsPlayerNearNPC(npcId, distance)
    local npc = GetNPC(npcId)
    local playerPos = GetPlayerPosition()
    
    if not npc or not playerPos then
        return false
    end
    
    -- Calculate distance (simple 2D distance for example)
    local dx = npc.position.x - playerPos.x
    local dz = npc.position.z - playerPos.z
    local calculatedDistance = math.sqrt(dx*dx + dz*dz)
    
    return calculatedDistance <= distance
end

-- Return true to indicate successful execution
return true
```

## Contributing

Contributions to ScheduleLua are welcome! To contribute:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

### Roadmap & Planned Features

- UI creation API for custom interfaces
- Quest system integration
- Economy and trade functions
- Building and construction functions
- World manipulation and generation
- Multiplayer support (when available)

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- MelonLoader team for the mod loader
- MoonSharp project for the Lua interpreter
- Schedule I development team
- All contributors and the modding community