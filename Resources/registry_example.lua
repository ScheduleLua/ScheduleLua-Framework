--[[
    Registry API Example Script
    
    This script demonstrates how to properly use the Registry API in Schedule I.
    It shows best practices for:
    - Waiting for Registry to be ready
    - Checking and retrieving items
    - Modifying existing items
    - Working with item instances
    - Safe handling during scene changes
]]

-- Global variables to track state
local initialized = false
local pendingOperations = {}

-- Mod information
local MOD_NAME = "Registry Example"
local MOD_VERSION = "1.0"
local isRegistered = false

-- Called when the script is first loaded
function Initialize()
    Log("=== " .. MOD_NAME .. " v" .. MOD_VERSION .. " ===")
    Log("Script initialized. Waiting for player and registry...")
    
    -- Start monitoring Registry status for scene changes
    StartRegistryMonitoring()
    
    return true
end

-- Called when the player is fully loaded
function OnPlayerReady()
    Log("Player is ready!")
    
    -- The player is ready, but Registry might not be
    -- We'll check if the Registry is ready, otherwise OnRegistryReady will handle it later
    if IsRegistryReady() then
        Log("Registry is already ready when player loaded!")
        -- We can initialize now
        if not initialized then
            InitializeMod()
        end
    else
        Log("Waiting for Registry to be ready...")
        -- OnRegistryReady will handle initialization
    end
end

-- Main initialization function - called when Registry is ready
function InitializeMod()
    if initialized then return end
    
    Log("Initializing mod with Registry access...")
    
    -- Modify some vanilla items
    ModifyVanillaItems()
    
    -- Mark as initialized
    initialized = true
    Log("Mod initialized successfully!")
    
    -- Schedule some follow-up tasks
    ScheduleFollowUpTasks()
end

-- Schedule tasks to execute after initialization
function ScheduleFollowUpTasks()
    -- Example of tasks that we want to run slightly after initialization
    Wait(3.0, function()
        if IsRegistryReady() then
            Log("Running follow-up tasks...")
            
            -- Analyze inventory after player has time to look at new items
            AnalyzePlayerInventory()
        else
            Log("Registry not ready for follow-up tasks, rescheduling...")
            Wait(2.0, ScheduleFollowUpTasks)
        end
    end)
end

-- Modify some vanilla items as examples
function ModifyVanillaItems()
    Log("Modifying vanilla items...")
    
    -- Safe modification with error handling
    local function SafeModifyItem(itemId, properties)
        if not DoesItemExist(itemId) then
            LogWarning("Cannot modify " .. itemId .. ": Item doesn't exist")
            return false
        end
        
        local success = ModifyItem(itemId, properties)
        if success then
            Log("Successfully modified " .. itemId)
        else
            LogWarning("Failed to modify " .. itemId)
        end
        return success
    end
    
    -- Increase jar capacity
    SafeModifyItem("jar", {
        stackLimit = 40,
        description = "A glass jar for storing product. Modified by Lua to hold more!"
    })
    
    -- Improve a strain's quality
    if DoesItemExist("ogkush") then
        SafeModifyItem("ogkush", {
            defaultQuality = "Perfect",
            description = "The finest OG Kush, enhanced by Lua modding"
        })
    end
    
    -- Add custom keywords to items for searchability
    if DoesItemExist("baggie") then
        local baggie = GetItem("baggie")
        if baggie then
            local keywordsTable = {}
            -- Add original keywords if they exist
            if baggie.keywords then
                for i, keyword in ipairs(baggie.keywords) do
                    table.insert(keywordsTable, keyword)
                end
            end
            -- Add our new keywords
            table.insert(keywordsTable, "modded")
            table.insert(keywordsTable, "enhanced")
            
            SafeModifyItem("baggie", {
                keywords = keywordsTable
            })
        end
    end
end

-- Set up continuous monitoring for Registry changes
function StartRegistryMonitoring()
    Log("Starting Registry monitoring...")
    
    -- Keep track of Registry state
    local lastRegistryState = IsRegistryReady()
    
    -- Function to check Registry status periodically
    local function CheckRegistryStatus()
        local currentState = IsRegistryReady()
        
        -- Detect changes in Registry availability
        if currentState ~= lastRegistryState then
            if currentState then
                Log("Registry is now available - processing any pending operations")
                ProcessPendingOperations()
            else
                Log("Registry is no longer available (scene change?)")
            end
            
            lastRegistryState = currentState
        end
        
        -- Continue checking
        Wait(2.0, CheckRegistryStatus)
    end
    
    -- Start the monitoring loop
    CheckRegistryStatus()
end

-- Process operations that were queued when Registry was unavailable
function ProcessPendingOperations()
    if #pendingOperations == 0 then
        Log("No pending operations to process")
        return
    end
    
    Log("Processing " .. #pendingOperations .. " pending operations")
    
    for i, op in ipairs(pendingOperations) do
        Log("Executing: " .. op.name)
        local success, error = pcall(op.func)
        if not success then
            LogWarning("Failed to execute operation: " .. op.name .. " - " .. tostring(error))
        end
    end
    
    -- Clear the queue
    pendingOperations = {}
end

-- Queue operations for when Registry becomes available
function QueueOperation(name, func)
    if IsRegistryReady() then
        -- Execute immediately if Registry is ready
        Log("Executing operation immediately: " .. name)
        local success, error = pcall(func)
        if not success then
            LogWarning("Failed to execute operation: " .. name .. " - " .. tostring(error))
        end
    else
        -- Queue for later execution
        Log("Queuing operation: " .. name)
        table.insert(pendingOperations, {name = name, func = func})
    end
end

-- Example function to analyze inventory contents
function AnalyzePlayerInventory()
    if not IsRegistryReady() then
        Log("Cannot analyze inventory - Registry not ready")
        QueueOperation("Analyze inventory", AnalyzePlayerInventory)
        return
    end
    
    Log("Analyzing player inventory")
    
    -- Get inventory slot count
    local slotCount = GetInventorySlotCount()
    Log("Inventory has " .. slotCount .. " slots")
    
    -- Count items by category
    local categoryCounts = {}
    for i = 0, slotCount - 1 do
        local itemName = GetInventoryItemAt(i)
        if itemName and itemName ~= "" then
            -- Try to find the item's category
            local item = GetItem(itemName)
            if item and item.category then
                categoryCounts[item.category] = (categoryCounts[item.category] or 0) + 1
            end
        end
    end
    
    -- Report category counts
    for category, count in pairs(categoryCounts) do
        Log("Category " .. category .. ": " .. count .. " items")
    end
end

-- Register for console commands when console is ready
function OnConsoleReady()
    if isRegistered then return end
    
    -- Register commands for interacting with our mod
    RegisterCommand(
        "regmenu",
        "Show Registry Example menu",
        "regmenu",
        function(args)
            Log("Registry Example Menu:")
            Log("  reganalyze - Analyze inventory")
            Log("  regstrains - List strains")
        end
    )
    
    RegisterCommand(
        "reganalyze",
        "Analyze inventory",
        "reganalyze",
        function(args)
            if IsRegistryReady() then
                AnalyzePlayerInventory()
            else
                Log("Registry not ready, cannot analyze inventory")
            end
        end
    )
    
    RegisterCommand(
        "regstrains",
        "List strains",
        "regstrains",
        function(args)
            if IsRegistryReady() then
                ListAllStrains()
            else
                Log("Registry not ready, cannot list strains")
            end
        end
    )

    isRegistered = true
    Log("Registry Example commands registered")
end

-- Called when script is unloaded
function Shutdown()
    -- Unregister commands
    if isRegistered then
        UnregisterCommand("regmenu")
        UnregisterCommand("reganalyze")
        UnregisterCommand("regstrains")
        Log("Registry Example commands unregistered")
    end
    
    Log("Registry Example shutdown")
    
    return true
end

function OnRegistryReady()
    Log("Registry is now ready!")
    
    -- Initialize our mod
    if not initialized then
        InitializeMod()
    end
end

-- Print initial message
Log("Registry Example Script loaded!")
