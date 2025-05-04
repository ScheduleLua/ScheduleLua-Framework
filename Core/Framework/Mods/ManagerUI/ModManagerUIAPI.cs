using MoonSharp.Interpreter;
using ScheduleLua.API.Core;

namespace ScheduleLua.Core.Framework.Mods.ManagerUI
{
    /// <summary>
    /// Exposes mod manager UI functionality to Lua scripts
    /// </summary>
    public static class ModManagerUIAPI
    {
        private static ModManagerUIController _uiController;
        
        /// <summary>
        /// Register the Mod Manager UI API with the Lua engine
        /// </summary>
        public static void RegisterAPI(Script luaEngine, ModManagerUIController uiController)
        {
            _uiController = uiController;
            
            // Register functions for Lua
            luaEngine.Globals["ShowModManager"] = (System.Action)ShowModManagerUI;
            luaEngine.Globals["HideModManager"] = (System.Action)HideModManagerUI;
            luaEngine.Globals["ToggleModManager"] = (System.Action)ToggleModManagerUI;
            luaEngine.Globals["IsModManagerVisible"] = (System.Func<bool>)IsModManagerVisible;
        }
        
        /// <summary>
        /// Show the mod manager UI
        /// </summary>
        private static void ShowModManagerUI()
        {
            if (_uiController != null && !_uiController.IsVisible())
            {
                _uiController.ToggleVisibility();
            }
        }
        
        /// <summary>
        /// Hide the mod manager UI
        /// </summary>
        private static void HideModManagerUI()
        {
            if (_uiController != null && _uiController.IsVisible())
            {
                _uiController.ToggleVisibility();
            }
        }
        
        /// <summary>
        /// Toggle the mod manager UI visibility
        /// </summary>
        private static void ToggleModManagerUI()
        {
            if (_uiController != null)
            {
                _uiController.ToggleVisibility();
            }
        }
        
        /// <summary>
        /// Check if the mod manager UI is visible
        /// </summary>
        private static bool IsModManagerVisible()
        {
            return _uiController != null && _uiController.IsVisible();
        }
    }
} 