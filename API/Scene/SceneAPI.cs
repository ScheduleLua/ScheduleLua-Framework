using MelonLoader;
using MoonSharp.Interpreter;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScheduleLua.API.Scene
{
    /// <summary>
    /// Provides scene management functionality to Lua scripts
    /// </summary>
    public static class SceneAPI
    {
        private static MelonLogger.Instance _logger => ScheduleLua.Core.Instance.LoggerInstance;
        private static bool _eventsRegistered = false;

        /// <summary>
        /// Registers Scene API functions with the Lua interpreter
        /// </summary>
        public static void RegisterAPI(Script luaEngine)
        {
            if (luaEngine == null)
                throw new ArgumentNullException(nameof(luaEngine));

            // Register scene management functions
            luaEngine.Globals["GetCurrentSceneName"] = (Func<string>)GetCurrentSceneName;
            luaEngine.Globals["IsInMainScene"] = (Func<bool>)IsInMainScene;
            luaEngine.Globals["IsInMenuScene"] = (Func<bool>)IsInMenuScene;

            // Register scene event handlers if not already done
            RegisterEventHandlers();

            _logger.Msg("Scene API registered");
        }

        /// <summary>
        /// Register scene change event handlers
        /// </summary>
        private static void RegisterEventHandlers()
        {
            if (_eventsRegistered)
                return;

            // Hook into Unity scene changes
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            _eventsRegistered = true;
            // _logger.Msg("Scene event handlers registered");
        }

        /// <summary>
        /// Scene loaded handler
        /// </summary>
        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            // Forward event to Lua scripts
            ScheduleLua.Core.Instance.TriggerEvent("OnSceneLoaded", scene.name);
            // _logger.Msg($"Scene loaded event triggered for: {scene.name}");
        }

        /// <summary>
        /// Scene unloaded handler
        /// </summary>
        private static void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            // Forward event to Lua scripts
            ScheduleLua.Core.Instance.TriggerEvent("OnSceneUnloaded", scene.name);
            // _logger.Msg($"Scene unloaded event triggered for: {scene.name}");
        }

        /// <summary>
        /// Gets the name of the current active scene
        /// </summary>
        public static string GetCurrentSceneName()
        {
            try
            {
                UnityEngine.SceneManagement.Scene activeScene = SceneManager.GetActiveScene();
                return activeScene.name;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting current scene name: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Checks if the currently active scene is the main game scene
        /// </summary>
        public static bool IsInMainScene()
        {
            try
            {
                return SceneManager.GetActiveScene().name == "Main";
            }
            catch (Exception ex)
            {
                _logger.Error($"Error checking if in main scene: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if the currently active scene is the menu scene
        /// </summary>
        public static bool IsInMenuScene()
        {
            try
            {
                return SceneManager.GetActiveScene().name == "Menu";
            }
            catch (Exception ex)
            {
                _logger.Error($"Error checking if in menu scene: {ex.Message}");
                return false;
            }
        }
    }
}