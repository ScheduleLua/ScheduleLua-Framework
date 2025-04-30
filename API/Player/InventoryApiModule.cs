using System;
using System.Collections.Generic;
using System.Text;
using MoonSharp.Interpreter;
using ScheduleOne.PlayerScripts;
using ScheduleOne.ItemFramework;
using ScheduleLua.API.Core;
using ScheduleOne.Storage;
using ScheduleOne;
using ScheduleOne.Product;
using ScheduleLua.API.Base;

namespace ScheduleLua.API.Player
{
    /// <summary>
    /// Module that provides Lua API access to inventory-related functionality
    /// </summary>
    public class InventoryApiModule : BaseLuaApiModule
    {
        /// <summary>
        /// Registers all inventory-related API functions with the Lua engine
        /// </summary>
        public override void RegisterAPI(Script luaEngine)
        {
            if (luaEngine == null)
                throw new ArgumentNullException(nameof(luaEngine));

            // Inventory functions
            luaEngine.Globals["GetInventorySlotCount"] = (Func<int>)GetInventorySlotCount;
            luaEngine.Globals["GetInventoryItemAt"] = (Func<int, string>)GetInventoryItemAt;
            luaEngine.Globals["AddItemToInventory"] = (Action<string, int>)AddItemToInventory;
            luaEngine.Globals["RemoveItemFromInventory"] = (Func<string, int, bool>)RemoveItemFromInventory;

            // Equipped item functions
            luaEngine.Globals["IsItemEquipped"] = (Func<bool>)IsItemEquipped;
            luaEngine.Globals["GetEquippedItemName"] = (Func<string>)GetEquippedItemName;
            luaEngine.Globals["GetEquippedItemAmount"] = (Func<int>)GetEquippedItemAmount;
            luaEngine.Globals["GetEquippedWeaponAmmo"] = (Func<int>)GetEquippedWeaponAmmo;
            luaEngine.Globals["SetEquippedWeaponAmmo"] = (Func<int, bool>)SetEquippedWeaponAmmo;
        }

        /// <summary>
        /// Gets the total number of inventory slots available to the player
        /// </summary>
        /// <returns>The number of inventory slots</returns>
        public int GetInventorySlotCount()
        {
            try
            {
                PlayerInventory inventory = PlayerInventory.Instance;
                if (inventory == null)
                    return 0;

                return inventory.TOTAL_SLOT_COUNT;
            }
            catch (Exception ex)
            {
                LogError($"Error getting inventory slot count: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the name of the item in the specified inventory slot
        /// </summary>
        /// <param name="slotIndex">The inventory slot index to check</param>
        /// <returns>The name of the item or an empty string if no item exists</returns>
        public string GetInventoryItemAt(int slotIndex)
        {
            try
            {
                ScheduleOne.PlayerScripts.Player player = ScheduleOne.PlayerScripts.Player.Local;
                if (player == null || player.Inventory == null || slotIndex < 0 || slotIndex >= player.Inventory.Length)
                    return string.Empty;

                ItemSlot slot = player.Inventory[slotIndex];
                if (slot == null || slot.ItemInstance == null)
                    return string.Empty;

                return slot.ItemInstance.Name ?? string.Empty;
            }
            catch (Exception ex)
            {
                LogError($"Error getting inventory item at index {slotIndex}: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Adds an item to the player's inventory
        /// </summary>
        /// <param name="itemName">The name of the item to add</param>
        /// <param name="amount">The amount of the item to add</param>
        public void AddItemToInventory(string itemName, int amount = 1)
        {
            try
            {
                if (string.IsNullOrEmpty(itemName))
                {
                    LogError($"Invalid or unknown item: '{itemName}'.");
                    return;
                }

                if (amount <= 0) amount = 1;

                var itemDef = ScheduleOne.Registry.GetItem(itemName);
                if (itemDef == null)
                {
                    LogError($"Item definition for '{itemName}' could not be found.");
                    return;
                }

                ItemInstance itemInstance;

                if (itemDef is ProductDefinition productDef)
                {
                    itemInstance = new ProductItemInstance(productDef, amount, EQuality.Standard);
                }
                else
                {
                    itemInstance = new StorableItemInstance(itemDef, amount);
                }

                PlayerInventory.Instance.AddItemToInventory(itemInstance);
                LogInfo($"Added {amount}x {itemName} to inventory.");
            }
            catch (Exception ex)
            {
                LogError($"Error adding item to inventory: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes an item from the player's inventory
        /// </summary>
        /// <param name="itemName">The name of the item to remove</param>
        /// <param name="amount">The amount of the item to remove</param>
        /// <returns>True if the item was removed successfully, false otherwise</returns>
        public bool RemoveItemFromInventory(string itemName, int amount = 1)
        {
            try
            {
                // This is a simplified implementation - would need to be expanded
                LogWarning("RemoveItemFromInventory not fully implemented yet");
                return false;
            }
            catch (Exception ex)
            {
                LogError($"Error removing item from inventory: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if the player has an item equipped
        /// </summary>
        /// <returns>True if any item is equipped, false otherwise</returns>
        public bool IsItemEquipped() 
        {
            try 
            {
                return PlayerInventory.Instance?.isAnythingEquipped ?? false; 
            }
            catch (Exception ex)
            {
                LogError($"Error checking if item is equipped: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the name of the currently equipped item
        /// </summary>
        /// <returns>The name of the equipped item or an empty string if no item is equipped</returns>
        public string GetEquippedItemName() 
        {
            try
            {
                return PlayerInventory.Instance?.equippedSlot?.ItemInstance?.Name ?? string.Empty;
            }
            catch (Exception ex)
            {
                LogError($"Error getting equipped item name: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the quantity of the currently equipped item
        /// </summary>
        /// <returns>The quantity of the equipped item or 0 if no item is equipped</returns>
        public int GetEquippedItemAmount() 
        {
            try
            {
                return PlayerInventory.Instance?.equippedSlot?.ItemInstance?.Quantity ?? 0;
            }
            catch (Exception ex)
            {
                LogError($"Error getting equipped item amount: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the ammo count of the currently equipped weapon
        /// </summary>
        /// <returns>The ammo count for the equipped weapon or 0 if no weapon is equipped</returns>
        public int GetEquippedWeaponAmmo()
        {
            try
            {
                var equipped = PlayerInventory.Instance?.equippedSlot?.ItemInstance;
                return equipped is IntegerItemInstance rangedWeapon ? rangedWeapon.Value : 0;
            }
            catch (Exception ex)
            {
                LogError($"Error getting equipped weapon ammo: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Sets the ammo count for the currently equipped weapon
        /// </summary>
        /// <param name="amount">The amount of ammo to set</param>
        /// <returns>True if the ammo was set successfully, false if no ranged weapon is equipped</returns>
        public bool SetEquippedWeaponAmmo(int amount)
        {
            try
            {
                var equipped = PlayerInventory.Instance?.equippedSlot?.ItemInstance;
                if (equipped is IntegerItemInstance rangedWeapon)
                {
                    rangedWeapon.Value = amount;
                    LogInfo($"Set equipped weapon ammo to {amount}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                LogError($"Error setting equipped weapon ammo: {ex.Message}");
                return false;
            }
        }
    }
} 