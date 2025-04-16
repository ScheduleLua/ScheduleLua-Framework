-- ScheduleLua Basic Example Script

-- Print a header for our script
Log("Example script loaded!")

-- Track player state
local isPlayerReady = false
local playerLastPosition = nil
local playerLastRegion = nil
local npcPositions = {}

-- Initialize function called when script is first loaded
function Initialize()
    Log("Example script initialized!")
end

-- Update function called every frame
function Update()
    -- This function is called frequently, you would want to limit how often you perform actions here
    
    if isPlayerReady then
        -- Check if player has moved significantly (more than 5 units)
        local currentPos = GetPlayerPosition()
        if playerLastPosition then
            -- Use Vector3Distance to compare positions
            local distance = Vector3Distance(currentPos, playerLastPosition)
            if distance > 5 then
                Log("Player moved significantly!")
                Log("Distance moved: " .. distance)
                playerLastPosition = currentPos
                OnPlayerMovedSignificantly()
            end
        end
    end
end

-- Called when the console is fully loaded and ready
function OnConsoleReady()
    RegisterCommand(
        "help",
        "Shows available commands",
        "help",
        function(args)
            Log("Available example commands: help, pos, lua_teleport, time, heal, energy, region, npcs")
        end
    )
    
    RegisterCommand(
        "pos",
        "Shows player position",
        "pos",
        function(args)
            local pos = GetPlayerPosition()
            Log("Position: " .. pos.x .. ", " .. pos.y .. ", " .. pos.z)
        end
    )

    RegisterCommand(
        "lua_teleport", 
        "Teleports player to specified coordinates", 
        "lua_teleport <x> <y> <z>", 
        function(args)
            -- Command name is already removed from args, so actual arguments start at index 1
            if #args < 3 then
                LogError("Not enough arguments. Usage: lua_teleport <x> <y> <z>")
                return
            end
            
            local x = tonumber(args[1])
            local y = tonumber(args[2])
            local z = tonumber(args[3])
            
            if not x or not y or not z then
                LogError("Invalid coordinates. All values must be numbers.")
                return
            end
            
            SetPlayerPosition(x, y, z)
            Log("Teleported to: " .. x .. ", " .. y .. ", " .. z)
        end
    )
    
    RegisterCommand(
        "time",
        "Shows current game time and day",
        "time",
        function(args)
            Log("Time: " .. FormatGameTime(GetGameTime()) .. ", Day: " .. GetGameDay())
        end
    )
    
    RegisterCommand(
        "heal",
        "Heals player to full health",
        "heal",
        function(args)
            SetPlayerHealth(100)
            Log("Healed player to full health")
        end
    )
    
    RegisterCommand(
        "energy",
        "Restores player energy",
        "energy",
        function(args)
            SetPlayerEnergy(100)
            Log("Restored player energy")
        end
    )
    
    RegisterCommand(
        "region",
        "Shows current region",
        "region",
        function(args)
            Log("Current region: " .. GetPlayerRegion())
        end
    )
    
    RegisterCommand(
        "npcs",
        "Shows the total number of NPCs in the world",
        "npcs",
        function(args)
            local npcs = GetAllNPCs()
            local count = 0
            for _ in pairs(npcs) do count = count + 1 end
            Log("There are " .. count .. " NPCs in the world")
        end
    )
end

-- Called when the player is fully loaded and ready
function OnPlayerReady()
    isPlayerReady = true
    Log("Player is ready!")
    
    -- Get initial player state
    playerLastPosition = GetPlayerPosition()
    playerLastRegion = GetPlayerRegion()
    
    -- Log player information
    Log("Player starting in region: " .. (playerLastRegion))
    Log("Player energy: " .. (GetPlayerEnergy()))
    Log("Player health: " .. (GetPlayerHealth()))
    
    -- Log player position
    local pos = GetPlayerPosition()
    if pos then
        Log("Player position: " .. pos.x .. ", " .. pos.y .. ", " .. pos.z)
    else
        Log("Player position: Unknown")
    end
    
    -- Get current game time
    local currentTime = GetGameTime()
    local formattedTime = currentTime and FormatGameTime(currentTime)
    Log("Current game time: " .. formattedTime)
    Log("Current day: " .. (GetGameDay()))
    
    -- Get all map regions
    Log("Available map regions:")
    local regions = GetAllMapRegions() or {}
    for i, region in pairs(regions) do
        Log("  - " .. region)
    end
    
    -- Get NPCs in the same region as player
    if playerLastRegion then
        Log("NPCs in player's region:")
        local npcsInRegion = GetNPCsInRegion(playerLastRegion) or {}
        for i, npc in pairs(npcsInRegion) do
            Log("  - " .. npc.fullName)
            -- Use GetNPC safely with error handling
            local npcObj = GetNPC(npc.id)
            if npcObj then
                -- Store position safely
                local npcPos = GetNPCPosition(npcObj)
                if npcPos then
                    npcPositions[npc.id] = npcPos
                    Log("    Position: " .. npcPos.x .. ", " .. npcPos.y .. ", " .. npcPos.z)
                end
            end
        end
    end
    
    -- Count inventory slots
    local slotCount = GetInventorySlotCount()
    Log("Player has " .. slotCount .. " inventory slots")
    
    -- Check what's in the inventory slots
    for i = 0, slotCount do
        local itemName = GetInventoryItemAt(i)
        if itemName and itemName ~= "" then
            Log("Slot " .. i .. " contains: " .. itemName)
        end
    end
end

function OnSceneLoaded(sceneName)
    Log("Scene loaded: " .. sceneName)
end

-- Called when the game day changes
function OnDayChanged(day)
    Log("Day changed to: " .. day)
end

-- Called when the game time changes
function OnTimeChanged(time)
    if time % 3 == 0 then
        Log("Time is now: " .. FormatGameTime(time))
        
        if IsNightTime() then
            Log("It's night time!")
        end
    end
end

-- Called when player health changes
function OnPlayerHealthChanged(newHealth)
    Log("Player health changed to: " .. newHealth)
    -- Provide healing items or special effects at low health
    if newHealth < 30 then
        Log("Player health is low!")
    end
end

-- Called when player energy changes
function OnPlayerEnergyChanged(newEnergy)
    Log("Player energy changed to: " .. newEnergy)
    -- Provide energy items or special effects at low energy
    if newEnergy < 30 then
        Log("Player energy is low!")
    end
end

-- Called when the player enters a new region
function OnPlayerMovedSignificantly()
    local currentRegion = GetPlayerRegion()
    
    if currentRegion and playerLastRegion and currentRegion ~= playerLastRegion then
        Log("Player changed region from " .. playerLastRegion .. " to " .. currentRegion)
        playerLastRegion = currentRegion
        
        -- Get NPCs in the new region
        Log("NPCs in new region:")
        local npcsInRegion = GetNPCsInRegion(currentRegion) or {}
        for i, npc in pairs(npcsInRegion) do
            Log("  - " .. npc.fullName)
        end
    elseif currentRegion and not playerLastRegion then
        Log("Player entered region: " .. currentRegion)
        playerLastRegion = currentRegion
    end
end

-- Cleanup function called when script is unloaded
function Shutdown()
    -- Unregister all commands
    UnregisterCommand("help")
    UnregisterCommand("pos")
    UnregisterCommand("time")
    UnregisterCommand("heal")
    UnregisterCommand("energy")
    UnregisterCommand("region")
    UnregisterCommand("npcs")
    
    Log("Example script shutdown, all commands unregistered")
end