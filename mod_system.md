# ScheduleLua Mod System

The ScheduleLua Mod System allows you to create modular Lua mods with dependencies, versions, and exported functionality. This document explains how to create and use mods in ScheduleLua.

## Table of Contents

- [ScheduleLua Mod System](#schedulelua-mod-system)
  - [Table of Contents](#table-of-contents)
  - [Introduction](#introduction)
  - [Folder Structure](#folder-structure)
  - [Manifest File](#manifest-file)
  - [Creating a Mod](#creating-a-mod)
  - [Using Mods](#using-mods)
    - [Exporting and Importing Functions](#exporting-and-importing-functions)
  - [Mod Lifecycle](#mod-lifecycle)
  - [Best Practices](#best-practices)
  - [Example Mod](#example-mod)

## Introduction

The ScheduleLua Mod System extends the existing script loading system to support:

1. Folder-based mods with manifest.json for metadata
2. Dependency management between mods
3. Load ordering
4. Function exports/imports between mods
5. Hot reloading of mod scripts

## Folder Structure

The mod system uses the existing scripts directory. Your mods should be placed in the ScheduleLua/Scripts directory, with each mod in its own folder:

```
Mods/
  ScheduleLua/
    Scripts/
      my_mod/                  # Mod folder
        manifest.json          # Required: Mod metadata
        init.lua               # Required: Main entry point
        utils.lua              # Optional: Additional scripts
        features/              # Optional: Subdirectories for organization
          feature1.lua
          feature2.lua
      single_script.lua        # Still supported but might be deprecated in favor of mod system: Individual scripts
```

## Manifest File

Each mod must have a `manifest.json` file in its root folder with the following structure:

```json
{
  "name": "My Mod",                        // Display name
  "version": "1.0.0",                      // Semantic version
  "description": "Description of the mod", // Short description
  "author": "Your Name",                   // Author name
  "main": "init.lua",                      // Main script (default: init.lua)
  "files": [                               // Additional scripts to load
    "utils.lua",
    "features/feature1.lua",
    "features/feature2.lua"
  ],
  "dependencies": [                        // Other mods this mod depends on
    "other_mod"
  ],
  "api_version": "0.1.2"                   // ScheduleLua API version
}
```

## Creating a Mod

To create a mod:

1. Create a folder in the `Mods/ScheduleLua/Scripts` directory.
2. Create a `manifest.json` file with your mod's metadata.
3. Create an `init.lua` file as the main entry point.
4. Add any additional script files and list them in the manifest.
5. Optionally create subdirectories for organization.

Example structure:

```
farming_mod/
  manifest.json
  init.lua
  utils.lua
  crops/
    wheat.lua
    cotton.lua
```

## Using Mods

When your mod is loaded, two special variables are set in the Lua environment:

- `__MOD_NAME`: The folder name of your mod
- `__MOD_PATH`: The full path to your mod folder

These can be used to reference files within your mod:

```lua
local imagePath = __MOD_PATH .. "/images/icon.png"
```

### Exporting and Importing Functions

Mods can export functions for other mods to use:

```lua
-- In your mod:
ExportFunction("calculateTaxes", function(amount)
    return amount * 0.15
end)

-- In another mod that depends on your mod:
local calculateTaxes = ImportFunction("tax_mod", "calculateTaxes")
if calculateTaxes then
    local tax = calculateTaxes(1000)
    Log("Tax amount: " .. tax)
end
```

Available functions:

- `GetMod(modName)`: Get a mod object by folder name
- `GetAllMods()`: Get information about all loaded mods
- `IsModLoaded(modName)`: Check if a mod is loaded
- `GetModExport(modName, exportName)`: Get an exported value from a mod
- `ExportFunction(name, function)`: Export a function from the current mod
- `ImportFunction(modName, functionName)`: Import a function from another mod

## Mod Lifecycle

Mods are loaded in this order:

1. Mods with lower `load_order` values are loaded first
2. Dependencies are loaded before the mods that depend on them
3. The `init.lua` file is loaded first, then additional files listed in the manifest
4. The `Initialize()` function is called if it exists
5. The `Update()` function is called every frame if it exists

## Best Practices

1. **Use namespaces**: Store your mod's data in a global table with a unique name to avoid conflicts:

```lua
MY_MOD = {
    version = "1.0.0",
    settings = {}
}
```

2. **Check for dependencies**:

```lua
if not IsModLoaded("required_mod") then
    LogError("This mod requires 'required_mod' to be installed")
    return
end
```

3. **Use OnRegistryReady** for initialization that requires the game registry:

```lua
OnRegistryReady(function()
    -- Safe to access registry functions here
    InitializeItems()
end)
```

4. **Organize your code** by splitting it into multiple files by functionality.

5. **Document your exports** so other mod authors know what functions are available.

## Example Mod

The Resources directory contains an example mod that demonstrates these concepts. Review the code to see how to structure your own mods. 