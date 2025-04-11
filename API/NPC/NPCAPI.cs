using System;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using ScheduleOne;
using ScheduleOne.Map;
using ScheduleLua.API.Core;
using ScheduleOne.NPCs;

namespace ScheduleLua.API.NPC
{
    /// <summary>
    /// Provides Lua API access to NPC-related functionality in Schedule I
    /// </summary>
    public static class NPCAPI
    {
        private static ScheduleOne.NPCs.NPCManager _npcManager;
        private static ScheduleOne.NPCs.NPCManager NPCManager => _npcManager ??= ScheduleOne.NPCs.NPCManager.Instance;

        /// <summary>
        /// Finds an NPC by name
        /// </summary>
        /// <param name="npcName">The name of the NPC to find</param>
        /// <returns>The NPC component or null if not found</returns>
        public static ScheduleOne.NPCs.NPC FindNPC(string npcName)
        {
            try
            {
                GameObject npcObject = GameObject.Find(npcName);
                if (npcObject == null)
                {
                    LuaUtility.LogWarning($"NPC with name {npcName} not found");
                    return null;
                }
                
                return npcObject.GetComponent<ScheduleOne.NPCs.NPC>();
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error finding NPC {npcName}", ex);
                return null;
            }
        }
        
        /// <summary>
        /// Gets an NPC's position
        /// </summary>
        /// <param name="npc">The NPC to get the position of</param>
        /// <returns>The position vector of the NPC</returns>
        public static Vector3 GetNPCPosition(ScheduleOne.NPCs.NPC npc)
        {
            try
            {
                if (npc == null)
                {
                    LuaUtility.LogWarning("Cannot get position of null NPC");
                    return Vector3.zero;
                }
                
                return npc.transform.position;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error getting NPC position", ex);
                return Vector3.zero;
            }
        }
        
        /// <summary>
        /// Gets an NPC's position as Vector3Proxy for Lua compatibility
        /// </summary>
        /// <param name="npc">The NPC to get the position of</param>
        /// <returns>The position vector of the NPC as Vector3Proxy</returns>
        public static API.Core.Vector3Proxy GetNPCPositionProxy(ScheduleOne.NPCs.NPC npc)
        {
            try
            {
                if (npc == null)
                {
                    LuaUtility.LogWarning("Cannot get position of null NPC");
                    return API.Core.Vector3Proxy.zero;
                }
                
                return new API.Core.Vector3Proxy(npc.transform.position);
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error getting NPC position", ex);
                return API.Core.Vector3Proxy.zero;
            }
        }

        /// <summary>
        /// Sets an NPC's position
        /// </summary>
        /// <param name="npc">The NPC to set the position of</param>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z coordinate</param>
        public static void SetNPCPosition(ScheduleOne.NPCs.NPC npc, float x, float y, float z)
        {
            try
            {
                if (npc == null)
                {
                    LuaUtility.LogWarning("Cannot set position of null NPC");
                    return;
                }
                
                npc.transform.position = new Vector3(x, y, z);
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error setting NPC position", ex);
            }
        }

        /// <summary>
        /// Gets an NPC by their ID
        /// </summary>
        /// <param name="npcId">The ID of the NPC to find</param>
        /// <returns>Lua table containing NPC data or nil if not found</returns>
        public static Table GetNPC(string npcId)
        {
            try
            {
                var npc = NPCManager.GetNPC(npcId);
                if (npc == null)
                {
                    LuaUtility.LogWarning($"NPC with ID {npcId} not found");
                    return null;
                }

                var table = LuaUtility.CreateTable();
                
                table["id"] = npc.ID;
                table["fullName"] = npc.fullName;
                table["isConscious"] = npc.IsConscious;
                table["region"] = npc.Region.ToString();
                table["position"] = LuaUtility.Vector3ToTable(npc.transform.position);
                
                if (npc.Movement != null)
                {
                    table["isMoving"] = npc.Movement.IsMoving;
                }
                
                return table;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error getting NPC {npcId}", ex);
                return null;
            }
        }

        /// <summary>
        /// Gets an NPC's current region as a string
        /// </summary>
        /// <param name="npcId">The ID of the NPC</param>
        /// <returns>String representing the region or null if not found</returns>
        public static string GetNPCRegion(string npcId)
        {
            try
            {
                var npc = NPCManager.GetNPC(npcId);
                if (npc == null)
                {
                    LuaUtility.LogWarning($"NPC with ID {npcId} not found");
                    return null;
                }

                return npc.Region.ToString();
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error getting region for NPC {npcId}", ex);
                return null;
            }
        }

        /// <summary>
        /// Gets all NPCs in a specific region
        /// </summary>
        /// <param name="region">The region to search in</param>
        /// <returns>Lua table containing NPC data tables or empty table if none found</returns>
        public static Table GetNPCsInRegion(string region)
        {
            try
            {
                EMapRegion mapRegion;
                if (!System.Enum.TryParse(region, out mapRegion))
                {
                    LuaUtility.LogWarning($"Invalid region name: {region}");
                    return LuaUtility.CreateTable();
                }

                var npcs = NPCManager.GetNPCsInRegion(mapRegion);
                var result = LuaUtility.CreateTable();
                
                int index = 1;
                foreach (var npc in npcs)
                {
                    var npcTable = LuaUtility.CreateTable();
                    npcTable["id"] = npc.ID;
                    npcTable["fullName"] = npc.fullName;
                    npcTable["region"] = npc.Region.ToString();
                    result[index++] = npcTable;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error getting NPCs in region {region}", ex);
                return LuaUtility.CreateTable();
            }
        }

        /// <summary>
        /// Gets basic information about all NPCs
        /// </summary>
        /// <returns>Lua table containing NPC data tables</returns>
        public static Table GetAllNPCs()
        {
            try
            {
                var npcs = NPCManager.NPCRegistry;
                var result = LuaUtility.CreateTable();
                
                int index = 1;
                foreach (var npc in npcs)
                {
                    var npcTable = LuaUtility.CreateTable();
                    npcTable["id"] = npc.ID;
                    npcTable["fullName"] = npc.fullName;
                    npcTable["region"] = npc.Region.ToString();
                    result[index++] = npcTable;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error getting all NPCs", ex);
                return LuaUtility.CreateTable();
            }
        }

        /// <summary>
        /// Checks if an NPC is currently in a specific region
        /// </summary>
        /// <param name="npcId">The ID of the NPC</param>
        /// <param name="region">The region to check</param>
        /// <returns>true if the NPC is in the region, false otherwise</returns>
        public static bool IsNPCInRegion(string npcId, string region)
        {
            try
            {
                var npc = NPCManager.GetNPC(npcId);
                if (npc == null)
                {
                    LuaUtility.LogWarning($"NPC with ID {npcId} not found");
                    return false;
                }

                EMapRegion mapRegion;
                if (!System.Enum.TryParse(region, out mapRegion))
                {
                    LuaUtility.LogWarning($"Invalid region name: {region}");
                    return false;
                }

                return npc.Region == mapRegion;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error checking region for NPC {npcId}", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets a list of all distinct regions that NPCs are in
        /// </summary>
        /// <returns>Table containing unique region names</returns>
        public static Table GetAllNPCRegions()
        {
            try
            {
                var npcs = NPCManager.NPCRegistry;
                var regions = new List<string>();
                var result = LuaUtility.CreateTable();
                
                foreach (var npc in npcs)
                {
                    string regionName = npc.Region.ToString();
                    if (!regions.Contains(regionName))
                    {
                        regions.Add(regionName);
                    }
                }
                
                regions.Sort();
                
                int index = 1;
                foreach (var region in regions)
                {
                    result[index++] = region;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error getting NPC regions", ex);
                return LuaUtility.CreateTable();
            }
        }
    }
} 