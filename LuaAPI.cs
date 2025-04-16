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

namespace ScheduleLua
{
    /// <summary>
    /// Provides game functionality to Lua scripts
    /// </summary>
    public class LuaAPI
    {
        private static MelonLogger.Instance _logger => Core.Instance.LoggerInstance;
        
        // Dictionary to cache loaded modules
        private static Dictionary<string, DynValue> _loadedModules = new Dictionary<string, DynValue>();

        /// <summary>
        /// Initializes API and registers it with the Lua interpreter
        /// </summary>
        public static void RegisterAPI(Script luaEngine)
        {
            if (luaEngine == null)
                throw new ArgumentNullException(nameof(luaEngine));

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
            
            // Player functions
            luaEngine.Globals["GetPlayer"] = (Func<Player>)PlayerAPI.GetPlayer;
            luaEngine.Globals["GetPlayerState"] = (Func<Table>)PlayerAPI.GetPlayerState;
            luaEngine.Globals["GetPlayerPosition"] = (Func<Vector3Proxy>)PlayerAPI.GetPlayerPositionProxy;
            luaEngine.Globals["GetPlayerRegion"] = (Func<string>)PlayerAPI.GetPlayerRegion;
            luaEngine.Globals["SetPlayerPosition"] = (Action<float, float, float>)PlayerAPI.SetPlayerPosition;
            luaEngine.Globals["GetPlayerEnergy"] = (Func<float>)PlayerAPI.GetPlayerEnergy;
            luaEngine.Globals["SetPlayerEnergy"] = (Action<float>)PlayerAPI.SetPlayerEnergy;
            luaEngine.Globals["GetPlayerHealth"] = (Func<float>)PlayerAPI.GetPlayerHealth;
            luaEngine.Globals["SetPlayerHealth"] = (Action<float>)PlayerAPI.SetPlayerHealth;
            luaEngine.Globals["GetPlayerMovementSpeed"] = (Func<float>)PlayerAPI.GetMovementSpeed;
            luaEngine.Globals["SetPlayerMovementSpeed"] = (Func<float, bool>)PlayerAPI.SetMovementSpeed;

            // Inventory functions
            luaEngine.Globals["GetInventorySlotCount"] = (Func<int>)GetInventorySlotCount;
            luaEngine.Globals["GetInventoryItemAt"] = (Func<int, string>)GetInventoryItemAt;
            luaEngine.Globals["AddItemToInventory"] = (Func<string, int, bool>)AddItemToInventory;
            luaEngine.Globals["RemoveItemFromInventory"] = (Func<string, int, bool>)RemoveItemFromInventory;
            
            // Time functions
            luaEngine.Globals["GetGameTime"] = (Func<int>)GetGameTime;
            luaEngine.Globals["GetGameDay"] = (Func<string>)GetGameDay;
            luaEngine.Globals["GetGameDayInt"] = (Func<int>)GetGameDayInt;
            luaEngine.Globals["IsNightTime"] = (Func<bool>)IsNightTime;
            luaEngine.Globals["FormatGameTime"] = (Func<int, string>)FormatGameTime;
            
            // NPC functions
            luaEngine.Globals["GetNPCPosition"] = (Func<NPC, Vector3Proxy>)NPCAPI.GetNPCPositionProxy;
            luaEngine.Globals["SetNPCPosition"] = (Action<NPC, float, float, float>)NPCAPI.SetNPCPosition;
            luaEngine.Globals["GetNPCState"] = (Func<string, Table>)NPCAPI.GetNPCState;
            luaEngine.Globals["GetNPC"] = (Func<string, NPC>)NPCAPI.GetNPC;
            luaEngine.Globals["GetNPCRegion"] = (Func<string, string>)NPCAPI.GetNPCRegion;
            luaEngine.Globals["GetNPCsInRegion"] = (Func<string, Table>)NPCAPI.GetNPCsInRegion;
            luaEngine.Globals["GetAllNPCs"] = (Func<Table>)NPCAPI.GetAllNPCs;
            luaEngine.Globals["GetAllNPCRegions"] = (Func<Table>)NPCAPI.GetAllNPCRegions;
            luaEngine.Globals["IsNPCInRegion"] = (Func<string, string, bool>)NPCAPI.IsNPCInRegion;
            
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

            // Register Registry API
            RegistryAPI.RegisterAPI(luaEngine);

            // Register Scene API
            SceneAPI.RegisterAPI(luaEngine);

            // Register Windows API
            WindowsAPI.RegisterAPI(luaEngine);

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
            _logger.Msg($"Requiring module: {moduleName}");
            
            // Check if the module is already loaded (caching)
            if (_loadedModules.TryGetValue(moduleName, out DynValue cachedModule))
            {
                _logger.Msg($"Returning cached module: {moduleName}");
                return cachedModule;
            }
            
            foreach (var scriptEntry in Core.Instance._loadedScripts)
            {
                string scriptName = Path.GetFileNameWithoutExtension(scriptEntry.Value.Name);
                
                if (string.Equals(scriptName, moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        if (luaEngine.Globals.Get(scriptName + "_module") != DynValue.Nil)
                        {
                            DynValue scriptResult = luaEngine.Globals.Get(scriptName + "_module");
                            _loadedModules[moduleName] = scriptResult;
                            _logger.Msg($"Requiring already loaded module: {moduleName}");
                            return scriptResult;
                        }
                        
                        string scriptPath = scriptEntry.Value.FilePath;
                        string content = File.ReadAllText(scriptPath);
                        
                        DynValue result = luaEngine.DoString(content, null, moduleName);

                        if (result.IsNil())
                        {
                            result = DynValue.NewTable(luaEngine);
                        }
                        
                        _loadedModules[moduleName] = result;
                        
                        _logger.Msg($"Successfully loaded module from script: {scriptName}");
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
                        // Load the module content
                        string content = File.ReadAllText(filePath);
                        
                        // Execute the module code as a chunk that can return a value
                        DynValue result = luaEngine.DoString(content, null, moduleName);
                        
                        // If the script doesn't return anything, create an empty table
                        if (result.IsNil())
                        {
                            result = DynValue.NewTable(luaEngine);
                        }
                        
                        // Cache the result
                        _loadedModules[moduleName] = result;
                        
                        _logger.Msg($"Successfully loaded module from file: {fileName}");
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
            _logger.Error($"Module not found: {moduleName}");
            throw new ScriptRuntimeException($"Module '{moduleName}' not found");
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

        #region Inventory Functions
        
        public static int GetInventorySlotCount()
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory == null)
                return 0;
                
            return inventory.TOTAL_SLOT_COUNT;
        }
        
        public static string GetInventoryItemAt(int slotIndex)
        {
            Player player = Player.Local;
            if (player == null || player.Inventory == null || slotIndex < 0 || slotIndex >= player.Inventory.Length)
                return string.Empty;
                
            ItemSlot slot = player.Inventory[slotIndex];
            if (slot == null || slot.ItemInstance == null)
                return string.Empty;
                
            return slot.ItemInstance.Name ?? string.Empty;
        }
        
        public static bool AddItemToInventory(string itemName, int amount = 1)
        {
            // This is a simplified implementation - would need to be expanded
            // to find the item definition by name and add it properly
            Log("AddItemToInventory not fully implemented yet");
            return false;
        }
        
        public static bool RemoveItemFromInventory(string itemName, int amount = 1)
        {
            // This is a simplified implementation - would need to be expanded
            Log("RemoveItemFromInventory not fully implemented yet");
            return false;
        }
        
        #endregion
        
        #region Time Functions
        
        public static int GetGameTime()
        {
            TimeManager time = TimeManager.Instance;
            if (time == null)
                return 0;
                
            return time.CurrentTime;
        }
        
        public static string GetGameDay()
        {
            TimeManager time = TimeManager.Instance;
            if (time == null)
                return "Monday";
                
            return time.CurrentDay.ToString();
        }
        
        public static int GetGameDayInt()
        {
            TimeManager time = TimeManager.Instance;
            if (time == null)
                return 0;
                
            return (int)time.CurrentDay;
        }
        
        public static bool IsNightTime()
        {
            TimeManager time = TimeManager.Instance;
            if (time == null)
                return false;
                
            return time.IsNight;
        }
        
        public static string FormatGameTime(int timeValue)
        {
            if (TimeManager.Instance == null)
                return "00:00";
            
            // Using static method instead of instance method
            return TimeManager.Get12HourTime(timeValue);
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
    }
} 