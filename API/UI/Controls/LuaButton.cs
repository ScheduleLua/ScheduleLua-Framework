using System;
using UnityEngine;
using MoonSharp.Interpreter;
using ScheduleLua.API.Core;
using ScheduleLua.API.UI.Styles;
using ScheduleLua.API.UI;

namespace ScheduleLua.API.UI.Controls
{
    /// <summary>
    /// Button control for Lua scripts
    /// </summary>
    public class LuaButton : LuaControl
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
                var buttonStyle = UIManager.StyleManager.ButtonStyle ?? GUI.skin.button;
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
                        ModCore.Instance._luaEngine.Call(_callback);
                    }
                }

                // Restore color
                GUI.backgroundColor = oldColor;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error drawing button '{Id}': {ex.Message}", ex);
            }
        }
    }
} 