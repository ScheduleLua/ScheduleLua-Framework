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