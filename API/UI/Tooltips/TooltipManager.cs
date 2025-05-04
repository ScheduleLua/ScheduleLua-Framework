using ScheduleLua.API.Core;
using UnityEngine;

namespace ScheduleLua.API.UI.Tooltips
{
    /// <summary>
    /// Manages tooltip display for the UI system
    /// </summary>
    public class TooltipManager
    {
        private string _tooltipText;
        private Vector2 _tooltipPosition;
        private bool _tooltipVisible;
        private bool _worldspaceTooltip;
        private float _hideTime;

        /// <summary>
        /// Shows a tooltip at the specified position
        /// </summary>
        public void ShowTooltip(string text, float x, float y, bool worldspace = false)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                {
                    LuaUtility.LogWarning("ShowTooltip: Text is null or empty");
                    return;
                }

                _tooltipText = text;
                _tooltipPosition = new Vector2(x, y);
                _tooltipVisible = true;
                _worldspaceTooltip = worldspace;
                _hideTime = Time.time + 3.0f; // Hide after 3 seconds
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error showing tooltip: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Hides the current tooltip
        /// </summary>
        public void HideTooltip()
        {
            _tooltipVisible = false;
        }

        /// <summary>
        /// Draws tooltips if any are visible
        /// </summary>
        public void DrawTooltips()
        {
            try
            {
                if (!_tooltipVisible)
                    return;

                // Auto-hide after timeout
                if (Time.time > _hideTime)
                {
                    _tooltipVisible = false;
                    return;
                }

                // Determine position
                Vector2 position = _tooltipPosition;
                if (_worldspaceTooltip)
                {
                    // Convert world position to screen position
                    Camera mainCamera = Camera.main;
                    if (mainCamera != null)
                    {
                        position = mainCamera.WorldToScreenPoint(new Vector3(position.x, position.y, 0));
                    }
                }

                // Measure text
                GUIContent content = new GUIContent(_tooltipText);
                GUIStyle style = new GUIStyle(GUI.skin.box);
                style.fontSize = 14;
                style.normal.textColor = Color.white;
                style.wordWrap = true;
                style.padding = new RectOffset(8, 8, 8, 8);

                // Determine size
                Vector2 size = style.CalcSize(content);
                size.x = Mathf.Min(size.x, 250); // Max width
                size.y = style.CalcHeight(content, size.x);

                // Draw the tooltip
                Rect rect = new Rect(position.x, position.y, size.x, size.y);

                // Adjust position to ensure tooltip stays on screen
                rect = EnsureRectIsOnScreen(rect);

                // Draw background
                Color oldColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
                GUI.Box(rect, "", style);

                // Draw border
                Color oldContentColor = GUI.contentColor;
                GUI.contentColor = new Color(0.7f, 0.7f, 0.9f, 1f);
                GUI.Box(rect, "", GUI.skin.box);

                // Draw text
                GUI.contentColor = Color.white;
                GUI.Label(rect, _tooltipText, style);

                // Restore colors
                GUI.backgroundColor = oldColor;
                GUI.contentColor = oldContentColor;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in DrawTooltips: {ex.Message}", ex);
                _tooltipVisible = false;
            }
        }

        /// <summary>
        /// Ensures the rect stays within screen boundaries
        /// </summary>
        private Rect EnsureRectIsOnScreen(Rect rect)
        {
            // Get screen size
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            // Adjust x position
            if (rect.xMax > screenWidth)
            {
                rect.x = screenWidth - rect.width;
            }
            if (rect.x < 0)
            {
                rect.x = 0;
            }

            // Adjust y position
            if (rect.yMax > screenHeight)
            {
                rect.y = screenHeight - rect.height;
            }
            if (rect.y < 0)
            {
                rect.y = 0;
            }

            return rect;
        }
    }
}