using MelonLoader;
using MoonSharp.Interpreter;
using ScheduleLua.API.Core;
using System;
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

                if (!Enum.TryParse<KeyCode>(keyName, true, out var key))
                {
                    LuaUtility.LogWarning($"IsKeyDown: Unknown key '{keyName}'");
                    return false;
                }

                return (GetAsyncKeyState((int)key) & 0x8000) != 0;
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

                if (!Enum.TryParse<KeyCode>(keyName, true, out var key))
                {
                    LuaUtility.LogWarning($"IsKeyPressed: Unknown key '{keyName}'");
                    return false;
                }

                return (GetAsyncKeyState((int)key) & 0x1) != 0;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error in IsKeyPressed", ex);
                return false;
            }
        }
    }
}