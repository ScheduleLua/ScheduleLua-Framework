using UnityEngine;

namespace ScheduleLua.Core.Framework.Mods.ManagerUI
{
    /// <summary>
    /// Handles keyboard shortcuts for the mod manager UI
    /// </summary>
    public class ModManagerKeyboardShortcut
    {
        private readonly ModManagerUIController _uiController;
        private readonly KeyCode _toggleKey;
        private readonly KeyCode _modifierKey;

        /// <summary>
        /// Creates a new keyboard shortcut handler
        /// </summary>
        /// <param name="uiController">The UI controller to toggle</param>
        /// <param name="toggleKey">The key to toggle the UI (default: F7)</param>
        /// <param name="modifierKey">Optional modifier key (default: None)</param>
        public ModManagerKeyboardShortcut(
            ModManagerUIController uiController,
            KeyCode toggleKey = KeyCode.F7,
            KeyCode modifierKey = KeyCode.None)
        {
            _uiController = uiController;
            _toggleKey = toggleKey;
            _modifierKey = modifierKey;
        }

        /// <summary>
        /// Update method to be called each frame to check for keyboard input
        /// </summary>
        public void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                if (_modifierKey == KeyCode.None || Input.GetKey(_modifierKey))
                {
                    _uiController.ToggleVisibility();
                }
            }
        }
    }
}