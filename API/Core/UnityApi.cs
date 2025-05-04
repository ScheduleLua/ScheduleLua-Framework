using MoonSharp.Interpreter;
using ScheduleLua.API.Base;
using UnityEngine;

namespace ScheduleLua.API.Core
{
    /// <summary>
    /// Provides scene management functionality to Lua scripts
    /// </summary>
    public class UnityAPI : BaseLuaApiModule
    {
        /// <summary>
        /// Registers Scene API functions with the Lua interpreter
        /// </summary>
        public override void RegisterAPI(Script luaEngine)
        {
            if (luaEngine == null)
                throw new ArgumentNullException(nameof(luaEngine));

            // Register key press functions
            luaEngine.Globals["IsKeyDown"] = (Func<string, bool>)IsKeyDown;
            luaEngine.Globals["IsKeyPressed"] = (Func<string, bool>)IsKeyPressed;

            // Register screen functions
            luaEngine.Globals["GetScreenWidth"] = (Func<int>)GetScreenWidth;
            luaEngine.Globals["GetScreenHeight"] = (Func<int>)GetScreenHeight;
            luaEngine.Globals["GetScreenResolution"] = (Func<Tuple<int, int>>)GetScreenResolution;
        }

        // ---------------------------
        // Key Input Bindings (Unity-based)
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

                return Input.GetKey(keyCode);
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

                return Input.GetKeyDown(keyCode);
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error in IsKeyPressed", ex);
                return false;
            }
        }

        // ---------------------------
        // Screen Resolution Functions
        // ---------------------------

        /// <summary>
        /// Gets the screen width in pixels
        /// </summary>
        public static int GetScreenWidth()
        {
            try
            {
                return Screen.width;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error in GetScreenWidth", ex);
                return 0;
            }
        }

        /// <summary>
        /// Gets the screen height in pixels
        /// </summary>
        public static int GetScreenHeight()
        {
            try
            {
                return Screen.height;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error in GetScreenHeight", ex);
                return 0;
            }
        }

        /// <summary>
        /// Gets both screen width and height as a tuple
        /// </summary>
        public static Tuple<int, int> GetScreenResolution()
        {
            try
            {
                return new Tuple<int, int>(Screen.width, Screen.height);
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error in GetScreenResolution", ex);
                return new Tuple<int, int>(0, 0);
            }
        }
    }
}