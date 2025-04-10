using System;
using UnityEngine;
using MoonSharp.Interpreter;
using ScheduleLua.API.NPC;
using ScheduleLua.API.Player;
using ScheduleLua.API.Core;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace ScheduleLua
{
    /// <summary>
    /// Manages the Lua scripting environment and script execution
    /// </summary>
    public class ScriptRunner : MonoBehaviour
    {
        private static ScriptRunner _instance;
        public static ScriptRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("LuaScriptRunner");
                    _instance = go.AddComponent<ScriptRunner>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        /// <summary>
        /// The current Lua script being executed
        /// </summary>
        public Script CurrentScript { get; private set; }

        /// <summary>
        /// Dictionary to store script names for better error reporting
        /// </summary>
        private Dictionary<Script, string> _scriptSources = new Dictionary<Script, string>();
        
        /// <summary>
        /// Dictionary to store script code for line number references
        /// </summary>
        private Dictionary<string, string[]> _scriptLines = new Dictionary<string, string[]>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(this.gameObject);

            // Initialize the Lua environment
            InitializeLua();
        }

        /// <summary>
        /// Initializes the MoonSharp Lua environment and registers API functions
        /// </summary>
        private void InitializeLua()
        {
            try
            {
                // Register Unity types with MoonSharp
                UserData.RegisterAssembly();

                // Create a new script instance
                CurrentScript = new Script();
                
                // Enable the debugger for better error reporting
                CurrentScript.DebuggerEnabled = true;
                
                // Register core utility functions
                RegisterCoreAPI();
                
                // Register API modules
                RegisterNPCAPI();
                RegisterPlayerAPI();
                
                Debug.Log("[ScheduleLua] Lua environment initialized successfully");
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// Registers essential core functions for Lua scripts
        /// </summary>
        private void RegisterCoreAPI()
        {
            // Register logging function
            CurrentScript.Globals["Log"] = (Action<string>)((message) => {
                Debug.Log($"[LuaScript] {message}");
            });
            
            // Register debug/print function
            CurrentScript.Globals["print"] = (Action<string>)((message) => {
                Debug.Log($"[LuaScript] {message}");
            });
        }

        /// <summary>
        /// Registers NPC-related API functions
        /// </summary>
        private void RegisterNPCAPI()
        {
            CurrentScript.Globals["GetNPC"] = (Func<string, Table>)NPCAPI.GetNPC;
            CurrentScript.Globals["GetNPCsInRegion"] = (Func<string, Table>)NPCAPI.GetNPCsInRegion;
            CurrentScript.Globals["GetAllNPCs"] = (Func<Table>)NPCAPI.GetAllNPCs;
            CurrentScript.Globals["IsNPCInRegion"] = (Func<string, string, bool>)NPCAPI.IsNPCInRegion;
        }

        /// <summary>
        /// Registers Player-related API functions
        /// </summary>
        private void RegisterPlayerAPI()
        {
            CurrentScript.Globals["GetPlayerState"] = (Func<Table>)PlayerAPI.GetPlayerState;
            CurrentScript.Globals["GetPlayerPosition"] = (Func<Table>)PlayerAPI.GetPlayerPosition;
            CurrentScript.Globals["SetPlayerHealth"] = (Func<float, bool>)PlayerAPI.SetHealth;
            CurrentScript.Globals["TeleportPlayer"] = (Func<float, float, float, bool>)PlayerAPI.TeleportPlayer;
            CurrentScript.Globals["GetPlayerRegion"] = (Func<string>)PlayerAPI.GetPlayerRegion;
            CurrentScript.Globals["IsPlayerInRegion"] = (Func<string, bool>)IsPlayerInRegion;
        }
        
        /// <summary>
        /// Checks if the player is in the specified region
        /// </summary>
        /// <param name="region">The region name to check</param>
        /// <returns>True if the player is in the specified region, false otherwise</returns>
        private bool IsPlayerInRegion(string region)
        {
            // Get the current player region
            string playerRegion = PlayerAPI.GetPlayerRegion();
            
            // Handle null or empty strings
            if (string.IsNullOrEmpty(playerRegion) || string.IsNullOrEmpty(region))
                return false;
                
            // Try direct case-insensitive match first
            if (playerRegion.Equals(region, StringComparison.OrdinalIgnoreCase))
                return true;
                
            // Try to parse as EMapRegion enum
            try
            {
                ScheduleOne.Map.EMapRegion requestedRegion;
                ScheduleOne.Map.EMapRegion currentRegion;
                
                // Try to parse the requested region
                if (Enum.TryParse<ScheduleOne.Map.EMapRegion>(region, true, out requestedRegion))
                {
                    // Try to parse the player's current region
                    if (Enum.TryParse<ScheduleOne.Map.EMapRegion>(playerRegion, true, out currentRegion))
                    {
                        return requestedRegion == currentRegion;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore any parsing errors and fall back to string comparison
            }
            
            // If all else fails, simply check if the regions contain each other
            return playerRegion.IndexOf(region, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   region.IndexOf(playerRegion, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        
        /// <summary>
        /// Extracts a line number from an error message if possible
        /// </summary>
        private int ExtractLineNumber(string errorMessage)
        {
            // Common format for Lua errors: ":line_number:"
            foreach (var part in errorMessage.Split(':'))
            {
                if (int.TryParse(part.Trim(), out int lineNum))
                {
                    return lineNum;
                }
            }
            return -1;
        }
        
        /// <summary>
        /// Attempts to extract the variable name from a nil value error
        /// </summary>
        private string ExtractVariableFromNilError(string errorMessage)
        {
            // The error is usually in the form "attempt to call a nil value (global 'X')"
            if (errorMessage.Contains("global '"))
            {
                int startIndex = errorMessage.IndexOf("global '") + 8;
                int endIndex = errorMessage.IndexOf("'", startIndex);
                if (endIndex > startIndex)
                {
                    return errorMessage.Substring(startIndex, endIndex - startIndex);
                }
            }
            return null;
        }

        /// <summary>
        /// Executes a Lua script from a string
        /// </summary>
        public DynValue ExecuteString(string code, string scriptName = "inline_code")
        {
            try
            {
                // Store script name and code for error reporting
                _scriptSources[CurrentScript] = scriptName;
                _scriptLines[scriptName] = code.Split('\n');
                
                // Execute the code
                return CurrentScript.DoString(code);
            }
            catch (InterpreterException luaEx)
            {
                return DynValue.Nil;
            }
            catch (Exception ex)
            {
                return DynValue.Nil;
            }
        }

        /// <summary>
        /// Executes a Lua script from a file
        /// </summary>
        public DynValue ExecuteFile(string filePath)
        {
            try
            {
                string scriptName = Path.GetFileName(filePath);
                Debug.Log($"[ScheduleLua] Loading script: {scriptName}");
                
                string code = File.ReadAllText(filePath);
                
                // Store the script lines for reference
                _scriptLines[scriptName] = code.Split('\n');
                
                // Execute the script
                return ExecuteString(code, scriptName);
            }
            catch (InterpreterException luaEx)
            {
                string scriptName = Path.GetFileName(filePath);
                string code = string.Empty;
                
                try { code = File.ReadAllText(filePath); } catch { /* Ignore if we can't read the file */ }
                
                Debug.Log($"Error loading scrip {luaEx.DecoratedMessage}");
                return DynValue.Nil;
            }
            catch (Exception ex)
            {
                return DynValue.Nil;
            }
        }

        /// <summary>
        /// Calls a global Lua function
        /// </summary>
        public DynValue CallFunction(string functionName, params object[] args)
        {
            try
            {
                // Check if the function exists before calling it
                bool functionExists = false;
                foreach (var key in CurrentScript.Globals.Keys)
                {
                    if (key.ToString() == functionName)
                    {
                        DynValue value = CurrentScript.Globals.Get(functionName);
                        if (value.Type != DataType.Function)
                        {
                            throw new InvalidOperationException($"'{functionName}' exists but is not a function (it's a {value.Type})");
                        }
                        functionExists = true;
                        break;
                    }
                }
                
                if (!functionExists)
                {
                    throw new InvalidOperationException($"Function '{functionName}' does not exist in the Lua environment");
                }
                
                return CurrentScript.Call(CurrentScript.Globals[functionName], args);
            }
            catch (InterpreterException luaEx)
            {
                string scriptName = _scriptSources.ContainsKey(CurrentScript) ? _scriptSources[CurrentScript] : "unknown";
                string code = _scriptLines.ContainsKey(scriptName) ? string.Join("\n", _scriptLines[scriptName]) : null;
                
                Debug.Log($"Error calling Lua function {luaEx.DecoratedMessage}");
                return DynValue.Nil;
            }
            catch (Exception ex)
            {
                Debug.Log($"Error calling Lua function {ex.Message}");
                return DynValue.Nil;
            }
        }

        /// <summary>
        /// Gets a readable description of a Lua value for debugging
        /// </summary>
        private string GetValueDescription(DynValue value)
        {
            if (value == null)
                return "nil";
                
            switch (value.Type)
            {
                case DataType.Nil:
                    return "nil";
                case DataType.Void:
                    return "void";
                case DataType.Boolean:
                    return value.Boolean.ToString().ToLower();
                case DataType.Number:
                    return value.Number.ToString();
                case DataType.String:
                    return $"\"{value.String}\"";
                case DataType.Function:
                    return "<function>";
                case DataType.Table:
                    return "<table>";
                case DataType.Tuple:
                    return "<tuple>";
                case DataType.UserData:
                    return $"<userdata:{value.UserData?.Object?.GetType().Name ?? "unknown"}>";
                case DataType.Thread:
                    return "<thread>";
                case DataType.ClrFunction:
                    return "<clr_function>";
                case DataType.TailCallRequest:
                    return "<tailcall>";
                case DataType.YieldRequest:
                    return "<yield>";
                default:
                    return $"<{value.Type}>";
            }
        }
    }
} 