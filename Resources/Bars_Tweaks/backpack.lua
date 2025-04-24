--[[
    Bars_Tweaks Backpack Module
    Provides an enhanced backpack with more slots and features
]]

-- Create a module table
local Backpack = {}

-- Require the menu module for integration
local Menu = require("menu")

-- Module configuration
local config = {
    enabled = false,
    slotCount = 12,
    rowCount = 3,
    toggleCooldown = 0.15  -- Time in seconds between toggles
}

-- Storage entity reference
local backpackId = nil
local lastToggleTime = 0
local isInitialized = false
local currentScne = "Menu";

-- Track debug state
local keyPressCheck = {
    lastCheckTime = 0,
    checkInterval = 5, -- Log debug info every 5 seconds
    lastKeyPressTime = 0
}

-- Initialize the backpack
function Backpack.Initialize()
    if isInitialized then 
        if _G.DEBUG_LOGGING then
            Log("DEBUG: Backpack already initialized, skipping")
        end
        return 
    end
    
    Log("Initializing Bars_Tweaks Backpack...")
    
    -- Create the backpack storage entity
    backpackId = CreateStorageEntity("Bars Backpack", config.slotCount, config.rowCount)
    if backpackId then
        if _G.DEBUG_LOGGING then
            Log("DEBUG: Backpack created successfully with ID: " .. backpackId)
        end
    else
        LogError("CRITICAL: Failed to create backpack storage entity")
    end
    
    SetStorageSubtitle(backpackId, "Enhanced storage capacity")
    
    Log("Backpack created with ID: " .. backpackId)
    Log("Press B to open/close your enhanced backpack when enabled")
    
    isInitialized = true
end

function Backpack.OnSceneLoaded(sceneName)
    currentScne = sceneName
    if sceneName == "Menu" then
        config.enabled = false
    end
end

-- Enable backpack functionality
function Backpack.Enable()
    if not isInitialized then
        Backpack.Initialize()
    end
    
    config.enabled = true
    Menu.SetTweakStatus("backpack", true)
    if isInitialized then
        Log("Backpack setup complete")
        ShowNotificationWithIcon("Bars Tweaks Backpack", "Press B to open", "icon.png")
    end
end

-- Disable backpack functionality
function Backpack.Disable()
    config.enabled = false
    Menu.SetTweakStatus("backpack", false)
    
    -- Close the backpack if it's open
    if backpackId and IsStorageOpen(backpackId) then
        CloseStorageEntity(backpackId)
    end
    
    _backpackEnabled = false
    ShowNotificationWithIcon("Bars Tweaks", "Backpack disabled", "icon.png")
end

-- Toggle backpack enabled/disabled state
function Backpack.ToggleTweaks()
    if config.enabled then
        Backpack.Disable()
    else
        Backpack.Enable()
    end
end

-- Get current slot count
function Backpack.GetSlotCount()
    return config.slotCount
end

-- Set slot count for backpack
function Backpack.SetSlotCount(count)
    -- Validate the input
    count = tonumber(count)
    if not count or count < 4 then
        count = 4
    elseif count > 36 then
        count = 36
    end
    
    -- Update the configuration
    config.slotCount = count
    Log("Backpack slot count set to " .. count)
    
    -- If backpack is already created, recreate it
    if backpackId then
        -- Save current items if backpack is open
        local items = nil
        if IsStorageOpen(backpackId) then
            items = GetStorageItems(backpackId)
            CloseStorageEntity(backpackId)
        end
        
        -- Create new backpack with updated slot count
        backpackId = CreateStorageEntity("Bars Backpack", config.slotCount, config.rowCount)
        SetStorageSubtitle(backpackId, "Enhanced storage capacity")
        
        -- Restore items if we had any
        if items then
            for _, item in ipairs(items) do
                AddItemToStorage(backpackId, item.id, item.quantity)
            end
            OpenStorageEntity(backpackId)
        end
    end
    
    return true
end

-- Get current row count
function Backpack.GetRowCount()
    return config.rowCount
end

-- Set row count for backpack
function Backpack.SetRowCount(count)
    -- Validate the input
    count = tonumber(count)
    if not count or count < 1 then
        count = 1
    elseif count > 6 then
        count = 6
    end
    
    -- Update the configuration
    config.rowCount = count
    Log("Backpack row count set to " .. count)
    
    -- If backpack is already created, recreate it
    if backpackId then
        -- Save current items if backpack is open
        local items = nil
        if IsStorageOpen(backpackId) then
            items = GetStorageItems(backpackId)
            CloseStorageEntity(backpackId)
        end
        
        -- Create new backpack with updated row count
        backpackId = CreateStorageEntity("Bars Backpack", config.slotCount, config.rowCount)
        SetStorageSubtitle(backpackId, "Enhanced storage capacity")
        
        -- Restore items if we had any
        if items then
            for _, item in ipairs(items) do
                AddItemToStorage(backpackId, item.id, item.quantity)
            end
            OpenStorageEntity(backpackId)
        end
    end
    
    return true
end

function Backpack.Update()
    -- Only check for key press if backpack is enabled and initialized
    if _G.DEBUG_LOGGING then
        local currentTime = GetGameTime and GetGameTime() or 0
        if currentTime - keyPressCheck.lastCheckTime >= keyPressCheck.checkInterval then
            -- Use safe string conversion with nil checks for all values
            local enabledText = config and config.enabled ~= nil and tostring(config.enabled) or "nil"
            local backpackIdText = backpackId ~= nil and tostring(backpackId) or "nil"
            local sceneText = currentScne ~= nil and tostring(currentScne) or "nil"
            
            Log("DEBUG: Backpack Update check - enabled: " .. enabledText .. 
                ", backpackId: " .. backpackIdText .. 
                ", currentScene: " .. sceneText)
            keyPressCheck.lastCheckTime = currentTime
        end
    end

    -- Check all required conditions first and log appropriately
    if not config or not config.enabled then
        -- No need to spam log, only log occasionally
        return
    end
    
    if not backpackId then
        LogError("ERROR: backpackId is nil in Backpack.Update()")
        return
    end
    
    if currentScne ~= "Main" then
        -- No need to log this repeatedly
        return
    end
    
    -- Check that the IsKeyPressed function exists
    if not IsKeyPressed then
        LogError("CRITICAL: IsKeyPressed function is missing")
        return
    end
    
    -- Check for B key press to toggle backpack
    local keyPressed = IsKeyPressed("b")
    if keyPressed then
        -- Debug output when a key is pressed
        if _G.DEBUG_LOGGING then
            Log("DEBUG: Backpack toggle key pressed")
        end
        
        -- Check that GetGameTime function exists
        if not GetGameTime then
            LogError("CRITICAL: GetGameTime function is missing")
            return
        end
        
        local currentTime = GetGameTime()
        -- Sanity check: if time went backwards, reset lastToggleTime
        if currentTime < lastToggleTime then
            if _G.DEBUG_LOGGING then
                LogWarning("WARNING: Game time went backwards. Resetting lastToggleTime.")
            end
            lastToggleTime = currentTime
            ToggleBackpack()
        end
        
        -- Log the time since last key press was detected
        if keyPressCheck.lastKeyPressTime > 0 and _G.DEBUG_LOGGING then
            local timeDiff = currentTime - keyPressCheck.lastKeyPressTime
            Log("DEBUG: Time since last key press: " .. tostring(timeDiff) .. " seconds")
        end
        keyPressCheck.lastKeyPressTime = currentTime
        
        -- Check for cooldown
        local elapsed = currentTime - lastToggleTime
        local remaining = math.max(0, config.toggleCooldown - elapsed)
        if elapsed >= config.toggleCooldown then
            if _G.DEBUG_LOGGING then
                Log("DEBUG: Toggling backpack, config.enabled=" .. tostring(config.enabled))
            end
            local toggleSuccess = ToggleBackpack()  -- Call the global toggle function
            -- Only update lastToggleTime if toggle was successful (returns true)
            if toggleSuccess ~= false then
                lastToggleTime = currentTime  -- Update the last toggle time
            end
        else
            if _G.DEBUG_LOGGING then
                Log("DEBUG: Backpack toggle on cooldown, remaining: " .. tostring(remaining) .. " seconds")
            end
        end
    end
end

-- Function to toggle the backpack open/closed (GLOBAL FUNCTION!)
function ToggleBackpack()
    if _G.DEBUG_LOGGING then
        Log("DEBUG: ToggleBackpack called")
    end
    
    if not backpackId then 
        LogError("CRITICAL: Backpack hasn't been created yet, backpackId is nil")
        return false
    end
    
    if not config.enabled then
        LogWarning("Backpack tweaks are not enabled")
        return false
    end
    
    if _G.DEBUG_LOGGING then
        Log("DEBUG: Attempting to toggle backpack with ID: " .. tostring(backpackId))
    end
    
    -- Check if IsStorageOpen function exists
    if not IsStorageOpen then
        LogError("CRITICAL: IsStorageOpen function is missing")
        return false
    end
    
    local isOpen = IsStorageOpen(backpackId)
    if isOpen == nil then
        LogError("CRITICAL: IsStorageOpen returned nil - possible API failure")
        return false
    end
    
    -- Safe string conversion with nil check
    if _G.DEBUG_LOGGING then
        local statusText = isOpen and "open" or "closed"
        Log("DEBUG: Backpack is currently " .. statusText)
    end
    
    if isOpen then
        -- Check if CloseStorageEntity function exists
        if not CloseStorageEntity then
            LogError("CRITICAL: CloseStorageEntity function is missing")
            return false
        end
        
        local success = CloseStorageEntity(backpackId)
        -- Use conditional to avoid nil in tostring
        if success ~= nil and _G.DEBUG_LOGGING then
            Log("DEBUG: CloseStorageEntity result: " .. tostring(success))
        end
        if success == nil and _G.DEBUG_LOGGING then
            Log("DEBUG: CloseStorageEntity returned nil")
        end
        return success ~= false
    else
        -- Check if OpenStorageEntity function exists
        if not OpenStorageEntity then
            LogError("CRITICAL: OpenStorageEntity function is missing")
            return false
        end
        
        local success = OpenStorageEntity(backpackId)
        -- Use conditional to avoid nil in tostring
        if success ~= nil and _G.DEBUG_LOGGING then
            Log("DEBUG: OpenStorageEntity result: " .. tostring(success))
        end
        if success == nil and _G.DEBUG_LOGGING then
            Log("DEBUG: OpenStorageEntity returned nil")
        end
        return success ~= false
    end
end

-- Function to list all items in the backpack
function ListBackpackContents()
    local items = GetStorageItems(backpackId)
    
    if items and #items > 0 then
        Log("Backpack contents:")
        for i, item in ipairs(items) do
            Log(" - " .. item.quantity .. "x " .. item.name)
        end
    else
        Log("Backpack is empty")
    end
end

-- Helper function to check if all required API functions exist
function Backpack.ValidateAPI()
    local requiredFunctions = {
        "CreateStorageEntity", "SetStorageSubtitle", "IsStorageOpen", 
        "OpenStorageEntity", "CloseStorageEntity", "GetGameTime", "IsKeyPressed"
    }
    
    if _G.DEBUG_LOGGING then
        Log("DEBUG: Validating required API functions...")
    end
    
    local allValid = true
    
    for _, funcName in ipairs(requiredFunctions) do
        if _G[funcName] == nil then
            LogError("CRITICAL: Required function missing: " .. funcName)
            allValid = false
        end
    end
    
    if allValid and _G.DEBUG_LOGGING then
        Log("DEBUG: All required API functions are available")
    end
    
    return allValid
end

-- Clean up when the mod is unloaded
function Backpack.Shutdown()
    if backpackId and IsStorageOpen and IsStorageOpen(backpackId) then
        CloseStorageEntity(backpackId)
    end
    
    Log("Backpack module shutdown")
end

return Backpack 