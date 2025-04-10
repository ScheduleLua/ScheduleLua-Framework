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
using ScheduleLua.API.Player;
using ScheduleLua.API.Core;
using ScheduleLua.API.NPC;

namespace ScheduleLua
{
    /// <summary>
    /// Provides game functionality to Lua scripts
    /// </summary>
    public class LuaAPI
    {
        private static MelonLogger.Instance _logger => Core.Instance.LoggerInstance;

        /// <summary>
        /// Initializes API and registers it with the Lua interpreter
        /// </summary>
        public static void RegisterAPI(Script luaEngine)
        {
            if (luaEngine == null)
                throw new ArgumentNullException(nameof(luaEngine));

            // Register basic API functions
            luaEngine.Globals["Log"] = (Action<string>)Log;
            luaEngine.Globals["LogWarning"] = (Action<string>)LogWarning;
            luaEngine.Globals["LogError"] = (Action<string>)LogError;
            
            // Game object functions
            luaEngine.Globals["FindGameObject"] = (Func<string, GameObject>)FindGameObject;
            luaEngine.Globals["GetPosition"] = (Func<GameObject, Vector3>)GetPosition;
            luaEngine.Globals["SetPosition"] = (Action<GameObject, float, float, float>)SetPosition;
            
            // Player functions
            luaEngine.Globals["GetPlayer"] = (Func<Player>)GetPlayer;
            luaEngine.Globals["GetPlayerState"] = (Func<Table>)API.Player.PlayerAPI.GetPlayerState;
            luaEngine.Globals["GetPlayerPosition"] = (Func<Vector3>)GetPlayerPosition;
            luaEngine.Globals["GetPlayerRegion"] = (Func<string>)API.Player.PlayerAPI.GetPlayerRegion;
            luaEngine.Globals["SetPlayerPosition"] = (Action<float, float, float>)SetPlayerPosition;
            luaEngine.Globals["GetPlayerMoney"] = (Func<float>)GetPlayerMoney;
            luaEngine.Globals["AddPlayerMoney"] = (Action<float>)AddPlayerMoney;
            luaEngine.Globals["GetPlayerEnergy"] = (Func<float>)GetPlayerEnergy;
            luaEngine.Globals["SetPlayerEnergy"] = (Action<float>)SetPlayerEnergy;
            luaEngine.Globals["GetPlayerHealth"] = (Func<float>)GetPlayerHealth;
            luaEngine.Globals["SetPlayerHealth"] = (Action<float>)SetPlayerHealth;
            
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
            luaEngine.Globals["FindNPC"] = (Func<string, NPC>)FindNPC;
            luaEngine.Globals["GetNPCPosition"] = (Func<NPC, Vector3>)GetNPCPosition;
            luaEngine.Globals["SetNPCPosition"] = (Action<NPC, float, float, float>)SetNPCPosition;
            luaEngine.Globals["GetNPC"] = (Func<string, Table>)NPCAPI.GetNPC;
            luaEngine.Globals["GetNPCRegion"] = (Func<string, string>)NPCAPI.GetNPCRegion;
            luaEngine.Globals["GetNPCsInRegion"] = (Func<string, Table>)NPCAPI.GetNPCsInRegion;
            luaEngine.Globals["GetAllNPCs"] = (Func<Table>)NPCAPI.GetAllNPCs;
            luaEngine.Globals["GetAllNPCRegions"] = (Func<Table>)NPCAPI.GetAllNPCRegions;
            luaEngine.Globals["IsNPCInRegion"] = (Func<string, string, bool>)NPCAPI.IsNPCInRegion;
            
            // Map functions
            luaEngine.Globals["GetAllMapRegions"] = (Func<Table>)GetAllMapRegions;
            
            // Helper functions
            luaEngine.Globals["Vector3"] = (Func<float, float, float, Vector3>)CreateVector3;
            luaEngine.Globals["Vector3Distance"] = (Func<Vector3, Vector3, float>)Vector3.Distance;
            
            // Register common Unity types
            UserData.RegisterType<Vector3>();
            UserData.RegisterType<GameObject>();
            UserData.RegisterType<Transform>();
            
            // Register game-specific types
            UserData.RegisterType<Player>();
            UserData.RegisterType<NPC>();
            UserData.RegisterType<TimeManager>();
            UserData.RegisterType<ItemDefinition>();
            UserData.RegisterType<ItemSlot>();
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
        
        #region GameObject Functions
        
        public static GameObject FindGameObject(string name)
        {
            return GameObject.Find(name);
        }
        
        public static Vector3 GetPosition(GameObject gameObject)
        {
            if (gameObject == null)
                return Vector3.zero;
                
            return gameObject.transform.position;
        }
        
        public static void SetPosition(GameObject gameObject, float x, float y, float z)
        {
            if (gameObject == null)
                return;
                
            gameObject.transform.position = new Vector3(x, y, z);
        }
        
        #endregion

        #region Player Functions
        
        public static Player GetPlayer()
        {
            if (Player.Local == null)
            {
                Log("GetPlayer: Player.Local is null, ensure player is initialized");
            }
            return Player.Local;
        }
        
        public static Vector3 GetPlayerPosition()
        {
            Player player = Player.Local;
            if (player == null)
            {
                Log("GetPlayerPosition: Player.Local is null, returning zero vector");
                return Vector3.zero;
            }
                
            return player.transform.position;
        }
        
        public static void SetPlayerPosition(float x, float y, float z)
        {
            Player player = Player.Local;
            if (player == null)
            {
                Log("SetPlayerPosition: Player.Local is null, position not set");
                return;
            }
                
            player.transform.position = new Vector3(x, y, z);
        }
        
        public static float GetPlayerMoney()
        {
            // Access the player's money through the appropriate API
            // This implementation may need to be adjusted based on actual game API
            if (Player.Local == null)
            {
                Log("GetPlayerMoney: Player.Local is null, returning 0");
                return 0f;
            }
                
            // Assuming the Player class has a method to get money
            return 0f; // Placeholder
        }
        
        public static void AddPlayerMoney(float amount)
        {
            // Add money to the player through the appropriate API
            // This implementation may need to be adjusted based on actual game API
            if (Player.Local == null)
            {
                Log("AddPlayerMoney: Player.Local is null, money not added");
                return;
            }
                
            // Placeholder implementation
        }
        
        public static float GetPlayerEnergy()
        {
            Player player = Player.Local;
            if (player == null)
            {
                Log("GetPlayerEnergy: Player.Local is null, returning 0");
                return 0f;
            }
                
            if (player.Energy == null)
            {
                Log("GetPlayerEnergy: Player.Energy component is null, returning 0");
                return 0f;
            }
                
            return player.Energy.CurrentEnergy;
        }
        
        public static void SetPlayerEnergy(float amount)
        {
            Player player = Player.Local;
            if (player == null)
            {
                Log("SetPlayerEnergy: Player.Local is null, energy not set");
                return;
            }
                
            if (player.Energy == null)
            {
                Log("SetPlayerEnergy: Player.Energy component is null, energy not set");
                return;
            }
                
            player.Energy.SetEnergy(amount);
        }
        
        public static float GetPlayerHealth()
        {
            Player player = Player.Local;
            if (player == null)
            {
                Log("GetPlayerHealth: Player.Local is null, returning 0");
                return 0f;
            }
                
            if (player.Health == null)
            {
                Log("GetPlayerHealth: Player.Health component is null, returning 0");
                return 0f;
            }
                
            return player.Health.CurrentHealth;
        }
        
        public static void SetPlayerHealth(float amount)
        {
            Player player = Player.Local;
            if (player == null)
            {
                Log("SetPlayerHealth: Player.Local is null, health not set");
                return;
            }
                
            if (player.Health == null)
            {
                Log("SetPlayerHealth: Player.Health component is null, health not set");
                return;
            }
                
            player.Health.SetHealth(amount);
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
        
        #region NPC Functions
        
        public static NPC FindNPC(string npcName)
        {
            GameObject npcObject = GameObject.Find(npcName);
            if (npcObject == null)
                return null;
                
            return npcObject.GetComponent<NPC>();
        }
        
        public static Vector3 GetNPCPosition(NPC npc)
        {
            if (npc == null)
                return Vector3.zero;
                
            return npc.transform.position;
        }
        
        public static void SetNPCPosition(NPC npc, float x, float y, float z)
        {
            if (npc == null)
                return;
                
            npc.transform.position = new Vector3(x, y, z);
        }
        
        #endregion
        
        #region Map Functions
        
        public static Table GetAllMapRegions()
        {
            string[] regions = API.Core.LuaUtility.GetAllMapRegions();
            return API.Core.LuaUtility.StringArrayToTable(regions);
        }
        
        #endregion
        
        #region Helper Functions
        
        public static Vector3 CreateVector3(float x, float y, float z)
        {
            return new Vector3(x, y, z);
        }
        
        #endregion
    }
} 