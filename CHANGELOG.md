# Changelog

All notable changes to ScheduleLua will be documented in this file.

## [0.1.0] - 2025-04-13

### Initial Release

First beta release of ScheduleLua, a Lua modding framework for Schedule I that enables custom gameplay mechanics and automation.

### Added

- Core framework integration with MelonLoader and MoonSharp Lua interpreter
- Hot reloading of scripts for rapid development
- Basic logging system (Log, LogWarning, LogError)
- Console command registration system

### Implemented APIs

#### Player System
- Basic player information (position, region, name)
- Player stats (health, energy, money)
- Player movement (teleport, set position)

#### NPC System
- Basic NPC information (position, region, name)
- Basic NPC management (get all NPCs, find NPC by name)

#### Time System
- Basic time functions (get game time, get game day)
- Time utilities (format time, check if night time)

#### Map/World System
- Region functions (get all regions, check if player is in region)

### Events
- OnSceneLoaded
- OnPlayerReady
- OnGameDayChanged
- OnGameTimeChanged
- OnPlayerHealthChanged
- OnPlayerEnergyChanged
- OnSleepStart
- OnSleepEnd

### Configuration
- EnableHotReload option
- LogScriptErrors option

### Known Limitations
- Only features documented in example scripts are guaranteed to work
- Advanced APIs (inventory, quests, economy, weather) not fully implemented yet
- Some functions may not be reliable after game updates 

### Note
- You can find full documentation on the [Wiki Page](https://ifbars.github.io/ScheduleLua-Docs/)