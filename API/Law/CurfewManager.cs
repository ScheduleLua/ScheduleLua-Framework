using MelonLoader;
using MoonSharp.Interpreter;
using ScheduleOne.Law;
using System;
using UnityEngine;

namespace ScheduleLua.API.Law
{
    /// <summary>
    /// Provides an interface to the ScheduleOne Curfew system for Lua scripts
    /// </summary>
    public static class CurfewManagerAPI
    {
        private static MelonLogger.Instance _logger => ScheduleLua.Core.Instance.LoggerInstance;

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
            luaEngine.Globals["RegisterCurfewCallback"] = (Action<string, string>)RegisterCurfewCallback;
            
            _logger.Msg("CurfewManager API registered with Lua engine");
        }

        #region Curfew Status Functions

        /// <summary>
        /// Checks if the curfew system is enabled in the game
        /// </summary>
        public static bool IsCurfewEnabled()
        {
            try
            {
                if (ScheduleOne.Law.CurfewManager.Instance == null)
                    return false;
                    
                return ScheduleOne.Law.CurfewManager.Instance.IsEnabled;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error checking if curfew is enabled: {ex.Message}");
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
                if (ScheduleOne.Law.CurfewManager.Instance == null)
                    return false;
                    
                return ScheduleOne.Law.CurfewManager.Instance.IsCurrentlyActive;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error checking if curfew is active: {ex.Message}");
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
                if (ScheduleOne.Law.CurfewManager.Instance == null)
                    return false;
                    
                return ScheduleOne.Law.CurfewManager.Instance.IsCurrentlyActiveWithTolerance;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error checking if curfew is active with tolerance: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the curfew start time (9PM/2100)
        /// </summary>
        public static int GetCurfewStartTime()
        {
            return ScheduleOne.Law.CurfewManager.CURFEW_START_TIME;
        }

        /// <summary>
        /// Gets the curfew end time (5AM/500)
        /// </summary>
        public static int GetCurfewEndTime()
        {
            return ScheduleOne.Law.CurfewManager.CURFEW_END_TIME;
        }

        /// <summary>
        /// Gets the curfew warning time (8:30PM/2030)
        /// </summary>
        public static int GetCurfewWarningTime()
        {
            return ScheduleOne.Law.CurfewManager.WARNING_TIME;
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
                if (timeManager.IsCurrentTimeWithinRange(ScheduleOne.Law.CurfewManager.WARNING_TIME, ScheduleOne.Law.CurfewManager.CURFEW_START_TIME))
                {
                    return ScheduleOne.GameTime.TimeManager.GetMinSumFrom24HourTime(ScheduleOne.Law.CurfewManager.CURFEW_START_TIME) - 
                           ScheduleOne.GameTime.TimeManager.GetMinSumFrom24HourTime(currentTime);
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error calculating time until curfew: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Registers a Lua function as a callback for curfew events
        /// </summary>
        public static void RegisterCurfewCallback(string eventType, string functionName)
        {
            try
            {
                if (string.IsNullOrEmpty(eventType) || string.IsNullOrEmpty(functionName))
                {
                    _logger.Error("Cannot register curfew callback: Event type or function name is empty");
                    return;
                }
                
                switch (eventType.ToLower())
                {
                    case "enabled":
                        HookCurfewEnabledEvent(functionName);
                        break;
                    case "disabled":
                        HookCurfewDisabledEvent(functionName);
                        break;
                    case "warning":
                        HookCurfewWarningEvent(functionName);
                        break;
                    case "hint":
                        HookCurfewHintEvent(functionName);
                        break;
                    default:
                        _logger.Error($"Unknown curfew event type: {eventType}. Valid types are: enabled, disabled, warning, hint");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error registering curfew callback: {ex.Message}");
            }
        }

        private static void HookCurfewEnabledEvent(string functionName)
        {
            if (ScheduleOne.Law.CurfewManager.Instance == null)
                return;
                
            ScheduleOne.Law.CurfewManager.Instance.onCurfewEnabled.AddListener(() => {
                ScheduleLua.Core.Instance.TriggerEvent(functionName);
            });
            
            _logger.Msg($"Registered Lua function '{functionName}' for curfew enabled event");
        }

        private static void HookCurfewDisabledEvent(string functionName)
        {
            if (ScheduleOne.Law.CurfewManager.Instance == null)
                return;
                
            ScheduleOne.Law.CurfewManager.Instance.onCurfewDisabled.AddListener(() => {
                ScheduleLua.Core.Instance.TriggerEvent(functionName);
            });
            
            _logger.Msg($"Registered Lua function '{functionName}' for curfew disabled event");
        }

        private static void HookCurfewWarningEvent(string functionName)
        {
            if (ScheduleOne.Law.CurfewManager.Instance == null)
                return;
                
            ScheduleOne.Law.CurfewManager.Instance.onCurfewWarning.AddListener(() => {
                ScheduleLua.Core.Instance.TriggerEvent(functionName);
            });
            
            _logger.Msg($"Registered Lua function '{functionName}' for curfew warning event");
        }

        private static void HookCurfewHintEvent(string functionName)
        {
            if (ScheduleOne.Law.CurfewManager.Instance == null)
                return;
                
            ScheduleOne.Law.CurfewManager.Instance.onCurfewHint.AddListener(() => {
                ScheduleLua.Core.Instance.TriggerEvent(functionName);
            });
            
            _logger.Msg($"Registered Lua function '{functionName}' for curfew hint event");
        }

        #endregion
    }
}
