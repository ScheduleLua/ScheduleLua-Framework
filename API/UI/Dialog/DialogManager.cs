using System;
using System.Collections;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using ScheduleLua.API.Core;
using UnityEngine;

namespace ScheduleLua.API.UI.Dialog
{
    /// <summary>
    /// Manages dialog functionality for Lua scripts
    /// </summary>
    public class DialogManager
    {
        // Singleton instance
        private static DialogManager _instance;
        
        // Public property to access the singleton instance
        public static DialogManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DialogManager();
                }
                return _instance;
            }
        }
        
        private string _currentDialogText;
        private string _currentDialogTitle;
        private bool _dialogVisible;
        private List<string> _dialogChoices = new List<string>();
        private DynValue _choiceCallback;
        private float _dialogCloseTime;
        
        // Private constructor for singleton pattern
        private DialogManager() { }
        
        /// <summary>
        /// Shows a dialog with the specified title and text
        /// </summary>
        public void ShowDialogue(string title, string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                {
                    LuaUtility.LogWarning("ShowDialogue: text is null or empty");
                    return;
                }
                
                _currentDialogTitle = title;
                _currentDialogText = text;
                _dialogVisible = true;
                _dialogChoices.Clear();
                _choiceCallback = null;
                _dialogCloseTime = 0;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error showing dialogue: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Shows a dialog with auto-close after the specified time
        /// </summary>
        public void ShowDialogueWithTimeout(string title, string text, float timeout = 5.0f)
        {
            try
            {
                ShowDialogue(title, text);
                _dialogCloseTime = Time.time + timeout;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error showing dialogue with timeout: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Shows a dialog with choices
        /// </summary>
        public void ShowChoiceDialogue(string title, string text, Table choices, DynValue callback)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                {
                    LuaUtility.LogWarning("ShowChoiceDialogue: text is null or empty");
                    return;
                }
                
                if (choices == null)
                {
                    LuaUtility.LogWarning("ShowChoiceDialogue: choices table is null");
                    return;
                }
                
                if (callback == null || callback.Type != DataType.Function)
                {
                    LuaUtility.LogWarning("ShowChoiceDialogue: callback is null or not a function");
                    return;
                }
                
                _currentDialogTitle = title;
                _currentDialogText = text;
                _dialogVisible = true;
                _dialogChoices.Clear();
                
                // Convert the Lua table to a list of choices
                foreach (var pair in choices.Pairs)
                {
                    if (pair.Value.Type == DataType.String)
                    {
                        _dialogChoices.Add(pair.Value.String);
                    }
                }
                
                _choiceCallback = callback;
                _dialogCloseTime = 0;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error showing choice dialogue: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Closes the current dialog
        /// </summary>
        public void CloseDialogue()
        {
            _dialogVisible = false;
            _dialogChoices.Clear();
            _choiceCallback = null;
        }
        
        /// <summary>
        /// Draws the dialog if visible
        /// </summary>
        public void DrawDialog()
        {
            if (!_dialogVisible)
                return;
                
            try
            {
                // Check for auto-close timeout
                if (_dialogCloseTime > 0 && Time.time > _dialogCloseTime)
                {
                    CloseDialogue();
                    return;
                }
                
                // Calculate dialog size and position
                float dialogWidth = Mathf.Min(500, Screen.width * 0.8f);
                float dialogHeight = _dialogChoices.Count > 0 ? 250 : 200;
                
                Rect dialogRect = new Rect(
                    Screen.width / 2 - dialogWidth / 2,
                    Screen.height / 2 - dialogHeight / 2,
                    dialogWidth,
                    dialogHeight
                );
                
                // Dialog background
                GUI.backgroundColor = new Color(0.1f, 0.1f, 0.2f, 0.95f);
                GUI.Box(dialogRect, "", UIManager.StyleManager.BoxStyle ?? GUI.skin.box);
                
                // Title bar
                Rect titleRect = new Rect(dialogRect.x, dialogRect.y, dialogRect.width, 30);
                GUI.backgroundColor = new Color(0.2f, 0.2f, 0.4f, 0.95f);
                GUI.Box(titleRect, _currentDialogTitle, UIManager.StyleManager.TitleStyle ?? GUI.skin.box);
                
                // Dialog content
                Rect contentRect = new Rect(
                    dialogRect.x + 10,
                    dialogRect.y + 40,
                    dialogRect.width - 20,
                    dialogRect.height - 90
                );
                
                GUI.backgroundColor = Color.white;
                GUI.Label(contentRect, _currentDialogText, UIManager.StyleManager.LabelStyle ?? GUI.skin.label);
                
                // Draw choices if any
                if (_dialogChoices.Count > 0)
                {
                    float buttonHeight = 30;
                    float buttonSpacing = 5;
                    float totalButtonHeight = _dialogChoices.Count * buttonHeight + (_dialogChoices.Count - 1) * buttonSpacing;
                    float buttonsStartY = contentRect.y + contentRect.height + 10;
                    
                    for (int i = 0; i < _dialogChoices.Count; i++)
                    {
                        Rect buttonRect = new Rect(
                            dialogRect.x + 20,
                            buttonsStartY + i * (buttonHeight + buttonSpacing),
                            dialogRect.width - 40,
                            buttonHeight
                        );
                        
                        if (GUI.Button(buttonRect, _dialogChoices[i], UIManager.StyleManager.ButtonStyle ?? GUI.skin.button))
                        {
                            InvokeChoiceCallback(i);
                            CloseDialogue();
                        }
                    }
                }
                else
                {
                    // Close button for normal dialogs
                    Rect closeButtonRect = new Rect(
                        dialogRect.x + dialogRect.width - 110,
                        dialogRect.y + dialogRect.height - 40,
                        100,
                        30
                    );
                    
                    if (GUI.Button(closeButtonRect, "Close", UIManager.StyleManager.ButtonStyle ?? GUI.skin.button))
                    {
                        CloseDialogue();
                    }
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error drawing dialog: {ex.Message}", ex);
                CloseDialogue();
            }
        }
        
        /// <summary>
        /// Invokes the choice callback with the selected index
        /// </summary>
        private void InvokeChoiceCallback(int choiceIndex)
        {
            try
            {
                if (_choiceCallback != null && _choiceCallback.Type == DataType.Function)
                {
                    ScheduleLua.Core.Instance._luaEngine.Call(_choiceCallback, DynValue.NewNumber(choiceIndex + 1));
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error invoking choice callback: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Sets custom dialogue for an NPC customer
        /// </summary>
        public void SetCustomerDialogue(string npcId, string newText)
        {
            try
            {
                LuaUtility.LogWarning("SetCustomerDialogue is not implemented in this version");
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in SetCustomerDialogue: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Sets custom dialogue for an NPC dealer
        /// </summary>
        public void SetDealerDialogue(string npcId, string newText)
        {
            try
            {
                LuaUtility.LogWarning("SetDealerDialogue is not implemented in this version");
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in SetDealerDialogue: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Sets custom dialogue for an NPC shop keeper
        /// </summary>
        public void SetShopDialogue(string npcId, string newText)
        {
            try
            {
                LuaUtility.LogWarning("SetShopDialogue is not implemented in this version");
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in SetShopDialogue: {ex.Message}", ex);
            }
        }
    }
} 