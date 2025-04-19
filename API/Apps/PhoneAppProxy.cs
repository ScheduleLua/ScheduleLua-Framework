using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ScheduleLua.API.Apps
{
    /// <summary>
    /// Phone app information storage class
    /// </summary>
    public class PhoneAppInfo
    {
        public string AppName { get; set; }
        public string DisplayName { get; set; }
        public GameObject AppObject { get; set; }
        public RectTransform Container { get; set; }
        public Dictionary<string, UIElementInfo> Elements { get; set; }
        public bool Active { get; set; }
    }

    /// <summary>
    /// Proxy class that exposes phone app functionality to Lua
    /// </summary>
    [MoonSharpUserData]
    public class PhoneAppProxy
    {
        internal PhoneAppInfo AppInfo { get; private set; }

        public PhoneAppProxy(PhoneAppInfo appInfo)
        {
            AppInfo = appInfo;
        }

        /// <summary>
        /// Gets the internal name of the app
        /// </summary>
        public string GetName()
        {
            return AppInfo?.AppName;
        }

        /// <summary>
        /// Gets the display name of the app
        /// </summary>
        public string GetDisplayName()
        {
            return AppInfo?.DisplayName;
        }

        /// <summary>
        /// Sets the display name of the app
        /// </summary>
        public void SetDisplayName(string displayName)
        {
            if (AppInfo != null)
            {
                AppInfo.DisplayName = displayName;

                // Update label on icon if it exists
                var iconGrid = GameObject.Find("Player_Local/CameraContainer/Camera/OverlayCamera/GameplayMenu/Phone/phone/HomeScreen/AppIcons");
                if (iconGrid != null)
                {
                    foreach (Transform child in iconGrid.transform)
                    {
                        if (child.name == AppInfo.AppName + "Icon")
                        {
                            var label = child.Find("Label")?.GetComponent<Text>();
                            if (label != null)
                            {
                                label.text = displayName;
                            }
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks if the app is currently active (visible)
        /// </summary>
        public bool IsActive()
        {
            return AppInfo?.Active ?? false;
        }

        /// <summary>
        /// Sets the app's active state
        /// </summary>
        public void SetActive(bool active)
        {
            if (AppInfo?.AppObject != null)
            {
                AppInfo.AppObject.SetActive(active);
                AppInfo.Active = active;
            }
        }

        /// <summary>
        /// Removes the app and its icon
        /// </summary>
        public void Remove()
        {
            if (AppInfo != null)
            {
                AppsAPI.RemovePhoneAppByName(AppInfo.AppName);
            }
        }

        /// <summary>
        /// Gets a list of all UI elements in the app
        /// </summary>
        public Table GetElements()
        {
            var table = new Table(ScheduleLua.Core.Instance._luaEngine);

            if (AppInfo?.Elements != null)
            {
                int index = 1;
                foreach (var element in AppInfo.Elements.Values)
                {
                    table[index++] = new UIElementProxy(element);
                }
            }

            return table;
        }

        /// <summary>
        /// Clears all UI elements from the app
        /// </summary>
        public void ClearElements()
        {
            if (AppInfo?.Container != null && AppInfo?.Elements != null)
            {
                foreach (var element in AppInfo.Elements.Values)
                {
                    if (element.GameObject != null)
                    {
                        GameObject.Destroy(element.GameObject);
                    }
                }

                AppInfo.Elements.Clear();
            }
        }

        /// <summary>
        /// Gets the app's width
        /// </summary>
        public float GetWidth()
        {
            if (AppInfo?.Container != null)
            {
                return AppInfo.Container.rect.width;
            }
            return 0;
        }

        /// <summary>
        /// Gets the app's height
        /// </summary>
        public float GetHeight()
        {
            if (AppInfo?.Container != null)
            {
                return AppInfo.Container.rect.height;
            }
            return 0;
        }
    }
}