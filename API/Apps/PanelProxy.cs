using UnityEngine;
using UnityEngine.UI;
using MoonSharp.Interpreter;
using System;
using MelonLoader;

namespace ScheduleLua.API.Apps
{
    /// <summary>
    /// Proxy class for managing panel containers in apps
    /// </summary>
    [MoonSharpUserData]
    public class PanelProxy
    {
        private static MelonLogger.Instance _logger => ScheduleLua.Core.Instance.LoggerInstance;
        
        public GameObject PanelObject { get; private set; }
        public RectTransform RectTransform { get; private set; }
        
        public PanelProxy(GameObject panelObject, RectTransform rectTransform)
        {
            PanelObject = panelObject;
            RectTransform = rectTransform;
        }
        
        /// <summary>
        /// Adds a text element to the panel
        /// </summary>
        public UIElementProxy AddText(string text, float height = 30, float fontSize = 16)
        {
            try
            {
                if (PanelObject == null)
                {
                    _logger.Error("AddText: Panel object not found");
                    return null;
                }
                
                // Create text object
                GameObject textObject = new GameObject("Text_" + Guid.NewGuid().ToString().Substring(0, 8));
                textObject.transform.SetParent(PanelObject.transform, false);
                
                // Add Text component
                Text textComponent = textObject.AddComponent<Text>();
                textComponent.text = text;
                textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                textComponent.color = Color.white;
                textComponent.fontSize = (int)fontSize;
                textComponent.alignment = TextAnchor.MiddleLeft;
                textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
                
                // Set layout properties
                LayoutElement layoutElement = textObject.AddComponent<LayoutElement>();
                layoutElement.minHeight = height;
                layoutElement.preferredHeight = height;
                
                // Create element info
                string elementId = "text_" + Guid.NewGuid().ToString().Substring(0, 8);
                var elementInfo = new UIElementInfo
                {
                    Id = elementId,
                    GameObject = textObject,
                    Type = UIElementType.Text
                };
                
                return new UIElementProxy(elementInfo);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error adding text to panel: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Adds a button element to the panel
        /// </summary>
        public UIElementProxy AddButton(string text, float height = 40, DynValue callback = null)
        {
            try
            {
                if (PanelObject == null)
                {
                    _logger.Error("AddButton: Panel object not found");
                    return null;
                }
                
                // Create button object
                GameObject buttonObject = new GameObject("Button_" + Guid.NewGuid().ToString().Substring(0, 8));
                buttonObject.transform.SetParent(PanelObject.transform, false);
                
                // Add Image component (for button background)
                Image image = buttonObject.AddComponent<Image>();
                image.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                
                // Add Button component
                Button button = buttonObject.AddComponent<Button>();
                ColorBlock colors = button.colors;
                colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
                colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
                button.colors = colors;
                
                // Add Text child
                GameObject textObject = new GameObject("Text");
                textObject.transform.SetParent(buttonObject.transform, false);
                Text textComponent = textObject.AddComponent<Text>();
                textComponent.text = text;
                textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                textComponent.color = Color.white;
                textComponent.fontSize = 16;
                textComponent.alignment = TextAnchor.MiddleCenter;
                
                // Position and size for text
                RectTransform textRectTransform = textObject.GetComponent<RectTransform>();
                textRectTransform.anchorMin = Vector2.zero;
                textRectTransform.anchorMax = Vector2.one;
                textRectTransform.offsetMin = Vector2.zero;
                textRectTransform.offsetMax = Vector2.zero;
                
                // Set layout properties
                LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
                layoutElement.minHeight = height;
                layoutElement.preferredHeight = height;
                
                // Create element info
                string elementId = "button_" + Guid.NewGuid().ToString().Substring(0, 8);
                var elementInfo = new UIElementInfo
                {
                    Id = elementId,
                    GameObject = buttonObject,
                    Type = UIElementType.Button,
                    Callback = callback
                };
                
                // Add click handler if callback provided
                if (callback != null && callback.Type == DataType.Function)
                {
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
                }
                
                return new UIElementProxy(elementInfo);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error adding button to panel: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Adds an image element to the panel
        /// </summary>
        public UIElementProxy AddImage(string imagePath, float height = 100)
        {
            try
            {
                if (PanelObject == null)
                {
                    _logger.Error("AddImage: Panel object not found");
                    return null;
                }
                
                // Create image object
                GameObject imageObject = new GameObject("Image_" + Guid.NewGuid().ToString().Substring(0, 8));
                imageObject.transform.SetParent(PanelObject.transform, false);
                
                // Add Image component
                Image image = imageObject.AddComponent<Image>();
                
                // Load image sprite
                Sprite sprite = AppsAPI.LoadImageFromPath(imagePath);
                if (sprite != null)
                {
                    image.sprite = sprite;
                    image.preserveAspect = true;
                }
                else
                {
                    // Create a placeholder colored image
                    image.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                }
                
                // Set layout properties
                LayoutElement layoutElement = imageObject.AddComponent<LayoutElement>();
                layoutElement.minHeight = height;
                layoutElement.preferredHeight = height;
                
                // Create element info
                string elementId = "image_" + Guid.NewGuid().ToString().Substring(0, 8);
                var elementInfo = new UIElementInfo
                {
                    Id = elementId,
                    GameObject = imageObject,
                    Type = UIElementType.Image
                };
                
                return new UIElementProxy(elementInfo);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error adding image to panel: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Adds a divider line to the panel
        /// </summary>
        public UIElementProxy AddDivider(float height = 2)
        {
            try
            {
                if (PanelObject == null)
                {
                    _logger.Error("AddDivider: Panel object not found");
                    return null;
                }
                
                // Create divider object
                GameObject dividerObject = new GameObject("Divider_" + Guid.NewGuid().ToString().Substring(0, 8));
                dividerObject.transform.SetParent(PanelObject.transform, false);
                
                // Add Image component
                Image image = dividerObject.AddComponent<Image>();
                image.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                
                // Set layout properties
                LayoutElement layoutElement = dividerObject.AddComponent<LayoutElement>();
                layoutElement.minHeight = height;
                layoutElement.preferredHeight = height;
                
                // Create element info
                string elementId = "divider_" + Guid.NewGuid().ToString().Substring(0, 8);
                var elementInfo = new UIElementInfo
                {
                    Id = elementId,
                    GameObject = dividerObject,
                    Type = UIElementType.Image
                };
                
                return new UIElementProxy(elementInfo);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error adding divider to panel: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Adds a spacer (empty space) to the panel
        /// </summary>
        public void AddSpacer(float height = 10)
        {
            try
            {
                if (PanelObject == null)
                {
                    _logger.Error("AddSpacer: Panel object not found");
                    return;
                }
                
                // Create spacer object
                GameObject spacerObject = new GameObject("Spacer_" + Guid.NewGuid().ToString().Substring(0, 8));
                spacerObject.transform.SetParent(PanelObject.transform, false);
                
                // Add rect transform
                spacerObject.AddComponent<RectTransform>();
                
                // Set layout properties
                LayoutElement layoutElement = spacerObject.AddComponent<LayoutElement>();
                layoutElement.minHeight = height;
                layoutElement.preferredHeight = height;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error adding spacer to panel: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Adds a row with multiple columns (for more complex layouts)
        /// </summary>
        public GameObject AddRow(float height = 40)
        {
            try
            {
                if (PanelObject == null)
                {
                    _logger.Error("AddRow: Panel object not found");
                    return null;
                }
                
                // Create row object
                GameObject rowObject = new GameObject("Row_" + Guid.NewGuid().ToString().Substring(0, 8));
                rowObject.transform.SetParent(PanelObject.transform, false);
                
                // Add horizontal layout group
                HorizontalLayoutGroup horizontalLayout = rowObject.AddComponent<HorizontalLayoutGroup>();
                horizontalLayout.childAlignment = TextAnchor.MiddleCenter;
                horizontalLayout.padding = new RectOffset(5, 5, 5, 5);
                horizontalLayout.spacing = 10;
                horizontalLayout.childControlWidth = false;
                horizontalLayout.childForceExpandWidth = false;
                
                // Set layout properties
                LayoutElement layoutElement = rowObject.AddComponent<LayoutElement>();
                layoutElement.minHeight = height;
                layoutElement.preferredHeight = height;
                
                return rowObject;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error adding row to panel: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Sets the background color of the panel
        /// </summary>
        public void SetBackgroundColor(float r, float g, float b, float a = 0.7f)
        {
            if (PanelObject == null)
                return;
                
            var image = PanelObject.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(r, g, b, a);
            }
        }
        
        /// <summary>
        /// Clears all content from the panel
        /// </summary>
        public void Clear()
        {
            if (PanelObject == null)
                return;
                
            foreach (Transform child in PanelObject.transform)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }
    }
} 