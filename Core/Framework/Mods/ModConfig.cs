using MoonSharp.Interpreter;
using Newtonsoft.Json;
using ScheduleLua.API.Core;

namespace ScheduleLua.Core.Framework.Mods
{
    /// <summary>
    /// Represents a configuration for a Lua mod
    /// </summary>
    [MoonSharpUserData]
    public class ModConfig
    {
        /// <summary>
        /// The mod this configuration belongs to
        /// </summary>
        [JsonIgnore]
        public LuaMod ParentMod { get; private set; }

        /// <summary>
        /// The configuration file path
        /// </summary>
        [JsonIgnore]
        public string ConfigFilePath { get; private set; }

        /// <summary>
        /// Dictionary containing all configuration values
        /// </summary>
        [JsonProperty("values")]
        private Dictionary<string, object> ConfigValues { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Dictionary containing default values for config entries
        /// </summary>
        [JsonIgnore]
        private Dictionary<string, object> DefaultValues { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Dictionary containing descriptions for config entries
        /// </summary>
        [JsonProperty("descriptions")]
        private Dictionary<string, string> ConfigDescriptions { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Creates a new mod configuration for the specified mod
        /// </summary>
        public ModConfig(LuaMod parentMod)
        {
            ParentMod = parentMod;

            // Create config file path in mod folder
            ConfigFilePath = Path.Combine(parentMod.FolderPath, "config.json");
        }

        /// <summary>
        /// Defines a new configuration entry with a default value and description
        /// </summary>
        public void DefineConfig<T>(string key, T defaultValue, string description = "")
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Config key cannot be null or empty", nameof(key));

            // Store the default value
            DefaultValues[key] = defaultValue;

            // Store the description
            if (!string.IsNullOrEmpty(description))
                ConfigDescriptions[key] = description;

            // If the value doesn't exist yet, use the default value
            if (!ConfigValues.ContainsKey(key))
                ConfigValues[key] = defaultValue;
        }

        /// <summary>
        /// Gets a configuration value by key, or returns the default if not found
        /// </summary>
        public T GetValue<T>(string key)
        {
            // If the key exists in config values, return it
            if (ConfigValues.TryGetValue(key, out object value))
            {
                try
                {
                    // Handle type conversion
                    if (value is T typedValue)
                        return typedValue;

                    // For numeric types, we need special handling due to JSON deserialization
                    if (typeof(T) == typeof(int) && value is long longValue)
                        return (T)(object)Convert.ToInt32(longValue);
                    else if (typeof(T) == typeof(float) && (value is double doubleValue))
                        return (T)(object)Convert.ToSingle(doubleValue);

                    // For other types, use JSON conversion
                    string json = JsonConvert.SerializeObject(value);
                    return JsonConvert.DeserializeObject<T>(json);
                }
                catch (Exception ex)
                {
                    LuaUtility.LogError($"Error converting config value for key '{key}': {ex.Message}");

                    // If conversion fails, check if we have a default value of the right type
                    if (DefaultValues.TryGetValue(key, out object defaultValue) && defaultValue is T defaultTypedValue)
                        return defaultTypedValue;
                }
            }

            // If we have a default value for this key, return it
            if (DefaultValues.TryGetValue(key, out object defaultVal) && defaultVal is T defaultTyped)
                return defaultTyped;

            // Otherwise throw an exception
            throw new KeyNotFoundException($"Config key '{key}' not found and no default value exists.");
        }

        /// <summary>
        /// Sets a configuration value
        /// </summary>
        public void SetValue<T>(string key, T value)
        {
            // If the value is a Table (from Lua), we need to convert it to a clean dictionary
            if (value is Table luaTable)
            {
                ConfigValues[key] = CleanTableForSerialization(luaTable);
            }
            else
            {
                ConfigValues[key] = value;
            }
        }

        /// <summary>
        /// Cleans a Lua table by removing references to the script engine and converting to simple C# objects
        /// </summary>
        private object CleanTableForSerialization(Table table)
        {
            // If the table appears to be an array (sequential numeric keys starting at 1)
            bool isArray = true;
            int arrayLength = 0;

            // Check if it's an array by verifying sequential integer keys starting from 1
            for (int i = 1; i <= table.Length; i++)
            {
                if (table.Get(i).IsNil())
                {
                    isArray = false;
                    break;
                }
                arrayLength = i;
            }

            if (isArray && arrayLength > 0)
            {
                // Create a list (will be serialized as a JSON array)
                var list = new List<object>();

                for (int i = 1; i <= arrayLength; i++)
                {
                    var value = table.Get(i);
                    list.Add(ConvertDynValueToSerializable(value));
                }

                return list;
            }
            else
            {
                // Create a dictionary (will be serialized as a JSON object)
                var dict = new Dictionary<object, object>();

                foreach (var pair in table.Pairs)
                {
                    var key = ConvertDynValueToSerializable(pair.Key);
                    var value = ConvertDynValueToSerializable(pair.Value);

                    // Only string keys are allowed in JSON
                    string stringKey = key?.ToString() ?? "null";
                    dict[stringKey] = value;
                }

                return dict;
            }
        }

        /// <summary>
        /// Converts a DynValue to a serializable object
        /// </summary>
        private object ConvertDynValueToSerializable(DynValue value)
        {
            switch (value.Type)
            {
                case DataType.Nil:
                    return null;

                case DataType.Boolean:
                    return value.Boolean;

                case DataType.Number:
                    return value.Number;

                case DataType.String:
                    return value.String;

                case DataType.Table:
                    return CleanTableForSerialization(value.Table);

                case DataType.Tuple:
                    var list = new List<object>();
                    foreach (var item in value.Tuple)
                    {
                        list.Add(ConvertDynValueToSerializable(item));
                    }
                    return list;

                default:
                    return value.ToString();
            }
        }

        /// <summary>
        /// Checks if a configuration key exists
        /// </summary>
        public bool HasKey(string key)
        {
            return ConfigValues.ContainsKey(key) || DefaultValues.ContainsKey(key);
        }

        /// <summary>
        /// Gets all configuration keys
        /// </summary>
        public IEnumerable<string> GetAllKeys()
        {
            // Return the union of config values and default values
            HashSet<string> allKeys = new HashSet<string>(ConfigValues.Keys);
            foreach (var key in DefaultValues.Keys)
                allKeys.Add(key);

            return allKeys;
        }

        /// <summary>
        /// Gets the description for a config key
        /// </summary>
        public string GetDescription(string key)
        {
            if (ConfigDescriptions.TryGetValue(key, out string description))
                return description;
            return string.Empty;
        }

        /// <summary>
        /// Loads the configuration from the config file
        /// </summary>
        public bool LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    LuaUtility.Log($"Config file does not exist for mod {ParentMod.Manifest.Name}, using defaults");
                    return false;
                }

                string json = File.ReadAllText(ConfigFilePath);

                try
                {
                    // Parse json without using dynamic
                    var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                    if (jsonObject == null)
                    {
                        LuaUtility.LogError($"Failed to deserialize config for mod {ParentMod.Manifest.Name}");
                        return false;
                    }

                    // Check if it has "values" property (new format)
                    if (jsonObject.TryGetValue("values", out object valuesObj))
                    {
                        // Convert to JSON and then back to dictionary
                        string valuesJson = JsonConvert.SerializeObject(valuesObj);
                        ConfigValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(valuesJson)
                            ?? new Dictionary<string, object>();

                        // Check for descriptions
                        if (jsonObject.TryGetValue("descriptions", out object descriptionsObj))
                        {
                            string descriptionsJson = JsonConvert.SerializeObject(descriptionsObj);
                            ConfigDescriptions = JsonConvert.DeserializeObject<Dictionary<string, string>>(descriptionsJson)
                                ?? new Dictionary<string, string>();
                        }
                    }
                    else
                    {
                        // Looks like old format or direct dictionary
                        // Try to load directly as config values
                        ConfigValues = jsonObject;
                    }
                }
                catch (Exception ex)
                {
                    LuaUtility.LogWarning($"Error parsing config structure for mod {ParentMod.Manifest.Name}: {ex.Message}");
                    LuaUtility.LogWarning("Attempting to load with direct deserialization...");

                    // Try deserializing as ModConfig object (old format)
                    try
                    {
                        var oldFormatConfig = JsonConvert.DeserializeObject<ModConfig>(json);
                        if (oldFormatConfig != null && oldFormatConfig.ConfigValues != null)
                        {
                            ConfigValues = oldFormatConfig.ConfigValues;
                            if (oldFormatConfig.ConfigDescriptions != null)
                            {
                                ConfigDescriptions = oldFormatConfig.ConfigDescriptions;
                            }
                        }
                        else
                        {
                            LuaUtility.LogWarning($"Config format for mod {ParentMod.Manifest.Name} couldn't be determined, using defaults");
                            return false;
                        }
                    }
                    catch
                    {
                        LuaUtility.LogError("Failed to load config in any format, using defaults");
                        return false;
                    }
                }

                LuaUtility.Log($"Loaded config for mod {ParentMod.Manifest.Name} with {ConfigValues.Count} values");
                return true;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error loading config for mod {ParentMod.Manifest.Name}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    LuaUtility.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// Saves the configuration to the config file
        /// </summary>
        public bool SaveConfig()
        {
            try
            {
                // Clean the ConfigValues dictionary to handle Lua tables
                Dictionary<string, object> cleanedValues = new Dictionary<string, object>();

                foreach (var kvp in ConfigValues)
                {
                    if (kvp.Value is Table luaTable)
                    {
                        cleanedValues[kvp.Key] = CleanTableForSerialization(luaTable);
                    }
                    else
                    {
                        cleanedValues[kvp.Key] = kvp.Value;
                    }
                }

                // Create a temporary object for serialization with cleaned values
                var serializableObj = new
                {
                    values = cleanedValues,
                    descriptions = ConfigDescriptions
                };

                // Serialize with indentation for readability
                string json = JsonConvert.SerializeObject(serializableObj, Formatting.Indented);

                File.WriteAllText(ConfigFilePath, json);
                LuaUtility.Log($"Saved config for mod {ParentMod.Manifest.Name}");
                return true;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error saving config for mod {ParentMod.Manifest.Name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Returns a Lua table representation of the configuration for use in Lua scripts
        /// </summary>
        public Table ToLuaTable(Script luaEngine)
        {
            var table = new Table(luaEngine);

            foreach (var key in GetAllKeys())
            {
                try
                {
                    object value = HasKey(key) ? ConfigValues[key] : DefaultValues[key];
                    table[key] = DynValue.FromObject(luaEngine, value);
                }
                catch (Exception ex)
                {
                    LuaUtility.LogError($"Error converting config value for key '{key}' to Lua: {ex.Message}");
                }
            }

            return table;
        }
    }
}