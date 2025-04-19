using UnityEngine;
using UnityEngine.UI;
using MoonSharp.Interpreter;
using System;
using MelonLoader;

namespace ScheduleLua.API.Apps
{
    /// <summary>
    /// Proxy class for managing scrollable views in apps
    /// </summary>
    [MoonSharpUserData]
    public class ScrollViewProxy
    {
        private static MelonLogger.Instance _logger => ScheduleLua.Core.Instance.LoggerInstance;
        
        public GameObject ScrollViewObject { get; private set; }
        public GameObject ContentObject { get; private set; }
        public RectTransform ContentRectTransform { get; private set; }
        
        public ScrollViewProxy(GameObject scrollViewObject, GameObject contentObject, RectTransform contentRectTransform)
        {
            ScrollViewObject = scrollViewObject;
            ContentObject = contentObject;
            ContentRectTransform = contentRectTransform;
        }
        
        /// <summary>
        /// Adds a text element to the scroll view
        /// </summary>
        public UIElementProxy AddText(string text, float height = 30, float fontSize = 16)
        {
            try
            {
                if (ContentObject == null)
                {
                    _logger.Error("AddText: Content object not found");
                    return null;
                }
                
                // Create text object
                GameObject textObject = new GameObject("Text_" + Guid.NewGuid().ToString().Substring(0, 8));
                textObject.transform.SetParent(ContentObject.transform, false);
                
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
                _logger.Error($"Error adding text to scroll view: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Adds a button element to the scroll view
        /// </summary>
        public UIElementProxy AddButton(string text, float height = 40, DynValue callback = null)
        {
            try
            {
                if (ContentObject == null)
                {
                    _logger.Error("AddButton: Content object not found");
                    return null;
                }
                
                // Create button object
                GameObject buttonObject = new GameObject("Button_" + Guid.NewGuid().ToString().Substring(0, 8));
                buttonObject.transform.SetParent(ContentObject.transform, false);
                
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
                _logger.Error($"Error adding button to scroll view: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Adds a divider line to the scroll view
        /// </summary>
        public UIElementProxy AddDivider(float height = 2)
        {
            try
            {
                if (ContentObject == null)
                {
                    _logger.Error("AddDivider: Content object not found");
                    return null;
                }
                
                // Create divider object
                GameObject dividerObject = new GameObject("Divider_" + Guid.NewGuid().ToString().Substring(0, 8));
                dividerObject.transform.SetParent(ContentObject.transform, false);
                
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
                _logger.Error($"Error adding divider to scroll view: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Adds a list item with title and value to the scroll view
        /// </summary>
        public UIElementProxy AddListItem(string title, string value, float height = 50)
        {
            try
            {
                if (ContentObject == null)
                {
                    _logger.Error("AddListItem: Content object not found");
                    return null;
                }
                
                // Create list item object
                GameObject listItemObject = new GameObject("ListItem_" + Guid.NewGuid().ToString().Substring(0, 8));
                listItemObject.transform.SetParent(ContentObject.transform, false);
                
                // Add background image
                Image bgImage = listItemObject.AddComponent<Image>();
                bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                
                // Set layout properties
                LayoutElement layoutElement = listItemObject.AddComponent<LayoutElement>();
                layoutElement.minHeight = height;
                layoutElement.preferredHeight = height;
                
                // Add horizontal layout group
                HorizontalLayoutGroup horizontalLayout = listItemObject.AddComponent<HorizontalLayoutGroup>();
                horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
                horizontalLayout.padding = new RectOffset(10, 10, 5, 5);
                horizontalLayout.spacing = 10;
                horizontalLayout.childControlWidth = false;
                horizontalLayout.childForceExpandWidth = false;
                
                // Add title text
                GameObject titleObject = new GameObject("Title");
                titleObject.transform.SetParent(listItemObject.transform, false);
                Text titleText = titleObject.AddComponent<Text>();
                titleText.text = title;
                titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                titleText.color = Color.white;
                titleText.fontSize = 16;
                titleText.alignment = TextAnchor.MiddleLeft;
                
                // Set title layout properties
                LayoutElement titleLayout = titleObject.AddComponent<LayoutElement>();
                titleLayout.minWidth = 120;
                titleLayout.preferredWidth = 120;
                
                // Add value text
                GameObject valueObject = new GameObject("Value");
                valueObject.transform.SetParent(listItemObject.transform, false);
                Text valueText = valueObject.AddComponent<Text>();
                valueText.text = value;
                valueText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                valueText.color = Color.white;
                valueText.fontSize = 16;
                valueText.alignment = TextAnchor.MiddleRight;
                
                // Set value layout properties
                LayoutElement valueLayout = valueObject.AddComponent<LayoutElement>();
                valueLayout.minWidth = 120;
                valueLayout.preferredWidth = 120;
                valueLayout.flexibleWidth = 1;
                
                // Create element info
                string elementId = "listitem_" + Guid.NewGuid().ToString().Substring(0, 8);
                var elementInfo = new UIElementInfo
                {
                    Id = elementId,
                    GameObject = listItemObject,
                    Type = UIElementType.Custom
                };
                
                // Add custom methods for updating values
                var proxy = new UIElementProxy(elementInfo);
                
                return proxy;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error adding list item to scroll view: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Clears all content from the scroll view
        /// </summary>
        public void Clear()
        {
            if (ContentObject == null)
                return;
                
            foreach (Transform child in ContentObject.transform)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }
        
        /// <summary>
        /// Scrolls to the top of the content
        /// </summary>
        public void ScrollToTop()
        {
            if (ScrollViewObject == null)
                return;
                
            var scrollRect = ScrollViewObject.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.normalizedPosition = new Vector2(0, 1);
            }
        }
        
        /// <summary>
        /// Scrolls to the bottom of the content
        /// </summary>
        public void ScrollToBottom()
        {
            if (ScrollViewObject == null)
                return;
                
            var scrollRect = ScrollViewObject.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.normalizedPosition = new Vector2(0, 0);
            }
        }
    }
} 