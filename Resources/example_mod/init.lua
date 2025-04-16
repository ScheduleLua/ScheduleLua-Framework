--[[
    Player Tools
    Description: A mod that provides helpful gameplay tools and enhancements for Schedule 1
    Author: Bars
    Version: 1.0.0
]]

-- Import our modules
local Economy = require("economy")
local PlayerStats = require("player_stats")

-- Register mod commands when console is ready
function OnConsoleReady()
    -- Register commands for economy functions
    RegisterCommand("money", "Shows your current money information", "", function()
        Economy.ShowPlayerMoneyInfo()
    end)
    
    RegisterCommand("transfer", "Transfer money to online account", "<amount>", function(args)
        if #args < 1 then
            LogWarning("Usage: transfer <amount>")
            return
        end
        
        local amount = tonumber(args[1])
        if amount and amount > 0 then
            Economy.TransferToOnlineBalance(amount)
        else
            LogWarning("Amount must be a positive number")
        end
    end)
    
    -- Register commands for player stats
    RegisterCommand("stats", "Shows your current player stats", "", function()
        PlayerStats.ShowPlayerStats()
    end)
    
    RegisterCommand("heal", "Restore player health", "[amount]", function(args)
        local amount = 100
        if #args >= 1 then
            amount = tonumber(args[1]) or 100
        end
        PlayerStats.RestoreHealth(amount)
    end)
    
    RegisterCommand("energy", "Restore player energy", "[amount]", function(args)
        local amount = 100
        if #args >= 1 then
            amount = tonumber(args[1]) or 100
        end
        PlayerStats.RestoreEnergy(amount)
    end)
    
    Log("Player Tools: Commands registered")
end

-- Initialize function - called when script is loaded
function Initialize()
    Log("Player Tools: Initializing...")
end

-- Called when player is ready
function OnPlayerReady()
    Log("Player Tools: Player ready")
    
    -- Display welcome message
    Log("===================================")
    Log("Player Tools mod loaded successfully!")
    Log("Type 'money' to view your finances")
    Log("Type 'stats' to view your player stats")
    Log("===================================")
    
    -- Show initial player information
    Economy.ShowPlayerMoneyInfo()
    PlayerStats.ShowPlayerStats()
end

-- Events we want to listen for
function OnPlayerHealthChanged(health)
    if health < 30 then
        Log("WARNING: Health is critically low!")
    end
end

function OnPlayerEnergyChanged(energy)
    if energy < 20 then
        Log("WARNING: Energy is critically low!")
    end
end

-- Day change event handler
function OnDayChanged(day)
    Log("It's a new day! Day " .. day)
    Economy.ShowPlayerMoneyInfo()
end

-- Shutdown function
function Shutdown()
    Log("Player Tools: Shutting down...")
end

Log("Player Tools: Main script loaded")