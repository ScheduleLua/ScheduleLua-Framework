using MelonLoader;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using System.Reflection;
using ScheduleOne.GameTime;
using ScheduleOne.PlayerScripts;
using UnityEngine.Events;
using MelonLoader.Utils;
using ScheduleLua.API.Registry;
using ScheduleLua.API.Core;

// Define version constant
[assembly: MelonInfo(typeof(ScheduleLua.Core), "ScheduleLua", ScheduleLua.Core.ModVersion, "Bars", null)]
[assembly: MelonGame("TVGS", "Schedule I")]
namespace ScheduleLua;

public class Core : MelonMod
{
    // Version constant that can be used in both the MelonInfo attribute and exposed to Lua
    public const string ModVersion = "0.1.6";

    private static Core _instance;
    public static Core Instance => _instance;

    // Lua script engine
    public Script _luaEngine;

    // File watcher for hot reloading
    private FileSystemWatcher _fileWatcher;

    // Scripts directory
    private string _scriptsDirectory;

    // Loaded scripts collection
    public Dictionary<string, LuaScript> _loadedScripts = new Dictionary<string, LuaScript>();

    // MelonPreferences
    private static MelonPreferences_Category _prefCategory;
    private static MelonPreferences_Entry<bool> _prefEnableHotReload;
    private static MelonPreferences_Entry<bool> _prefLogScriptErrors;

    private bool _playerEventsBound = false;
    private bool _playerReadyTriggered = false;
    public bool _consoleReadyTriggered = false;

    // GUI callback event
    public event Action OnGUICallback;

    // Add GUI initialization flag
    private bool _guiInitialized = false;

    // Add this queue for pending script reloads
    private Queue<string> _pendingScriptReloads = new Queue<string>();
    private object _queueLock = new object(); // Lock for thread safety

    // Add ModManager to class variables
    public API.Mods.ModManager _modManager;

    public override void OnInitializeMelon()
    {
        _instance = this;
        LoggerInstance.Msg("Initializing ScheduleLua...");

        SetupPreferences();
        SetupScriptsDirectory();
        InitializeLuaEngine();
        LoadScripts();

        // Only setup file watcher if hot reload is enabled
        if (_prefEnableHotReload.Value)
        {
            SetupFileWatcher();
        }

        // Hook into game events
        HookGameEvents();

        // Make sure GUI is initialized when the mod loads
        InitializeGUI();

        LoggerInstance.Msg("ScheduleLua initialized successfully.");
    }

    private void SetupPreferences()
    {
        _prefCategory = MelonPreferences.CreateCategory("ScheduleLua");
        _prefEnableHotReload = _prefCategory.CreateEntry("EnableHotReload", true, "Enable Hot Reload", "Automatically reload scripts when they are modified");
        _prefLogScriptErrors = _prefCategory.CreateEntry("LogScriptErrors", true, "Log Script Errors", "Log detailed error messages when scripts fail");
    }

    private void InitializeLuaEngine()
    {
        LoggerInstance.Msg("Initializing Lua engine...");

        // Register Unity types with MoonSharp
        UserData.RegisterAssembly();

        // Set up script loader for better compatibility with IL2CPP/AOT platforms
        SetupScriptLoader();

        // Create Lua script environment
        _luaEngine = new Script(CoreModules.Preset_Complete);

        // Enable the debugger for better error reporting
        _luaEngine.DebuggerEnabled = true;

        // Use the configured script loader
        _luaEngine.Options.ScriptLoader = Script.DefaultOptions.ScriptLoader;

        // Initialize Unity type proxies for better IL2CPP/AOT compatibility
        API.Core.UnityTypeProxies.Initialize();

        // Initialize GUI system
        InitializeGUI();

        // Register Lua API
        LuaAPI.RegisterAPI(_luaEngine);

        // Initialize Mod Manager and register Mods API
        _modManager = new API.Mods.ModManager(LoggerInstance, _luaEngine, _scriptsDirectory);
        API.Mods.ModsAPI.RegisterAPI(_luaEngine, _modManager);
    }

    /// <summary>
    /// Sets up the script loader with explicit registration of script files
    /// This improves compatibility on IL2CPP and AOT platforms
    /// </summary>
    private void SetupScriptLoader()
    {
        // Load all Lua scripts from Resources folder
        Dictionary<string, string> scripts = new Dictionary<string, string>();

        try
        {
            // Load scripts from Resources/Lua folder
            object[] result = Resources.LoadAll("Lua", typeof(TextAsset));

            foreach (TextAsset ta in result)
            {
                scripts.Add(ta.name, ta.text);
                LoggerInstance.Msg($"Registered script from resources: {ta.name}");
            }

            // Also register scripts from file system if they exist
            if (Directory.Exists(_scriptsDirectory))
            {
                foreach (string filePath in Directory.GetFiles(_scriptsDirectory, "*.lua", SearchOption.AllDirectories))
                {
                    string scriptName = Path.GetFileNameWithoutExtension(filePath);
                    string scriptText = File.ReadAllText(filePath);

                    // Don't overwrite resources scripts with same name
                    if (!scripts.ContainsKey(scriptName))
                    {
                        scripts.Add(scriptName, scriptText);
                    }
                }
            }

            // Set the script loader
            Script.DefaultOptions.ScriptLoader = new MoonSharp.Interpreter.Loaders.UnityAssetsScriptLoader(scripts);
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"Error setting up script loader: {ex.Message}");

            // Fall back to standard script loader
            Script.DefaultOptions.ScriptLoader = new MoonSharp.Interpreter.Loaders.FileSystemScriptLoader();
        }
    }

    private void SetupScriptsDirectory()
    {
        // Create scripts directory if it doesn't exist
        _scriptsDirectory = Path.Combine(MelonEnvironment.ModsDirectory, "ScheduleLua", "Scripts");

        if (!Directory.Exists(_scriptsDirectory))
        {
            Directory.CreateDirectory(_scriptsDirectory);
        }

        LoggerInstance.Msg($"Scripts directory: {_scriptsDirectory}");
    }

    private void LoadScripts()
    {
        if (!Directory.Exists(_scriptsDirectory))
            return;

        LoggerInstance.Msg("Loading Lua scripts...");

        // First, discover and load mods
        _modManager.DiscoverAndLoadMods();

        // Then load individual scripts (excluding those in mod folders with manifest.json)
        int individualScriptsCount = 0;
        foreach (string file in Directory.GetFiles(_scriptsDirectory, "*.lua", SearchOption.TopDirectoryOnly))
        {
            if (LoadScript(file))
                individualScriptsCount++;
        }

        // Load scripts from subdirectories that don't contain manifest.json
        foreach (string dir in Directory.GetDirectories(_scriptsDirectory))
        {
            // Skip directories with manifest.json (they are mods and already loaded)
            if (File.Exists(Path.Combine(dir, "manifest.json")))
                continue;

            // Load all Lua files from this subdirectory
            foreach (string file in Directory.GetFiles(dir, "*.lua", SearchOption.AllDirectories))
            {
                if (LoadScript(file))
                    individualScriptsCount++;
            }
        }

        LoggerInstance.Msg($"Loaded {individualScriptsCount} individual Lua scripts.");

        // Initialize all loaded individual scripts
        InitializeScripts();

        // Initialize all loaded mods
        _modManager.InitializeMods();
    }

    private bool LoadScript(string filePath)
    {
        try
        {
            // Check if this script is already handled by the mod system
            if (_modManager.IsScriptPathProcessed(filePath))
            {
                // Script is already managed by the mod system, don't load it again
                return false;
            }

            string relativePath = filePath.Replace(_scriptsDirectory, "").TrimStart('\\', '/');

            // Set the correct script path in global context before loading
            _luaEngine.Globals["SCRIPT_PATH"] = filePath;

            var script = new LuaScript(filePath, _luaEngine, LoggerInstance);
            if (script.Load())
            {
                _loadedScripts[filePath] = script;
                LoggerInstance.Msg($"Successfully loaded script: {script.Name}");
                return true;
            }
        }
        catch (MoonSharp.Interpreter.InterpreterException luaEx)
        {
            string scriptName = Path.GetFileName(filePath);
            LuaUtility.LogError($"Error loading script {scriptName}", luaEx);
        }
        catch (Exception ex)
        {
            if (_prefLogScriptErrors.Value)
            {
                LoggerInstance.Error($"Error loading script {filePath}: {ex.Message}");
            }
        }
        return false;
    }

    private void InitializeScripts()
    {
        // Track how many scripts were initialized
        int initializedCount = 0;

        foreach (var script in _loadedScripts.Values)
        {
            // Skip scripts that are handled by the mod system
            string scriptPath = script.FilePath;
            if (_modManager.IsScriptPathProcessed(scriptPath))
            {
                // LoggerInstance.Msg($"Skipping initialization of mod-managed script: {script.Name}");
                continue;
            }

            try
            {
                // Set the correct script path in global context before initializing
                _luaEngine.Globals["SCRIPT_PATH"] = scriptPath;

                if (script.Initialize())
                {
                    initializedCount++;
                    // LoggerInstance.Msg($"Initialized script: {script.Name}");
                }
                else
                {
                    LoggerInstance.Warning($"Failed to initialize script: {script.Name}");
                }
            }
            catch (Exception ex)
            {
                if (_prefLogScriptErrors.Value)
                {
                    LoggerInstance.Error($"Error initializing script {script.Name}: {ex.Message}");
                }
            }
        }

        LoggerInstance.Msg($"Initialized {initializedCount} individual scripts.");
    }

    private void ReloadScript(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return;

            string relativePath = filePath.Replace(_scriptsDirectory, "").TrimStart('\\', '/');

            // If script is already loaded, reload it
            if (_loadedScripts.TryGetValue(filePath, out LuaScript script))
            {
                // Set the correct script path in global context before reloading
                _luaEngine.Globals["SCRIPT_PATH"] = filePath;

                if (script.Reload())
                {
                    LoggerInstance.Msg($"Reloaded script: {script.Name}");
                }
                else
                {
                    LoggerInstance.Warning($"Failed to reload script: {script.Name}");
                }
            }
            else
            {
                // New script, load it
                LoadScript(filePath);
            }
        }
        catch (Exception ex)
        {
            if (_prefLogScriptErrors.Value)
            {
                LoggerInstance.Error($"Error reloading script {filePath}: {ex.Message}");
            }
        }
    }

    private void SetupFileWatcher()
    {
        try
        {
            // Create a file watcher for the scripts directory
            _fileWatcher = new FileSystemWatcher
            {
                Path = _scriptsDirectory,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = "*.lua",
                IncludeSubdirectories = true
            };

            // Hook up events
            _fileWatcher.Changed += OnScriptFileChanged;
            _fileWatcher.Created += OnScriptFileChanged;
            _fileWatcher.Renamed += OnScriptFileChanged;

            // Also watch for changes to manifest.json files
            var manifestWatcher = new FileSystemWatcher
            {
                Path = _scriptsDirectory,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                Filter = "manifest.json",
                IncludeSubdirectories = true
            };

            manifestWatcher.Changed += OnManifestFileChanged;
            manifestWatcher.Created += OnManifestFileChanged;

            // Start watching
            _fileWatcher.EnableRaisingEvents = true;
            manifestWatcher.EnableRaisingEvents = true;

            LoggerInstance.Msg("Hot reload enabled: Script changes will be automatically applied");
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"Error setting up file watcher: {ex.Message}");
        }
    }

    private void OnScriptFileChanged(object sender, FileSystemEventArgs e)
    {
        // Instead of reloading immediately, queue the file for reload on the main thread
        try
        {
            // Wait to ensure file is not locked
            System.Threading.Thread.Sleep(100);

            // Queue the file for reload
            lock (_queueLock)
            {
                // Check if file already queued to avoid duplicates
                if (!_pendingScriptReloads.Contains(e.FullPath))
                {
                    _pendingScriptReloads.Enqueue(e.FullPath);
                    LoggerInstance.Msg($"Script queued for reload: {Path.GetFileName(e.FullPath)}");
                }
            }
        }
        catch (Exception ex)
        {
            if (_prefLogScriptErrors.Value)
            {
                LoggerInstance.Error($"Error in file watcher: {ex.Message}");
            }
        }
    }

    private void OnManifestFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            // Wait to ensure file is not locked
            System.Threading.Thread.Sleep(100);

            string modFolder = Path.GetDirectoryName(e.FullPath);
            string modName = Path.GetFileName(modFolder);

            LoggerInstance.Msg($"Manifest changed for mod: {modName}, queuing full mod reload");

            // Currently we'll just tell the user that the mod needs to be reloaded
            // A more sophisticated approach would reload the entire mod
            LoggerInstance.Warning($"Changes to manifest.json require a game restart to take effect fully.");
        }
        catch (Exception ex)
        {
            if (_prefLogScriptErrors.Value)
            {
                LoggerInstance.Error($"Error handling manifest change: {ex.Message}");
            }
        }
    }

    public override void OnUpdate()
    {
        // Process queued script reloads
        lock (_queueLock)
        {
            while (_pendingScriptReloads.Count > 0)
            {
                string filePath = _pendingScriptReloads.Dequeue();
                ReloadScript(filePath);
            }
        }

        // Call Update on all loaded scripts
        foreach (var script in _loadedScripts.Values)
        {
            if (script.IsLoaded && script.IsInitialized)
            {
                script.Update();
            }
        }

        // Call Update on all loaded mods
        _modManager.UpdateMods();

        // Only monitor player health and energy if the flag is set
        if (_isMonitoring && _playerReadyTriggered && Player.Local != null)
        {
            // Check health
            float currentHealth = API.PlayerAPI.GetPlayerHealth();
            if (currentHealth != _lastHealthValue)
            {
                TriggerEvent("OnPlayerHealthChanged", currentHealth);
                _lastHealthValue = currentHealth;
            }

            // Check energy
            float currentEnergy = API.PlayerAPI.GetPlayerEnergy();
            if (currentEnergy != _lastEnergyValue)
            {
                TriggerEvent("OnPlayerEnergyChanged", currentEnergy);
                _lastEnergyValue = currentEnergy;
            }
        }
    }

    /// <summary>
    /// Hook into game events
    /// </summary>
    private void HookGameEvents()
    {
        try
        {
            // Only hook events if we're in the main game scene
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Main")
            {
                // LoggerInstance.Msg("Not in main game scene, skipping game event hooks");
                return;
            }

            // Time events
            if (TimeManager.Instance != null)
            {
                TimeManager timeManager = TimeManager.Instance;

                // Day change event
                timeManager.onDayPass = (Action)Delegate.Combine(timeManager.onDayPass, new Action(() => {
                    TriggerEvent("OnDayChanged", timeManager.CurrentDay.ToString());
                }));

                // Time change event
                timeManager.onHourPass = (Action)Delegate.Combine(timeManager.onHourPass, new Action(() => {
                    TriggerEvent("OnTimeChanged", timeManager.CurrentTime);
                }));

                // Sleep events
                if (timeManager._onSleepStart != null)
                {
                    timeManager._onSleepStart.AddListener(() => {
                        TriggerEvent("OnSleepStart");
                    });
                }

                if (timeManager._onSleepEnd != null)
                {
                    timeManager._onSleepEnd.AddListener(new UnityEngine.Events.UnityAction(OnSleepEndHandler));
                }
            }
            else
            {
                LoggerInstance.Warning("TimeManager not found, time events won't be triggered");
            }

            LoggerInstance.Msg("Core game events hooked successfully");
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"Error hooking game events: {ex.Message}");
        }
    }

    /// <summary>
    /// Hook into player events - can be called multiple times until successful
    /// </summary>
    private void HookPlayerEvents()
    {
        if (_playerEventsBound)
            return;

        if (Player.Local != null)
        {
            // Health change - try to find and attach to appropriate events
            if (Player.Local.Health != null)
            {
                try
                {
                    // Try to subscribe to health events
                    AttachToPlayerHealthEvents(Player.Local.Health);
                }
                catch (Exception ex)
                {
                    LoggerInstance.Warning($"Could not attach to player health events: {ex.Message}");
                }
            }

            // Energy change - try to find and attach to appropriate events
            if (Player.Local.Energy != null)
            {
                try
                {
                    // Try to subscribe to energy events
                    AttachToPlayerEnergyEvents(Player.Local.Energy);
                }
                catch (Exception ex)
                {
                    LoggerInstance.Warning($"Could not attach to player energy events: {ex.Message}");
                }
            }

            _playerEventsBound = true;
            LoggerInstance.Msg("Player events bound successfully");
        }
        else
        {
            LoggerInstance.Warning("Local player not found, will attempt to bind player events later");
        }
    }

    private void OnSleepEndHandler()
    {
        TriggerEvent("OnSleepEnd");
    }

    private void AttachToPlayerHealthEvents(object playerHealth)
    {
        var healthValue = API.PlayerAPI.GetPlayerHealth();

        StartHealthMonitoring();
    }

    private void AttachToPlayerEnergyEvents(object playerEnergy)
    {
        var energyValue = API.PlayerAPI.GetPlayerEnergy();

        StartEnergyMonitoring();
    }
    private float _lastHealthValue = -1;
    private float _lastEnergyValue = -1;
    private bool _isMonitoring = false;

    private void StartHealthMonitoring()
    {
        if (!_isMonitoring)
        {
            _isMonitoring = true;
            _lastHealthValue = API.PlayerAPI.GetPlayerHealth();
            _lastEnergyValue = API.PlayerAPI.GetPlayerEnergy();
        }
    }

    private void StartEnergyMonitoring()
    {
        StartHealthMonitoring();
    }

    // Override OnLateUpdate to check for health/energy changes
    public override void OnLateUpdate()
    {
        base.OnLateUpdate();

        if (!_consoleReadyTriggered && ScheduleOne.Console.Commands.Count > 0 && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Main")
        {
            // LoggerInstance.Msg($"Console is ready with {ScheduleOne.Console.Commands.Count} commands, triggering OnConsoleReady event");
            _consoleReadyTriggered = true;

            LoggerInstance.Msg("Registering Lua backend commands");
            API.Registry.ScriptCommands.RegisterBackendCommands();
            TriggerEvent("OnConsoleReady");
        }

        // If player events aren't bound yet, try to bind them now
        if (!_playerEventsBound && Player.Local != null)
        {
            LoggerInstance.Msg("Player is now available, binding events...");
            HookPlayerEvents();

            // Trigger OnPlayerReady event once when player is available
            if (!_playerReadyTriggered)
            {
                // LoggerInstance.Msg("Player is ready, triggering OnPlayerReady event");
                _playerReadyTriggered = true;
                TriggerEvent("OnPlayerReady");
            }
        }

        if (_isMonitoring && Player.Local != null)
        {
            // Check health
            float currentHealth = API.PlayerAPI.GetPlayerHealth();
            if (currentHealth != _lastHealthValue)
            {
                TriggerEvent("OnPlayerHealthChanged", currentHealth);
                _lastHealthValue = currentHealth;
            }

            // Check energy
            float currentEnergy = API.PlayerAPI.GetPlayerEnergy();
            if (currentEnergy != _lastEnergyValue)
            {
                TriggerEvent("OnPlayerEnergyChanged", currentEnergy);
                _lastEnergyValue = currentEnergy;
            }
        }
    }

    /// <summary>
    /// OnGUI is called for rendering and handling GUI events
    /// </summary>
    public override void OnGUI()
    {
        // Trigger the OnGUI event for any subscribers (like UIAPI)
        if (_guiInitialized)
        {
            OnGUICallback?.Invoke();
        }
    }

    /// <summary>
    /// Trigger an event across all loaded scripts
    /// </summary>
    public void TriggerEvent(string eventName, params object[] args)
    {
        foreach (var script in _loadedScripts.Values)
        {
            if (script.IsInitialized)
            {
                script.TriggerEvent(eventName, args);
            }
        }

        // Also trigger the event for all mods
        if (_modManager != null)
        {
            _modManager.TriggerEvent(eventName, args);
        }
    }

    public override void OnDeinitializeMelon()
    {
        // Clean up resources
        if (_fileWatcher != null)
        {
            _fileWatcher.EnableRaisingEvents = false;
            _fileWatcher.Changed -= OnScriptFileChanged;
            _fileWatcher.Created -= OnScriptFileChanged;
            _fileWatcher.Renamed -= OnScriptFileChanged;
            _fileWatcher.Dispose();
        }

        // Unregister all Lua commands before exiting
        CommandRegistry.UnregisterAllCommands();

        LoggerInstance.Msg("ScheduleLua unloaded.");
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        base.OnSceneWasLoaded(buildIndex, sceneName);

        // Re-hook events when entering the main game scene
        if (sceneName == "Main")
        {
            LoggerInstance.Msg("Main scene loaded, hooking game events...");
            HookGameEvents();

            // Register backend commands if console is ready
            if (_consoleReadyTriggered)
            {
                LoggerInstance.Msg("Console is ready, registering Lua backend commands");
                API.Registry.ScriptCommands.RegisterBackendCommands();
            }
        }
        else if (sceneName == "Menu")
        {
            _playerEventsBound = false;
            _playerReadyTriggered = false;
            _consoleReadyTriggered = false;
        }
    }

    // Make sure GUI is initialized when the mod loads
    private void InitializeGUI()
    {
        if (_guiInitialized)
            return;

        LoggerInstance.Msg("Initializing GUI system...");
        _guiInitialized = true;
    }
}

// Helper class to track script information (no longer needed with LuaScript class)
public class ScriptInfo
{
    public string Path { get; set; }
    public DateTime LastModified { get; set; }
    public string Content { get; set; }
}