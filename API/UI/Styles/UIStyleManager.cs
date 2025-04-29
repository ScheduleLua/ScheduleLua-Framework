using System;
using UnityEngine;
using ScheduleLua.API.Core;

namespace ScheduleLua.API.UI.Styles
{
    /// <summary>
    /// Manages styles for UI elements
    /// </summary>
    public class UIStyleManager
    {
        // GUI styles
        private GUIStyle _windowStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _textFieldStyle;
        
        // State tracking
        public bool IsInitialized { get; private set; } = false;
        private bool _needsFullRefresh = false;
        
        // Cache for color settings to recreate styles exactly
        private Color _windowBgColor = new Color(0.1f, 0.1f, 0.2f, 0.95f);
        private Color _windowTextColor = Color.white;
        private Color _buttonBgColor = new Color(0.3f, 0.3f, 0.8f, 0.9f);
        private Color _buttonTextColor = Color.white;
        private Color _buttonHoverColor = new Color(0.4f, 0.4f, 0.9f, 0.9f);
        private Color _buttonActiveColor = new Color(0.5f, 0.5f, 1.0f, 0.9f);
        private Color _labelTextColor = Color.white;
        private Color _boxBgColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        private Color _boxTextColor = Color.white;
        private Color _textFieldBgColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        private Color _textFieldTextColor = Color.white;
        
        // GUI style properties with automatic initialization
        public GUIStyle WindowStyle 
        { 
            get 
            {
                if (_windowStyle == null)
                {
                    EnsureStyles();
                }
                return _windowStyle;
            }
            private set { _windowStyle = value; }
        }
        
        public GUIStyle TitleStyle 
        { 
            get 
            {
                if (_titleStyle == null)
                {
                    EnsureStyles();
                }
                return _titleStyle;
            }
            private set { _titleStyle = value; }
        }
        
        public GUIStyle ButtonStyle 
        { 
            get 
            {
                if (_buttonStyle == null)
                {
                    EnsureStyles();
                }
                return _buttonStyle;
            }
            private set { _buttonStyle = value; }
        }
        
        public GUIStyle LabelStyle 
        { 
            get 
            {
                if (_labelStyle == null)
                {
                    EnsureStyles();
                }
                return _labelStyle;
            }
            private set { _labelStyle = value; }
        }
        
        public GUIStyle BoxStyle 
        { 
            get 
            {
                if (_boxStyle == null)
                {
                    EnsureStyles();
                }
                return _boxStyle;
            }
            private set { _boxStyle = value; }
        }
        
        public GUIStyle TextFieldStyle 
        { 
            get 
            {
                if (_textFieldStyle == null)
                {
                    EnsureStyles();
                }
                return _textFieldStyle;
            }
            private set { _textFieldStyle = value; }
        }

        public UIStyleManager()
        {
            
        }

        /// <summary>
        /// Initializes UIStyleManager
        /// </summary>
        public void Initialize()
        {
            // Only set the flag - actual styles will be created later in OnGUI
            IsInitialized = true;
            // Do not call InitializeStyles() here - it must only be called from OnGUI
        }
        
        /// <summary>
        /// Forces a complete refresh of all styles
        /// This is useful when themes are changed
        /// </summary>
        public void RefreshStyles()
        {
            // Clear existing styles to force recreation
            _windowStyle = null;
            _titleStyle = null;
            _buttonStyle = null;
            _labelStyle = null;
            _boxStyle = null;
            _textFieldStyle = null;
            
            // Mark for full refresh on next OnGUI call
            _needsFullRefresh = true;
            
            // Don't initialize here - wait for OnGUI to call InitializeStyles
            LuaUtility.Log("UI styles marked for refresh");
        }

        /// <summary>
        /// Creates and initializes all GUI styles - call this only from OnGUI
        /// </summary>
        public void InitializeStyles()
        {
            try
            {
                // If we need a full refresh or haven't initialized yet, recreate all styles
                if (_needsFullRefresh || _windowStyle == null)
                {
                    LuaUtility.Log("Fully reinitializing UI styles");
                    CreateAllStyles();
                    _needsFullRefresh = false;
                }
                // Otherwise just ensure styles are valid
                else
                {
                    EnsureStyles();
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error initializing styles: {ex.Message}", ex);
                // Use default styles if custom initialization fails
                _windowStyle = new GUIStyle(GUI.skin.window);
                _buttonStyle = new GUIStyle(GUI.skin.button);
                _labelStyle = new GUIStyle(GUI.skin.label);
                _boxStyle = new GUIStyle(GUI.skin.box);
                _textFieldStyle = new GUIStyle(GUI.skin.textField);
            }
        }
        
        /// <summary>
        /// Creates all UI styles from scratch using cached color values
        /// </summary>
        private void CreateAllStyles()
        {
            // Create window style
            _windowStyle = new GUIStyle(GUI.skin.window);
            var windowTex = MakeColorTexture(_windowBgColor);
            if (windowTex != null)
            {
                _windowStyle.normal.background = windowTex;
                _windowStyle.onNormal.background = windowTex;
                _windowStyle.hover.background = windowTex;
                _windowStyle.onHover.background = windowTex;
                _windowStyle.active.background = windowTex;
                _windowStyle.onActive.background = windowTex;
                _windowStyle.focused.background = windowTex;
                _windowStyle.onFocused.background = windowTex;
            }
            _windowStyle.border = new RectOffset(10, 10, 10, 10);
            _windowStyle.padding = new RectOffset(10, 10, 25, 10);
            _windowStyle.normal.textColor = _windowTextColor;
            _windowStyle.onNormal.textColor = _windowTextColor;
            _windowStyle.hover.textColor = _windowTextColor;
            _windowStyle.onHover.textColor = _windowTextColor;
            _windowStyle.active.textColor = _windowTextColor;
            _windowStyle.onActive.textColor = _windowTextColor;
            _windowStyle.focused.textColor = _windowTextColor;
            _windowStyle.onFocused.textColor = _windowTextColor;
            _windowStyle.fontStyle = FontStyle.Bold;
            _windowStyle.fontSize = 16;

            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.alignment = TextAnchor.MiddleCenter;
            _titleStyle.fontSize = 18;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.normal.textColor = _windowTextColor;
            _titleStyle.hover.textColor = _windowTextColor;
            _titleStyle.active.textColor = _windowTextColor;
            _titleStyle.focused.textColor = _windowTextColor;

            // Create button style
            _buttonStyle = new GUIStyle(GUI.skin.button);
            var buttonTex = MakeColorTexture(_buttonBgColor);
            if (buttonTex != null)
            {
                _buttonStyle.normal.background = buttonTex;
                _buttonStyle.onNormal.background = buttonTex;
            }
            var buttonHoverTex = MakeColorTexture(_buttonHoverColor);
            if (buttonHoverTex != null)
            {
                _buttonStyle.hover.background = buttonHoverTex;
                _buttonStyle.onHover.background = buttonHoverTex;
            }
            var buttonActiveTex = MakeColorTexture(_buttonActiveColor);
            if (buttonActiveTex != null)
            {
                _buttonStyle.active.background = buttonActiveTex;
                _buttonStyle.onActive.background = buttonActiveTex;
                _buttonStyle.focused.background = buttonActiveTex;
                _buttonStyle.onFocused.background = buttonActiveTex;
            }
            _buttonStyle.normal.textColor = _buttonTextColor;
            _buttonStyle.hover.textColor = _buttonTextColor;
            _buttonStyle.active.textColor = _buttonTextColor;
            _buttonStyle.focused.textColor = _buttonTextColor;
            _buttonStyle.onNormal.textColor = _buttonTextColor;
            _buttonStyle.onHover.textColor = _buttonTextColor;
            _buttonStyle.onActive.textColor = _buttonTextColor;
            _buttonStyle.onFocused.textColor = _buttonTextColor;
            _buttonStyle.fontSize = 14;
            _buttonStyle.fontStyle = FontStyle.Bold;
            _buttonStyle.alignment = TextAnchor.MiddleCenter;
            _buttonStyle.border = new RectOffset(5, 5, 5, 5);

            // Create label style
            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.normal.textColor = _labelTextColor;
            _labelStyle.hover.textColor = _labelTextColor;
            _labelStyle.active.textColor = _labelTextColor;
            _labelStyle.focused.textColor = _labelTextColor;
            _labelStyle.onNormal.textColor = _labelTextColor;
            _labelStyle.onHover.textColor = _labelTextColor;
            _labelStyle.onActive.textColor = _labelTextColor;
            _labelStyle.onFocused.textColor = _labelTextColor;
            _labelStyle.fontSize = 14;
            _labelStyle.fontStyle = FontStyle.Normal;
            _labelStyle.wordWrap = true;

            // Create box style
            _boxStyle = new GUIStyle(GUI.skin.box);
            var boxTex = MakeColorTexture(_boxBgColor);
            if (boxTex != null)
            {
                _boxStyle.normal.background = boxTex;
                _boxStyle.onNormal.background = boxTex;
                _boxStyle.hover.background = boxTex;
                _boxStyle.onHover.background = boxTex;
                _boxStyle.active.background = boxTex;
                _boxStyle.onActive.background = boxTex;
                _boxStyle.focused.background = boxTex;
                _boxStyle.onFocused.background = boxTex;
            }
            _boxStyle.normal.textColor = _boxTextColor;
            _boxStyle.hover.textColor = _boxTextColor;
            _boxStyle.active.textColor = _boxTextColor;
            _boxStyle.focused.textColor = _boxTextColor;
            _boxStyle.onNormal.textColor = _boxTextColor;
            _boxStyle.onHover.textColor = _boxTextColor;
            _boxStyle.onActive.textColor = _boxTextColor;
            _boxStyle.onFocused.textColor = _boxTextColor;
            _boxStyle.border = new RectOffset(5, 5, 5, 5);

            // Create text field style
            _textFieldStyle = new GUIStyle(GUI.skin.textField);
            _textFieldStyle.normal.textColor = _textFieldTextColor;
            _textFieldStyle.hover.textColor = _textFieldTextColor;
            _textFieldStyle.active.textColor = _textFieldTextColor;
            _textFieldStyle.focused.textColor = _textFieldTextColor;
            _textFieldStyle.onNormal.textColor = _textFieldTextColor;
            _textFieldStyle.onHover.textColor = _textFieldTextColor;
            _textFieldStyle.onActive.textColor = _textFieldTextColor;
            _textFieldStyle.onFocused.textColor = _textFieldTextColor;
            var textFieldTex = MakeColorTexture(_textFieldBgColor);
            if (textFieldTex != null)
            {
                _textFieldStyle.normal.background = textFieldTex;
                _textFieldStyle.onNormal.background = textFieldTex;
                _textFieldStyle.hover.background = textFieldTex;
                _textFieldStyle.onHover.background = textFieldTex;
                _textFieldStyle.active.background = textFieldTex;
                _textFieldStyle.onActive.background = textFieldTex;
                _textFieldStyle.focused.background = textFieldTex;
                _textFieldStyle.onFocused.background = textFieldTex;
            }
            _textFieldStyle.fontSize = 14;
        }

        // Helper method to ensure styles are created when accessed
        private void EnsureStyles()
        {
            if (_windowStyle == null || _buttonStyle == null || _labelStyle == null || _boxStyle == null || _textFieldStyle == null)
            {
                CreateAllStyles();
                
                // Double-check if any style is still null after initialization
                if (_windowStyle == null) _windowStyle = new GUIStyle(GUI.skin.window);
                if (_buttonStyle == null) _buttonStyle = new GUIStyle(GUI.skin.button);
                if (_labelStyle == null) _labelStyle = new GUIStyle(GUI.skin.label);
                if (_boxStyle == null) _boxStyle = new GUIStyle(GUI.skin.box);
                if (_textFieldStyle == null) _textFieldStyle = new GUIStyle(GUI.skin.textField);
            }
        }

        /// <summary>
        /// Creates a solid color texture for GUI elements
        /// </summary>
        private Texture2D MakeColorTexture(Color color)
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
                LuaUtility.LogError($"Error creating texture: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Sets the window style colors
        /// </summary>
        public void SetWindowStyle(string colorName, float r, float g, float b, float a = 1.0f)
        {
            try
            {
                if (!IsInitialized)
                    Initialize();

                Color color = new Color(r, g, b, a);
                
                // Store the colors for later recreation
                switch (colorName.ToLower())
                {
                    case "background":
                        _windowBgColor = color;
                        break;
                    case "text":
                        _windowTextColor = color;
                        break;
                }
                
                // Force recreation of styles on next frame
                _needsFullRefresh = true;

                // Just try to update current style if it exists (will be overwritten on next OnGUI)
                if (_windowStyle != null)
                {
                    SetStyleColor(_windowStyle, colorName, r, g, b, a);
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in SetWindowStyle: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sets the button style colors
        /// </summary>
        public void SetButtonStyle(string colorName, float r, float g, float b, float a = 1.0f)
        {
            try
            {
                if (!IsInitialized)
                    Initialize();

                Color color = new Color(r, g, b, a);
                
                // Store the colors for later recreation
                switch (colorName.ToLower())
                {
                    case "background":
                        _buttonBgColor = color;
                        break;
                    case "text":
                        _buttonTextColor = color;
                        break;
                    case "hover":
                        _buttonHoverColor = color;
                        break;
                    case "active":
                        _buttonActiveColor = color;
                        break;
                }
                
                // Force recreation of styles on next frame
                _needsFullRefresh = true;

                // Just try to update current style if it exists
                if (_buttonStyle != null)
                {
                    SetStyleColor(_buttonStyle, colorName, r, g, b, a);
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in SetButtonStyle: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sets the label style colors
        /// </summary>
        public void SetLabelStyle(string colorName, float r, float g, float b, float a = 1.0f)
        {
            try
            {
                if (!IsInitialized)
                    Initialize();

                Color color = new Color(r, g, b, a);
                
                // Store the color for later recreation
                if (colorName.ToLower() == "text")
                {
                    _labelTextColor = color;
                }
                
                // Force recreation of styles on next frame
                _needsFullRefresh = true;

                // Just try to update current style if it exists
                if (_labelStyle != null)
                {
                    SetStyleColor(_labelStyle, colorName, r, g, b, a);
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in SetLabelStyle: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sets the text field style colors
        /// </summary>
        public void SetTextFieldStyle(string colorName, float r, float g, float b, float a = 1.0f)
        {
            try
            {
                if (!IsInitialized)
                    Initialize();

                Color color = new Color(r, g, b, a);
                
                // Store the colors for later recreation
                switch (colorName.ToLower())
                {
                    case "background":
                        _textFieldBgColor = color;
                        break;
                    case "text":
                        _textFieldTextColor = color;
                        break;
                }
                
                // Force recreation of styles on next frame
                _needsFullRefresh = true;

                // Just try to update current style if it exists
                if (_textFieldStyle != null)
                {
                    SetStyleColor(_textFieldStyle, colorName, r, g, b, a);
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in SetTextFieldStyle: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sets the box style colors
        /// </summary>
        public void SetBoxStyle(string colorName, float r, float g, float b, float a = 1.0f)
        {
            try
            {
                if (!IsInitialized)
                    Initialize();

                Color color = new Color(r, g, b, a);
                
                // Store the colors for later recreation
                switch (colorName.ToLower())
                {
                    case "background":
                        _boxBgColor = color;
                        break;
                    case "text":
                        _boxTextColor = color;
                        break;
                }
                
                // Force recreation of styles on next frame
                _needsFullRefresh = true;

                // Just try to update current style if it exists
                if (_boxStyle != null)
                {
                    SetStyleColor(_boxStyle, colorName, r, g, b, a);
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in SetBoxStyle: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Helper method to set a color on a style
        /// </summary>
        private void SetStyleColor(GUIStyle style, string colorName, float r, float g, float b, float a)
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
                        // Apply to all normal states for better coverage
                        style.normal.background = tex;
                        style.onNormal.background = tex;
                        
                        // Also ensure the background is properly visible by setting additional states
                        // if they don't already have backgrounds
                        if (style.hover.background == null)
                            style.hover.background = tex;
                        if (style.onHover.background == null)
                            style.onHover.background = tex;
                        if (style.active.background == null)
                            style.active.background = tex;
                        if (style.onActive.background == null)
                            style.onActive.background = tex;
                        if (style.focused.background == null)
                            style.focused.background = tex;
                        if (style.onFocused.background == null)
                            style.onFocused.background = tex;
                    }
                    break;
                case "text":
                    // Set text color for all states
                    style.normal.textColor = color;
                    style.onNormal.textColor = color;
                    style.hover.textColor = color;
                    style.onHover.textColor = color;
                    style.active.textColor = color;
                    style.onActive.textColor = color;
                    style.focused.textColor = color;
                    style.onFocused.textColor = color;
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
                    LuaUtility.LogWarning($"Unknown color name: {colorName}");
                    break;
            }
        }

        /// <summary>
        /// Sets the font size for a UI element style
        /// </summary>
        public void SetFontSize(string styleName, int size)
        {
            try
            {
                if (!IsInitialized)
                    Initialize();

                // Force style refresh on next frame
                _needsFullRefresh = true;

                // Update immediately if possible                
                GUIStyle style = GetStyleByName(styleName);
                if (style != null)
                {
                    style.fontSize = size;
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in SetFontSize: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sets the font style for a UI element
        /// </summary>
        public void SetFontStyle(string styleName, string fontStyle)
        {
            try
            {
                if (!IsInitialized)
                    Initialize();
                
                // Force style refresh on next frame
                _needsFullRefresh = true;

                // Update immediately if possible
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
                            LuaUtility.LogWarning($"Unknown font style: {fontStyle}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in SetFontStyle: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sets the text alignment for a UI element
        /// </summary>
        public void SetTextAlignment(string styleName, string alignment)
        {
            try
            {
                if (!IsInitialized)
                    Initialize();
                
                // Force style refresh on next frame
                _needsFullRefresh = true;

                // Update immediately if possible
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
                            LuaUtility.LogWarning($"Unknown text alignment: {alignment}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in SetTextAlignment: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sets the border for a UI element style
        /// </summary>
        public void SetBorder(string styleName, int left, int right, int top, int bottom)
        {
            try
            {
                if (!IsInitialized)
                    Initialize();
                
                // Force style refresh on next frame
                _needsFullRefresh = true;

                // Update immediately if possible
                GUIStyle style = GetStyleByName(styleName);
                if (style != null)
                {
                    style.border = new RectOffset(left, right, top, bottom);
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in SetBorder: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sets the padding for a UI element style
        /// </summary>
        public void SetPadding(string styleName, int left, int right, int top, int bottom)
        {
            try
            {
                if (!IsInitialized)
                    Initialize();
                
                // Force style refresh on next frame
                _needsFullRefresh = true;

                // Update immediately if possible
                GUIStyle style = GetStyleByName(styleName);
                if (style != null)
                {
                    style.padding = new RectOffset(left, right, top, bottom);
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in SetPadding: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Helper method to get a style by name
        /// </summary>
        private GUIStyle GetStyleByName(string styleName)
        {
            EnsureStyles();
            
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
                    LuaUtility.LogWarning($"Unknown style name: {styleName}");
                    return null;
            }
        }
    }
} 