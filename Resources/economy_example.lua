-- ScheduleLua Example: Economy Functions
-- This script demonstrates how to use the Economy API to interact with the game's money system

-- Store functions to be initialized only after player is ready
local EconomyFunctions = {}
local currentScene = ""

-- Initialize function called when script is first loaded
function Initialize()
    Log("Economy API example initialized!")
    return true
end

-- Display player's money information
function EconomyFunctions.ShowPlayerMoneyInfo()
    local cash = GetPlayerCash()
    local online = GetPlayerOnlineBalance()
    local lifetime = GetLifetimeEarnings()
    local netWorth = GetNetWorth()
    
    Log("====== Player Money Information ======")
    Log("Cash on hand: " .. FormatMoney(cash))
    Log("Online balance: " .. FormatMoney(online))
    Log("Lifetime earnings: " .. FormatMoney(lifetime))
    Log("Total net worth: " .. FormatMoney(netWorth))
    Log("======================================")
end

-- Add cash to the player (100)
function EconomyFunctions.AddSomeCash()
    local amountToAdd = 100
    
    Log("Adding " .. FormatMoney(amountToAdd) .. " to your cash...")
    
    if AddPlayerCash(amountToAdd) then
        Log("Successfully added money! New balance: " .. FormatMoney(GetPlayerCash()))
    else
        LogError("Failed to add money!")
    end
end

-- Create a transaction for buying an item
function EconomyFunctions.MakePurchase(itemName, price, quantity, useOnlineBalance)
    if not itemName or not price or not quantity then
        LogError("MakePurchase requires itemName, price, and quantity")
        return false
    end
    
    -- Default to using cash if not specified
    useOnlineBalance = useOnlineBalance or false
    
    local totalCost = price * quantity
    local canAfford = false
    
    -- Check if player can afford based on payment method
    if useOnlineBalance then
        canAfford = GetPlayerOnlineBalance() >= totalCost
    else
        canAfford = CheckIfCanAfford(totalCost)
    end
    
    if not canAfford then
        LogWarning("Cannot afford " .. quantity .. "x " .. itemName .. " for " .. FormatMoney(totalCost))
        return false
    end
    
    -- Create transaction with the specified payment method
    if CreateTransaction("Purchase: " .. itemName, price, quantity, useOnlineBalance) then
        Log("Successfully purchased " .. quantity .. "x " .. itemName .. " for " .. FormatMoney(totalCost))
        Log("Payment method: " .. (useOnlineBalance and "Online Balance" or "Cash"))
        return true
    else
        LogError("Failed to process payment")
        return false
    end
end

-- Transfer money between cash and online balance
function EconomyFunctions.TransferToOnlineBalance(amount)
    if not amount or amount <= 0 then
        LogError("Transfer amount must be positive")
        return false
    end
    
    Log("Transferring " .. FormatMoney(amount) .. " from cash to online balance...")
    
    if RemovePlayerCash(amount) and AddOnlineBalance(amount) then
        Log("Transfer successful!")
        Log("New cash balance: " .. FormatMoney(GetPlayerCash()))
        Log("New online balance: " .. FormatMoney(GetPlayerOnlineBalance()))
        return true
    else
        LogError("Transfer failed!")
        return false
    end
end

-- Transfer money from online balance to cash
function EconomyFunctions.TransferToCash(amount)
    if not amount or amount <= 0 then
        LogError("Transfer amount must be positive")
        return false
    end
    
    Log("Transferring " .. FormatMoney(amount) .. " from online balance to cash...")
    
    if RemoveOnlineBalance(amount) and AddPlayerCash(amount) then
        Log("Transfer successful!")
        Log("New cash balance: " .. FormatMoney(GetPlayerCash()))
        Log("New online balance: " .. FormatMoney(GetPlayerOnlineBalance()))
        return true
    else
        LogError("Transfer failed!")
        return false
    end
end

-- Initialize player-dependent code only when player is ready
function OnPlayerReady()
    Log("Economy example: Player is ready, economy functions initialized")
    
    -- Show the player's current cash and online balance
    local cash = GetPlayerCash()
    local online = GetPlayerOnlineBalance()
    
    Log("====== Initial Player Finances ======")
    Log("Cash on hand: " .. FormatMoney(cash))
    Log("Online balance: " .. FormatMoney(online))
    Log("Total: " .. FormatMoney(cash + online))
    Log("===================================")
    
    Log("Use 'economy_examples' console command to see more detailed information")
end

function OnSceneLoaded(sceneName)
    currentScene = sceneName
end

-- Register console commands when the console is ready
function OnConsoleReady()
    if currentScene == "Menu" then return false end

    Log("Economy example: Console is ready, registering commands")
    
    -- Register economy examples command using proper syntax
    RegisterCommand(
        "economy_examples",
        "Demonstrates the Economy API functions",
        "economy_examples",
        function(args)
            Log("Running economy API examples...")
            
            -- Display initial money info
            EconomyFunctions.ShowPlayerMoneyInfo()
            
            -- Add some cash
            EconomyFunctions.AddSomeCash()
            
            -- Make a purchase using cash
            EconomyFunctions.MakePurchase("ogkush", 25.0, 2, false)
            
            -- Make another purchase using online balance
            EconomyFunctions.MakePurchase("ogkush", 25.0, 1, true)

            EconomyFunctions.ShowPlayerMoneyInfo()
            
            -- Transfer some money to online balance
            EconomyFunctions.TransferToOnlineBalance(250)
        end
    )
    
    -- Command to transfer money between cash and online balance
    RegisterCommand(
        "transfer",
        "Transfer money between cash and online balance",
        "transfer <direction> <amount>",
        function(args)
            if not args or #args < 2 then
                LogWarning("Usage: transfer <direction> <amount>")
                LogWarning("  direction: 'to_online' or 'to_cash'")
                LogWarning("  amount: amount to transfer")
                return
            end
            
            local direction = args[1]
            local amount = tonumber(args[2])
            
            if not amount then
                LogError("Amount must be a number")
                return
            end
            
            if direction == "to_online" then
                EconomyFunctions.TransferToOnlineBalance(amount)
            elseif direction == "to_cash" then
                EconomyFunctions.TransferToCash(amount)
            else
                LogWarning("Direction must be 'to_online' or 'to_cash'")
            end
        end
    )
    
    Log("Economy commands registered. Available commands:")
    Log("  economy_examples - Run a demonstration of economy functions")
    Log("  transfer <direction> <amount> - Transfer money between cash and online")
end

-- Update function called every frame (use sparingly)
function Update()
    -- This script doesn't need to do anything in Update
end

-- Cleanup function called when script is unloaded
function Shutdown()
    -- Unregister all commands
    UnregisterCommand("economy_examples")
    UnregisterCommand("transfer")
    
    Log("Economy example script shutdown, all commands unregistered")
end

-- Initial script load message
Log("Economy example script loaded.")