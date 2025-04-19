using UnityEngine;
using UnityEngine.UI;
using MoonSharp.Interpreter;
using System.Collections.Generic;
using System;
using ScheduleOne.UI.Phone;
using ScheduleOne.DevUtilities;
using MelonLoader;

namespace ScheduleLua.API.Apps
{
    /// <summary>
    /// Proxy class that exposes the native App functionality to Lua
    /// </summary>
    [MoonSharpUserData]
    public class AppBaseProxy
    {
        private static MelonLogger.Instance _logger => ScheduleLua.Core.Instance.LoggerInstance;

        public GameObject AppObject { get; private set; }
        public RectTransform AppContainer { get; private set; }
        public string AppName { get; private set; }
        public string IconLabel { get; private set; }
        public Sprite AppIcon { get; private set; }
        public string Orientation { get; private set; }
        public bool AvailableInTutorial { get; private set; }
        public PhoneAppInfo AppInfo { get; private set; }

        // Event handling
        private DynValue _onShowCallback;
        private DynValue _onHideCallback;
        private DynValue _onUpdateCallback;
        private DynValue _onDestroyCallback;

        // Track game objects created for this app
        private List<GameObject> _managedObjects = new List<GameObject>();

        // Last update time for throttling
        private float _lastUpdateTime = 0f;

        public AppBaseProxy(PhoneAppInfo appInfo)
        {
            AppInfo = appInfo;
            AppObject = appInfo.AppObject;
            AppContainer = appInfo.Container;
            AppName = appInfo.AppName;
            IconLabel = appInfo.DisplayName;
            Orientation = "Vertical"; // Default orientation
            AvailableInTutorial = true; // Default availability

            // Register with Unity's update cycle instead of using Core.OnUpdateCallback
            GameObject updateObject = new GameObject("AppUpdateHandler_" + appInfo.AppName);
            UnityEngine.Object.DontDestroyOnLoad(updateObject);
            var updateHandler = updateObject.AddComponent<AppUpdateHandler>();
            updateHandler.Initialize(this);
            AddManagedObject(updateObject);
        }

        /// <summary>
        /// Called when the app needs to update
        /// </summary>
        public void Update()
        {
            if (AppInfo?.Active != true)
                return;

            // Throttle updates to 10 times per second
            if (Time.time - _lastUpdateTime < 0.1f)
                return;

            _lastUpdateTime = Time.time;

            try
            {
                if (_onUpdateCallback != null && _onUpdateCallback.Type == DataType.Function)
                {
                    ScheduleLua.Core.Instance._luaEngine.Call(_onUpdateCallback);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in app update callback: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when the app is shown
        /// </summary>
        public void OnShow()
        {
            try
            {
                if (_onShowCallback != null && _onShowCallback.Type == DataType.Function)
                {
                    ScheduleLua.Core.Instance._luaEngine.Call(_onShowCallback);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in app show callback: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when the app is hidden
        /// </summary>
        public void OnHide()
        {
            try
            {
                if (_onHideCallback != null && _onHideCallback.Type == DataType.Function)
                {
                    ScheduleLua.Core.Instance._luaEngine.Call(_onHideCallback);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in app hide callback: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when the app is being destroyed
        /// </summary>
        public void OnDestroy()
        {
            try
            {
                if (_onDestroyCallback != null && _onDestroyCallback.Type == DataType.Function)
                {
                    ScheduleLua.Core.Instance._luaEngine.Call(_onDestroyCallback);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in app destroy callback: {ex.Message}");
            }

            // Clean up managed objects
            foreach (var obj in _managedObjects)
            {
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj);
                }
            }

            _managedObjects.Clear();
        }

        /// <summary>
        /// Helper MonoBehaviour to handle Unity update cycle
        /// </summary>
        private class AppUpdateHandler : MonoBehaviour
        {
            private AppBaseProxy _app;

            public void Initialize(AppBaseProxy app)
            {
                _app = app;
            }

            void Update()
            {
                if (_app != null)
                {
                    _app.Update();
                }
            }
        }

        /// <summary>
        /// Sets the app icon sprite
        /// </summary>
        public void SetIcon(string iconPath)
        {
            Sprite iconSprite = AppsAPI.LoadImageFromPath(iconPath);
            if (iconSprite != null)
            {
                AppIcon = iconSprite;

                // Find and update icon in home screen
                var iconGrid = GameObject.Find("Player_Local/CameraContainer/Camera/OverlayCamera/GameplayMenu/Phone/phone/HomeScreen/AppIcons");
                if (iconGrid != null)
                {
                    foreach (Transform child in iconGrid.transform)
                    {
                        if (child.name == AppName + "Icon")
                        {
                            var imageComponent = child.Find("Mask/Image")?.GetComponent<Image>();
                            if (imageComponent != null)
                            {
                                imageComponent.sprite = iconSprite;
                            }
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the app name and label
        /// </summary>
        public void SetAppName(string name, string label)
        {
            AppName = name;
            SetDisplayName(label);
        }

        /// <summary>
        /// Sets the display name (label) of the app
        /// </summary>
        public void SetDisplayName(string displayName)
        {
            IconLabel = displayName;
            AppInfo.DisplayName = displayName;

            // Update label on icon if it exists
            var iconGrid = GameObject.Find("Player_Local/CameraContainer/Camera/OverlayCamera/GameplayMenu/Phone/phone/HomeScreen/AppIcons");
            if (iconGrid != null)
            {
                foreach (Transform child in iconGrid.transform)
                {
                    if (child.name == AppName + "Icon")
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

        /// <summary>
        /// Sets the orientation of the app
        /// </summary>
        public void SetOrientation(string orientation)
        {
            if (orientation != "Vertical" && orientation != "Horizontal")
            {
                _logger.Warning("Invalid orientation. Use 'Vertical' or 'Horizontal'");
                return;
            }

            Orientation = orientation;

            // Update layout component based on orientation
            var layoutGroup = AppContainer.GetComponent<LayoutGroup>();
            if (layoutGroup != null)
            {
                UnityEngine.Object.Destroy(layoutGroup);
            }

            if (orientation == "Vertical")
            {
                var verticalLayout = AppContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                verticalLayout.childAlignment = TextAnchor.UpperCenter;
                verticalLayout.padding = new RectOffset(10, 10, 10, 10);
                verticalLayout.spacing = 10;
                verticalLayout.childControlWidth = true;
                verticalLayout.childControlHeight = false;
                verticalLayout.childForceExpandWidth = true;
                verticalLayout.childForceExpandHeight = false;
            }
            else
            {
                var horizontalLayout = AppContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
                horizontalLayout.childAlignment = TextAnchor.UpperCenter;
                horizontalLayout.padding = new RectOffset(10, 10, 10, 10);
                horizontalLayout.spacing = 10;
                horizontalLayout.childControlWidth = false;
                horizontalLayout.childControlHeight = true;
                horizontalLayout.childForceExpandWidth = false;
                horizontalLayout.childForceExpandHeight = true;
            }
        }

        /// <summary>
        /// Sets whether the app is available in tutorial
        /// </summary>
        public void SetAvailableInTutorial(bool available)
        {
            AvailableInTutorial = available;
        }

        /// <summary>
        /// Sets the callback function for when the app is shown
        /// </summary>
        public void SetOnShowCallback(DynValue callback)
        {
            if (callback == null || callback.Type != DataType.Function)
            {
                _logger.Error("OnShow callback must be a valid function");
                return;
            }

            _onShowCallback = callback;
        }

        /// <summary>
        /// Sets the callback function for when the app is hidden
        /// </summary>
        public void SetOnHideCallback(DynValue callback)
        {
            if (callback == null || callback.Type != DataType.Function)
            {
                _logger.Error("OnHide callback must be a valid function");
                return;
            }

            _onHideCallback = callback;
        }

        /// <summary>
        /// Sets the callback function for when the app should update
        /// </summary>
        public void SetOnUpdateCallback(DynValue callback)
        {
            if (callback == null || callback.Type != DataType.Function)
            {
                _logger.Error("OnUpdate callback must be a valid function");
                return;
            }

            _onUpdateCallback = callback;
        }

        /// <summary>
        /// Sets the callback function for when the app is being destroyed
        /// </summary>
        public void SetOnDestroyCallback(DynValue callback)
        {
            if (callback == null || callback.Type != DataType.Function)
            {
                _logger.Error("OnDestroy callback must be a valid function");
                return;
            }

            _onDestroyCallback = callback;
        }

        /// <summary>
        /// Adds a managed GameObject to be cleaned up when the app is destroyed
        /// </summary>
        public void AddManagedObject(GameObject obj)
        {
            if (obj != null)
            {
                _managedObjects.Add(obj);
            }
        }

        /// <summary>
        /// Shows the app
        /// </summary>
        public void Show()
        {
            if (AppInfo?.AppObject != null)
            {
                AppInfo.AppObject.SetActive(true);
                AppInfo.Active = true;
                OnShow();
            }
        }

        /// <summary>
        /// Hides the app
        /// </summary>
        public void Hide()
        {
            if (AppInfo?.AppObject != null)
            {
                AppInfo.AppObject.SetActive(false);
                AppInfo.Active = false;
                OnHide();
            }
        }

        /// <summary>
        /// Creates a scrollable view within the app
        /// </summary>
        public ScrollViewProxy CreateScrollView(float x, float y, float width, float height)
        {
            try
            {
                if (AppContainer == null)
                {
                    _logger.Error("CreateScrollView: App container not found");
                    return null;
                }

                // Create scroll view object
                GameObject scrollViewObject = new GameObject("ScrollView_" + Guid.NewGuid().ToString().Substring(0, 8));
                scrollViewObject.transform.SetParent(AppContainer, false);

                // Add scroll rect component
                ScrollRect scrollRect = scrollViewObject.AddComponent<ScrollRect>();
                RectTransform rectTransform = scrollViewObject.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(x, y);
                rectTransform.sizeDelta = new Vector2(width, height);

                // Create viewport
                GameObject viewportObject = new GameObject("Viewport");
                viewportObject.transform.SetParent(scrollViewObject.transform, false);
                RectTransform viewportRectTransform = viewportObject.AddComponent<RectTransform>();
                viewportRectTransform.anchorMin = new Vector2(0, 0);
                viewportRectTransform.anchorMax = new Vector2(1, 1);
                viewportRectTransform.offsetMin = Vector2.zero;
                viewportRectTransform.offsetMax = Vector2.zero;
                Image viewportImage = viewportObject.AddComponent<Image>();
                viewportImage.color = new Color(1, 1, 1, 0.05f);
                Mask viewportMask = viewportObject.AddComponent<Mask>();
                viewportMask.showMaskGraphic = false;

                // Create content container
                GameObject contentObject = new GameObject("Content");
                contentObject.transform.SetParent(viewportObject.transform, false);
                RectTransform contentRectTransform = contentObject.AddComponent<RectTransform>();
                contentRectTransform.anchorMin = new Vector2(0, 1);
                contentRectTransform.anchorMax = new Vector2(1, 1);
                contentRectTransform.pivot = new Vector2(0.5f, 1);
                contentRectTransform.offsetMin = new Vector2(0, 0);
                contentRectTransform.offsetMax = new Vector2(0, 0);

                // Setup vertical layout group on content
                VerticalLayoutGroup contentLayout = contentObject.AddComponent<VerticalLayoutGroup>();
                contentLayout.childAlignment = TextAnchor.UpperCenter;
                contentLayout.padding = new RectOffset(10, 10, 10, 10);
                contentLayout.spacing = 10;
                contentLayout.childControlWidth = true;
                contentLayout.childControlHeight = false;
                contentLayout.childForceExpandWidth = true;
                contentLayout.childForceExpandHeight = false;
                ContentSizeFitter contentSizeFitter = contentObject.AddComponent<ContentSizeFitter>();
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // Configure scroll rect
                scrollRect.viewport = viewportRectTransform;
                scrollRect.content = contentRectTransform;
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Elastic;
                scrollRect.elasticity = 0.1f;
                scrollRect.inertia = true;
                scrollRect.decelerationRate = 0.135f;
                scrollRect.scrollSensitivity = 1;

                // Add to managed objects
                AddManagedObject(scrollViewObject);

                return new ScrollViewProxy(scrollViewObject, contentObject, contentRectTransform);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error creating scroll view: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a panel with background
        /// </summary>
        public PanelProxy CreatePanel(float x, float y, float width, float height, bool withBackground = true)
        {
            try
            {
                if (AppContainer == null)
                {
                    _logger.Error("CreatePanel: App container not found");
                    return null;
                }

                // Create panel object
                GameObject panelObject = new GameObject("Panel_" + Guid.NewGuid().ToString().Substring(0, 8));
                panelObject.transform.SetParent(AppContainer, false);

                // Add rect transform
                RectTransform rectTransform = panelObject.AddComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(x, y);
                rectTransform.sizeDelta = new Vector2(width, height);

                if (withBackground)
                {
                    // Add background image
                    Image bgImage = panelObject.AddComponent<Image>();
                    bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);
                }

                // Add layout group
                VerticalLayoutGroup layoutGroup = panelObject.AddComponent<VerticalLayoutGroup>();
                layoutGroup.childAlignment = TextAnchor.UpperCenter;
                layoutGroup.padding = new RectOffset(10, 10, 10, 10);
                layoutGroup.spacing = 10;
                layoutGroup.childControlWidth = true;
                layoutGroup.childControlHeight = false;
                layoutGroup.childForceExpandWidth = true;
                layoutGroup.childForceExpandHeight = false;

                // Add to managed objects
                AddManagedObject(panelObject);

                return new PanelProxy(panelObject, rectTransform);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error creating panel: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a header with title text
        /// </summary>
        public UIElementProxy CreateHeader(string text, float fontSize = 24)
        {
            try
            {
                if (AppContainer == null)
                {
                    _logger.Error("CreateHeader: App container not found");
                    return null;
                }

                // Create header object
                GameObject headerObject = new GameObject("Header_" + Guid.NewGuid().ToString().Substring(0, 8));
                headerObject.transform.SetParent(AppContainer, false);

                // Add Text component
                Text textComponent = headerObject.AddComponent<Text>();
                textComponent.text = text;
                textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                textComponent.color = Color.white;
                textComponent.fontSize = (int)fontSize;
                textComponent.alignment = TextAnchor.MiddleCenter;
                textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;

                // Position and size
                RectTransform rectTransform = headerObject.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(AppContainer.rect.width - 40, 40);

                // Add to managed objects
                AddManagedObject(headerObject);

                // Create element info
                string elementId = "header_" + Guid.NewGuid().ToString().Substring(0, 8);
                var elementInfo = new UIElementInfo
                {
                    Id = elementId,
                    GameObject = headerObject,
                    Type = UIElementType.Text
                };

                return new UIElementProxy(elementInfo);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error creating header: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Destroys the app
        /// </summary>
        public void Destroy()
        {
            OnDestroy();
            AppsAPI.RemovePhoneAppByName(AppName);
        }
    }
}