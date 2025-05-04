using MelonLoader;
using MoonSharp.Interpreter;
using ScheduleLua.API.Core;
using UnityEngine;

namespace ScheduleLua.Core.Framework.Mods.ManagerUI
{
    /// <summary>
    /// Controls the UI for managing Lua mods
    /// </summary>
    [MoonSharpUserData]
    public class ModManagerUIController
    {
        private ModManager _modManager;
        private bool _isVisible = false;
        private Rect _windowRect = new Rect(50, 50, 800, 750);
        private Vector2 _modListScrollPosition = Vector2.zero;
        private Vector2 _configScrollPosition = Vector2.zero;
        private LuaMod _selectedMod = null;
        private Dictionary<string, bool> _modEnabledStates = new Dictionary<string, bool>();
        private GUIStyle _headerStyle;
        private GUIStyle _modNameStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _modDescriptionStyle;
        private GUIStyle _configValueStyle;
        private GUIStyle _configLabelStyle;
        private GUIStyle _tabStyle;
        private GUIStyle _tabSelectedStyle;
        private Dictionary<string, object> _tempConfigValues = new Dictionary<string, object>();
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "Mods", "Settings" };
        private bool _stylesInitialized = false;

        // Configuration storage
        private ModManagerConfigStorage _configStorage;

        // Settings
        private bool _enableHotReload = true;
        private bool _logScriptErrors = true;

        private Dictionary<string, string> _tableNewEntryKeys = new Dictionary<string, string>();
        private Dictionary<string, string> _tableNewEntryValues = new Dictionary<string, string>();
        private Dictionary<string, int> _tableNewEntryTypes = new Dictionary<string, int>();

        private GUIStyle _windowStyle;

        /// <summary>
        /// Creates a new ModManagerUIController
        /// </summary>
        public ModManagerUIController(ModManager modManager, string scriptsDirectory)
        {
            _modManager = modManager;

            // Initialize config storage
            _configStorage = new ModManagerConfigStorage(scriptsDirectory);

            // Load settings from MelonPreferences
            var prefCategory = MelonPreferences.GetCategory("ScheduleLua");
            if (prefCategory != null)
            {
                var hotReloadPref = prefCategory.GetEntry<bool>("EnableHotReload");
                if (hotReloadPref != null)
                {
                    _enableHotReload = hotReloadPref.Value;
                }

                var logErrorsPref = prefCategory.GetEntry<bool>("LogScriptErrors");
                if (logErrorsPref != null)
                {
                    _logScriptErrors = logErrorsPref.Value;
                }
            }

            InitializeModStates();
        }

        /// <summary>
        /// Initialize the enabled/disabled state for each mod
        /// </summary>
        private void InitializeModStates()
        {
            foreach (var mod in _modManager.LoadedMods.Values)
            {
                // Get enabled state from config storage or default to enabled
                bool enabled = _configStorage.GetModEnabled(mod.FolderName);
                _modEnabledStates[mod.FolderName] = enabled;
            }
        }

        /// <summary>
        /// Creates a texture with a solid color
        /// </summary>
        private Texture2D CreateColorTexture(int width, int height, Color color)
        {
            Texture2D texture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Initialize the window style to ensure it doesn't get reset
        /// </summary>
        private void EnsureWindowStyle()
        {
            Color primaryColor = new Color(0.2f, 0.6f, 0.9f);

            if (_windowStyle == null)
            {
                _windowStyle = new GUIStyle(GUI.skin.window);
                Texture2D bgTexture = CreateColorTexture(2, 2, new Color(0.2f, 0.2f, 0.25f, 0.95f));

                // Set all state backgrounds to the same texture
                _windowStyle.normal.background = bgTexture;
                _windowStyle.onNormal.background = bgTexture;
                _windowStyle.active.background = bgTexture;
                _windowStyle.onActive.background = bgTexture;
                _windowStyle.focused.background = bgTexture;
                _windowStyle.onFocused.background = bgTexture;
                _windowStyle.hover.background = bgTexture;
                _windowStyle.onHover.background = bgTexture;

                // Set text color for all states
                _windowStyle.normal.textColor = primaryColor;
                _windowStyle.onNormal.textColor = primaryColor;
                _windowStyle.active.textColor = primaryColor;
                _windowStyle.onActive.textColor = primaryColor;
                _windowStyle.focused.textColor = primaryColor;
                _windowStyle.onFocused.textColor = primaryColor;
                _windowStyle.hover.textColor = primaryColor;
                _windowStyle.onHover.textColor = primaryColor;

                _windowStyle.border = new RectOffset(6, 6, 6, 6);
                _windowStyle.padding = new RectOffset(15, 15, 30, 15);
                _windowStyle.fontSize = 16;
                _windowStyle.fontStyle = FontStyle.Bold;
            }
        }

        /// <summary>
        /// Initialize the UI styles
        /// </summary>
        private void InitializeStyles()
        {
            if (_stylesInitialized)
                return;

            Color primaryColor = new Color(0.2f, 0.6f, 0.9f);
            Color secondaryColor = new Color(0.16f, 0.16f, 0.18f);
            Color lightColor = new Color(0.9f, 0.9f, 0.9f);
            Color accentColor = new Color(0.95f, 0.5f, 0.2f);

            // Create a custom box style for containers
            GUI.skin.box.normal.background = CreateColorTexture(2, 2, new Color(0.18f, 0.18f, 0.22f, 0.8f));
            GUI.skin.box.border = new RectOffset(4, 4, 4, 4);
            GUI.skin.box.margin = new RectOffset(5, 5, 5, 5);
            GUI.skin.box.padding = new RectOffset(10, 10, 10, 10);

            // Initialize the window style separately (moved to EnsureWindowStyle method)
            EnsureWindowStyle();

            // Update the toggle style
            GUI.skin.toggle.normal.textColor = lightColor;
            GUI.skin.toggle.fontSize = 14;
            GUI.skin.toggle.padding = new RectOffset(5, 5, 5, 5);

            // Update the label style
            GUI.skin.label.fontSize = 14;
            GUI.skin.label.normal.textColor = lightColor;

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 15, 15),
                normal = { textColor = primaryColor }
            };

            _modNameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = lightColor },
                hover = { textColor = accentColor },
                padding = new RectOffset(5, 5, 5, 5)
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(15, 15, 8, 8),
                margin = new RectOffset(5, 5, 5, 5),
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = lightColor, background = CreateColorTexture(2, 2, primaryColor) },
                hover = { textColor = Color.white, background = CreateColorTexture(2, 2, new Color(0.3f, 0.7f, 1f)) },
                active = { textColor = Color.white, background = CreateColorTexture(2, 2, new Color(0.1f, 0.5f, 0.8f)) }
            };

            _modDescriptionStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                fontSize = 12,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) },
                padding = new RectOffset(5, 5, 3, 3)
            };

            _configLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(5, 5, 10, 3),
                normal = { textColor = primaryColor }
            };

            _configValueStyle = new GUIStyle(GUI.skin.textField)
            {
                margin = new RectOffset(5, 5, 3, 10),
                padding = new RectOffset(8, 8, 6, 6),
                fontSize = 13,
                normal = { textColor = lightColor, background = CreateColorTexture(2, 2, secondaryColor) },
                focused = { textColor = Color.white, background = CreateColorTexture(2, 2, new Color(0.22f, 0.22f, 0.26f)) }
            };

            _tabStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(20, 20, 8, 8),
                margin = new RectOffset(2, 2, 0, 15),
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = lightColor, background = CreateColorTexture(2, 2, secondaryColor) },
                hover = { textColor = lightColor, background = CreateColorTexture(2, 2, new Color(0.22f, 0.22f, 0.26f)) },
                active = { textColor = Color.white, background = CreateColorTexture(2, 2, primaryColor) }
            };

            _tabSelectedStyle = new GUIStyle(_tabStyle)
            {
                normal = { textColor = Color.white, background = CreateColorTexture(2, 2, primaryColor) }
            };

            _stylesInitialized = true;
        }

        /// <summary>
        /// Toggle the visibility of the mod manager UI
        /// </summary>
        public void ToggleVisibility()
        {
            _isVisible = !_isVisible;

            if (_isVisible)
            {
                // Center the window when opening
                _windowRect.x = (Screen.width - _windowRect.width) / 2;
                _windowRect.y = (Screen.height - _windowRect.height) / 2;
            }
        }

        /// <summary>
        /// Check if the mod manager UI is visible
        /// </summary>
        public bool IsVisible()
        {
            return _isVisible;
        }

        /// <summary>
        /// Update the UI state
        /// </summary>
        public void Update()
        {
            // Process any background tasks if needed
        }

        /// <summary>
        /// Draw the UI state
        /// </summary>
        public void OnGUI()
        {
            if (!_isVisible)
                return;

            InitializeStyles();

            // Ensure the window style is always properly initialized
            EnsureWindowStyle();

            // Draw the window
            _windowRect = GUILayout.Window(
                123456, // Unique ID for this window
                _windowRect,
                DrawModManagerWindow,
                "ScheduleLua Manager",
                _windowStyle,
                GUILayout.Width(_windowRect.width),
                GUILayout.Height(_windowRect.height)
            );
        }

        /// <summary>
        /// Draw the mod manager window content
        /// </summary>
        private void DrawModManagerWindow(int windowID)
        {
            // Create a drag handle at the top of the window
            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 25));

            GUILayout.BeginVertical();

            // Draw tabs
            GUILayout.BeginHorizontal();
            for (int i = 0; i < _tabNames.Length; i++)
            {
                if (GUILayout.Button(_tabNames[i], i == _selectedTab ? _tabSelectedStyle : _tabStyle, GUILayout.Height(30)))
                {
                    _selectedTab = i;
                }
            }
            GUILayout.EndHorizontal();

            // Draw selected tab content
            switch (_selectedTab)
            {
                case 0:
                    DrawModsTab();
                    break;
                case 1:
                    DrawSettingsTab();
                    break;
            }

            GUILayout.EndVertical();
        }

        /// <summary>
        /// Draw the mods tab
        /// </summary>
        private void DrawModsTab()
        {
            GUILayout.Label("Installed Mods", _headerStyle);

            // Split view: Mod list on left, mod details/config on right
            GUILayout.BeginHorizontal();

            // Mod list (left panel)
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(250), GUILayout.ExpandHeight(true));
            DrawModList();
            GUILayout.EndVertical();

            // Mod details/config (right panel)
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (_selectedMod != null)
            {
                DrawModDetails();
            }
            else
            {
                GUIStyle noSelectionStyle = new GUIStyle(GUI.skin.label);
                noSelectionStyle.alignment = TextAnchor.MiddleCenter;
                noSelectionStyle.fontSize = 15;
                noSelectionStyle.normal.textColor = new Color(0.7f, 0.7f, 0.8f);

                GUILayout.FlexibleSpace();
                GUILayout.Label("Select a mod to view details", noSelectionStyle);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // Bottom buttons with improved styling
            GUILayout.Space(10);

            GUIStyle bottomBarStyle = new GUIStyle(GUIStyle.none);
            bottomBarStyle.padding = new RectOffset(5, 5, 8, 8);
            GUILayout.BeginHorizontal(bottomBarStyle);

            GUIStyle closeButtonStyle = new GUIStyle(_buttonStyle);
            closeButtonStyle.normal.background = CreateColorTexture(2, 2, new Color(0.6f, 0.2f, 0.2f));
            closeButtonStyle.hover.background = CreateColorTexture(2, 2, new Color(0.7f, 0.3f, 0.3f));
            closeButtonStyle.active.background = CreateColorTexture(2, 2, new Color(0.5f, 0.15f, 0.15f));

            if (GUILayout.Button("Close", closeButtonStyle, GUILayout.Height(30), GUILayout.Width(120)))
            {
                _isVisible = false;
            }

            GUILayout.FlexibleSpace();

            GUIStyle applyButtonStyle = new GUIStyle(_buttonStyle);
            applyButtonStyle.normal.background = CreateColorTexture(2, 2, new Color(0.2f, 0.6f, 0.3f));
            applyButtonStyle.hover.background = CreateColorTexture(2, 2, new Color(0.3f, 0.7f, 0.4f));
            applyButtonStyle.active.background = CreateColorTexture(2, 2, new Color(0.15f, 0.5f, 0.25f));

            if (GUILayout.Button("Apply Changes", applyButtonStyle, GUILayout.Height(30), GUILayout.Width(150)))
            {
                ApplyChanges();
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw the settings tab
        /// </summary>
        private void DrawSettingsTab()
        {
            GUILayout.Label("ScheduleLua Settings", _headerStyle);

            GUIStyle settingsBoxStyle = new GUIStyle(GUI.skin.box);
            settingsBoxStyle.margin = new RectOffset(0, 0, 10, 10);
            settingsBoxStyle.padding = new RectOffset(15, 15, 15, 15);

            GUILayout.BeginVertical(settingsBoxStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            // Create setting item background
            GUIStyle settingItemStyle = new GUIStyle(GUIStyle.none);
            settingItemStyle.normal.background = CreateColorTexture(2, 2, new Color(0.22f, 0.22f, 0.27f, 0.7f));
            settingItemStyle.margin = new RectOffset(0, 0, 0, 10);
            settingItemStyle.padding = new RectOffset(15, 15, 10, 10);

            // Toggle style
            GUIStyle toggleLabelStyle = new GUIStyle(GUI.skin.label);
            toggleLabelStyle.fontSize = 15;
            toggleLabelStyle.fontStyle = FontStyle.Bold;
            toggleLabelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

            // Description style
            GUIStyle settingDescStyle = new GUIStyle(_modDescriptionStyle);
            settingDescStyle.margin = new RectOffset(25, 5, 5, 5);

            // Hot reload setting
            GUILayout.BeginVertical(settingItemStyle);

            GUILayout.BeginHorizontal();
            _enableHotReload = GUILayout.Toggle(_enableHotReload, "", GUILayout.Width(20));
            GUILayout.Label("Enable Hot Reload", toggleLabelStyle);
            GUILayout.EndHorizontal();

            GUILayout.Label("Automatically reload scripts when they are modified", settingDescStyle);
            GUILayout.EndVertical();

            // Log errors setting
            GUILayout.BeginVertical(settingItemStyle);

            GUILayout.BeginHorizontal();
            _logScriptErrors = GUILayout.Toggle(_logScriptErrors, "", GUILayout.Width(20));
            GUILayout.Label("Log Script Errors", toggleLabelStyle);
            GUILayout.EndHorizontal();

            GUILayout.Label("Log detailed error messages when scripts fail", settingDescStyle);
            GUILayout.EndVertical();

            GUILayout.EndVertical();

            // Spacer
            GUILayout.FlexibleSpace();

            // Bottom buttons with improved styling
            GUILayout.Space(10);

            GUIStyle bottomBarStyle = new GUIStyle(GUIStyle.none);
            bottomBarStyle.padding = new RectOffset(5, 5, 8, 8);
            GUILayout.BeginHorizontal(bottomBarStyle);

            GUIStyle closeButtonStyle = new GUIStyle(_buttonStyle);
            closeButtonStyle.normal.background = CreateColorTexture(2, 2, new Color(0.6f, 0.2f, 0.2f));
            closeButtonStyle.hover.background = CreateColorTexture(2, 2, new Color(0.7f, 0.3f, 0.3f));
            closeButtonStyle.active.background = CreateColorTexture(2, 2, new Color(0.5f, 0.15f, 0.15f));

            if (GUILayout.Button("Close", closeButtonStyle, GUILayout.Height(30), GUILayout.Width(120)))
            {
                _isVisible = false;
            }

            GUILayout.FlexibleSpace();

            GUIStyle saveButtonStyle = new GUIStyle(_buttonStyle);
            saveButtonStyle.normal.background = CreateColorTexture(2, 2, new Color(0.2f, 0.6f, 0.3f));
            saveButtonStyle.hover.background = CreateColorTexture(2, 2, new Color(0.3f, 0.7f, 0.4f));
            saveButtonStyle.active.background = CreateColorTexture(2, 2, new Color(0.15f, 0.5f, 0.25f));

            if (GUILayout.Button("Save Settings", saveButtonStyle, GUILayout.Height(30), GUILayout.Width(150)))
            {
                SaveSettings();
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Save the mod manager settings
        /// </summary>
        private void SaveSettings()
        {
            // Update MelonPreferences
            var prefCategory = MelonLoader.MelonPreferences.GetCategory("ScheduleLua");
            if (prefCategory != null)
            {
                var hotReloadPref = prefCategory.GetEntry<bool>("EnableHotReload");
                if (hotReloadPref != null)
                {
                    hotReloadPref.Value = _enableHotReload;
                }

                var logErrorsPref = prefCategory.GetEntry<bool>("LogScriptErrors");
                if (logErrorsPref != null)
                {
                    logErrorsPref.Value = _logScriptErrors;
                }

                MelonLoader.MelonPreferences.Save();
                LuaUtility.Log("Mod manager settings saved successfully");
            }
        }

        /// <summary>
        /// Draw the mod list
        /// </summary>
        private void DrawModList()
        {
            // Add a title for the mod list section
            GUILayout.Label("Available Mods", _configLabelStyle);

            // Create a styled box for the scroll view
            GUILayout.BeginVertical(GUI.skin.box);

            _modListScrollPosition = GUILayout.BeginScrollView(_modListScrollPosition);

            bool alternate = false;
            foreach (var mod in _modManager.LoadedMods.Values)
            {
                // Get the enabled state for this mod
                // bool isEnabled = _modEnabledStates.ContainsKey(mod.FolderName) && _modEnabledStates[mod.FolderName];

                // Create a background color based on selection and alternating rows
                Color bgColor;
                if (mod == _selectedMod)
                {
                    bgColor = new Color(0.3f, 0.5f, 0.7f, 0.5f);
                }
                else
                {
                    bgColor = alternate ?
                        new Color(0.2f, 0.2f, 0.25f, 0.5f) :
                        new Color(0.22f, 0.22f, 0.28f, 0.3f);
                }
                alternate = !alternate;

                // Create a custom style for this row
                GUIStyle rowStyle = new GUIStyle(GUIStyle.none);
                rowStyle.normal.background = CreateColorTexture(2, 2, bgColor);
                rowStyle.margin = new RectOffset(1, 1, 1, 1);
                rowStyle.padding = new RectOffset(5, 5, 5, 5);

                // Use the custom style for the row
                GUILayout.BeginHorizontal(rowStyle);

                // Enable/disable toggle
                // bool newEnabled = GUILayout.Toggle(isEnabled, "", GUILayout.Width(20));
                // if (newEnabled != isEnabled)
                // {
                //     _modEnabledStates[mod.FolderName] = newEnabled;
                // }

                // Mod name button with custom style
                GUIStyle nameButtonStyle = new GUIStyle(_modNameStyle);
                nameButtonStyle.alignment = TextAnchor.MiddleLeft;

                if (GUILayout.Button(mod.Manifest.Name, nameButtonStyle, GUILayout.ExpandWidth(true)))
                {
                    _selectedMod = mod;

                    // Reset the config scroll position when selecting a new mod
                    _configScrollPosition = Vector2.zero;

                    // Initialize temporary config values for the selected mod
                    InitializeTempConfigValues();
                }

                // Display version number
                GUILayout.Label(mod.Manifest.Version, GUILayout.Width(60));

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Initialize temporary config values for the selected mod
        /// </summary>
        private void InitializeTempConfigValues()
        {
            if (_selectedMod == null)
                return;

            _tempConfigValues.Clear();

            var config = _selectedMod.GetModConfig();
            if (config != null)
            {
                foreach (var key in config.GetAllKeys())
                {
                    try
                    {
                        var value = config.GetValue<object>(key);
                        _tempConfigValues[key] = value;
                    }
                    catch (Exception ex)
                    {
                        LuaUtility.LogError($"Error getting config value for {key}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Draw the mod details
        /// </summary>
        private void DrawModDetails()
        {
            // Mod header with details
            GUILayout.Label(_selectedMod.Manifest.Name, _headerStyle);

            // Create a stylish header section
            GUIStyle headerBoxStyle = new GUIStyle(GUI.skin.box);
            headerBoxStyle.normal.background = CreateColorTexture(2, 2, new Color(0.25f, 0.25f, 0.3f, 0.8f));
            headerBoxStyle.padding = new RectOffset(12, 12, 12, 12);
            headerBoxStyle.margin = new RectOffset(0, 0, 5, 15);

            GUILayout.BeginVertical(headerBoxStyle);

            // Display version with a styled label
            GUIStyle infoLabelStyle = new GUIStyle(GUI.skin.label);
            infoLabelStyle.fontSize = 14;
            infoLabelStyle.normal.textColor = new Color(0.8f, 0.8f, 0.9f);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Version:", infoLabelStyle, GUILayout.Width(70));
            GUILayout.Label(_selectedMod.Manifest.Version, infoLabelStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Author:", infoLabelStyle, GUILayout.Width(70));
            GUILayout.Label(_selectedMod.Manifest.Author, infoLabelStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.Label("Description:", _configLabelStyle);
            GUIStyle descStyle = new GUIStyle(_modDescriptionStyle);
            descStyle.margin = new RectOffset(10, 10, 5, 10);
            GUILayout.Label(_selectedMod.Manifest.Description, descStyle);

            GUILayout.EndVertical();

            // Only show config section if the mod has a config
            var config = _selectedMod.GetModConfig();
            if (config != null && config.GetAllKeys().GetEnumerator().MoveNext())
            {
                // Config header with styled background
                GUIStyle configHeaderStyle = new GUIStyle(_configLabelStyle);
                configHeaderStyle.fontSize = 16;
                configHeaderStyle.alignment = TextAnchor.MiddleLeft;
                configHeaderStyle.padding = new RectOffset(10, 10, 8, 8);
                configHeaderStyle.margin = new RectOffset(0, 0, 15, 0);
                configHeaderStyle.normal.background = CreateColorTexture(2, 2, new Color(0.2f, 0.4f, 0.6f, 0.5f));
                configHeaderStyle.normal.textColor = Color.white;

                GUILayout.Label("Configuration Settings", configHeaderStyle);

                // Config scroll view with custom background
                GUIStyle configBoxStyle = new GUIStyle(GUI.skin.box);
                configBoxStyle.padding = new RectOffset(10, 10, 10, 10);

                GUILayout.BeginVertical(configBoxStyle);
                _configScrollPosition = GUILayout.BeginScrollView(_configScrollPosition);

                bool alternate = false;
                foreach (var key in config.GetAllKeys())
                {
                    try
                    {
                        string description = config.GetDescription(key);

                        // Alternate row background for better readability
                        Color bgColor = alternate ?
                            new Color(0.22f, 0.22f, 0.27f, 0.5f) :
                            new Color(0.2f, 0.2f, 0.25f, 0.3f);
                        alternate = !alternate;

                        GUIStyle itemBgStyle = new GUIStyle(GUIStyle.none);
                        itemBgStyle.normal.background = CreateColorTexture(2, 2, bgColor);
                        itemBgStyle.margin = new RectOffset(0, 0, 5, 5);
                        itemBgStyle.padding = new RectOffset(8, 8, 8, 8);

                        GUILayout.BeginVertical(itemBgStyle);

                        // Show the key label and description
                        GUIStyle keyLabelStyle = new GUIStyle(_configLabelStyle);
                        keyLabelStyle.margin = new RectOffset(0, 0, 0, 5);
                        GUILayout.Label(key, keyLabelStyle);

                        if (!string.IsNullOrEmpty(description))
                        {
                            GUIStyle descriptionStyle = new GUIStyle(_modDescriptionStyle);
                            descriptionStyle.margin = new RectOffset(10, 0, 0, 8);
                            GUILayout.Label(description, descriptionStyle);
                        }

                        // Get and display the appropriate editor for this config type
                        var value = config.GetValue<object>(key);
                        DrawConfigValueEditor(key, value);

                        GUILayout.EndVertical();
                    }
                    catch (Exception ex)
                    {
                        GUILayout.Label($"Error displaying {key}: {ex.Message}", _modDescriptionStyle);
                    }
                }

                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
            else
            {
                GUIStyle noConfigStyle = new GUIStyle(_modDescriptionStyle);
                noConfigStyle.alignment = TextAnchor.MiddleCenter;
                noConfigStyle.fontSize = 14;
                noConfigStyle.margin = new RectOffset(0, 0, 20, 0);

                GUILayout.FlexibleSpace();
                GUILayout.Label("This mod has no configurable options.", noConfigStyle);
                GUILayout.FlexibleSpace();
            }
        }

        /// <summary>
        /// Draw the appropriate editor for a config value based on its type
        /// </summary>
        private void DrawConfigValueEditor(string key, object value)
        {
            if (value == null)
            {
                GUILayout.Label("null", _configValueStyle);
                return;
            }

            Type valueType = value.GetType();

            if (valueType == typeof(bool))
            {
                // Boolean toggle
                bool boolValue = (bool)(_tempConfigValues.ContainsKey(key) ? _tempConfigValues[key] : value);

                GUILayout.BeginHorizontal();

                // Create a custom toggle style to prevent text overlap
                GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle);
                toggleStyle.margin = new RectOffset(0, 5, 0, 0);

                // Only display the toggle without text
                bool newValue = GUILayout.Toggle(boolValue, "", toggleStyle, GUILayout.Width(30));

                // Display the value as text separately
                GUIStyle valueTextStyle = new GUIStyle(GUI.skin.label);
                valueTextStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
                valueTextStyle.fontSize = 13;
                GUILayout.Label(boolValue ? "Enabled" : "Disabled", valueTextStyle);

                GUILayout.EndHorizontal();

                if (newValue != boolValue)
                {
                    _tempConfigValues[key] = newValue;
                }
            }
            else if (valueType == typeof(int) || valueType == typeof(long))
            {
                // Integer field
                string strValue = (_tempConfigValues.ContainsKey(key) ? _tempConfigValues[key] : value).ToString();
                string newValue = GUILayout.TextField(strValue, _configValueStyle);

                if (newValue != strValue)
                {
                    if (int.TryParse(newValue, out int intValue))
                    {
                        _tempConfigValues[key] = intValue;
                    }
                }
            }
            else if (valueType == typeof(float) || valueType == typeof(double))
            {
                // Float field
                string strValue = (_tempConfigValues.ContainsKey(key) ? _tempConfigValues[key] : value).ToString();
                string newValue = GUILayout.TextField(strValue, _configValueStyle);

                if (newValue != strValue)
                {
                    if (float.TryParse(newValue, out float floatValue))
                    {
                        _tempConfigValues[key] = floatValue;
                    }
                }
            }
            else if (valueType == typeof(string))
            {
                // String field
                string strValue = (string)(_tempConfigValues.ContainsKey(key) ? _tempConfigValues[key] : value);
                string newValue = GUILayout.TextField(strValue, _configValueStyle);

                if (newValue != strValue)
                {
                    _tempConfigValues[key] = newValue;
                }
            }
            else if (value is IList<object> || value is Dictionary<string, object>)
            {
                // Complex types (lists/dictionaries) - just show a disabled field with a value summary
                string summary = value is IList<object> list
                    ? $"Array with {list.Count} items"
                    : $"Object with {((Dictionary<string, object>)value).Count} properties";

                GUILayout.Label(summary, _configValueStyle);
                GUILayout.Label("Complex values can only be edited in the config file directly.", _modDescriptionStyle);
            }
            else if (value is MoonSharp.Interpreter.Table table)
            {
                // MoonSharp Table editor
                GUILayout.BeginVertical(GUI.skin.box);

                // Create a foldout for the table entries
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Table with {table.Pairs.Count()} entries", _configLabelStyle);
                GUILayout.EndHorizontal();

                // Save the current table if it doesn't exist in temp values
                if (!_tempConfigValues.ContainsKey(key))
                {
                    _tempConfigValues[key] = table;
                }

                MoonSharp.Interpreter.Table currentTable = (MoonSharp.Interpreter.Table)_tempConfigValues[key];

                // Show table entries
                GUILayout.BeginVertical(GUI.skin.box);

                // Track entries to remove
                List<DynValue> keysToRemove = new List<DynValue>();

                // Display each key-value pair in the table
                foreach (var pair in currentTable.Pairs)
                {
                    GUILayout.BeginHorizontal();

                    // Show key
                    GUILayout.Label(pair.Key.ToString(), GUILayout.Width(100));

                    // Show value based on type
                    switch (pair.Value.Type)
                    {
                        case DataType.Boolean:
                            bool boolValue = pair.Value.Boolean;

                            // Use same approach as above for boolean toggles
                            GUIStyle pairToggleStyle = new GUIStyle(GUI.skin.toggle);
                            pairToggleStyle.margin = new RectOffset(0, 5, 0, 0);

                            bool newBoolValue = GUILayout.Toggle(boolValue, "", pairToggleStyle, GUILayout.Width(20));
                            GUILayout.Label(boolValue ? "True" : "False", GUILayout.Width(40));

                            if (newBoolValue != boolValue)
                            {
                                currentTable[pair.Key] = DynValue.NewBoolean(newBoolValue);
                            }
                            break;

                        case DataType.Number:
                            string numStr = pair.Value.Number.ToString();
                            string newNumStr = GUILayout.TextField(numStr, _configValueStyle, GUILayout.Width(150));
                            if (newNumStr != numStr && double.TryParse(newNumStr, out double newNum))
                            {
                                currentTable[pair.Key] = DynValue.NewNumber(newNum);
                            }
                            break;

                        case DataType.String:
                            string strVal = pair.Value.String;
                            string newStrVal = GUILayout.TextField(strVal, _configValueStyle, GUILayout.Width(150));
                            if (newStrVal != strVal)
                            {
                                currentTable[pair.Key] = DynValue.NewString(newStrVal);
                            }
                            break;

                        default:
                            // For other types, just show type information
                            GUILayout.Label($"({pair.Value.Type}) {pair.Value}", _configValueStyle, GUILayout.Width(150));
                            break;
                    }

                    // Delete button
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        keysToRemove.Add(pair.Key);
                    }

                    GUILayout.EndHorizontal();
                }

                // Remove entries marked for deletion
                foreach (var key_to_remove in keysToRemove)
                {
                    currentTable[key_to_remove] = DynValue.Nil;
                }

                // Add new entry
                GUILayout.Space(10);
                GUILayout.Label("Add new entry:", _configLabelStyle);

                // Static variables to store new entry state
                if (!_tableNewEntryKeys.ContainsKey(key))
                {
                    _tableNewEntryKeys[key] = "";
                    _tableNewEntryValues[key] = "";
                    _tableNewEntryTypes[key] = 0; // Default to string
                }

                GUILayout.BeginHorizontal();

                // Key field
                GUILayout.Label("Key:", GUILayout.Width(30));
                _tableNewEntryKeys[key] = GUILayout.TextField(_tableNewEntryKeys[key], _configValueStyle, GUILayout.Width(100));

                // Type selector
                GUILayout.Label("Type:", GUILayout.Width(40));
                _tableNewEntryTypes[key] = GUILayout.SelectionGrid(_tableNewEntryTypes[key], new string[] { "String", "Number", "Boolean" }, 3, GUILayout.Width(200));

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                // Value field based on selected type
                GUILayout.Label("Value:", GUILayout.Width(40));

                switch (_tableNewEntryTypes[key])
                {
                    case 0: // String
                        _tableNewEntryValues[key] = GUILayout.TextField(_tableNewEntryValues[key], _configValueStyle, GUILayout.Width(200));
                        break;
                    case 1: // Number
                        _tableNewEntryValues[key] = GUILayout.TextField(_tableNewEntryValues[key], _configValueStyle, GUILayout.Width(200));
                        break;
                    case 2: // Boolean
                        bool tempBool = _tableNewEntryValues[key] == "true";
                        GUIStyle addBoolStyle = new GUIStyle(GUI.skin.toggle);
                        tempBool = GUILayout.Toggle(tempBool, "True/False", addBoolStyle, GUILayout.Width(100));
                        _tableNewEntryValues[key] = tempBool ? "true" : "false";
                        break;
                }

                // Add button
                if (GUILayout.Button("Add", GUILayout.Width(50)) && !string.IsNullOrEmpty(_tableNewEntryKeys[key]))
                {
                    // Create the key DynValue
                    DynValue keyDyn;

                    // Try to convert key to number if it parses
                    if (double.TryParse(_tableNewEntryKeys[key], out double keyNum))
                        keyDyn = DynValue.NewNumber(keyNum);
                    else
                        keyDyn = DynValue.NewString(_tableNewEntryKeys[key]);

                    // Create value based on selected type
                    DynValue valueDyn;
                    switch (_tableNewEntryTypes[key])
                    {
                        case 0: // String
                            valueDyn = DynValue.NewString(_tableNewEntryValues[key]);
                            break;
                        case 1: // Number
                            if (double.TryParse(_tableNewEntryValues[key], out double valueNum))
                                valueDyn = DynValue.NewNumber(valueNum);
                            else
                                valueDyn = DynValue.NewNumber(0);
                            break;
                        case 2: // Boolean
                            valueDyn = DynValue.NewBoolean(_tableNewEntryValues[key] == "true");
                            break;
                        default:
                            valueDyn = DynValue.NewString("");
                            break;
                    }

                    // Add to table
                    currentTable[keyDyn] = valueDyn;

                    // Clear input fields
                    _tableNewEntryKeys[key] = "";
                    _tableNewEntryValues[key] = "";
                }

                GUILayout.EndHorizontal();

                GUILayout.EndVertical(); // End of table entries
                GUILayout.EndVertical(); // End of table editor
            }
            else
            {
                // Fallback for other types
                GUILayout.Label(value.ToString(), _configValueStyle);
            }
        }

        /// <summary>
        /// Apply changes to mod configurations
        /// </summary>
        private void ApplyChanges()
        {
            if (_selectedMod != null)
            {
                var config = _selectedMod.GetModConfig();
                if (config != null)
                {
                    // Apply the temporary values to the actual config
                    foreach (var kvp in _tempConfigValues)
                    {
                        if (config.HasKey(kvp.Key))
                        {
                            try
                            {
                                Type valueType = kvp.Value.GetType();

                                if (valueType == typeof(int))
                                    config.SetValue(kvp.Key, (int)kvp.Value);
                                else if (valueType == typeof(float))
                                    config.SetValue(kvp.Key, (float)kvp.Value);
                                else if (valueType == typeof(bool))
                                    config.SetValue(kvp.Key, (bool)kvp.Value);
                                else if (valueType == typeof(string))
                                    config.SetValue(kvp.Key, (string)kvp.Value);
                                else if (kvp.Value is Table table)
                                    config.SetValue(kvp.Key, table);
                                else
                                    config.SetValue(kvp.Key, kvp.Value);
                            }
                            catch (Exception ex)
                            {
                                LuaUtility.LogError($"Error setting config value for {kvp.Key}: {ex.Message}");
                            }
                        }
                    }

                    // Save the config to disk
                    config.SaveConfig();

                    LuaUtility.Log($"Configuration saved for mod: {_selectedMod.Manifest.Name}");

                    // TODO: Add option to reload the mod if necessary
                }
            }

            // Save the enabled/disabled state of mods
            SaveModEnabledStates();
        }

        /// <summary>
        /// Save the enabled/disabled state of mods
        /// </summary>
        private void SaveModEnabledStates()
        {
            // Update the config storage with current states
            _configStorage.UpdateModEnabledStates(_modEnabledStates);

            // Save to disk
            if (_configStorage.SaveConfig())
            {
                LuaUtility.Log("Mod enabled states saved");

                // Log which mods are enabled/disabled
                foreach (var kvp in _modEnabledStates)
                {
                    LuaUtility.Log($"Mod {kvp.Key} is {(kvp.Value ? "enabled" : "disabled")}");
                }
            }
        }
    }
}