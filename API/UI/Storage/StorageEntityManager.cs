using MoonSharp.Interpreter;
using ScheduleLua.API.Core;
using UnityEngine;

namespace ScheduleLua.API.UI.Storage
{
    /// <summary>
    /// Manages storage entities for Lua scripts
    /// </summary>
    public class StorageEntityManager
    {
        private Dictionary<string, StorageEntity> _storageEntities = new Dictionary<string, StorageEntity>();
        private Dictionary<string, ScheduleOne.Storage.StorageEntity> _gameStorageEntities = new Dictionary<string, ScheduleOne.Storage.StorageEntity>();
        private int _storageEntityCounter = 0;

        /// <summary>
        /// Creates a new storage entity
        /// </summary>
        public string CreateStorageEntity(string name, int slotCount, int rowCount)
        {
            try
            {
                if (slotCount <= 0)
                {
                    LuaUtility.LogWarning("CreateStorageEntity: slotCount must be greater than 0");
                    return string.Empty;
                }

                if (rowCount <= 0)
                {
                    LuaUtility.LogWarning("CreateStorageEntity: rowCount must be greater than 0");
                    return string.Empty;
                }

                // Generate a unique ID for this storage entity
                string entityId = $"storage_{++_storageEntityCounter}";

                // Create the storage entity with the specified parameters
                var entity = new StorageEntity
                {
                    Id = entityId,
                    Name = name,
                    Subtitle = "",
                    SlotCount = slotCount,
                    RowCount = rowCount,
                    Items = new List<StorageItem>(),
                    IsOpen = false
                };

                // Add to the dictionary of storage entities
                _storageEntities[entityId] = entity;

                // Create actual game storage entity
                try
                {
                    // Clamp slot count to valid range
                    slotCount = Mathf.Clamp(slotCount, 1, 50);

                    // Try to find an existing StorageEntity to use as a template
                    ScheduleOne.Storage.StorageEntity templateEntity = UnityEngine.Object.FindObjectOfType<ScheduleOne.Storage.StorageEntity>();
                    if (templateEntity == null)
                    {
                        LuaUtility.LogWarning("No StorageEntity template found in scene! Using new GameObject approach instead.");

                        // Create storage with manual approach (fallback)
                        GameObject storageObject = new GameObject($"LuaStorage_{name}");
                        UnityEngine.Object.DontDestroyOnLoad(storageObject);

                        // Add StorageEntity component
                        ScheduleOne.Storage.StorageEntity storageEntity = storageObject.AddComponent<ScheduleOne.Storage.StorageEntity>();

                        // Configure storage entity properties
                        storageEntity.StorageEntityName = name;
                        storageEntity.SlotCount = slotCount;
                        storageEntity.DisplayRowCount = rowCount;

                        // Configure for local-only mode
                        storageEntity.AccessSettings = ScheduleOne.Storage.StorageEntity.EAccessSettings.Full;
                        storageEntity.MaxAccessDistance = 999f;

                        // Initialize ItemSlots list with proper List
                        var slots = new List<ScheduleOne.ItemFramework.ItemSlot>();
                        for (int i = 0; i < slotCount; i++)
                        {
                            var slot = new ScheduleOne.ItemFramework.ItemSlot();
                            slot.SetSlotOwner(storageEntity);
                            slots.Add(slot);
                        }
                        storageEntity.ItemSlots = slots;

                        // Store the storage entity in our dictionary
                        _gameStorageEntities[entityId] = storageEntity;
                    }
                    else
                    {
                        // Create a storage entity using the template as a base (preferred method)
                        ScheduleOne.Storage.StorageEntity storageEntity = UnityEngine.Object.Instantiate(templateEntity);

                        // Configure storage entity properties
                        storageEntity.name = $"LuaStorage_{name}";
                        storageEntity.StorageEntityName = name;
                        storageEntity.SlotCount = slotCount;
                        storageEntity.DisplayRowCount = rowCount;

                        // Configure for local-only mode
                        storageEntity.AccessSettings = ScheduleOne.Storage.StorageEntity.EAccessSettings.Full;
                        storageEntity.MaxAccessDistance = 999f;

                        // Initialize ItemSlots list
                        var slots = new List<ScheduleOne.ItemFramework.ItemSlot>();
                        for (int i = 0; i < slotCount; i++)
                        {
                            var slot = new ScheduleOne.ItemFramework.ItemSlot();
                            slots.Add(slot);
                        }
                        storageEntity.ItemSlots = slots;

                        // Make sure the GameObject persists
                        UnityEngine.Object.DontDestroyOnLoad(storageEntity.gameObject);

                        // Store the storage entity in our dictionary
                        _gameStorageEntities[entityId] = storageEntity;
                    }

                    // Initially close the storage entity
                    CloseStorageEntity(entityId);
                }
                catch (Exception ex)
                {
                    LuaUtility.LogError($"Error creating game storage entity: {ex.Message}", ex);
                }

                return entityId;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error creating storage entity: {ex.Message}", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Opens a storage entity
        /// </summary>
        public void OpenStorageEntity(string entityId)
        {
            try
            {
                if (!_storageEntities.TryGetValue(entityId, out var entity))
                {
                    LuaUtility.LogWarning($"OpenStorageEntity: Entity '{entityId}' not found");
                    return;
                }

                entity.IsOpen = true;
                LuaUtility.Log($"Storage entity '{entity.Name}' opened");

                // Get the game storage entity and open it
                if (_gameStorageEntities.TryGetValue(entityId, out var gameEntity))
                {
                    try
                    {
                        gameEntity.Open();
                    }
                    catch (Exception ex)
                    {
                        LuaUtility.LogError($"Error calling Open method: {ex.Message}", ex);

                        // Fallback: Try manual opening through the StorageMenu
                        var storageMenu = ScheduleOne.DevUtilities.Singleton<ScheduleOne.UI.StorageMenu>.Instance;
                        if (storageMenu != null)
                        {
                            storageMenu.Open(gameEntity);
                        }
                        else
                        {
                            LuaUtility.LogError("StorageMenu instance not available");
                        }
                    }
                }
                else
                {
                    LuaUtility.LogWarning($"OpenStorageEntity: Game entity for '{entityId}' not found");
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error opening storage entity: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Closes a storage entity
        /// </summary>
        public void CloseStorageEntity(string entityId)
        {
            try
            {
                if (!_storageEntities.TryGetValue(entityId, out var entity))
                {
                    LuaUtility.LogWarning($"CloseStorageEntity: Entity '{entityId}' not found");
                    return;
                }

                entity.IsOpen = false;
                LuaUtility.Log($"Storage entity '{entity.Name}' closed");

                // Get the game storage entity and close it
                if (_gameStorageEntities.TryGetValue(entityId, out var gameEntity))
                {
                    gameEntity.Close();
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error closing storage entity: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Adds an item to a storage entity
        /// </summary>
        public bool AddItemToStorage(string entityId, string itemId, int quantity = 1)
        {
            try
            {
                if (!_storageEntities.TryGetValue(entityId, out var entity))
                {
                    LuaUtility.LogWarning($"AddItemToStorage: Entity '{entityId}' not found");
                    return false;
                }

                if (string.IsNullOrEmpty(itemId))
                {
                    LuaUtility.LogWarning("AddItemToStorage: itemId is null or empty");
                    return false;
                }

                if (quantity <= 0)
                {
                    LuaUtility.LogWarning("AddItemToStorage: quantity must be greater than 0");
                    return false;
                }

                // Check if we have a game storage entity
                if (_gameStorageEntities.TryGetValue(entityId, out var gameEntity))
                {
                    // Try to create an item instance from the item ID
                    ScheduleOne.ItemFramework.ItemDefinition itemDef = null;

                    // Check if the item exists in the registry first
                    if (!ScheduleLua.API.Registry.RegistryAPI.DoesItemExist(itemId))
                    {
                        LuaUtility.LogError($"Item with ID '{itemId}' not found in registry. Cannot add to storage entity '{entityId}'");
                        return false;
                    }

                    // Get the item definition using the Registry API
                    itemDef = ScheduleLua.API.Registry.RegistryAPI.GetItemDirect(itemId);
                    if (itemDef == null)
                    {
                        LuaUtility.LogError($"Failed to retrieve item definition for '{itemId}' despite item existing in registry");
                        return false;
                    }

                    // Create an item instance
                    ScheduleOne.ItemFramework.ItemInstance itemInstance = itemDef.GetDefaultInstance();
                    if (itemInstance == null)
                    {
                        LuaUtility.LogError($"Failed to create item instance for {itemId}");
                        return false;
                    }

                    // Set the quantity
                    itemInstance.SetQuantity(quantity);

                    // Check if we can fit this item
                    if (!gameEntity.CanItemFit(itemInstance))
                    {
                        LuaUtility.LogWarning($"Cannot fit item {itemId} x{quantity} in storage {entityId}");
                        return false;
                    }

                    // Use the built-in method to insert the item
                    try
                    {
                        gameEntity.InsertItem(itemInstance, false);

                        // Also update our internal model
                        var existingItem = entity.Items.Find(i => i.Id == itemId);
                        if (existingItem != null)
                        {
                            existingItem.Quantity += quantity;
                        }
                        else
                        {
                            var newItem = new StorageItem
                            {
                                Id = itemId,
                                Quantity = quantity
                            };
                            entity.Items.Add(newItem);
                        }

                        return true;
                    }
                    catch (Exception ex)
                    {
                        LuaUtility.LogError($"Error inserting item: {ex.Message}", ex);

                        // Fallback: Try to insert manually to an empty slot
                        if (gameEntity.ItemSlots != null)
                        {
                            for (int i = 0; i < gameEntity.ItemSlots.Count; i++)
                            {
                                if (gameEntity.ItemSlots[i] != null && gameEntity.ItemSlots[i].ItemInstance == null)
                                {
                                    // Use SetStoredItem method instead of direct property assignment
                                    gameEntity.ItemSlots[i].SetStoredItem(itemInstance, true);

                                    // Also update our internal model
                                    var existingItem = entity.Items.Find(i => i.Id == itemId);
                                    if (existingItem != null)
                                    {
                                        existingItem.Quantity += quantity;
                                    }
                                    else
                                    {
                                        var newItem = new StorageItem
                                        {
                                            Id = itemId,
                                            Quantity = quantity
                                        };
                                        entity.Items.Add(newItem);
                                    }

                                    return true;
                                }
                            }
                        }
                    }
                    return false;
                }
                else
                {
                    // Fall back to our own internal storage model if no game entity exists
                    var existingItem = entity.Items.Find(i => i.Id == itemId);
                    if (existingItem != null)
                    {
                        existingItem.Quantity += quantity;
                        return true;
                    }

                    if (entity.Items.Count < entity.SlotCount)
                    {
                        var newItem = new StorageItem
                        {
                            Id = itemId,
                            Quantity = quantity
                        };

                        entity.Items.Add(newItem);
                        return true;
                    }

                    LuaUtility.LogWarning($"AddItemToStorage: No space left in entity '{entityId}'");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error adding item to storage: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets all items in a storage entity
        /// </summary>
        public Table GetStorageItems(string entityId)
        {
            try
            {
                Table itemsTable = new Table(ModCore.Instance._luaEngine);

                // First try to get items from the game storage entity
                if (_gameStorageEntities.TryGetValue(entityId, out var gameEntity))
                {
                    // Get all items from storage
                    List<ScheduleOne.ItemFramework.ItemInstance> items = gameEntity.GetAllItems();

                    int index = 1;
                    foreach (var item in items)
                    {
                        if (item != null)
                        {
                            Table itemData = new Table(ModCore.Instance._luaEngine);
                            itemData["id"] = item.ID;
                            itemData["name"] = item.Name;
                            itemData["quantity"] = item.Quantity;
                            itemData["stackLimit"] = item.StackLimit;

                            // Add quality property if available
                            if (item is ScheduleOne.ItemFramework.QualityItemInstance qualityItem)
                            {
                                itemData["quality"] = qualityItem.Quality.ToString();
                            }

                            itemsTable[index++] = itemData;
                        }
                    }

                    return itemsTable;
                }

                // Fallback to our internal storage model
                if (!_storageEntities.TryGetValue(entityId, out var entity))
                {
                    LuaUtility.LogWarning($"GetStorageItems: Entity '{entityId}' not found");
                    return itemsTable;
                }

                for (int i = 0; i < entity.Items.Count; i++)
                {
                    var item = entity.Items[i];
                    Table itemTable = new Table(ModCore.Instance._luaEngine);
                    itemTable["id"] = item.Id;
                    itemTable["quantity"] = item.Quantity;
                    itemsTable[i + 1] = itemTable;
                }

                return itemsTable;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error getting storage items: {ex.Message}", ex);
                return new Table(ModCore.Instance._luaEngine);
            }
        }

        /// <summary>
        /// Checks if a storage entity is open
        /// </summary>
        public bool IsStorageOpen(string entityId)
        {
            try
            {
                // First check the game storage entity
                if (_gameStorageEntities.TryGetValue(entityId, out var gameEntity))
                {
                    return gameEntity.IsOpened;
                }

                // Fallback to our internal model
                if (!_storageEntities.TryGetValue(entityId, out var entity))
                {
                    LuaUtility.LogWarning($"IsStorageOpen: Entity '{entityId}' not found");
                    return false;
                }

                return entity.IsOpen;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error checking if storage is open: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Sets the name of a storage entity
        /// </summary>
        public void SetStorageName(string entityId, string name)
        {
            try
            {
                // Update our internal model
                if (_storageEntities.TryGetValue(entityId, out var entity))
                {
                    entity.Name = name;
                }

                // Update the game storage entity
                if (_gameStorageEntities.TryGetValue(entityId, out var gameEntity))
                {
                    gameEntity.StorageEntityName = name;
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error setting storage name: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sets the subtitle of a storage entity
        /// </summary>
        public void SetStorageSubtitle(string entityId, string subtitle)
        {
            try
            {
                // Update our internal model
                if (_storageEntities.TryGetValue(entityId, out var entity))
                {
                    entity.Subtitle = subtitle;
                }

                // Update the game storage entity
                if (_gameStorageEntities.TryGetValue(entityId, out var gameEntity))
                {
                    gameEntity.StorageEntitySubtitle = subtitle;
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error setting storage subtitle: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Clears all items from a storage entity
        /// </summary>
        public void ClearStorageContents(string entityId)
        {
            try
            {
                // Clear our internal model
                if (_storageEntities.TryGetValue(entityId, out var entity))
                {
                    entity.Items.Clear();
                }

                // Clear the game storage entity
                if (_gameStorageEntities.TryGetValue(entityId, out var gameEntity))
                {
                    gameEntity.ClearContents();
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error clearing storage contents: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the count of storage entities
        /// </summary>
        public int GetStorageEntityCount()
        {
            return _storageEntities.Count;
        }
    }

    /// <summary>
    /// Represents a storage entity
    /// </summary>
    public class StorageEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Subtitle { get; set; }
        public int SlotCount { get; set; }
        public int RowCount { get; set; }
        public List<StorageItem> Items { get; set; }
        public bool IsOpen { get; set; }
    }

    /// <summary>
    /// Represents an item in storage
    /// </summary>
    public class StorageItem
    {
        public string Id { get; set; }
        public int Quantity { get; set; }
    }
}