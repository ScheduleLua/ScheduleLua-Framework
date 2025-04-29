--[[
    Bars_Tweaks Backpack Module
    Provides an enhanced backpack with more slots and features
]]

local Backpack = {}
local Menu = require("menu")

local config = {
    enabled = false,
    slotCount = 12,
    rowCount = 3
}

local backpackId = nil
local isInitialized = false
local currentScne = "Menu";

function Backpack.Initialize()
    if isInitialized then return end
    
    Log("Initializing Bars_Tweaks Backpack...")
    
    backpackId = CreateStorageEntity("Bars Backpack", config.slotCount, config.rowCount)
    if not backpackId then
        LogError("CRITICAL: Failed to create backpack storage entity")
        return
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

function Backpack.Disable()
    config.enabled = false
    Menu.SetTweakStatus("backpack", false)
    
    if backpackId and IsStorageOpen(backpackId) then
        CloseStorageEntity(backpackId)
    end
    
    _backpackEnabled = false
    ShowNotificationWithIcon("Bars Tweaks", "Backpack disabled", "icon.png")
end

function Backpack.ToggleTweaks()
    if config.enabled then
        Backpack.Disable()
    else
        Backpack.Enable()
    end
end

function Backpack.GetSlotCount()
    return config.slotCount
end

function Backpack.SetSlotCount(count)
    count = tonumber(count)
    if not count or count < 4 then
        count = 4
    elseif count > 36 then
        count = 36
    end
    
    config.slotCount = count
    Log("Backpack slot count set to " .. count)
    
    if backpackId then
        local items = IsStorageOpen(backpackId) and GetStorageItems(backpackId)
        CloseStorageEntity(backpackId)
        
        backpackId = CreateStorageEntity("Bars Backpack", config.slotCount, config.rowCount)
        SetStorageSubtitle(backpackId, "Enhanced storage capacity")
        
        if items then
            for _, item in ipairs(items) do
                AddItemToStorage(backpackId, item.id, item.quantity)
            end
            OpenStorageEntity(backpackId)
        end
    end
    
    return true
end

function Backpack.GetRowCount()
    return config.rowCount
end

function Backpack.SetRowCount(count)
    count = tonumber(count)
    if not count or count < 1 then
        count = 1
    elseif count > 6 then
        count = 6
    end
    
    config.rowCount = count
    Log("Backpack row count set to " .. count)
    
    if backpackId then
        local items = IsStorageOpen(backpackId) and GetStorageItems(backpackId)
        CloseStorageEntity(backpackId)
        
        backpackId = CreateStorageEntity("Bars Backpack", config.slotCount, config.rowCount)
        SetStorageSubtitle(backpackId, "Enhanced storage capacity")
        
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
    if not config.enabled or not backpackId or currentScne ~= "Main" or not IsKeyPressed then
        return
    end
    
    if IsKeyPressed("b") then
        ToggleBackpack()
    end
end

function ToggleBackpack()
    if not backpackId then 
        LogError("CRITICAL: Backpack hasn't been created yet, backpackId is nil")
        return false
    end
    
    if not config.enabled then
        LogWarning("Backpack tweaks are not enabled")
        return false
    end
    
    if not IsStorageOpen then
        LogError("CRITICAL: IsStorageOpen function is missing")
        return false
    end
    
    local isOpen = IsStorageOpen(backpackId)
    if isOpen == nil then
        LogError("CRITICAL: IsStorageOpen returned nil - possible API failure")
        return false
    end
    
    if isOpen then
        if not CloseStorageEntity then
            LogError("CRITICAL: CloseStorageEntity function is missing")
            return false
        end
        
        return CloseStorageEntity(backpackId) ~= false
    else
        if not OpenStorageEntity then
            LogError("CRITICAL: OpenStorageEntity function is missing")
            return false
        end
        
        return OpenStorageEntity(backpackId) ~= false
    end
end

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

function Backpack.ValidateAPI()
    local requiredFunctions = {
        "CreateStorageEntity", "SetStorageSubtitle", "IsStorageOpen", 
        "OpenStorageEntity", "CloseStorageEntity", "GetGameTime", "IsKeyPressed"
    }
    
    local allValid = true
    
    for _, funcName in ipairs(requiredFunctions) do
        if _G[funcName] == nil then
            LogError("CRITICAL: Required function missing: " .. funcName)
            allValid = false
        end
    end
    
    return allValid
end

function Backpack.Shutdown()
    if backpackId and IsStorageOpen and IsStorageOpen(backpackId) then
        CloseStorageEntity(backpackId)
    end
    
    Log("Backpack module shutdown")
end

return Backpack 