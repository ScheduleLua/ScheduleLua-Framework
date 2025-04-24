--[[
    Bars_Tweaks Item Tweaks Module
    Increases stack limits for all items
]]

-- Create a module table
local ItemTweaks = {}

-- Require the menu module for integration
local Menu = require("menu")

-- Module configuration
local config = {
    enabled = false,
    stackMultiplier = 2,  -- Multiplier for stack limits
    minStackSize = 2,    -- Minimum stack size for modified items
    originalStackLimits = {}  -- Store original stack limits to restore when disabled
}

-- Debug logging
local debugLogging = _G.DEBUG_LOGGING or false

-- Flag to track if Registry has been modified
local isRegistryModified = false
local isInitialized = false
local currentScene = "Menu"

-- Initialize the item tweaks
function ItemTweaks.Initialize()
    if isInitialized then return end
    
    Log("Initializing Bars_Tweaks Item Tweaks...")
    isInitialized = true
    
    if _G.DEBUG_LOGGING then
        Log("DEBUG: Item Tweaks initialized with stackMultiplier=" .. config.stackMultiplier)
    end
end

function ItemTweaks.OnSceneLoaded(sceneName)
    currentScene = sceneName
    if sceneName == "Menu" then
        config.enabled = false
    end
end

-- Enable item tweaks functionality
function ItemTweaks.Enable()
    if not isInitialized then
        ItemTweaks.Initialize()
    end
    
    if not IsRegistryReady() then
        Log("Registry not ready. Item tweaks cannot be enabled yet.")
        return
    end
    
    config.enabled = true
    Menu.SetTweakStatus("items", true)
    
    -- Modify all items' stack limits
    ModifyItemStackLimits()
    
    Log("Item tweaks enabled")
    ShowNotificationWithIcon("Bars Tweaks", "Item stack limits increased", "icon.png")
end

-- Disable item tweaks functionality
function ItemTweaks.Disable()
    config.enabled = false
    Menu.SetTweakStatus("items", false)
    
    -- Restore original stack limits
    RestoreOriginalStackLimits()
    
    Log("Item tweaks disabled")
    ShowNotificationWithIcon("Bars Tweaks", "Item stack limits restored", "icon.png")
end

-- Toggle item tweaks enabled/disabled state
function ItemTweaks.ToggleTweaks()
    if config.enabled then
        ItemTweaks.Disable()
    else
        ItemTweaks.Enable()
    end
end

-- Get current stack multiplier
function ItemTweaks.GetStackMultiplier()
    return config.stackMultiplier
end

-- Set stack multiplier
function ItemTweaks.SetStackMultiplier(value)
    -- Validate the input
    value = tonumber(value)
    if not value or value < 1.1 then
        value = 1.1
    elseif value > 10 then
        value = 10
    end
    
    -- Round to one decimal place
    value = math.floor(value * 10) / 10
    
    -- Update configuration
    config.stackMultiplier = value
    Log("Stack multiplier set to " .. value)
    
    -- If tweaks are already enabled, reapply with the new multiplier
    if config.enabled and isRegistryModified then
        RestoreOriginalStackLimits()
        ModifyItemStackLimits()
        Log("Applied new stack multiplier to all items")
    end
    
    return true
end

-- Modify all items' stack limits
function ModifyItemStackLimits()
    if not IsRegistryReady() then
        Log("Registry not ready. Cannot modify items.")
        return
    end
    
    Log("Modifying item stack limits...")
    
    -- Get all items from the registry
    local allItems = GetAllItems()
    local modifiedCount = 0
    
    -- Clear previous original values if any
    config.originalStackLimits = {}
    
    -- Loop through all items and modify stack limits
    for i, itemTable in pairs(allItems) do
        local itemId = itemTable.id
        local stackLimit = itemTable.stackLimit or 1
        
        -- Store original stack limit for later restoration
        config.originalStackLimits[itemId] = stackLimit
        
        -- Calculate new stack limit
        local newStackLimit = math.max(config.minStackSize, math.floor(stackLimit * config.stackMultiplier))
        
        -- Only modify if the new limit is different
        if newStackLimit > stackLimit then
            local properties = {
                stackLimit = newStackLimit
            }
            
            if ModifyItem(itemId, properties) then
                modifiedCount = modifiedCount + 1
                Log("Modified item: " .. itemId .. " - Stack limit: " .. stackLimit .. " → " .. newStackLimit)
            end
        end
    end
    
    isRegistryModified = true
    Log("Modified " .. modifiedCount .. " items")
end

-- Restore all items to their original stack limits
function RestoreOriginalStackLimits()
    if not IsRegistryReady() or not isRegistryModified then
        return
    end
    
    Log("Restoring original item stack limits...")
    local restoredCount = 0
    
    -- Loop through stored original values and restore them
    for itemId, originalStackLimit in pairs(config.originalStackLimits) do
        local properties = {
            stackLimit = originalStackLimit
        }
        
        if ModifyItem(itemId, properties) then
            restoredCount = restoredCount + 1
            Log("Restored item: " .. itemId .. " - Stack limit: " .. originalStackLimit)
        end
    end
    
    isRegistryModified = false
    Log("Restored " .. restoredCount .. " items")
end

-- Apply the current stack multiplier to all items
function ItemTweaks.ApplyStackMultiplier()
    -- Check if tweaks are enabled
    if not config.enabled then
        if _G.DEBUG_LOGGING then
            Log("DEBUG: ApplyStackMultiplier called, but tweaks are not enabled")
        end
        return false
    end
    
    -- Check if registry is ready
    if not IsRegistryReady then
        LogError("ERROR: IsRegistryReady function is missing")
        return false
    end
    
    if not IsRegistryReady() then
        if _G.DEBUG_LOGGING then
            Log("DEBUG: ApplyStackMultiplier failed - Registry not ready")
        end
        return false
    end
    
    -- First restore original limits
    if isRegistryModified then
        RestoreOriginalStackLimits()
    end
    
    -- Then apply new limits with current multiplier
    ModifyItemStackLimits()
    Log("Stack multiplier applied: " .. config.stackMultiplier)
    
    return true
end

-- Register commands
function ItemTweaks.OnConsoleReady()
    -- Command to set the stack multiplier
    RegisterCommand(
        "item_stack_multiplier",
        "Set the stack limit multiplier",
        "item_stack_multiplier <value>",
        function(args)
            local value = tonumber(args[1])
            if not value or value <= 0 then
                Log("Usage: /item_stack_multiplier <value> - where value is a positive number")
                return
            end
            
            config.stackMultiplier = value
            Log("Stack multiplier set to " .. value)
            
            -- If tweaks are enabled, reapply with new multiplier
            if config.enabled then
                RestoreOriginalStackLimits()
                ModifyItemStackLimits()
            end
        end
    )
    
    Log("Item tweaks module command registered")
end

-- Clean up when the mod is unloaded
function ItemTweaks.Shutdown()
    if config.enabled then
        RestoreOriginalStackLimits()
    end
    
    Log("Item tweaks module shutdown")
end

return ItemTweaks 