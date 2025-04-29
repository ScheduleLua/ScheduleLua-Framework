using System;
using System.Collections.Generic;
using ScheduleLua.API.Core;
using ScheduleLua.API.UI.Windows;
using UnityEngine;

namespace ScheduleLua.API.UI.Windows
{
    /// <summary>
    /// Manages windows for the UI system
    /// </summary>
    public class WindowManager
    {
        // Store for registered GUI windows
        private Dictionary<string, LuaWindow> _windows = new Dictionary<string, LuaWindow>();

        /// <summary>
        /// Creates a new window
        /// </summary>
        public string CreateWindow(string id, string title, float x, float y, float width, float height)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    id = Guid.NewGuid().ToString();
                }
                else if (_windows.ContainsKey(id))
                {
                    // If window already exists, just return the existing ID
                    return id;
                }

                var window = new LuaWindow(id, title, x, y, width, height);
                _windows[id] = window;

                LuaUtility.Log($"Created window '{id}' ({title}) at ({x},{y}) with size ({width}x{height})");
                return id;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error creating window: {ex.Message}", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Sets a window's position
        /// </summary>
        public void SetWindowPosition(string windowId, float x, float y)
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
                    LuaUtility.LogWarning($"SetWindowPosition: Window '{windowId}' not found");
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error setting window position: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sets a window's size
        /// </summary>
        public void SetWindowSize(string windowId, float width, float height)
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
                    LuaUtility.LogWarning($"SetWindowSize: Window '{windowId}' not found");
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error setting window size: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Shows or hides a window
        /// </summary>
        public void ShowWindow(string windowId, bool visible)
        {
            try
            {
                if (_windows.TryGetValue(windowId, out var window))
                {
                    window.IsVisible = visible;
                }
                else
                {
                    LuaUtility.LogWarning($"ShowWindow: Window '{windowId}' not found");
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error showing/hiding window: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks if a window is visible
        /// </summary>
        public bool IsWindowVisible(string windowId)
        {
            try
            {
                if (_windows.TryGetValue(windowId, out var window))
                {
                    return window.IsVisible;
                }
                else
                {
                    LuaUtility.LogWarning($"IsWindowVisible: Window '{windowId}' not found");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error checking window visibility: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets a window by ID
        /// </summary>
        public LuaWindow GetWindow(string windowId)
        {
            if (_windows.TryGetValue(windowId, out var window))
            {
                return window;
            }
            return null;
        }

        /// <summary>
        /// Destroys a window
        /// </summary>
        public void DestroyWindow(string windowId)
        {
            try
            {
                if (_windows.ContainsKey(windowId))
                {
                    _windows.Remove(windowId);
                }
                else
                {
                    LuaUtility.LogWarning($"DestroyWindow: Window '{windowId}' not found");
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error destroying window: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Draws all windows
        /// </summary>
        public void DrawAllWindows()
        {
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
                LuaUtility.LogError($"Error in DrawAllWindows: {ex.Message}", ex);
            }
        }
    }
} 