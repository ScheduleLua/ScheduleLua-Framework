-- ScheduleLua NPC API Example Script

-- Print a header for our script
Log("NPC API Example script loaded!")

-- Track NPC information
local trackedNPCs = {}
local lastCheckedRegion = nil

-- Initialize function called when script is first loaded
function Initialize()
    Log("NPC API Example script initialized!")
end

-- Called when the console is fully loaded and ready
function OnConsoleReady()
    RegisterCommand(
        "npc_find",
        "Finds an NPC by id",
        "npc_find <id>",
        function(args)
            if #args < 1 then
                LogError("Usage: npc_find <id>")
                return
            end
            
            local npcId = args[1]
            local npc = GetNPC(npcId)
            
            if npc then
                local pos = GetNPCPosition(npc)
                Log("Found NPC: " .. npcId)
                Log("Position: " .. pos.x .. ", " .. pos.y .. ", " .. pos.z)
            else
                Log("NPC not found: " .. npcId)
            end
        end
    )
    
    RegisterCommand(
        "npc_info",
        "Gets info about an NPC by ID",
        "npc_info <id>",
        function(args)
            if #args < 1 then
                LogError("Usage: npc_info <id>")
                return
            end
            
            local npcId = args[1]
            local npcData = GetNPC(npcId)
            
            if npcData then
                Log("NPC ID: " .. npcData.id)
                Log("Name: " .. npcData.fullName)
                Log("Region: " .. npcData.region)
                Log("Is Conscious: " .. (npcData.isConscious and "Yes" or "No"))
                if npcData.isMoving ~= nil then
                    Log("Is Moving: " .. (npcData.isMoving and "Yes" or "No"))
                end
            else
                Log("NPC not found with ID: " .. npcId)
            end
        end
    )
    
    RegisterCommand(
        "npc_region",
        "Shows the region an NPC is in",
        "npc_region <id>",
        function(args)
            if #args < 1 then
                LogError("Usage: npc_region <id>")
                return
            end
            
            local npcId = args[1]
            local region = GetNPCRegion(npcId)
            
            if region then
                Log("NPC " .. npcId .. " is in region: " .. region)
            else
                Log("NPC not found or has no region")
            end
        end
    )
    
    RegisterCommand(
        "npc_list",
        "Lists all NPCs",
        "npc_list",
        function(args)
            local npcs = GetAllNPCs()
            Log("All NPCs:")
            for i, npc in pairs(npcs) do
                Log(i .. ": " .. npc.fullName .. " (ID: " .. npc.id .. ") in " .. npc.region)
            end
        end
    )
    
    RegisterCommand(
        "npc_regions",
        "Lists all regions with NPCs",
        "npc_regions",
        function(args)
            local regions = GetAllNPCRegions()
            Log("Regions with NPCs:")
            for i, region in pairs(regions) do
                Log(i .. ": " .. region)
            end
        end
    )
    
    RegisterCommand(
        "npc_in_region",
        "Lists all NPCs in a specific region",
        "npc_in_region <region>",
        function(args)
            if #args < 1 then
                LogError("Usage: npc_in_region <region>")
                return
            end
            
            local region = args[1]
            local npcs = GetNPCsInRegion(region)
            
            if #npcs == 0 then
                Log("No NPCs found in region: " .. region)
            else
                Log("NPCs in region " .. region .. ":")
                for i, npc in pairs(npcs) do
                    Log(i .. ": " .. npc.fullName .. " (ID: " .. npc.id .. ")")
                end
            end
        end
    )
    
    RegisterCommand(
        "npc_check_region",
        "Checks if an NPC is in a specific region",
        "npc_check_region <id> <region>",
        function(args)
            if #args < 2 then
                LogError("Usage: npc_check_region <id> <region>")
                return
            end
            
            local npcId = args[1]
            local region = args[2]
            local isInRegion = IsNPCInRegion(npcId, region)
            
            if isInRegion then
                Log("NPC " .. npcId .. " is in region " .. region)
            else
                Log("NPC " .. npcId .. " is NOT in region " .. region)
            end
        end
    )
    
    RegisterCommand(
        "npc_track",
        "Starts tracking NPC positions",
        "npc_track <id>",
        function(args)
            if #args < 1 then
                LogError("Usage: npc_track <id>")
                return
            end
            
            local npcId = args[1]
            local npcData = GetNPC(npcId)
            
            if npcData then
                trackedNPCs[npcId] = true
                Log("Now tracking NPC: " .. npcData.fullName)
            else
                Log("NPC not found with ID: " .. npcId)
            end
        end
    )
    
    RegisterCommand(
        "npc_untrack",
        "Stops tracking an NPC",
        "npc_untrack <id>",
        function(args)
            if #args < 1 then
                LogError("Usage: npc_untrack <id>")
                return
            end
            
            local npcId = args[1]
            if trackedNPCs[npcId] then
                trackedNPCs[npcId] = nil
                Log("Stopped tracking NPC: " .. npcId)
            else
                Log("NPC not being tracked: " .. npcId)
            end
        end
    )
    
    RegisterCommand(
        "npc_untrack_all",
        "Stops tracking all NPCs",
        "npc_untrack_all",
        function(args)
            trackedNPCs = {}
            Log("Stopped tracking all NPCs")
        end
    )
end

-- Called when the player is fully loaded and ready
function OnPlayerReady()
    Log("Player is ready, scanning NPCs...")
    
    -- Get the player's current region
    local playerRegion = GetPlayerRegion()
    if playerRegion then
        Log("Player is in region: " .. playerRegion)
        lastCheckedRegion = playerRegion
        
        -- Log NPCs in the player's region
        Log("NPCs in player's region:")
        local npcsInRegion = GetNPCsInRegion(playerRegion) or {}
        for i, npc in pairs(npcsInRegion) do
            Log("  - " .. npc.fullName .. " (ID: " .. npc.id .. ")")
        end
    end
    
    -- Count total NPCs in the world
    local allNpcs = GetAllNPCs() or {}
    local count = 0
    for _ in pairs(allNpcs) do count = count + 1 end
    Log("There are " .. count .. " NPCs in the world")
    
    -- Get all unique regions with NPCs
    local regions = GetAllNPCRegions() or {}
    Log("NPCs are present in the following regions:")
    for i, region in pairs(regions) do
        Log("  - " .. region)
    end
end

-- Called on significant player movement
function OnPlayerMovedSignificantly()
    local currentRegion = GetPlayerRegion()
    
    if currentRegion and lastCheckedRegion and currentRegion ~= lastCheckedRegion then
        Log("Player changed region from " .. lastCheckedRegion .. " to " .. currentRegion)
        lastCheckedRegion = currentRegion
        
        -- Get NPCs in the new region
        Log("NPCs in new region:")
        local npcsInRegion = GetNPCsInRegion(currentRegion) or {}
        for i, npc in pairs(npcsInRegion) do
            Log("  - " .. npc.fullName .. " (ID: " .. npc.id .. ")")
        end
    end
end

-- Called when game time changes
function OnTimeChanged(time)
    -- Only check every 1 hour of game time (to reduce spam)
    if time % 1 == 0 then
        -- Update tracked NPCs
        for npcId, _ in pairs(trackedNPCs) do
            local npcData = GetNPCState(npcId)
            if npcData then
                Log("Tracked NPC " .. npcData.fullName .. " is in region: " .. npcData.region)
                
                -- Get the position from the position table in npcData
                local pos = npcData.position
                if pos then
                    Log("  Position: " .. pos.x .. ", " .. pos.y .. ", " .. pos.z)
                end
            else
                -- NPC no longer exists, stop tracking
                Log("NPC with ID " .. npcId .. " no longer exists, removing from tracked NPCs")
                trackedNPCs[npcId] = nil
            end
        end
    end
end

-- Cleanup function called when script is unloaded
function Shutdown()
    -- Unregister all commands
    UnregisterCommand("npc_find")
    UnregisterCommand("npc_info")
    UnregisterCommand("npc_region")
    UnregisterCommand("npc_list")
    UnregisterCommand("npc_regions")
    UnregisterCommand("npc_in_region")
    UnregisterCommand("npc_check_region")
    UnregisterCommand("npc_track")
    UnregisterCommand("npc_untrack")
    UnregisterCommand("npc_untrack_all")
    
    Log("NPC API Example script shutdown, all commands unregistered")
end