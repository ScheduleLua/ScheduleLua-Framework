using MelonLoader;
using MoonSharp.Interpreter;
using System.Collections.Generic;
using System.IO;
using ScheduleLua.API.Core;

namespace ScheduleLua.API.Mods
{
    /// <summary>
    /// Represents a loaded Lua mod with its manifest, scripts, and exported functionality
    /// </summary>
    [MoonSharpUserData]
    public class LuaMod
    {
        private readonly MelonLogger.Instance _logger;
        private bool _initialized = false;
        private bool _initializing = false;

        /// <summary>
        /// The mod's manifest information
        /// </summary>
        public ModManifest Manifest { get; private set; }

        /// <summary>
        /// The directory path where the mod is located
        /// </summary>
        public string FolderPath { get; private set; }

        /// <summary>
        /// Flag indicating if the mod is loaded successfully
        /// </summary>
        public bool IsLoaded { get; private set; }

        /// <summary>
        /// The mod's folder name (used as the mod ID)
        /// </summary>
        public string FolderName => Path.GetFileName(FolderPath);

        /// <summary>
        /// Collection of all loaded scripts in this mod
        /// </summary>
        public List<LuaScript> Scripts { get; private set; } = new List<LuaScript>();

        /// <summary>
        /// Dictionary of functions and values this mod exports for other mods to use
        /// </summary>
        private Dictionary<string, object> Exports { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Main script of the mod (the entry point)
        /// </summary>
        private LuaScript _mainScript;

        /// <summary>
        /// Creates a new mod instance from a folder path and manifest
        /// </summary>
        public LuaMod(string folderPath, ModManifest manifest, MelonLogger.Instance logger)
        {
            FolderPath = folderPath;
            Manifest = manifest;
            _logger = logger;
        }

        /// <summary>
        /// Adds a loaded script to this mod
        /// </summary>
        public void AddScript(LuaScript script)
        {
            Scripts.Add(script);

            // If this is the main script, store a reference to it
            if (Path.GetFileName(script.FilePath) == Manifest.Main)
            {
                _mainScript = script;
            }
        }

        /// <summary>
        /// Exports a function or value under the specified name for other mods to use
        /// </summary>
        public void SetExport(string key, object value)
        {
            Exports[key] = value;
        }

        /// <summary>
        /// Gets an exported function or value by name
        /// </summary>
        public object GetExport(string key)
        {
            if (Exports.TryGetValue(key, out object value))
                return value;
            return null;
        }

        /// <summary>
        /// Gets all export names from this mod
        /// </summary>
        public string[] GetExportNames()
        {
            string[] keys = new string[Exports.Count];
            Exports.Keys.CopyTo(keys, 0);
            return keys;
        }

        /// <summary>
        /// Mark this mod as loaded
        /// </summary>
        public void SetLoaded()
        {
            IsLoaded = true;
        }

        /// <summary>
        /// Initialize all scripts in this mod
        /// </summary>
        public bool Initialize()
        {
            // Prevent double initialization
            if (_initialized)
            {
                LuaUtility.Log($"Mod {Manifest.Name} already initialized, skipping");
                return true;
            }

            // Prevent recursive initialization
            if (_initializing)
                return true;

            _initializing = true;
            LuaUtility.Log($"Initializing mod: {Manifest.Name} v{Manifest.Version} by {Manifest.Author}");

            bool success = true;

            // Save current Lua context
            Script scriptEngine = Scripts.FirstOrDefault()?.GetScriptEngine();
            if (scriptEngine == null)
            {
                LuaUtility.LogError($"No script engine available for mod {Manifest.Name}");
                _initializing = false;
                return false;
            }

            // Store original global values
            DynValue origModName = scriptEngine.Globals.Get("MOD_NAME");
            DynValue origModPath = scriptEngine.Globals.Get("MOD_PATH");
            DynValue origModVersion = scriptEngine.Globals.Get("MOD_VERSION");
            DynValue origScriptPath = scriptEngine.Globals.Get("SCRIPT_PATH");

            try
            {
                // First, find and initialize the main script
                var mainScript = Scripts.Find(s => Path.GetFileName(s.FilePath) == Manifest.Main);
                if (mainScript != null)
                {
                    _mainScript = mainScript;

                    // Set correct context for main script
                    scriptEngine.Globals["MOD_NAME"] = FolderName;
                    scriptEngine.Globals["MOD_PATH"] = FolderPath;
                    scriptEngine.Globals["MOD_VERSION"] = Manifest.Version;
                    scriptEngine.Globals["SCRIPT_PATH"] = mainScript.FilePath;

                    if (!mainScript.Initialize())
                    {
                        LuaUtility.LogError($"Failed to initialize main script {mainScript.Name} in mod {Manifest.Name}");
                        success = false;
                    }
                }
                else
                {
                    LuaUtility.LogError($"Could not find main script {Manifest.Main} in mod {Manifest.Name}");
                    success = false;
                }

                // Only initialize other scripts if they don't have the same name as modules required by the main script
                if (success)
                {
                    foreach (var script in Scripts)
                    {
                        // Skip the main script (already initialized)
                        if (script == mainScript)
                            continue;

                        // Skip scripts that are likely loaded via require() from the main script
                        string scriptName = Path.GetFileNameWithoutExtension(script.FilePath);
                        if (Manifest.Files.Contains(scriptName + ".lua"))
                        {
                            // These scripts are initialized when required, not needed here
                            continue;
                        }

                        // Set correct context for this script
                        scriptEngine.Globals["MOD_NAME"] = FolderName;
                        scriptEngine.Globals["MOD_PATH"] = FolderPath;
                        scriptEngine.Globals["MOD_VERSION"] = Manifest.Version;
                        scriptEngine.Globals["SCRIPT_PATH"] = script.FilePath;

                        if (!script.Initialize())
                        {
                            LuaUtility.LogError($"Failed to initialize script {script.Name} in mod {Manifest.Name}");
                            success = false;
                        }
                    }
                }
            }
            finally
            {
                // Restore original global values
                scriptEngine.Globals["MOD_NAME"] = origModName;
                scriptEngine.Globals["MOD_PATH"] = origModPath;
                scriptEngine.Globals["MOD_VERSION"] = origModVersion;
                scriptEngine.Globals["SCRIPT_PATH"] = origScriptPath;

                _initialized = success;
                _initializing = false;
            }

            if (success)
            {
                LuaUtility.Log($"Successfully initialized mod: {Manifest.Name}");
            }

            return success;
        }

        /// <summary>
        /// Calls Update on all scripts in this mod
        /// </summary>
        public void Update()
        {
            if (!_initialized)
                return;

            foreach (var script in Scripts)
            {
                script.Update();
            }
        }

        /// <summary>
        /// Trigger an event on all scripts of this mod
        /// </summary>
        public void TriggerEvent(string eventName, params object[] args)
        {
            if (!_initialized)
                return;

            // First, trigger on main script to maintain compatibility
            if (_mainScript != null)
            {
                _mainScript.TriggerEvent(eventName, args);
            }

            // Then trigger on all other scripts
            foreach (var script in Scripts)
            {
                // Skip main script as we already triggered it
                if (script == _mainScript)
                    continue;

                // Trigger the event on this script
                script.TriggerEvent(eventName, args);
            }
        }

        /// <summary>
        /// Gets a friendly string representation of this mod
        /// </summary>
        public override string ToString()
        {
            return $"{Manifest.Name} v{Manifest.Version} by {Manifest.Author}";
        }

        /// <summary>
        /// Loads and initializes a module by name from this mod's files
        /// </summary>
        public DynValue LoadModule(string moduleName)
        {
            try
            {
                // Check if module exists in this mod
                string moduleFileName = moduleName + ".lua";
                string modulePath = Path.Combine(FolderPath, moduleFileName);

                if (!File.Exists(modulePath) || !Manifest.Files.Contains(moduleFileName))
                {
                    LuaUtility.LogWarning($"Module {moduleName} not found in mod {Manifest.Name}");
                    return DynValue.Nil;
                }

                // Find if the module script is already loaded
                var script = Scripts.Find(s => Path.GetFileNameWithoutExtension(s.FilePath) == moduleName);
                if (script != null)
                {
                    // Return module from script's result if available
                    var moduleValue = script.GetModuleExport();
                    if (moduleValue != null && !moduleValue.IsNil())
                    {
                        return moduleValue;
                    }
                }

                // If we get here, we need to require the module
                var scriptEngine = Scripts.FirstOrDefault()?.GetScriptEngine();
                if (scriptEngine != null)
                {
                    // Use the require function
                    object[] args = new object[] { moduleName };
                    return scriptEngine.Call(scriptEngine.Globals["require"], args);
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error loading module {moduleName} in mod {Manifest.Name}: {ex.Message}");
            }

            return DynValue.Nil;
        }

        /// <summary>
        /// Reloads the mod by reloading all of its scripts and re-initializing
        /// </summary>
        public bool Reload()
        {
            if (!IsLoaded)
                return false;

            LuaUtility.Log($"Reloading mod: {Manifest.Name}");

            _initialized = false;
            _initializing = false;

            // Clear event handlers and reset state
            var oldScripts = new List<LuaScript>(Scripts);
            Scripts.Clear();

            // Reload main script first
            string mainScriptPath = Path.Combine(FolderPath, Manifest.Main);
            if (!File.Exists(mainScriptPath))
            {
                LuaUtility.LogError($"Main script {Manifest.Main} not found for mod {Manifest.Name}.");
                return false;
            }

            // Load main script
            var script = oldScripts.Find(s => Path.GetFileName(s.FilePath) == Manifest.Main);
            if (script == null || !script.Reload())
            {
                LuaUtility.LogError($"Failed to reload main script for mod {Manifest.Name}.");
                return false;
            }

            Scripts.Add(script);
            _mainScript = script;

            // Reload additional files
            foreach (var file in Manifest.Files)
            {
                var filePath = Path.Combine(FolderPath, file);
                if (!File.Exists(filePath))
                {
                    LuaUtility.LogWarning($"Script file {file} not found for mod {Manifest.Name}.");
                    continue;
                }

                var existingScript = oldScripts.Find(s => s.FilePath == filePath);
                if (existingScript != null)
                {
                    if (existingScript.Reload())
                    {
                        Scripts.Add(existingScript);
                    }
                    else
                    {
                        LuaUtility.LogError($"Failed to reload script {file} for mod {Manifest.Name}.");
                    }
                }
            }

            // Re-initialize the mod
            return Initialize();
        }
    }
}