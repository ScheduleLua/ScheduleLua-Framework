using MoonSharp.Interpreter;
using ScheduleLua.API.Base;
using ScheduleLua.API.Core;
using ScheduleLua.API.UI.Controls;
using ScheduleLua.API.UI.Dialog;
using ScheduleLua.API.UI.Notifications;
using ScheduleLua.API.UI.Phone;
using ScheduleLua.API.UI.Storage;
using ScheduleLua.API.UI.Tooltips;
using ScheduleLua.API.UI.Utils;
using ScheduleLua.API.UI.Windows;

namespace ScheduleLua.API.UI
{
    /// <summary>
    /// Provides UI functionality to Lua scripts using MelonLoader's IMGUI implementation
    /// </summary>
    public class UIAPI : BaseLuaApiModule
    {
        // Private manager instances
        private static WindowManager _windowManager;
        private static ControlManager _controlManager;
        private static DialogManager _dialogManager;
        private static TooltipManager _tooltipManager;
        private static PhoneIntegration _phoneIntegration;
        private static NotificationManager _notificationManager;
        private static StorageEntityManager _storageEntityManager;

        // Style change tracking
        private static bool _styleChangesMade = false;

        // GUI variables
        private static bool _guiInitialized = false;
        private static bool _guiEnabled = true;

        /// <summary>
        /// Registers UI API functions with the Lua engine
        /// </summary>
        public override void RegisterAPI(Script luaEngine)
        {
            if (luaEngine == null)
                throw new ArgumentNullException(nameof(luaEngine));

            // Initialize UIManager first, so it's ready for other components
            UIManager.Initialize();

            // Initialize UI managers
            _windowManager = new WindowManager();
            _controlManager = new ControlManager(_windowManager);
            _dialogManager = DialogManager.Instance;
            _tooltipManager = new TooltipManager();
            _phoneIntegration = new PhoneIntegration();
            _notificationManager = new NotificationManager();
            _storageEntityManager = new StorageEntityManager();

            // Register Window Management functions
            luaEngine.Globals["CreateWindow"] = (Func<string, string, float, float, float, float, string>)_windowManager.CreateWindow;
            luaEngine.Globals["SetWindowPosition"] = (Action<string, float, float>)_windowManager.SetWindowPosition;
            luaEngine.Globals["SetWindowSize"] = (Action<string, float, float>)_windowManager.SetWindowSize;
            luaEngine.Globals["ShowWindow"] = (Action<string, bool>)_windowManager.ShowWindow;
            luaEngine.Globals["IsWindowVisible"] = (Func<string, bool>)_windowManager.IsWindowVisible;
            luaEngine.Globals["DestroyWindow"] = (Action<string>)_windowManager.DestroyWindow;

            // Register Control Management functions
            luaEngine.Globals["AddButton"] = (Func<string, string, string, DynValue, string>)_controlManager.AddButton;
            luaEngine.Globals["AddLabel"] = (Func<string, string, string, string>)_controlManager.AddLabel;
            luaEngine.Globals["AddTextField"] = (Func<string, string, string, string>)_controlManager.AddTextField;
            luaEngine.Globals["GetControlText"] = (Func<string, string>)_controlManager.GetControlText;
            luaEngine.Globals["SetControlText"] = (Action<string, string>)_controlManager.SetControlText;
            luaEngine.Globals["SetControlPosition"] = (Action<string, float, float>)_controlManager.SetControlPosition;
            luaEngine.Globals["SetControlSize"] = (Action<string, float, float>)_controlManager.SetControlSize;
            luaEngine.Globals["ShowControl"] = (Action<string, bool>)_controlManager.ShowControl;
            luaEngine.Globals["DestroyControl"] = (Action<string>)_controlManager.DestroyControl;

            // Register Global UI functions
            luaEngine.Globals["EnableGUI"] = (Action<bool>)EnableGUI;
            luaEngine.Globals["IsGUIEnabled"] = (Func<bool>)IsGUIEnabled;

            // Register Tooltip functions
            luaEngine.Globals["ShowTooltip"] = (Action<string, float, float, bool>)_tooltipManager.ShowTooltip;

            // Register Phone functions
            luaEngine.Globals["IsPhoneOpen"] = (Func<bool>)_phoneIntegration.IsPhoneOpen;
            luaEngine.Globals["OpenPhone"] = (Action)_phoneIntegration.OpenPhone;
            luaEngine.Globals["ClosePhone"] = (Action)_phoneIntegration.ClosePhone;
            luaEngine.Globals["TogglePhoneFlashlight"] = (Action)_phoneIntegration.TogglePhoneFlashlight;
            luaEngine.Globals["IsPhoneFlashlightOn"] = (Func<bool>)_phoneIntegration.IsPhoneFlashlightOn;

            // Register Notification functions
            luaEngine.Globals["ShowNotification"] = (Action<string, string>)_notificationManager.ShowNotification;
            luaEngine.Globals["ShowNotificationWithIcon"] = DynValue.NewCallback(_notificationManager.ShowNotificationWithIconDyn);
            luaEngine.Globals["ShowNotificationWithTimeout"] = (Action<string, float>)_notificationManager.ShowNotificationWithTimeout;
            luaEngine.Globals["ShowNotificationWithIconAndTimeout"] = DynValue.NewCallback(_notificationManager.ShowNotificationWithIconAndTimeoutDyn);

            // Register UI Item functions
            luaEngine.Globals["GetHoveredItemName"] = (Func<string>)UIUtilities.GetHoveredItemName;
            luaEngine.Globals["IsItemBeingDragged"] = (Func<bool>)UIUtilities.IsItemBeingDragged;

            // Register sprite loading utility
            luaEngine.Globals["LoadSpriteFromFile"] = DynValue.NewCallback(
                (Func<ScriptExecutionContext, CallbackArguments, DynValue>)
                ((ctx, args) =>
                {
                    if (args.Count < 1)
                        return DynValue.Nil;

                    string filePath = args[0].String;
                    return DynValue.FromObject(ctx.GetScript(), UIUtilities.LoadSpriteFromFile(filePath, ctx));
                }));

            // Register Dialog functions
            luaEngine.Globals["ShowDialogue"] = (Action<string, string>)_dialogManager.ShowDialogue;
            luaEngine.Globals["ShowDialogueWithTimeout"] = (Action<string, string, float>)_dialogManager.ShowDialogueWithTimeout;
            luaEngine.Globals["ShowChoiceDialogue"] = (Action<string, string, Table, DynValue>)_dialogManager.ShowChoiceDialogue;
            luaEngine.Globals["CloseDialogue"] = (Action)_dialogManager.CloseDialogue;
            luaEngine.Globals["SetCustomerDialogue"] = (Action<string, string>)_dialogManager.SetCustomerDialogue;
            luaEngine.Globals["SetDealerDialogue"] = (Action<string, string>)_dialogManager.SetDealerDialogue;
            luaEngine.Globals["SetShopDialogue"] = (Action<string, string>)_dialogManager.SetShopDialogue;

            // Register UI Style functions - use wrapped functions to track style changes
            luaEngine.Globals["SetWindowStyle"] = (Action<string, float, float, float, float>)SetWindowStyleWrapper;
            luaEngine.Globals["SetButtonStyle"] = (Action<string, float, float, float, float>)SetButtonStyleWrapper;
            luaEngine.Globals["SetLabelStyle"] = (Action<string, float, float, float, float>)SetLabelStyleWrapper;
            luaEngine.Globals["SetTextFieldStyle"] = (Action<string, float, float, float, float>)SetTextFieldStyleWrapper;
            luaEngine.Globals["SetBoxStyle"] = (Action<string, float, float, float, float>)SetBoxStyleWrapper;
            luaEngine.Globals["SetFontSize"] = (Action<string, int>)SetFontSizeWrapper;
            luaEngine.Globals["SetFontStyle"] = (Action<string, string>)SetFontStyleWrapper;
            luaEngine.Globals["SetTextAlignment"] = (Action<string, string>)SetTextAlignmentWrapper;
            luaEngine.Globals["SetBorder"] = (Action<string, int, int, int, int>)SetBorderWrapper;
            luaEngine.Globals["SetPadding"] = (Action<string, int, int, int, int>)SetPaddingWrapper;

            // Register Storage Entity functions
            luaEngine.Globals["CreateStorageEntity"] = (Func<string, int, int, string>)_storageEntityManager.CreateStorageEntity;
            luaEngine.Globals["OpenStorageEntity"] = (Action<string>)_storageEntityManager.OpenStorageEntity;
            luaEngine.Globals["CloseStorageEntity"] = (Action<string>)_storageEntityManager.CloseStorageEntity;
            luaEngine.Globals["AddItemToStorage"] = (Func<string, string, int, bool>)_storageEntityManager.AddItemToStorage;
            luaEngine.Globals["GetStorageItems"] = (Func<string, Table>)_storageEntityManager.GetStorageItems;
            luaEngine.Globals["IsStorageOpen"] = (Func<string, bool>)_storageEntityManager.IsStorageOpen;
            luaEngine.Globals["SetStorageName"] = (Action<string, string>)_storageEntityManager.SetStorageName;
            luaEngine.Globals["SetStorageSubtitle"] = (Action<string, string>)_storageEntityManager.SetStorageSubtitle;
            luaEngine.Globals["ClearStorageContents"] = (Action<string>)_storageEntityManager.ClearStorageContents;
            luaEngine.Globals["GetStorageEntityCount"] = (Func<int>)_storageEntityManager.GetStorageEntityCount;

            // Initialize GUI
            InitializeGUI();
        }

        #region Style Function Wrappers

        private static void SetWindowStyleWrapper(string colorName, float r, float g, float b, float a = 1.0f)
        {
            UIManager.StyleManager.SetWindowStyle(colorName, r, g, b, a);
            _styleChangesMade = true;
            LuaUtility.Log($"Window style updated: {colorName} set to ({r}, {g}, {b}, {a})");
            ApplyStyleChanges();
        }

        private static void SetButtonStyleWrapper(string colorName, float r, float g, float b, float a = 1.0f)
        {
            UIManager.StyleManager.SetButtonStyle(colorName, r, g, b, a);
            _styleChangesMade = true;
            LuaUtility.Log($"Button style updated: {colorName} set to ({r}, {g}, {b}, {a})");
            ApplyStyleChanges();
        }

        private static void SetLabelStyleWrapper(string colorName, float r, float g, float b, float a = 1.0f)
        {
            UIManager.StyleManager.SetLabelStyle(colorName, r, g, b, a);
            _styleChangesMade = true;
            LuaUtility.Log($"Label style updated: {colorName} set to ({r}, {g}, {b}, {a})");
            ApplyStyleChanges();
        }

        private static void SetTextFieldStyleWrapper(string colorName, float r, float g, float b, float a = 1.0f)
        {
            UIManager.StyleManager.SetTextFieldStyle(colorName, r, g, b, a);
            _styleChangesMade = true;
            LuaUtility.Log($"TextField style updated: {colorName} set to ({r}, {g}, {b}, {a})");
            ApplyStyleChanges();
        }

        private static void SetBoxStyleWrapper(string colorName, float r, float g, float b, float a = 1.0f)
        {
            UIManager.StyleManager.SetBoxStyle(colorName, r, g, b, a);
            _styleChangesMade = true;
            LuaUtility.Log($"Box style updated: {colorName} set to ({r}, {g}, {b}, {a})");
            ApplyStyleChanges();
        }

        private static void SetFontSizeWrapper(string styleName, int size)
        {
            UIManager.StyleManager.SetFontSize(styleName, size);
            _styleChangesMade = true;
            LuaUtility.Log($"{styleName} font size set to {size}");
            ApplyStyleChanges();
        }

        private static void SetFontStyleWrapper(string styleName, string fontStyle)
        {
            UIManager.StyleManager.SetFontStyle(styleName, fontStyle);
            _styleChangesMade = true;
            LuaUtility.Log($"{styleName} font style set to {fontStyle}");
            ApplyStyleChanges();
        }

        private static void SetTextAlignmentWrapper(string styleName, string alignment)
        {
            UIManager.StyleManager.SetTextAlignment(styleName, alignment);
            _styleChangesMade = true;
            LuaUtility.Log($"{styleName} text alignment set to {alignment}");
            ApplyStyleChanges();
        }

        private static void SetBorderWrapper(string styleName, int left, int right, int top, int bottom)
        {
            UIManager.StyleManager.SetBorder(styleName, left, right, top, bottom);
            _styleChangesMade = true;
            LuaUtility.Log($"{styleName} border set to ({left}, {right}, {top}, {bottom})");
            ApplyStyleChanges();
        }

        private static void SetPaddingWrapper(string styleName, int left, int right, int top, int bottom)
        {
            UIManager.StyleManager.SetPadding(styleName, left, right, top, bottom);
            _styleChangesMade = true;
            LuaUtility.Log($"{styleName} padding set to ({left}, {right}, {top}, {bottom})");
            ApplyStyleChanges();
        }

        private static void ApplyStyleChanges()
        {
            if (_styleChangesMade)
            {
                // Just mark for refresh - actual refresh happens in OnGUI
                // This avoids trying to recreate styles outside of OnGUI
                UIManager.StyleManager.RefreshStyles();
                _styleChangesMade = false;
                LuaUtility.Log("UI styles marked for refresh on next frame");
            }
        }

        #endregion

        #region GUI Management

        /// <summary>
        /// Initializes the GUI system for Lua scripts
        /// </summary>
        private static void InitializeGUI()
        {
            if (_guiInitialized)
                return;

            if (ModCore.Instance != null)
            {
                ModCore.Instance.OnGUICallback += OnGUI;
            }
            else
            {
                LuaUtility.LogError("Cannot register OnGUI callback: Core.Instance is null");
                return;
            }

            _guiInitialized = true;
            _guiEnabled = true;
            LuaUtility.Log("Lua GUI system initialized");
        }

        /// <summary>
        /// OnGUI callback for rendering all UI elements
        /// </summary>
        private static void OnGUI()
        {
            if (!_guiEnabled || !_guiInitialized)
            {
                return;
            }

            try
            {
                // Make sure UI styles are initialized - this is now safe because we're in OnGUI
                if (!UIManager.StyleManager.IsInitialized)
                {
                    UIManager.StyleManager.Initialize();
                }

                // Always ensure styles are initialized - this is safe in OnGUI context
                UIManager.StyleManager.InitializeStyles();

                // Apply any style changes before drawing
                ApplyStyleChanges();

                // Draw all registered windows
                _windowManager.DrawAllWindows();

                // Draw dialogs
                _dialogManager.DrawDialog();

                // Draw tooltips
                _tooltipManager.DrawTooltips();
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in UIAPI.OnGUI: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Enable or disable GUI rendering
        /// </summary>
        public static void EnableGUI(bool enable)
        {
            _guiEnabled = enable;
        }

        /// <summary>
        /// Check if GUI is enabled
        /// </summary>
        public static bool IsGUIEnabled()
        {
            return _guiEnabled;
        }

        #endregion
    }
}