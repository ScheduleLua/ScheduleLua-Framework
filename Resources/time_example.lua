-- Time Management Example Script

function Initialize()
    Log("Time management script initialized!")
end

function OnConsoleReady()
    -- Register a command to display time information
    RegisterCommand(
        "timeinfo",
        "Shows detailed time information",
        "timeinfo",
        function(args)
            ShowTimeInfo()
        end
    )
end

function ShowTimeInfo()
    -- Get current time information
    local currentTime = GetGameTime()
    local formattedTime = FormatGameTime(currentTime)
    local dayString = GetGameDay()
    local dayNumber = GetGameDayInt()
    
    Log("Current time: " .. formattedTime)
    Log("Day (string): " .. dayString)
    Log("Day (number): " .. dayNumber)
    Log("Night time: " .. (IsNightTime() and "Yes" or "No"))
end

-- Helper function to get day name from number
function GetDayName(dayNumber)
    local days = {
        [0] = "Sunday",
        [1] = "Monday",
        [2] = "Tuesday",
        [3] = "Wednesday",
        [4] = "Thursday",
        [5] = "Friday",
        [6] = "Saturday"
    }
    return days[dayNumber] or "Unknown"
end

-- Called when game time changes
function OnTimeChanged(time)
    -- Only log occasionally to avoid spam
    if time % 6 == 0 then
        Log("Time updated: " .. FormatGameTime(time))
    end
end

-- Called when the game day changes
function OnDayChanged(day)
    Log("Day changed to: " .. day)
    Log("Day number is now: " .. GetGameDayInt())
    
    -- Do something special on specific days
    if day == "Sunday" then
        Log("It's Sunday! Day of rest.")
    elseif day == "Friday" then
        Log("It's Friday! Weekend is coming.")
    end
end