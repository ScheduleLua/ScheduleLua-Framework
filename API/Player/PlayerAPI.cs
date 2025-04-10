using System;
using UnityEngine;
using MoonSharp.Interpreter;
using ScheduleOne;
using ScheduleLua.API.Core;
using ScheduleOne.PlayerScripts;
using ScheduleOne.PlayerScripts.Health;
using System.Linq;
using System.Collections.Generic;

namespace ScheduleLua.API.Player
{
    /// <summary>
    /// Provides Lua API access to player-related functionality in Schedule I
    /// </summary>
    public static class PlayerAPI
    {
        private static GameObject _player;
        private static GameObject Player => _player ??= (ScheduleOne.PlayerScripts.Player.Local != null ? ScheduleOne.PlayerScripts.Player.Local.gameObject : GameObject.FindGameObjectWithTag("Player"));

        private static ScheduleOne.PlayerScripts.Player _playerInstance;
        private static ScheduleOne.PlayerScripts.Player PlayerInstance => _playerInstance ??= ScheduleOne.PlayerScripts.Player.Local ?? Player?.GetComponent<ScheduleOne.PlayerScripts.Player>();

        private static PlayerHealth _playerHealth;
        private static PlayerHealth PlayerHealth => _playerHealth ??= Player?.GetComponent<PlayerHealth>();

        private static PlayerMovement _playerMovement;
        private static PlayerMovement PlayerMovement => _playerMovement ??= Player?.GetComponent<PlayerMovement>();

        /// <summary>
        /// Gets the player's current state and information
        /// </summary>
        /// <returns>Lua table containing player data</returns>
        public static Table GetPlayerState()
        {
            try
            {
                var player = Player;
                if (player == null)
                {
                    LuaUtility.LogWarning("Player not found");
                    return null;
                }

                var table = LuaUtility.CreateTable();

                table["position"] = LuaUtility.Vector3ToTable(player.transform.position);

                var playerInstance = PlayerInstance;
                if (playerInstance != null)
                {
                    table["playerName"] = playerInstance.PlayerName;
                    table["isRagdolled"] = playerInstance.IsRagdolled;
                    table["isCrouched"] = playerInstance.Crouched;
                }

                if (PlayerHealth != null)
                {
                    table["health"] = PlayerHealth.CurrentHealth;
                    table["maxHealth"] = PlayerHealth.MAX_HEALTH;
                    table["isAlive"] = PlayerHealth.IsAlive;
                }

                // Add movement info if available
                if (PlayerMovement != null)
                {
                    table["isGrounded"] = PlayerMovement.IsGrounded;
                    table["isSprinting"] = PlayerMovement.isSprinting;
                    table["moveSpeed"] = PlayerMovement.MoveSpeedMultiplier;
                }

                return table;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error getting player state", ex);
                return null;
            }
        }

        /// <summary>
        /// Gets the player's current position
        /// </summary>
        /// <returns>Lua table containing x, y, z coordinates</returns>
        public static Table GetPlayerPosition()
        {
            try
            {
                var player = Player;
                if (player == null)
                {
                    LuaUtility.LogWarning("Player not found");
                    return null;
                }

                return LuaUtility.Vector3ToTable(player.transform.position);
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error getting player position", ex);
                return null;
            }
        }

        /// <summary>
        /// Sets the player's health if the PlayerHealth component is available
        /// </summary>
        /// <param name="health">The new health value</param>
        /// <returns>true if successful, false otherwise</returns>
        public static bool SetHealth(float health)
        {
            try
            {
                var playerHealth = PlayerHealth;
                if (playerHealth == null)
                {
                    LuaUtility.LogWarning("Player health component not found");
                    return false;
                }

                if (health < 0 || health > PlayerHealth.MAX_HEALTH)
                {
                    LuaUtility.LogWarning($"Health must be between 0 and {PlayerHealth.MAX_HEALTH}");
                    return false;
                }

                playerHealth.SetHealth(health);
                return true;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error setting health", ex);
                return false;
            }
        }

        /// <summary>
        /// Teleports the player to a specific position
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z coordinate</param>
        /// <returns>true if successful, false otherwise</returns>
        public static bool TeleportPlayer(float x, float y, float z)
        {
            try
            {
                var playerMovement = PlayerMovement;
                if (playerMovement == null)
                {
                    LuaUtility.LogWarning("Player movement component not found");
                    return false;
                }

                playerMovement.Teleport(new Vector3(x, y, z));
                return true;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error teleporting player", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets the player's current region 
        /// </summary>
        /// <returns>string representing the region or null if not found</returns>
        public static string GetPlayerRegion()
        {
            try
            {
                var player = PlayerInstance;
                if (player == null)
                {
                    LuaUtility.LogWarning("Player not found");
                    return null;
                }

                // Try to get player position and use Map to determine the region
                Vector3 playerPosition = player.transform.position;
                
                // Use the Map singleton to find the region the player is in
                var map = ScheduleOne.Map.Map.Instance;
                if (map != null && map.Regions != null)
                {
                    // Find which region contains the player's position
                    
                    // Check if the Player is in an NPCPoI's area which would give us a region
                    var npcPois = GameObject.FindObjectsOfType<ScheduleOne.Map.NPCPoI>();
                    foreach (var npcPoi in npcPois)
                    {
                        if (npcPoi.NPC != null)
                        {
                            // Use the NPC's region if the player is close to the NPC
                            float distanceToNPC = Vector3.Distance(playerPosition, npcPoi.transform.position);
                            if (distanceToNPC < 30f) // Adjust threshold as needed
                            {
                                return npcPoi.NPC.Region.ToString();
                            }
                        }
                    }
                    
                    // Fallback approach - try to determine region from map data
                    float closestDistance = float.MaxValue;
                    ScheduleOne.Map.EMapRegion closestRegion = ScheduleOne.Map.EMapRegion.Downtown; // Default region
                    
                    // Use specific known POIs to determine regions
                    var allPois = GameObject.FindObjectsOfType<ScheduleOne.Map.POI>();
                    
                    // Group POIs by their transform parent name, which might give us region information
                    var poiGroups = new Dictionary<string, List<ScheduleOne.Map.POI>>();
                    
                    foreach (var poi in allPois)
                    {
                        // Skip null transforms or those without parents
                        if (poi.transform == null || poi.transform.parent == null)
                            continue;
                            
                        string parentName = poi.transform.parent.name;
                        if (!poiGroups.ContainsKey(parentName))
                            poiGroups[parentName] = new List<ScheduleOne.Map.POI>();
                            
                        poiGroups[parentName].Add(poi);
                    }
                    
                    // For each region in the map
                    foreach (var regionData in map.Regions)
                    {
                        // Try to find POIs that match this region's name in their parent hierarchy
                        string regionNameLower = regionData.Name.ToLower();
                        
                        // Look through our grouped POIs to find those that might match this region
                        foreach (var group in poiGroups)
                        {
                            if (group.Key.ToLower().Contains(regionNameLower))
                            {
                                // Calculate center of this group
                                Vector3 groupCenter = Vector3.zero;
                                foreach (var poi in group.Value)
                                {
                                    groupCenter += poi.transform.position;
                                }
                                groupCenter /= group.Value.Count;
                                
                                // Check if player is closer to this group than any previous group
                                float distance = Vector3.Distance(playerPosition, groupCenter);
                                if (distance < closestDistance)
                                {
                                    closestDistance = distance;
                                    closestRegion = regionData.Region;
                                    
                                    // If very close, just use this region
                                    if (distance < 200f)
                                        return regionData.Region.ToString();
                                }
                            }
                        }
                    }
                    
                    // If we found a region within reasonable distance, return it
                    if (closestDistance < 1000f)
                    {
                        return closestRegion.ToString();
                    }
                }

                // Fallback: Use the legacy reflection-based approach if the spatial method fails
                // First, try to access CurrentRegion property if it exists
                var currentRegionProperty = player.GetType().GetProperty("CurrentRegion");
                if (currentRegionProperty != null)
                {
                    var value = currentRegionProperty.GetValue(player);
                    if (value != null)
                        return value.ToString();
                }

                // Next, try to access Region property if it exists
                var regionProperty = player.GetType().GetProperty("Region");
                if (regionProperty != null)
                {
                    var value = regionProperty.GetValue(player);
                    if (value != null)
                        return value.ToString();
                }

                // Try public fields
                var regionField = player.GetType().GetField("currentRegion") ?? 
                                 player.GetType().GetField("region");
                if (regionField != null)
                {
                    var value = regionField.GetValue(player);
                    if (value != null)
                        return value.ToString();
                }

                // We couldn't determine the region, so return "Unknown"
                LuaUtility.LogWarning("Could not determine player's region");
                return "Unknown";
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error getting player region", ex);
                return null;
            }
        }
    }
}