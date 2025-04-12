-- Example script showing how to use the Curfew API with Event System
-- This script will display curfew status and use events for curfew notifications

function Initialize()
    Log("Updated Curfew example script initialized!")
end

-- Function called every game frame (use sparingly)
function Update()
    -- Only check curfew status once per minute
    local gameTime = GetGameTime()
    if gameTime ~= lastCheckedTime then
        lastCheckedTime = gameTime
        
        -- If curfew is active, show a warning if player is outside
        if IsCurfewActive() then
            local playerRegion = GetPlayerRegion()
            if playerRegion and not string.match(playerRegion:lower(), "indoor") and not string.match(playerRegion:lower(), "house") then
                -- Player is outdoors during curfew!
                if not warnedAboutCurfew then
                    LogWarning("You are outside during curfew! Police will be more active.")
                    warnedAboutCurfew = true
                end
            else
                -- Player is inside, reset warning flag
                warnedAboutCurfew = false
            end
        else
            -- Not curfew time, reset warning flag
            warnedAboutCurfew = false
            
            -- If curfew is approaching, give a countdown
            local timeUntilCurfew = GetTimeUntilCurfew()
            if timeUntilCurfew > 0 and timeUntilCurfew <= 15 and (timeUntilCurfew % 5 == 0) then
                Log("Curfew begins in " .. timeUntilCurfew .. " minutes!")
            end
        end
    end
end

-- Prints detailed information about the curfew
function PrintCurfewInfo()
    Log("==== Curfew Information ====")
    Log("Is Enabled: " .. tostring(IsCurfewEnabled()))
    Log("Is Active: " .. tostring(IsCurfewActive()))
    Log("Is Active (with tolerance): " .. tostring(IsCurfewActiveWithTolerance()))
    Log("Start Time: " .. FormatGameTime(GetCurfewStartTime()))
    Log("End Time: " .. FormatGameTime(GetCurfewEndTime()))
    Log("Warning Time: " .. FormatGameTime(GetCurfewWarningTime()))
    
    local timeUntilCurfew = GetTimeUntilCurfew()
    if timeUntilCurfew > 0 then
        Log("Time until curfew: " .. timeUntilCurfew .. " minutes")
    end
    Log("===========================")
end

-- Event system handler for curfew being enabled
function OnCurfewEnabled()
    Log("CURFEW SYSTEM HAS BEEN ENABLED")
    PrintCurfewInfo()
end

-- Event system handler for curfew being disabled
function OnCurfewDisabled()
    Log("CURFEW SYSTEM HAS BEEN DISABLED")
    PrintCurfewInfo()
end

-- Event system handler for curfew warning
function OnCurfewWarning()
    Log("CURFEW WARNING: Curfew will begin at 9:00 PM")
end

-- Event system handler for curfew hint
function OnCurfewHint()
    Log("CURFEW HINT: During curfew (9 PM - 5 AM), stay indoors to avoid detection by police.")
end

-- Global variables
lastCheckedTime = 0
warnedAboutCurfew = false 