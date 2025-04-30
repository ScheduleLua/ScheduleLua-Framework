using System;
using UnityEngine;
using ScheduleLua.API.Core;

namespace ScheduleLua.API.UI.Dialog
{
    /// <summary>
    /// Handles dialogue interactions in the game
    /// </summary>
    public class DialogueHandler : MonoBehaviour
    {
        /// <summary>
        /// The currently active dialogue
        /// </summary>
        public static Dialogue activeDialogue { get; set; }

        /// <summary>
        /// Ends the current dialogue
        /// </summary>
        public void EndDialogue()
        {
            try
            {
                if (activeDialogue != null)
                {
                    // Implementation would clean up the active dialogue
                    Debug.Log("DialogueHandler: Ending active dialogue");
                    activeDialogue = null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in DialogueHandler.EndDialogue: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Represents a dialogue in the game
    /// </summary>
    public class Dialogue
    {
        public string Text { get; set; }
        public string Title { get; set; }
    }
} 