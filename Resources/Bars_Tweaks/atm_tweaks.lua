--[[
    Bars_Tweaks ATM Tweaks Module
    Allows changing the ATM deposit limit in the game
]]

-- Create a module table
local ATMTweaks = {}

-- Require the menu module for integration
local Menu = require("menu")

-- Module configuration
local config = {
    enabled = false,
    currentPreset = "Medium",
    originalLimit = nil,    -- Will store the original limit when first enabled
    presetLimits = {
        ["Default"] = 10000.0,
        ["Medium"] = 25000.0, 
        ["High"] = 50000.0,
        ["Very High"] = 100000.0,
        ["Unlimited"] = 999999.0
    }
}

-- Debug logging
local debugLogging = _G.DEBUG_LOGGING or false

local isInitialized = false
local currentScene = "Menu"

-- Initialize the ATM tweaks
function ATMTweaks.Initialize()
    if isInitialized then return end
    
    Log("Initializing Bars_Tweaks ATM Tweaks...")
    
    -- Store the original limit when we first initialize
    if GetATMDepositLimit then
        config.originalLimit = GetATMDepositLimit()
        Log("Original ATM deposit limit: " .. FormatMoney(config.originalLimit))
        
        if _G.DEBUG_LOGGING then
            Log("DEBUG: ATM Tweaks initialized with preset: " .. config.currentPreset)
            Log("DEBUG: Available ATM presets: " .. table.concat(GetTableKeys(config.presetLimits), ", "))
        end
    end
    
    isInitialized = true
end

function ATMTweaks.OnSceneLoaded(sceneName)
    currentScene = sceneName
    if sceneName == "Menu" then
        config.enabled = false
    end
end

-- Enable ATM tweaks functionality
function ATMTweaks.Enable()
    if not isInitialized then
        ATMTweaks.Initialize()
    end
    
    -- Verify if required functions exist
    if not GetATMDepositLimit or not SetATMDepositLimit then
        Log("ATM functions not available. ATM tweaks cannot be enabled.")
        return
    end
    
    -- Store original limit if not already stored
    if not config.originalLimit then
        config.originalLimit = GetATMDepositLimit()
    end
    
    -- Apply the selected preset
    local limitToApply = config.presetLimits[config.currentPreset]
    if SetATMDepositLimit(limitToApply) then
        config.enabled = true
        Menu.SetTweakStatus("atm", true)
        
        Log("ATM tweaks enabled with preset: " .. config.currentPreset)
        ShowNotificationWithIcon("Bars Tweaks", "ATM limit: " .. config.currentPreset .. " (" .. FormatMoney(limitToApply) .. ")", "icon.png")
    else
        Log("Failed to apply ATM deposit limit")
        ShowNotificationWithIcon("Bars Tweaks", "Failed to apply ATM limit", "icon.png")
    end
end

-- Disable ATM tweaks functionality
function ATMTweaks.Disable()
    -- Restore original limit
    if config.originalLimit and SetATMDepositLimit then
        SetATMDepositLimit(config.originalLimit)
    end
    
    config.enabled = false
    Menu.SetTweakStatus("atm", false)
    
    Log("ATM tweaks disabled")
    ShowNotificationWithIcon("Bars Tweaks", "ATM limit restored", "icon.png")
end

-- Toggle ATM tweaks enabled/disabled state
function ATMTweaks.ToggleTweaks()
    if config.enabled then
        ATMTweaks.Disable()
    else
        ATMTweaks.Enable()
    end
end

-- Get current ATM preset
function ATMTweaks.GetCurrentPreset()
    return config.currentPreset
end

-- Change the ATM limit preset
function ATMTweaks.SetPreset(presetName)
    if not config.presetLimits[presetName] then
        Log("Invalid preset: " .. presetName)
        return false
    end
    
    config.currentPreset = presetName
    Log("ATM limit preset changed to: " .. presetName)
    
    -- If already enabled, apply the new preset immediately
    if config.enabled then
        local limitToApply = config.presetLimits[presetName]
        if SetATMDepositLimit(limitToApply) then
            Log("Applied new ATM limit: " .. FormatMoney(limitToApply))
            ShowNotificationWithIcon("Bars Tweaks", "ATM limit: " .. presetName, "icon.png")
            return true
        else
            Log("Failed to apply new ATM limit")
            return false
        end
    end
    
    return true
end

-- Register commands
function ATMTweaks.OnConsoleReady()
    -- Command to change ATM limit preset
    RegisterCommand(
        "atm_preset",
        "Set the ATM limit preset",
        "atm_preset <preset>",
        function(args)
            if #args == 0 then
                -- Display available presets
                Log("Current ATM limit preset: " .. config.currentPreset)
                Log("Available presets:")
                for name, limit in pairs(config.presetLimits) do
                    Log("  - " .. name .. ": " .. FormatMoney(limit))
                end
                return
            end
            
            local preset = args[1]
            -- Try to match preset name (case insensitive)
            local matchedPreset = nil
            for name, _ in pairs(config.presetLimits) do
                if string.lower(name) == string.lower(preset) or 
                   string.lower(name:gsub(" ", "")) == string.lower(preset) then
                    matchedPreset = name
                    break
                end
            end
            
            if matchedPreset then
                if ATMTweaks.SetPreset(matchedPreset) then
                    Log("ATM limit preset set to: " .. matchedPreset .. " (" .. 
                        FormatMoney(config.presetLimits[matchedPreset]) .. ")")
                else
                    Log("Failed to set ATM limit preset")
                end
            else
                Log("Invalid preset. Available presets:")
                for name, limit in pairs(config.presetLimits) do
                    Log("  - " .. name .. ": " .. FormatMoney(limit))
                end
            end
        end
    )
    
    -- Command to add a custom preset
    RegisterCommand(
        "atm_custom",
        "Set a custom ATM limit",
        "atm_custom <amount>",
        function(args)
            if #args == 0 then
                Log("Usage: /atm_custom <amount> - Sets a custom ATM deposit limit")
                return
            end
            
            local amount = tonumber(args[1])
            if not amount or amount < 0 then
                Log("Invalid amount. Please specify a positive number.")
                return
            end
            
            -- Add/update the custom preset
            config.presetLimits["Custom"] = amount
            
            -- Apply it if enabled
            if ATMTweaks.SetPreset("Custom") and config.enabled then
                Log("Custom ATM limit set to: " .. FormatMoney(amount))
                ShowNotificationWithIcon("Bars Tweaks", "ATM limit: " .. FormatMoney(amount), "icon.png")
            else
                Log("Custom preset created but not applied (tweaks not enabled)")
            end
        end
    )
    
    Log("ATM tweaks module commands registered")
end

-- Clean up when the mod is unloaded
function ATMTweaks.Shutdown()
    if config.enabled then
        ATMTweaks.Disable()
    end
    
    Log("ATM tweaks module shutdown")
end

-- Helper function to get table keys
function GetTableKeys(tbl)
    local keys = {}
    for k, _ in pairs(tbl) do
        table.insert(keys, k)
    end
    return keys
end

return ATMTweaks 