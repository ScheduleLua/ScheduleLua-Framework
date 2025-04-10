using MelonLoader;
using System;
using System.IO;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;

namespace ScheduleLua
{
    /// <summary>
    /// Represents a loaded Lua script with lifecycle methods
    /// </summary>
    public class LuaScript
    {
        private readonly string _filePath;
        private readonly string _name;
        private readonly Script _scriptEngine;
        private readonly MelonLogger.Instance _logger;
        
        private DynValue _scriptInstance;
        private bool _isInitialized = false;
        private bool _hasUpdateFunction = false;
        
        // Dictionary of event handlers
        private Dictionary<string, DynValue> _eventHandlers = new Dictionary<string, DynValue>();
        
        public string FilePath => _filePath;
        public string Name => _name;
        public bool IsLoaded => _scriptInstance != null;
        public bool IsInitialized => _isInitialized;
        
        public LuaScript(string filePath, Script scriptEngine, MelonLogger.Instance logger)
        {
            _filePath = filePath;
            _name = Path.GetFileNameWithoutExtension(filePath);
            _scriptEngine = scriptEngine;
            _logger = logger;
            
            // Ensure the debugger is enabled for better error reporting
            if (_scriptEngine != null && !_scriptEngine.DebuggerEnabled)
            {
                _scriptEngine.DebuggerEnabled = true;
            }
        }
        
        /// <summary>
        /// Loads the script from file
        /// </summary>
        public bool Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    _logger.Error($"Script file not found: {_filePath}");
                    return false;
                }
                
                string scriptContent = File.ReadAllText(_filePath);
                _scriptInstance = _scriptEngine.DoString(scriptContent, null, _name);
                
                CheckForUpdateFunction();
                RegisterEventHandlers();
                
                return true;
            }
            catch (InterpreterException luaEx)
            {
                LogDetailedError(luaEx, $"Error loading script {_name}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error loading script {_name}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Logs detailed error information for Lua script errors including stack traces
        /// </summary>
        private void LogDetailedError(InterpreterException luaEx, string context)
        {
            string errorMessage = luaEx.DecoratedMessage;
            string scriptContent = string.Empty;
            
            try {
                scriptContent = File.ReadAllText(_filePath);
            } catch {
                // Ignore errors when trying to read the file
            }
            
            _logger.Error($"{context}: {errorMessage}");
            
            // Extract line number if available
            int lineNumber = -1;
            foreach (var part in errorMessage.Split(':'))
            {
                if (int.TryParse(part.Trim(), out int line))
                {
                    lineNumber = line;
                    break;
                }
            }
            
            // Print problematic code section if available
            if (lineNumber > 0 && !string.IsNullOrEmpty(scriptContent))
            {
                string[] lines = scriptContent.Split('\n');
                if (lineNumber <= lines.Length)
                {
                    _logger.Error($"Error on line {lineNumber}:");
                    
                    int startLine = Math.Max(0, lineNumber - 3);
                    int endLine = Math.Min(lines.Length - 1, lineNumber + 2);
                    
                    for (int i = startLine; i <= endLine; i++)
                    {
                        string linePrefix = (i + 1 == lineNumber) ? ">>>" : "   ";
                        _logger.Error($"{linePrefix} {i + 1}: {lines[i].TrimEnd()}");
                    }
                }
            }
            
            // Print stack trace
            _logger.Error("Stack trace:");
            if (luaEx.CallStack != null && luaEx.CallStack.Count > 0)
            {
                foreach (var frame in luaEx.CallStack)
                {
                    string funcName = !string.IsNullOrEmpty(frame.Name) ? frame.Name : "<anonymous>";
                    _logger.Error($"  at {funcName} in {frame.Location}");
                }
            }
            else
            {
                _logger.Error($"  at {_name}:{lineNumber}");
            }
            
            // Provide additional context for nil value errors
            if (errorMessage.Contains("attempt to call a nil value"))
            {
                _logger.Error("This error occurs when trying to call a function that doesn't exist.");
                _logger.Error("Check for:");
                _logger.Error("1. Misspelled function names");
                _logger.Error("2. Functions defined in the wrong scope");
                _logger.Error("3. Missing API functions or libraries");
                
                // Try to extract the variable name from the error
                string varName = null;
                if (errorMessage.Contains("global '"))
                {
                    int startIndex = errorMessage.IndexOf("global '") + 8;
                    int endIndex = errorMessage.IndexOf("'", startIndex);
                    if (endIndex > startIndex)
                    {
                        varName = errorMessage.Substring(startIndex, endIndex - startIndex);
                        _logger.Error($"The undefined function appears to be: '{varName}'");
                    }
                }
            }
        }
        
        /// <summary>
        /// Initializes the script by calling its Initialize function if it exists
        /// </summary>
        public bool Initialize()
        {
            if (!IsLoaded)
                return false;
                
            try
            {
                DynValue initFunction = _scriptEngine.Globals.Get("Initialize");
                
                if (initFunction.Type == DataType.Function)
                {
                    _scriptEngine.Call(initFunction);
                    _isInitialized = true;
                    return true;
                }
                
                // No initialization function is still a success
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error initializing script {_name}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Calls the script's Update function if it exists
        /// </summary>
        public void Update()
        {
            if (!IsInitialized || !_hasUpdateFunction)
                return;
                
            try
            {
                DynValue updateFunction = _scriptEngine.Globals.Get("Update");
                _scriptEngine.Call(updateFunction);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in script {_name} Update: {ex.Message}");
                _hasUpdateFunction = false; // Prevent further update calls if there's an error
            }
        }
        
        /// <summary>
        /// Reloads the script from its file
        /// </summary>
        public bool Reload()
        {
            bool wasInitialized = _isInitialized;
            
            // Clear event handlers
            _eventHandlers.Clear();
            
            // Load script again
            if (!Load())
                return false;
                
            // Re-initialize if it was initialized before
            if (wasInitialized)
                return Initialize();
                
            return true;
        }
        
        /// <summary>
        /// Triggers an event in this script
        /// </summary>
        public void TriggerEvent(string eventName, params object[] args)
        {
            if (!IsInitialized || !_eventHandlers.ContainsKey(eventName))
                return;
                
            try
            {
                DynValue handler = _eventHandlers[eventName];
                _scriptEngine.Call(handler, args);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in script {_name} event handler for {eventName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Registers event handlers from the script
        /// </summary>
        private void RegisterEventHandlers()
        {
            _eventHandlers.Clear();
            
            // Check for OnCommand event handler
            CheckAndRegisterEvent("OnCommand");
            
            // Game-specific events
            CheckAndRegisterEvent("OnDayChanged");
            CheckAndRegisterEvent("OnTimeChanged");
            CheckAndRegisterEvent("OnSleepStart");
            CheckAndRegisterEvent("OnSleepEnd");
            CheckAndRegisterEvent("OnPlayerMoneyChanged");
            CheckAndRegisterEvent("OnPlayerHealthChanged");
            CheckAndRegisterEvent("OnPlayerEnergyChanged");
            CheckAndRegisterEvent("OnItemAdded");
            CheckAndRegisterEvent("OnItemRemoved");
            CheckAndRegisterEvent("OnPlayerMovedSignificantly");
            CheckAndRegisterEvent("OnNPCInteraction");
            CheckAndRegisterEvent("OnSceneLoaded");
            CheckAndRegisterEvent("OnPlayerReady");
        }
        
        /// <summary>
        /// Check for a function and register it as an event handler if it exists
        /// </summary>
        private void CheckAndRegisterEvent(string eventName)
        {
            DynValue handler = _scriptEngine.Globals.Get(eventName);
            if (handler != null && handler.Type == DataType.Function)
            {
                _eventHandlers[eventName] = handler;
                _logger.Msg($"Script {_name} registered handler for {eventName}");
            }
        }
        
        /// <summary>
        /// Calls a function in the script if it exists and returns the result
        /// </summary>
        public DynValue CallFunction(string functionName, params object[] args)
        {
            if (!IsInitialized)
                return DynValue.Nil;
                
            try
            {
                DynValue function = _scriptEngine.Globals.Get(functionName);
                
                if (function != null && function.Type == DataType.Function)
                {
                    return _scriptEngine.Call(function, args);
                }
                
                return DynValue.Nil;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error calling function {functionName} in script {_name}: {ex.Message}");
                return DynValue.Nil;
            }
        }
        
        private void CheckForUpdateFunction()
        {
            DynValue updateFunction = _scriptEngine.Globals.Get("Update");
            _hasUpdateFunction = updateFunction != null && updateFunction.Type == DataType.Function;
        }
    }
} 