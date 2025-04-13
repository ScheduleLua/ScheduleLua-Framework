-- Player Example Script

-- Global variables to track player resources
local playerEnergy = 0
local playerHealth = 0
local lastPosition = nil
local energyDecayTimer = 0
local healthDecayTimer = 0
local resourceCheckpoints = {}
local lastRegion = nil
local lastUpdateTime = 0

-- Initialize function called when script is first loaded
function Initialize()
    Log("Player example script initialized!")
end

-- Update function called every frame
function Update()
    -- Use game time for timers instead of delta time
    local currentTime = GetGameTime()
    if lastUpdateTime == 0 then
        lastUpdateTime = currentTime
    end
    
    -- Calculate elapsed time (each unit is one hour in game time)
    local timeElapsed = (currentTime - lastUpdateTime) * 60 -- Convert to minutes for finer control
    
    -- Accumulate time in our timers
    energyDecayTimer = energyDecayTimer + timeElapsed
    healthDecayTimer = healthDecayTimer + timeElapsed
    
    -- Check player movement and consume energy accordingly
    if energyDecayTimer >= 1.0 then -- check periodically
        energyDecayTimer = 0
        CheckPlayerMovement()
    end
    
    -- Gradually reduce health if energy is depleted
    if healthDecayTimer >= 5.0 then -- check less frequently
        healthDecayTimer = 0
        if GetPlayerEnergy() <= 0 then
            local currentHealth = GetPlayerHealth()
            if currentHealth > 10 then -- Don't kill the player
                SetPlayerHealth(currentHealth - 5)
                ShowNotification("Low energy is affecting your health!")
            end
        end
    end
    
    -- Update last update time
    lastUpdateTime = currentTime
end

-- Check player movement and consume resources
function CheckPlayerMovement()
    local currentPos = GetPlayerPosition()
    if lastPosition then
        local distance = Vector3Distance(currentPos, lastPosition)
        if distance > 0.5 then -- Player is moving
            local currentEnergy = GetPlayerEnergy()
            local energyDecrease = distance * 0.2 -- Energy consumption based on distance moved
            
            if currentEnergy > energyDecrease then
                SetPlayerEnergy(currentEnergy - energyDecrease)
            else
                SetPlayerEnergy(0)
            end
        end
    end
    lastPosition = currentPos
end

-- Called when the console is fully loaded and ready
function OnConsoleReady()
    -- Register resource management related commands
    RegisterCommand(
        "resources",
        "Shows player resources",
        "resources",
        function(args)
            local health = GetPlayerHealth()
            local energy = GetPlayerEnergy()
            Log("Health: " .. string.format("%.1f", health) .. "  Energy: " .. string.format("%.1f", energy))
        end
    )
    
    RegisterCommand(
        "restore",
        "Restores player health and energy",
        "restore [amount]",
        function(args)
            local amount = 100
            if args[1] then
                amount = tonumber(args[1]) or 100
            end
            
            SetPlayerHealth(amount)
            SetPlayerEnergy(amount)
            Log("Restored health and energy to " .. amount)
        end
    )
    
    RegisterCommand(
        "checkpoint",
        "Creates a resource checkpoint at current location",
        "checkpoint [name]",
        function(args)
            local name = args[1] or ("Checkpoint " .. #resourceCheckpoints + 1)
            local pos = GetPlayerPosition()
            local checkpoint = {
                name = name,
                position = {x = pos.x, y = pos.y, z = pos.z},
                region = GetPlayerRegion()
            }
            
            table.insert(resourceCheckpoints, checkpoint)
            Log("Created checkpoint '" .. name .. "' at position " .. 
                pos.x .. ", " .. pos.y .. ", " .. pos.z ..
                " in region " .. checkpoint.region)
        end
    )
    
    RegisterCommand(
        "checkpoints",
        "Lists all resource checkpoints",
        "checkpoints",
        function(args)
            if #resourceCheckpoints == 0 then
                Log("No checkpoints have been created yet.")
                return
            end
            
            Log("Resource Checkpoints:")
            for i, checkpoint in ipairs(resourceCheckpoints) do
                local pos = checkpoint.position
                Log(i .. ". " .. checkpoint.name .. 
                    " - Position: " .. pos.x .. ", " .. pos.y .. ", " .. pos.z ..
                    " - Region: " .. checkpoint.region)
            end
        end
    )
    
    RegisterCommand(
        "goto_checkpoint",
        "Teleport to a checkpoint by index",
        "goto_checkpoint <index>",
        function(args)
            if #args < 1 then
                LogError("Please specify a checkpoint index. Use 'checkpoints' to see available checkpoints.")
                return
            end
            
            local index = tonumber(args[1])
            if not index then
                LogError("Invalid checkpoint index. Please provide a number.")
                return
            end
            
            if index < 1 or index > #resourceCheckpoints then
                LogError("Checkpoint index out of range. Use 'checkpoints' to see available checkpoints.")
                return
            end
            
            local checkpoint = resourceCheckpoints[index]
            local pos = checkpoint.position
            SetPlayerPosition(pos.x, pos.y, pos.z)
            Log("Teleported to checkpoint: " .. checkpoint.name)
        end
    )
end

-- Called when the player is fully loaded and ready
function OnPlayerReady()
    Log("Resource Management system active!")
    
    -- Get initial player state
    playerEnergy = GetPlayerEnergy()
    playerHealth = GetPlayerHealth()
    lastPosition = GetPlayerPosition()
    lastRegion = GetPlayerRegion()
    lastUpdateTime = GetGameTime()
    
    -- Log initial resource state
    Log("Initial resources - Health: " .. playerHealth .. " Energy: " .. playerEnergy)
    Log("Player starting in region: " .. lastRegion)
    
    -- Create initial checkpoint at spawn location
    local pos = GetPlayerPosition()
    local spawnCheckpoint = {
        name = "Spawn Point",
        position = {x = pos.x, y = pos.y, z = pos.z},
        region = lastRegion
    }
    table.insert(resourceCheckpoints, spawnCheckpoint)
    
    -- Show tutorial message
    ShowNotification("Resource Management Active: Watch your health and energy!")
end

-- Called when player health changes
function OnPlayerHealthChanged(newHealth)
    playerHealth = newHealth
    
    if newHealth < 30 then
        ShowNotification("Warning: Health critically low!")
    end
end

-- Called when player energy changes
function OnPlayerEnergyChanged(newEnergy)
    playerEnergy = newEnergy
    
    if newEnergy < 20 then
        ShowNotification("Warning: Energy running low! Find food or rest!")
    end
end

-- Called when the player changes regions
function OnPlayerRegionChanged(newRegion)
    if newRegion and lastRegion and newRegion ~= lastRegion then
        Log("Region changed from " .. lastRegion .. " to " .. newRegion)
        lastRegion = newRegion
        
        -- Create a checkpoint at the region boundary
        local pos = GetPlayerPosition()
        local regionCheckpoint = {
            name = "Region: " .. newRegion,
            position = {x = pos.x, y = pos.y, z = pos.z},
            region = newRegion
        }
        table.insert(resourceCheckpoints, regionCheckpoint)
        
        -- Different regions might have different resource consumption rates
        AdjustResourceConsumptionForRegion(newRegion)
    end
end

-- Adjust resource consumption based on region
function AdjustResourceConsumptionForRegion(region)
    -- Example regional adjustments
    if region == "Downtown" then
        -- Downtown has more resources available
        ShowNotification("Downtown: Energy consumption reduced")
    elseif region == "Industrial" then
        -- Industrial areas might be more hazardous
        ShowNotification("Industrial Area: Health regeneration reduced")
    elseif region == "Residential" then
        -- Residential areas are peaceful
        ShowNotification("Residential Area: Resources balanced")
    elseif region == "Forest" or region == "Park" then
        -- Natural areas restore resources
        local currentEnergy = GetPlayerEnergy()
        SetPlayerEnergy(currentEnergy + 20)
        ShowNotification("Natural Area: Energy partially restored")
    end
end

-- Cleanup function called when script is unloaded
function Shutdown()
    -- Unregister all commands
    UnregisterCommand("resources")
    UnregisterCommand("restore")
    UnregisterCommand("checkpoint")
    UnregisterCommand("checkpoints")
    UnregisterCommand("goto_checkpoint")
    
    -- Save checkpoint data (in a real script, you'd save to persistent storage)
    Log("Resource Management script shutdown, " .. #resourceCheckpoints .. " checkpoints recorded")
end 