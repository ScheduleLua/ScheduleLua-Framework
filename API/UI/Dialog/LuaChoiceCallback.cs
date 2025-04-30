using System;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using ScheduleLua.API.Core;

namespace ScheduleLua.API.UI.Dialog
{
    /// <summary>
    /// MonoBehaviour that handles choice selection for Lua dialogues
    /// </summary>
    public class LuaChoiceCallback : MonoBehaviour
    {
        private List<string> _choices = new List<string>();
        private DynValue _callback;
        private bool _isMonitoring = false;
        private KeyCode[] _numberKeys =
        [
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
            KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9
        ];

        /// <summary>
        /// Sets the choices and callback for this instance
        /// </summary>
        public void SetChoices(List<string> choices, DynValue callback)
        {
            _choices = choices;
            _callback = callback;
            _isMonitoring = false;
        }

        /// <summary>
        /// Begins monitoring for key input to select choices
        /// </summary>
        public void StartMonitoring()
        {
            _isMonitoring = true;
        }

        /// <summary>
        /// Called each frame to check for key input
        /// </summary>
        private void Update()
        {
            if (!_isMonitoring || _choices == null || _choices.Count == 0)
                return;

            for (int i = 0; i < _numberKeys.Length && i < _choices.Count; i++)
            {
                if (UnityEngine.Input.GetKeyDown(_numberKeys[i]))
                {
                    SelectChoice(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Handles the selection of a choice option
        /// </summary>
        private void SelectChoice(int index)
        {
            if (!_isMonitoring || index < 0 || index >= _choices.Count)
                return;

            _isMonitoring = false;

            if (_callback != null && _callback.Type == DataType.Function)
            {
                try
                {
                    ModCore.Instance._luaEngine.Call(_callback, index + 1);
                }
                catch (Exception ex)
                {
                    LuaUtility.LogError($"Error in choice callback: {ex.Message}", ex);
                }
            }

            DialogManager.Instance?.CloseDialogue();
        }

        /// <summary>
        /// Called when the object is destroyed
        /// </summary>
        private void OnDestroy()
        {
            _choices = null;
            _callback = null;
            _isMonitoring = false;
        }
    }
} 