using System;
using UnityEngine;
using MoonSharp.Interpreter;
using ScheduleOne;
using System.Collections.Generic;
using System.IO;
using StringMatchingTools;

namespace ScheduleLua.API.Core
{
    /// <summary>
    /// Provides utility methods and shared functionality for working with MoonSharp Lua
    /// </summary>
    public static class LuaUtility
    {
        // Add static readonly fields for logger and engine references
        private static readonly MelonLoader.MelonLogger.Instance LoggerInstance;
        private static readonly Script _luaEngine;

        static LuaUtility()
        {
            // Store references to avoid repeatedly accessing Core.Instance
            LoggerInstance = ScheduleLua.Core.Instance.LoggerInstance;
            _luaEngine = ScheduleLua.Core.Instance._luaEngine;

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
            // Return the cached reference
            return _luaEngine;
        }

        /// <summary>
        /// Creates a new Lua table
        /// </summary>
        public static Table CreateTable()
        {
            return new Table(_luaEngine);
        }

        /// <summary>
        /// Logs a message with the Lua API prefix
        /// </summary>
        public static void Log(string message)
        {
            LoggerInstance.Msg($"{message}");
        }

        /// <summary>
        /// Logs a warning message with the Lua API prefix
        /// </summary>
        public static void LogWarning(string message)
        {
            LoggerInstance.Warning($"{message}");
        }

        /// <summary>
        /// Logs an error message with the Lua API prefix
        /// </summary>
        public static void LogError(string message, Exception ex = null)
        {
            string errorMessage = $"{message}";

            // Check if the exception is a Lua interpreter exception
            if (ex != null)
            {
                if (ex is MoonSharp.Interpreter.InterpreterException luaEx)
                {
                    // Use the detailed Lua error logging for interpreter exceptions
                    LogLuaError(luaEx, message, GetCurrentScriptPath());
                    return;
                }
                else
                {
                    // For other exceptions, just append the message
                    errorMessage += $": {ex.Message}";
                }
            }

            LoggerInstance.Error(errorMessage);
        }

        /// <summary>
        /// Logs detailed error information for Lua script errors
        /// </summary>
        public static void LogLuaError(MoonSharp.Interpreter.InterpreterException luaEx, string context, string filePath)
        {
            try
            {
                string scriptName = Path.GetFileName(filePath);
                string scriptContent = null;

                try
                {
                    scriptContent = File.ReadAllText(filePath);
                }
                catch
                {
                    // Ignore errors reading the file
                }

                string errorMessage = luaEx.DecoratedMessage;
                LoggerInstance.Error($"{context}: {errorMessage}");

                // Extract line number if possible
                int lineNumber = -1;
                foreach (var part in errorMessage.Split(':'))
                {
                    if (int.TryParse(part.Trim(), out int line))
                    {
                        lineNumber = line;
                        break;
                    }
                }

                // Output code snippet
                if (lineNumber > 0 && scriptContent != null)
                {
                    LoggerInstance.Error($"Error on line {lineNumber}:");

                    string[] lines = scriptContent.Split('\n');
                    if (lineNumber <= lines.Length)
                    {
                        int startLine = Math.Max(0, lineNumber - 3);
                        int endLine = Math.Min(lines.Length - 1, lineNumber + 2);

                        for (int i = startLine; i <= endLine; i++)
                        {
                            string prefix = (i + 1 == lineNumber) ? ">>>" : "   ";
                            LoggerInstance.Error($"{prefix} {i + 1}: {lines[i].TrimEnd()}");
                        }
                    }
                }

                // Output stack trace
                LoggerInstance.Error("Stack trace:");
                if (luaEx.CallStack != null && luaEx.CallStack.Count > 0)
                {
                    foreach (var frame in luaEx.CallStack)
                    {
                        string funcName = !string.IsNullOrEmpty(frame.Name) ? frame.Name : "<anonymous>";
                        LoggerInstance.Error($"  at {funcName} in {frame.Location}");
                    }
                }
                else
                {
                    LoggerInstance.Error($"  at {scriptName}:{lineNumber}");
                }

                // Special handling for nil value errors
                if (errorMessage.Contains("attempt to call a nil value"))
                {
                    LoggerInstance.Error("This error occurs when trying to call a function that doesn't exist.");
                    LoggerInstance.Error("Check for:");
                    LoggerInstance.Error("1. Misspelled function names");
                    LoggerInstance.Error("2. Functions defined in the wrong scope");
                    LoggerInstance.Error("3. Missing API functions or libraries");

                    // Try to extract variable name
                    if (errorMessage.Contains("global '"))
                    {
                        int start = errorMessage.IndexOf("global '") + 8;
                        int end = errorMessage.IndexOf("'", start);
                        if (end > start)
                        {
                            string varName = errorMessage.Substring(start, end - start);
                            LoggerInstance.Error($"The undefined function appears to be: '{varName}'");

                            // List available API functions that might be similar
                            LoggerInstance.Error("Available global functions with similar names:");
                            int matchesFound = 0;
                            foreach (var key in _luaEngine.Globals.Keys)
                            {
                                string keyStr = key.ToString();
                                if (keyStr.Contains(varName) || (varName.Length > 3 &&
                                    SMT.Calculate(keyStr, varName) <= 3))
                                {
                                    DynValue val = _luaEngine.Globals.Get(keyStr);
                                    LoggerInstance.Error($"  {keyStr} (type: {val.Type})");
                                    matchesFound++;

                                    if (matchesFound >= 5)
                                        break;
                                }
                            }

                            if (matchesFound == 0)
                            {
                                LoggerInstance.Error("  No similar function names found.");
                            }
                        }
                    }
                }
                // Handle 'attempt to index a nil value' errors
                else if (errorMessage.Contains("attempt to index a nil value"))
                {
                    LoggerInstance.Error("This error occurs when trying to access a field or method on a variable that is nil (undefined).");
                    LoggerInstance.Error("Check for:");
                    LoggerInstance.Error("1. Misspelled variable or table names");
                    LoggerInstance.Error("2. Variables that were never assigned a value");
                    LoggerInstance.Error("3. API functions or objects that failed to initialize");
                    LoggerInstance.Error("4. Logic that may set a variable to nil before this line");
                }
                // Handle 'attempt to perform arithmetic on a nil value' errors
                else if (errorMessage.Contains("attempt to perform arithmetic on a nil value"))
                {
                    LoggerInstance.Error("This error occurs when trying to use a nil (undefined) variable in a math operation.");
                    LoggerInstance.Error("Check for:");
                    LoggerInstance.Error("1. Variables that were never assigned a value");
                    LoggerInstance.Error("2. Functions that may return nil instead of a number");
                    LoggerInstance.Error("3. Misspelled variable names");
                }
                // Handle type mismatch errors
                else if (errorMessage.Contains("attempt to concatenate") && errorMessage.Contains("a nil value"))
                {
                    LoggerInstance.Error("This error occurs when trying to concatenate (join) a string with a nil value.");
                    LoggerInstance.Error("Check for:");
                    LoggerInstance.Error("1. Variables that may be nil when used in string concatenation");
                    LoggerInstance.Error("2. Use tostring(var) to safely convert variables to strings");
                }
                else if (errorMessage.Contains("attempt to concatenate") && errorMessage.Contains("a number value"))
                {
                    LoggerInstance.Error("This error occurs when trying to concatenate (join) a string with a number value without converting it to a string.");
                    LoggerInstance.Error("Check for:");
                    LoggerInstance.Error("1. Use tostring(var) to convert numbers to strings before concatenation");
                }
                else if (errorMessage.Contains("bad argument") && errorMessage.Contains("number expected"))
                {
                    LoggerInstance.Error("This error occurs when a function expected a number argument but received a different type (often nil or string).");
                    LoggerInstance.Error("Check for:");
                    LoggerInstance.Error("1. The value passed to the function is actually a number");
                    LoggerInstance.Error("2. Use tonumber(var) to convert strings to numbers if needed");
                }
                else if (errorMessage.Contains("bad argument") && errorMessage.Contains("string expected"))
                {
                    LoggerInstance.Error("This error occurs when a function expected a string argument but received a different type (often nil or number).");
                    LoggerInstance.Error("Check for:");
                    LoggerInstance.Error("1. The value passed to the function is actually a string");
                    LoggerInstance.Error("2. Use tostring(var) to convert numbers to strings if needed");
                }
            }
            catch (Exception ex)
            {
                // Fallback if detailed error reporting fails
                LogError($"{context}: {luaEx.Message}");
                LogError($"Error while generating detailed error report: {ex.Message}");
            }
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

        /// <summary>
        /// Gets the current script path from the Lua environment
        /// </summary>
        private static string GetCurrentScriptPath()
        {
            try
            {
                // Try to get the script path from the global SCRIPT_PATH variable
                DynValue scriptPath = _luaEngine.Globals.Get("SCRIPT_PATH");
                if (scriptPath != null && scriptPath.Type == DataType.String)
                {
                    return scriptPath.String;
                }
            }
            catch
            {
                // Ignore errors trying to get the script path
            }

            return "unknown";
        }
    }
}