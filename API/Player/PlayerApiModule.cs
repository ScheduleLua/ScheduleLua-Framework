using System;
using UnityEngine;
using MoonSharp.Interpreter;
using ScheduleOne;
using ScheduleLua.API.Base;
using ScheduleLua.API.Core;
using ScheduleOne.PlayerScripts;
using ScheduleOne.PlayerScripts.Health;
using System.Linq;
using System.Collections.Generic;
using ScheduleOne.DevUtilities;
using UnityEngine.SceneManagement;
using ScheduleLua.API.Core.TypeProxies;

namespace ScheduleLua.API.Player
{
    /// <summary>
    /// Module that provides Lua API access to player-related functionality in Schedule I
    /// </summary>
    public class PlayerApiModule : BaseLuaApiModule
    {
        private GameObject _player;
        private GameObject Player => _player ??= (ScheduleOne.PlayerScripts.Player.Local != null ? 
            ScheduleOne.PlayerScripts.Player.Local.gameObject : GameObject.FindGameObjectWithTag("Player"));

        private ScheduleOne.PlayerScripts.Player _playerInstance;
        private ScheduleOne.PlayerScripts.Player PlayerInstance => _playerInstance ??= 
            ScheduleOne.PlayerScripts.Player.Local ?? Player?.GetComponent<ScheduleOne.PlayerScripts.Player>();

        private PlayerHealth _playerHealth;
        private PlayerHealth PlayerHealth => _playerHealth ??= Player?.GetComponent<PlayerHealth>();

        private PlayerMovement _playerMovement;
        private PlayerMovement PlayerMovement => _playerMovement ??= PlayerSingleton<PlayerMovement>.Instance;

        private bool _sceneChangeListenerAdded = false;

        /// <summary>
        /// Initializes the Player API module
        /// </summary>
        public override void Initialize()
        {
            // Set up scene change listener
            if (!_sceneChangeListenerAdded)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                _sceneChangeListenerAdded = true;
            }

            if (_player == null)
            {
                _player = GameObject.FindGameObjectWithTag("Player");
            }

            if (_player != null)
            {
                if (_playerInstance == null)
                {
                    _playerInstance = _player.GetComponent<ScheduleOne.PlayerScripts.Player>();
                }
                if (_playerHealth == null)
                {
                    _playerHealth = _player.GetComponent<PlayerHealth>();
                }
            }
            
            _playerMovement = PlayerSingleton<PlayerMovement>.Instance;
        }

        /// <summary>
        /// Registers all player-related API functions with the Lua engine
        /// </summary>
        public override void RegisterAPI(Script luaEngine)
        {
            if (luaEngine == null)
                throw new ArgumentNullException(nameof(luaEngine));

            // Player functions
            luaEngine.Globals["GetPlayerState"] = (Func<Table>)GetPlayerState;
            luaEngine.Globals["GetPlayerPosition"] = (Func<Vector3Proxy>)GetPlayerPositionProxy;
            luaEngine.Globals["GetPlayerRegion"] = (Func<string>)GetPlayerRegion;
            luaEngine.Globals["SetPlayerPosition"] = (Action<float, float, float>)SetPlayerPosition;
            luaEngine.Globals["GetPlayerEnergy"] = (Func<float>)GetPlayerEnergy;
            luaEngine.Globals["SetPlayerEnergy"] = (Action<float>)SetPlayerEnergy;
            luaEngine.Globals["GetPlayerHealth"] = (Func<float>)GetPlayerHealth;
            luaEngine.Globals["SetPlayerHealth"] = (Action<float>)SetPlayerHealth;
            luaEngine.Globals["GetPlayerMovementSpeed"] = (Func<float>)GetMovementSpeed;
            luaEngine.Globals["SetPlayerMovementSpeed"] = (Func<float, bool>)SetMovementSpeed;
        }

        /// <summary>
        /// Handles scene change events and resets player references when necessary
        /// </summary>
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            // Reset cached references when returning to menu or changing scenes
            if (scene.name == "Menu" || !Player || !PlayerInstance)
            {
                ResetPlayerReferences();
            }
        }

        /// <summary>
        /// Cleans up resources when the module is shut down
        /// </summary>
        public override void Shutdown()
        {
            if (_sceneChangeListenerAdded)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                _sceneChangeListenerAdded = false;
            }
            
            ResetPlayerReferences();
        }

        /// <summary>
        /// Resets all cached player references
        /// </summary>
        public void ResetPlayerReferences()
        {
            _player = null;
            _playerInstance = null;
            _playerHealth = null;
            _playerMovement = null;
        }

        /// <summary>
        /// Gets the player's current state and information
        /// </summary>
        /// <returns>Lua table containing player data</returns>
        public Table GetPlayerState()
        {
            try
            {
                var player = Player;
                if (player == null)
                {
                    LogWarning("Player not found");
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
                LogError($"Error getting player state: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the player's position as a Vector3Proxy for Lua compatibility
        /// </summary>
        /// <returns>Vector3Proxy position of the player</returns>
        public Vector3Proxy GetPlayerPositionProxy()
        {
            try
            {
                var player = Player;
                if (player == null)
                {
                    LogWarning("Player not found");
                    return Vector3Proxy.zero;
                }

                return new Vector3Proxy(player.transform.position);
            }
            catch (Exception ex)
            {
                LogError($"Error getting player position: {ex.Message}");
                return Vector3Proxy.zero;
            }
        }

        /// <summary>
        /// Gets the player's current region 
        /// </summary>
        /// <returns>string representing the region or "Unknown" if not found</returns>
        public string GetPlayerRegion()
        {
            try
            {
                var player = PlayerInstance;
                if (player == null)
                {
                    LogWarning("Player not found");
                    return "Unknown";
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

                // Fallback: Try to access reflection-based properties if the spatial method fails
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
                LogWarning("Could not determine player's region");
                return "Unknown";
            }
            catch (Exception ex)
            {
                LogError($"Error getting player region: {ex.Message}");
                return "Unknown";
            }
        }

        /// <summary>
        /// Sets the player's position
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z coordinate</param>
        public void SetPlayerPosition(float x, float y, float z)
        {
            try
            {
                var player = Player;
                if (player == null)
                {
                    LogWarning("Player not found, position not set");
                    return;
                }

                player.transform.position = new Vector3(x, y, z);
                LogInfo($"Set player position to {x}, {y}, {z}");
            }
            catch (Exception ex)
            {
                LogError($"Error setting player position: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the player's current energy
        /// </summary>
        /// <returns>The amount of energy the player has</returns>
        public float GetPlayerEnergy()
        {
            try
            {
                // Check if we're in a scene where the player should exist
                if (SceneManager.GetActiveScene().name == "Menu")
                {
                    return 0f;
                }

                var player = PlayerInstance;
                if (player == null)
                {
                    // Silent fail in menu scene to avoid console spam
                    if (SceneManager.GetActiveScene().name != "Main")
                    {
                        return 0f;
                    }
                    LogWarning("Player not found, returning 0");
                    return 0f;
                }

                if (player.Energy == null)
                {
                    LogWarning("Player.Energy component is null, returning 0");
                    return 0f;
                }

                return player.Energy.CurrentEnergy;
            }
            catch (Exception ex)
            {
                LogError($"Error getting player energy: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// Sets the player's energy
        /// </summary>
        /// <param name="amount">The amount of energy to set</param>
        public void SetPlayerEnergy(float amount)
        {
            try
            {
                var player = PlayerInstance;
                if (player == null)
                {
                    LogWarning("Player not found, energy not set");
                    return;
                }

                if (player.Energy == null)
                {
                    LogWarning("Player.Energy component is null, energy not set");
                    return;
                }

                player.Energy.SetEnergy(amount);
                LogInfo($"Set player energy to {amount}");
            }
            catch (Exception ex)
            {
                LogError($"Error setting player energy: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the player's current health
        /// </summary>
        /// <returns>The amount of health the player has</returns>
        public float GetPlayerHealth()
        {
            try
            {
                // Check if we're in a scene where the player should exist
                if (SceneManager.GetActiveScene().name == "Menu")
                {
                    return 0f;
                }

                var playerHealth = PlayerHealth;
                if (playerHealth == null)
                {
                    // Silent fail in menu scene to avoid console spam
                    if (SceneManager.GetActiveScene().name != "Main")
                    {
                        return 0f;
                    }
                    LogWarning("Player health component not found, returning 0");
                    return 0f;
                }

                return playerHealth.CurrentHealth;
            }
            catch (Exception ex)
            {
                LogError($"Error getting player health: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// Sets the player's health
        /// </summary>
        /// <param name="amount">The amount of health to set</param>
        public void SetPlayerHealth(float amount)
        {
            try
            {
                var playerHealth = PlayerHealth;
                if (playerHealth == null)
                {
                    LogWarning("Player health component not found, health not set");
                    return;
                }

                playerHealth.SetHealth(amount);
                LogInfo($"Set player health to {amount}");
            }
            catch (Exception ex)
            {
                LogError($"Error setting player health: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the player's current movement speed multiplier
        /// </summary>
        /// <returns>The current movement speed multiplier</returns>
        public float GetMovementSpeed()
        {
            try
            {
                var playerMovement = PlayerMovement;
                if (playerMovement == null)
                {
                    LogWarning("Player movement component not found, returning 1");
                    return 1.0f;
                }

                return playerMovement.MoveSpeedMultiplier;
            }
            catch (Exception ex)
            {
                LogError($"Error getting player movement speed: {ex.Message}");
                return 1.0f;
            }
        }

        /// <summary>
        /// Sets the player's movement speed multiplier
        /// </summary>
        /// <param name="speedMultiplier">The speed multiplier to set (1.0 is normal speed)</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SetMovementSpeed(float speedMultiplier)
        {
            try
            {
                var playerMovement = PlayerMovement;
                if (playerMovement == null)
                {
                    LogWarning("Player movement component not found, speed not set");
                    return false;
                }

                // Clamp values to reasonable range to prevent game-breaking issues
                float clampedSpeed = Mathf.Clamp(speedMultiplier, 0.1f, 5.0f);

                if (clampedSpeed != speedMultiplier)
                {
                    LogWarning($"Speed multiplier clamped from {speedMultiplier} to {clampedSpeed}");
                }

                playerMovement.MoveSpeedMultiplier = clampedSpeed;
                LogInfo($"Set player movement speed to {clampedSpeed}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error setting player movement speed: {ex.Message}");
                return false;
            }
        }
    }
} 