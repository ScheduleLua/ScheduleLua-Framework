using ScheduleLua.API.Core;
using ScheduleLua.API.UI.Controls;
using UnityEngine;

namespace ScheduleLua.API.UI.Windows
{
    /// <summary>
    /// Window container for Lua UI elements
    /// </summary>
    public class LuaWindow
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

                // Get the appropriate styles - always use fresh references with fallbacks
                var windowStyle = UIManager.StyleManager.WindowStyle ?? GUI.skin.window;
                var boxStyle = UIManager.StyleManager.BoxStyle ?? GUI.skin.box;
                var titleStyle = UIManager.StyleManager.TitleStyle ?? GUI.skin.label;

                // Draw with fallback in case styles don't have proper background
                Color oldColor = GUI.backgroundColor;

                // Draw main window background
                GUI.backgroundColor = new Color(0.1f, 0.1f, 0.2f, 0.95f);
                GUI.Box(_windowRect, "", boxStyle);

                // Draw title bar
                Rect titleRect = new Rect(X, Y, Width, 35);
                GUI.backgroundColor = new Color(0.2f, 0.2f, 0.4f, 0.95f);
                GUI.Box(titleRect, Title, titleStyle);

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
                LuaUtility.LogError($"Error drawing window '{Id}': {ex.Message}", ex);
            }
        }
    }
}