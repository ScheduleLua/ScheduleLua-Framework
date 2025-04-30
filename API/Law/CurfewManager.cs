using MelonLoader;
using MoonSharp.Interpreter;
using ScheduleLua.API.Core;
using ScheduleOne.Law;
using UnityEngine.SceneManagement;

namespace ScheduleLua.API.Law
{
    /// <summary>
    /// Provides an interface to the ScheduleOne Curfew system for Lua scripts
    /// </summary>
    public static class CurfewManagerAPI
    {
        private static bool _eventsHooked = false;

        /// <summary>
        /// Registers Curfew API with the Lua interpreter
        /// </summary>
        public static void RegisterAPI(Script luaEngine)
        {
            if (luaEngine == null)
                throw new ArgumentNullException(nameof(luaEngine));

            // Curfew Status Functions
            luaEngine.Globals["IsCurfewEnabled"] = (Func<bool>)IsCurfewEnabled;
            luaEngine.Globals["IsCurfewActive"] = (Func<bool>)IsCurfewActive;
            luaEngine.Globals["IsCurfewActiveWithTolerance"] = (Func<bool>)IsCurfewActiveWithTolerance;
            luaEngine.Globals["GetCurfewStartTime"] = (Func<int>)GetCurfewStartTime;
            luaEngine.Globals["GetCurfewEndTime"] = (Func<int>)GetCurfewEndTime;
            luaEngine.Globals["GetCurfewWarningTime"] = (Func<int>)GetCurfewWarningTime;
            luaEngine.Globals["GetTimeUntilCurfew"] = (Func<int>)GetTimeUntilCurfew;

            // Curfew Control Functions
            luaEngine.Globals["EnableCurfew"] = (Action)EnableCurfew;
            luaEngine.Globals["DisableCurfew"] = (Action)DisableCurfew;

            RegisterAllCurfewEvents();

            // Hook into scene changes to detect when we enter the main game
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            // Reset event hooks when entering Menu scene
            if (scene.name == "Menu")
            {
                _eventsHooked = false;
            }
            else if (scene.name == "Main")
            {
                TryHookCurfewEvents();
            }
        }

        private static void TryHookCurfewEvents()
        {
            // Don't attempt to hook events if we're in the Menu scene
            if (SceneManager.GetActiveScene().name == "Menu")
            {
                LuaUtility.Log("Currently in Menu scene, skipping curfew event hooks");
                return;
            }

            // Don't hook events again if already hooked
            if (_eventsHooked)
            {
                return;
            }

            // If CurfewManager is available now, hook events immediately
            if (CurfewManager.Instance != null)
            {
                HookCurfewEvents();
            }
            else
            {
                // If not, wait a bit and try again
                MelonCoroutines.Start(WaitForCurfewManager());
            }
        }

        private static System.Collections.IEnumerator WaitForCurfewManager()
        {
            // Wait a few frames for CurfewManager to be initialized
            for (int i = 0; i < 10; i++)
            {
                yield return null;

                // Try to hook events if the CurfewManager is now available
                if (CurfewManager.Instance != null)
                {
                    HookCurfewEvents();
                    yield break;
                }
            }

            // Still not available after waiting
            LuaUtility.LogWarning("CurfewManager not available after waiting. Events will be hooked when the CurfewManager becomes available.");
        }

        private static void HookCurfewEvents()
        {
            // Skip if already hooked or if in Menu scene
            if (_eventsHooked || SceneManager.GetActiveScene().name == "Menu")
                return;

            // Hook into the CurfewManager events once
            var curfewManager = CurfewManager.Instance;
            if (curfewManager == null)
                return;

            curfewManager.onCurfewEnabled.AddListener((UnityEngine.Events.UnityAction)(() => {
                ScheduleLua.ModCore.Instance.TriggerEvent("OnCurfewEnabled");
            }));

            curfewManager.onCurfewDisabled.AddListener((UnityEngine.Events.UnityAction)(() => {
                ScheduleLua.ModCore.Instance.TriggerEvent("OnCurfewDisabled");
            }));

            curfewManager.onCurfewWarning.AddListener((UnityEngine.Events.UnityAction)(() => {
                ScheduleLua.ModCore.Instance.TriggerEvent("OnCurfewWarning");
            }));

            curfewManager.onCurfewHint.AddListener((UnityEngine.Events.UnityAction)(() => {
                ScheduleLua.ModCore.Instance.TriggerEvent("OnCurfewHint");
            }));

            _eventsHooked = true;
        }

        #region Curfew Status Functions

        /// <summary>
        /// Checks if the curfew system is enabled in the game
        /// </summary>
        public static bool IsCurfewEnabled()
        {
            try
            {
                if (CurfewManager.Instance == null)
                    return false;

                return CurfewManager.Instance.IsEnabled;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error checking if curfew is enabled: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if curfew is currently active (between 9PM and 5AM)
        /// </summary>
        public static bool IsCurfewActive()
        {
            try
            {
                if (CurfewManager.Instance == null)
                    return false;

                return CurfewManager.Instance.IsCurrentlyActive;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error checking if curfew is active: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if curfew is currently active with 15-minute tolerance (between 9:15PM and 5AM)
        /// </summary>
        public static bool IsCurfewActiveWithTolerance()
        {
            try
            {
                if (CurfewManager.Instance == null)
                    return false;

                return CurfewManager.Instance.IsCurrentlyActiveWithTolerance;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error checking if curfew is active with tolerance: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the curfew start time (9PM/2100)
        /// </summary>
        public static int GetCurfewStartTime()
        {
            return CurfewManager.CURFEW_START_TIME;
        }

        /// <summary>
        /// Gets the curfew end time (5AM/500)
        /// </summary>
        public static int GetCurfewEndTime()
        {
            return CurfewManager.CURFEW_END_TIME;
        }

        /// <summary>
        /// Gets the curfew warning time (8:30PM/2030)
        /// </summary>
        public static int GetCurfewWarningTime()
        {
            return CurfewManager.WARNING_TIME;
        }

        /// <summary>
        /// Gets the minutes until curfew starts (or 0 if curfew is active or not enabled)
        /// </summary>
        public static int GetTimeUntilCurfew()
        {
            try
            {
                if (!IsCurfewEnabled() || IsCurfewActive())
                    return 0;

                var timeManager = ScheduleOne.GameTime.TimeManager.Instance;
                if (timeManager == null)
                    return 0;

                int currentTime = timeManager.CurrentTime;

                // If we're already past the warning time but before start time
                if (timeManager.IsCurrentTimeWithinRange(CurfewManager.WARNING_TIME, CurfewManager.CURFEW_START_TIME))
                {
                    return ScheduleOne.GameTime.TimeManager.GetMinSumFrom24HourTime(CurfewManager.CURFEW_START_TIME) -
                           ScheduleOne.GameTime.TimeManager.GetMinSumFrom24HourTime(currentTime);
                }

                return 0;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error calculating time until curfew: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Registers all curfew events
        /// </summary>
        public static void RegisterAllCurfewEvents()
        {
            try
            {
                // Register all curfew event types
                HookCurfewEnabledEvent("OnCurfewEnabled");
                HookCurfewDisabledEvent("OnCurfewDisabled");
                HookCurfewWarningEvent("OnCurfewWarning");
                HookCurfewHintEvent("OnCurfewHint");
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error registering curfew events: {ex.Message}");
            }
        }

        private static void HookCurfewEnabledEvent(string functionName)
        {
            if (CurfewManager.Instance == null)
                return;

            CurfewManager.Instance.onCurfewEnabled.AddListener((UnityEngine.Events.UnityAction)(() => {
                ScheduleLua.ModCore.Instance.TriggerEvent(functionName);
            }));
        }

        private static void HookCurfewDisabledEvent(string functionName)
        {
            if (CurfewManager.Instance == null)
                return;

            CurfewManager.Instance.onCurfewDisabled.AddListener((UnityEngine.Events.UnityAction)(() => {
                ScheduleLua.ModCore.Instance.TriggerEvent(functionName);
            }));
        }

        private static void HookCurfewWarningEvent(string functionName)
        {
            if (CurfewManager.Instance == null)
                return;

            CurfewManager.Instance.onCurfewWarning.AddListener((UnityEngine.Events.UnityAction)(() => {
                ScheduleLua.ModCore.Instance.TriggerEvent(functionName);
            }));
        }

        private static void HookCurfewHintEvent(string functionName)
        {
            if (CurfewManager.Instance == null)
                return;

            CurfewManager.Instance.onCurfewHint.AddListener((UnityEngine.Events.UnityAction)(() => {
                ScheduleLua.ModCore.Instance.TriggerEvent(functionName);
            }));
        }

        #endregion

        #region Curfew Control Functions

        /// <summary>
        /// Enables the curfew system
        /// </summary>
        public static void EnableCurfew() => CurfewManager.Instance.Enable(null);

        /// <summary>
        /// Disables the curfew system
        /// </summary>
        public static void DisableCurfew() => CurfewManager.Instance.Disable();

        #endregion
    }
}
