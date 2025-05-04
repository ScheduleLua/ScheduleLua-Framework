using ScheduleLua.API.Core;
using UnityEngine;

namespace ScheduleLua.API.UI.Phone
{
    /// <summary>
    /// Provides integration with the in-game phone
    /// </summary>
    public class PhoneIntegration
    {
        // Cache for performance
        private GameObject _phoneInstance;

        /// <summary>
        /// Checks if the phone is currently open
        /// </summary>
        public bool IsPhoneOpen()
        {
            try
            {
                var phoneManager = FindPhoneManager();
                if (phoneManager != null)
                {
                    // This is a placeholder - implementation would depend on game's actual phone system
                    return phoneManager.activeInHierarchy;
                }
                return false;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error checking if phone is open: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Opens the in-game phone
        /// </summary>
        public void OpenPhone()
        {
            try
            {
                var phoneManager = FindPhoneManager();
                if (phoneManager != null && !phoneManager.activeInHierarchy)
                {
                    // This is a placeholder - implementation would depend on game's actual phone system
                    phoneManager.SetActive(true);
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error opening phone: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Closes the in-game phone
        /// </summary>
        public void ClosePhone()
        {
            try
            {
                var phoneManager = FindPhoneManager();
                if (phoneManager != null && phoneManager.activeInHierarchy)
                {
                    // This is a placeholder - implementation would depend on game's actual phone system
                    phoneManager.SetActive(false);
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error closing phone: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Toggles the phone flashlight on/off
        /// </summary>
        public void TogglePhoneFlashlight()
        {
            try
            {
                var phoneManager = FindPhoneManager();
                if (phoneManager != null)
                {
                    // Find flashlight component and toggle
                    var flashlight = phoneManager.transform.Find("Flashlight");
                    if (flashlight != null)
                    {
                        flashlight.gameObject.SetActive(!flashlight.gameObject.activeSelf);
                    }
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error toggling flashlight: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks if the phone flashlight is on
        /// </summary>
        public bool IsPhoneFlashlightOn()
        {
            try
            {
                var phoneManager = FindPhoneManager();
                if (phoneManager != null)
                {
                    // Find flashlight component
                    var flashlight = phoneManager.transform.Find("Flashlight");
                    if (flashlight != null)
                    {
                        return flashlight.gameObject.activeSelf;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error checking flashlight status: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Finds the phone manager game object
        /// </summary>
        private GameObject FindPhoneManager()
        {
            if (_phoneInstance != null)
                return _phoneInstance;

            // Try to find the phone manager in the scene
            _phoneInstance = GameObject.Find("PhoneManager");
            return _phoneInstance;
        }
    }
}