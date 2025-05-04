-- PlayerSettings/settings.lua
-- Contains functions for managing player settings

local Settings = {}

-- Initialize settings with default values
function Settings.initialize()
    -- Define configuration values with defaults and descriptions
    DefineConfigValue("maxHealth", 100, "Maximum player health")
    DefineConfigValue("moveSpeed", 10.0, "Player movement speed")
    DefineConfigValue("startingMoney", 500, "Starting money for the player")
    DefineConfigValue("difficultyLevel", "normal", "Game difficulty (easy, normal, hard)")
    DefineConfigValue("enableNotifications", true, "Enable in-game notifications")
    DefineConfigValue("favoriteItems", {"Pistol", "Bandage", "Water"}, "List of favorite items")
    
    -- Define more complex settings with nested tables
    DefineConfigValue("keybinds", {
        inventory = "Tab",
        sprint = "Shift",
        interact = "E",
        reload = "R"
    }, "Keyboard bindings for actions")
    
    -- Load the config (happens automatically, but we can get it as a table)
    local config = GetModConfig()
    Log("Player Settings loaded with difficulty: " .. config.difficultyLevel)
end

-- Apply the current settings to the game
function Settings.applySettings()
    local config = GetModConfig()
    
    Log("Applying player settings...")
    Log("  Max Health: " .. config.maxHealth)
    Log("  Move Speed: " .. config.moveSpeed)
    Log("  Starting Money: " .. config.startingMoney)
    Log("  Difficulty: " .. config.difficultyLevel)
    Log("  Notifications: " .. tostring(config.enableNotifications))
    
    -- You would typically call game API functions here to apply these settings
    -- Example (pseudo-code):
    -- Game.Player.SetMaxHealth(config.maxHealth)
    -- Game.Player.SetMoveSpeed(config.moveSpeed)
    -- etc.
end

-- Update a setting by key and value
function Settings.updateSetting(key, value)
    if HasConfigKey(key) then
        SetConfigValue(key, value)
        SaveModConfig() -- Save changes to disk
        Log("Updated setting: " .. key .. " = " .. tostring(value))
        return true
    else
        Log("Setting key not found: " .. key)
        return false
    end
end

-- Reset all settings to defaults
function Settings.resetToDefaults()
    -- For this example, we'll just update a few values back to defaults
    SetConfigValue("maxHealth", 100)
    SetConfigValue("moveSpeed", 10.0)
    SetConfigValue("startingMoney", 500)
    SetConfigValue("difficultyLevel", "normal")
    SetConfigValue("enableNotifications", true)
    
    SaveModConfig()
    Log("All settings reset to defaults")
end

-- List all available settings
function Settings.listAllSettings()
    local keys = GetConfigKeys()
    local config = GetModConfig()
    
    Log("Available settings:")
    for i, key in ipairs(keys) do
        local value = config[key]
        if type(value) == "table" then
            Log("  " .. key .. ": [table]")
        else
            Log("  " .. key .. ": " .. tostring(value))
        end
    end
end

return Settings 