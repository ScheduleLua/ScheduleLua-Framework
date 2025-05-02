using MelonLoader;
using System;
using System.IO;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;
using ScheduleLua.API.Registry;
using ScheduleLua.Core.Framework; // Assuming this exists

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
        private readonly LuaScriptErrorHandler _errorHandler; // Added

        private DynValue _scriptInstance;
        private bool _isInitialized = false;
        private bool _hasUpdateFunction = false;
        private Table _scriptEnvironment;

        private Dictionary<string, DynValue> _eventHandlers = new Dictionary<string, DynValue>();
        private HashSet<string> _registeredCommands = new HashSet<string>();

        public string FilePath => _filePath;
        public string Name => _name;
        public bool IsLoaded => _scriptInstance != null;
        public bool IsInitialized => _isInitialized;
        public IReadOnlyCollection<string> RegisteredCommands => _registeredCommands;
        public Table ScriptEnvironment => _scriptEnvironment;

        public LuaScript(string filePath, Script scriptEngine, MelonLogger.Instance logger)
        {
            _filePath = filePath;
            _name = Path.GetFileNameWithoutExtension(filePath);
            _scriptEngine = scriptEngine;
            _logger = logger;

            // Instantiate the error handler
            _errorHandler = new LuaScriptErrorHandler(_logger, _filePath, _name); // Added

            if (_scriptEngine != null && !_scriptEngine.DebuggerEnabled)
            {
                _scriptEngine.DebuggerEnabled = true;
            }

            // Assuming CommandRegistry exists and is static or accessible
            CommandRegistry.RegisterScriptInstance(this);
        }

        public void AddRegisteredCommand(string commandName)
        {
            if (!string.IsNullOrEmpty(commandName))
            {
                _registeredCommands.Add(commandName);
            }
        }

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
                DynValue envTable = DynValue.NewTable(_scriptEngine);
                _scriptEnvironment = envTable.Table;

                Table globals = _scriptEngine.Globals;
                foreach (var pair in globals.Pairs)
                {
                    _scriptEnvironment[pair.Key] = pair.Value;
                }

                Table mt = new Table(_scriptEngine);
                mt["__index"] = _scriptEngine.Globals;
                _scriptEnvironment.MetaTable = mt;

                _scriptEnvironment["SCRIPT_PATH"] = _filePath;
                _scriptEnvironment["SCRIPT_NAME"] = _name;

                _scriptInstance = _scriptEngine.DoString(scriptContent, _scriptEnvironment, _name);

                if (!_scriptInstance.IsNil() && !_scriptInstance.IsVoid())
                {
                    _scriptEnvironment[_name + "_module"] = _scriptInstance;
                    _scriptEngine.Globals[_name + "_module"] = _scriptInstance;
                }

                CheckForUpdateFunction(_scriptEnvironment);
                RegisterEventHandlers(_scriptEnvironment);

                return true;
            }
            catch (InterpreterException luaEx)
            {
                // Use the error handler
                _errorHandler.LogDetailedError(luaEx, $"Error loading script"); // Changed
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error loading script {_name}: {ex.Message}\n{ex.StackTrace}"); // Keep generic error logging simple
                return false;
            }
        }

        // REMOVED LogDetailedError method from here

        public bool Initialize()
        {
            if (!IsLoaded) return false;
            if (_isInitialized)
            {
                // _logger.Msg($"Script {_name} already initialized, skipping."); // Keep logs concise
                return true;
            }

            string key = $"__initializing_{_name}";
            try
            {
                DynValue initializingFlag = _scriptEnvironment.Get(key);
                if (initializingFlag.Type != DataType.Nil && initializingFlag.Boolean)
                {
                    // _logger.Msg($"Recursive initialization detected for {_name}, skipping.");
                    return true;
                }

                _scriptEnvironment[key] = DynValue.True; // Use DynValue

                DynValue initFunction = _scriptEnvironment.Get("Initialize");
                if (initFunction.Type == DataType.Function)
                {
                    _scriptEngine.Call(initFunction);
                }
                _isInitialized = true; // Mark initialized even if no Initialize function

                _scriptEnvironment[key] = DynValue.False; // Use DynValue
                return true;
            }
            catch (InterpreterException luaEx)
            {
                try { _scriptEnvironment[key] = DynValue.False; } catch { }
                // Use the error handler
                _errorHandler.LogDetailedError(luaEx, $"Error initializing script"); // Changed
                return false;
            }
            catch (Exception ex)
            {
                try { _scriptEnvironment[key] = DynValue.False; } catch { }
                _logger.Error($"Error initializing script {_name}: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        public void Update()
        {
            if (!IsInitialized || !_hasUpdateFunction) return;

            try
            {
                // Optimization: Cache the update function? Maybe not needed if Get is fast.
                DynValue updateFunction = _scriptEnvironment.Get("Update");
                // Re-check just in case it got removed somehow (unlikely but safe)
                if (updateFunction.Type == DataType.Function)
                {
                    _scriptEngine.Call(updateFunction);
                }
                else
                {
                    _hasUpdateFunction = false; // Function disappeared
                }
            }
            catch (InterpreterException luaEx)
            {
                // Use the error handler
                _errorHandler.LogDetailedError(luaEx, $"Error in script Update"); // Changed
                _hasUpdateFunction = false; // Disable future calls on error
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in script {_name} Update: {ex.Message}\n{ex.StackTrace}");
                _hasUpdateFunction = false; // Disable future calls on error
            }
        }

        public bool Reload()
        {
            bool wasInitialized = _isInitialized;
            // Store current registered commands before clearing
            HashSet<string> previousCommands = new HashSet<string>(_registeredCommands);

            _logger.Msg($"Unregistering {_registeredCommands.Count} commands from script {_name} for reload.");
            foreach (var command in _registeredCommands)
            {
                CommandRegistry.UnregisterCommand(command); // Assuming static access
            }
            _registeredCommands.Clear(); // Clear our tracking

            _eventHandlers.Clear();
            _isInitialized = false;
            _hasUpdateFunction = false; // Reset this flag too
            _scriptInstance = null; // Clear previous instance
            _scriptEnvironment = null; // Clear previous environment

            if (!Load()) // Load creates a new environment
            {
                _logger.Error($"Failed to reload script {_name}. It will remain unloaded.");
                return false;
            }

            if (wasInitialized)
            {
                _logger.Msg($"Re-initializing script {_name} after reload.");
                bool initResult = Initialize();

                // Trigger OnConsoleReady *after* successful initialization
                // Scripts should re-register commands in their OnConsoleReady handler
                if (initResult && _eventHandlers.ContainsKey("OnConsoleReady"))
                {
                    _logger.Msg($"Triggering OnConsoleReady for script {_name} post-reload.");
                    TriggerEvent("OnConsoleReady");
                }
                else if (!initResult)
                {
                    _logger.Error($"Failed to re-initialize script {_name} after reload.");
                }

                return initResult;
            }

            _logger.Msg($"Script {_name} reloaded successfully (was not previously initialized).");
            return true;
        }

        public void TriggerEvent(string eventName, params object[] args)
        {
            // Check IsInitialized first for efficiency
            if (!IsInitialized || !_eventHandlers.TryGetValue(eventName, out DynValue handler))
                return;

            // Ensure it's still a function (paranoid check)
            if (handler.Type != DataType.Function)
            {
                _eventHandlers.Remove(eventName); // Clean up invalid handler
                return;
            }

            try
            {
                _scriptEngine.Call(handler, args);
            }
            catch (InterpreterException luaEx)
            {
                // Use the error handler
                _errorHandler.LogDetailedError(luaEx, $"Error in event handler '{eventName}'"); // Changed
                // Optionally disable the handler?
                // _eventHandlers.Remove(eventName);
                // _logger.Warning($"Disabled event handler '{eventName}' for script '{_name}' due to error.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in script {_name} event handler for {eventName}: {ex.Message}\n{ex.StackTrace}");
                // Optionally disable the handler?
            }
        }

        private void RegisterEventHandlers(Table env)
        {
            _eventHandlers.Clear(); // Start fresh

            // List of known event names
            var eventNames = new List<string> {
                "OnDayChanged", "OnTimeChanged", "OnSleepStart", "OnSleepEnd",
                "OnPlayerMoneyChanged", "OnPlayerHealthChanged", "OnPlayerEnergyChanged",
                "OnItemAdded", "OnItemRemoved", "OnPlayerMovedSignificantly",
                "OnNPCInteraction", "OnSceneLoaded", "OnPlayerReady", "OnConsoleReady",
                "OnRegistryReady", "OnCurfewEnabled", "OnCurfewDisabled",
                "OnCurfewWarning", "OnCurfewHint"
                // Add any other potential event names here
            };

            foreach (var eventName in eventNames)
            {
                CheckAndRegisterEvent(eventName, env);
            }
        }

        private void CheckAndRegisterEvent(string eventName, Table env)
        {
            DynValue handler = env.Get(eventName);
            // Check type explicitly for safety
            if (handler != null && handler.Type == DataType.Function)
            {
                _eventHandlers[eventName] = handler;
                // _logger.Msg($"Script {_name} registered handler for {eventName}"); // Maybe too verbose
            }
        }

        public DynValue CallFunction(string functionName, params object[] args)
        {
            if (!IsInitialized) return DynValue.Nil;

            try
            {
                DynValue function = _scriptEnvironment.Get(functionName);
                if (function != null && function.Type == DataType.Function)
                {
                    return _scriptEngine.Call(function, args);
                }
                // _logger.Warning($"Function '{functionName}' not found or not a function in script '{_name}'.");
                return DynValue.Nil; // Return nil if not found or not a function
            }
            catch (InterpreterException luaEx)
            {
                // Use the error handler
                _errorHandler.LogDetailedError(luaEx, $"Error calling function '{functionName}'"); // Changed
                return DynValue.Nil;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error calling function {functionName} in script {_name}: {ex.Message}\n{ex.StackTrace}");
                return DynValue.Nil;
            }
        }

        public DynValue GetModuleExport()
        {
            // Ensure environment exists before accessing
            if (_scriptEnvironment == null) return DynValue.Nil;

            try
            {
                // Prefer the environment-specific module key
                string moduleKey = _name + "_module";
                DynValue result = _scriptEnvironment.Get(moduleKey);

                // Fallback to global only if necessary (and log if it happens?)
                if (result.IsNilOrNan())
                {
                    result = _scriptEngine.Globals.Get(moduleKey);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting module export for {_name}: {ex.Message}\n{ex.StackTrace}");
                return DynValue.Nil;
            }
        }

        public Script GetScriptEngine()
        {
            return _scriptEngine;
        }

        private void CheckForUpdateFunction(Table env)
        {
            DynValue updateFunction = env.Get("Update");
            _hasUpdateFunction = (updateFunction != null && updateFunction.Type == DataType.Function);
        }
    }
}
