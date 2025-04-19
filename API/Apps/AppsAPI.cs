using MelonLoader;
using MoonSharp.Interpreter;
using ScheduleOne.UI.Phone;
using ScheduleLua.API.Core;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using System.Collections.Generic;
using FluffyUnderware.Curvy.ThirdParty.LibTessDotNet;
using ScheduleOne.DevUtilities;

namespace ScheduleLua.API.Apps
{
    /// <summary>
    /// API for creating and managing phone apps in Schedule I
    /// </summary>
    public static class AppsAPI
    {
        private static MelonLogger.Instance _logger => ScheduleLua.Core.Instance.LoggerInstance;

        // Dictionary to keep track of created apps by Lua scripts
        private static Dictionary<string, PhoneAppInfo> _createdApps = new Dictionary<string, PhoneAppInfo>();

        /// <summary>
        /// Registers the Apps API with the Lua engine
        /// </summary>
        public static void RegisterAPI(Script luaEngine)
        {
            // Register the API functions
            luaEngine.Globals["CreatePhoneApp"] = (Func<string, string, string, PhoneAppProxy>)CreatePhoneApp;
            luaEngine.Globals["SetAppActive"] = (Action<PhoneAppProxy, bool>)SetAppActive;
            luaEngine.Globals["RemovePhoneApp"] = (Action<PhoneAppProxy>)RemovePhoneApp;
            luaEngine.Globals["RemovePhoneAppByName"] = (Action<string>)RemovePhoneAppByName;
            luaEngine.Globals["GetAllCreatedApps"] = (Func<Table>)GetAllCreatedApps;
            luaEngine.Globals["IsAppActive"] = (Func<PhoneAppProxy, bool>)IsAppActive;

            // Advanced API functions
            luaEngine.Globals["CreateAdvancedApp"] = (Func<string, string, string, AppBaseProxy>)CreateAdvancedApp;

            // Advanced UI Manipulation
            luaEngine.Globals["AddTextToApp"] = (Func<PhoneAppProxy, string, float, float, float, float, UIElementProxy>)AddTextToApp;
            luaEngine.Globals["AddButtonToApp"] = (Func<PhoneAppProxy, string, float, float, float, float, DynValue, UIElementProxy>)AddButtonToApp;
            luaEngine.Globals["AddImageToApp"] = (Func<PhoneAppProxy, string, float, float, float, float, UIElementProxy>)AddImageToApp;

            // Register helper classes
            UserData.RegisterType<PhoneAppProxy>();
            UserData.RegisterType<UIElementProxy>();
            UserData.RegisterType<AppBaseProxy>();
            UserData.RegisterType<ScrollViewProxy>();
            UserData.RegisterType<PanelProxy>();
        }

        /// <summary>
        /// Creates a new phone app with the specified name, label, and icon
        /// </summary>
        /// <param name="appName">Internal name of the app</param>
        /// <param name="displayName">Display name shown under the app icon</param>
        /// <param name="iconPath">Path to the icon image file relative to the script directory</param>
        /// <returns>A proxy object representing the created phone app</returns>
        public static PhoneAppProxy CreatePhoneApp(string appName, string displayName, string iconPath)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrEmpty(appName))
                {
                    _logger.Error("CreatePhoneApp: appName cannot be null or empty");
                    return null;
                }

                if (string.IsNullOrEmpty(displayName))
                {
                    _logger.Error("CreatePhoneApp: displayName cannot be null or empty");
                    return null;
                }

                // Check if app with this name already exists
                if (_createdApps.ContainsKey(appName))
                {
                    _logger.Warning($"App with name '{appName}' already exists. Returning existing app.");
                    return new PhoneAppProxy(_createdApps[appName]);
                }

                // Get the AppsCanvas instance
                var appsCanvas = PlayerSingleton<AppsCanvas>.Instance;
                if (appsCanvas == null)
                {
                    _logger.Error("CreatePhoneApp: AppsCanvas instance not found");
                    return null;
                }

                // Create app from template (ProductManagerApp)
                var templateApp = appsCanvas.canvas.transform.Find("ProductManagerApp");
                if (templateApp == null)
                {
                    _logger.Error("CreatePhoneApp: ProductManagerApp template not found");
                    return null;
                }

                // Clone the template
                var appObject = UnityEngine.Object.Instantiate(templateApp.gameObject, appsCanvas.canvas.transform);
                appObject.name = appName;
                appObject.SetActive(false);

                // Remove the ProductManagerApp component if it exists
                var oldAppComponent = appObject.GetComponent<ScheduleOne.UI.Phone.ProductManagerApp.ProductManagerApp>();
                if (oldAppComponent != null)
                {
                    UnityEngine.Object.Destroy(oldAppComponent);
                }

                // Find container and clear its contents
                var container = appObject.transform.Find("Container")?.GetComponent<RectTransform>();
                if (container != null)
                {
                    foreach (Transform child in container)
                    {
                        UnityEngine.Object.Destroy(child.gameObject);
                    }
                }

                // Create app info
                var appInfo = new PhoneAppInfo
                {
                    AppName = appName,
                    DisplayName = displayName,
                    AppObject = appObject,
                    Container = container,
                    Elements = new Dictionary<string, UIElementInfo>(),
                    Active = false
                };

                // Create app icon
                CreateAppIcon(appName, displayName, iconPath, appObject);

                // Store the app info
                _createdApps[appName] = appInfo;

                _logger.Msg($"Successfully created phone app: {appName}");

                return new PhoneAppProxy(appInfo);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error creating phone app: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a new advanced phone app with the specified name, label, and icon
        /// </summary>
        public static AppBaseProxy CreateAdvancedApp(string appName, string displayName, string iconPath)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrEmpty(appName))
                {
                    _logger.Error("CreateAdvancedApp: appName cannot be null or empty");
                    return null;
                }

                if (string.IsNullOrEmpty(displayName))
                {
                    _logger.Error("CreateAdvancedApp: displayName cannot be null or empty");
                    return null;
                }

                // Check if app with this name already exists
                if (_createdApps.ContainsKey(appName))
                {
                    _logger.Warning($"App with name '{appName}' already exists.");
                    var existingAppInfo = _createdApps[appName];
                    return new AppBaseProxy(existingAppInfo);
                }

                // Get the AppsCanvas instance
                var appsCanvas = PlayerSingleton<AppsCanvas>.Instance;
                if (appsCanvas == null)
                {
                    _logger.Error("CreateAdvancedApp: AppsCanvas instance not found");
                    return null;
                }

                // Create app from template (ProductManagerApp)
                var templateApp = appsCanvas.canvas.transform.Find("ProductManagerApp");
                if (templateApp == null)
                {
                    _logger.Error("CreateAdvancedApp: ProductManagerApp template not found");
                    return null;
                }

                // Clone the template
                var appObject = UnityEngine.Object.Instantiate(templateApp.gameObject, appsCanvas.canvas.transform);
                appObject.name = appName;
                appObject.SetActive(false);

                // Remove the ProductManagerApp component if it exists
                var oldAppComponent = appObject.GetComponent<ScheduleOne.UI.Phone.ProductManagerApp.ProductManagerApp>();
                if (oldAppComponent != null)
                {
                    UnityEngine.Object.Destroy(oldAppComponent);
                }

                // Find container and clear its contents
                var container = appObject.transform.Find("Container")?.GetComponent<RectTransform>();
                if (container != null)
                {
                    foreach (Transform child in container)
                    {
                        UnityEngine.Object.Destroy(child.gameObject);
                    }

                    // Add vertical layout group to container
                    var layoutGroup = container.gameObject.AddComponent<VerticalLayoutGroup>();
                    layoutGroup.childAlignment = TextAnchor.UpperCenter;
                    layoutGroup.padding = new RectOffset(10, 10, 10, 10);
                    layoutGroup.spacing = 10;
                    layoutGroup.childControlWidth = true;
                    layoutGroup.childControlHeight = false;
                    layoutGroup.childForceExpandWidth = true;
                    layoutGroup.childForceExpandHeight = false;
                }

                // Create app info
                var appInfo = new PhoneAppInfo
                {
                    AppName = appName,
                    DisplayName = displayName,
                    AppObject = appObject,
                    Container = container,
                    Elements = new Dictionary<string, UIElementInfo>(),
                    Active = false
                };

                // Create app icon
                CreateAppIcon(appName, displayName, iconPath, appObject);

                // Store the app info
                _createdApps[appName] = appInfo;

                // Create app proxy
                var appProxy = new AppBaseProxy(appInfo);

                _logger.Msg($"Successfully created advanced phone app: {appName}");

                return appProxy;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error creating advanced phone app: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates an app icon in the phone home screen
        /// </summary>
        private static void CreateAppIcon(string appName, string displayName, string iconPath, GameObject appObject)
        {
            try
            {
                // Find the icon grid
                var iconGrid = GameObject.Find("Player_Local/CameraContainer/Camera/OverlayCamera/GameplayMenu/Phone/phone/HomeScreen/AppIcons");
                if (iconGrid == null)
                {
                    _logger.Error("CreateAppIcon: AppIcons grid not found");
                    return;
                }

                // Remove existing icon with the same name if it exists
                foreach (Transform child in iconGrid.transform)
                {
                    if (child.name == appName + "Icon")
                    {
                        UnityEngine.Object.Destroy(child.gameObject);
                        break;
                    }
                }

                // Use an existing icon as template (typically index 6 is the Products app)
                Transform templateIcon = iconGrid.transform.GetChild(6);
                if (templateIcon == null)
                {
                    _logger.Error("CreateAppIcon: Template icon not found at index 6");
                    return;
                }

                // Clone the template icon
                GameObject newIcon = UnityEngine.Object.Instantiate(templateIcon.gameObject, iconGrid.transform);
                newIcon.name = appName + "Icon";

                // Set the display name
                newIcon.transform.Find("Label").GetComponent<Text>().text = displayName;

                // Load and set the icon image if provided
                if (!string.IsNullOrEmpty(iconPath))
                {
                    Sprite iconSprite = LoadImageFromPath(iconPath);
                    if (iconSprite != null)
                    {
                        newIcon.transform.Find("Mask/Image").GetComponent<Image>().sprite = iconSprite;
                    }
                    else
                    {
                        _logger.Warning($"Failed to load icon image from: {iconPath}");
                    }
                }

                // Add click handler to open the app
                var button = newIcon.GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => {
                    // Hide duplicate icons if any
                    RemoveDuplicateIcons(displayName, iconGrid);

                    // Activate the app
                    appObject.SetActive(true);

                    // Update active state
                    if (_createdApps.TryGetValue(appName, out var appInfo))
                    {
                        appInfo.Active = true;

                        // Check for AppBaseProxy and call OnShow
                        if (appObject.GetComponent<AppBaseProxy>() is AppBaseProxy appProxy)
                        {
                            appProxy.OnShow();
                        }
                    }
                });

                _logger.Msg($"Created icon for app: {appName}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error creating app icon: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes duplicate app icons with the same display name
        /// </summary>
        private static void RemoveDuplicateIcons(string displayName, GameObject iconGrid)
        {
            foreach (Transform icon in iconGrid.transform)
            {
                var label = icon.Find("Label")?.GetComponent<Text>();
                if (label != null && label.text == displayName && icon.name.Contains("(Clone)"))
                {
                    UnityEngine.Object.Destroy(icon.gameObject);
                    _logger.Msg($"Removed duplicate icon for: {displayName}");
                }
            }
        }

        /// <summary>
        /// Loads an image from a file path
        /// </summary>
        public static Sprite LoadImageFromPath(string filePath)
        {
            try
            {
                // First try as absolute path
                if (!File.Exists(filePath))
                {
                    // Try relative to the mods directory
                    string modsPath = MelonLoader.Utils.MelonEnvironment.ModsDirectory;
                    string fullPath = Path.Combine(modsPath, "ScheduleLua", filePath);

                    if (!File.Exists(fullPath))
                    {
                        // Try in Resources directory
                        fullPath = Path.Combine(modsPath, "ScheduleLua", "Resources", filePath);

                        if (!File.Exists(fullPath))
                        {
                            _logger.Error($"Image file not found: {filePath}");
                            return null;
                        }
                    }

                    filePath = fullPath;
                }

                byte[] imageData = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2);

                if (texture.LoadImage(imageData))
                {
                    return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }
                else
                {
                    _logger.Error($"Failed to load image data from: {filePath}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error loading image: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sets the active state of a phone app
        /// </summary>
        public static void SetAppActive(PhoneAppProxy app, bool active)
        {
            if (app == null || app.AppInfo == null || app.AppInfo.AppObject == null)
            {
                _logger.Error("SetAppActive: Invalid app reference");
                return;
            }

            app.AppInfo.AppObject.SetActive(active);
            app.AppInfo.Active = active;
        }

        /// <summary>
        /// Checks if an app is currently active (visible)
        /// </summary>
        public static bool IsAppActive(PhoneAppProxy app)
        {
            if (app == null || app.AppInfo == null || app.AppInfo.AppObject == null)
            {
                _logger.Error("IsAppActive: Invalid app reference");
                return false;
            }

            return app.AppInfo.Active;
        }

        /// <summary>
        /// Removes a phone app and its icon
        /// </summary>
        public static void RemovePhoneApp(PhoneAppProxy app)
        {
            if (app == null || app.AppInfo == null)
            {
                _logger.Error("RemovePhoneApp: Invalid app reference");
                return;
            }

            RemovePhoneAppByName(app.AppInfo.AppName);
        }

        /// <summary>
        /// Removes a phone app by its name
        /// </summary>
        public static void RemovePhoneAppByName(string appName)
        {
            try
            {
                if (string.IsNullOrEmpty(appName))
                {
                    _logger.Error("RemovePhoneAppByName: appName cannot be null or empty");
                    return;
                }

                if (!_createdApps.TryGetValue(appName, out var appInfo))
                {
                    _logger.Warning($"App with name '{appName}' not found");
                    return;
                }

                // Destroy app object
                if (appInfo.AppObject != null)
                {
                    UnityEngine.Object.Destroy(appInfo.AppObject);
                }

                // Remove app icon
                var iconGrid = GameObject.Find("Player_Local/CameraContainer/Camera/OverlayCamera/GameplayMenu/Phone/phone/HomeScreen/AppIcons");
                if (iconGrid != null)
                {
                    foreach (Transform child in iconGrid.transform)
                    {
                        if (child.name == appName + "Icon")
                        {
                            UnityEngine.Object.Destroy(child.gameObject);
                            break;
                        }
                    }
                }

                // Remove from dictionary
                _createdApps.Remove(appName);

                _logger.Msg($"Successfully removed phone app: {appName}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error removing phone app: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns a table of all created apps
        /// </summary>
        public static Table GetAllCreatedApps()
        {
            var table = new Table(ScheduleLua.Core.Instance._luaEngine);
            int index = 1;

            foreach (var app in _createdApps.Values)
            {
                table[index++] = new PhoneAppProxy(app);
            }

            return table;
        }

        /// <summary>
        /// Adds a text element to a phone app
        /// </summary>
        public static UIElementProxy AddTextToApp(PhoneAppProxy app, string text, float x, float y, float width, float height)
        {
            try
            {
                if (app == null || app.AppInfo == null)
                {
                    _logger.Error("AddTextToApp: Invalid app reference");
                    return null;
                }

                if (app.AppInfo.Container == null)
                {
                    _logger.Error("AddTextToApp: App container not found");
                    return null;
                }

                // Create text object
                GameObject textObject = new GameObject("Text_" + Guid.NewGuid().ToString().Substring(0, 8));
                textObject.transform.SetParent(app.AppInfo.Container, false);

                // Add Text component
                Text textComponent = textObject.AddComponent<Text>();
                textComponent.text = text;
                textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                textComponent.color = Color.black;
                textComponent.fontSize = 18;
                textComponent.alignment = TextAnchor.UpperLeft;

                // Position and size
                RectTransform rectTransform = textObject.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(x, y);
                rectTransform.sizeDelta = new Vector2(width, height);

                // Create element info
                string elementId = "text_" + Guid.NewGuid().ToString().Substring(0, 8);
                var elementInfo = new UIElementInfo
                {
                    Id = elementId,
                    GameObject = textObject,
                    Type = UIElementType.Text
                };

                // Add to app's elements
                app.AppInfo.Elements[elementId] = elementInfo;

                return new UIElementProxy(elementInfo);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error adding text to app: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Adds a button element to a phone app
        /// </summary>
        public static UIElementProxy AddButtonToApp(PhoneAppProxy app, string text, float x, float y, float width, float height, DynValue callback)
        {
            try
            {
                if (app == null || app.AppInfo == null)
                {
                    _logger.Error("AddButtonToApp: Invalid app reference");
                    return null;
                }

                if (app.AppInfo.Container == null)
                {
                    _logger.Error("AddButtonToApp: App container not found");
                    return null;
                }

                if (callback == null || callback.Type != DataType.Function)
                {
                    _logger.Error("AddButtonToApp: Callback must be a valid function");
                    return null;
                }

                // Create button object
                GameObject buttonObject = new GameObject("Button_" + Guid.NewGuid().ToString().Substring(0, 8));
                buttonObject.transform.SetParent(app.AppInfo.Container, false);

                // Add Image component (for button background)
                Image image = buttonObject.AddComponent<Image>();
                image.color = new Color(0.9f, 0.9f, 0.9f);

                // Add Button component
                Button button = buttonObject.AddComponent<Button>();
                ColorBlock colors = button.colors;
                colors.normalColor = new Color(0.9f, 0.9f, 0.9f);
                colors.highlightedColor = new Color(0.8f, 0.8f, 0.8f);
                colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
                button.colors = colors;

                // Add Text child
                GameObject textObject = new GameObject("Text");
                textObject.transform.SetParent(buttonObject.transform, false);
                Text textComponent = textObject.AddComponent<Text>();
                textComponent.text = text;
                textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                textComponent.color = Color.black;
                textComponent.fontSize = 16;
                textComponent.alignment = TextAnchor.MiddleCenter;

                // Position and size for button
                RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(x, y);
                rectTransform.sizeDelta = new Vector2(width, height);

                // Position and size for text
                RectTransform textRectTransform = textObject.GetComponent<RectTransform>();
                textRectTransform.anchorMin = Vector2.zero;
                textRectTransform.anchorMax = Vector2.one;
                textRectTransform.offsetMin = Vector2.zero;
                textRectTransform.offsetMax = Vector2.zero;

                // Create element info
                string elementId = "button_" + Guid.NewGuid().ToString().Substring(0, 8);
                var elementInfo = new UIElementInfo
                {
                    Id = elementId,
                    GameObject = buttonObject,
                    Type = UIElementType.Button,
                    Callback = callback
                };

                // Add to app's elements
                app.AppInfo.Elements[elementId] = elementInfo;

                // Add click handler
                button.onClick.AddListener(() => {
                    try
                    {
                        ScheduleLua.Core.Instance._luaEngine.Call(callback);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error in button callback: {ex.Message}");
                    }
                });

                return new UIElementProxy(elementInfo);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error adding button to app: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Adds an image element to a phone app
        /// </summary>
        public static UIElementProxy AddImageToApp(PhoneAppProxy app, string imagePath, float x, float y, float width, float height)
        {
            try
            {
                if (app == null || app.AppInfo == null)
                {
                    _logger.Error("AddImageToApp: Invalid app reference");
                    return null;
                }

                if (app.AppInfo.Container == null)
                {
                    _logger.Error("AddImageToApp: App container not found");
                    return null;
                }

                // Create image object
                GameObject imageObject = new GameObject("Image_" + Guid.NewGuid().ToString().Substring(0, 8));
                imageObject.transform.SetParent(app.AppInfo.Container, false);

                // Add Image component
                Image image = imageObject.AddComponent<Image>();

                // Load image sprite
                Sprite sprite = LoadImageFromPath(imagePath);
                if (sprite != null)
                {
                    image.sprite = sprite;
                }
                else
                {
                    _logger.Warning($"Failed to load image from: {imagePath}");
                    // Create a placeholder colored image
                    image.color = new Color(0.8f, 0.8f, 0.8f);
                }

                // Position and size
                RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(x, y);
                rectTransform.sizeDelta = new Vector2(width, height);

                // Create element info
                string elementId = "image_" + Guid.NewGuid().ToString().Substring(0, 8);
                var elementInfo = new UIElementInfo
                {
                    Id = elementId,
                    GameObject = imageObject,
                    Type = UIElementType.Image
                };

                // Add to app's elements
                app.AppInfo.Elements[elementId] = elementInfo;

                return new UIElementProxy(elementInfo);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error adding image to app: {ex.Message}");
                return null;
            }
        }
    }
}