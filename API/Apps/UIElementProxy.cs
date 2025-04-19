using UnityEngine;
using UnityEngine.UI;
using MoonSharp.Interpreter;

namespace ScheduleLua.API.Apps
{
    /// <summary>
    /// Types of UI elements that can be added to a phone app
    /// </summary>
    public enum UIElementType
    {
        Text,
        Button,
        Image,
        Custom
    }

    /// <summary>
    /// Stores information about a UI element
    /// </summary>
    public class UIElementInfo
    {
        public string Id { get; set; }
        public GameObject GameObject { get; set; }
        public UIElementType Type { get; set; }
        public DynValue Callback { get; set; }
    }

    /// <summary>
    /// Proxy class that exposes UI element functionality to Lua
    /// </summary>
    [MoonSharpUserData]
    public class UIElementProxy
    {
        internal UIElementInfo ElementInfo { get; private set; }

        public UIElementProxy(UIElementInfo elementInfo)
        {
            ElementInfo = elementInfo;
        }

        /// <summary>
        /// Gets the ID of the UI element
        /// </summary>
        public string GetId()
        {
            return ElementInfo?.Id;
        }

        /// <summary>
        /// Gets the type of the UI element
        /// </summary>
        public string GetType()
        {
            if (ElementInfo == null)
                return "Unknown";

            return ElementInfo.Type.ToString();
        }

        /// <summary>
        /// Sets the position of the UI element
        /// </summary>
        public void SetPosition(float x, float y)
        {
            if (ElementInfo?.GameObject != null)
            {
                var rectTransform = ElementInfo.GameObject.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2(x, y);
                }
            }
        }

        /// <summary>
        /// Gets the X position of the UI element
        /// </summary>
        public float GetX()
        {
            if (ElementInfo?.GameObject != null)
            {
                var rectTransform = ElementInfo.GameObject.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    return rectTransform.anchoredPosition.x;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the Y position of the UI element
        /// </summary>
        public float GetY()
        {
            if (ElementInfo?.GameObject != null)
            {
                var rectTransform = ElementInfo.GameObject.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    return rectTransform.anchoredPosition.y;
                }
            }
            return 0;
        }

        /// <summary>
        /// Sets the size of the UI element
        /// </summary>
        public void SetSize(float width, float height)
        {
            if (ElementInfo?.GameObject != null)
            {
                var rectTransform = ElementInfo.GameObject.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = new Vector2(width, height);
                }
            }
        }

        /// <summary>
        /// Gets the width of the UI element
        /// </summary>
        public float GetWidth()
        {
            if (ElementInfo?.GameObject != null)
            {
                var rectTransform = ElementInfo.GameObject.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    return rectTransform.sizeDelta.x;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the height of the UI element
        /// </summary>
        public float GetHeight()
        {
            if (ElementInfo?.GameObject != null)
            {
                var rectTransform = ElementInfo.GameObject.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    return rectTransform.sizeDelta.y;
                }
            }
            return 0;
        }

        /// <summary>
        /// Sets the text of a text element
        /// </summary>
        public void SetText(string text)
        {
            if (ElementInfo?.GameObject != null && ElementInfo.Type == UIElementType.Text)
            {
                var textComponent = ElementInfo.GameObject.GetComponent<Text>();
                if (textComponent != null)
                {
                    textComponent.text = text;
                }
            }
            else if (ElementInfo?.GameObject != null && ElementInfo.Type == UIElementType.Button)
            {
                var textComponent = ElementInfo.GameObject.transform.Find("Text")?.GetComponent<Text>();
                if (textComponent != null)
                {
                    textComponent.text = text;
                }
            }
        }

        /// <summary>
        /// Gets the text of a text element
        /// </summary>
        public string GetText()
        {
            if (ElementInfo?.GameObject != null && ElementInfo.Type == UIElementType.Text)
            {
                var textComponent = ElementInfo.GameObject.GetComponent<Text>();
                if (textComponent != null)
                {
                    return textComponent.text;
                }
            }
            else if (ElementInfo?.GameObject != null && ElementInfo.Type == UIElementType.Button)
            {
                var textComponent = ElementInfo.GameObject.transform.Find("Text")?.GetComponent<Text>();
                if (textComponent != null)
                {
                    return textComponent.text;
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Sets the color of a UI element
        /// </summary>
        public void SetColor(float r, float g, float b, float a = 1.0f)
        {
            if (ElementInfo?.GameObject != null)
            {
                if (ElementInfo.Type == UIElementType.Text)
                {
                    var textComponent = ElementInfo.GameObject.GetComponent<Text>();
                    if (textComponent != null)
                    {
                        textComponent.color = new Color(r, g, b, a);
                    }
                }
                else if (ElementInfo.Type == UIElementType.Image)
                {
                    var imageComponent = ElementInfo.GameObject.GetComponent<Image>();
                    if (imageComponent != null)
                    {
                        imageComponent.color = new Color(r, g, b, a);
                    }
                }
                else if (ElementInfo.Type == UIElementType.Button)
                {
                    var imageComponent = ElementInfo.GameObject.GetComponent<Image>();
                    if (imageComponent != null)
                    {
                        imageComponent.color = new Color(r, g, b, a);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the image of an image element
        /// </summary>
        public void SetImage(string imagePath)
        {
            if (ElementInfo?.GameObject != null && ElementInfo.Type == UIElementType.Image)
            {
                var imageComponent = ElementInfo.GameObject.GetComponent<Image>();
                if (imageComponent != null)
                {
                    var sprite = AppsAPI.LoadImageFromPath(imagePath);
                    if (sprite != null)
                    {
                        imageComponent.sprite = sprite;
                    }
                }
            }
        }

        /// <summary>
        /// Sets the font size of a text element
        /// </summary>
        public void SetFontSize(int fontSize)
        {
            if (ElementInfo?.GameObject != null && ElementInfo.Type == UIElementType.Text)
            {
                var textComponent = ElementInfo.GameObject.GetComponent<Text>();
                if (textComponent != null)
                {
                    textComponent.fontSize = fontSize;
                }
            }
            else if (ElementInfo?.GameObject != null && ElementInfo.Type == UIElementType.Button)
            {
                var textComponent = ElementInfo.GameObject.transform.Find("Text")?.GetComponent<Text>();
                if (textComponent != null)
                {
                    textComponent.fontSize = fontSize;
                }
            }
        }

        /// <summary>
        /// Destroys the UI element
        /// </summary>
        public void Destroy()
        {
            if (ElementInfo?.GameObject != null)
            {
                GameObject.Destroy(ElementInfo.GameObject);
            }
        }
    }
}