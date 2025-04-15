using System;
using UnityEngine;
using MoonSharp.Interpreter;
using ScheduleOne;
using System.Collections.Generic;

namespace ScheduleLua.API.Core
{
    /// <summary>
    /// Provides utility methods and shared functionality for working with MoonSharp Lua
    /// </summary>
    public static class LuaUtility
    {
        static LuaUtility()
        {
            // Register proxy types instead of structs
            UserData.RegisterType<Vector3Proxy>();
            
            // Register other needed types
            UserData.RegisterType<GameObject>();
        }

        /// <summary>
        /// Gets the active MoonSharp Script instance from the Lua environment
        /// </summary>
        public static Script GetLuaScript()
        {
            // Use Core's script instance instead of ScriptRunner
            return ScheduleLua.Core.Instance._luaEngine;
        }

        /// <summary>
        /// Creates a new Lua table
        /// </summary>
        public static Table CreateTable()
        {
            var script = GetLuaScript();
            return new Table(script);
        }

        /// <summary>
        /// Logs a message with the Lua API prefix
        /// </summary>
        public static void Log(string message)
        {
            ScheduleLua.Core.Instance.LoggerInstance.Msg($"[ScheduleLua] {message}");
        }

        /// <summary>
        /// Logs a warning message with the Lua API prefix
        /// </summary>
        public static void LogWarning(string message)
        {
            ScheduleLua.Core.Instance.LoggerInstance.Warning($"[ScheduleLua] {message}");
        }

        /// <summary>
        /// Logs an error message with the Lua API prefix
        /// </summary>
        public static void LogError(string message, Exception ex = null)
        {
            string errorMessage = $"[ScheduleLua] {message}";
            if (ex != null)
            {
                errorMessage += $": {ex.Message}";
            }
            ScheduleLua.Core.Instance.LoggerInstance.Error(errorMessage);
        }

        /// <summary>
        /// Converts a Vector3 to a Lua table
        /// </summary>
        public static Table Vector3ToTable(Vector3 vector)
        {
            var table = CreateTable();
            table["x"] = vector.x;
            table["y"] = vector.y;
            table["z"] = vector.z;
            return table;
        }

        /// <summary>
        /// Gets all available map regions as an array of strings
        /// </summary>
        /// <returns>Array of region names</returns>
        public static string[] GetAllMapRegions()
        {
            try
            {
                var regionValues = Enum.GetValues(typeof(ScheduleOne.Map.EMapRegion));
                string[] regions = new string[regionValues.Length];
                
                for (int i = 0; i < regionValues.Length; i++)
                {
                    regions[i] = regionValues.GetValue(i).ToString();
                }
                
                return regions;
            }
            catch (Exception ex)
            {
                LogError("Error getting map regions", ex);
                return new string[0];
            }
        }

        /// <summary>
        /// Creates a Lua table from an array of strings
        /// </summary>
        /// <param name="array">The string array to convert</param>
        /// <returns>A Lua table with 1-based indexed strings</returns>
        public static Table StringArrayToTable(string[] array)
        {
            var table = CreateTable();
            
            for (int i = 0; i < array.Length; i++)
            {
                table[i + 1] = array[i];
            }
            
            return table;
        }
    }
} 