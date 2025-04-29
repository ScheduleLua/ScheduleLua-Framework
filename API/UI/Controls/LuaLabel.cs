using System;
using UnityEngine;
using ScheduleLua.API.Core;
using ScheduleLua.API.UI.Styles;
using ScheduleLua.API.UI;

namespace ScheduleLua.API.UI.Controls
{
    /// <summary>
    /// Label control for Lua scripts
    /// </summary>
    public class LuaLabel : LuaControl
    {
        public LuaLabel(string id, string windowId, string text)
            : base(id, windowId, text)
        {
        }

        public override void Draw(float windowX, float windowY)
        {
            try
            {
                var labelStyle = UIManager.StyleManager.LabelStyle ?? GUI.skin.label;
                var boxStyle = UIManager.StyleManager.BoxStyle ?? GUI.skin.box;
                Rect rect = GetRect(windowX, windowY);

                // Draw a background for the label for better visibility
                Color oldColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                GUI.Box(rect, "", boxStyle);

                // Draw the label with our custom style
                GUI.Label(rect, Text, labelStyle);

                // Restore color
                GUI.backgroundColor = oldColor;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error drawing label '{Id}': {ex.Message}", ex);
            }
        }
    }
} 