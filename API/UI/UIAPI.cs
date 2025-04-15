using MelonLoader;
using System;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using ScheduleOne.UI;
using ScheduleOne.UI.Tooltips;
using ScheduleOne.UI.Phone;
using ScheduleOne.UI.Items;
using ScheduleOne.Dialogue;
using ScheduleLua.API.Core;
using System.Linq;

namespace ScheduleLua.API.UI
{
    /// <summary>
    /// Provides UI functionality to Lua scripts using MelonLoader's IMGUI implementation
    /// </summary>
    public static class UIAPI
    {
        private static MelonLogger.Instance _logger => ScheduleLua.Core.Instance.LoggerInstance;

        // Store for registered GUI windows and controls
        private static Dictionary<string, LuaWindow> _windows = new Dictionary<string, LuaWindow>();
        private static Dictionary<string, LuaControl> _controls = new Dictionary<string, LuaControl>();

        // GUI styles
        public static GUIStyle _windowStyle;
        public static GUIStyle _buttonStyle;
        public static GUIStyle _labelStyle;
        public static GUIStyle _boxStyle;
        public static GUIStyle _textFieldStyle;
        public static bool _stylesInitialized = false;

        // GUI variables
        private static bool _guiInitialized = false;
        private static bool _guiEnabled = true;

        /// <summary>
        /// Registers UI API functions with the Lua engine
        /// </summary>
        public static void RegisterAPI(Script luaEngine)
        {
            if (luaEngine == null)
                throw new ArgumentNullException(nameof(luaEngine));

            // Register GUI functions
            luaEngine.Globals["CreateWindow"] = (Func<string, string, float, float, float, float, string>)CreateWindow;
            luaEngine.Globals["SetWindowPosition"] = (Action<string, float, float>)SetWindowPosition;
            luaEngine.Globals["SetWindowSize"] = (Action<string, float, float>)SetWindowSize;
            luaEngine.Globals["ShowWindow"] = (Action<string, bool>)ShowWindow;
            luaEngine.Globals["IsWindowVisible"] = (Func<string, bool>)IsWindowVisible;
            luaEngine.Globals["DestroyWindow"] = (Action<string>)DestroyWindow;

            // Register control functions
            luaEngine.Globals["AddButton"] = (Func<string, string, string, DynValue, string>)AddButton;
            luaEngine.Globals["AddLabel"] = (Func<string, string, string, string>)AddLabel;
            luaEngine.Globals["AddTextField"] = (Func<string, string, string, string>)AddTextField;
            luaEngine.Globals["GetControlText"] = (Func<string, string>)GetControlText;
            luaEngine.Globals["SetControlText"] = (Action<string, string>)SetControlText;
            luaEngine.Globals["SetControlPosition"] = (Action<string, float, float>)SetControlPosition;
            luaEngine.Globals["SetControlSize"] = (Action<string, float, float>)SetControlSize;
            luaEngine.Globals["ShowControl"] = (Action<string, bool>)ShowControl;
            luaEngine.Globals["DestroyControl"] = (Action<string>)DestroyControl;

            // Global UI functions
            luaEngine.Globals["EnableGUI"] = (Action<bool>)EnableGUI;
            luaEngine.Globals["IsGUIEnabled"] = (Func<bool>)IsGUIEnabled;

            // Tooltip Functions
            luaEngine.Globals["ShowTooltip"] = (Action<string, float, float, bool>)ShowTooltip;

            // Phone Functions
            luaEngine.Globals["IsPhoneOpen"] = (Func<bool>)IsPhoneOpen;
            luaEngine.Globals["OpenPhone"] = (Action)OpenPhone;
            luaEngine.Globals["ClosePhone"] = (Action)ClosePhone;
            luaEngine.Globals["TogglePhoneFlashlight"] = (Action)TogglePhoneFlashlight;
            luaEngine.Globals["IsPhoneFlashlightOn"] = (Func<bool>)IsPhoneFlashlightOn;

            // Notification Functions
            luaEngine.Globals["ShowNotification"] = (Action<string>)ShowNotification;
            luaEngine.Globals["ShowNotificationWithTimeout"] = (Action<string, float>)ShowNotificationWithTimeout;

            // UI Item Functions
            luaEngine.Globals["GetHoveredItemName"] = (Func<string>)GetHoveredItemName;
            luaEngine.Globals["IsItemBeingDragged"] = (Func<bool>)IsItemBeingDragged;

            // Dialog Functions
            luaEngine.Globals["ShowDialogue"] = (Action<string, string>)ShowDialogue;
            luaEngine.Globals["ShowDialogueWithTimeout"] = (Action<string, string, float>)ShowDialogueWithTimeout;
            luaEngine.Globals["ShowChoiceDialogue"] = (Action<string, string, Table, DynValue>)ShowChoiceDialogue;
            luaEngine.Globals["CloseDialogue"] = (Action)CloseDialogue;

            // Register UI Style functions
            luaEngine.Globals["SetWindowStyle"] = (Action<string, float, float, float, float>)SetWindowStyle;
            luaEngine.Globals["SetButtonStyle"] = (Action<string, float, float, float, float>)SetButtonStyle;
            luaEngine.Globals["SetLabelStyle"] = (Action<string, float, float, float, float>)SetLabelStyle;
            luaEngine.Globals["SetTextFieldStyle"] = (Action<string, float, float, float, float>)SetTextFieldStyle;
            luaEngine.Globals["SetBoxStyle"] = (Action<string, float, float, float, float>)SetBoxStyle;

            luaEngine.Globals["SetFontSize"] = (Action<string, int>)SetFontSize;
            luaEngine.Globals["SetFontStyle"] = (Action<string, string>)SetFontStyle;
            luaEngine.Globals["SetTextAlignment"] = (Action<string, string>)SetTextAlignment;
            luaEngine.Globals["SetBorder"] = (Action<string, int, int, int, int>)SetBorder;
            luaEngine.Globals["SetPadding"] = (Action<string, int, int, int, int>)SetPadding;

            // Initialize GUI
            InitializeGUI();
        }

        #region GUI Management

        /// <summary>
        /// Initializes the GUI system for Lua scripts
        /// </summary>
        private static void InitializeGUI()
        {
            if (_guiInitialized)
                return;

            if (ScheduleLua.Core.Instance != null)
            {
                ScheduleLua.Core.Instance.OnGUICallback += OnGUI;
            }
            else
            {
                _logger.Error("Cannot register OnGUI callback: Core.Instance is null");
                return;
            }

            _guiInitialized = true;
            _guiEnabled = true;
            _logger.Msg("Lua GUI system initialized");
        }

        /// <summary>
        /// Initializes GUI styles for consistent appearance
        /// </summary>
        private static void InitializeStyles()
        {
            if (_stylesInitialized)
                return;

            try
            {
                // Create window style
                _windowStyle = new GUIStyle(GUI.skin.window);
                var windowTex = MakeColorTexture(new Color(0.1f, 0.1f, 0.2f, 0.95f));
                if (windowTex != null)
                {
                    _windowStyle.normal.background = windowTex;
                    _windowStyle.onNormal.background = windowTex;
                }
                _windowStyle.border = new RectOffset(10, 10, 10, 10);
                _windowStyle.padding = new RectOffset(10, 10, 25, 10);
                _windowStyle.normal.textColor = Color.white;
                _windowStyle.onNormal.textColor = Color.white;
                _windowStyle.fontStyle = FontStyle.Bold;
                _windowStyle.fontSize = 16;

                // Create button style
                _buttonStyle = new GUIStyle(GUI.skin.button);
                var buttonTex = MakeColorTexture(new Color(0.3f, 0.3f, 0.8f, 0.9f));
                if (buttonTex != null)
                {
                    _buttonStyle.normal.background = buttonTex;
                    _buttonStyle.onNormal.background = buttonTex;
                }
                var buttonHoverTex = MakeColorTexture(new Color(0.4f, 0.4f, 0.9f, 0.9f));
                if (buttonHoverTex != null)
                {
                    _buttonStyle.hover.background = buttonHoverTex;
                    _buttonStyle.onHover.background = buttonHoverTex;
                }
                var buttonActiveTex = MakeColorTexture(new Color(0.5f, 0.5f, 1.0f, 0.9f));
                if (buttonActiveTex != null)
                {
                    _buttonStyle.active.background = buttonActiveTex;
                    _buttonStyle.onActive.background = buttonActiveTex;
                }
                _buttonStyle.normal.textColor = Color.white;
                _buttonStyle.hover.textColor = Color.white;
                _buttonStyle.active.textColor = Color.white;
                _buttonStyle.onNormal.textColor = Color.white;
                _buttonStyle.onHover.textColor = Color.white;
                _buttonStyle.onActive.textColor = Color.white;
                _buttonStyle.fontSize = 14;
                _buttonStyle.fontStyle = FontStyle.Bold;
                _buttonStyle.alignment = TextAnchor.MiddleCenter;
                _buttonStyle.border = new RectOffset(5, 5, 5, 5);

                // Create label style
                _labelStyle = new GUIStyle(GUI.skin.label);
                _labelStyle.normal.textColor = Color.white;
                _labelStyle.fontSize = 14;
                _labelStyle.fontStyle = FontStyle.Normal;
                _labelStyle.wordWrap = true;

                // Create box style
                _boxStyle = new GUIStyle(GUI.skin.box);
                var boxTex = MakeColorTexture(new Color(0.2f, 0.2f, 0.2f, 0.95f));
                if (boxTex != null)
                {
                    _boxStyle.normal.background = boxTex;
                    _boxStyle.onNormal.background = boxTex;
                }
                _boxStyle.normal.textColor = Color.white;
                _boxStyle.border = new RectOffset(5, 5, 5, 5);

                // Create text field style
                _textFieldStyle = new GUIStyle(GUI.skin.textField);
                _textFieldStyle.normal.textColor = Color.white;
                var textFieldTex = MakeColorTexture(new Color(0.15f, 0.15f, 0.15f, 0.95f));
                if (textFieldTex != null)
                {
                    _textFieldStyle.normal.background = textFieldTex;
                    _textFieldStyle.onNormal.background = textFieldTex;
                }
                _textFieldStyle.fontSize = 14;

                _stylesInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error initializing styles: {ex.Message}");
                // Use default styles if custom initialization fails
                _windowStyle = new GUIStyle(GUI.skin.window);
                _buttonStyle = new GUIStyle(GUI.skin.button);
                _labelStyle = new GUIStyle(GUI.skin.label);
                _boxStyle = new GUIStyle(GUI.skin.box);
                _textFieldStyle = new GUIStyle(GUI.skin.textField);
                _stylesInitialized = true;
            }
        }

        /// <summary>
        /// Creates a solid color texture for GUI elements
        /// </summary>
        private static Texture2D MakeColorTexture(Color color)
        {
            try
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Color[] colors = new Color[4] { color, color, color, color };
                texture.SetPixels(colors);
                texture.Apply();
                texture.hideFlags = HideFlags.HideAndDontSave;
                texture.wrapMode = TextureWrapMode.Repeat;
                return texture;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error creating texture: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Draw a colored box with a border
        /// </summary>
        private static void DrawColoredBox(Rect position, string text, Color backgroundColor, Color borderColor, GUIStyle style)
        {
            Color oldColor = GUI.backgroundColor;

            // Draw box background
            GUI.backgroundColor = backgroundColor;
            GUI.Box(position, text, style);

            // Draw border if box was drawn successfully
            Rect borderRect = new Rect(position.x, position.y, position.width, position.height);
            GUI.backgroundColor = borderColor;
            GUI.Box(borderRect, "", GUI.skin.GetStyle("box"));

            GUI.backgroundColor = oldColor;
        }

        /// <summary>
        /// OnGUI callback for rendering all UI elements
        /// </summary>
        private static void OnGUI()
        {
            if (!_guiEnabled || !_guiInitialized)
            {
                return;
            }

            // Initialize styles if needed
            if (!_stylesInitialized)
            {
                InitializeStyles();
            }

            try
            {
                // Draw all registered windows
                foreach (var windowEntry in _windows)
                {
                    var window = windowEntry.Value;
                    if (window.IsVisible)
                    {
                        window.Draw();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in UIAPI.OnGUI: {ex.Message}");
            }
        }

        /// <summary>
        /// Enable or disable GUI rendering
        /// </summary>
        public static void EnableGUI(bool enable)
        {
            _guiEnabled = enable;
        }

        /// <summary>
        /// Check if GUI is enabled
        /// </summary>
        public static bool IsGUIEnabled()
        {
            return _guiEnabled;
        }

        #endregion

        #region Window Management

        /// <summary>
        /// Creates a new window with the specified parameters
        /// </summary>
        public static string CreateWindow(string id, string title, float x, float y, float width, float height)
        {
            try
            {
                // Create a unique ID if none provided
                if (string.IsNullOrEmpty(id))
                {
                    id = "window_" + Guid.NewGuid().ToString("N");
                }

                // Check if window with this ID already exists
                if (_windows.ContainsKey(id))
                {
                    _logger.Warning($"Window with ID '{id}' already exists. Returning existing window.");
                    return id;
                }

                // Create new window
                var window = new LuaWindow(id, title, x, y, width, height);
                _windows[id] = window;

                _logger.Msg($"Created window '{id}' ({title}) at ({x},{y}) with size ({width}x{height})");
                return id;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in CreateWindow: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Sets the position of a window
        /// </summary>
        public static void SetWindowPosition(string windowId, float x, float y)
        {
            try
            {
                if (_windows.TryGetValue(windowId, out var window))
                {
                    window.X = x;
                    window.Y = y;
                }
                else
                {
                    _logger.Warning($"SetWindowPosition: Window '{windowId}' not found");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in SetWindowPosition: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the size of a window
        /// </summary>
        public static void SetWindowSize(string windowId, float width, float height)
        {
            try
            {
                if (_windows.TryGetValue(windowId, out var window))
                {
                    window.Width = width;
                    window.Height = height;
                }
                else
                {
                    _logger.Warning($"SetWindowSize: Window '{windowId}' not found");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in SetWindowSize: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows or hides a window
        /// </summary>
        public static void ShowWindow(string windowId, bool visible)
        {
            try
            {
                if (_windows.TryGetValue(windowId, out var window))
                {
                    window.IsVisible = visible;
                }
                else
                {
                    _logger.Warning($"ShowWindow: Window '{windowId}' not found");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in ShowWindow: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a window is visible
        /// </summary>
        public static bool IsWindowVisible(string windowId)
        {
            try
            {
                if (_windows.TryGetValue(windowId, out var window))
                {
                    return window.IsVisible;
                }
                else
                {
                    _logger.Warning($"IsWindowVisible: Window '{windowId}' not found");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in IsWindowVisible: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Destroys a window
        /// </summary>
        public static void DestroyWindow(string windowId)
        {
            try
            {
                if (_windows.ContainsKey(windowId))
                {
                    // Remove all controls from this window
                    var controlsToRemove = new List<string>();
                    foreach (var controlEntry in _controls)
                    {
                        if (controlEntry.Value.WindowId == windowId)
                        {
                            controlsToRemove.Add(controlEntry.Key);
                        }
                    }

                    foreach (var controlId in controlsToRemove)
                    {
                        _controls.Remove(controlId);
                    }

                    // Remove window
                    _windows.Remove(windowId);
                    _logger.Msg($"Destroyed window '{windowId}'");
                }
                else
                {
                    _logger.Warning($"DestroyWindow: Window '{windowId}' not found");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in DestroyWindow: {ex.Message}");
            }
        }

        #endregion

        #region Control Management

        /// <summary>
        /// Adds a button to a window
        /// </summary>
        public static string AddButton(string windowId, string id, string text, DynValue callback)
        {
            try
            {
                if (!_windows.TryGetValue(windowId, out var window))
                {
                    _logger.Warning($"AddButton: Window '{windowId}' not found");
                    return string.Empty;
                }

                if (callback == null || callback.Type != DataType.Function)
                {
                    _logger.Warning($"AddButton: Invalid callback function");
                    return string.Empty;
                }

                // Create a unique ID if none provided
                if (string.IsNullOrEmpty(id))
                {
                    id = $"button_{windowId}_{Guid.NewGuid().ToString("N")}";
                }

                // Check if control with this ID already exists
                if (_controls.ContainsKey(id))
                {
                    _logger.Warning($"Control with ID '{id}' already exists. Returning existing control.");
                    return id;
                }

                // Create button
                var button = new LuaButton(id, windowId, text, callback);
                _controls[id] = button;
                window.AddControl(button);

                return id;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in AddButton: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Adds a label to a window
        /// </summary>
        public static string AddLabel(string windowId, string id, string text)
        {
            try
            {
                if (!_windows.TryGetValue(windowId, out var window))
                {
                    _logger.Warning($"AddLabel: Window '{windowId}' not found");
                    return string.Empty;
                }

                // Create a unique ID if none provided
                if (string.IsNullOrEmpty(id))
                {
                    id = $"label_{windowId}_{Guid.NewGuid().ToString("N")}";
                }

                // Check if control with this ID already exists
                if (_controls.ContainsKey(id))
                {
                    _logger.Warning($"Control with ID '{id}' already exists. Returning existing control.");
                    return id;
                }

                // Create label
                var label = new LuaLabel(id, windowId, text);
                _controls[id] = label;
                window.AddControl(label);

                return id;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in AddLabel: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Adds a text field to a window
        /// </summary>
        public static string AddTextField(string windowId, string id, string text)
        {
            try
            {
                if (!_windows.TryGetValue(windowId, out var window))
                {
                    _logger.Warning($"AddTextField: Window '{windowId}' not found");
                    return string.Empty;
                }

                // Create a unique ID if none provided
                if (string.IsNullOrEmpty(id))
                {
                    id = $"textfield_{windowId}_{Guid.NewGuid().ToString("N")}";
                }

                // Check if control with this ID already exists
                if (_controls.ContainsKey(id))
                {
                    _logger.Warning($"Control with ID '{id}' already exists. Returning existing control.");
                    return id;
                }

                // Create text field
                var textField = new LuaTextField(id, windowId, text);
                _controls[id] = textField;
                window.AddControl(textField);

                return id;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in AddTextField: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the text of a control
        /// </summary>
        public static string GetControlText(string controlId)
        {
            try
            {
                if (_controls.TryGetValue(controlId, out var control))
                {
                    return control.Text;
                }
                else
                {
                    _logger.Warning($"GetControlText: Control '{controlId}' not found");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in GetControlText: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Sets the text of a control
        /// </summary>
        public static void SetControlText(string controlId, string text)
        {
            try
            {
                if (_controls.TryGetValue(controlId, out var control))
                {
                    control.Text = text;
                }
                else
                {
                    _logger.Warning($"SetControlText: Control '{controlId}' not found");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in SetControlText: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the position of a control within its parent window
        /// </summary>
        public static void SetControlPosition(string controlId, float x, float y)
        {
            try
            {
                if (_controls.TryGetValue(controlId, out var control))
                {
                    control.X = x;
                    control.Y = y;
                }
                else
                {
                    _logger.Warning($"SetControlPosition: Control '{controlId}' not found");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in SetControlPosition: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the size of a control
        /// </summary>
        public static void SetControlSize(string controlId, float width, float height)
        {
            try
            {
                if (_controls.TryGetValue(controlId, out var control))
                {
                    control.Width = width;
                    control.Height = height;
                }
                else
                {
                    _logger.Warning($"SetControlSize: Control '{controlId}' not found");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in SetControlSize: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows or hides a control
        /// </summary>
        public static void ShowControl(string controlId, bool visible)
        {
            try
            {
                if (_controls.TryGetValue(controlId, out var control))
                {
                    control.IsVisible = visible;
                }
                else
                {
                    _logger.Warning($"ShowControl: Control '{controlId}' not found");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in ShowControl: {ex.Message}");
            }
        }

        /// <summary>
        /// Destroys a control
        /// </summary>
        public static void DestroyControl(string controlId)
        {
            try
            {
                if (_controls.TryGetValue(controlId, out var control))
                {
                    // Remove from parent window
                    if (_windows.TryGetValue(control.WindowId, out var window))
                    {
                        window.RemoveControl(control);
                    }

                    // Remove from controls dictionary
                    _controls.Remove(controlId);
                    _logger.Msg($"Destroyed control '{controlId}'");
                }
                else
                {
                    _logger.Warning($"DestroyControl: Control '{controlId}' not found");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in DestroyControl: {ex.Message}");
            }
        }

        #endregion

        #region Tooltip Functions

        /// <summary>
        /// Shows a tooltip at the specified position
        /// </summary>
        public static void ShowTooltip(string text, float x, float y, bool worldspace = false)
        {
            if (string.IsNullOrEmpty(text))
            {
                _logger.Warning("ShowTooltip: text is null or empty");
                return;
            }

            try
            {
                if (TooltipManager.Instance == null)
                {
                    _logger.Warning("ShowTooltip: TooltipManager not available");
                    return;
                }

                TooltipManager.Instance.ShowTooltip(text, new Vector2(x, y), worldspace);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in ShowTooltip: {ex.Message}");
            }
        }

        #endregion

        #region Phone Functions

        /// <summary>
        /// Checks if the phone is currently open
        /// </summary>
        public static bool IsPhoneOpen()
        {
            try
            {
                if (Phone.Instance == null)
                {
                    _logger.Warning("IsPhoneOpen: Phone not available");
                    return false;
                }

                return Phone.Instance.IsOpen;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in IsPhoneOpen: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Opens the player's phone
        /// </summary>
        public static void OpenPhone()
        {
            try
            {
                if (Phone.Instance == null)
                {
                    _logger.Warning("OpenPhone: Phone not available");
                    return;
                }

                if (!Phone.Instance.IsOpen)
                {
                    Phone.Instance.SetIsOpen(true);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in OpenPhone: {ex.Message}");
            }
        }

        /// <summary>
        /// Closes the player's phone
        /// </summary>
        public static void ClosePhone()
        {
            try
            {
                if (Phone.Instance == null)
                {
                    _logger.Warning("ClosePhone: Phone not available");
                    return;
                }

                if (Phone.Instance.IsOpen)
                {
                    Phone.Instance.SetIsOpen(false);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in ClosePhone: {ex.Message}");
            }
        }

        /// <summary>
        /// Toggles the phone's flashlight on/off
        /// </summary>
        public static void TogglePhoneFlashlight()
        {
            try
            {
                if (Phone.Instance == null)
                {
                    _logger.Warning("TogglePhoneFlashlight: Phone not available");
                    return;
                }

                // Use reflection to access the private method if needed
                var flashlightMethod = typeof(Phone).GetMethod("ToggleFlashlight",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (flashlightMethod != null)
                {
                    flashlightMethod.Invoke(Phone.Instance, null);
                }
                else
                {
                    _logger.Warning("TogglePhoneFlashlight: Could not find method");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in TogglePhoneFlashlight: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if the phone's flashlight is currently on
        /// </summary>
        public static bool IsPhoneFlashlightOn()
        {
            try
            {
                if (Phone.Instance == null)
                {
                    _logger.Warning("IsPhoneFlashlightOn: Phone not available");
                    return false;
                }

                return Phone.Instance.FlashlightOn;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in IsPhoneFlashlightOn: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Notification Functions

        /// <summary>
        /// Shows a notification to the player
        /// </summary>
        public static void ShowNotification(string message)
        {
            try
            {
                if (string.IsNullOrEmpty(message))
                {
                    _logger.Warning("ShowNotification: message is null or empty");
                    return;
                }

                var notificationsManager = UnityEngine.Object.FindObjectOfType<NotificationsManager>();
                if (notificationsManager == null)
                {
                    _logger.Warning("ShowNotification: NotificationsManager not available");
                    return;
                }

                notificationsManager.SendNotification("Lua Notification", message, null);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in ShowNotification: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows a notification to the player with a custom timeout
        /// </summary>
        public static void ShowNotificationWithTimeout(string message, float timeout)
        {
            try
            {
                if (string.IsNullOrEmpty(message))
                {
                    _logger.Warning("ShowNotificationWithTimeout: message is null or empty");
                    return;
                }

                var notificationsManager = UnityEngine.Object.FindObjectOfType<NotificationsManager>();
                if (notificationsManager == null)
                {
                    _logger.Warning("ShowNotificationWithTimeout: NotificationsManager not available");
                    return;
                }

                notificationsManager.SendNotification("Lua Notification", message, null, timeout);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in ShowNotificationWithTimeout: {ex.Message}");
            }
        }

        #endregion

        #region UI Item Functions

        /// <summary>
        /// Gets the name of the item currently hovered in inventory UI
        /// </summary>
        public static string GetHoveredItemName()
        {
            try
            {
                if (ItemUIManager.Instance == null || ItemUIManager.Instance.HoveredSlot == null)
                {
                    return string.Empty;
                }

                var slot = ItemUIManager.Instance.HoveredSlot.assignedSlot;
                if (slot == null || slot.ItemInstance == null)
                {
                    return string.Empty;
                }

                return slot.ItemInstance.Name;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in GetHoveredItemName: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Checks if an item is currently being dragged
        /// </summary>
        public static bool IsItemBeingDragged()
        {
            try
            {
                if (ItemUIManager.Instance == null)
                {
                    return false;
                }

                // This is a simplification - we'd need to check if there's an active drag operation
                // For now, this is an approximate implementation
                return ItemUIManager.Instance.DraggingEnabled &&
                       ItemUIManager.Instance.HoveredSlot != null &&
                       ItemUIManager.Instance.HoveredSlot.IsBeingDragged;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in IsItemBeingDragged: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Dialog Functions

        /// <summary>
        /// Shows a dialogue with the given title and text
        /// </summary>
        public static void ShowDialogue(string title, string text)
        {
            try
            {
                var dialogueCanvas = UnityEngine.Object.FindObjectOfType<DialogueCanvas>();
                if (dialogueCanvas == null)
                {
                    _logger.Warning("ShowDialogue: DialogueCanvas not available");
                    return;
                }

                // Combine title and text with proper formatting
                string formattedText = string.IsNullOrEmpty(title) ? text : $"<b>{title}</b>\n\n{text}";

                // Check if there's already an active dialogue
                if (dialogueCanvas.isActive)
                {

                    // First try to clear any existing dialogue
                    try
                    {
                        dialogueCanvas.StopTextOverride();
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"Error stopping text override: {ex.Message}");
                    }
                }

                // Show the dialogue with proper error handling
                try
                {
                    // _logger.Msg($"Showing dialogue: {formattedText.Substring(0, Math.Min(50, formattedText.Length))}...");
                    dialogueCanvas.OverrideText(formattedText);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error in dialogueCanvas.OverrideText: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in ShowDialogue: {ex.Message}");
                _logger.Error(ex.StackTrace);
            }
        }

        /// <summary>
        /// Shows a dialogue with the given title and text that automatically closes after the specified time
        /// Idealy this should be manually closed from lua inside a Wait() call using CloseDialogue()
        /// </summary>
        public static void ShowDialogueWithTimeout(string title, string text, float timeout = 5.0f)
        {
            try
            {
                // Check if there's already an active dialogue that needs to be closed first
                var dialogueCanvas = UnityEngine.Object.FindObjectOfType<DialogueCanvas>();
                if (dialogueCanvas != null && dialogueCanvas.isActive)
                {
                    CloseDialogue();

                    // Small delay to ensure previous dialogue is closed
                    System.Threading.Thread.Sleep(50);
                }

                // Now show the new dialogue
                ShowDialogue(title, text);

                // Schedule auto-close after timeout
                MelonLoader.MelonCoroutines.Start(AutoCloseDialogueCoroutine(timeout));
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in ShowDialogueWithTimeout: {ex.Message}");
                _logger.Error(ex.StackTrace);
            }
        }

        /// <summary>
        /// Coroutine to automatically close the dialogue after a delay
        /// </summary>
        private static System.Collections.IEnumerator AutoCloseDialogueCoroutine(float delay)
        {
            yield return new UnityEngine.WaitForSeconds(delay);

            CloseDialogue();
        }

        /// <summary>
        /// Shows a dialogue with choices using the game's native dialogue system
        /// </summary>
        public static void ShowChoiceDialogue(string title, string text, Table choices, DynValue callback)
        {
            try
            {
                // Check if there's a valid choices table
                if (choices == null || choices.Length == 0)
                {
                    _logger.Warning("ShowChoiceDialogue: choices table is null or empty");
                    return;
                }

                // _logger.Msg($"Showing choice dialogue with {choices.Length} options");

                // Convert the Lua table of choices to a C# list
                List<string> choiceTexts = new List<string>();
                for (int i = 1; i <= choices.Length; i++)
                {
                    // Lua uses 1-based indexing, so adjust accordingly
                    DynValue choiceText = choices.Get(i);
                    string choiceString = choiceText.String ?? choiceText.ToString();
                    choiceTexts.Add(choiceString);
                }

                // Store the callback for later use
                _choiceDialogueCallback = callback;

                // Create a simple choice dialogue by repurposing the standard dialogue system
                // This doesn't use the full DialogueNode system but is simpler and more reliable
                var dialogueCanvas = UnityEngine.Object.FindObjectOfType<DialogueCanvas>();
                if (dialogueCanvas == null)
                {
                    _logger.Warning("ShowChoiceDialogue: DialogueCanvas not available");
                    return;
                }

                // Format the title and text properly
                string formattedText = string.IsNullOrEmpty(title)
                    ? text
                    : $"<b>{title}</b>\n\n{text}";

                // Add choice numbers to make selection clearer
                List<string> numberedChoices = new List<string>();
                for (int i = 0; i < choiceTexts.Count; i++)
                {
                    numberedChoices.Add($"{i + 1}. {choiceTexts[i]}");
                }

                // Append choices to the dialogue text
                formattedText += "\n\n";
                formattedText += string.Join("\n", numberedChoices);

                // Register for choice selection events
                if (_luaChoiceCallback == null)
                {
                    _luaChoiceCallback = new GameObject("LuaChoiceHandler").AddComponent<LuaChoiceCallback>();
                    UnityEngine.Object.DontDestroyOnLoad(_luaChoiceCallback.gameObject);
                }

                // Setup the choice callback
                _luaChoiceCallback.SetChoices(choiceTexts, callback);

                // Display a custom dialogue using the OverrideText method
                dialogueCanvas.OverrideText(formattedText);

                // Start monitoring for choice selection
                _luaChoiceCallback.StartMonitoring();
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in ShowChoiceDialogue: {ex.Message}");
                _logger.Error(ex.StackTrace);
            }
        }

        // Storage for active dialogue callbacks
        private static DynValue _choiceDialogueCallback;
        private static LuaChoiceCallback _luaChoiceCallback;

        /// <summary>
        /// MonoBehaviour that handles choice selection for Lua dialogues
        /// </summary>
        private class LuaChoiceCallback : MonoBehaviour
        {
            private List<string> _choices = new List<string>();
            private DynValue _callback;
            private bool _isMonitoring = false;
            private KeyCode[] _numberKeys = new KeyCode[]
            {
                KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
                KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9
            };

            public void SetChoices(List<string> choices, DynValue callback)
            {
                _choices = choices;
                _callback = callback;
                _isMonitoring = false;
            }

            public void StartMonitoring()
            {
                _isMonitoring = true;
                // _logger.Msg("Started monitoring for dialogue choices");
            }

            private void Update()
            {
                if (!_isMonitoring || _choices == null || _choices.Count == 0)
                    return;

                // Check for number key presses (1-9)
                for (int i = 0; i < _numberKeys.Length && i < _choices.Count; i++)
                {
                    if (UnityEngine.Input.GetKeyDown(_numberKeys[i]))
                    {
                        SelectChoice(i);
                        break;
                    }
                }
            }

            private void SelectChoice(int index)
            {
                if (!_isMonitoring || index < 0 || index >= _choices.Count)
                    return;

                _isMonitoring = false;

                // Call the Lua callback with the selected index (1-based for Lua)
                if (_callback != null && _callback.Type == DataType.Function)
                {
                    try
                    {
                        // _logger.Msg($"Calling Lua callback with index: {index + 1}");
                        ScheduleLua.Core.Instance._luaEngine.Call(_callback, index + 1);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error in choice callback: {ex.Message}");
                    }
                }

                // Close the dialogue
                CloseDialogue();
            }
        }

        /// <summary>
        /// Closes any open dialogue
        /// </summary>
        public static void CloseDialogue()
        {
            try
            {
                var dialogueCanvas = UnityEngine.Object.FindObjectOfType<DialogueCanvas>();
                if (dialogueCanvas == null)
                {
                    _logger.Warning("CloseDialogue: DialogueCanvas not available");
                    return;
                }

                dialogueCanvas.EndDialogue();
                // _logger.Msg("EndDialogue called successfully");

                // Handle active dialogue in DialogueHandler if one exists
                if (DialogueHandler.activeDialogue != null)
                {
                    // Find the handler that created the active dialogue
                    var dialogueHandler = UnityEngine.Object.FindObjectOfType<DialogueHandler>();
                    if (dialogueHandler != null)
                    {
                        dialogueHandler.EndDialogue();
                        // _logger.Msg("DialogueHandler.EndDialogue called");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in CloseDialogue: {ex.Message}");
                _logger.Error(ex.StackTrace);
            }
        }

        #endregion

        #region GUI Classes

        /// <summary>
        /// Base class for all Lua GUI elements
        /// </summary>
        private abstract class LuaControl
        {
            public string Id { get; private set; }
            public string WindowId { get; private set; }
            public float X { get; set; } = 10;
            public float Y { get; set; } = 10;
            public float Width { get; set; } = 150;
            public float Height { get; set; } = 30;
            public bool IsVisible { get; set; } = true;
            public string Text { get; set; }

            protected LuaControl(string id, string windowId, string text)
            {
                Id = id;
                WindowId = windowId;
                Text = text;
            }

            public abstract void Draw(float windowX, float windowY);

            protected Rect GetRect(float windowX, float windowY)
            {
                return new Rect(windowX + X, windowY + Y, Width, Height);
            }
        }

        /// <summary>
        /// Button control for Lua scripts
        /// </summary>
        private class LuaButton : LuaControl
        {
            private DynValue _callback;

            public LuaButton(string id, string windowId, string text, DynValue callback)
                : base(id, windowId, text)
            {
                _callback = callback;
            }

            public override void Draw(float windowX, float windowY)
            {
                try
                {
                    var buttonStyle = _stylesInitialized ? UIAPI._buttonStyle : GUI.skin.button;
                    Rect rect = GetRect(windowX, windowY);

                    // Draw button with fallback color if style doesn't have background
                    Color oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.3f, 0.3f, 0.8f, 0.9f);

                    // Draw the button with a more visible style
                    if (GUI.Button(rect, Text, buttonStyle))
                    {
                        // Call the Lua callback function
                        if (_callback != null && _callback.Type == DataType.Function)
                        {
                            ScheduleLua.Core.Instance._luaEngine.Call(_callback);
                        }
                    }

                    // Restore color
                    GUI.backgroundColor = oldColor;
                }
                catch (Exception ex)
                {
                    var logger = ScheduleLua.Core.Instance.LoggerInstance;
                    logger.Error($"Error drawing button '{Id}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Label control for Lua scripts
        /// </summary>
        private class LuaLabel : LuaControl
        {
            public LuaLabel(string id, string windowId, string text)
                : base(id, windowId, text)
            {
            }

            public override void Draw(float windowX, float windowY)
            {
                try
                {
                    var labelStyle = _stylesInitialized ? UIAPI._labelStyle : GUI.skin.label;
                    Rect rect = GetRect(windowX, windowY);

                    // Draw a background for the label for better visibility
                    Color oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                    GUI.Box(rect, "", GUI.skin.box);

                    // Draw the label with our custom style
                    GUI.Label(rect, Text, labelStyle);

                    // Restore color
                    GUI.backgroundColor = oldColor;
                }
                catch (Exception ex)
                {
                    var logger = ScheduleLua.Core.Instance.LoggerInstance;
                    logger.Error($"Error drawing label '{Id}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Text field control for Lua scripts
        /// </summary>
        private class LuaTextField : LuaControl
        {
            public LuaTextField(string id, string windowId, string text)
                : base(id, windowId, text)
            {
            }

            public override void Draw(float windowX, float windowY)
            {
                try
                {
                    var textFieldStyle = _stylesInitialized ? UIAPI._textFieldStyle : GUI.skin.textField;
                    Rect rect = GetRect(windowX, windowY);

                    // Draw text field with fallback background
                    Color oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);

                    // Draw the text field with our custom style
                    Text = GUI.TextField(rect, Text, textFieldStyle);

                    // Restore color
                    GUI.backgroundColor = oldColor;
                }
                catch (Exception ex)
                {
                    var logger = ScheduleLua.Core.Instance.LoggerInstance;
                    logger.Error($"Error drawing text field '{Id}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Window container for Lua UI elements
        /// </summary>
        private class LuaWindow
        {
            public string Id { get; private set; }
            public string Title { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public float Width { get; set; }
            public float Height { get; set; }
            public bool IsVisible { get; set; } = true;
            public bool IsDraggable { get; set; } = true;

            private List<LuaControl> _controls = new List<LuaControl>();
            private Rect _windowRect;
            private static MelonLogger.Instance _logger => ScheduleLua.Core.Instance.LoggerInstance;

            public LuaWindow(string id, string title, float x, float y, float width, float height)
            {
                Id = id;
                Title = title;
                X = x;
                Y = y;
                Width = width;
                Height = height;
                _windowRect = new Rect(x, y, width, height);
            }

            public void AddControl(LuaControl control)
            {
                _controls.Add(control);
            }

            public void RemoveControl(LuaControl control)
            {
                _controls.Remove(control);
            }

            public void Draw()
            {
                try
                {
                    // Update window rect
                    _windowRect = new Rect(X, Y, Width, Height);

                    // Get the appropriate styles
                    var windowStyle = _stylesInitialized ? UIAPI._windowStyle : GUI.skin.window;
                    var boxStyle = _stylesInitialized ? UIAPI._boxStyle : GUI.skin.box;

                    // Draw with fallback in case styles don't have proper background
                    Color oldColor = GUI.backgroundColor;

                    // Draw main window background
                    GUI.backgroundColor = new Color(0.1f, 0.1f, 0.2f, 0.95f);
                    GUI.Box(_windowRect, "", boxStyle);

                    // Draw title bar
                    Rect titleRect = new Rect(X, Y, Width, 25);
                    GUI.backgroundColor = new Color(0.2f, 0.2f, 0.4f, 0.95f);
                    GUI.Box(titleRect, Title, windowStyle);

                    // Restore color
                    GUI.backgroundColor = oldColor;

                    // Draw all controls
                    foreach (var control in _controls)
                    {
                        if (control.IsVisible)
                        {
                            control.Draw(X, Y);
                        }
                    }

                    // Handle dragging if enabled
                    if (IsDraggable && Event.current != null &&
                        Event.current.type == EventType.MouseDrag &&
                        titleRect.Contains(Event.current.mousePosition))
                    {
                        X += Event.current.delta.x;
                        Y += Event.current.delta.y;
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error drawing window '{Id}': {ex.Message}");
                    }
                }
            }
        }

        #endregion

        #region UI Style Functions

        /// <summary>
        /// Sets the window style colors
        /// </summary>
        /// <param name="colorName">Name of the color to set: "background", "text", "hover", "active"</param>
        /// <param name="r">Red component (0-1)</param>
        /// <param name="g">Green component (0-1)</param>
        /// <param name="b">Blue component (0-1)</param>
        /// <param name="a">Alpha component (0-1)</param>
        public static void SetWindowStyle(string colorName, float r, float g, float b, float a = 1.0f)
        {
            try
            {
                if (!_stylesInitialized)
                    InitializeStyles();

                SetStyleColor(_windowStyle, colorName, r, g, b, a);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in SetWindowStyle: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the button style colors
        /// </summary>
        /// <param name="colorName">Name of the color to set: "background", "text", "hover", "active"</param>
        /// <param name="r">Red component (0-1)</param>
        /// <param name="g">Green component (0-1)</param>
        /// <param name="b">Blue component (0-1)</param>
        /// <param name="a">Alpha component (0-1)</param>
        public static void SetButtonStyle(string colorName, float r, float g, float b, float a = 1.0f)
        {
            try
            {
                if (!_stylesInitialized)
                    InitializeStyles();

                SetStyleColor(_buttonStyle, colorName, r, g, b, a);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in SetButtonStyle: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the label style colors
        /// </summary>
        /// <param name="colorName">Name of the color to set: "background", "text"</param>
        /// <param name="r">Red component (0-1)</param>
        /// <param name="g">Green component (0-1)</param>
        /// <param name="b">Blue component (0-1)</param>
        /// <param name="a">Alpha component (0-1)</param>
        public static void SetLabelStyle(string colorName, float r, float g, float b, float a = 1.0f)
        {
            try
            {
                if (!_stylesInitialized)
                    InitializeStyles();

                SetStyleColor(_labelStyle, colorName, r, g, b, a);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in SetLabelStyle: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the text field style colors
        /// </summary>
        /// <param name="colorName">Name of the color to set: "background", "text"</param>
        /// <param name="r">Red component (0-1)</param>
        /// <param name="g">Green component (0-1)</param>
        /// <param name="b">Blue component (0-1)</param>
        /// <param name="a">Alpha component (0-1)</param>
        public static void SetTextFieldStyle(string colorName, float r, float g, float b, float a = 1.0f)
        {
            try
            {
                if (!_stylesInitialized)
                    InitializeStyles();

                SetStyleColor(_textFieldStyle, colorName, r, g, b, a);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in SetTextFieldStyle: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the box style colors
        /// </summary>
        /// <param name="colorName">Name of the color to set: "background", "text"</param>
        /// <param name="r">Red component (0-1)</param>
        /// <param name="g">Green component (0-1)</param>
        /// <param name="b">Blue component (0-1)</param>
        /// <param name="a">Alpha component (0-1)</param>
        public static void SetBoxStyle(string colorName, float r, float g, float b, float a = 1.0f)
        {
            try
            {
                if (!_stylesInitialized)
                    InitializeStyles();

                SetStyleColor(_boxStyle, colorName, r, g, b, a);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in SetBoxStyle: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to set a color on a style
        /// </summary>
        private static void SetStyleColor(GUIStyle style, string colorName, float r, float g, float b, float a)
        {
            if (style == null)
                return;

            Color color = new Color(r, g, b, a);

            switch (colorName.ToLower())
            {
                case "background":
                    var tex = MakeColorTexture(color);
                    if (tex != null)
                    {
                        style.normal.background = tex;
                        style.onNormal.background = tex;
                    }
                    break;
                case "text":
                    style.normal.textColor = color;
                    style.onNormal.textColor = color;
                    break;
                case "hover":
                    var hoverTex = MakeColorTexture(color);
                    if (hoverTex != null)
                    {
                        style.hover.background = hoverTex;
                        style.onHover.background = hoverTex;
                    }
                    style.hover.textColor = color;
                    style.onHover.textColor = color;
                    break;
                case "active":
                    var activeTex = MakeColorTexture(color);
                    if (activeTex != null)
                    {
                        style.active.background = activeTex;
                        style.onActive.background = activeTex;
                    }
                    style.active.textColor = color;
                    style.onActive.textColor = color;
                    break;
                default:
                    _logger.Warning($"Unknown color name: {colorName}");
                    break;
            }
        }

        /// <summary>
        /// Sets the font size for a UI element style
        /// </summary>
        /// <param name="styleName">Name of the style: "window", "button", "label", "textfield", "box"</param>
        /// <param name="size">Font size</param>
        public static void SetFontSize(string styleName, int size)
        {
            try
            {
                if (!_stylesInitialized)
                    InitializeStyles();

                GUIStyle style = GetStyleByName(styleName);
                if (style != null)
                {
                    style.fontSize = size;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in SetFontSize: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the font style for a UI element
        /// </summary>
        /// <param name="styleName">Name of the style: "window", "button", "label", "textfield", "box"</param>
        /// <param name="fontStyle">Font style: "normal", "bold", "italic", "bolditalic"</param>
        public static void SetFontStyle(string styleName, string fontStyle)
        {
            try
            {
                if (!_stylesInitialized)
                    InitializeStyles();

                GUIStyle style = GetStyleByName(styleName);
                if (style != null)
                {
                    switch (fontStyle.ToLower())
                    {
                        case "normal":
                            style.fontStyle = FontStyle.Normal;
                            break;
                        case "bold":
                            style.fontStyle = FontStyle.Bold;
                            break;
                        case "italic":
                            style.fontStyle = FontStyle.Italic;
                            break;
                        case "bolditalic":
                            style.fontStyle = FontStyle.BoldAndItalic;
                            break;
                        default:
                            _logger.Warning($"Unknown font style: {fontStyle}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in SetFontStyle: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the text alignment for a UI element
        /// </summary>
        /// <param name="styleName">Name of the style: "window", "button", "label", "textfield", "box"</param>
        /// <param name="alignment">Alignment: "left", "center", "right", "topleft", "topcenter", "topright", 
        /// "middleleft", "middlecenter", "middleright", "bottomleft", "bottomcenter", "bottomright"</param>
        public static void SetTextAlignment(string styleName, string alignment)
        {
            try
            {
                if (!_stylesInitialized)
                    InitializeStyles();

                GUIStyle style = GetStyleByName(styleName);
                if (style != null)
                {
                    switch (alignment.ToLower())
                    {
                        case "left":
                            style.alignment = TextAnchor.MiddleLeft;
                            break;
                        case "center":
                            style.alignment = TextAnchor.MiddleCenter;
                            break;
                        case "right":
                            style.alignment = TextAnchor.MiddleRight;
                            break;
                        case "topleft":
                            style.alignment = TextAnchor.UpperLeft;
                            break;
                        case "topcenter":
                            style.alignment = TextAnchor.UpperCenter;
                            break;
                        case "topright":
                            style.alignment = TextAnchor.UpperRight;
                            break;
                        case "middleleft":
                            style.alignment = TextAnchor.MiddleLeft;
                            break;
                        case "middlecenter":
                            style.alignment = TextAnchor.MiddleCenter;
                            break;
                        case "middleright":
                            style.alignment = TextAnchor.MiddleRight;
                            break;
                        case "bottomleft":
                            style.alignment = TextAnchor.LowerLeft;
                            break;
                        case "bottomcenter":
                            style.alignment = TextAnchor.LowerCenter;
                            break;
                        case "bottomright":
                            style.alignment = TextAnchor.LowerRight;
                            break;
                        default:
                            _logger.Warning($"Unknown text alignment: {alignment}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in SetTextAlignment: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the border for a UI element style
        /// </summary>
        /// <param name="styleName">Name of the style: "window", "button", "label", "textfield", "box"</param>
        /// <param name="left">Left border width</param>
        /// <param name="right">Right border width</param>
        /// <param name="top">Top border width</param>
        /// <param name="bottom">Bottom border width</param>
        public static void SetBorder(string styleName, int left, int right, int top, int bottom)
        {
            try
            {
                if (!_stylesInitialized)
                    InitializeStyles();

                GUIStyle style = GetStyleByName(styleName);
                if (style != null)
                {
                    style.border = new RectOffset(left, right, top, bottom);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in SetBorder: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the padding for a UI element style
        /// </summary>
        /// <param name="styleName">Name of the style: "window", "button", "label", "textfield", "box"</param>
        /// <param name="left">Left padding</param>
        /// <param name="right">Right padding</param>
        /// <param name="top">Top padding</param>
        /// <param name="bottom">Bottom padding</param>
        public static void SetPadding(string styleName, int left, int right, int top, int bottom)
        {
            try
            {
                if (!_stylesInitialized)
                    InitializeStyles();

                GUIStyle style = GetStyleByName(styleName);
                if (style != null)
                {
                    style.padding = new RectOffset(left, right, top, bottom);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in SetPadding: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to get a style by name
        /// </summary>
        private static GUIStyle GetStyleByName(string styleName)
        {
            switch (styleName.ToLower())
            {
                case "window":
                    return _windowStyle;
                case "button":
                    return _buttonStyle;
                case "label":
                    return _labelStyle;
                case "textfield":
                    return _textFieldStyle;
                case "box":
                    return _boxStyle;
                default:
                    _logger.Warning($"Unknown style name: {styleName}");
                    return null;
            }
        }

        #endregion
    }
}