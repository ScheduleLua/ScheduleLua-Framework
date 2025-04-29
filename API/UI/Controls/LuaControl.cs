using System;
using UnityEngine;
using ScheduleLua.API.Core;

namespace ScheduleLua.API.UI.Controls
{
    /// <summary>
    /// Base class for all Lua UI controls
    /// </summary>
    public abstract class LuaControl
    {
        /// <summary>
        /// Unique identifier for this control
        /// </summary>
        public string Id { get; private set; }
        
        /// <summary>
        /// ID of the window this control belongs to
        /// </summary>
        public string WindowId { get; private set; }
        
        /// <summary>
        /// X position relative to its window
        /// </summary>
        public float X { get; set; } = 10;
        
        /// <summary>
        /// Y position relative to its window
        /// </summary>
        public float Y { get; set; } = 10;
        
        /// <summary>
        /// Width of the control
        /// </summary>
        public float Width { get; set; } = 150;
        
        /// <summary>
        /// Height of the control
        /// </summary>
        public float Height { get; set; } = 30;
        
        /// <summary>
        /// Whether the control is visible
        /// </summary>
        public bool IsVisible { get; set; } = true;
        
        /// <summary>
        /// Text content of the control
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Creates a new control with the specified ID, window ID, and text
        /// </summary>
        protected LuaControl(string id, string windowId, string text)
        {
            Id = id;
            WindowId = windowId;
            Text = text;
        }

        /// <summary>
        /// Draws the control at the specified window position
        /// </summary>
        public abstract void Draw(float windowX, float windowY);

        /// <summary>
        /// Gets the rectangle for this control, adjusted for window position
        /// </summary>
        protected Rect GetRect(float windowX, float windowY)
        {
            return new Rect(windowX + X, windowY + Y, Width, Height);
        }
    }
} 