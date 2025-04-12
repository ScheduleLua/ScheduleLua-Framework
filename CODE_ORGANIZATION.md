# ScheduleLua Code Organization

This document outlines the recommended folder structure and code organization for the ScheduleLua framework, aligned with the game's architecture.

## Recommended Folder Structure

```
ScheduleLua/
├── API/                    # API modules
│   ├── Core/               # Core game systems
│   │   ├── GameManagerAPI.cs    # Game state and management
│   │   ├── TimeAPI.cs           # Time and scheduling
│   │   └── SaveAPI.cs           # Save/load functionality
│   ├── Player/             # Player-related systems
│   │   ├── PlayerAPI.cs         # Player state and actions
│   │   ├── InventoryAPI.cs      # Player inventory
│   │   └── MovementAPI.cs       # Player movement
│   ├── NPC/                # NPC-related systems
│   │   ├── NPCAPI.cs            # NPC state and actions
│   │   ├── ScheduleAPI.cs       # NPC scheduling
│   │   └── RelationshipAPI.cs   # NPC relationships
│   ├── World/              # World-related systems
│   │   ├── MapAPI.cs            # Map and locations
│   │   ├── BuildingAPI.cs       # Buildings and interiors
│   │   └── WeatherAPI.cs        # Weather and environment
│   ├── Economy/            # Economy-related systems
│   │   ├── MoneyAPI.cs          # Money and transactions
│   │   ├── ShopAPI.cs           # Shops and trading
│   │   └── ProductAPI.cs        # Products and items
│   └── UI/                 # UI-related systems
│       ├── UIManagerAPI.cs      # UI management
│       ├── MenuAPI.cs           # Menu interactions
│       └── HUDAPI.cs            # HUD elements
├── Core/                   # Core framework components
│   ├── LuaManager.cs            # Core script loader and manager
│   ├── EventManager.cs          # Event handling system
│   ├── ConfigManager.cs         # Configuration management
│   └── CommandHandler.cs        # Command processing
├── Utils/                  # Utility classes
│   ├── Extensions.cs            # Extension methods
│   ├── LuaConverter.cs          # Conversion between C# and Lua types
│   └── PerformanceMonitor.cs    # Performance tracking
├── Scripting/              # Scripting infrastructure
│   ├── LuaScript.cs             # Script representation
│   ├── LuaEnvironment.cs        # Lua environment setup
│   └── HotReload.cs             # Hot reload functionality
├── Resources/              # Embedded resources
│   ├── DefaultScripts/          # Default script templates
│   └── Documentation/           # Embedded documentation
└── Examples/               # Example scripts and implementations
    ├── BasicExample.cs          # Simple example of usage
    └── AdvancedExample.cs       # More complex example
```

## Key Components and Their Responsibilities

### API Layer

The API layer is organized to match the game's subsystems:

1. **Core Systems**
   - Game state management
   - Time and scheduling
   - Save/load functionality

2. **Player Systems**
   - Player state and actions
   - Inventory management
   - Movement and interaction

3. **NPC Systems**
   - NPC state and behavior
   - Scheduling and routines
   - Relationship management

4. **World Systems**
   - Map and location data
   - Building management
   - Weather and environment

5. **Economy Systems**
   - Money and transactions
   - Shop and trading
   - Product management

6. **UI Systems**
   - UI management
   - Menu interactions
   - HUD elements

### Core Components

- **LuaManager**: Central manager for script loading, unloading, and management
- **EventManager**: Handles event registration, triggering, and clean-up
- **ConfigManager**: Manages configuration settings via MelonPreferences
- **CommandHandler**: Processes in-game commands and routes them to scripts

### Scripting Infrastructure

- **LuaScript**: Represents a loaded Lua script with lifecycle methods
- **LuaEnvironment**: Sets up the Lua environment with sandboxing and protection
- **HotReload**: Handles file watching and script reloading

## Implementation Guidelines

1. **Namespace Organization**: Each API module should use the same namespace structure as the game
2. **Singleton Access**: Use the game's singleton pattern for accessing managers
3. **Event Integration**: Hook into the game's event system for notifications
4. **Error Handling**: Implement comprehensive error handling and logging
5. **Performance**: Optimize for performance, especially in frequently called methods

## Migration Plan

To migrate from the current implementation to this structure:

1. Create the new folder structure
2. Split LuaAPI.cs into the respective API modules
3. Update references throughout the codebase
4. Implement new functionality in the appropriate modules
5. Update documentation

## Adding New API Areas

When adding a new API area:

1. Create a new file in the appropriate API folder
2. Follow the existing naming and method patterns
3. Register the new methods in LuaManager
4. Document the new methods in the API reference
5. Add examples demonstrating the new functionality 