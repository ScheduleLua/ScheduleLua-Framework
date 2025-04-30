using System;
using UnityEngine;
using ScheduleLua.API.Core;

namespace ScheduleLua.API.UI.Dialog
{
    /// <summary>
    /// Represents the canvas for displaying dialogues in the game
    /// </summary>
    public class DialogueCanvas : MonoBehaviour
    {
        /// <summary>
        /// Whether the dialogue is currently active
        /// </summary>
        public bool isActive { get; private set; }

        /// <summary>
        /// Overrides the text displayed in the dialogue
        /// </summary>
        public void OverrideText(string text)
        {
            try
            {
                isActive = true;
                // Implementation would interact with game's dialogue system
                Debug.Log($"DialogueCanvas: Overriding text to: {text}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in DialogueCanvas.OverrideText: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops the text override
        /// </summary>
        public void StopTextOverride()
        {
            try
            {
                // Implementation would restore dialogue system to normal
                Debug.Log("DialogueCanvas: Stopping text override");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in DialogueCanvas.StopTextOverride: {ex.Message}");
            }
        }

        /// <summary>
        /// Ends the current dialogue
        /// </summary>
        public void EndDialogue()
        {
            try
            {
                isActive = false;
                // Implementation would close dialogue UI
                Debug.Log("DialogueCanvas: Ending dialogue");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in DialogueCanvas.EndDialogue: {ex.Message}");
            }
        }
    }
} 