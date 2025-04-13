-- shop.lua
-- A simple shop system using economy.lua functions

-- Require the economy module
local EconomyFunctions = require("economy_example")

local Shop = {}

-- Define some shop items
Shop.items = {
    {
        name = "baggie",
        price = 1,
        description = "Baggie"
    },
    {
        name = "ogkush",
        price = 75,
        description = "OG Kush"
    }
}

-- Initialize function
function Initialize()
    Log("Shop system initialized!")
    return true
end

-- Display shop menu
local function ShowShopMenu()
    Log("====== Welcome to the Shop ======")
    Log("Your current finances:")
    EconomyFunctions.ShowPlayerMoneyInfo()
    Log("\nAvailable Items:")
    
    for i, item in ipairs(Shop.items) do
        Log(string.format("%d. %s - %s", i, item.name, FormatMoney(item.price)))
        Log("   " .. item.description)
    end
    Log("==============================")
end

-- Purchase item from shop
local function PurchaseItem(itemIndex, quantity, useOnlineBalance)
    if not itemIndex or itemIndex < 1 or itemIndex > #Shop.items then
        LogError("Invalid item index!")
        return false
    end
    
    quantity = quantity or 1
    local item = Shop.items[itemIndex]
    
    -- Attempt to make the purchase
    local success = EconomyFunctions.MakePurchase(
        item.name,
        item.price,
        quantity,
        useOnlineBalance
    )
    
    if success then
        Log(string.format("Thank you for purchasing %dx %s!", quantity, item.name))
        return true
    else
        Log("Purchase failed. Please check your balance.")
        return false
    end
end

-- Register console commands when the console is ready
function OnConsoleReady()
    Log("Shop system: Registering commands")
    
    -- Register shop command
    RegisterCommand(
        "shop",
        "Open the shop menu",
        "shop",
        function(args)
            ShowShopMenu()
        end
    )
    
    -- Register buy command
    RegisterCommand(
        "buy",
        "Buy an item from the shop",
        "buy <item_number> [quantity] [use_online_balance]",
        function(args)
            if not args or #args < 1 then
                LogWarning("Usage: buy <item_number> [quantity] [use_online_balance]")
                return
            end
            
            local itemIndex = tonumber(args[1])
            local quantity = tonumber(args[2]) or 1
            local useOnline = args[3] == "true" or args[3] == "1"
            
            PurchaseItem(itemIndex, quantity, useOnline)
        end
    )
    
    Log("Shop commands registered. Available commands:")
    Log("  shop - Show the shop menu")
    Log("  buy <item_number> [quantity] [use_online_balance] - Purchase an item")
end

-- Cleanup function
function Shutdown()
    UnregisterCommand("shop")
    UnregisterCommand("buy")
    Log("Shop system shutdown")
end

-- Initial script load message
Log("Shop system loaded.")
