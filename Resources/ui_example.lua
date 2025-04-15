-- UI Example Script

-- Log that we're starting
Log("Starting UI Example script")

-- Helper function to close a dialogue after a delay
local function ShowTemporaryDialogue(title, text, delay)
    delay = delay or 5.0  -- Default to 5 seconds
    
    -- Show the dialogue
    ShowDialogue(title, text)
    
    -- Schedule closing the dialogue
    Wait(delay, function()
        CloseDialogue()
    end)
end

-- Global variables to track UI state and elements
local isUIVisible = false
local mainWindowId = nil
local isMainSceneLoaded = false
local commandRegistered = false
local currentTheme = "blue" -- Default theme

-- Button/Control IDs
local showTooltipBtnId = nil
local togglePhoneBtnId = nil
local toggleFlashlightBtnId = nil
local showDialogBtnId = nil
local showChoicesBtnId = nil
local statusLabelId = nil

-- Theme definitions
local themes = {
    blue = {
        window = {background = {0.1, 0.1, 0.3, 0.95}, text = {1, 1, 1, 1}},
        button = {background = {0.2, 0.2, 0.8, 0.9}, text = {1, 1, 1, 1}, hover = {0.3, 0.3, 0.9, 0.9}},
        label = {text = {1, 1, 1, 1}},
        textfield = {background = {0.15, 0.15, 0.25, 0.95}, text = {1, 1, 1, 1}}
    },
    dark = {
        window = {background = {0.1, 0.1, 0.1, 0.95}, text = {0.9, 0.9, 0.9, 1}},
        button = {background = {0.2, 0.2, 0.2, 0.9}, text = {0.9, 0.9, 0.9, 1}, hover = {0.3, 0.3, 0.3, 0.9}},
        label = {text = {0.9, 0.9, 0.9, 1}},
        textfield = {background = {0.15, 0.15, 0.15, 0.95}, text = {0.9, 0.9, 0.9, 1}}
    },
    green = {
        window = {background = {0.1, 0.2, 0.1, 0.95}, text = {1, 1, 1, 1}},
        button = {background = {0.2, 0.5, 0.2, 0.9}, text = {1, 1, 1, 1}, hover = {0.3, 0.6, 0.3, 0.9}},
        label = {text = {0.8, 1, 0.8, 1}},
        textfield = {background = {0.15, 0.25, 0.15, 0.95}, text = {1, 1, 1, 1}}
    },
    red = {
        window = {background = {0.2, 0.1, 0.1, 0.95}, text = {1, 1, 1, 1}},
        button = {background = {0.6, 0.2, 0.2, 0.9}, text = {1, 1, 1, 1}, hover = {0.7, 0.3, 0.3, 0.9}},
        label = {text = {1, 0.9, 0.9, 1}},
        textfield = {background = {0.25, 0.15, 0.15, 0.95}, text = {1, 1, 1, 1}}
    }
}

-- Apply a theme to the UI
local function ApplyTheme(themeName)
    local theme = themes[themeName]
    if not theme then
        Log("Theme not found: " .. themeName)
        return
    end
    
    Log("Applying theme: " .. themeName)
    
    -- Apply window style
    if theme.window.background then
        local bg = theme.window.background
        SetWindowStyle("background", bg[1], bg[2], bg[3], bg[4])
    end
    if theme.window.text then
        local txt = theme.window.text
        SetWindowStyle("text", txt[1], txt[2], txt[3], txt[4])
    end
    
    -- Apply button style
    if theme.button.background then
        local bg = theme.button.background
        SetButtonStyle("background", bg[1], bg[2], bg[3], bg[4])
    end
    if theme.button.text then
        local txt = theme.button.text
        SetButtonStyle("text", txt[1], txt[2], txt[3], txt[4])
    end
    if theme.button.hover then
        local hov = theme.button.hover
        SetButtonStyle("hover", hov[1], hov[2], hov[3], hov[4])
    end
    
    -- Apply label style
    if theme.label.text then
        local txt = theme.label.text
        SetLabelStyle("text", txt[1], txt[2], txt[3], txt[4])
    end
    
    -- Apply text field style
    if theme.textfield.background then
        local bg = theme.textfield.background
        SetTextFieldStyle("background", bg[1], bg[2], bg[3], bg[4])
    end
    if theme.textfield.text then
        local txt = theme.textfield.text
        SetTextFieldStyle("text", txt[1], txt[2], txt[3], txt[4])
    end
    
    currentTheme = themeName
    
    -- Only set label text if status label exists
    if statusLabelId then
        SetControlText(statusLabelId, "Status: Theme changed to " .. themeName)
    end
end

-- Function to create the UI
local function CreateUI()
    if mainWindowId ~= nil then
        -- UI already created, just show it
        ShowWindow(mainWindowId, true)
        ShowNotification("UI window shown")
        return
    end
    
    Log("Creating UI for UI Example script")
    
    -- Make sure GUI is enabled
    EnableGUI(true)
    Log("GUI Enabled: " .. tostring(IsGUIEnabled()))
    
    -- Create main window - parameters: id, title, x, y, width, height
    -- Using center position for better visibility
    local screenWidth = 800 -- Approximate screen width
    local screenHeight = 600 -- Approximate screen height
    local winWidth = 400
    local winHeight = 660  -- Increased height for extra buttons
    local x = (screenWidth - winWidth) / 2
    local y = (screenHeight - winHeight) / 2
    
    mainWindowId = CreateWindow("main_window", "Lua UI Example", x, y, winWidth, winHeight)
    Log("Window created with ID: " .. mainWindowId)
    
    -- Initialize styles with default theme
    ApplyTheme(currentTheme)
    
    -- Additional style customizations
    SetFontSize("window", 18)  -- Larger window title
    SetFontStyle("window", "bold")
    SetFontSize("button", 14)
    SetFontStyle("button", "bold")
    SetTextAlignment("button", "center")
    SetBorder("button", 5, 5, 5, 5)
    SetPadding("button", 5, 5, 5, 5)
    
    -- Add a status label at the top
    statusLabelId = AddLabel(mainWindowId, "status_label", "Status: Ready")
    SetControlPosition(statusLabelId, 10, 40)
    SetControlSize(statusLabelId, 380, 30)
    
    -- Add test label
    local testLabelId = AddLabel(mainWindowId, "test_label", "THIS IS A TEST LABEL - UI IS WORKING")
    SetControlPosition(testLabelId, 10, 80)
    SetControlSize(testLabelId, 380, 30)
    SetFontStyle("label", "bold")
    SetTextAlignment("label", "center")
    
    -- Add buttons with callbacks
    showTooltipBtnId = AddButton(mainWindowId, "tooltip_btn", "Show Tooltip", function()
        Log("Showing tooltip")
        -- Show a tooltip at the mouse position
        -- Use fixed position since we don't have direct Input access in Lua
        local mouseX = 100
        local mouseY = 100
        ShowTooltip("This is a sample tooltip from Lua!", mouseX, mouseY, false)
        ShowNotification("Tooltip displayed!")
        SetControlText(statusLabelId, "Status: Tooltip shown")
    end)
    SetControlPosition(showTooltipBtnId, 50, 120)
    SetControlSize(showTooltipBtnId, 300, 40)
    
    togglePhoneBtnId = AddButton(mainWindowId, "phone_btn", "Toggle Phone", function()
        if IsPhoneOpen() then
            Log("Closing phone")
            ClosePhone()
            ShowNotification("Phone closed via Lua!")
            SetControlText(statusLabelId, "Status: Phone closed")
        else
            Log("Opening phone")
            OpenPhone()
            ShowNotification("Phone opened via Lua!")
            SetControlText(statusLabelId, "Status: Phone opened")
        end
    end)
    SetControlPosition(togglePhoneBtnId, 50, 180)
    SetControlSize(togglePhoneBtnId, 300, 40)
    
    toggleFlashlightBtnId = AddButton(mainWindowId, "flashlight_btn", "Toggle Flashlight", function()
        Log("Toggling flashlight")
        TogglePhoneFlashlight()
        
        local status = IsPhoneFlashlightOn() and "ON" or "OFF"
        ShowNotification("Flashlight is now " .. status)
        SetControlText(statusLabelId, "Status: Flashlight " .. status)
    end)
    SetControlPosition(toggleFlashlightBtnId, 50, 240)
    SetControlSize(toggleFlashlightBtnId, 300, 40)
    
    showDialogBtnId = AddButton(mainWindowId, "dialog_btn", "Show Dialog", function()
        Log("Showing dialog")
        ShowDialogue("Lua Dialog", "This dialog was created from Lua!\n\nNote: This will stay open until manually closed.")
        SetControlText(statusLabelId, "Status: Dialog shown")
    end)
    SetControlPosition(showDialogBtnId, 50, 300)
    SetControlSize(showDialogBtnId, 300, 40)
    
    -- Add auto-close dialog button
    local autoCloseDialogBtnId = AddButton(mainWindowId, "auto_close_dialog_btn", "Auto-Close Dialog (5s)", function()
        Log("Showing auto-close dialog")
        
        -- Use pcall to catch any errors
        local success, err = pcall(function()
            ShowDialogueWithTimeout("Auto-Closing Dialog", "This dialog will automatically close after 5 seconds.\n\nUsing C# built-in timeout functionality.", 5.0)
        end)
        
        if success then
            SetControlText(statusLabelId, "Status: Auto-close dialog shown (5s)")
        else
            Log("Error showing auto-close dialog: " .. tostring(err))
            SetControlText(statusLabelId, "Status: Error showing dialog")
        end
    end)
    SetControlPosition(autoCloseDialogBtnId, 50, 360)
    SetControlSize(autoCloseDialogBtnId, 300, 40)
    
    -- Add Lua-based temporary dialog button
    local luaCloseDialogBtnId = AddButton(mainWindowId, "lua_close_dialog_btn", "Lua-Managed Dialog (3s)", function()
        Log("Showing Lua-managed temporary dialog")
        
        -- Use pcall to catch any errors
        local success, err = pcall(function()
            ShowTemporaryDialogue("Lua-Managed Dialog", "This dialog uses Lua Wait() to close after 3 seconds.\n\nUsing Lua-side timeout handling.", 3.0)
        end)
        
        if success then
            SetControlText(statusLabelId, "Status: Lua-managed dialog shown (3s)")
        else
            Log("Error showing Lua-managed dialog: " .. tostring(err))
            SetControlText(statusLabelId, "Status: Error showing dialog: " .. tostring(err))
        end
    end)
    SetControlPosition(luaCloseDialogBtnId, 50, 420)
    SetControlSize(luaCloseDialogBtnId, 300, 40)
    
    showChoicesBtnId = AddButton(mainWindowId, "choices_btn", "Show Choices", function()
        Log("Showing choices dialog")
        
        -- Create a table of choices
        local choices = {"Option 1", "Option 2", "Option 3", "Cancel"}
        
        -- Use pcall to catch any errors
        local success, err = pcall(function()
            -- Show dialog with choices and callback function
            -- The callback receives the selected choice index (1-based in Lua)
            ShowChoiceDialogue("Lua Choices Dialog", "Select an option from the list below.\nPress the corresponding number key (1-4) to select.", choices, function(index)
                Log("Choice callback received - selected index: " .. tostring(index))
                local choice = choices[index] or "Unknown"
                ShowNotification("You selected: " .. choice)
                SetControlText(statusLabelId, "Status: Selected option " .. index .. " (" .. choice .. ")")
            end)
        end)
        
        if success then
            SetControlText(statusLabelId, "Status: Showing choices dialog")
        else
            Log("Error showing choices dialog: " .. tostring(err))
            SetControlText(statusLabelId, "Status: Error showing choices: " .. tostring(err))
        end
    end)
    SetControlPosition(showChoicesBtnId, 50, 480)
    SetControlSize(showChoicesBtnId, 300, 40)
    
    -- Add a theme switcher button
    local themeBtnId = AddButton(mainWindowId, "theme_btn", "Switch Theme", function()
        local themes = {"blue", "dark", "green", "red"}
        local nextThemeIndex = 1
        
        -- Find current theme index
        for i, theme in ipairs(themes) do
            if theme == currentTheme then
                nextThemeIndex = i % #themes + 1
                break
            end
        end
        
        -- Apply next theme
        ApplyTheme(themes[nextThemeIndex])
        ShowNotification("Theme changed to: " .. themes[nextThemeIndex])
    end)
    SetControlPosition(themeBtnId, 50, 540)
    SetControlSize(themeBtnId, 300, 40)
    
    -- Add a close button
    local closeBtnId = AddButton(mainWindowId, "close_btn", "Close Window", function()
        ShowWindow(mainWindowId, false)
        isUIVisible = false
        ShowNotification("UI window hidden")
    end)
    SetControlPosition(closeBtnId, 50, 600)
    SetControlSize(closeBtnId, 300, 40)
    
    -- Make the window visible
    ShowWindow(mainWindowId, true)
    isUIVisible = true
    
    -- Always show a notification to confirm the UI is loaded
    ShowNotification("UI Example loaded.")
    Log("UI window created and shown")
    
    -- Debug info
    Log("IsGUIEnabled: " .. tostring(IsGUIEnabled()))
    Log("IsWindowVisible: " .. tostring(IsWindowVisible(mainWindowId)))
end

-- Sets up the UI command
local function SetupCommand()
    if commandRegistered then
        Log("UI Example command already registered")
        return
    end
    
    -- Register a game command to open our UI
    RegisterCommand("ui_example", "Shows/hides the UI example", "ui_example", function(args)
        Log("UI Example command triggered")
        ToggleUI()
    end)
    
    commandRegistered = true
    Log("UI Example command registered.")
end

-- Scene loaded event handler
function OnSceneLoaded(sceneName)
    Log("Scene loaded: " .. sceneName)
    
    if sceneName == "Main" then
        -- Main game scene loaded, we can create UI now
        Log("Main scene loaded")
        isMainSceneLoaded = true
        
        -- Create UI immediately on main scene load
        -- Wait a short time for everything to initialize
        Wait(1.0, function()
            -- Don't automatically show UI on scene load, let the player use the command
            if not commandRegistered then
                SetupCommand()
            end
            -- Create UI automatically
            CreateUI()
            ShowNotification("UI Example loaded.")
        end)
    elseif sceneName == "Menu" then
        -- Menu scene - hide UI if it exists
        if mainWindowId ~= nil then
            ShowWindow(mainWindowId, false)
        end
        isMainSceneLoaded = false
        isUIVisible = false
        Log("Menu scene loaded, UI hidden")
    end
end

-- Helper function to toggle UI
function ToggleUI()
    if not isMainSceneLoaded then
        ShowNotification("Cannot show UI: Not in main game scene")
        return
    end
    
    if mainWindowId == nil then
        CreateUI()
        return
    end
    
    isUIVisible = not isUIVisible
    ShowWindow(mainWindowId, isUIVisible)
    
    ShowNotification("UI Example " .. (isUIVisible and "shown" or "hidden"))
end

function OnConsoleReady()
    SetupCommand()
    -- Create UI automatically on console ready
    if isMainSceneLoaded and mainWindowId == nil then
        Wait(0.5, function()
            CreateUI()
        end)
    end
end 