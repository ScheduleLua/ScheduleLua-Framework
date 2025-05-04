-- PlayerSettings/init.lua
-- Main entry point for the Player Settings mod

-- Get the settings module
local Settings = require(MOD_PATH .. "/settings")

-- Initialize the mod when it's loaded
function Initialize()
    Log("Initializing Player Settings mod...")
    
    -- Initialize our configuration settings
    Settings.initialize()
    
    -- Apply the settings
    Settings.applySettings()
    
    Log("Player Settings mod initialized successfully!")
end

function OnConsoleReady()
    -- Register console commands for changing settings
    RegisterCommand("list_settings", "list_settings", "list_settings", function()
        Settings.listAllSettings()
    end)
    
    RegisterCommand("set_setting", "set_setting", "set_setting", function(key, value)
        if key == nil or value == nil then
            Log("Usage: set_setting <key> <value>")
            return
        end
        
        -- Try to convert the value to the appropriate type
        local convertedValue = value
        if value == "true" then
            convertedValue = true
        elseif value == "false" then
            convertedValue = false
        elseif tonumber(value) ~= nil then
            convertedValue = tonumber(value)
        end
        
        Settings.updateSetting(key, convertedValue)
        Settings.applySettings() -- Apply changes immediately
    end)
    
    RegisterCommand("reset_settings", "reset_settings", "reset_settings", function()
        Settings.resetToDefaults()
        Settings.applySettings() -- Apply changes immediately
        Log("All settings have been reset to defaults")
    end)
end

-- Add a menu item to the game's settings menu (if available)
-- This is just an example and would need to be adjusted to work with the actual game's UI system
function OnMenuOpen()
    -- Pseudo-code example:
    -- local settingsMenu = Game.UI.GetSettingsMenu()
    -- settingsMenu:AddCategory("Player Settings")
    -- Add UI controls for each setting...
    
    Log("Player Settings menu opened")
end

-- Register event callbacks (if the game supports them)
-- RegisterEventCallback("OnGameStart", Settings.applySettings)
-- RegisterEventCallback("OnMenuOpen", OnMenuOpen)

-- Return any functions or variables you want to expose to other mods
return {
    GetSettings = function() return GetModConfig() end,
    UpdateSetting = Settings.updateSetting,
    ResetSettings = Settings.resetToDefaults
} 