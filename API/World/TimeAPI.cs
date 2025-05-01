using System;
using System.Collections.Generic;
using System.Text;
using MoonSharp.Interpreter;
using ScheduleOne.GameTime;
using ScheduleLua.API.Core;
using ScheduleLua.API.Base;

namespace ScheduleLua.API.World
{
    public class TimeAPI : BaseLuaApiModule
    {
        /// <summary>
        /// Registers all time-related API functions with the Lua engine
        /// </summary>
        public override void RegisterAPI(Script luaEngine)
        {
            if (luaEngine == null)
                throw new ArgumentNullException(nameof(luaEngine));

            // Time functions
            luaEngine.Globals["GetGameTime"] = (Func<int>)GetGameTime;
            luaEngine.Globals["GetGameDay"] = (Func<string>)GetGameDay;
            luaEngine.Globals["GetGameDayInt"] = (Func<int>)GetGameDayInt;
            luaEngine.Globals["IsNightTime"] = (Func<bool>)IsNightTime;
            luaEngine.Globals["FormatGameTime"] = (Func<int, string>)FormatGameTime;
        }

        /// <summary>
        /// Gets the current game time
        /// </summary>
        /// <returns>The current time value</returns>
        public static int GetGameTime()
        {
            TimeManager time = TimeManager.Instance;
            if (time == null)
                return 0;

            return time.CurrentTime;
        }

        /// <summary>
        /// Gets the current game day as a string
        /// </summary>
        /// <returns>The current day name</returns>
        public static string GetGameDay()
        {
            TimeManager time = TimeManager.Instance;
            if (time == null)
                return "Monday";

            return time.CurrentDay.ToString();
        }

        /// <summary>
        /// Gets the current game day as an integer
        /// </summary>
        /// <returns>The current day as an integer value</returns>
        public static int GetGameDayInt()
        {
            TimeManager time = TimeManager.Instance;
            if (time == null)
                return 0;

            return (int)time.CurrentDay;
        }

        /// <summary>
        /// Checks if it is currently night time in the game
        /// </summary>
        /// <returns>True if it is night time, false otherwise</returns>
        public static bool IsNightTime()
        {
            TimeManager time = TimeManager.Instance;
            if (time == null)
                return false;

            return time.IsNight;
        }

        /// <summary>
        /// Formats a time value into a human-readable string
        /// </summary>
        /// <param name="timeValue">The time value to format</param>
        /// <returns>Formatted time string (e.g. "12:30 PM")</returns>
        public static string FormatGameTime(int timeValue)
        {
            if (TimeManager.Instance == null)
                return "00:00";

            // Using static method instead of instance method
            return TimeManager.Get12HourTime(timeValue);
        }
    }
}
