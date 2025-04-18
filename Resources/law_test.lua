-- Law API Testing Script
-- Tests the functionality of ScheduleLua's Law API

-- Track script state
local initialized = false

-- Initialize function called when script is first loaded
function Initialize()
    Log("Law API Test Script initialized!")
    initialized = true
end

-- Register commands for each test function
function OnConsoleReady()
    Log("Law API Test Script console ready!")
    
    -- Register main info command
    RegisterCommand("law_status", "Display law enforcement status", "law_status", function(args)
        ShowLawStatus()
    end)
    
    -- Register curfew commands
    RegisterCommand("curfew_status", "Display curfew information", "curfew_status", function(args)
        ShowCurfewStatus()
    end)
    
    RegisterCommand("curfew_toggle", "Toggle curfew on/off", "curfew_toggle", function(args)
        ToggleCurfew()
    end)
    
    -- Register police/law commands
    RegisterCommand("police_call", "Call police on yourself", "police_call", function(args)
        CallPoliceOnSelf()
    end)
    
    RegisterCommand("foot_patrol", "Start a foot patrol", "foot_patrol", function(args)
        StartFootPatrolCommand()
    end)
    
    RegisterCommand("vehicle_patrol", "Start a vehicle patrol", "vehicle_patrol", function(args)
        StartVehiclePatrolCommand()
    end)
    
    RegisterCommand("law_intensity", "Get or set law intensity", "law_intensity [value]", function(args)
        if #args == 0 then
            GetLawIntensityCommand()
        else
            local value = tonumber(args[1])
            SetLawIntensityCommand(value)
        end
    end)
    
    Log("Law API Test commands registered")
end

-- Called when player is fully loaded
function OnPlayerReady()
    Log("Player ready in Law API Test Script")
    
    -- Show current law status when player loads
    local intensity = GetLawIntensity()
    Log("Current law intensity: " .. string.format("%.2f", intensity))
    
    if IsCurfewEnabled() then
        Log("Curfew is currently enabled")
        if IsCurfewActive() then
            Log("Curfew is active - be careful outside!")
        else
            local timeUntilCurfew = GetTimeUntilCurfew()
            if timeUntilCurfew > 0 then
                Log("Curfew begins in " .. timeUntilCurfew .. " minutes")
            else
                Log("Curfew is enabled but not currently active")
            end
        end
    else
        Log("Curfew is disabled")
    end
end

-- Show overall law status information
function ShowLawStatus()
    local intensity = GetLawIntensity()
    
    local status = [[Law Status:

- Law Enforcement Intensity: ]] .. string.format("%.2f", intensity) .. [[

Curfew Status:
- Enabled: ]] .. tostring(IsCurfewEnabled()) .. [[
- Active: ]] .. tostring(IsCurfewActive()) .. [[
- Active (with tolerance): ]] .. tostring(IsCurfewActiveWithTolerance()) .. [[

Available Commands:
- law_status - Show this information
- curfew_status - Show detailed curfew information
- curfew_toggle - Toggle curfew on/off
- police_call - Call police on yourself
- foot_patrol - Start a foot patrol
- vehicle_patrol - Start a vehicle patrol
- law_intensity [value] - Get or set law intensity
]]

    Log(status)
    ShowNotification("Law status displayed in console")
end

-- Call police on the player
function CallPoliceOnSelf()
    Log("Calling police on self...")
    local success, err = pcall(PoliceCallOnSelf)
    if success then
        ShowNotification("Police have been called on you!")
    else
        LogError("Error calling police: " .. tostring(err))
        ShowNotification("Error calling police")
    end
end

-- Start a foot patrol
function StartFootPatrolCommand()
    Log("Starting foot patrol...")
    local success, err = pcall(StartFootPatrol)
    if success then
        ShowNotification("Started foot patrol!")
    else
        LogError("Error starting foot patrol: " .. tostring(err))
        ShowNotification("Error starting foot patrol")
    end
end

-- Start a vehicle patrol
function StartVehiclePatrolCommand()
    Log("Starting vehicle patrol...")
    local success, err = pcall(StartVehiclePatrol)
    if success then
        ShowNotification("Started vehicle patrol!")
    else
        LogError("Error starting vehicle patrol: " .. tostring(err))
        ShowNotification("Error starting vehicle patrol")
    end
end

-- Get the current law intensity
function GetLawIntensityCommand()
    local success, intensity = pcall(GetLawIntensity)
    if success then
        Log("Current law intensity: " .. string.format("%.2f", intensity))
        ShowNotification("Current law intensity: " .. string.format("%.2f", intensity))
    else
        LogError("Error getting law intensity: " .. tostring(intensity))
        ShowNotification("Error getting law intensity")
    end
end

-- Set the law intensity to a value
function SetLawIntensityCommand(value)
    if not value or value < 0.0 or value > 1.0 then
        ShowNotification("Invalid value. Must be between 0.0 and 1.0.")
        return
    end
    
    local success, err = pcall(function() 
        SetLawIntensity(value)
    end)
    
    if success then
        Log("Law intensity set to: " .. string.format("%.2f", value))
        ShowNotification("Law intensity set to: " .. string.format("%.2f", value))
    else
        LogError("Error setting law intensity: " .. tostring(err))
        ShowNotification("Error setting law intensity")
    end
end

-- Toggle curfew on/off
function ToggleCurfew()
    if IsCurfewEnabled() then
        local success, err = pcall(DisableCurfew)
        if success then
            Log("Curfew disabled")
            ShowNotification("Curfew has been disabled")
        else
            LogError("Error disabling curfew: " .. tostring(err))
            ShowNotification("Error disabling curfew")
        end
    else
        local success, err = pcall(EnableCurfew)
        if success then
            Log("Curfew enabled")
            ShowNotification("Curfew has been enabled")
        else
            LogError("Error enabling curfew: " .. tostring(err))
            ShowNotification("Error enabling curfew")
        end
    end
end

-- Detailed curfew status display
function ShowCurfewStatus()
    local status = [[Curfew Status:

- Enabled: ]] .. tostring(IsCurfewEnabled()) .. [[
- Active: ]] .. tostring(IsCurfewActive()) .. [[
- Active (with tolerance): ]] .. tostring(IsCurfewActiveWithTolerance()) .. [[
- Curfew Start Time: ]] .. GetCurfewStartTime() .. [[ (]] .. FormatGameTime(GetCurfewStartTime()) .. [[)
- Curfew End Time: ]] .. GetCurfewEndTime() .. [[ (]] .. FormatGameTime(GetCurfewEndTime()) .. [[)
- Curfew Warning Time: ]] .. GetCurfewWarningTime() .. [[ (]] .. FormatGameTime(GetCurfewWarningTime()) .. [[)
- Time Until Curfew: ]] .. GetTimeUntilCurfew() .. [[ minutes

Current Game Time: ]] .. FormatGameTime(GetGameTime()) .. [[
]]

    Log(status)
    ShowNotification("Curfew status displayed in console")
end

-- Called when game time changes
function OnTimeChanged(time)
    -- Check for approaching curfew
    if IsCurfewEnabled() and not IsCurfewActive() then
        local timeUntilCurfew = GetTimeUntilCurfew()
        -- Warn at 30, 15, and 5 minutes before curfew
        if timeUntilCurfew == 30 or timeUntilCurfew == 15 or timeUntilCurfew == 5 then
            ShowNotification("Curfew begins in " .. timeUntilCurfew .. " minutes!")
        end
    end
end

-- Event handlers for curfew events
function OnCurfewEnabled()
    Log("EVENT: Curfew has been enabled")
    ShowNotification("Curfew system has been enabled")
end

function OnCurfewDisabled()
    Log("EVENT: Curfew has been disabled")
    ShowNotification("Curfew system has been disabled")
end

function OnCurfewWarning()
    Log("EVENT: Curfew warning received")
    ShowNotification("WARNING: Curfew will begin soon!")
end

function OnCurfewHint()
    Log("EVENT: Curfew hint received")
    ShowNotification("HINT: During curfew, stay indoors to avoid police")
end

-- Cleanup function called when script is unloaded
function Shutdown()
    -- Unregister all commands
    UnregisterCommand("law_status")
    UnregisterCommand("curfew_status")
    UnregisterCommand("curfew_toggle")
    UnregisterCommand("police_call")
    UnregisterCommand("foot_patrol")
    UnregisterCommand("vehicle_patrol")
    UnregisterCommand("law_intensity")
    
    Log("Law API Test script shutdown, all commands unregistered")
    return true
end

-- Print initial loading message
Log("Law API Test Script loaded!")
