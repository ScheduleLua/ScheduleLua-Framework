using MelonLoader;
using MelonLoader.Utils;
using MoonSharp.Interpreter;
using ScheduleLua.API.Core;
using ScheduleLua.API.Core.TypeProxies;
using ScheduleLua.API.Economy;
using ScheduleLua.API.Law;
using ScheduleLua.API.NPC;
using ScheduleLua.API.Player;
using ScheduleLua.API.Registry;
using ScheduleLua.API.Scene;
using ScheduleLua.API.UI;
using ScheduleLua.API.World;
using ScheduleLua.Core.Framework;
using System.Collections;
using UnityEngine;

namespace ScheduleLua
{
    /// <summary>
    /// Provides game functionality to Lua scripts
    /// </summary>
    public class LuaAPI
    {
        private static MelonLogger.Instance _logger => ModCore.Instance.LoggerInstance;
        private static ApiRegistry _apiRegistry;

        // Dictionary to cache loaded modules
        private static Dictionary<string, DynValue> _loadedModules = new Dictionary<string, DynValue>();

        /// <summary>
        /// Get the PlayerApiModule instance from the registry
        /// </summary>
        /// <returns>PlayerApiModule instance or null if not registered</returns>
        public static PlayerApiModule GetPlayerApiModule()
        {
            return _apiRegistry?.GetModule<PlayerApiModule>();
        }

        /// <summary>
        /// Initializes API and registers it with the Lua interpreter
        /// </summary>
        public static void RegisterAPI(Script luaEngine)
        {
            if (luaEngine == null)
                throw new ArgumentNullException(nameof(luaEngine));

            // Expose mod version to Lua
            luaEngine.Globals["SCHEDULELUA_VERSION"] = ModCore.ModVersion;
            luaEngine.Globals["GAME_VERSION"] = Application.version;

            // Create the API registry
            _apiRegistry = new ApiRegistry(luaEngine);

            // Register basic API functions
            luaEngine.Globals["Log"] = (Action<string>)Log;
            luaEngine.Globals["LogWarning"] = (Action<string>)LogWarning;
            luaEngine.Globals["LogError"] = (Action<string>)LogError;

            // Register the custom require function
            luaEngine.Globals["require"] = (Func<string, DynValue>)((moduleName) => RequireModule(luaEngine, moduleName));

            // Register basic functions and utilities (these don't need to be in modules)
            RegisterBasicFunctions(luaEngine);

            // Register all API modules
            RegisterApiModules(luaEngine);

            // Initialize all modules
            _apiRegistry.InitializeAll();
        }

        /// <summary>
        /// Register basic utility functions that don't belong in a specific module
        /// </summary>
        private static void RegisterBasicFunctions(Script luaEngine)
        {
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

            // Use proxy objects instead of direct Unity type registration
            // This improves compatibility across platforms, especially on IL2CPP/AOT
            RegisterProxyTypes(luaEngine);
        }

        /// <summary>
        /// Register all API modules with the registry
        /// </summary>
        private static void RegisterApiModules(Script luaEngine)
        {
            // New API module system
            _apiRegistry.RegisterModule(new PlayerApiModule());
            _apiRegistry.RegisterModule(new InventoryApiModule());
            _apiRegistry.RegisterModule(new NPCApiModule());
            _apiRegistry.RegisterModule(new RegistryAPI());
            _apiRegistry.RegisterModule(new CommandRegistry());
            _apiRegistry.RegisterModule(new CurfewManagerAPI());
            _apiRegistry.RegisterModule(new LawAPI());
            _apiRegistry.RegisterModule(new UIAPI());
            _apiRegistry.RegisterModule(new EconomyAPI());
            _apiRegistry.RegisterModule(new TimeAPI());
            _apiRegistry.RegisterModule(new SceneAPI());
            _apiRegistry.RegisterModule(new UnityAPI());
            _apiRegistry.RegisterModule(new ExplosionAPI());

            // Deprecated in favor of Unity API
            // _apiRegistry.RegisterModule(new WindowsAPI());

            // Temporary: Call legacy API registration for modules not yet converted
            // These will be removed as modules are converted to the new format
        }

        /// <summary>
        /// Shuts down all registered API modules
        /// </summary>
        public static void ShutdownAPI()
        {
            _apiRegistry?.ShutdownAll();
        }

        /// <summary>
        /// Custom implementation of require function to load modules from loaded scripts
        /// </summary>
        private static DynValue RequireModule(Script luaEngine, string moduleName)
        {
            // Declare variables outside the try block so they're accessible in the finally block
            DynValue origModName = DynValue.Nil;
            DynValue origModPath = DynValue.Nil;
            DynValue origModVersion = DynValue.Nil;
            DynValue origScriptPath = DynValue.Nil;
            Table origCallingEnv = null;

            try
            {
                // Save original script context
                origModName = luaEngine.Globals.Get("MOD_NAME");
                origModPath = luaEngine.Globals.Get("MOD_PATH");
                origModVersion = luaEngine.Globals.Get("MOD_VERSION");
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
                        moduleEnv["MOD_VERSION"] = origModVersion;

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
                        luaEngine.Globals["MOD_VERSION"] = origModVersion;
                        luaEngine.Globals["SCRIPT_PATH"] = origScriptPath;

                        return result;
                    }
                }

                // Check in already loaded scripts
                foreach (var scriptEntry in ModCore.Instance._loadedScripts)
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
                luaEngine.Globals["MOD_VERSION"] = origModVersion;
                luaEngine.Globals["SCRIPT_PATH"] = origScriptPath;
            }
        }

        #region Logging Functions
        public static void Log(string message) => LuaUtility.Log($"[Lua] {message}");
        public static void LogWarning(string message) => LuaUtility.LogWarning($"[Lua] {message}");
        public static void LogError(string message) => LuaUtility.LogError($"[Lua] {message}");
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

            MelonCoroutines.Start(WaitCoroutine(seconds, callback));
        }

        private static IEnumerator WaitCoroutine(float seconds, DynValue callback)
        {
            yield return new WaitForSeconds(seconds);

            try
            {
                var script = ModCore.Instance._luaEngine;
                script.Call(callback);
            }
            catch (Exception ex)
            {
                LogError($"Error in Wait callback: {ex.Message}");
            }
        }

        #endregion

        #region GameObject Functions
        public static GameObject FindGameObject(string name) => GameObject.Find(name);

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
            // TODO: Add more proxy registration here as needed
        }

        /// <summary>
        /// Find the LuaScript instance that is currently executing
        /// This is useful for getting the script's private environment
        /// </summary>
        private static LuaScript FindScriptInstanceForCurrentExecution(Script luaEngine)
        {
            try
            {
                DynValue scriptPath = luaEngine.Globals.Get("SCRIPT_PATH");
                if (scriptPath.Type != DataType.Nil && !string.IsNullOrEmpty(scriptPath.String))
                {
                    var script = ModCore.Instance._loadedScripts.Values
                        .FirstOrDefault(s => s.FilePath == scriptPath.String);

                    if (script != null)
                        return script;
                }

                // TODO: Improve if needed

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}