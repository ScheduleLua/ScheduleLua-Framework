--[[
    Bars_Tweaks Menu Module
    Provides a toggleable UI menu for the Bars_Tweaks mod
]]

-- Create a module table
local Menu = {}

-- Global variables to track UI state and elements
local isUIVisible = false
local mainWindowId = nil
local isMainSceneLoaded = false
local currentTheme = "dark" -- Default theme
local config = {}

-- Track UI controls for easier reference
local menuControls = {}

-- Tweak status tracking
local tweakStatus = {
    backpack = false,
    items = false,
    atm = false
}

-- Button/Control IDs
local backpackBtnId = nil
local itemTweaksBtnId = nil
local atmTweaksBtnId = nil
local statusLabelId = nil
local versionLabelId = nil

-- Array to track all controls for proper cleanup
local allControlIds = {}

-- Safe function to call UI operations
local function SafeUICall(func, ...)
    local success, result = pcall(func, ...)
    return success, result
end

local function SafeSaveConfig()
    if _G["SaveConfig"] == nil then
        _G["SaveConfig"] = function()
            return true
        end
    end
    return true
end

-- Helper function to track a control ID
local function TrackControl(controlId)
    if controlId then
        table.insert(allControlIds, controlId)
    end
    return controlId
end

-- Enhanced themes with more styling properties
local themes = {
    -- Dark Theme
    dark = {
        window        = { background = {0.12, 0.14, 0.18, 0.97}, text = {0.95, 0.95, 0.98, 1}, border = {0.18, 0.20, 0.25, 1.0} },
        header        = { background = {0.18, 0.20, 0.25, 1.0 }, text = {1,    1,    1,    1} },
        footer        = { background = {0.18, 0.20, 0.25, 1.0 }, text = {0.85, 0.87, 0.90, 1} },
        button        = { background = {0.22, 0.24, 0.30, 0.95}, text = {0.95, 0.95, 0.98, 1}, hover = {0.28, 0.30, 0.38, 0.95}, 
                          active = {0.30, 0.32, 0.40, 0.95}, border = {0.28, 0.30, 0.38, 1.0} },
        activeButton  = { background = {0.25, 0.65, 0.85, 0.95}, text = {1,    1,    1,    1}, hover = {0.30, 0.70, 0.90, 0.95},
                          active = {0.35, 0.75, 0.95, 0.95}, border = {0.35, 0.75, 0.95, 1.0} },
        label         = { text = {0.95, 0.95, 0.98, 1} },
        highlightLabel= { text = {0.30, 0.70, 0.95, 1} },
        statusLabel   = { text = {0.80, 0.82, 0.85, 1} },
        textfield     = { background = {0.16, 0.18, 0.23, 0.95}, text = {0.95, 0.95, 0.98, 1}, border = {0.22, 0.24, 0.30, 1.0} },
        box           = { background = {0.14, 0.16, 0.20, 0.95}, text = {0.95, 0.95, 0.98, 1}, border = {0.20, 0.22, 0.28, 1.0} },
        
        fontSizes     = { window = 22, button = 16, label = 14, textfield = 14 },
        fontStyles    = { window = "bold", button = "bold", label = "normal", textfield = "normal" },
        textAlignments= { button = "middlecenter", label = "middlecenter", textfield = "middlecenter" },
        borders       = { window = {3, 3, 3, 3}, button = {3, 3, 3, 3}, textfield = {2, 2, 2, 2}, box = {2, 2, 2, 2} },
        padding       = { window = {4, 4, 4, 4}, button = {6, 6, 6, 6}, label = {2, 2, 2, 2}, textfield = {5, 5, 5, 5}, box = {4, 4, 4, 4} }
    },

    -- Ocean Theme
    ocean = {
        window        = { background = {0.05, 0.10, 0.20, 0.98}, text = {0.95, 0.97, 1.00, 1}, border = {0.12, 0.20, 0.35, 1.0} },
        header        = { background = {0.08, 0.15, 0.28, 1.0 }, text = {1,    1,    1,    1} },
        footer        = { background = {0.08, 0.15, 0.28, 1.0 }, text = {0.85, 0.90, 0.95, 1} },
        button        = { background = {0.12, 0.22, 0.35, 0.95}, text = {1,    1,    1,    1}, hover = {0.15, 0.28, 0.42, 0.95},
                          active = {0.18, 0.32, 0.48, 0.95}, border = {0.18, 0.30, 0.45, 1.0} },
        activeButton  = { background = {0.15, 0.48, 0.70, 0.95}, text = {1,    1,    1,    1}, hover = {0.20, 0.55, 0.78, 0.95},
                          active = {0.25, 0.60, 0.85, 0.95}, border = {0.25, 0.60, 0.85, 1.0} },
        label         = { text = {0.95, 0.97, 1.00, 1} },
        highlightLabel= { text = {0.45, 0.85, 1.00, 1} },
        statusLabel   = { text = {0.80, 0.88, 0.95, 1} },
        textfield     = { background = {0.08, 0.16, 0.26, 0.95}, text = {0.95, 0.97, 1.00, 1}, border = {0.15, 0.25, 0.40, 1.0} },
        box           = { background = {0.06, 0.13, 0.22, 0.95}, text = {0.95, 0.97, 1.00, 1}, border = {0.15, 0.30, 0.50, 1.0} },
        
        fontSizes     = { window = 22, button = 16, label = 14, textfield = 14 },
        fontStyles    = { window = "bold", button = "bold", label = "normal", textfield = "normal" },
        textAlignments= { button = "middlecenter", label = "middlecenter", textfield = "middlecenter" },
        borders       = { window = {3, 3, 3, 3}, button = {3, 3, 3, 3}, textfield = {2, 2, 2, 2}, box = {2, 2, 2, 2} },
        padding       = { window = {4, 4, 6, 4}, button = {6, 6, 6, 6}, label = {2, 2, 2, 2}, textfield = {5, 5, 5, 5}, box = {4, 4, 4, 4} }
    },

    -- Refined Ocean Theme with deeper blues
    ocean = {
        window        = { background = {0.05, 0.10, 0.20, 0.98}, text = {0.95, 0.97, 1.00, 1} },
        header        = { background = {0.08, 0.15, 0.28, 1.0 }, text = {1,    1,    1,    1} },
        footer        = { background = {0.08, 0.15, 0.28, 1.0 }, text = {0.85, 0.90, 0.95, 1} },
        button        = { background = {0.12, 0.22, 0.35, 0.95}, text = {1,    1,    1,    1}, hover = {0.15, 0.28, 0.42, 0.95}, active = {0.15, 0.48, 0.70, 0.95}, border = {0.15, 0.28, 0.42, 1.0} },
        label         = { text = {0.95, 0.97, 1.00, 1} },
        highlightLabel= { text = {0.45, 0.85, 1.00, 1} },
        statusLabel   = { text = {0.80, 0.88, 0.95, 1} },
        textfield     = { background = {0.08, 0.16, 0.26, 0.95}, text = {0.95, 0.97, 1.00, 1}, border = {0.08, 0.16, 0.26, 1.0} },
        box           = { background = {0.08, 0.16, 0.26, 1.0}, text = {0.95, 0.97, 1.00, 1.0}, border = {0.08, 0.16, 0.26, 1.0} },
        fontSizes     = { window = 22, button = 16, label = 14, textfield = 14 },
        fontStyles    = { window = "bold", button = "bold", label = "normal", textfield = "normal" },
        textAlignments = { button = "middlecenter", label = "middlecenter", textfield = "middlecenter" },
        borders       = { window = {0.08, 0.16, 0.26, 1.0}, button = {0.15, 0.28, 0.42, 1.0}, textfield = {0.08, 0.16, 0.26, 1.0}, box = {0.08, 0.16, 0.26, 1.0} },
        padding       = { window = {4, 4, 4, 4}, button = {2, 2, 2, 2}, label = {2, 2, 2, 2}, textfield = {2, 2, 2, 2}, box = {2, 2, 2, 2} }
    },

    -- Enhanced Forest Theme with richer greens
    forest = {
        window        = { background = {0.08, 0.14, 0.08, 0.98}, text = {0.92, 0.97, 0.92, 1} },
        header        = { background = {0.12, 0.20, 0.12, 1.0 }, text = {1,    1,    1,    1} },
        footer        = { background = {0.12, 0.20, 0.12, 1.0 }, text = {0.75, 0.85, 0.75, 1} },
        button        = { background = {0.15, 0.28, 0.15, 0.95}, text = {0.92, 0.97, 0.92, 1}, hover = {0.20, 0.35, 0.20, 0.95}, active = {0.35, 0.70, 0.40, 0.95}, border = {0.15, 0.28, 0.15, 1.0} },
        label         = { text = {0.92, 0.97, 0.92, 1} },
        highlightLabel= { text = {0.60, 1.00, 0.65, 1} },
        statusLabel   = { text = {0.75, 0.85, 0.75, 1} },
        textfield     = { background = {0.10, 0.18, 0.10, 0.95}, text = {0.92, 0.97, 0.92, 1}, border = {0.10, 0.18, 0.10, 1.0} },
        box           = { background = {0.10, 0.18, 0.10, 1.0}, text = {0.92, 0.97, 0.92, 1.0}, border = {0.10, 0.18, 0.10, 1.0} },
        fontSizes     = { window = 22, button = 16, label = 14, textfield = 14 },
        fontStyles    = { window = "bold", button = "bold", label = "normal", textfield = "normal" },
        textAlignments = { button = "middlecenter", label = "middlecenter", textfield = "middlecenter" },
        borders       = { window = {0.10, 0.18, 0.10, 1.0}, button = {0.15, 0.28, 0.15, 1.0}, textfield = {0.10, 0.18, 0.10, 1.0}, box = {0.10, 0.18, 0.10, 1.0} },
        padding       = { window = {4, 4, 4, 4}, button = {2, 2, 2, 2}, label = {2, 2, 2, 2}, textfield = {2, 2, 2, 2}, box = {2, 2, 2, 2} }
    },

    -- Refined Sunset Theme with warmer tones
    sunset = {
        window        = { background = {0.15, 0.08, 0.10, 0.98}, text = {1.00, 0.96, 0.92, 1} },
        header        = { background = {0.25, 0.12, 0.15, 1.0 }, text = {1,    1,    1,    1} },
        footer        = { background = {0.25, 0.12, 0.15, 1.0 }, text = {0.92, 0.85, 0.80, 1} },
        button        = { background = {0.35, 0.18, 0.20, 0.95}, text = {1.00, 0.96, 0.92, 1}, hover = {0.42, 0.22, 0.25, 0.95}, active = {0.95, 0.50, 0.30, 0.95}, border = {0.30, 0.18, 0.20, 1.0} },
        label         = { text = {1.00, 0.96, 0.92, 1} },
        highlightLabel= { text = {1.00, 0.70, 0.40, 1} },
        statusLabel   = { text = {0.92, 0.85, 0.80, 1} },
        textfield     = { background = {0.22, 0.12, 0.15, 0.95}, text = {1.00, 0.96, 0.92, 1}, border = {0.22, 0.12, 0.15, 1.0} },
        box           = { background = {0.22, 0.12, 0.15, 1.0}, text = {1.00, 0.96, 0.92, 1.0}, border = {0.22, 0.12, 0.15, 1.0} },
        fontSizes     = { window = 22, button = 16, label = 14, textfield = 14 },
        fontStyles    = { window = "bold", button = "bold", label = "normal", textfield = "normal" },
        textAlignments = { button = "middlecenter", label = "middlecenter", textfield = "middlecenter" },
        borders       = { window = {0.22, 0.12, 0.15, 1.0}, button = {0.30, 0.18, 0.20, 1.0}, textfield = {0.22, 0.12, 0.15, 1.0}, box = {0.22, 0.12, 0.15, 1.0} },
        padding       = { window = {4, 4, 4, 4}, button = {2, 2, 2, 2}, label = {2, 2, 2, 2}, textfield = {2, 2, 2, 2}, box = {2, 2, 2, 2} }
    },
    
    -- New Modern Theme with blue accents
    modern = {
        window        = { background = {0.14, 0.14, 0.16, 0.97}, text = {0.98, 0.98, 0.98, 1} },
        header        = { background = {0.16, 0.16, 0.18, 1.0 }, text = {1,    1,    1,    1} },
        footer        = { background = {0.16, 0.16, 0.18, 1.0 }, text = {0.80, 0.80, 0.82, 1} },
        button        = { background = {0.20, 0.20, 0.22, 0.95}, text = {0.98, 0.98, 0.98, 1}, hover = {0.24, 0.24, 0.26, 0.95}, active = {0.20, 0.55, 0.90, 0.95}, border = {0.20, 0.20, 0.22, 1.0} },
        label         = { text = {0.98, 0.98, 0.98, 1} },
        highlightLabel= { text = {0.35, 0.65, 1.00, 1} },
        statusLabel   = { text = {0.80, 0.80, 0.82, 1} },
        textfield     = { background = {0.18, 0.18, 0.20, 0.95}, text = {0.98, 0.98, 0.98, 1}, border = {0.18, 0.18, 0.20, 1.0} },
        box           = { background = {0.16, 0.16, 0.18, 1.0}, text = {0.80, 0.80, 0.82, 1.0}, border = {0.18, 0.18, 0.20, 1.0} },
        fontSizes     = { window = 22, button = 16, label = 14, textfield = 14 },
        fontStyles    = { window = "bold", button = "bold", label = "normal", textfield = "normal" },
        textAlignments = { button = "middlecenter", label = "middlecenter", textfield = "middlecenter" },
        borders       = { window = {0.16, 0.16, 0.18, 1.0}, button = {0.20, 0.20, 0.22, 1.0}, textfield = {0.18, 0.18, 0.20, 1.0}, box = {0.16, 0.16, 0.18, 1.0} },
        padding       = { window = {4, 4, 4, 4}, button = {2, 2, 2, 2}, label = {2, 2, 2, 2}, textfield = {2, 2, 2, 2}, box = {2, 2, 2, 2} }
    },
    
    -- New Mint Theme with fresh greens
    mint = {
        window        = { background = {0.12, 0.16, 0.16, 0.97}, text = {0.94, 0.98, 0.96, 1} },
        header        = { background = {0.14, 0.20, 0.20, 1.0 }, text = {1,    1,    1,    1} },
        footer        = { background = {0.14, 0.20, 0.20, 1.0 }, text = {0.80, 0.90, 0.85, 1} },
        button        = { background = {0.18, 0.24, 0.24, 0.95}, text = {0.94, 0.98, 0.96, 1}, hover = {0.22, 0.30, 0.28, 0.95}, active = {0.20, 0.70, 0.55, 0.95}, border = {0.18, 0.24, 0.24, 1.0} },
        label         = { text = {0.94, 0.98, 0.96, 1} },
        highlightLabel= { text = {0.30, 0.85, 0.70, 1} },
        statusLabel   = { text = {0.80, 0.90, 0.85, 1} },
        textfield     = { background = {0.16, 0.22, 0.22, 0.95}, text = {0.94, 0.98, 0.96, 1}, border = {0.16, 0.22, 0.22, 1.0} },
        box           = { background = {0.14, 0.20, 0.20, 1.0}, text = {0.80, 0.90, 0.85, 1.0}, border = {0.14, 0.20, 0.20, 1.0} },
        fontSizes     = { window = 22, button = 16, label = 14, textfield = 14 },
        fontStyles    = { window = "bold", button = "bold", label = "normal", textfield = "normal" },
        textAlignments = { button = "middlecenter", label = "middlecenter", textfield = "middlecenter" },
        borders       = { window = {0.14, 0.20, 0.20, 1.0}, button = {0.18, 0.24, 0.24, 1.0}, textfield = {0.16, 0.22, 0.22, 1.0}, box = {0.14, 0.20, 0.20, 1.0} },
        padding       = { window = {4, 4, 4, 4}, button = {2, 2, 2, 2}, label = {2, 2, 2, 2}, textfield = {2, 2, 2, 2}, box = {2, 2, 2, 2} }
    },
    
    -- New Amber Theme with warm gold accents
    amber = {
        window        = { background = {0.15, 0.14, 0.12, 0.97}, text = {0.98, 0.96, 0.90, 1} },
        header        = { background = {0.20, 0.18, 0.15, 1.0 }, text = {1,    1,    1,    1} },
        footer        = { background = {0.20, 0.18, 0.15, 1.0 }, text = {0.90, 0.85, 0.78, 1} },
        button        = { background = {0.25, 0.22, 0.18, 0.95}, text = {0.98, 0.96, 0.90, 1}, hover = {0.30, 0.26, 0.22, 0.95}, active = {0.90, 0.70, 0.20, 0.95}, border = {0.25, 0.22, 0.18, 1.0} },
        label         = { text = {0.98, 0.96, 0.90, 1} },
        highlightLabel= { text = {0.95, 0.75, 0.25, 1} },
        statusLabel   = { text = {0.90, 0.85, 0.78, 1} },
        textfield     = { background = {0.22, 0.20, 0.16, 0.95}, text = {0.98, 0.96, 0.90, 1}, border = {0.22, 0.20, 0.16, 1.0} },
        box           = { background = {0.20, 0.18, 0.15, 1.0}, text = {0.90, 0.85, 0.78, 1.0}, border = {0.20, 0.18, 0.15, 1.0} },
        fontSizes     = { window = 22, button = 16, label = 14, textfield = 14 },
        fontStyles    = { window = "bold", button = "bold", label = "normal", textfield = "normal" },
        textAlignments = { button = "middlecenter", label = "middlecenter", textfield = "middlecenter" },
        borders       = { window = {0.20, 0.18, 0.15, 1.0}, button = {0.25, 0.22, 0.18, 1.0}, textfield = {0.22, 0.20, 0.16, 1.0}, box = {0.20, 0.18, 0.15, 1.0} },
        padding       = { window = {4, 4, 4, 4}, button = {2, 2, 2, 2}, label = {2, 2, 2, 2}, textfield = {2, 2, 2, 2}, box = {2, 2, 2, 2} }
    },
    
    -- New Violet Theme
    violet = {
        window        = { background = {0.12, 0.10, 0.16, 0.97}, text = {0.96, 0.94, 0.98, 1} },
        header        = { background = {0.18, 0.14, 0.24, 1.0 }, text = {1,    1,    1,    1} },
        footer        = { background = {0.18, 0.14, 0.24, 1.0 }, text = {0.85, 0.80, 0.90, 1} },
        button        = { background = {0.22, 0.18, 0.28, 0.95}, text = {0.96, 0.94, 0.98, 1}, hover = {0.28, 0.22, 0.35, 0.95}, active = {0.60, 0.40, 0.85, 0.95}, border = {0.22, 0.18, 0.28, 1.0} },
        label         = { text = {0.96, 0.94, 0.98, 1} },
        highlightLabel= { text = {0.75, 0.60, 1.00, 1} },
        statusLabel   = { text = {0.85, 0.80, 0.90, 1} },
        textfield     = { background = {0.18, 0.14, 0.22, 0.95}, text = {0.96, 0.94, 0.98, 1}, border = {0.18, 0.14, 0.22, 1.0} },
        box           = { background = {0.18, 0.14, 0.24, 1.0}, text = {0.85, 0.80, 0.90, 1.0}, border = {0.18, 0.14, 0.24, 1.0} },
        fontSizes     = { window = 22, button = 16, label = 14, textfield = 14 },
        fontStyles    = { window = "bold", button = "bold", label = "normal", textfield = "normal" },
        textAlignments = { button = "middlecenter", label = "middlecenter", textfield = "middlecenter" },
        borders       = { window = {0.18, 0.14, 0.24, 1.0}, button = {0.22, 0.18, 0.28, 1.0}, textfield = {0.18, 0.14, 0.22, 1.0}, box = {0.18, 0.14, 0.24, 1.0} },
        padding       = { window = {4, 4, 4, 4}, button = {2, 2, 2, 2}, label = {2, 2, 2, 2}, textfield = {2, 2, 2, 2}, box = {2, 2, 2, 2} }
    }
}

-- Apply a theme to the UI with enhanced styling
local function ApplyTheme(themeName)
    local theme = themes[themeName]
    if not theme then
        return
    end
    
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
    if theme.button.active then
        local act = theme.button.active
        SetButtonStyle("active", act[1], act[2], act[3], act[4])
    end
    
    -- Apply label style
    if theme.label.text then
        local txt = theme.label.text
        SetLabelStyle("text", txt[1], txt[2], txt[3], txt[4])
    end
    
    -- Apply highlight label style as a special case
    if theme.highlightLabel and theme.highlightLabel.text then
        -- Store current label style
        local origLabel = theme.label.text
        
        -- Apply highlight style temporarily 
        local hl = theme.highlightLabel.text
        SetLabelStyle("text", hl[1], hl[2], hl[3], hl[4])
        
        -- Apply to any specific highlight labels here if needed
        
        -- Restore normal label style
        SetLabelStyle("text", origLabel[1], origLabel[2], origLabel[3], origLabel[4])
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
    
    -- Apply box styles
    if theme.box then
        if theme.box.background then
            local bg = theme.box.background
            SetBoxStyle("background", bg[1], bg[2], bg[3], bg[4])
        end
        if theme.box.text then
            local txt = theme.box.text
            SetBoxStyle("text", txt[1], txt[2], txt[3], txt[4])
        end
    end
    
    -- Apply font sizes
    SetFontSize("window", theme.fontSizes and theme.fontSizes.window or 22)
    SetFontSize("button", theme.fontSizes and theme.fontSizes.button or 16)
    SetFontSize("label", theme.fontSizes and theme.fontSizes.label or 14)
    SetFontSize("textfield", theme.fontSizes and theme.fontSizes.textfield or 14)
    
    -- Apply font styles
    SetFontStyle("window", theme.fontStyles and theme.fontStyles.window or "bold")
    SetFontStyle("button", theme.fontStyles and theme.fontStyles.button or "bold")
    SetFontStyle("label", theme.fontStyles and theme.fontStyles.label or "normal")
    SetFontStyle("textfield", theme.fontStyles and theme.fontStyles.textfield or "normal")
    
    -- Apply text alignments
    SetTextAlignment("button", theme.textAlignments and theme.textAlignments.button or "middlecenter")
    SetTextAlignment("label", theme.textAlignments and theme.textAlignments.label or "middlecenter")
    SetTextAlignment("textfield", theme.textAlignments and theme.textAlignments.textfield or "middlecenter")
    
    -- Apply borders
    if theme.borders then
        if theme.borders.window then
            local b = theme.borders.window
            SetBorder("window", b[1], b[2], b[3], b[4])
        end
        if theme.borders.button then
            local b = theme.borders.button
            SetBorder("button", b[1], b[2], b[3], b[4])
        end
        if theme.borders.textfield then
            local b = theme.borders.textfield
            SetBorder("textfield", b[1], b[2], b[3], b[4])
        end
        if theme.borders.box then
            local b = theme.borders.box
            SetBorder("box", b[1], b[2], b[3], b[4])
        end
    end
    
    -- Apply padding
    if theme.padding then
        if theme.padding.window then
            local p = theme.padding.window
            SetPadding("window", p[1], p[2], p[3], p[4])
        end
        if theme.padding.button then
            local p = theme.padding.button
            SetPadding("button", p[1], p[2], p[3], p[4])
        end
        if theme.padding.label then
            local p = theme.padding.label
            SetPadding("label", p[1], p[2], p[3], p[4])
        end
        if theme.padding.textfield then
            local p = theme.padding.textfield
            SetPadding("textfield", p[1], p[2], p[3], p[4])
        end
        if theme.padding.box then
            local p = theme.padding.box
            SetPadding("box", p[1], p[2], p[3], p[4])
        end
    end
    
    currentTheme = themeName

    -- Update status label if it exists
    if statusLabelId then
        SetControlText(statusLabelId, "Theme: " .. themeName)
    end
    
    -- Log the theme change for debugging
    Log("Applied UI theme: " .. themeName)
end

-- Helper function to get the appropriate button style based on active status
local function UpdateButtonStyles()
    local theme = themes[currentTheme]
    
    -- Update backpack button style
    if backpackBtnId then
        SetControlText(backpackBtnId, tweakStatus.backpack and "✓ Backpack" or "Backpack")
    end
    
    -- Update item tweaks button style
    if itemTweaksBtnId then
        SetControlText(itemTweaksBtnId, tweakStatus.items and "✓ Item Tweaks" or "Item Tweaks")
    end
    
    -- Update ATM tweaks button style
    if atmTweaksBtnId then
        SetControlText(atmTweaksBtnId, tweakStatus.atm and "✓ ATM Tweaks" or "ATM Tweaks")
    end
end

-- Function to create a horizontal separator line
local function CreateSeparator(windowId, y, width)
    local separatorLabelId = TrackControl(AddLabel(windowId, "separator_" .. y, "───────────────────────────────────"))
    SetControlPosition(separatorLabelId, 10, y)
    SetControlSize(separatorLabelId, width - 20, 10)
    SetTextAlignment("label", "middlecenter")
    return separatorLabelId
end

-- Function to destroy all UI elements
local function DestroyUI()
    -- Destroy all tracked controls
    for _, controlId in ipairs(allControlIds) do
        SafeUICall(DestroyControl, controlId)
    end
    
    -- Reset tracking arrays
    allControlIds = {}
    menuControls = {}
    
    -- If window still exists, destroy it
    if mainWindowId then
        SafeUICall(DestroyWindow, mainWindowId)
        mainWindowId = nil
    end
    
    isUIVisible = false
end

-- Function to create the UI
local function CreateUI()
    -- Always destroy existing UI first to prevent duplicates
    DestroyUI()
    
    -- Make sure GUI is enabled
    EnableGUI(true)
    
    -- Set up window dimensions - WIDER FOR BETTER HORIZONTAL USAGE
    local screenWidth = GetScreenWidth() -- Approximate screen width
    local screenHeight = GetScreenHeight() -- Approximate screen height
    local winWidth = 660  -- Increased width for horizontal layout
    local winHeight = 800 -- Slightly reduced height 
    local x = (screenWidth - winWidth) / 2
    local y = (screenHeight - winHeight) / 2
    
    -- Create main window
    mainWindowId = CreateWindow("bars_tweaks_window", "Bars Tweaks", x, y, winWidth, winHeight)
    
    -- Initialize styles with default theme
    ApplyTheme(currentTheme)
    
    -- Additional style customizations
    SetFontSize("window", 22)  -- Larger window title
    SetFontStyle("window", "bold")
    SetFontSize("button", 16)  -- Increased button font 
    SetFontStyle("button", "bold")
    SetTextAlignment("button", "middlecenter")
    SetBorder("button", 3, 3, 3, 3)  -- Increased border
    
    -- HEADER SECTION
    local headerY = 10
    local headerHeight = 50  -- Increased height
    
    -- Add version label on the right side
    versionLabelId = TrackControl(AddLabel(mainWindowId, "version_label", "v" .. MOD_VERSION))
    SetControlPosition(versionLabelId, winWidth - 90, headerY + 5)
    SetControlSize(versionLabelId, 70, 35)
    SetFontSize("label", 16)
    
    -- Add separator after header
    CreateSeparator(mainWindowId, headerY + headerHeight, winWidth)
    
    -- MODULES SECTION - TWO COLUMN LAYOUT
    local contentY = headerY + headerHeight + 30  -- More space after header
    local buttonHeight = 45   -- Taller buttons
    local buttonSpacing = 25  -- Space between buttons
    
    -- Two-column layout dimensions
    local columnWidth = 280   -- Width of each column
    local columnGap = 40      -- Gap between columns
    local leftColX = 20       -- Left column X position
    local rightColX = leftColX + columnWidth + columnGap -- Right column X position
    
    -- Improved header for tweak modules
    local moduleHeaderId = TrackControl(AddLabel(mainWindowId, "module_header", "TWEAK MODULES"))
    SetControlPosition(moduleHeaderId, 30, contentY)
    SetControlSize(moduleHeaderId, winWidth - 60, 35)
    SetFontSize("label", 18)  -- Larger section headers
    SetFontStyle("label", "bold")
    
    -- Add module description
    local moduleDescId = TrackControl(AddLabel(mainWindowId, "module_desc", "Toggle individual tweak modules"))
    SetControlPosition(moduleDescId, 30, contentY + 35)
    SetControlSize(moduleDescId, winWidth - 60, 25)
    SetFontSize("label", 14)
    
    contentY = contentY + 70  -- More space before first button
    
    -- LEFT COLUMN MODULES
    
    -- Backpack module (left column)
    backpackBtnId = TrackControl(AddButton(mainWindowId, "backpack_btn", "Backpack", function()
        SafeUICall(function()
            tweakStatus.backpack = not tweakStatus.backpack
            UpdateButtonStyles()
            local Backpack = require("backpack")
            Backpack.ToggleTweaks()
        end)
    end))
    SetControlPosition(backpackBtnId, leftColX, contentY)
    SetControlSize(backpackBtnId, columnWidth, buttonHeight)
    
    -- Add tiny description label under the button
    local backpackDescId = TrackControl(AddLabel(mainWindowId, "backpack_desc", "Enhanced storage (b)"))
    SetControlPosition(backpackDescId, leftColX, contentY + buttonHeight + 5)
    SetControlSize(backpackDescId, columnWidth, 20)
    SetFontSize("label", 12)
    SetTextAlignment("label", "middlecenter")
    
    -- RIGHT COLUMN MODULES
    
    -- Item Tweaks module (right column)
    itemTweaksBtnId = TrackControl(AddButton(mainWindowId, "item_tweaks_btn", "Item Tweaks", function()
        SafeUICall(function()
            tweakStatus.items = not tweakStatus.items
            UpdateButtonStyles()
            local statusText = tweakStatus.items and "enabled" or "disabled"
            ShowNotificationWithIcon("Bars Tweaks", "Item tweaks " .. statusText, "icon.png")
            local ItemTweaks = require("item_tweaks")
            ItemTweaks.ToggleTweaks()
        end)
    end))
    SetControlPosition(itemTweaksBtnId, rightColX, contentY)
    SetControlSize(itemTweaksBtnId, columnWidth, buttonHeight)
    
    -- Add tiny description label under the button
    local itemDescId = TrackControl(AddLabel(mainWindowId, "item_desc", "Higher item stack limits"))
    SetControlPosition(itemDescId, rightColX, contentY + buttonHeight + 5)
    SetControlSize(itemDescId, columnWidth, 20)
    SetFontSize("label", 12)
    SetTextAlignment("label", "middlecenter")
    
    -- Move down for next row of modules
    contentY = contentY + buttonHeight + buttonSpacing + 15 -- Extra space for descriptions
    
    -- ATM module (left column, second row)
    atmTweaksBtnId = TrackControl(AddButton(mainWindowId, "atm_tweaks_btn", "ATM Tweaks", function()
        SafeUICall(function()
            tweakStatus.atm = not tweakStatus.atm
            UpdateButtonStyles()
            local statusText = tweakStatus.atm and "enabled" or "disabled"
            local AtmTweaks = require("atm_tweaks")
            AtmTweaks.ToggleTweaks()
        end)
    end))
    SetControlPosition(atmTweaksBtnId, leftColX, contentY)
    SetControlSize(atmTweaksBtnId, columnWidth, buttonHeight)
    
    -- Add tiny description label under the button
    local atmDescId = TrackControl(AddLabel(mainWindowId, "atm_desc", "Higher ATM limits"))
    SetControlPosition(atmDescId, leftColX, contentY + buttonHeight + 5)
    SetControlSize(atmDescId, columnWidth, 20)
    SetFontSize("label", 12)
    SetTextAlignment("label", "middlecenter")
    
    -- Reserve space for future modules in the right column
    -- If needed, add another module button here in the right column
    
    contentY = contentY + buttonHeight + buttonSpacing + 25 -- Extra space after module section
    
    -- ADVANCED SETTINGS SECTION - HORIZONTAL LAYOUT
    -- Add separator before advanced settings section
    CreateSeparator(mainWindowId, contentY, winWidth)
    contentY = contentY + 30
    
    -- Advanced settings section
    local advSettingsHeaderId = TrackControl(AddLabel(mainWindowId, "adv_settings_header", "ADVANCED SETTINGS"))
    SetControlPosition(advSettingsHeaderId, 30, contentY)
    SetControlSize(advSettingsHeaderId, winWidth - 60, 35)
    SetFontSize("label", 18)
    SetFontStyle("label", "bold")
    
    contentY = contentY + 50
    
    -- BACKPACK SETTINGS - LEFT COLUMN
    local backpackSettingsHeaderId = TrackControl(AddLabel(mainWindowId, "backpack_settings_header", "Backpack Settings:"))
    SetControlPosition(backpackSettingsHeaderId, leftColX, contentY)
    SetControlSize(backpackSettingsHeaderId, columnWidth, 25)
    SetFontSize("label", 16)
    SetFontStyle("label", "bold")
    
    contentY = contentY + 40
    
    -- Backpack Slots
    local backpackSlotsLabelId = TrackControl(AddLabel(mainWindowId, "backpack_slots_label", "Slots:"))
    SetControlPosition(backpackSlotsLabelId, leftColX, contentY)
    SetControlSize(backpackSlotsLabelId, 100, 30)
    SetFontSize("label", 14)
    
    -- Add decrease button (ANONYMOUS FUNCTION WITH ERROR HANDLING)
    local decBackpackSlotsBtnId = TrackControl(AddButton(mainWindowId, "dec_backpack_slots_btn", "-", function()
        SafeUICall(function()
            local Backpack = require("backpack")
            local currentSlots = Backpack.GetSlotCount()
            local newValue = math.max(4, currentSlots - 4)
            Backpack.SetSlotCount(newValue)
            if menuControls.backpackSlotsValueId then
                SetControlText(menuControls.backpackSlotsValueId, tostring(newValue))
            end
            config.backpackSlots = newValue
            SafeSaveConfig()
            Log("Set backpack slots to " .. newValue)
            ShowNotificationWithIcon("Bars Tweaks", "Backpack slots: " .. newValue, "icon.png")
        end)
    end))
    SetControlPosition(decBackpackSlotsBtnId, leftColX + 110, contentY)
    SetControlSize(decBackpackSlotsBtnId, 40, 30)
    
    -- Add value label
    local backpackSlotsValueId = TrackControl(AddLabel(mainWindowId, "backpack_slots_value", "12"))
    SetControlPosition(backpackSlotsValueId, leftColX + 155, contentY)
    SetControlSize(backpackSlotsValueId, 50, 30)
    SetFontSize("label", 14)
    SetTextAlignment("label", "middlecenter")
    menuControls.backpackSlotsValueId = backpackSlotsValueId
    
    -- Add increase button (ANONYMOUS FUNCTION WITH ERROR HANDLING)
    local incBackpackSlotsBtnId = TrackControl(AddButton(mainWindowId, "inc_backpack_slots_btn", "+", function()
        SafeUICall(function()
            local Backpack = require("backpack")
            local currentSlots = Backpack.GetSlotCount()
            local newValue = math.min(20, currentSlots + 4)
            Backpack.SetSlotCount(newValue)
            if menuControls.backpackSlotsValueId then
                SetControlText(menuControls.backpackSlotsValueId, tostring(newValue))
            end
            config.backpackSlots = newValue
            SafeSaveConfig()
            Log("Set backpack slots to " .. newValue)
            ShowNotificationWithIcon("Bars Tweaks", "Backpack slots: " .. newValue, "icon.png")
        end)
    end))
    SetControlPosition(incBackpackSlotsBtnId, leftColX + 210, contentY)
    SetControlSize(incBackpackSlotsBtnId, 40, 30)
    
    contentY = contentY + 45
    
    -- Backpack Rows
    local backpackRowsLabelId = TrackControl(AddLabel(mainWindowId, "backpack_rows_label", "Rows:"))
    SetControlPosition(backpackRowsLabelId, leftColX, contentY)
    SetControlSize(backpackRowsLabelId, 100, 30)
    SetFontSize("label", 14)
    
    -- Add decrease button (ANONYMOUS FUNCTION WITH ERROR HANDLING)
    local decBackpackRowsBtnId = TrackControl(AddButton(mainWindowId, "dec_backpack_rows_btn", "-", function()
        SafeUICall(function()
            local Backpack = require("backpack")
            local currentRows = Backpack.GetRowCount()
            local newValue = math.max(1, currentRows - 1)
            Backpack.SetRowCount(newValue)
            if menuControls.backpackRowsValueId then
                SetControlText(menuControls.backpackRowsValueId, tostring(newValue))
            end
            config.backpackRows = newValue
            SafeSaveConfig()
            Log("Set backpack rows to " .. newValue)
            ShowNotificationWithIcon("Bars Tweaks", "Backpack rows: " .. newValue, "icon.png")
        end)
    end))
    SetControlPosition(decBackpackRowsBtnId, leftColX + 110, contentY)
    SetControlSize(decBackpackRowsBtnId, 40, 30)
    
    -- Add value label
    local backpackRowsValueId = TrackControl(AddLabel(mainWindowId, "backpack_rows_value", "3"))
    SetControlPosition(backpackRowsValueId, leftColX + 155, contentY)
    SetControlSize(backpackRowsValueId, 50, 30)
    SetFontSize("label", 14)
    SetTextAlignment("label", "middlecenter")
    menuControls.backpackRowsValueId = backpackRowsValueId
    
    -- Add increase button (ANONYMOUS FUNCTION WITH ERROR HANDLING)
    local incBackpackRowsBtnId = TrackControl(AddButton(mainWindowId, "inc_backpack_rows_btn", "+", function()
        SafeUICall(function()
            local Backpack = require("backpack")
            local currentRows = Backpack.GetRowCount()
            local newValue = math.min(6, currentRows + 1)
            Backpack.SetRowCount(newValue)
            if menuControls.backpackRowsValueId then
                SetControlText(menuControls.backpackRowsValueId, tostring(newValue))
            end
            config.backpackRows = newValue
            SafeSaveConfig()
            Log("Set backpack rows to " .. newValue)
            ShowNotificationWithIcon("Bars Tweaks", "Backpack rows: " .. newValue, "icon.png")
        end)
    end))
    SetControlPosition(incBackpackRowsBtnId, leftColX + 210, contentY)
    SetControlSize(incBackpackRowsBtnId, 40, 30)
    
    -- ITEM SETTINGS - RIGHT COLUMN
    local itemSettingsHeaderId = TrackControl(AddLabel(mainWindowId, "item_settings_header", "Item Settings:"))
    SetControlPosition(itemSettingsHeaderId, rightColX, contentY - 85)
    SetControlSize(itemSettingsHeaderId, columnWidth, 25)
    SetFontSize("label", 16)
    SetFontStyle("label", "bold")
    
    -- Item Stack Multiplier
    local itemMultiLabelId = TrackControl(AddLabel(mainWindowId, "item_multi_label", "Stack Multiplier:"))
    SetControlPosition(itemMultiLabelId, rightColX, contentY - 45)
    SetControlSize(itemMultiLabelId, 120, 30)
    SetFontSize("label", 14)
    
    -- Add decrease button (ANONYMOUS FUNCTION WITH ERROR HANDLING)
    local decItemMultiBtnId = TrackControl(AddButton(mainWindowId, "dec_item_multi_btn", "-", function()
        SafeUICall(function()
            local ItemTweaks = require("item_tweaks")
            local newValue = math.max(1.1, ItemTweaks.GetStackMultiplier() - 0.5)
            ItemTweaks.SetStackMultiplier(newValue)
            if menuControls.itemMultiValueId then
                SetControlText(menuControls.itemMultiValueId, string.format("%.1f", newValue))
            end
            config.stackMultiplier = newValue
            SafeSaveConfig()
            
            -- Safely call ApplyStackMultiplier with error handling
            if ItemTweaks and ItemTweaks.ApplyStackMultiplier then
                local applySuccess, applyError = pcall(ItemTweaks.ApplyStackMultiplier)
                if not applySuccess then
                    LogError("ERROR: Failed to apply stack multiplier - " .. tostring(applyError))
                end
            else
                LogWarning("WARNING: ItemTweaks.ApplyStackMultiplier not available")
            end
            
            Log("Set stack multiplier to " .. newValue)
            ShowNotificationWithIcon("Bars Tweaks", "Stack multiplier: " .. string.format("%.1f", newValue), "icon.png")
        end)
    end))
    SetControlPosition(decItemMultiBtnId, rightColX + 130, contentY - 45)
    SetControlSize(decItemMultiBtnId, 40, 30)
    
    -- Add value label
    local itemMultiValueId = TrackControl(AddLabel(mainWindowId, "item_multi_value", "2.0"))
    SetControlPosition(itemMultiValueId, rightColX + 175, contentY - 45)
    SetControlSize(itemMultiValueId, 50, 30)
    SetFontSize("label", 14)
    SetTextAlignment("label", "middlecenter")
    menuControls.itemMultiValueId = itemMultiValueId
    
    -- Add increase button (ANONYMOUS FUNCTION WITH ERROR HANDLING)
    local incItemMultiBtnId = TrackControl(AddButton(mainWindowId, "inc_item_multi_btn", "+", function()
        SafeUICall(function()
            local ItemTweaks = require("item_tweaks")
            local newValue = math.min(10, ItemTweaks.GetStackMultiplier() + 0.5)
            ItemTweaks.SetStackMultiplier(newValue)
            if menuControls.itemMultiValueId then
                SetControlText(menuControls.itemMultiValueId, string.format("%.1f", newValue))
            end
            config.stackMultiplier = newValue
            SafeSaveConfig()
            
            -- Safely call ApplyStackMultiplier with error handling
            if ItemTweaks and ItemTweaks.ApplyStackMultiplier then
                local applySuccess, applyError = pcall(ItemTweaks.ApplyStackMultiplier)
                if not applySuccess then
                    LogError("ERROR: Failed to apply stack multiplier - " .. tostring(applyError))
                end
            else
                LogWarning("Warning: ItemTweaks.ApplyStackMultiplier not available")
            end
            
            Log("Set stack multiplier to " .. newValue)
            ShowNotificationWithIcon("Bars Tweaks", "Stack multiplier: " .. string.format("%.1f", newValue), "icon.png")
        end)
    end))
    SetControlPosition(incItemMultiBtnId, rightColX + 230, contentY - 45)
    SetControlSize(incItemMultiBtnId, 40, 30)
    
    -- ATM SETTINGS - RIGHT COLUMN, BELOW ITEM SETTINGS
    local atmSettingsHeaderId = TrackControl(AddLabel(mainWindowId, "atm_settings_header", "ATM Settings:"))
    SetControlPosition(atmSettingsHeaderId, rightColX, contentY)
    SetControlSize(atmSettingsHeaderId, columnWidth, 25)
    SetFontSize("label", 16)
    SetFontStyle("label", "bold")
    
    contentY = contentY + 45
    
    -- ATM Presets - Horizontal row
    -- Button dimensions
    local presetButtonWidth = 90
    local presetButtonHeight = 30
    local presetButtonGap = 10
    
    -- Row 1 of presets
    local atmPresetY = contentY
    
    -- Default preset button
    local lowPresetBtnId = TrackControl(AddButton(mainWindowId, "low_preset_btn", "Default", function()
        SafeUICall(function()
            local AtmTweaks = require("atm_tweaks")
            AtmTweaks.SetPreset("Default")
            Log("Setting ATM preset to Default")
            ShowNotificationWithIcon("Bars Tweaks", "ATM limit: Default", "icon.png")
            UpdateATMPresetButtonStyles()
        end)
    end))
    SetControlPosition(lowPresetBtnId, rightColX, atmPresetY)
    SetControlSize(lowPresetBtnId, presetButtonWidth, presetButtonHeight)
    menuControls.lowPresetBtnId = lowPresetBtnId
    
    -- Medium preset button
    local medPresetBtnId = TrackControl(AddButton(mainWindowId, "med_preset_btn", "Medium", function()
        SafeUICall(function()
            local AtmTweaks = require("atm_tweaks")
            AtmTweaks.SetPreset("Medium")
            Log("Setting ATM preset to Medium")
            ShowNotificationWithIcon("Bars Tweaks", "ATM limit: Medium", "icon.png")
            UpdateATMPresetButtonStyles()
        end)
    end))
    SetControlPosition(medPresetBtnId, rightColX + presetButtonWidth + presetButtonGap, atmPresetY)
    SetControlSize(medPresetBtnId, presetButtonWidth, presetButtonHeight)
    menuControls.medPresetBtnId = medPresetBtnId
    
    -- High preset button
    local highPresetBtnId = TrackControl(AddButton(mainWindowId, "high_preset_btn", "High", function()
        SafeUICall(function()
            local AtmTweaks = require("atm_tweaks")
            AtmTweaks.SetPreset("High")
            Log("Setting ATM preset to High")
            ShowNotificationWithIcon("Bars Tweaks", "ATM limit: High", "icon.png")
            UpdateATMPresetButtonStyles()
        end)
    end))
    SetControlPosition(highPresetBtnId, rightColX + (presetButtonWidth + presetButtonGap) * 2, atmPresetY)
    SetControlSize(highPresetBtnId, presetButtonWidth, presetButtonHeight)
    menuControls.highPresetBtnId = highPresetBtnId
    
    -- Row 2 of presets - Very High and Unlimited
    atmPresetY = atmPresetY + presetButtonHeight + 10
    
    -- Very high preset button
    local veryHighPresetBtnId = TrackControl(AddButton(mainWindowId, "veryhigh_preset_btn", "Very High", function()
        SafeUICall(function()
            local AtmTweaks = require("atm_tweaks")
            AtmTweaks.SetPreset("Very High")
            Log("Setting ATM preset to Very High")
            ShowNotificationWithIcon("Bars Tweaks", "ATM limit: Very High", "icon.png")
            UpdateATMPresetButtonStyles()
        end)
    end))
    SetControlPosition(veryHighPresetBtnId, rightColX, atmPresetY)
    SetControlSize(veryHighPresetBtnId, presetButtonWidth * 1.5, presetButtonHeight)
    menuControls.veryHighPresetBtnId = veryHighPresetBtnId
    
    -- Unlimited preset button
    local unlimitedPresetBtnId = TrackControl(AddButton(mainWindowId, "unlimited_preset_btn", "Unlimited", function()
        SafeUICall(function()
            local AtmTweaks = require("atm_tweaks")
            AtmTweaks.SetPreset("Unlimited")
            Log("Setting ATM preset to Unlimited")
            ShowNotificationWithIcon("Bars Tweaks", "ATM limit: Unlimited", "icon.png")
            UpdateATMPresetButtonStyles()
        end)
    end))
    SetControlPosition(unlimitedPresetBtnId, rightColX + presetButtonWidth * 1.5 + presetButtonGap, atmPresetY)
    SetControlSize(unlimitedPresetBtnId, presetButtonWidth * 1.5, presetButtonHeight)
    menuControls.unlimitedPresetBtnId = unlimitedPresetBtnId
    
    contentY = contentY + 90  -- Move down past ATM presets
    
    -- SETTINGS SECTION
    -- Add separator before settings section
    CreateSeparator(mainWindowId, contentY, winWidth)
    contentY = contentY + 30
    
    -- Two-column layout for general settings
    -- Theme switcher (left column)
    local themeBtnId = TrackControl(AddButton(mainWindowId, "theme_btn", "Switch Theme", function()
        SafeUICall(function()
            local themesList = {"dark", "ocean", "forest", "sunset", "modern", "mint", "amber", "violet"}
            local nextThemeIndex = 1
            
            -- Find current theme index
            for i, themeName in ipairs(themesList) do
                if themeName == currentTheme then
                    nextThemeIndex = i % #themesList + 1
                    break
                end
            end
            
            -- Apply next theme
            ApplyTheme(themesList[nextThemeIndex])
            UpdateButtonStyles()
            
            -- Save theme config
            if config then
                config.theme = themesList[nextThemeIndex]
                -- Only call SaveConfig if it exists
                if SafeSaveConfig then
                    SafeSaveConfig()
                end
            end
            
            Log("Switched to theme: " .. themesList[nextThemeIndex])
            ShowNotificationWithIcon("Bars Tweaks", "Theme: " .. themesList[nextThemeIndex], "icon.png")
        end)
    end))
    SetControlPosition(themeBtnId, leftColX, contentY)
    SetControlSize(themeBtnId, columnWidth, buttonHeight)
    
    -- Close button (right column)
    local closeBtnId = TrackControl(AddButton(mainWindowId, "close_btn", "Close Menu", function()
        ShowWindow(mainWindowId, false)
        isUIVisible = false
        ShowNotificationWithIcon("Bars Tweaks", "Menu hidden", "icon.png")
    end))
    SetControlPosition(closeBtnId, rightColX, contentY)
    SetControlSize(closeBtnId, columnWidth, buttonHeight)
    
    -- FOOTER SECTION
    local footerY = winHeight - 60
    
    -- Add separator before footer
    CreateSeparator(mainWindowId, footerY - 15, winWidth)
    
    -- Add status label in the footer
    statusLabelId = TrackControl(AddLabel(mainWindowId, "status_label", "Theme: " .. currentTheme))
    SetControlPosition(statusLabelId, 30, footerY + 5)
    SetControlSize(statusLabelId, winWidth - 60, 30)
    SetFontSize("label", 14)
    SetTextAlignment("label", "middlecenter")
    
    -- Make the window visible
    ShowWindow(mainWindowId, true)
    isUIVisible = true

    -- Apply padding to UI elements
    SetPadding("window", 4, 4, 4, 4)
    SetPadding("button", 2, 2, 2, 2)
    SetPadding("box", 2, 2, 2, 2)
    SetPadding("label", 2, 2, 2, 2)
    SetPadding("textfield", 2, 2, 2, 2)
    
    -- Update button styles based on current status
    UpdateButtonStyles()
    
    -- Initialize values from modules
    InitializeAdvancedSettingsValues()
    
    -- Show a notification to confirm the UI is loaded
    ShowNotificationWithIcon("Bars Tweaks", "Menu loaded", "icon.png")
end

-- Function to initialize advanced settings values from modules
function InitializeAdvancedSettingsValues()
    -- Get current values from modules
    local ItemTweaks = require("item_tweaks")
    local Backpack = require("backpack")
    local AtmTweaks = require("atm_tweaks")
    
    -- Update UI elements with current values
    if ItemTweaks.GetStackMultiplier then
        local multiplier = ItemTweaks.GetStackMultiplier()
        if menuControls.itemMultiValueId then
            SetControlText(menuControls.itemMultiValueId, string.format("%.1f", multiplier))
        end
    end
    
    if Backpack.GetSlotCount then
        local slots = Backpack.GetSlotCount()
        if menuControls.backpackSlotsValueId then
            SetControlText(menuControls.backpackSlotsValueId, tostring(slots))
        end
    end
    
    if Backpack.GetRowCount then
        local rows = Backpack.GetRowCount()
        if menuControls.backpackRowsValueId then
            SetControlText(menuControls.backpackRowsValueId, tostring(rows))
        end
    end
    
    -- Update ATM preset button styles
    UpdateATMPresetButtonStyles()
end

-- Helper function to find a control by name
function FindControl(name)
    -- Simply return the stored reference if it exists
    return menuControls[name]
end

-- Function to update ATM preset button styles
function UpdateATMPresetButtonStyles()
    -- Safe require with error handling
    local success, AtmTweaks = pcall(require, "atm_tweaks")
    if not success then
        return
    end
    
    -- Check for the GetCurrentPreset function
    if not AtmTweaks.GetCurrentPreset then 
        return 
    end
    
    -- Get current preset with error handling
    local currentPreset
    success, currentPreset = pcall(AtmTweaks.GetCurrentPreset)
    if not success then
        return
    end
    
    -- Button IDs for each preset
    local presetButtons = {
        ["Default"] = "lowPresetBtnId",
        ["Medium"] = "medPresetBtnId",
        ["High"] = "highPresetBtnId",
        ["Very High"] = "veryHighPresetBtnId", 
        ["Unlimited"] = "unlimitedPresetBtnId"
    }
    
    -- Update each button's style
    for preset, btnVarName in pairs(presetButtons) do
        -- Get the button ID from menuControls with nil check
        local btnId = menuControls and menuControls[btnVarName]
        if btnId then
            -- Set appropriate text
            local buttonText = preset
            if preset == currentPreset then
                buttonText = "✓ " .. preset
            end
            
            -- Set control text with error handling
            local setSuccess, setError = pcall(SetControlText, btnId, buttonText)
            if not setSuccess then
                LogError("ERROR: Failed to set control text for ATM preset button - " .. tostring(setError))
            end
        end
    end
end

-- Initialize the menu module
function Menu.Initialize()
    -- Apply any initial settings here
end

function Menu.Update()
    -- Only check for key press if we're in the main scene
    if not isMainSceneLoaded then
        -- Not in main scene, no need to check keys
        return
    end
    
    -- Check for = key press to toggle menu
    if not IsKeyPressed then
        return
    end
    
    local keyPressed = IsKeyPressed("Equals")
    if keyPressed then
        -- Toggle the menu immediately (no cooldown check - handled by WindowsAPI)
        Menu.ToggleUI()
    end
end

-- Scene loaded event handler
function Menu.OnSceneLoaded(sceneName)
    if sceneName == "Main" then
        -- Main game scene loaded, we can create UI now
        isMainSceneLoaded = true
        
        -- Wait a short time for scene to fully load before creating UI
        -- This helps prevent UI issues during scene transitions
        if not Wait then
            return
        else
            Wait(0.5, function()
                if isUIVisible then
                    CreateUI()
                end
            end)
        end
    elseif sceneName == "Menu" then
        -- Menu scene - hide UI if it exists
        if mainWindowId ~= nil then
            ShowWindow(mainWindowId, false)
        end
        isMainSceneLoaded = false
        isUIVisible = false
    end
end

-- Toggle the UI visibility
function Menu.ToggleUI()
    isUIVisible = not isUIVisible
    
    if isUIVisible then
        -- Create fresh UI if it doesn't exist
        if mainWindowId == nil then
            CreateUI()
        else
            ShowWindow(mainWindowId, true)
        end
    else
        -- Just hide the window if it exists
        if mainWindowId ~= nil then
            ShowWindow(mainWindowId, false)
        end
    end
    
    ShowNotificationWithIcon("Bars Tweaks", "Menu " .. (isUIVisible and "shown" or "hidden"), "icon.png")
end

-- Helper function to validate required API functions
function Menu.ValidateAPI()
    local requiredFunctions = {
        "IsKeyPressed", "GetGameTime", "ShowWindow", "CreateWindow",
        "DestroyWindow", "AddButton", "AddLabel", "Wait",
        "ShowNotificationWithIcon"
    }
    
    local allValid = true
    
    for _, funcName in ipairs(requiredFunctions) do
        if _G[funcName] == nil then
            allValid = false
        end
    end
    
    return allValid
end

-- Update tweak statuses from external modules
function Menu.SetTweakStatus(tweakName, status)
    if tweakStatus[tweakName] ~= nil then
        tweakStatus[tweakName] = status
        if mainWindowId ~= nil and isUIVisible then
            UpdateButtonStyles()
        end
    end
end

-- Get tweak status
function Menu.GetTweakStatus(tweakName)
    return tweakStatus[tweakName] or false
end

-- Shutdown the menu module
function Menu.Shutdown()
    DestroyUI()
end

return Menu 