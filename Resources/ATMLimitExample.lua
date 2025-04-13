-- ATM Limit Example Script
-- This script demonstrates how to use the ATM Harmony patching to modify ATM deposit limits

-- Track ATM limit changes
local originalLimit = 10000.0
local presetLimits = {
    ["Default"] = 10000.0,
    ["Medium"] = 25000.0, 
    ["High"] = 50000.0,
    ["Very High"] = 100000.0,
    ["Unlimited"] = 999999.0
}

-- Initialize function called when script is first loaded
function Initialize()
    Log("ATM Limit Example script initialized!")
    return true
end

-- Called when the console is fully loaded and ready
function OnConsoleReady()
    -- Register console commands for ATM limits
    RegisterCommand(
        "atmlimit",
        "Shows or sets the ATM deposit limit using Harmony patching",
        "atmlimit [amount/preset]",
        function(args)
            if #args == 0 then
                -- No args, show current limit
                local currentLimit = GetATMDepositLimit()
                Log("Current ATM deposit limit: " .. FormatMoney(currentLimit))
                Log("Available presets: default, medium, high, veryhigh, unlimited")
                for name, limit in pairs(presetLimits) do
                    Log("  - " .. name .. ": " .. FormatMoney(limit))
                end
            else
                -- Try to set the limit
                local newLimit
                local presetName = string.lower(args[1])
                
                -- Check if it's a preset name
                if presetName == "default" then
                    newLimit = presetLimits["Default"]
                elseif presetName == "medium" then
                    newLimit = presetLimits["Medium"]
                elseif presetName == "high" then
                    newLimit = presetLimits["High"]
                elseif presetName == "veryhigh" then
                    newLimit = presetLimits["Very High"]
                elseif presetName == "unlimited" then
                    newLimit = presetLimits["Unlimited"]
                else
                    -- Try to parse as a number
                    newLimit = tonumber(args[1])
                    if not newLimit then
                        LogError("Invalid limit. Please specify a number or preset (default, medium, high, veryhigh, unlimited)")
                        return
                    end
                end
                
                -- Set the new limit
                Log("Applying Harmony patches for ATM deposit limit: " .. FormatMoney(newLimit))
                if SetATMDepositLimit(newLimit) then
                    Log("Successfully applied patches for ATM deposit limit: " .. FormatMoney(newLimit))
                    Log("Try visiting an ATM to see the new limit in action.")
                    Log("Note: This change affects all ATMs in the game!")
                else
                    LogError("Failed to apply patches for ATM deposit limit")
                end
            end
        end
    )
    
    RegisterCommand(
        "resetatmlimit",
        "Resets the ATM deposit limit to the default value",
        "resetatmlimit",
        function(args)
            if SetATMDepositLimit(originalLimit) then
                Log("Applied Harmony patches to reset ATM deposit limit to default: " .. FormatMoney(originalLimit))
            else
                LogError("Failed to reset ATM deposit limit")
            end
        end
    )
    
    RegisterCommand(
        "findatms",
        "Shows information about ATMs in the game world",
        "findatms",
        function(args)
            Log("Checking for ATM objects in the game...")
            local currentLimit = GetATMDepositLimit()
            Log("Current ATM deposit limit: " .. FormatMoney(currentLimit))
            Log("ATM patching status: Active")
            Log("Note: Changes made via the atmlimit command will apply to ALL ATMs in the game!")
            Log("Use 'atmlimit' command to change the limit value")
        end
    )
    
    Log("ATM Limit commands registered: 'atmlimit', 'resetatmlimit', 'findatms'")
end

-- Called when the player is fully loaded and ready
function OnPlayerReady()
    Log("ATM Limit Example: Player is ready!")
    
    -- Store the original limit when we start
    originalLimit = GetATMDepositLimit()
    Log("Current ATM deposit limit: " .. FormatMoney(originalLimit))
    
    -- Display available presets
    Log("Available ATM limit presets:")
    for name, limit in pairs(presetLimits) do
        Log("  - " .. name .. ": " .. FormatMoney(limit))
    end
    
    Log("Use the 'atmlimit' command to view or change the limit.")
end

-- Cleanup function called when script is unloaded
function Shutdown()
    -- Unregister all commands
    UnregisterCommand("atmlimit")
    UnregisterCommand("resetatmlimit")
    UnregisterCommand("findatms")
    
    Log("ATM Limit Example script shutdown, all commands unregistered")
end

-- Return true to indicate successful execution
return true 