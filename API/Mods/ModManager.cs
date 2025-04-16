using MelonLoader;
using MoonSharp.Interpreter;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ScheduleLua.API.Core;

namespace ScheduleLua.API.Mods
{
    /// <summary>
    /// Manages the discovery, loading, and lifecycle of Lua mods
    /// </summary>
    public class ModManager
    {
        private readonly MelonLogger.Instance _logger;
        private readonly Script _luaEngine;
        private readonly string _scriptsDirectory;
        private readonly Dictionary<string, LuaMod> _loadedMods = new Dictionary<string, LuaMod>();
        private readonly HashSet<string> _processedScriptPaths = new HashSet<string>();

        /// <summary>
        /// Creates a new mod manager
        /// </summary>
        public ModManager(MelonLogger.Instance logger, Script luaEngine, string scriptsDirectory)
        {
            _logger = logger;
            _luaEngine = luaEngine;
            _scriptsDirectory = scriptsDirectory;
        }

        /// <summary>
        /// Gets all currently loaded mods
        /// </summary>
        public IReadOnlyDictionary<string, LuaMod> LoadedMods => _loadedMods;

        /// <summary>
        /// Gets all processed script paths to avoid double-loading
        /// </summary>
        public IReadOnlyCollection<string> ProcessedScriptPaths => _processedScriptPaths;

        /// <summary>
        /// Discovers and loads all mods in the scripts directory
        /// </summary>
        public void DiscoverAndLoadMods()
        {
            if (!Directory.Exists(_scriptsDirectory))
            {
                LuaUtility.LogError($"Scripts directory does not exist: {_scriptsDirectory}");
                return;
            }

            // The existing scripts directory can contain both individual scripts and mod folders
            var directories = Directory.GetDirectories(_scriptsDirectory);
            List<(string folderPath, ModManifest manifest)> discoveredMods = new List<(string, ModManifest)>();

            // First pass: Discover all mod folders with manifest.json
            foreach (var folder in directories)
            {
                var manifestPath = Path.Combine(folder, "manifest.json");
                if (!File.Exists(manifestPath))
                {
                    // This folder doesn't have a manifest.json, so it's not a mod folder
                    continue;
                }

                try
                {
                    var manifestJson = File.ReadAllText(manifestPath);
                    var manifest = JsonConvert.DeserializeObject<ModManifest>(manifestJson);

                    if (manifest == null)
                    {
                        LuaUtility.LogWarning($"Failed to parse manifest for folder: {Path.GetFileName(folder)}");
                        continue;
                    }

                    // API version check
                    if (!string.IsNullOrEmpty(manifest.ApiVersion) && manifest.ApiVersion != ScheduleLua.Core.ModVersion)
                    {
                        LuaUtility.LogWarning($"Mod {manifest.Name} requires API version {manifest.ApiVersion}, but current version is {ScheduleLua.Core.ModVersion}");
                    }

                    discoveredMods.Add((folder, manifest));
                }
                catch (Exception ex)
                {
                    LuaUtility.LogError($"Error parsing manifest for folder {Path.GetFileName(folder)}: {ex.Message}");
                }
            }

            // Sort mods by load order
            discoveredMods = discoveredMods.OrderBy(m => m.manifest.LoadOrder).ToList();

            // Second pass: Load mods in dependency order
            foreach (var (folderPath, manifest) in discoveredMods)
            {
                LoadMod(folderPath, manifest, discoveredMods);
            }

            LuaUtility.Log($"Loaded {_loadedMods.Count} Lua mods successfully.");
        }

        /// <summary>
        /// Loads a mod and its dependencies
        /// </summary>
        private bool LoadMod(string folderPath, ModManifest manifest, List<(string folderPath, ModManifest manifest)> allMods)
        {
            var modFolderName = Path.GetFileName(folderPath);

            // Check if already loaded
            if (_loadedMods.ContainsKey(modFolderName))
                return true;

            // First load dependencies
            foreach (var dependency in manifest.Dependencies)
            {
                // Find the dependency in discovered mods
                var dependencyData = allMods.FirstOrDefault(m => Path.GetFileName(m.folderPath) == dependency);

                if (dependencyData == default)
                {
                    LuaUtility.LogError($"Mod {manifest.Name} depends on {dependency}, but it was not found.");
                    return false;
                }

                if (!LoadMod(dependencyData.folderPath, dependencyData.manifest, allMods))
                {
                    LuaUtility.LogError($"Failed to load dependency {dependency} for mod {manifest.Name}.");
                    return false;
                }
            }

            try
            {
                // Create the mod instance
                var mod = new LuaMod(folderPath, manifest, _logger);

                // Check if main script is also in the files list (common error)
                if (manifest.Files.Contains(manifest.Main))
                {
                    LuaUtility.LogWarning($"Mod {manifest.Name}: The main script '{manifest.Main}' should not be included in the 'files' section of manifest.json. It is automatically loaded as the main entry point.");
                }

                // First load the main script
                var mainScriptPath = Path.Combine(folderPath, manifest.Main);
                if (!File.Exists(mainScriptPath))
                {
                    LuaUtility.LogError($"Main script {manifest.Main} not found for mod {manifest.Name}.");
                    return false;
                }

                // Set mod context in globals for this script execution
                _luaEngine.Globals["MOD_NAME"] = modFolderName;
                _luaEngine.Globals["MOD_PATH"] = folderPath;

                // Track all script paths in this mod to avoid duplicates
                HashSet<string> modScriptPaths = new HashSet<string> { mainScriptPath };
                foreach (var file in manifest.Files)
                {
                    modScriptPaths.Add(Path.Combine(folderPath, file));
                }

                // Register all script paths with the mod manager to prevent double-loading
                foreach (var scriptPath in modScriptPaths)
                {
                    _processedScriptPaths.Add(scriptPath);
                }

                // Load main script
                var mainScript = LoadScriptForMod(mainScriptPath);
                if (mainScript == null)
                {
                    LuaUtility.LogError($"Failed to load main script for mod {manifest.Name}.");
                    return false;
                }

                mod.AddScript(mainScript);

                // Load all additional files
                foreach (var file in manifest.Files)
                {
                    var filePath = Path.Combine(folderPath, file);
                    if (!File.Exists(filePath))
                    {
                        LuaUtility.LogWarning($"Script file {file} not found for mod {manifest.Name}.");
                        continue;
                    }

                    var script = LoadScriptForMod(filePath);
                    if (script != null)
                    {
                        mod.AddScript(script);
                    }
                }

                // Add mod to loaded mods
                _loadedMods[modFolderName] = mod;
                mod.SetLoaded();

                LuaUtility.Log($"Successfully loaded mod: {manifest.Name}");
                return true;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error loading mod {manifest.Name}: {ex.Message}");
                LuaUtility.LogError(ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// Loads a script file into the Lua engine for a mod
        /// </summary>
        private LuaScript LoadScriptForMod(string scriptPath)
        {
            try
            {
                // Determine the mod folder from the script path
                string modFolder = Path.GetDirectoryName(scriptPath);
                string modFolderName = Path.GetFileName(modFolder);

                // Set Lua context for this mod/script
                _luaEngine.Globals["MOD_NAME"] = modFolderName;
                _luaEngine.Globals["MOD_PATH"] = modFolder;
                _luaEngine.Globals["SCRIPT_PATH"] = scriptPath;

                // Create and load the script
                var script = new LuaScript(scriptPath, _luaEngine, _logger);

                // For module files (not the main script), pre-register the module table
                string fileName = Path.GetFileName(scriptPath);
                string moduleName = Path.GetFileNameWithoutExtension(scriptPath);

                // If this is a module file, ensure it has a global module table to prevent nil errors
                if (_loadedMods.TryGetValue(modFolderName, out var mod) &&
                    fileName != mod.Manifest.Main &&
                    mod.Manifest.Files.Contains(fileName))
                {
                    // Pre-create the module table in the globals
                    if (_luaEngine.Globals.Get(moduleName + "_module").IsNil())
                    {
                        LuaUtility.Log($"Pre-registering module: {moduleName}");
                        _luaEngine.Globals[moduleName + "_module"] = _luaEngine.DoString("return {}");
                    }
                }

                if (script.Load())
                {
                    return script;
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error loading script {scriptPath}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Initializes all loaded mods
        /// </summary>
        public void InitializeMods()
        {
            foreach (var mod in _loadedMods.Values)
            {
                mod.Initialize();
            }
        }

        /// <summary>
        /// Updates all loaded mods
        /// </summary>
        public void UpdateMods()
        {
            foreach (var mod in _loadedMods.Values)
            {
                mod.Update();
            }
        }

        /// <summary>
        /// Trigger an event for all loaded mods
        /// </summary>
        public void TriggerEvent(string eventName, params object[] args)
        {
            foreach (var mod in _loadedMods.Values)
            {
                mod.TriggerEvent(eventName, args);
            }
        }

        /// <summary>
        /// Checks if a script path is part of a mod
        /// </summary>
        public bool IsScriptPathProcessed(string scriptPath)
        {
            return _processedScriptPaths.Contains(scriptPath);
        }

        /// <summary>
        /// Gets a mod by its folder name
        /// </summary>
        public LuaMod GetMod(string modFolderName)
        {
            if (_loadedMods.TryGetValue(modFolderName, out var mod))
                return mod;
            return null;
        }

        /// <summary>
        /// Gets an exported value from a mod
        /// </summary>
        public object GetModExport(string modName, string exportName)
        {
            var mod = GetMod(modName);
            if (mod == null)
                return null;

            return mod.GetExport(exportName);
        }
    }
}