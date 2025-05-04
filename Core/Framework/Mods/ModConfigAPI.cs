using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using ScheduleLua.API.Core;

namespace ScheduleLua.Core.Framework.Mods
{
    /// <summary>
    /// Exposes mod configuration functionality to Lua scripts
    /// </summary>
    public static class ModConfigAPI
    {
        private static ModManager _modManager;
        
        /// <summary>
        /// Register the ModConfig API with the Lua engine and mod manager
        /// </summary>
        public static void RegisterAPI(Script luaEngine, ModManager modManager)
        {
            _modManager = modManager;
            
            // Register types for Lua
            UserData.RegisterType<ModConfig>();
            
            // Register configuration-related functions
            luaEngine.Globals["GetModConfig"] = (Func<Table>)GetModConfig;
            luaEngine.Globals["SaveModConfig"] = (Func<bool>)SaveModConfig;
            luaEngine.Globals["DefineConfigValue"] = (Func<string, object, string, bool>)DefineConfigValue;
            luaEngine.Globals["GetConfigValue"] = (Func<string, object>)GetConfigValue;
            luaEngine.Globals["SetConfigValue"] = (Func<string, object, bool>)SetConfigValue;
            luaEngine.Globals["HasConfigKey"] = (Func<string, bool>)HasConfigKey;
            luaEngine.Globals["GetConfigKeys"] = (Func<Table>)GetConfigKeys;
        }
        
        /// <summary>
        /// Get the current mod's configuration as a Lua table
        /// </summary>
        private static Table GetModConfig()
        {
            Script luaEngine = ModCore.Instance._luaEngine;
            string modName = luaEngine.Globals.Get("MOD_NAME").String;
            
            if (string.IsNullOrEmpty(modName))
            {
                LuaUtility.LogError("Failed to get mod config: not in a mod context");
                return new Table(luaEngine);
            }
            
            var mod = _modManager.GetMod(modName);
            if (mod == null)
            {
                LuaUtility.LogError($"Failed to get mod config: mod {modName} not found");
                return new Table(luaEngine);
            }
            
            // Get or create mod config
            ModConfig config = GetOrCreateConfig(mod);
            
            // Convert to Lua table
            return config.ToLuaTable(luaEngine);
        }
        
        /// <summary>
        /// Save the current mod's configuration to disk
        /// </summary>
        private static bool SaveModConfig()
        {
            Script luaEngine = ModCore.Instance._luaEngine;
            string modName = luaEngine.Globals.Get("MOD_NAME").String;
            
            if (string.IsNullOrEmpty(modName))
            {
                LuaUtility.LogError("Failed to save mod config: not in a mod context");
                return false;
            }
            
            var mod = _modManager.GetMod(modName);
            if (mod == null)
            {
                LuaUtility.LogError($"Failed to save mod config: mod {modName} not found");
                return false;
            }
            
            // Get or create mod config
            ModConfig config = GetOrCreateConfig(mod);
            
            // Save config
            return config.SaveConfig();
        }
        
        /// <summary>
        /// Define a configuration value with a default value and description
        /// </summary>
        private static bool DefineConfigValue(string key, object defaultValue, string description)
        {
            Script luaEngine = ModCore.Instance._luaEngine;
            string modName = luaEngine.Globals.Get("MOD_NAME").String;
            
            if (string.IsNullOrEmpty(modName))
            {
                LuaUtility.LogError("Failed to define config value: not in a mod context");
                return false;
            }
            
            var mod = _modManager.GetMod(modName);
            if (mod == null)
            {
                LuaUtility.LogError($"Failed to define config value: mod {modName} not found");
                return false;
            }
            
            // Get or create mod config
            ModConfig config = GetOrCreateConfig(mod);
            
            try
            {
                // Handle special case for nil default value
                if (defaultValue is DynValue dynValue && dynValue.IsNil())
                    defaultValue = null;
                
                // Define config value
                config.DefineConfig(key, defaultValue, description);
                return true;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error defining config value: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get a configuration value by key
        /// </summary>
        private static object GetConfigValue(string key)
        {
            Script luaEngine = ModCore.Instance._luaEngine;
            string modName = luaEngine.Globals.Get("MOD_NAME").String;
            
            if (string.IsNullOrEmpty(modName))
            {
                LuaUtility.LogError("Failed to get config value: not in a mod context");
                return DynValue.Nil;
            }
            
            var mod = _modManager.GetMod(modName);
            if (mod == null)
            {
                LuaUtility.LogError($"Failed to get config value: mod {modName} not found");
                return DynValue.Nil;
            }
            
            // Get or create mod config
            ModConfig config = GetOrCreateConfig(mod);
            
            try
            {
                if (!config.HasKey(key))
                    return DynValue.Nil;
                
                // Get the value with fallback to default
                return config.GetValue<object>(key);
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error getting config value: {ex.Message}");
                return DynValue.Nil;
            }
        }
        
        /// <summary>
        /// Set a configuration value
        /// </summary>
        private static bool SetConfigValue(string key, object value)
        {
            Script luaEngine = ModCore.Instance._luaEngine;
            string modName = luaEngine.Globals.Get("MOD_NAME").String;
            
            if (string.IsNullOrEmpty(modName))
            {
                LuaUtility.LogError("Failed to set config value: not in a mod context");
                return false;
            }
            
            var mod = _modManager.GetMod(modName);
            if (mod == null)
            {
                LuaUtility.LogError($"Failed to set config value: mod {modName} not found");
                return false;
            }
            
            // Get or create mod config
            ModConfig config = GetOrCreateConfig(mod);
            
            try
            {
                // Handle special case for nil value
                if (value is DynValue dynValue)
                {
                    if (dynValue.IsNil())
                    {
                        value = null;
                    }
                    else if (dynValue.Type == DataType.Table)
                    {
                        // For DynValue containing a table, extract the table directly
                        value = dynValue.Table;
                    }
                    else
                    {
                        // For other DynValue types, extract the actual value
                        value = dynValue.ToObject();
                    }
                }
                
                // Set the value
                config.SetValue(key, value);
                return true;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error setting config value: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Check if a configuration key exists
        /// </summary>
        private static bool HasConfigKey(string key)
        {
            Script luaEngine = ModCore.Instance._luaEngine;
            string modName = luaEngine.Globals.Get("MOD_NAME").String;
            
            if (string.IsNullOrEmpty(modName))
            {
                LuaUtility.LogError("Failed to check config key: not in a mod context");
                return false;
            }
            
            var mod = _modManager.GetMod(modName);
            if (mod == null)
            {
                LuaUtility.LogError($"Failed to check config key: mod {modName} not found");
                return false;
            }
            
            // Get or create mod config
            ModConfig config = GetOrCreateConfig(mod);
            
            return config.HasKey(key);
        }
        
        /// <summary>
        /// Get all configuration keys as a Lua table
        /// </summary>
        private static Table GetConfigKeys()
        {
            Script luaEngine = ModCore.Instance._luaEngine;
            string modName = luaEngine.Globals.Get("MOD_NAME").String;
            
            if (string.IsNullOrEmpty(modName))
            {
                LuaUtility.LogError("Failed to get config keys: not in a mod context");
                return new Table(luaEngine);
            }
            
            var mod = _modManager.GetMod(modName);
            if (mod == null)
            {
                LuaUtility.LogError($"Failed to get config keys: mod {modName} not found");
                return new Table(luaEngine);
            }
            
            // Get or create mod config
            ModConfig config = GetOrCreateConfig(mod);
            
            // Create a table with the keys
            var table = new Table(luaEngine);
            int index = 1;
            
            foreach (var key in config.GetAllKeys())
            {
                table[index++] = key;
            }
            
            return table;
        }
        
        /// <summary>
        /// Utility to get or create a mod config instance
        /// </summary>
        private static ModConfig GetOrCreateConfig(LuaMod mod)
        {
            // Check if mod already has a config
            var config = mod.GetModConfig();
            
            if (config == null)
            {
                // Create new config
                config = new ModConfig(mod);
                
                // Load existing config if it exists
                config.LoadConfig();
                
                // Store config in mod
                mod.SetModConfig(config);
            }
            
            return config;
        }
    }
} 