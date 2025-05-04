using Newtonsoft.Json;
using ScheduleLua.API.Core;

namespace ScheduleLua.Core.Framework.Mods.ManagerUI
{
    /// <summary>
    /// Handles saving and loading of mod manager configuration
    /// </summary>
    public class ModManagerConfigStorage
    {
        private readonly string _configFilePath;
        private ModManagerConfig _config;

        /// <summary>
        /// Creates a new instance of the mod manager config storage
        /// </summary>
        public ModManagerConfigStorage(string scriptsDirectory)
        {
            // Config file is stored in the ScheduleLua directory
            _configFilePath = Path.Combine(
                Path.GetDirectoryName(scriptsDirectory),
                "mod_manager_config.json");

            // Initialize with default config
            _config = new ModManagerConfig();

            // Load existing config if available
            LoadConfig();
        }

        /// <summary>
        /// Loads the mod manager configuration from disk
        /// </summary>
        public bool LoadConfig()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    LuaUtility.Log("Mod manager config does not exist, using defaults");
                    return false;
                }

                string json = File.ReadAllText(_configFilePath);
                var loadedConfig = JsonConvert.DeserializeObject<ModManagerConfig>(json);

                if (loadedConfig != null)
                {
                    _config = loadedConfig;
                    LuaUtility.Log("Loaded mod manager configuration");
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                LuaUtility.LogError($"Error loading mod manager config: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Saves the mod manager configuration to disk
        /// </summary>
        public bool SaveConfig()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(_configFilePath, json);
                LuaUtility.Log("Saved mod manager configuration");
                return true;
            }
            catch (System.Exception ex)
            {
                LuaUtility.LogError($"Error saving mod manager config: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get the enabled state for a mod
        /// </summary>
        public bool GetModEnabled(string modFolderName)
        {
            if (_config.ModEnabledStates.TryGetValue(modFolderName, out bool enabled))
                return enabled;

            // Default to enabled if not specified
            return true;
        }

        /// <summary>
        /// Set the enabled state for a mod
        /// </summary>
        public void SetModEnabled(string modFolderName, bool enabled)
        {
            _config.ModEnabledStates[modFolderName] = enabled;
        }

        /// <summary>
        /// Updates the mod enabled states from the provided dictionary
        /// </summary>
        public void UpdateModEnabledStates(Dictionary<string, bool> modEnabledStates)
        {
            foreach (var kvp in modEnabledStates)
            {
                _config.ModEnabledStates[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Configuration data for the mod manager
        /// </summary>
        private class ModManagerConfig
        {
            /// <summary>
            /// Dictionary mapping mod folder names to their enabled state
            /// </summary>
            [JsonProperty("mod_enabled_states")]
            public Dictionary<string, bool> ModEnabledStates { get; set; } = new Dictionary<string, bool>();

            /// <summary>
            /// Additional configuration options can be added here
            /// </summary>
        }
    }
}