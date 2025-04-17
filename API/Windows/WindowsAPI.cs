using MelonLoader;
using MoonSharp.Interpreter;
using ScheduleLua.API.Core;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ScheduleLua.API.Windows
{
    /// <summary>
    /// Provides scene management functionality to Lua scripts
    /// </summary>
    public static class WindowsAPI
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        // Windows Virtual Key Codes
        private static readonly Dictionary<KeyCode, int> KeyCodeToVK = new Dictionary<KeyCode, int>()
        {
            // Letters
            { KeyCode.A, 0x41 },
            { KeyCode.B, 0x42 },
            { KeyCode.C, 0x43 },
            { KeyCode.D, 0x44 },
            { KeyCode.E, 0x45 },
            { KeyCode.F, 0x46 },
            { KeyCode.G, 0x47 },
            { KeyCode.H, 0x48 },
            { KeyCode.I, 0x49 },
            { KeyCode.J, 0x4A },
            { KeyCode.K, 0x4B },
            { KeyCode.L, 0x4C },
            { KeyCode.M, 0x4D },
            { KeyCode.N, 0x4E },
            { KeyCode.O, 0x4F },
            { KeyCode.P, 0x50 },
            { KeyCode.Q, 0x51 },
            { KeyCode.R, 0x52 },
            { KeyCode.S, 0x53 },
            { KeyCode.T, 0x54 },
            { KeyCode.U, 0x55 },
            { KeyCode.V, 0x56 },
            { KeyCode.W, 0x57 },
            { KeyCode.X, 0x58 },
            { KeyCode.Y, 0x59 },
            { KeyCode.Z, 0x5A },
            
            // Numbers
            { KeyCode.Alpha0, 0x30 },
            { KeyCode.Alpha1, 0x31 },
            { KeyCode.Alpha2, 0x32 },
            { KeyCode.Alpha3, 0x33 },
            { KeyCode.Alpha4, 0x34 },
            { KeyCode.Alpha5, 0x35 },
            { KeyCode.Alpha6, 0x36 },
            { KeyCode.Alpha7, 0x37 },
            { KeyCode.Alpha8, 0x38 },
            { KeyCode.Alpha9, 0x39 },
            
            // Function keys
            { KeyCode.F1, 0x70 },
            { KeyCode.F2, 0x71 },
            { KeyCode.F3, 0x72 },
            { KeyCode.F4, 0x73 },
            { KeyCode.F5, 0x74 },
            { KeyCode.F6, 0x75 },
            { KeyCode.F7, 0x76 },
            { KeyCode.F8, 0x77 },
            { KeyCode.F9, 0x78 },
            { KeyCode.F10, 0x79 },
            { KeyCode.F11, 0x7A },
            { KeyCode.F12, 0x7B },
            
            // Special keys
            { KeyCode.Space, 0x20 },
            { KeyCode.Return, 0x0D },
            { KeyCode.Escape, 0x1B },
            { KeyCode.LeftShift, 0xA0 },
            { KeyCode.RightShift, 0xA1 },
            { KeyCode.LeftControl, 0xA2 },
            { KeyCode.RightControl, 0xA3 },
            { KeyCode.LeftAlt, 0xA4 },
            { KeyCode.RightAlt, 0xA5 },
            { KeyCode.Tab, 0x09 },
            { KeyCode.Backspace, 0x08 },
            
            // Arrow keys
            { KeyCode.UpArrow, 0x26 },
            { KeyCode.DownArrow, 0x28 },
            { KeyCode.LeftArrow, 0x25 },
            { KeyCode.RightArrow, 0x27 },
            
            // Numpad keys
            { KeyCode.Keypad0, 0x60 },
            { KeyCode.Keypad1, 0x61 },
            { KeyCode.Keypad2, 0x62 },
            { KeyCode.Keypad3, 0x63 },
            { KeyCode.Keypad4, 0x64 },
            { KeyCode.Keypad5, 0x65 },
            { KeyCode.Keypad6, 0x66 },
            { KeyCode.Keypad7, 0x67 },
            { KeyCode.Keypad8, 0x68 },
            { KeyCode.Keypad9, 0x69 },
            { KeyCode.KeypadMultiply, 0x6A },
            { KeyCode.KeypadPlus, 0x6B },
            { KeyCode.KeypadEnter, 0x0D },
            { KeyCode.KeypadMinus, 0x6D },
            { KeyCode.KeypadPeriod, 0x6E },
            { KeyCode.KeypadDivide, 0x6F },
            
            // Navigation keys
            { KeyCode.Home, 0x24 },
            { KeyCode.End, 0x23 },
            { KeyCode.PageUp, 0x21 },
            { KeyCode.PageDown, 0x22 },
            { KeyCode.Insert, 0x2D },
            { KeyCode.Delete, 0x2E },
            
            // Special characters
            { KeyCode.Comma, 0xBC },
            { KeyCode.Period, 0xBE },
            { KeyCode.Slash, 0xBF },
            { KeyCode.Semicolon, 0xBA },
            { KeyCode.Quote, 0xDE },
            { KeyCode.LeftBracket, 0xDB },
            { KeyCode.RightBracket, 0xDD },
            { KeyCode.Backslash, 0xDC },
            { KeyCode.Minus, 0xBD },
            { KeyCode.Equals, 0xBB },
            { KeyCode.BackQuote, 0xC0 },
            
            // System keys
            { KeyCode.Print, 0x2A },  // Print Screen
            { KeyCode.ScrollLock, 0x91 },
            { KeyCode.Pause, 0x13 },
            { KeyCode.Menu, 0x12 },  // Alt key
            { KeyCode.CapsLock, 0x14 },
            { KeyCode.Numlock, 0x90 },
            
            // Media/browser keys
            { KeyCode.LeftWindows, 0x5B },
            { KeyCode.RightWindows, 0x5C },
            
            // Mouse buttons
            { KeyCode.Mouse0, 0x01 },  // Left mouse button
            { KeyCode.Mouse1, 0x02 },  // Right mouse button
            { KeyCode.Mouse2, 0x04 },  // Middle mouse button
        };

        /// <summary>
        /// Registers Scene API functions with the Lua interpreter
        /// </summary>
        public static void RegisterAPI(Script luaEngine)
        {
            if (luaEngine == null)
                throw new ArgumentNullException(nameof(luaEngine));

            // Register key press functions
            luaEngine.Globals["IsKeyDown"] = (Func<string, bool>)IsKeyDown;
            luaEngine.Globals["IsKeyPressed"] = (Func<string, bool>)IsKeyPressed;
        }

        // ---------------------------
        // Key Input Bindings (Low-Level)
        // ---------------------------

        public static bool IsKeyDown(string keyName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyName))
                {
                    LuaUtility.LogWarning("IsKeyDown: empty keyName");
                    return false;
                }

                if (!Enum.TryParse<KeyCode>(keyName, true, out var keyCode))
                {
                    LuaUtility.LogWarning($"IsKeyDown: Unknown key '{keyName}'");
                    return false;
                }

                if (!KeyCodeToVK.TryGetValue(keyCode, out int vkCode))
                {
                    LuaUtility.LogWarning($"IsKeyDown: No Windows virtual key code mapping for '{keyName}'");
                    return false;
                }

                return (GetAsyncKeyState(vkCode) & 0x8000) != 0;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error in IsKeyDown", ex);
                return false;
            }
        }

        public static bool IsKeyPressed(string keyName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyName))
                {
                    LuaUtility.LogWarning("IsKeyPressed: empty keyName");
                    return false;
                }

                if (!Enum.TryParse<KeyCode>(keyName, true, out var keyCode))
                {
                    LuaUtility.LogWarning($"IsKeyPressed: Unknown key '{keyName}'");
                    return false;
                }

                if (!KeyCodeToVK.TryGetValue(keyCode, out int vkCode))
                {
                    LuaUtility.LogWarning($"IsKeyPressed: No Windows virtual key code mapping for '{keyName}'");
                    return false;
                }

                return (GetAsyncKeyState(vkCode) & 0x1) != 0;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error in IsKeyPressed", ex);
                return false;
            }
        }
    }
}