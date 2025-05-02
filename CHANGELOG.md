# Changelog

All notable changes to ScheduleLua will be documented in this file.

## [0.1.6] - 2025-04-21

## Fixed
- Issue with notification icons breaking across multiple lua mods

## Changed

- Entire rewrite of the API system to be more modular

## [0.1.5] - 2025-04-19

## Fixed
- Another case of the Player API issue in 0.1.3
- Fixed some keys not registering
- Fixed GetAllItems()
- Improved Lua Mod system

## Added
- Screen size functions
- Notification icons
- UI Storage Entities (e.g. Backpacks)
- MOD_VERSION global

## Changed
- Improved UI API

## [0.1.4] - 2025-04-18

## Fixed
- Better error logs
- Better documentation
- Bug fixes and overall better stability
- Custom key press bindings

## Added
- More inventory bindings
- More curfew bindings
- Explosion bindings
- Customize UI Styles Via Lua
- Work in progress Law bindings
- Export all item ids example lua script
- MOD_NAME global

## Changed
- Better dialogue bindings
- Cleanup of C# code

## [0.1.3] - 2025-04-16

### Fixed
- Issue where Player API did not handle scene changes properly, causing spammed warning logs

## [0.1.2] - 2025-04-16

### Changed
- Better inventory binding system
- Better registry bindings
- Major refactoring to the C# backend

### Added
- New "Lua Mod" system

## [0.1.1] - 2025-04-14

### Fixed
- Stabilized ATM withdraw limit patches
- Fixed bug where online balance would not update in the hotbar UI
- Fixed various areas in the documentation site

### Added
- Added `SCHEDULELUA_VERSION` binding for Lua scripts to access the mod's version

### Changed
- Removed redundant logging and code

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