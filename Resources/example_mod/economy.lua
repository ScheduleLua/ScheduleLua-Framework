--[[
    economy.lua
    Description: Economy module for Player Tools mod
    Author: Bars
    Version: 1.0.0
]]

-- Create a module for economy-related functions
local EconomyModule = {}

-- Format money with currency symbol and commas
function EconomyModule.FormatMoney(amount)
    -- Format with commas for thousands
    local formattedAmount = tostring(amount)
    local formatted = formattedAmount:reverse():gsub("(%d%d%d)", "%1,"):reverse():gsub("^,", "")
    return "$" .. formatted
end

-- Display player's money information
function EconomyModule.ShowPlayerMoneyInfo()
    local cash = GetPlayerCash()
    local online = GetPlayerOnlineBalance()
    local lifetime = GetLifetimeEarnings() 
    local netWorth = GetNetWorth()
    
    Log("====== Player Money Information ======")
    Log("Cash on hand: " .. EconomyModule.FormatMoney(cash))
    Log("Online balance: " .. EconomyModule.FormatMoney(online))
    Log("Lifetime earnings: " .. EconomyModule.FormatMoney(lifetime))
    Log("Net worth: " .. EconomyModule.FormatMoney(netWorth))
    Log("======================================")
end

-- Transfer money between cash and online balance
function EconomyModule.TransferToOnlineBalance(amount)
    -- Parameter validation
    if not amount or amount <= 0 then
        LogWarning("Transfer amount must be positive")
        return false
    end
    
    -- Check if player has enough cash
    local cash = GetPlayerCash()
    if cash < amount then
        LogWarning("Not enough cash! You only have " .. EconomyModule.FormatMoney(cash))
        return false
    end
    
    -- Perform the transfer
    RemovePlayerCash(amount)
    AddOnlineBalance(amount)
    Log("Transfer successful!")
    Log("Transferred " .. EconomyModule.FormatMoney(amount) .. " to online account")
    Log("New cash balance: " .. EconomyModule.FormatMoney(GetPlayerCash()))
    Log("New online balance: " .. EconomyModule.FormatMoney(GetPlayerOnlineBalance()))
    return true
end

-- Transfer money from online to cash
function EconomyModule.WithdrawFromOnline(amount)
    -- Parameter validation
    if not amount or amount <= 0 then
        LogWarning("Withdrawal amount must be positive")
        return false
    end
    
    -- Check if player has enough online balance
    local online = GetPlayerOnlineBalance()
    if online < amount then
        LogWarning("Not enough online balance! You only have " .. EconomyModule.FormatMoney(online))
        return false
    end
    
    -- Perform the withdrawal
    RemoveOnlineBalance(amount)
    AddPlayerCash(amount)
    Log("Withdrawal successful!")
    Log("Withdrew " .. EconomyModule.FormatMoney(amount) .. " from online account")
    Log("New cash balance: " .. EconomyModule.FormatMoney(GetPlayerCash()))
    Log("New online balance: " .. EconomyModule.FormatMoney(GetPlayerOnlineBalance()))
    return true
end

-- Return the module
return EconomyModule 