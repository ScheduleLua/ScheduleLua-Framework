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

// Define version constant
[assembly: MelonInfo(typeof(ScheduleLua.Core), "ScheduleLua", ScheduleLua.Core.ModVersion, "Bars", null)]
[assembly: MelonGame("TVGS", "Schedule I")]
namespace ScheduleLua;

public class Core : MelonMod
{
    // Version constant that can be used in both the MelonInfo attribute and exposed to Lua
    public const string ModVersion = "0.1.2";
    
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

    public override void OnInitializeMelon()
    {
        _instance = this;
        LoggerInstance.Msg("Initializing ScheduleLua...");

        SetupPreferences();
        InitializeLuaEngine();
        SetupScriptsDirectory();
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

        // Register game-specific API
        LuaAPI.RegisterAPI(_luaEngine);

        // Expose mod version to Lua
        _luaEngine.Globals["SCHEDULELUA_VERSION"] = ModVersion;
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
                        LoggerInstance.Msg($"Registered script from file system: {scriptName}");
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

        foreach (string file in Directory.GetFiles(_scriptsDirectory, "*.lua", SearchOption.AllDirectories))
        {
            LoadScript(file);
        }

        LoggerInstance.Msg($"Loaded {_loadedScripts.Count} Lua scripts.");

        // Initialize all loaded scripts
        InitializeScripts();
    }

    private void LoadScript(string filePath)
    {
        try
        {
            string relativePath = filePath.Replace(_scriptsDirectory, "").TrimStart('\\', '/');
            LoggerInstance.Msg($"Loading script: {relativePath}");

            var script = new LuaScript(filePath, _luaEngine, LoggerInstance);
            if (script.Load())
            {
                _loadedScripts[filePath] = script;
                LoggerInstance.Msg($"Successfully loaded script: {script.Name}");
            }
        }
        catch (MoonSharp.Interpreter.InterpreterException luaEx)
        {
            string scriptName = Path.GetFileName(filePath);
            LogDetailedError(luaEx, $"Error loading script {scriptName}", filePath);
        }
        catch (Exception ex)
        {
            if (_prefLogScriptErrors.Value)
            {
                LoggerInstance.Error($"Error loading script {filePath}: {ex.Message}");
                LoggerInstance.Error(ex.StackTrace);
            }
        }
    }

    /// <summary>
    /// Log detailed error information for Lua script errors
    /// </summary>
    private void LogDetailedError(MoonSharp.Interpreter.InterpreterException luaEx, string context, string filePath)
    {
        if (!_prefLogScriptErrors.Value)
            return;

        try
        {
            string scriptName = Path.GetFileName(filePath);
            string scriptContent = null;

            try
            {
                scriptContent = File.ReadAllText(filePath);
            }
            catch
            {
                // Ignore errors reading the file
            }

            string errorMessage = luaEx.DecoratedMessage;
            LoggerInstance.Error($"{context}: {errorMessage}");

            // Extract line number if possible
            int lineNumber = -1;
            foreach (var part in errorMessage.Split(':'))
            {
                if (int.TryParse(part.Trim(), out int line))
                {
                    lineNumber = line;
                    break;
                }
            }

            // Output code snippet
            if (lineNumber > 0 && scriptContent != null)
            {
                string[] lines = scriptContent.Split('\n');
                if (lineNumber <= lines.Length)
                {
                    LoggerInstance.Error($"Error on line {lineNumber}:");

                    int startLine = Math.Max(0, lineNumber - 3);
                    int endLine = Math.Min(lines.Length - 1, lineNumber + 2);

                    for (int i = startLine; i <= endLine; i++)
                    {
                        string prefix = (i + 1 == lineNumber) ? ">>>" : "   ";
                        LoggerInstance.Error($"{prefix} {i + 1}: {lines[i].TrimEnd()}");
                    }
                }
            }

            // Output stack trace
            LoggerInstance.Error("Stack trace:");
            if (luaEx.CallStack != null && luaEx.CallStack.Count > 0)
            {
                foreach (var frame in luaEx.CallStack)
                {
                    string funcName = !string.IsNullOrEmpty(frame.Name) ? frame.Name : "<anonymous>";
                    LoggerInstance.Error($"  at {funcName} in {frame.Location}");
                }
            }
            else
            {
                LoggerInstance.Error($"  at {scriptName}:{lineNumber}");
            }

            // Special handling for nil value errors
            if (errorMessage.Contains("attempt to call a nil value"))
            {
                LoggerInstance.Error("This error occurs when trying to call a function that doesn't exist.");
                LoggerInstance.Error("Check for:");
                LoggerInstance.Error("1. Misspelled function names");
                LoggerInstance.Error("2. Functions defined in the wrong scope");
                LoggerInstance.Error("3. Missing API functions or libraries");

                // Try to extract variable name
                if (errorMessage.Contains("global '"))
                {
                    int start = errorMessage.IndexOf("global '") + 8;
                    int end = errorMessage.IndexOf("'", start);
                    if (end > start)
                    {
                        string varName = errorMessage.Substring(start, end - start);
                        LoggerInstance.Error($"The undefined function appears to be: '{varName}'");

                        // List available API functions that might be similar
                        LoggerInstance.Error("Available global functions with similar names:");
                        int matchesFound = 0;
                        foreach (var key in _luaEngine.Globals.Keys)
                        {
                            string keyStr = key.ToString();
                            if (keyStr.Contains(varName) || (varName.Length > 3 &&
                                LevenshteinDistance(keyStr, varName) <= 3))
                            {
                                DynValue val = _luaEngine.Globals.Get(keyStr);
                                LoggerInstance.Error($"  {keyStr} (type: {val.Type})");
                                matchesFound++;

                                if (matchesFound >= 5)
                                    break;
                            }
                        }

                        if (matchesFound == 0)
                        {
                            LoggerInstance.Error("  No similar function names found.");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Fallback if detailed error reporting fails
            LoggerInstance.Error($"{context}: {luaEx.Message}");
            LoggerInstance.Error($"Error while generating detailed error report: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculate Levenshtein distance between two strings (for finding similar names)
    /// </summary>
    private int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s))
            return string.IsNullOrEmpty(t) ? 0 : t.Length;
        if (string.IsNullOrEmpty(t))
            return s.Length;

        int[] v0 = new int[t.Length + 1];
        int[] v1 = new int[t.Length + 1];

        for (int i = 0; i < v0.Length; i++)
            v0[i] = i;

        for (int i = 0; i < s.Length; i++)
        {
            v1[0] = i + 1;
            for (int j = 0; j < t.Length; j++)
            {
                int cost = s[i] == t[j] ? 0 : 1;
                v1[j + 1] = Math.Min(Math.Min(v1[j] + 1, v0[j + 1] + 1), v0[j] + cost);
            }

            for (int j = 0; j < v0.Length; j++)
                v0[j] = v1[j];
        }

        return v1[t.Length];
    }

    private void InitializeScripts()
    {
        foreach (var script in _loadedScripts.Values)
        {
            try
            {
                if (script.Initialize())
                {
                    LoggerInstance.Msg($"Initialized script: {script.Name}");
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
        _fileWatcher = new FileSystemWatcher
        {
            Path = _scriptsDirectory,
            Filter = "*.lua",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        _fileWatcher.Changed += OnScriptFileChanged;
        _fileWatcher.Created += OnScriptFileChanged;

        LoggerInstance.Msg("File watcher initialized for hot reloading.");
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

    public override void OnUpdate()
    {
        // Process any pending script reloads on the main thread
        if (_pendingScriptReloads.Count > 0)
        {
            string filePath = null;

            lock (_queueLock)
            {
                if (_pendingScriptReloads.Count > 0)
                {
                    filePath = _pendingScriptReloads.Dequeue();
                }
            }

            if (filePath != null)
            {
                ReloadScript(filePath);
            }
        }

        // Call Update on all initialized scripts
        foreach (var script in _loadedScripts.Values)
        {
            if (script.IsInitialized)
            {
                script.Update();
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
    }

    public override void OnDeinitializeMelon()
    {
        // Clean up resources
        if (_fileWatcher != null)
        {
            _fileWatcher.EnableRaisingEvents = false;
            _fileWatcher.Changed -= OnScriptFileChanged;
            _fileWatcher.Created -= OnScriptFileChanged;
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