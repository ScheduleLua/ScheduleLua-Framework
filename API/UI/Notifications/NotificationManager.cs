using System;
using System.IO;
using UnityEngine;
using ScheduleOne.UI;
using ScheduleLua.API.Core;
using ScheduleLua.API.UI.Utils;
using ScheduleLua.API.UI;
using MoonSharp.Interpreter;

namespace ScheduleLua.API.UI.Notifications
{
    /// <summary>
    /// Manages in-game notifications for Lua scripts
    /// </summary>
    public class NotificationManager
    {
        /// <summary>
        /// Shows a notification to the player
        /// </summary>
        public void ShowNotification(string title, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(message))
                {
                    LuaUtility.LogWarning("ShowNotification: message is null or empty");
                    return;
                }

                var notificationsManager = UnityEngine.Object.FindObjectOfType<NotificationsManager>();
                if (notificationsManager == null)
                {
                    LuaUtility.LogWarning("ShowNotification: NotificationsManager not available");
                    return;
                }

                notificationsManager.SendNotification(title, message, null);
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in ShowNotification: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Shows a notification to the player with a custom icon
        /// </summary>
        public void ShowNotificationWithIcon(string title, string message, string iconPath)
        {
            try
            {
                if (string.IsNullOrEmpty(message))
                {
                    LuaUtility.LogWarning("ShowNotificationWithIcon: message is null or empty");
                    return;
                }

                var notificationsManager = UnityEngine.Object.FindObjectOfType<NotificationsManager>();
                if (notificationsManager == null)
                {
                    LuaUtility.LogWarning("ShowNotificationWithIcon: NotificationsManager not available");
                    return;
                }

                // Load the icon from the file path
                Sprite icon = UIUtilities.LoadSpriteFromFile(iconPath);
                notificationsManager.SendNotification(title, message, icon);
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in ShowNotificationWithIcon: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Shows a notification to the player with a custom icon and script path context
        /// </summary>
        public void ShowNotificationWithIcon(string title, string message, string iconPath, string scriptPath)
        {
            try
            {
                if (string.IsNullOrEmpty(message))
                {
                    LuaUtility.LogWarning("ShowNotificationWithIcon: message is null or empty");
                    return;
                }

                var notificationsManager = UnityEngine.Object.FindObjectOfType<NotificationsManager>();
                if (notificationsManager == null)
                {
                    LuaUtility.LogWarning("ShowNotificationWithIcon: NotificationsManager not available");
                    return;
                }

                // Load the icon from the file path with script path context
                Sprite icon = UIUtilities.LoadSpriteFromFile(iconPath, scriptPath);
                notificationsManager.SendNotification(title, message, icon);
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in ShowNotificationWithIcon: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Shows a notification to the player with a custom timeout
        /// </summary>
        public void ShowNotificationWithTimeout(string message, float timeout)
        {
            try
            {
                if (string.IsNullOrEmpty(message))
                {
                    LuaUtility.LogWarning("ShowNotificationWithTimeout: message is null or empty");
                    return;
                }

                var notificationsManager = UnityEngine.Object.FindObjectOfType<NotificationsManager>();
                if (notificationsManager == null)
                {
                    LuaUtility.LogWarning("ShowNotificationWithTimeout: NotificationsManager not available");
                    return;
                }

                notificationsManager.SendNotification("Lua Notification", message, null, timeout);
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in ShowNotificationWithTimeout: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Shows a notification to the player with an icon and custom timeout
        /// </summary>
        public void ShowNotificationWithIconAndTimeout(string title, string message, string iconPath, float timeout)
        {
            try
            {
                if (string.IsNullOrEmpty(message))
                {
                    LuaUtility.LogWarning("ShowNotificationWithIconAndTimeout: message is null or empty");
                    return;
                }

                var notificationsManager = UnityEngine.Object.FindObjectOfType<NotificationsManager>();
                if (notificationsManager == null)
                {
                    LuaUtility.LogWarning("ShowNotificationWithIconAndTimeout: NotificationsManager not available");
                    return;
                }

                // Load the icon from the file path
                Sprite icon = UIUtilities.LoadSpriteFromFile(iconPath);
                notificationsManager.SendNotification(title, message, icon, timeout);
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in ShowNotificationWithIconAndTimeout: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Shows a notification to the player with an icon, custom timeout, and script path context
        /// </summary>
        public void ShowNotificationWithIconAndTimeout(string title, string message, string iconPath, float timeout, string scriptPath)
        {
            try
            {
                if (string.IsNullOrEmpty(message))
                {
                    LuaUtility.LogWarning("ShowNotificationWithIconAndTimeout: message is null or empty");
                    return;
                }

                var notificationsManager = UnityEngine.Object.FindObjectOfType<NotificationsManager>();
                if (notificationsManager == null)
                {
                    LuaUtility.LogWarning("ShowNotificationWithIconAndTimeout: NotificationsManager not available");
                    return;
                }

                // Load the icon from the file path with script path context
                Sprite icon = UIUtilities.LoadSpriteFromFile(iconPath, scriptPath);
                notificationsManager.SendNotification(title, message, icon, timeout);
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in ShowNotificationWithIconAndTimeout: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// DynCallback for ShowNotificationWithIcon
        /// </summary>
        public DynValue ShowNotificationWithIconDyn(ScriptExecutionContext ctx, CallbackArguments args)
        {
            string title = args[0].CastToString();
            string message = args[1].CastToString();
            string iconPath = args[2].CastToString();
            string scriptPath = null;
            try
            {
                Table env = UIManager.GetCallingEnvironment(ctx);
                if (env != null)
                {
                    var scriptPathVal = env.Get("SCRIPT_PATH");
                    if (scriptPathVal != null && scriptPathVal.Type == DataType.String)
                        scriptPath = scriptPathVal.String;
                }
            }
            catch { }
            ShowNotificationWithIcon(title, message, iconPath, scriptPath);
            return DynValue.Nil;
        }

        /// <summary>
        /// DynCallback for ShowNotificationWithIconAndTimeout
        /// </summary>
        public DynValue ShowNotificationWithIconAndTimeoutDyn(ScriptExecutionContext ctx, CallbackArguments args)
        {
            string title = args[0].CastToString();
            string message = args[1].CastToString();
            string iconPath = args[2].CastToString();
            float timeout = (float)args[3].CastToNumber();
            string scriptPath = null;
            try
            {
                Table env = UIManager.GetCallingEnvironment(ctx);
                if (env != null)
                {
                    var scriptPathVal = env.Get("SCRIPT_PATH");
                    if (scriptPathVal != null && scriptPathVal.Type == DataType.String)
                        scriptPath = scriptPathVal.String;
                }
            }
            catch { }
            ShowNotificationWithIconAndTimeout(title, message, iconPath, timeout, scriptPath);
            return DynValue.Nil;
        }
    }
} 