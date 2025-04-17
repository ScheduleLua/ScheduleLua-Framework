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
        private Table _scriptEnvironment; // Store the script's private environment

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

        // Allow access to the script's environment
        public Table ScriptEnvironment => _scriptEnvironment;

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

                // Create script isolation by using a private environment table
                DynValue envTable = DynValue.NewTable(_scriptEngine);
                _scriptEnvironment = envTable.Table;

                // Copy globals from the main environment to our private one
                Table globals = _scriptEngine.Globals;
                foreach (var pair in globals.Pairs)
                {
                    _scriptEnvironment[pair.Key] = pair.Value;
                }

                // Set up proper environment metatable that falls back to global for undefined values
                Table mt = new Table(_scriptEngine);
                mt["__index"] = _scriptEngine.Globals;
                _scriptEnvironment.MetaTable = mt;

                // Set this script's specific context variables
                _scriptEnvironment["SCRIPT_PATH"] = _filePath;
                _scriptEnvironment["SCRIPT_NAME"] = _name;

                // Execute the script with the isolated environment
                _scriptInstance = _scriptEngine.DoString(scriptContent, _scriptEnvironment, _name);

                if (!_scriptInstance.IsNil() && !_scriptInstance.IsVoid())
                {
                    // Store module export in both script environment and global space
                    _scriptEnvironment[_name + "_module"] = _scriptInstance;
                    _scriptEngine.Globals[_name + "_module"] = _scriptInstance;
                }

                CheckForUpdateFunction(_scriptEnvironment);
                RegisterEventHandlers(_scriptEnvironment);

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

            try
            {
                scriptContent = File.ReadAllText(_filePath);
            }
            catch
            {
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

            // Print stack trace with function names
            _logger.Error("Stack trace:");
            if (luaEx.CallStack != null && luaEx.CallStack.Count > 0)
            {
                bool firstFrame = true;
                foreach (var frame in luaEx.CallStack)
                {
                    string funcName = !string.IsNullOrEmpty(frame.Name) ? frame.Name : "<anonymous>";
                    string location = frame.Location != null ? frame.Location.ToString() : "<unknown location>";
                    string highlight = firstFrame ? "[ERROR HERE] " : "";
                    _logger.Error($"  {highlight}at {funcName} in {location}");
                    firstFrame = false;
                }
            }
            else
            {
                _logger.Error($"  at {_name}:{lineNumber}");
            }

            // Provide additional context for different error types
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
            else if (errorMessage.Contains("attempt to index a nil value"))
            {
                _logger.Error("This error occurs when trying to access a field or method on a variable that is nil (undefined).");
                _logger.Error("Check for:");
                _logger.Error("1. Misspelled variable or table names");
                _logger.Error("2. Variables that were never assigned a value");
                _logger.Error("3. API objects that failed to initialize");
                _logger.Error("4. Using a variable before it's defined or assigned");
                
                // Try to extract the variable name from the error
                if (errorMessage.Contains("'"))
                {
                    int startIndex = errorMessage.IndexOf("'") + 1;
                    int endIndex = errorMessage.IndexOf("'", startIndex);
                    if (endIndex > startIndex)
                    {
                        string varName = errorMessage.Substring(startIndex, endIndex - startIndex);
                        _logger.Error($"The nil variable appears to be: '{varName}'");
                    }
                }
            }
            else if (errorMessage.Contains("attempt to perform arithmetic on"))
            {
                _logger.Error("This error occurs when trying to use a nil or non-number value in a math operation.");
                _logger.Error("Check for:");
                _logger.Error("1. Variables that were never assigned a value");
                _logger.Error("2. Functions that may return nil instead of a number");
                _logger.Error("3. String values being used in arithmetic (use tonumber() to convert)");
                
                // Try to extract the problematic type
                if (errorMessage.Contains("on a"))
                {
                    int startIndex = errorMessage.IndexOf("on a") + 4;
                    int endIndex = errorMessage.IndexOf(" value", startIndex);
                    if (endIndex > startIndex)
                    {
                        string typeName = errorMessage.Substring(startIndex, endIndex - startIndex);
                        _logger.Error($"The problematic type is: '{typeName}'");
                    }
                }
            }
            else if (errorMessage.Contains("attempt to concatenate"))
            {
                _logger.Error("This error occurs when trying to concatenate (join) strings with invalid value types.");
                _logger.Error("Check for:");
                _logger.Error("1. Using nil values in string concatenation");
                _logger.Error("2. Using non-string/non-number values in concatenation");
                _logger.Error("3. Use tostring() to safely convert values to strings before concatenation");
                
                // Try to extract the problematic type
                if (errorMessage.Contains("a"))
                {
                    int startIndex = errorMessage.IndexOf("a") + 1;
                    int endIndex = errorMessage.IndexOf(" value", startIndex);
                    if (endIndex > startIndex)
                    {
                        string typeName = errorMessage.Substring(startIndex, endIndex - startIndex);
                        _logger.Error($"Attempted to concatenate with a'{typeName}' value");
                    }
                }
            }
            else if (errorMessage.Contains("bad argument"))
            {
                _logger.Error("This error occurs when a function receives an argument of the wrong type.");
                _logger.Error("Check for:");
                _logger.Error("1. Incorrect parameter order in function calls");
                _logger.Error("2. Missing required parameters");
                _logger.Error("3. Passing wrong data types to function parameters");
                
                // Try to extract expected type
                if (errorMessage.Contains("expected"))
                {
                    int startIndex = errorMessage.IndexOf("expected") + 9;
                    int endIndex = errorMessage.IndexOf(" ", startIndex);
                    if (endIndex > startIndex)
                    {
                        string expectedType = errorMessage.Substring(startIndex, endIndex - startIndex);
                        _logger.Error($"The function expected a '{expectedType}' type argument");
                        
                        if (expectedType == "number")
                        {
                            _logger.Error("Tip: Use tonumber() to convert strings to numbers");
                        }
                        else if (expectedType == "string")
                        {
                            _logger.Error("Tip: Use tostring() to convert values to strings");
                        }
                    }
                }
            }
            else if (errorMessage.Contains("attempt to call a"))
            {
                _logger.Error("This error occurs when trying to call something that is not a function.");
                _logger.Error("Check for:");
                _logger.Error("1. Variable containing a value that is not a function");
                _logger.Error("2. Syntax errors in function definitions");
                _logger.Error("3. Table access instead of method call (use : instead of .)");
                
                // Try to extract the problematic type
                if (errorMessage.Contains("call a"))
                {
                    int startIndex = errorMessage.IndexOf("call a") + 6;
                    int endIndex = errorMessage.IndexOf(" value", startIndex);
                    if (endIndex > startIndex)
                    {
                        string typeName = errorMessage.Substring(startIndex, endIndex - startIndex);
                        _logger.Error($"Attempted to call a '{typeName}' value, which is not a function");
                    }
                }
            }
            // Add more error type handlers here for other common Lua errors
        }

        /// <summary>
        /// Initializes the script by calling its Initialize function if it exists
        /// </summary>
        public bool Initialize()
        {
            if (!IsLoaded)
                return false;

            // Already initialized, don't re-initialize
            if (_isInitialized)
            {
                _logger.Msg($"Script {_name} already initialized, skipping duplicate initialization");
                return true;
            }

            // Static flag to prevent recursive initialization
            string key = $"__initializing_{_name}";

            try
            {
                // Check if this script is already being initialized
                DynValue initializingFlag = _scriptEnvironment.Get(key);
                if (initializingFlag.Type != DataType.Nil && initializingFlag.Boolean)
                {
                    // Already in initialization process, prevent recursion
                    _logger.Msg($"Script {_name} is currently initializing, preventing recursive initialization");
                    return true;
                }

                // Set flag to indicate initialization in progress
                _scriptEnvironment[key] = true;
                // _logger.Msg($"Initializing script: {_name} (path: {Path.GetFileName(_filePath)})");

                DynValue initFunction = _scriptEnvironment.Get("Initialize");

                if (initFunction.Type == DataType.Function)
                {
                    _scriptEngine.Call(initFunction);
                    _isInitialized = true;
                }
                else
                {
                    // No initialization function is still a success
                    _isInitialized = true;
                    // _logger.Msg($"No Initialize() function found for script: {_name}, marking as initialized");
                }

                // Clear the flag
                _scriptEnvironment[key] = false;
                return true;
            }
            catch (InterpreterException luaEx)
            {
                // Clear the flag on error
                try { _scriptEnvironment[key] = false; } catch { }

                LogDetailedError(luaEx, $"Error initializing script {_name}");
                return false;
            }
            catch (Exception ex)
            {
                // Clear the flag on error
                try { _scriptEnvironment[key] = false; } catch { }

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
                DynValue updateFunction = _scriptEnvironment.Get("Update");
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

            // Reset state
            _isInitialized = false;

            // Load script again with a fresh environment
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
        private void RegisterEventHandlers(Table env)
        {
            _eventHandlers.Clear();

            // Game-specific events
            CheckAndRegisterEvent("OnDayChanged", env);
            CheckAndRegisterEvent("OnTimeChanged", env);
            CheckAndRegisterEvent("OnSleepStart", env);
            CheckAndRegisterEvent("OnSleepEnd", env);
            CheckAndRegisterEvent("OnPlayerMoneyChanged", env);
            CheckAndRegisterEvent("OnPlayerHealthChanged", env);
            CheckAndRegisterEvent("OnPlayerEnergyChanged", env);
            CheckAndRegisterEvent("OnItemAdded", env);
            CheckAndRegisterEvent("OnItemRemoved", env);
            CheckAndRegisterEvent("OnPlayerMovedSignificantly", env);
            CheckAndRegisterEvent("OnNPCInteraction", env);
            CheckAndRegisterEvent("OnSceneLoaded", env);
            CheckAndRegisterEvent("OnPlayerReady", env);
            CheckAndRegisterEvent("OnConsoleReady", env);
            CheckAndRegisterEvent("OnRegistryReady", env);
            CheckAndRegisterEvent("OnCurfewEnabled", env);
            CheckAndRegisterEvent("OnCurfewDisabled", env);
            CheckAndRegisterEvent("OnCurfewWarning", env);
            CheckAndRegisterEvent("OnCurfewHint", env);
        }

        /// <summary>
        /// Check for a function and register it as an event handler if it exists
        /// </summary>
        private void CheckAndRegisterEvent(string eventName, Table env)
        {
            DynValue handler = env.Get(eventName);
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
                DynValue function = _scriptEnvironment.Get(functionName);

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

        /// <summary>
        /// Gets the module export from this script if it exists
        /// </summary>
        public DynValue GetModuleExport()
        {
            if (_scriptEngine == null || _scriptEnvironment == null)
                return DynValue.Nil;

            try
            {
                string moduleName = _name;
                // First try to get from the script's own environment
                DynValue result = _scriptEnvironment.Get(moduleName + "_module");

                // If not found in script environment, check global (for backward compatibility)
                if (result.IsNil())
                {
                    result = _scriptEngine.Globals.Get(moduleName + "_module");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting module export for {_name}: {ex.Message}");
                return DynValue.Nil;
            }
        }

        /// <summary>
        /// Gets the script engine associated with this script
        /// </summary>
        public Script GetScriptEngine()
        {
            return _scriptEngine;
        }

        private void CheckForUpdateFunction(Table env)
        {
            DynValue updateFunction = env.Get("Update");
            _hasUpdateFunction = updateFunction != null && updateFunction.Type == DataType.Function;
        }
    }
}