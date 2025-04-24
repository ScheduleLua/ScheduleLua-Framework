--[[
    Bars_Tweaks
    Description: A QOL mod to enhance the Schedule 1 experience
    Author: Bars
    Version: 1.0.0
]]

-- Debug settings
local updateCheck = {
    lastLogTime = 0,
    logInterval = 10  -- Log debug status every 10 seconds
}

-- Module instances
local Menu = nil
local Backpack = nil
local ItemTweaks = nil
local AtmTweaks = nil

-- Function to log errors with nil checks
local function SafeLog(level, message)
    if not Log then
        -- We can't log that Log is missing, but at least we avoid errors
        return
    end
    
    if level == "error" and LogError then
        LogError(message)
    elseif level == "warning" and LogWarning then
        LogWarning(message)
    else
        Log(message)
    end
end

-- Function to safely load a module with error handling
local function SafeRequire(moduleName)
    local success, module = pcall(require, moduleName)
    if not success then
        SafeLog("error", "CRITICAL: Failed to load module '" .. moduleName .. "': " .. tostring(module))
        return nil
    end
    
    if _G.DEBUG_LOGGING then
        SafeLog("info", "DEBUG: Successfully loaded module '" .. moduleName .. "'")
    end
    
    return module
end

-- Register mod commands when console is ready
function OnConsoleReady()    
    SafeLog("info", "Console ready, registering commands...")
    
    -- Load modules if not already loaded
    if not Menu then Menu = SafeRequire("menu") end
    if not Backpack then Backpack = SafeRequire("backpack") end
    if not ItemTweaks then ItemTweaks = SafeRequire("item_tweaks") end
    if not AtmTweaks then AtmTweaks = SafeRequire("atm_tweaks") end
    
    -- Register debug command
    if RegisterCommand then
        RegisterCommand(
            "debug",
            "Toggles debug logging for all modules",
            "debug",
            function(args)
                -- Toggle global debug state
                _G.DEBUG_LOGGING = not _G.DEBUG_LOGGING
                
                -- Notify user
                SafeLog("info", "Debug logging " .. (_G.DEBUG_LOGGING and "enabled" or "disabled"))
                if ShowNotificationWithIcon then
                    ShowNotificationWithIcon("Bars Tweaks", "Debug logs " .. (_G.DEBUG_LOGGING and "enabled" or "disabled"), "icon.png")
                end
            end
        )
        SafeLog("info", "Registered 'debug' command")
    end
    
    -- Validate modules were loaded successfully
    if not Backpack then
        SafeLog("error", "CRITICAL: Failed to load backpack module for command registration")
    else
        -- Register a shortcut command to toggle backpack
        if RegisterCommand then
            RegisterCommand("bp", "Toggles the enhanced backpack feature", "", function()
                Backpack.ToggleTweaks()
            end)
            SafeLog("info", "Registered 'bp' command")
        else
            SafeLog("error", "CRITICAL: RegisterCommand function is missing, cannot register 'bp'")
        end
    end
    
    -- Register item tweaks commands
    if not ItemTweaks then
        SafeLog("error", "CRITICAL: Failed to load item tweaks module for command registration")
    else
        -- Register a shortcut command to toggle item tweaks
        if RegisterCommand then
            RegisterCommand("it", "Toggles the item stack limit tweaks", "", function()
                ItemTweaks.ToggleTweaks()
            end)
            SafeLog("info", "Registered 'it' command")
        else
            SafeLog("error", "CRITICAL: RegisterCommand function is missing, cannot register 'it'")
        end
        
        if ItemTweaks.OnConsoleReady then
            ItemTweaks.OnConsoleReady()
        else
            SafeLog("warning", "WARNING: ItemTweaks.OnConsoleReady function is missing")
        end
    end
    
    -- Register ATM tweaks commands
    if not AtmTweaks then
        SafeLog("error", "CRITICAL: Failed to load ATM tweaks module for command registration")
    else
        -- Register a shortcut command to toggle ATM tweaks
        if RegisterCommand then
            RegisterCommand("atm", "Toggles the ATM limit tweaks", "", function()
                AtmTweaks.ToggleTweaks()
            end)
            SafeLog("info", "Registered 'atm' command")
        else
            SafeLog("error", "CRITICAL: RegisterCommand function is missing, cannot register 'atm'")
        end
        
        if AtmTweaks.OnConsoleReady then
            AtmTweaks.OnConsoleReady()
        else
            SafeLog("warning", "WARNING: AtmTweaks.OnConsoleReady function is missing")
        end
    end
    
    SafeLog("info", MOD_NAME .. ": Commands registered")
end

-- Initialize function - called when script is loaded
function Initialize()
    SafeLog("info", MOD_NAME .. ": Initializing...")
    
    -- Load all modules
    Menu = SafeRequire("menu")
    Backpack = SafeRequire("backpack")
    ItemTweaks = SafeRequire("item_tweaks")
    AtmTweaks = SafeRequire("atm_tweaks")
    
    -- Validate API functions
    if _G.DEBUG_LOGGING then
        ValidateGlobalAPI()
    end
    
    -- Initialize modules
    if Menu and Menu.Initialize then
        Menu.Initialize()
    else
        SafeLog("error", "CRITICAL: Menu module or Initialize function missing")
    end
    
    SafeLog("info", MOD_NAME .. ": Initialization complete")
    return true
end

-- Validate global API functions needed by the mod
function ValidateGlobalAPI()
    local requiredFunctions = {
        "Log", "LogError", "LogWarning", "RegisterCommand",
        "IsKeyPressed", "GetGameTime", "ShowNotificationWithIcon",
        "Wait"
    }
    
    if _G.DEBUG_LOGGING then
        SafeLog("info", "DEBUG: Validating global API functions...")
    end
    
    local missingCount = 0
    
    for _, funcName in ipairs(requiredFunctions) do
        if _G[funcName] == nil then
            SafeLog("error", "CRITICAL: Required global function missing: " .. funcName)
            missingCount = missingCount + 1
        end
    end
    
    if missingCount == 0 and _G.DEBUG_LOGGING then
        SafeLog("info", "DEBUG: All required global API functions are available")
    else
        SafeLog("error", "CRITICAL: " .. missingCount .. " required global functions are missing")
    end
end

function Update()
    local currentTime = GetGameTime and GetGameTime() or 0
    
    -- Periodically log update status for debugging
    if _G.DEBUG_LOGGING and currentTime - updateCheck.lastLogTime >= updateCheck.logInterval then
        SafeLog("info", "DEBUG: Update cycle - Modules loaded: Menu=" .. tostring(Menu ~= nil) .. 
                ", Backpack=" .. tostring(Backpack ~= nil) .. 
                ", ItemTweaks=" .. tostring(ItemTweaks ~= nil) .. 
                ", AtmTweaks=" .. tostring(AtmTweaks ~= nil))
        updateCheck.lastLogTime = currentTime
    end
    
    -- Call update functions for modules that have them
    if Backpack and Backpack.Update then
        local success, error = pcall(Backpack.Update)
        if not success and _G.DEBUG_LOGGING then
            SafeLog("error", "ERROR in Backpack.Update(): " .. tostring(error))
        end
    end
    
    if Menu and Menu.Update then
        local success, error = pcall(Menu.Update)
        if not success and _G.DEBUG_LOGGING then
            SafeLog("error", "ERROR in Menu.Update(): " .. tostring(error))
        end
    end
end

-- Called when player is ready
function OnPlayerReady()
    SafeLog("info", MOD_NAME .. ": Player ready")
    
    -- Display welcome message
    SafeLog("info", "===================================")
    SafeLog("info", MOD_NAME .. " mod loaded successfully!")
    SafeLog("info", "Press = to open the tweaks menu")
    SafeLog("info", "Type 'bp' to toggle the enhanced backpack")
    SafeLog("info", "Type 'it' to toggle the item stack tweaks")
    SafeLog("info", "Type 'atm' to toggle the ATM limit tweaks")
    SafeLog("info", "===================================")
    
    -- Check if required modules are available
    if not Menu or not Backpack or not ItemTweaks or not AtmTweaks then
        SafeLog("warning", "WARNING: Not all modules are loaded. Some features may not work.")
        
        if not Menu then SafeLog("error", "CRITICAL: Menu module is missing") end
        if not Backpack then SafeLog("error", "CRITICAL: Backpack module is missing") end
        if not ItemTweaks then SafeLog("error", "CRITICAL: ItemTweaks module is missing") end
        if not AtmTweaks then SafeLog("error", "CRITICAL: ATM tweaks module is missing") end
    end
end

-- Called when Registry is ready
function OnRegistryReady()
    SafeLog("info", MOD_NAME .. ": Registry ready")
    
    if ItemTweaks and ItemTweaks.Initialize then
        local success, error = pcall(ItemTweaks.Initialize)
        if not success then
            SafeLog("error", "ERROR initializing ItemTweaks: " .. tostring(error))
        end
    else
        SafeLog("warning", "WARNING: ItemTweaks module or Initialize function missing")
    end
    
    if AtmTweaks and AtmTweaks.Initialize then
        local success, error = pcall(AtmTweaks.Initialize)
        if not success then
            SafeLog("error", "ERROR initializing AtmTweaks: " .. tostring(error))
        end
    else
        SafeLog("warning", "WARNING: AtmTweaks module or Initialize function missing")
    end
end

-- Called when scene is loaded
function OnSceneLoaded(sceneName)
    SafeLog("info", MOD_NAME .. ": Scene loaded: " .. tostring(sceneName))
    
    -- Forward scene load events to our modules
    if Menu and Menu.OnSceneLoaded then
        local success, error = pcall(function() Menu.OnSceneLoaded(sceneName) end)
        if not success then
            SafeLog("error", "ERROR in Menu.OnSceneLoaded(): " .. tostring(error))
        end
    end
    
    if Backpack and Backpack.OnSceneLoaded then
        local success, error = pcall(function() Backpack.OnSceneLoaded(sceneName) end)
        if not success then
            SafeLog("error", "ERROR in Backpack.OnSceneLoaded(): " .. tostring(error))
        end
    end
    
    if ItemTweaks and ItemTweaks.OnSceneLoaded then
        local success, error = pcall(function() ItemTweaks.OnSceneLoaded(sceneName) end)
        if not success then
            SafeLog("error", "ERROR in ItemTweaks.OnSceneLoaded(): " .. tostring(error))
        end
    end
    
    if AtmTweaks and AtmTweaks.OnSceneLoaded then
        local success, error = pcall(function() AtmTweaks.OnSceneLoaded(sceneName) end)
        if not success then
            SafeLog("error", "ERROR in AtmTweaks.OnSceneLoaded(): " .. tostring(error))
        end
    end
end

-- Shutdown function
function Shutdown()
    SafeLog("info", MOD_NAME .. ": Shutting down...")
    
    -- Unregister commands
    if UnregisterCommand then
        UnregisterCommand("debug")
        UnregisterCommand("bp")
        UnregisterCommand("it")
        UnregisterCommand("atm")
        
        SafeLog("info", "Unregistered commands")
    end
    
    if Backpack and Backpack.Shutdown then
        local success, error = pcall(Backpack.Shutdown)
        if not success then
            SafeLog("error", "ERROR during Backpack.Shutdown(): " .. tostring(error))
        end
    end
    
    if ItemTweaks and ItemTweaks.Shutdown then
        local success, error = pcall(ItemTweaks.Shutdown)
        if not success then
            SafeLog("error", "ERROR during ItemTweaks.Shutdown(): " .. tostring(error))
        end
    end
    
    if AtmTweaks and AtmTweaks.Shutdown then
        local success, error = pcall(AtmTweaks.Shutdown)
        if not success then
            SafeLog("error", "ERROR during AtmTweaks.Shutdown(): " .. tostring(error))
        end
    end
    
    if Menu and Menu.Shutdown then
        local success, error = pcall(Menu.Shutdown)
        if not success then
            SafeLog("error", "ERROR during Menu.Shutdown(): " .. tostring(error))
        end
    end
end

SafeLog("info", MOD_NAME .. ": Main script loaded") 