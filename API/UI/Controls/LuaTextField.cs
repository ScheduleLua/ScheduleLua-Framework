using ScheduleLua.API.Core;
using UnityEngine;

namespace ScheduleLua.API.UI.Controls
{
    /// <summary>
    /// Text field control for Lua scripts
    /// </summary>
    public class LuaTextField : LuaControl
    {
        public LuaTextField(string id, string windowId, string text)
            : base(id, windowId, text)
        {
        }

        public override void Draw(float windowX, float windowY)
        {
            try
            {
                var textFieldStyle = UIManager.StyleManager.TextFieldStyle ?? GUI.skin.textField;
                Rect rect = GetRect(windowX, windowY);

                // Draw a background for the text field for better visibility
                Color oldColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);

                // Draw the text field with our custom style and update the text
                string newText = GUI.TextField(rect, Text, textFieldStyle);
                if (newText != Text)
                {
                    Text = newText;
                }

                // Restore color
                GUI.backgroundColor = oldColor;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error drawing text field '{Id}': {ex.Message}", ex);
            }
        }
    }
}