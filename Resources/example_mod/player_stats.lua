--[[
    player_stats.lua
    Description: Player stats module for Player Tools mod
    Author: Bars
    Version: 1.0.0
]]

-- Create a module for player-related functions
local PlayerStatsModule = {}

-- Format percentage with 2 decimal places
function PlayerStatsModule.FormatPercent(value)
    return string.format("%.2f%%", value * 100)
end

-- Display player stats
function PlayerStatsModule.ShowPlayerStats()
    local health = GetPlayerHealth()
    -- Use a fixed value for max health instead of GetPlayerMaxHealth()
    local maxHealth = 100
    local energy = GetPlayerEnergy()
    -- Use a fixed value for max energy instead of GetPlayerMaxEnergy()
    local maxEnergy = 100
    
    -- Get player position
    local position = GetPlayerPosition()
    local region = GetPlayerRegion()
    
    Log("========= Player Statistics =========")
    Log("Health: " .. health .. "/" .. maxHealth .. " (" .. PlayerStatsModule.FormatPercent(health/maxHealth) .. ")")
    Log("Energy: " .. energy .. "/" .. maxEnergy .. " (" .. PlayerStatsModule.FormatPercent(energy/maxEnergy) .. ")")
    Log("Current region: " .. (region or "Unknown"))
    
    if position then
        Log("Position: X=" .. position.x .. ", Y=" .. position.y .. ", Z=" .. position.z)
    end
    
    -- Get player legal status if available
    local wantedLevel = GetPlayerWantedLevel and GetPlayerWantedLevel() or 0
    if wantedLevel > 0 then
        Log("WARNING: You are wanted by law enforcement (Level " .. wantedLevel .. ")")
    end
    
    Log("====================================")
end

-- Restore player health
function PlayerStatsModule.RestoreHealth(amount)
    amount = amount or 100
    
    local currentHealth = GetPlayerHealth()
    -- Use fixed max health
    local maxHealth = 100
    
    -- Don't exceed max health
    local newHealth = math.min(currentHealth + amount, maxHealth)
    local restored = newHealth - currentHealth
    
    SetPlayerHealth(newHealth)
    Log("Restored " .. restored .. " health points")
    Log("Current health: " .. newHealth .. "/" .. maxHealth)
    return true
end

-- Restore player energy
function PlayerStatsModule.RestoreEnergy(amount)
    amount = amount or 100
    
    local currentEnergy = GetPlayerEnergy()
    -- Use fixed max energy
    local maxEnergy = 100
    
    -- Don't exceed max energy
    local newEnergy = math.min(currentEnergy + amount, maxEnergy)
    local restored = newEnergy - currentEnergy
    
    SetPlayerEnergy(newEnergy)
    Log("Restored " .. restored .. " energy points")
    Log("Current energy: " .. newEnergy .. "/" .. maxEnergy)
    return true
end

-- Teleport player to a specified location
function PlayerStatsModule.TeleportPlayer(x, y, z)
    local position = Vector3(x, y, z)
    SetPlayerPosition(position)
    Log("Teleported to X=" .. x .. ", Y=" .. y .. ", Z=" .. z)
    return true
end

-- Return the module
return PlayerStatsModule 