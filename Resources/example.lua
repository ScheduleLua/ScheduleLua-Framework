-- ScheduleLua Example Script
-- This script demonstrates the API available for modding ScheduleOne

-- Print a header for our script
Log("Example script loaded!")

-- Track player state
local playerLastPosition = nil
local playerLastRegion = nil
local playerLastMoney = 0
local npcPositions = {}

-- Helper function to safely handle nil values in string concatenation
local function safeStr(value)
    if value == nil then
        return "nil"
    else
        return tostring(value)
    end
end

-- Initialize function called when script is first loaded
function Initialize()
    Log("Example script initialized!")
    
    -- Get initial player state
    playerLastPosition = GetPlayerPosition()
    playerLastRegion = GetPlayerRegion()
    playerLastMoney = GetPlayerMoney()
    
    -- Log player information
    Log("Player starting in region: " .. safeStr(playerLastRegion))
    Log("Player energy: " .. safeStr(GetPlayerEnergy()))
    Log("Player health: " .. safeStr(GetPlayerHealth()))
    
    -- Log player position (using Vector3Proxy)
    local pos = GetPlayerPosition()
    if pos then
        Log("Player position: " .. pos.x .. ", " .. pos.y .. ", " .. pos.z)
    else
        Log("Player position: unknown")
    end
    
    -- Get current game time
    local currentTime = GetGameTime()
    local formattedTime = FormatGameTime(currentTime)
    Log("Current game time: " .. safeStr(formattedTime))
    Log("Current day: " .. safeStr(GetGameDay()))
    
    -- Get all map regions
    Log("Available map regions:")
    local regions = GetAllMapRegions()
    if regions then
        local hasRegions = false
        for i, region in pairs(regions) do
            Log("  - " .. safeStr(region))
            hasRegions = true
        end
        if not hasRegions then
            Log("  No regions found")
        end
    else
        Log("  Unable to get regions")
    end
    
    -- Find NPCs in the same region as player
    if playerLastRegion then
        Log("NPCs in player's region:")
        local npcsInRegion = GetNPCsInRegion(playerLastRegion)
        if npcsInRegion then
            local hasNpcs = false
            for i, npc in pairs(npcsInRegion) do
                if npc and npc.fullName then
                    Log("  - " .. npc.fullName)
                    -- Store initial NPC positions for tracking
                    local npcObj = FindNPC(npc.fullName)
                    if npcObj then
                        npcPositions[npc.id] = GetNPCPosition(npcObj)
                    end
                    hasNpcs = true
                end
            end
            if not hasNpcs then
                Log("  No NPCs found in region")
            end
        else
            Log("  Unable to get NPCs in region")
        end
    else
        Log("NPCs in player's region: Unknown region")
    end
    
    return true
end

-- Update function called every frame
function Update()
    -- This function is called frequently, so we'll only do occasional checks
    -- In a real mod, you would want to limit how often you perform actions here
    
    -- Check if player has moved significantly (more than 5 units)
    local currentPos = GetPlayerPosition()
    if playerLastPosition and currentPos then
        -- Use Vector3Distance to compare positions
        local distance = Vector3Distance(currentPos, playerLastPosition)
        if distance > 5 then
            Log("Player moved significantly!")
            Log("Distance moved: " .. distance)
            playerLastPosition = currentPos
            
            -- Create a custom event for movement
            OnPlayerMovedSignificantly()
        end
    end
end

-- Event Handlers

-- Called when the game day changes
function OnDayChanged(day)
    Log("Day changed to: " .. safeStr(day))
    -- You could reset daily tracking variables here
end

-- Called when the game time changes
function OnTimeChanged(time)
    -- Only log time changes occasionally to avoid spam
    if time and time % 3 == 0 then
        Log("Time is now: " .. safeStr(FormatGameTime(time)))
        
        -- Check if it's night time
        if IsNightTime() then
            Log("It's night time!")
        end
    end
end

-- Called when the player goes to sleep
function OnSleepStart()
    Log("Player going to sleep...")
    -- Save any data that should persist through sleep
end

-- Called when the player wakes up
function OnSleepEnd()
    Log("Player woke up!")
    -- Restore any data or perform morning activities
end

-- Called when player health changes
function OnPlayerHealthChanged(newHealth)
    Log("Player health changed to: " .. safeStr(newHealth))
    -- Provide healing items or special effects at low health
    if newHealth and newHealth < 30 then
        Log("Player health is low!")
    end
end

-- Called when player energy changes
function OnPlayerEnergyChanged(newEnergy)
    Log("Player energy changed to: " .. safeStr(newEnergy))
    -- Provide energy items or special effects at low energy
    if newEnergy and newEnergy < 30 then
        Log("Player energy is low!")
    end
end

-- Called when the player enters a new region
function OnPlayerMovedSignificantly()
    local currentRegion = GetPlayerRegion()
    if currentRegion and playerLastRegion and currentRegion ~= playerLastRegion then
        Log("Player changed region from " .. safeStr(playerLastRegion) .. " to " .. safeStr(currentRegion))
        playerLastRegion = currentRegion
        
        -- Get NPCs in the new region
        Log("NPCs in new region:")
        local npcsInRegion = GetNPCsInRegion(currentRegion)
        if npcsInRegion then
            local hasNpcs = false
            for i, npc in pairs(npcsInRegion) do
                if npc and npc.fullName then
                    Log("  - " .. npc.fullName)
                    hasNpcs = true
                end
            end
            if not hasNpcs then
                Log("  No NPCs found in region")
            end
        else
            Log("  Unable to get NPCs in region")
        end
    elseif currentRegion ~= playerLastRegion then
        playerLastRegion = currentRegion
    end
end

-- Called when the player's money changes
function OnPlayerMoneyChanged(newMoney)
    if newMoney then
        local difference = newMoney - playerLastMoney
        if difference > 0 then
            Log("Player gained " .. difference .. " money!")
        else
            Log("Player spent " .. math.abs(difference) .. " money!")
        end
        playerLastMoney = newMoney
    end
end

-- Called when the player interacts with an NPC
function OnNPCInteraction(npcId)
    Log("Player interacted with NPC: " .. safeStr(npcId))
    if npcId then
        local npc = GetNPC(npcId)
        if npc then
            Log("  Name: " .. safeStr(npc.fullName))
            Log("  Region: " .. safeStr(npc.region))
        end
    end
end

-- Called when a scene is loaded
function OnSceneLoaded(sceneName)
    Log("Scene loaded: " .. safeStr(sceneName))
end

-- Called when the player is fully loaded and ready
function OnPlayerReady()
    Log("Player is ready!")
    
    -- This is a good place to perform one-time setup that requires
    -- the player to be fully initialized
    
    -- Example: Count inventory slots
    local slotCount = GetInventorySlotCount()
    Log("Player has " .. safeStr(slotCount) .. " inventory slots")
    
    -- Check what's in the first few inventory slots
    if slotCount and slotCount > 0 then
        for i = 0, math.min(5, slotCount - 1) do
            local itemName = GetInventoryItemAt(i)
            if itemName and itemName ~= "" then
                Log("Slot " .. i .. " contains: " .. itemName)
            end
        end
    end
end

-- Process chat commands
function HandleCommand(command)
    -- Simple command handler
    if command == "/help" then
        return "Available commands: /help, /pos, /time, /heal, /energy, /region, /npcs"
    elseif command == "/pos" then
        local pos = GetPlayerPosition()
        if pos then
            return "Position: " .. pos.x .. ", " .. pos.y .. ", " .. pos.z
        else
            return "Position: unknown"
        end
    elseif command == "/time" then
        return "Time: " .. safeStr(FormatGameTime(GetGameTime())) .. ", Day: " .. safeStr(GetGameDay())
    elseif command == "/heal" then
        SetPlayerHealth(100)
        return "Healed player to full health"
    elseif command == "/energy" then
        SetPlayerEnergy(100)
        return "Restored player energy"
    elseif command == "/region" then
        return "Current region: " .. safeStr(GetPlayerRegion())
    elseif command == "/npcs" then
        local npcs = GetAllNPCs()
        if npcs then
            local count = 0
            for _ in pairs(npcs) do count = count + 1 end
            return "There are " .. count .. " NPCs in the world"
        else
            return "Could not get NPC information"
        end
    end
    
    return nil  -- Command not handled
end

-- Return true to indicate successful execution
return true 