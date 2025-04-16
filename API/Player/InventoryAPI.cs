using System;
using System.Collections.Generic;
using System.Text;
using MoonSharp.Interpreter;
using ScheduleOne.PlayerScripts;
using ScheduleOne.ItemFramework;
using ScheduleLua.API.Core;

namespace ScheduleLua.API.Player
{
    public static class InventoryAPI
    {
        /// <summary>
        /// Registers all inventory-related API functions with the Lua engine
        /// </summary>
        public static void RegisterAPI(Script luaEngine)
        {
            if (luaEngine == null)
                throw new ArgumentNullException(nameof(luaEngine));

            // Inventory functions
            luaEngine.Globals["GetInventorySlotCount"] = (Func<int>)GetInventorySlotCount;
            luaEngine.Globals["GetInventoryItemAt"] = (Func<int, string>)GetInventoryItemAt;
            luaEngine.Globals["AddItemToInventory"] = (Func<string, int, bool>)AddItemToInventory;
            luaEngine.Globals["RemoveItemFromInventory"] = (Func<string, int, bool>)RemoveItemFromInventory;
        }

        /// <summary>
        /// Gets the total number of inventory slots available to the player
        /// </summary>
        /// <returns>The number of inventory slots</returns>
        public static int GetInventorySlotCount()
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory == null)
                return 0;

            return inventory.TOTAL_SLOT_COUNT;
        }

        /// <summary>
        /// Gets the name of the item in the specified inventory slot
        /// </summary>
        /// <param name="slotIndex">The inventory slot index to check</param>
        /// <returns>The name of the item or an empty string if no item exists</returns>
        public static string GetInventoryItemAt(int slotIndex)
        {
            ScheduleOne.PlayerScripts.Player player = ScheduleOne.PlayerScripts.Player.Local;
            if (player == null || player.Inventory == null || slotIndex < 0 || slotIndex >= player.Inventory.Length)
                return string.Empty;

            ItemSlot slot = player.Inventory[slotIndex];
            if (slot == null || slot.ItemInstance == null)
                return string.Empty;

            return slot.ItemInstance.Name ?? string.Empty;
        }

        /// <summary>
        /// Adds an item to the player's inventory
        /// </summary>
        /// <param name="itemName">The name of the item to add</param>
        /// <param name="amount">The amount of the item to add</param>
        /// <returns>True if the item was added successfully, false otherwise</returns>
        public static bool AddItemToInventory(string itemName, int amount = 1)
        {
            try
            {
                // This is a simplified implementation - would need to be expanded
                // to find the item definition by name and add it properly
                LuaUtility.LogWarning("AddItemToInventory not fully implemented yet");
                return false;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error adding item to inventory", ex);
                return false;
            }
        }

        /// <summary>
        /// Removes an item from the player's inventory
        /// </summary>
        /// <param name="itemName">The name of the item to remove</param>
        /// <param name="amount">The amount of the item to remove</param>
        /// <returns>True if the item was removed successfully, false otherwise</returns>
        public static bool RemoveItemFromInventory(string itemName, int amount = 1)
        {
            try
            {
                // This is a simplified implementation - would need to be expanded
                LuaUtility.LogWarning("RemoveItemFromInventory not fully implemented yet");
                return false;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error removing item from inventory", ex);
                return false;
            }
        }
    }
}
