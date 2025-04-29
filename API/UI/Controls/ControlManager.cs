using System;
using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;
using ScheduleLua.API.Core;
using ScheduleLua.API.UI.Windows;

namespace ScheduleLua.API.UI.Controls
{
    /// <summary>
    /// Manages all UI controls for the Lua UI system
    /// </summary>
    public class ControlManager
    {
        // Dictionary to store all controls
        private Dictionary<string, LuaControl> _controls = new Dictionary<string, LuaControl>();
        private WindowManager _windowManager;

        public ControlManager(WindowManager windowManager)
        {
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
        }

        /// <summary>
        /// Adds a button to a window
        /// </summary>
        public string AddButton(string windowId, string id, string text, DynValue callback)
        {
            try
            {
                var window = _windowManager.GetWindow(windowId);
                if (window == null)
                {
                    LuaUtility.LogWarning($"AddButton: Window '{windowId}' not found");
                    return string.Empty;
                }

                if (string.IsNullOrEmpty(id))
                {
                    id = Guid.NewGuid().ToString();
                }
                else if (_controls.ContainsKey(id))
                {
                    // Control already exists, just return its ID
                    return id;
                }

                var button = new LuaButton(id, windowId, text, callback);
                window.AddControl(button);
                _controls[id] = button;

                return id;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error adding button: {ex.Message}", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Adds a label to a window
        /// </summary>
        public string AddLabel(string windowId, string id, string text)
        {
            try
            {
                var window = _windowManager.GetWindow(windowId);
                if (window == null)
                {
                    LuaUtility.LogWarning($"AddLabel: Window '{windowId}' not found");
                    return string.Empty;
                }

                if (string.IsNullOrEmpty(id))
                {
                    id = Guid.NewGuid().ToString();
                }
                else if (_controls.ContainsKey(id))
                {
                    // Control already exists, just return its ID
                    return id;
                }

                var label = new LuaLabel(id, windowId, text);
                window.AddControl(label);
                _controls[id] = label;

                return id;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error adding label: {ex.Message}", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Adds a text field to a window
        /// </summary>
        public string AddTextField(string windowId, string id, string text)
        {
            try
            {
                var window = _windowManager.GetWindow(windowId);
                if (window == null)
                {
                    LuaUtility.LogWarning($"AddTextField: Window '{windowId}' not found");
                    return string.Empty;
                }

                if (string.IsNullOrEmpty(id))
                {
                    id = Guid.NewGuid().ToString();
                }
                else if (_controls.ContainsKey(id))
                {
                    // Control already exists, just return its ID
                    return id;
                }

                var textField = new LuaTextField(id, windowId, text);
                window.AddControl(textField);
                _controls[id] = textField;

                return id;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error adding text field: {ex.Message}", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the text of a control
        /// </summary>
        public string GetControlText(string controlId)
        {
            try
            {
                if (_controls.TryGetValue(controlId, out var control))
                {
                    return control.Text;
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error getting control text: {ex.Message}", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Sets the text of a control
        /// </summary>
        public void SetControlText(string controlId, string text)
        {
            try
            {
                if (_controls.TryGetValue(controlId, out var control))
                {
                    control.Text = text;
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error setting control text: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sets the position of a control within its window
        /// </summary>
        public void SetControlPosition(string controlId, float x, float y)
        {
            try
            {
                if (_controls.TryGetValue(controlId, out var control))
                {
                    control.X = x;
                    control.Y = y;
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error setting control position: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sets the size of a control
        /// </summary>
        public void SetControlSize(string controlId, float width, float height)
        {
            try
            {
                if (_controls.TryGetValue(controlId, out var control))
                {
                    control.Width = width;
                    control.Height = height;
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error setting control size: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Shows or hides a control
        /// </summary>
        public void ShowControl(string controlId, bool visible)
        {
            try
            {
                if (_controls.TryGetValue(controlId, out var control))
                {
                    control.IsVisible = visible;
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error showing/hiding control: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Destroys a control and removes it from its window
        /// </summary>
        public void DestroyControl(string controlId)
        {
            try
            {
                if (_controls.TryGetValue(controlId, out var control))
                {
                    var window = _windowManager.GetWindow(control.WindowId);
                    if (window != null)
                    {
                        window.RemoveControl(control);
                    }
                    _controls.Remove(controlId);
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error destroying control: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets a control by ID
        /// </summary>
        public LuaControl GetControl(string controlId)
        {
            if (_controls.TryGetValue(controlId, out var control))
            {
                return control;
            }
            return null;
        }
    }
} 