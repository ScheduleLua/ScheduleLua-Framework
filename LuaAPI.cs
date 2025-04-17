using MelonLoader;
using System;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using ScheduleOne.PlayerScripts;
using ScheduleOne.GameTime;
using ScheduleOne.ItemFramework;
using ScheduleOne.Management;
using ScheduleOne.NPCs;
using ScheduleLua.API.Core;
using ScheduleLua.API.Scene;
using ScheduleLua.API.NPC;
using ScheduleLua.API.Registry;
using ScheduleLua.API.Law;
using ScheduleLua.API.UI;
using ScheduleLua.API.Economy;
using ScheduleLua.API;
using System.Collections;
using System.IO;
using MelonLoader.Utils;
using ScheduleLua.API.Windows;
using ScheduleLua.API.Player;
using ScheduleLua.API.World;
using ScheduleLua.API.Mods;
using System.Linq;

namespace ScheduleLua
{
    /// <summary>
    /// Provides game functionality to Lua scripts
    /// </summary>
    public class LuaAPI
    {
        private static MelonLogger.Instance _logger => Core.Instance.LoggerInstance;
        
        // Store a reference to the Lua engine
        private static Script _luaEngine;

        // Dictionary to cache loaded modules
        private static Dictionary<string, DynValue> _loadedModules = new Dictionary<string, DynValue>();

        /// <summary>
        /// Initializes API and registers it with the Lua interpreter
        /// </summary>
        public static void RegisterAPI(Script luaEngine)
        {
            if (luaEngine == null)
                throw new ArgumentNullException(nameof(luaEngine));
                
            // Store the engine reference for later use
            _luaEngine = luaEngine;

            // Expose mod version to Lua
            luaEngine.Globals["SCHEDULELUA_VERSION"] = Core.ModVersion;
            luaEngine.Globals["GAME_VERSION"] = Application.version;

            // Register basic API functions
            luaEngine.Globals["Log"] = (Action<string>)Log;
            luaEngine.Globals["LogWarning"] = (Action<string>)LogWarning;
            luaEngine.Globals["LogError"] = (Action<string>)LogError;

            // Register the custom require function
            luaEngine.Globals["require"] = (Func<string, DynValue>)((moduleName) => RequireModule(luaEngine, moduleName));

            // Game object functions
            luaEngine.Globals["FindGameObject"] = (Func<string, GameObject>)FindGameObject;
            luaEngine.Globals["GetPosition"] = (Func<GameObject, Vector3Proxy>)GetPosition;
            luaEngine.Globals["SetPosition"] = (Action<GameObject, float, float, float>)SetPosition;

            // Map functions
            luaEngine.Globals["GetAllMapRegions"] = (Func<Table>)GetAllMapRegions;

            // Helper functions
            luaEngine.Globals["Vector3"] = (Func<float, float, float, Vector3Proxy>)CreateVector3;
            luaEngine.Globals["Vector3Distance"] = (Func<Vector3Proxy, Vector3Proxy, float>)Vector3Proxy.Distance;

            // Timing and coroutine functions
            luaEngine.Globals["Wait"] = (Action<float, DynValue>)Wait;
            luaEngine.Globals["Delay"] = (Action<float, DynValue>)Wait; // Alias for Wait

            // Register console command registry
            CommandRegistry.RegisterCommandAPI(luaEngine);

            // Register Law/Curfew API
            CurfewManagerAPI.RegisterAPI(luaEngine);

            // Register UI API
            UIAPI.RegisterAPI(luaEngine);

            // Register Economy API
            EconomyAPI.RegisterAPI(luaEngine);

            // Register Player API
            PlayerAPI.RegisterAPI(luaEngine);

            // Register Inventory API
            InventoryAPI.RegisterAPI(luaEngine);

            // Register Time API
            TimeAPI.RegisterAPI(luaEngine);

            // Register NPC API
            NPCAPI.RegisterAPI(luaEngine);

            // Register Registry API
            RegistryAPI.RegisterAPI(luaEngine);

            // Register Scene API
            SceneAPI.RegisterAPI(luaEngine);

            // Register Windows API
            WindowsAPI.RegisterAPI(luaEngine);

            // Note: Mods API is registered in Core.cs after mod manager is initialized

            // Use proxy objects instead of direct Unity type registration
            // This improves compatibility across platforms, especially on IL2CPP/AOT
            RegisterProxyTypes(luaEngine);

            // Register necessary types that can't be proxied easily
            // Make sure to test these thoroughly on target platforms
            UserData.RegisterType<Vector3Proxy>();

            // IMPORTANT: Don't directly register Unity types, use proxy methods instead

            // Set up hardwiring for IL2CPP and AOT compatibility
            // This pre-generates necessary conversion code
            Script.WarmUp();
        }

        /// <summary>
        /// Custom implementation of require function to load modules from loaded scripts
        /// </summary>
        private static DynValue RequireModule(Script luaEngine, string moduleName)
        {
            // Declare variables outside the try block so they're accessible in the finally block
            DynValue origModName = DynValue.Nil;
            DynValue origModPath = DynValue.Nil;
            DynValue origScriptPath = DynValue.Nil;
            Table origCallingEnv = null;

            try
            {
                // Save original script context
                origModName = luaEngine.Globals.Get("MOD_NAME");
                origModPath = luaEngine.Globals.Get("MOD_PATH");
                origScriptPath = luaEngine.Globals.Get("SCRIPT_PATH");

                // If the calling script has its own environment, get it
                var scriptInstance = FindScriptInstanceForCurrentExecution(luaEngine);
                if (scriptInstance != null)
                {
                    origCallingEnv = scriptInstance.ScriptEnvironment;
                }

                // Check if module is already loaded and cached
                if (_loadedModules.TryGetValue(moduleName, out DynValue cachedModule))
                {
                    return cachedModule;
                }

                // First look for modules in the current mod
                string currentModName = luaEngine.Globals.Get("MOD_NAME").String;
                string currentModPath = luaEngine.Globals.Get("MOD_PATH").String;

                if (!string.IsNullOrEmpty(currentModName) && !string.IsNullOrEmpty(currentModPath))
                {
                    string moduleFileName = moduleName + ".lua";
                    string modRelativeModulePath = Path.Combine(currentModPath, moduleFileName);

                    if (File.Exists(modRelativeModulePath))
                    {
                        // Update script context to point to this module
                        luaEngine.Globals["SCRIPT_PATH"] = modRelativeModulePath;

                        // Check if the module is already registered in globals
                        var existingModule = luaEngine.Globals.Get(moduleName + "_module");
                        if (!existingModule.IsNil())
                        {
                            _loadedModules[moduleName] = existingModule;
                            return existingModule;
                        }

                        // Load the module content
                        string content = File.ReadAllText(modRelativeModulePath);

                        // _logger.Msg($"Requiring module {moduleName} from mod {currentModName}");

                        // Create a separate environment for this module
                        DynValue envTable = DynValue.NewTable(luaEngine);
                        Table moduleEnv = envTable.Table;

                        // Copy globals from the main environment
                        Table globals = luaEngine.Globals;
                        foreach (var pair in globals.Pairs)
                        {
                            moduleEnv[pair.Key] = pair.Value;
                        }

                        // Set up metatable for environment
                        Table mt = new Table(luaEngine);
                        mt["__index"] = luaEngine.Globals;
                        moduleEnv.MetaTable = mt;

                        // Set module-specific context
                        moduleEnv["SCRIPT_PATH"] = modRelativeModulePath;
                        moduleEnv["SCRIPT_NAME"] = moduleName;
                        moduleEnv["MOD_NAME"] = currentModName;
                        moduleEnv["MOD_PATH"] = currentModPath;

                        // Execute the module code as a chunk that can return a value
                        DynValue result = luaEngine.DoString(content, moduleEnv, moduleName);

                        // If the script doesn't return anything, try to get any registered module
                        if (result.IsNil() || result.IsVoid())
                        {
                            result = moduleEnv.Get(moduleName + "_module");

                            // If still nil, return an empty table
                            if (result.IsNil())
                            {
                                result = DynValue.NewTable(luaEngine);
                            }
                        }

                        // Cache the result
                        _loadedModules[moduleName] = result;

                        // Also make it available in both global and module environments for compatibility
                        luaEngine.Globals[moduleName + "_module"] = result;
                        moduleEnv[moduleName + "_module"] = result;

                        // Restore original script context
                        luaEngine.Globals["MOD_NAME"] = origModName;
                        luaEngine.Globals["MOD_PATH"] = origModPath;
                        luaEngine.Globals["SCRIPT_PATH"] = origScriptPath;

                        return result;
                    }
                }

                // Check in already loaded scripts
                foreach (var scriptEntry in Core.Instance._loadedScripts)
                {
                    string scriptName = Path.GetFileNameWithoutExtension(scriptEntry.Value.Name);

                    if (string.Equals(scriptName, moduleName, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            // Update script context to point to this module
                            luaEngine.Globals["SCRIPT_PATH"] = scriptEntry.Value.FilePath;

                            if (luaEngine.Globals.Get(scriptName + "_module") != DynValue.Nil)
                            {
                                DynValue scriptResult = luaEngine.Globals.Get(scriptName + "_module");
                                _loadedModules[moduleName] = scriptResult;
                                return scriptResult;
                            }

                            string scriptPath = scriptEntry.Value.FilePath;
                            string content = File.ReadAllText(scriptPath);

                            // Create a separate environment for this module
                            DynValue envTable = DynValue.NewTable(luaEngine);
                            Table moduleEnv = envTable.Table;

                            // Copy globals from the main environment
                            Table globals = luaEngine.Globals;
                            foreach (var pair in globals.Pairs)
                            {
                                moduleEnv[pair.Key] = pair.Value;
                            }

                            // Set up metatable for environment
                            Table mt = new Table(luaEngine);
                            mt["__index"] = luaEngine.Globals;
                            moduleEnv.MetaTable = mt;

                            // Set module-specific context
                            moduleEnv["SCRIPT_PATH"] = scriptPath;
                            moduleEnv["SCRIPT_NAME"] = moduleName;

                            // Execute the module code with the isolated environment
                            DynValue result = luaEngine.DoString(content, moduleEnv, moduleName);

                            if (result.IsNil())
                            {
                                // Check if the module registered itself in its environment
                                result = moduleEnv.Get(moduleName + "_module");

                                // If still nil, create an empty table
                                if (result.IsNil())
                                {
                                    result = DynValue.NewTable(luaEngine);
                                }
                            }

                            // Cache the result
                            _loadedModules[moduleName] = result;

                            // Make the module available in global space for backward compatibility
                            luaEngine.Globals[moduleName + "_module"] = result;
                            moduleEnv[moduleName + "_module"] = result;

                            return result;
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Error loading module {moduleName} from script {scriptName}: {ex.Message}");
                            throw new ScriptRuntimeException($"Error loading module '{moduleName}' from script {scriptName}: {ex.Message}");
                        }
                    }
                }

                // If no matching loaded script, look for the file on disk
                string scriptsDirectory = Path.Combine(MelonEnvironment.ModsDirectory, "ScheduleLua", "Scripts");

                // Look for .lua files that match the module name
                foreach (string filePath in Directory.GetFiles(scriptsDirectory, "*.lua", SearchOption.AllDirectories))
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);

                    if (string.Equals(fileName, moduleName, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            // Update script context to point to this module
                            luaEngine.Globals["SCRIPT_PATH"] = filePath;

                            // Load the module content
                            string content = File.ReadAllText(filePath);

                            // Create a separate environment for this module
                            DynValue envTable = DynValue.NewTable(luaEngine);
                            Table moduleEnv = envTable.Table;

                            // Copy globals from the main environment
                            Table globals = luaEngine.Globals;
                            foreach (var pair in globals.Pairs)
                            {
                                moduleEnv[pair.Key] = pair.Value;
                            }

                            // Set up metatable for environment
                            Table mt = new Table(luaEngine);
                            mt["__index"] = luaEngine.Globals;
                            moduleEnv.MetaTable = mt;

                            // Set module-specific context
                            moduleEnv["SCRIPT_PATH"] = filePath;
                            moduleEnv["SCRIPT_NAME"] = moduleName;

                            // Execute the module code with the isolated environment
                            DynValue result = luaEngine.DoString(content, moduleEnv, moduleName);

                            // If the script doesn't return anything, check for module in environment
                            if (result.IsNil())
                            {
                                result = moduleEnv.Get(moduleName + "_module");

                                // If still nil, create an empty table
                                if (result.IsNil())
                                {
                                    result = DynValue.NewTable(luaEngine);
                                }
                            }

                            // Cache the result
                            _loadedModules[moduleName] = result;

                            // Make available in both environments for compatibility
                            luaEngine.Globals[moduleName + "_module"] = result;
                            moduleEnv[moduleName + "_module"] = result;

                            return result;
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Error loading module {moduleName}: {ex.Message}");
                            throw new ScriptRuntimeException($"Error loading module '{moduleName}': {ex.Message}");
                        }
                    }
                }

                // Module not found
                _logger.Error($"Module not found: {moduleName}" + (currentModName != null ? $" in mod {currentModName}" : ""));
                throw new ScriptRuntimeException($"Module '{moduleName}' not found");
            }
            finally
            {
                // Restore original script context
                luaEngine.Globals["MOD_NAME"] = origModName;
                luaEngine.Globals["MOD_PATH"] = origModPath;
                luaEngine.Globals["SCRIPT_PATH"] = origScriptPath;
            }
        }

        #region Logging Functions

        public static void Log(string message)
        {
            _logger.Msg($"[Lua] {message}");
        }

        public static void LogWarning(string message)
        {
            _logger.Warning($"[Lua] {message}");
        }

        public static void LogError(string message)
        {
            // Forward to the enhanced error logging method in LuaUtility
            // This will get the current exception if we're in an error handler
            try {
                var currentError = _luaEngine.GetDebugger().GetErrorSource();
                if (currentError != null && currentError is MoonSharp.Interpreter.InterpreterException luaEx)
                {
                    API.Core.LuaUtility.LogError(message, luaEx);
                    return;
                }
            } catch {
                // Ignore any errors trying to get the current exception
            }
            
            // Fall back to basic error logging
            _logger.Error($"[Lua] {message}");
        }

        #endregion

        #region Timing and Coroutine Functions

        /// <summary>
        /// Executes a Lua function after a specified delay
        /// </summary>
        /// <param name="seconds">Delay in seconds</param>
        /// <param name="callback">Lua function to call after the delay</param>
        public static void Wait(float seconds, DynValue callback)
        {
            if (callback == null || callback.Type != DataType.Function)
            {
                LogWarning("Wait: callback is not a function");
                return;
            }

            if (seconds < 0)
                seconds = 0;

            // Use MelonCoroutines instead of MonoBehaviour for running coroutines
            MelonLoader.MelonCoroutines.Start(WaitCoroutine(seconds, callback));
        }

        private static IEnumerator WaitCoroutine(float seconds, DynValue callback)
        {
            yield return new WaitForSeconds(seconds);

            try
            {
                var script = Core.Instance._luaEngine;
                script.Call(callback);
            }
            catch (Exception ex)
            {
                LogError($"Error in Wait callback: {ex.Message}");
            }
        }

        #endregion

        #region GameObject Functions

        public static GameObject FindGameObject(string name)
        {
            return GameObject.Find(name);
        }

        public static Vector3Proxy GetPosition(GameObject gameObject)
        {
            if (gameObject == null)
                return Vector3Proxy.zero;

            return new Vector3Proxy(gameObject.transform.position);
        }

        public static void SetPosition(GameObject gameObject, float x, float y, float z)
        {
            if (gameObject == null)
                return;

            gameObject.transform.position = new Vector3(x, y, z);
        }

        #endregion

        #region Map Functions

        public static Table GetAllMapRegions()
        {
            string[] regions = LuaUtility.GetAllMapRegions();
            return LuaUtility.StringArrayToTable(regions);
        }

        #endregion

        #region Helper Functions

        public static Vector3Proxy CreateVector3(float x, float y, float z)
        {
            return new Vector3Proxy(x, y, z);
        }

        #endregion

        /// <summary>
        /// Registers proxy classes instead of direct Unity types for better compatibility
        /// </summary>
        private static void RegisterProxyTypes(Script luaEngine)
        {
            // Register proxy classes
            luaEngine.Globals["CreateGameObject"] = (Func<string, GameObject>)(name => new GameObject(name));

            // GameObject proxy methods (instead of direct GameObject registration)
            luaEngine.Globals["GetGameObjectName"] = (Func<GameObject, string>)(go => go?.name ?? string.Empty);
            luaEngine.Globals["SetGameObjectName"] = (Action<GameObject, string>)((go, name) => { if (go != null) go.name = name; });
            luaEngine.Globals["SetGameObjectActive"] = (Action<GameObject, bool>)((go, active) => { if (go != null) go.SetActive(active); });
            luaEngine.Globals["IsGameObjectActive"] = (Func<GameObject, bool>)(go => go != null && go.activeSelf);

            // Transform proxy methods (instead of direct Transform registration)
            luaEngine.Globals["GetTransform"] = (Func<GameObject, Transform>)(go => go?.transform);

            // Fix: Return Vector3Proxy instead of Vector3
            luaEngine.Globals["GetTransformPosition"] = (Func<Transform, Vector3Proxy>)(t =>
                t != null ? new Vector3Proxy(t.position) : Vector3Proxy.zero);

            luaEngine.Globals["SetTransformPosition"] = (Action<Transform, Vector3Proxy>)((t, pos) =>
            { if (t != null) t.position = pos; });

            luaEngine.Globals["GetTransformRotation"] = (Func<Transform, Vector3Proxy>)(t =>
                t != null ? new Vector3Proxy(t.eulerAngles) : Vector3Proxy.zero);

            luaEngine.Globals["SetTransformRotation"] = (Action<Transform, Vector3Proxy>)((t, rot) =>
            { if (t != null) t.eulerAngles = rot; });

            // Add additional proxy methods for any Unity types you need to expose

            // Add more proxy registration here as needed
        }

        /// <summary>
        /// Find the LuaScript instance that is currently executing
        /// This is useful for getting the script's private environment
        /// </summary>
        private static LuaScript FindScriptInstanceForCurrentExecution(Script luaEngine)
        {
            try
            {
                // Try to find by SCRIPT_PATH if available
                DynValue scriptPath = luaEngine.Globals.Get("SCRIPT_PATH");
                if (scriptPath.Type != DataType.Nil && !string.IsNullOrEmpty(scriptPath.String))
                {
                    var script = Core.Instance._loadedScripts.Values
                        .FirstOrDefault(s => s.FilePath == scriptPath.String);

                    if (script != null)
                        return script;
                }

                // Try to find by current call stack (more complex but more reliable)
                // This would require deeper MoonSharp integration

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}