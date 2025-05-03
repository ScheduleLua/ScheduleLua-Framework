using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.IO;
using ScheduleLua.API.Core;

namespace ScheduleLua.Core.Framework.Mods
{
    /// <summary>
    /// Exposes mod-related functionality to Lua scripts
    /// </summary>
    public static class ModsAPI
    {
        private static ModManager _modManager;
        
        /// <summary>
        /// Initialize the Mods API with the mod manager
        /// </summary>
        public static void RegisterAPI(Script luaEngine, ModManager modManager)
        {
            _modManager = modManager;
            
            // Register types for Lua
            UserData.RegisterType<LuaMod>();
            
            // Register mod-related functions
            luaEngine.Globals["GetMod"] = (Func<string, LuaMod>)GetMod;
            luaEngine.Globals["GetModExport"] = (Func<string, string, object>)GetModExport;
            luaEngine.Globals["GetAllMods"] = (Func<Table>)GetAllMods;
            luaEngine.Globals["ExportFunction"] = (Action<string, DynValue>)ExportFunction;
            luaEngine.Globals["ImportFunction"] = (Func<string, string, DynValue>)ImportFunction;
            luaEngine.Globals["IsModLoaded"] = (Func<string, bool>)IsModLoaded;
            
            // Register the ModConfig API
            ModConfigAPI.RegisterAPI(luaEngine, modManager);
        }
        
        /// <summary>
        /// Get a mod by its folder name
        /// </summary>
        private static LuaMod GetMod(string modName)
        {
            return _modManager.GetMod(modName);
        }
        
        /// <summary>
        /// Check if a mod is loaded
        /// </summary>
        private static bool IsModLoaded(string modName)
        {
            return _modManager.GetMod(modName) != null;
        }
        
        /// <summary>
        /// Get an exported value from a mod
        /// </summary>
        private static object GetModExport(string modName, string exportName)
        {
            return _modManager.GetModExport(modName, exportName);
        }
        
        /// <summary>
        /// Get information about all loaded mods
        /// </summary>
        private static Table GetAllMods()
        {
            var script = ModCore.Instance._luaEngine;
            var modTable = new Table(script);
            int index = 1;
            
            foreach (var mod in _modManager.LoadedMods.Values)
            {
                var entry = new Table(script);
                entry["name"] = mod.Manifest.Name;
                entry["version"] = mod.Manifest.Version;
                entry["author"] = mod.Manifest.Author;
                entry["description"] = mod.Manifest.Description;
                entry["folder"] = mod.FolderName;
                
                modTable[index++] = entry;
            }
            
            return modTable;
        }
        
        /// <summary>
        /// Export a function from the current mod for other mods to use
        /// </summary>
        private static void ExportFunction(string name, DynValue function)
        {
            var script = ModCore.Instance._luaEngine;
            
            if (function == null || function.Type != DataType.Function)
            {
                LuaUtility.LogError("ExportFunction requires a function as the second argument.");
                return;
            }
            
            string modName = script.Globals.Get("MOD_NAME").String;
            if (string.IsNullOrEmpty(modName))
            {
                LuaUtility.LogError("Failed to export function: not in a mod context.");
                return;
            }
            
            var mod = _modManager.GetMod(modName);
            if (mod != null)
            {
                mod.SetExport(name, function);
            }
            else
            {
                LuaUtility.LogError($"Cannot export function: mod {modName} not found.");
            }
        }
        
        /// <summary>
        /// Import a function from another mod
        /// </summary>
        private static DynValue ImportFunction(string modName, string functionName)
        {
            var export = GetModExport(modName, functionName);
            if (export is DynValue funcValue && funcValue.Type == DataType.Function)
                return funcValue;
                
            LuaUtility.LogWarning($"Function '{functionName}' not found in mod '{modName}'");
            return DynValue.Nil;
        }
    }
} 