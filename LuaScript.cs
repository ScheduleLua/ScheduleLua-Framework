using MelonLoader;
using System;
using System.IO;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;
using ScheduleLua.API.Registry;

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
        
        // Track commands registered by this script
        private HashSet<string> _registeredCommands = new HashSet<string>();
        
        public string FilePath => _filePath;
        public string Name => _name;
        public bool IsLoaded => _scriptInstance != null;
        public bool IsInitialized => _isInitialized;
        
        // Allow accessing the script's registered commands
        public IReadOnlyCollection<string> RegisteredCommands => _registeredCommands;
        
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
            
            // Register the script instance with the command registry
            CommandRegistry.RegisterScriptInstance(this);
        }
        
        /// <summary>
        /// Registers a command as belonging to this script
        /// </summary>
        public void AddRegisteredCommand(string commandName)
        {
            if (!string.IsNullOrEmpty(commandName))
            {
                _registeredCommands.Add(commandName);
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
                
                if (!_scriptInstance.IsNil() && !_scriptInstance.IsVoid())
                {
                    _scriptEngine.Globals[_name + "_module"] = _scriptInstance;
                }
                
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
            catch (InterpreterException luaEx)
            {
                LogDetailedError(luaEx, $"Error initializing script {_name}");
                return false;
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
            catch (InterpreterException luaEx)
            {
                LogDetailedError(luaEx, $"Error in script {_name} Update");
                _hasUpdateFunction = false; // Prevent further update calls if there's an error
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in script {_name} Update: {ex.Message}");
                _hasUpdateFunction = false; // Prevent further update calls if there's an error
            }
        }
        
        /// <summary>
        /// Reloads the script from its file, preserving and restoring its registered commands
        /// </summary>
        public bool Reload()
        {
            bool wasInitialized = _isInitialized;
            
            // Store current registered commands
            HashSet<string> previousCommands = new HashSet<string>(_registeredCommands);
            
            // Unregister only this script's commands
            _logger.Msg($"Unregistering {_registeredCommands.Count} commands from script {_name} before reload");
            foreach (var command in _registeredCommands)
            {
                CommandRegistry.UnregisterCommand(command);
            }
            
            // Clear command list but don't forget which ones we had
            _registeredCommands.Clear();
            
            // Clear event handlers
            _eventHandlers.Clear();
            
            // Load script again
            if (!Load())
                return false;
                
            // Re-initialize if it was initialized before
            if (wasInitialized)
            {
                bool initResult = Initialize();
                
                // Now trigger the OnConsoleReady event to ensure commands get re-registered
                // Note: Re-registering commands is the script's responsibility during OnConsoleReady
                if (initResult && _eventHandlers.ContainsKey("OnConsoleReady"))
                {
                    _logger.Msg($"Triggering OnConsoleReady to re-register commands for script {_name}");
                    TriggerEvent("OnConsoleReady");
                }
                
                return initResult;
            }
                
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
            catch (InterpreterException luaEx)
            {
                LogDetailedError(luaEx, $"Error in script {_name} event handler for {eventName}");
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
            CheckAndRegisterEvent("OnConsoleReady");
            CheckAndRegisterEvent("OnRegistryReady");
            CheckAndRegisterEvent("OnCurfewEnabled");
            CheckAndRegisterEvent("OnCurfewDisabled");
            CheckAndRegisterEvent("OnCurfewWarning");
            CheckAndRegisterEvent("OnCurfewHint");
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
                // _logger.Msg($"Script {_name} registered handler for {eventName}");
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
            catch (InterpreterException luaEx)
            {
                LogDetailedError(luaEx, $"Error calling function {functionName} in script {_name}");
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