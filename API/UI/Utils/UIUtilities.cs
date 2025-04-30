using System;
using UnityEngine;
using System.IO;
using ScheduleLua.API.Core;
using ScheduleLua.API.UI;
using MoonSharp.Interpreter;
using ScheduleOne.UI.Items;

namespace ScheduleLua.API.UI.Utils
{
    /// <summary>
    /// Provides utility functions for UI
    /// </summary>
    public static class UIUtilities
    {
        /// <summary>
        /// Draw a colored box with a border
        /// </summary>
        public static void DrawColoredBox(Rect position, string text, Color backgroundColor, Color borderColor, GUIStyle style)
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
        /// Loads a sprite from a file path
        /// </summary>
        public static Sprite LoadSpriteFromFile(string filePath, string scriptPath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    LuaUtility.LogWarning("LoadSpriteFromFile: filePath is null or empty");
                    return null;
                }

                string fullPath = filePath;
                if (!Path.IsPathRooted(filePath))
                {
                    if (!string.IsNullOrEmpty(scriptPath))
                    {
                        string scriptDir = Path.GetDirectoryName(scriptPath);
                        fullPath = Path.Combine(scriptDir, filePath);
                        fullPath = Path.GetFullPath(fullPath);
                    }
                    else
                    {
                        // Fallback to global context if scriptPath is not provided
                        string fallbackScriptPath = "unknown";
                        try
                        {
                            var scriptPathValue = ModCore.Instance._luaEngine.Globals.Get("SCRIPT_PATH");
                            if (scriptPathValue != null && scriptPathValue.Type == DataType.String)
                                fallbackScriptPath = scriptPathValue.String;
                        }
                        catch { }
                        
                        if (fallbackScriptPath != "unknown" && !string.IsNullOrEmpty(fallbackScriptPath))
                        {
                            string scriptDir = Path.GetDirectoryName(fallbackScriptPath);
                            fullPath = Path.Combine(scriptDir, filePath);
                            fullPath = Path.GetFullPath(fullPath);
                        }
                        else
                        {
                            fullPath = Path.Combine(Application.dataPath, "..", filePath);
                            fullPath = Path.GetFullPath(fullPath);
                        }
                    }
                }

                if (!File.Exists(fullPath))
                {
                    LuaUtility.LogWarning($"LoadSpriteFromFile: File not found at path: {fullPath}");
                    return null;
                }

                byte[] fileData = File.ReadAllBytes(fullPath);
                Texture2D texture = new Texture2D(2, 2);
                if (texture.LoadImage(fileData))
                {
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f));
                    return sprite;
                }
                else
                {
                    LuaUtility.LogWarning($"LoadSpriteFromFile: Failed to load image data from {filePath}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error loading sprite from file: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Get script path from Lua context
        /// </summary>
        public static string GetScriptPathFromContext(ScriptExecutionContext ctx)
        {
            try
            {
                if (ctx == null)
                    return null;
                
                Table env = UIManager.GetCallingEnvironment(ctx);
                if (env != null)
                {
                    var scriptPathVal = env.Get("SCRIPT_PATH");
                    if (scriptPathVal != null && scriptPathVal.Type == DataType.String)
                        return scriptPathVal.String;
                }
            }
            catch { }
            
            return null;
        }

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
                LuaUtility.LogError($"Error in GetHoveredItemName: {ex.Message}", ex);
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
                LuaUtility.LogError($"Error in IsItemBeingDragged: {ex.Message}", ex);
                return false;
            }
        }
    }
} 