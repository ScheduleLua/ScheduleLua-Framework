-- ScheduleLua Example Script
-- This script demonstrates the API available for modding ScheduleOne

-- Print a header for our script
Log("Example script loaded!")

-- Flag to track if we've run the main example code
local hasRunMainExample = false

-- This function gets called whenever a scene is loaded
function OnSceneLoaded(sceneName)
    Log("Scene loaded: " .. sceneName)
end

-- This function gets called once when the player is fully loaded and ready
function OnPlayerReady()
    Log("Player is now ready!")
    
    if not hasRunMainExample then
        hasRunMainExample = true
        RunMainExample()
    end
end

-- Function to run all the example code that requires the Main scene
function RunMainExample()
    Log("Running main example code...")
    
    -------------------------
    -- Player API Examples --
    -------------------------
    
    -- Get the player's full state (position, health, movement info, etc)
    local playerState = GetPlayerState()
    if playerState then
        -- Safely access player properties with nil checks
        local playerName = playerState.playerName or "Unknown"
        local health = playerState.health or 0
        local maxHealth = playerState.maxHealth or 100
        local isAlive = playerState.isAlive or false
        
        Log("Player name: " .. playerName)
        Log("Player health: " .. health .. "/" .. maxHealth)
        Log("Player is alive: " .. tostring(isAlive))
        
        -- Check if player is sprinting
        if playerState.isSprinting then
            Log("Player is sprinting")
        end
    else
        Log("Player not found or not initialized")
    end
    
    -- Get just the player's position
    local pos = GetPlayerPosition()
    if pos then
        Log("Player position: X=" .. pos.x .. ", Y=" .. pos.y .. ", Z=" .. pos.z)
    else
        Log("Could not get player position")
    end
    
    -- Get the player's current region
    local region = GetPlayerRegion()
    Log("Player is in region: " .. (region or "Unknown"))
    
    -- Example of setting player health (commented out for safety)
    -- SetPlayerHealth(100) -- Sets player health to 100
    
    -- Example of teleporting the player (commented out for safety)
    -- TeleportPlayer(100, 10, 100) -- Teleports player to X=100, Y=10, Z=100
    
    -----------------------
    -- NPC API Examples --
    -----------------------
    
    -- Get all NPCs in the game
    local allNPCs = GetAllNPCs()
    if allNPCs then
        Log("Found " .. #allNPCs .. " NPCs in the game")
        
        -- Loop through the first 3 NPCs (to avoid spam)
        local maxToShow = math.min(3, #allNPCs)
        for i=1, maxToShow do
            local npc = allNPCs[i]
            Log("NPC " .. i .. ": " .. npc.fullName .. " (ID: " .. npc.id .. ")")
        end
    end
    
    -- Get all NPC regions to see what regions exist
    local allRegions = GetAllNPCRegions()
    if allRegions then
        local regionsList = ""
        for i=1, #allRegions do
            if i > 1 then
                regionsList = regionsList .. ", "
            end
            regionsList = regionsList .. allRegions[i]
        end
        Log("Available NPC regions: " .. regionsList)
        
        -- Try to get NPCs in the first detected region
        if #allRegions > 0 then
            local firstRegion = allRegions[1]
            local npcsInRegion = GetNPCsInRegion(firstRegion)
            Log("Found " .. #npcsInRegion .. " NPCs in " .. firstRegion)
        end
    end
    
    -- Try several common region names
    local regionsToTry = {"Town", "Downtown", "City", "Village", "Central", "Main"}
    for _, regionName in ipairs(regionsToTry) do
        local npcsInRegion = GetNPCsInRegion(regionName)
        if npcsInRegion and #npcsInRegion > 0 then
            Log("Found " .. #npcsInRegion .. " NPCs in " .. regionName)
        end
    end
    
    -- Get a specific NPC by ID (using "npc_mayor" as an example)
    local exampleNpcId = "npc_mayor"
    local npc = GetNPC(exampleNpcId)
    if npc then
        Log("Found NPC: " .. npc.fullName)
        Log("NPC position: X=" .. npc.position.x .. ", Y=" .. npc.position.y .. ", Z=" .. npc.position.z)
        Log("NPC is in region: " .. npc.region)
        Log("NPC is conscious: " .. tostring(npc.isConscious))
    else
        Log("Could not find NPC with ID: " .. exampleNpcId)
        
        -- Show first NPC ID as a fallback
        if allNPCs and #allNPCs > 0 then
            Log("Try using this NPC ID instead: " .. allNPCs[1].id)
        end
    end
    
    -- Check if an NPC is in a specific region
    local isInRegion = IsNPCInRegion(exampleNpcId, "Town")
    Log("Is " .. exampleNpcId .. " in Town? " .. tostring(isInRegion))
    
    -- Let the system know this script ran successfully
    Log("Example script completed successfully!")
end

-- Example of a simple gameplay function that uses the API
function IsPlayerNearNPC(npcId, distance)
    local npc = GetNPC(npcId)
    local playerPos = GetPlayerPosition()
    
    if not npc or not playerPos then
        return false
    end
    
    -- Calculate distance (simple 2D distance for example)
    local dx = npc.position.x - playerPos.x
    local dz = npc.position.z - playerPos.z
    local calculatedDistance = math.sqrt(dx*dx + dz*dz)
    
    return calculatedDistance <= distance
end

-- Return true to indicate successful execution
return true 