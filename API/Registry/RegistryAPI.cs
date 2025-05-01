using MelonLoader;
using MoonSharp.Interpreter;
using ScheduleOne;
using ScheduleOne.ConstructableScripts;
using ScheduleOne.ItemFramework;
using ScheduleOne.Growing;
using ScheduleOne.UI;
using ScheduleOne.GameTime;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ScheduleOne.PlayerScripts;
using ScheduleOne.DevUtilities;
using System.Collections;
using System.Reflection;
using ScheduleLua.API.Base;
using ScheduleLua.API.Core.TypeProxies;

namespace ScheduleLua.API.Registry
{
    /// <summary>
    /// Provides an interface to the ScheduleOne Registry system for Lua scripts
    /// </summary>
    public class RegistryAPI : BaseLuaApiModule
    {
        private static MelonLogger.Instance _logger => ModCore.Instance.LoggerInstance;
        private static bool _registryReady = false;
        private static bool _checkingRegistry = false;

        /// <summary>
        /// Registers Registry API with the Lua interpreter
        /// </summary>
        public override void RegisterAPI(Script luaEngine)
        {
            if (luaEngine == null)
                throw new ArgumentNullException(nameof(luaEngine));

            // Registry Ready Event
            luaEngine.Globals["OnRegistryReady"] = (Action<DynValue>)OnRegistryReady;
            luaEngine.Globals["IsRegistryReady"] = (Func<bool>)IsRegistryReady;

            // Item Functions
            luaEngine.Globals["GetItem"] = (Func<string, Table>)GetItem;
            luaEngine.Globals["GetItemDirect"] = (Func<string, ItemDefinition>)GetItemDirect;
            luaEngine.Globals["DoesItemExist"] = (Func<string, bool>)DoesItemExist;
            luaEngine.Globals["GetItemCategories"] = (Func<Table>)GetItemCategories;
            luaEngine.Globals["GetItemsInCategory"] = (Func<string, Table>)GetItemsInCategory;

            // New Item Management Functions
            luaEngine.Globals["CreateItem"] = (Func<string, string, string, string, int, Table>)CreateItem;
            luaEngine.Globals["CreateQualityItem"] = (Func<string, string, string, string, int, string, Table>)CreateQualityItem;
            luaEngine.Globals["CreateIntegerItem"] = (Func<string, string, string, string, int, int, Table>)CreateIntegerItem;
            luaEngine.Globals["ModifyItem"] = (Func<string, Table, bool>)ModifyItem;
            luaEngine.Globals["GetAllItems"] = (Func<Table>)GetAllItems;
            luaEngine.Globals["CreateItemInstance"] = (Func<string, int, ItemInstance>)CreateItemInstance;
            luaEngine.Globals["AddItemToPlayerInventory"] = (Func<ItemInstance, bool>)AddItemToPlayerInventory;

            // Prefab Functions
            luaEngine.Globals["GetPrefab"] = (Func<string, GameObject>)GetPrefab;
            luaEngine.Globals["DoesPrefabExist"] = (Func<string, bool>)DoesPrefabExist;

            // Constructable Functions
            luaEngine.Globals["GetConstructable"] = (Func<string, Constructable>)GetConstructable;
            luaEngine.Globals["DoesConstructableExist"] = (Func<string, bool>)DoesConstructableExist;

            // Seed Functions
            luaEngine.Globals["GetSeed"] = (Func<string, Table>)GetSeed;
            luaEngine.Globals["GetAllSeeds"] = (Func<Table>)GetAllSeeds;

            // Quality Functions
            luaEngine.Globals["GetQualityLevel"] = (Func<string, int>)GetQualityLevel;
            luaEngine.Globals["GetQualityName"] = (Func<string, string>)GetQualityName;
            luaEngine.Globals["GetAllQualities"] = (Func<Table>)GetAllQualities;

            // Register custom type handlers
            RegisterCustomTypes(luaEngine);

            // Start checking if registry is ready
            StartRegistryReadyCheck();
        }

        /// <summary>
        /// Subscribe to the registry ready event
        /// </summary>
        public static void OnRegistryReady(DynValue callback)
        {

            _logger.Msg("OnRegistryReady called - use event subscription instead of callbacks");

            // Make sure we're checking for registry readiness
            if (!_checkingRegistry)
            {
                StartRegistryReadyCheck();
            }
        }

        /// <summary>
        /// Check if the registry is ready
        /// </summary>
        public static bool IsRegistryReady()
        {
            return _registryReady;
        }

        /// <summary>
        /// Start checking if the registry is ready
        /// </summary>
        private static void StartRegistryReadyCheck()
        {
            if (_checkingRegistry)
                return;

            _checkingRegistry = true;
            MelonCoroutines.Start(CheckRegistryReady());
        }

        /// <summary>
        /// Coroutine to check if the registry is ready
        /// </summary>
        private static IEnumerator CheckRegistryReady()
        {
            // Wait until the current scene is not the menu scene
            while (SceneManager.GetActiveScene().name == "Menu" || !IsGameSceneLoaded())
            {
                yield return new WaitForSeconds(0.5f);
            }

            // Wait a bit for the registry to be fully initialized
            yield return new WaitForSeconds(1.0f);

            // Additional checks to ensure Registry is fully initialized
            while (ScheduleOne.Registry.Instance == null ||
                  TimeManager.Instance == null ||
                  !CheckRegistryInitialized())
            {
                yield return new WaitForSeconds(0.5f);
            }

            // Registry is now ready
            _registryReady = true;

            // Trigger callbacks
            ModCore.Instance.TriggerEvent("OnRegistryReady");
        }

        /// <summary>
        /// Checks if the Registry has been initialized with items
        /// </summary>
        private static bool CheckRegistryInitialized()
        {
            try
            {
                return ScheduleOne.Registry.Instance != null &&
                       ScheduleOne.Registry.GetItem("cash") != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if a game scene is loaded (not menu or loading screen)
        /// </summary>
        private static bool IsGameSceneLoaded()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            return sceneName != "Menu" &&
                   sceneName != "Loading" &&
                   sceneName != "CharacterCreation" &&
                   !string.IsNullOrEmpty(sceneName);
        }

        /// <summary>
        /// Registers custom types needed for Registry API
        /// </summary>
        private static void RegisterCustomTypes(Script luaEngine)
        {
            // Register item-related types for Lua with safe proxies
            UserData.RegisterType<ItemProxy>();
            UserData.RegisterType<ItemInstanceProxy>();
            UserData.RegisterType<EItemCategory>();
            UserData.RegisterType<EQuality>();
            UserData.RegisterType<ELegalStatus>();

            // Register conversion functions
            luaEngine.Globals["CreateItemProxy"] = (Func<ItemDefinition, ItemProxy>)CreateItemProxy;
            luaEngine.Globals["CreateItemInstanceProxy"] = (Func<ItemInstance, ItemInstanceProxy>)CreateItemInstanceProxy;
        }

        #region Item Functions

        /// <summary>
        /// Gets an item by ID and returns it as a Lua-friendly table
        /// </summary>
        public static Table GetItem(string itemId)
        {
            try
            {
                var item = ScheduleOne.Registry.GetItem(itemId);
                if (item == null)
                    return null;

                return ItemToTable(item);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting item '{itemId}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets an item by ID directly (for advanced use)
        /// </summary>
        public static ItemDefinition GetItemDirect(string itemId)
        {
            try
            {
                return ScheduleOne.Registry.GetItem(itemId);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting direct item '{itemId}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Checks if an item with the given ID exists
        /// </summary>
        public static bool DoesItemExist(string itemId)
        {
            return ScheduleOne.Registry.GetItem(itemId) != null;
        }

        /// <summary>
        /// Gets all item categories as a Lua table
        /// </summary>
        public static Table GetItemCategories()
        {
            var table = new Table(ModCore.Instance._luaEngine);

            foreach (EItemCategory category in Enum.GetValues(typeof(EItemCategory)))
            {
                table.Append(DynValue.NewString(category.ToString()));
            }

            return table;
        }

        /// <summary>
        /// Gets all items in a specific category
        /// </summary>
        public static Table GetItemsInCategory(string categoryName)
        {
            if (!Enum.TryParse(categoryName, true, out EItemCategory category))
                return new Table(ModCore.Instance._luaEngine);

            var table = new Table(ModCore.Instance._luaEngine);

            try
            {
                // Loop through all known items and filter by category
                var allItems = GetAllAvailableItems();

                int index = 1;
                foreach (var item in allItems)
                {
                    if (item.Category == category)
                    {
                        table.Set(index++, DynValue.NewTable(ItemToTable(item)));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting items in category {categoryName}: {ex.Message}");
            }

            return table;
        }

        /// <summary>
        /// Helper method to get all available items in the game
        /// </summary>
        private static List<ItemDefinition> GetAllAvailableItems()
        {
            var items = new HashSet<ItemDefinition>();
            var registry = ScheduleOne.Registry.Instance;

            if (registry == null)
            {
                _logger.Error("Registry instance is null");
                return new List<ItemDefinition>();
            }

            try
            {
                // Use reflection to access the private ItemRegistry field
                var itemRegistryField = registry.GetType().GetField("ItemRegistry",
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);

                if (itemRegistryField != null)
                {
                    // ItemRegistry is a List<ItemRegister>, not a Dictionary
                    var itemRegistry = itemRegistryField.GetValue(registry);

                    if (itemRegistry != null)
                    {
                        // Get the type of the List
                        Type listType = itemRegistry.GetType();

                        // Get Count property
                        var countProperty = listType.GetProperty("Count");
                        int count = (int)countProperty.GetValue(itemRegistry);

                        _logger.Msg($"Found {count} items in ItemRegistry");

                        // Iterate through the list
                        for (int i = 0; i < count; i++)
                        {
                            // Get item at index i
                            var indexer = listType.GetMethod("get_Item");
                            var itemRegister = indexer.Invoke(itemRegistry, new object[] { i });

                            // Get Definition property from ItemRegister
                            var itemRegisterType = itemRegister.GetType();
                            var definitionField = itemRegisterType.GetField("Definition");

                            if (definitionField != null)
                            {
                                var itemDefinition = definitionField.GetValue(itemRegister) as ItemDefinition;

                                if (itemDefinition != null)
                                {
                                    items.Add(itemDefinition);
                                }
                            }
                        }

                        _logger.Msg($"Successfully retrieved {items.Count} items from ItemRegistry via reflection");
                    }
                    else
                    {
                        _logger.Error("ItemRegistry field value is null");
                    }
                }
                else
                {
                    _logger.Error("Failed to find ItemRegistry field via reflection");

                    // Fallback method - try to get at least some known items
                    var cashItem = ScheduleOne.Registry.GetItem("cash");
                    if (cashItem != null)
                    {
                        items.Add(cashItem);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Reflection error accessing ItemRegistry: {ex.Message}");
            }

            _logger.Msg($"Found {items.Count} total items");

            return items.ToList();
        }

        /// <summary>
        /// Gets all items in the registry
        /// </summary>
        public static Table GetAllItems()
        {
            var table = new Table(ModCore.Instance._luaEngine);

            try
            {
                var allItems = GetAllAvailableItems();

                int index = 1;
                foreach (var item in allItems)
                {
                    table.Set(index++, DynValue.NewTable(ItemToTable(item)));
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting all items: {ex.Message}");
            }

            return table;
        }

        /// <summary>
        /// Creates a new basic item definition
        /// </summary>
        public static Table CreateItem(string id, string name, string description, string category, int stackLimit)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.Error("Cannot create item with empty ID");
                    return null;
                }

                if (DoesItemExist(id))
                {
                    _logger.Error($"Item with ID '{id}' already exists");
                    return null;
                }

                if (!Enum.TryParse(category, true, out EItemCategory itemCategory))
                {
                    _logger.Error($"Invalid item category: {category}");
                    return null;
                }

                var item = ScriptableObject.CreateInstance<ItemDefinition>();
                item.ID = id;
                item.Name = name;
                item.Description = description;
                item.Category = itemCategory;
                item.StackLimit = stackLimit;
                item.AvailableInDemo = true;

                // Register the item with the game registry
                ScheduleOne.Registry.Instance.AddToRegistry(item);

                _logger.Msg($"Created new item '{id}'");
                return ItemToTable(item);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error creating item: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a new quality item definition
        /// </summary>
        public static Table CreateQualityItem(string id, string name, string description, string category, int stackLimit, string defaultQuality)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.Error("Cannot create quality item with empty ID");
                    return null;
                }

                if (DoesItemExist(id))
                {
                    _logger.Error($"Item with ID '{id}' already exists");
                    return null;
                }

                if (!Enum.TryParse(category, true, out EItemCategory itemCategory))
                {
                    _logger.Error($"Invalid item category: {category}");
                    return null;
                }

                if (!Enum.TryParse(defaultQuality, true, out EQuality quality))
                {
                    _logger.Error($"Invalid quality: {defaultQuality}");
                    return null;
                }

                var item = ScriptableObject.CreateInstance<QualityItemDefinition>();
                item.ID = id;
                item.Name = name;
                item.Description = description;
                item.Category = itemCategory;
                item.StackLimit = stackLimit;
                item.AvailableInDemo = true;
                item.DefaultQuality = quality;

                // Register the item with the game registry
                ScheduleOne.Registry.Instance.AddToRegistry(item);

                _logger.Msg($"Created new quality item '{id}'");
                return ItemToTable(item);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error creating quality item: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a new integer item definition
        /// </summary>
        public static Table CreateIntegerItem(string id, string name, string description, string category, int stackLimit, int defaultValue)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.Error("Cannot create integer item with empty ID");
                    return null;
                }

                if (DoesItemExist(id))
                {
                    _logger.Error($"Item with ID '{id}' already exists");
                    return null;
                }

                if (!Enum.TryParse(category, true, out EItemCategory itemCategory))
                {
                    _logger.Error($"Invalid item category: {category}");
                    return null;
                }

                var item = ScriptableObject.CreateInstance<IntegerItemDefinition>();
                item.ID = id;
                item.Name = name;
                item.Description = description;
                item.Category = itemCategory;
                item.StackLimit = stackLimit;
                item.AvailableInDemo = true;
                item.DefaultValue = defaultValue;

                ScheduleOne.Registry.Instance.AddToRegistry(item);

                _logger.Msg($"Created new integer item '{id}'");
                return ItemToTable(item);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error creating integer item: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Modifies an existing item definition
        /// </summary>
        public static bool ModifyItem(string itemId, Table properties)
        {
            try
            {
                var item = ScheduleOne.Registry.GetItem(itemId);
                if (item == null)
                {
                    _logger.Error($"Item '{itemId}' not found");
                    return false;
                }

                // Process each property
                foreach (var pair in properties.Pairs)
                {
                    string propertyName = pair.Key.String;
                    DynValue propertyValue = pair.Value;

                    switch (propertyName.ToLower())
                    {
                        case "name":
                            item.Name = propertyValue.String;
                            break;
                        case "description":
                            item.Description = propertyValue.String;
                            break;
                        case "stacklimit":
                            item.StackLimit = (int)propertyValue.Number;
                            break;
                        case "availableindemo":
                            item.AvailableInDemo = propertyValue.Boolean;
                            break;
                        case "keywords":
                            if (propertyValue.Type == DataType.Table)
                            {
                                var keywordsTable = propertyValue.Table;
                                var keywords = new List<string>();
                                foreach (var kv in keywordsTable.Pairs)
                                {
                                    if (kv.Value.Type == DataType.String)
                                    {
                                        keywords.Add(kv.Value.String);
                                    }
                                }
                                item.Keywords = keywords.ToArray();
                            }
                            break;
                        case "legalstatus":
                            if (Enum.TryParse(propertyValue.String, true, out ELegalStatus legalStatus))
                            {
                                item.legalStatus = legalStatus;
                            }
                            break;
                        case "category":
                            if (Enum.TryParse(propertyValue.String, true, out EItemCategory category))
                            {
                                item.Category = category;
                            }
                            break;
                        case "defaultquality":
                            if (item is QualityItemDefinition qualityItem &&
                                Enum.TryParse(propertyValue.String, true, out EQuality quality))
                            {
                                qualityItem.DefaultQuality = quality;
                            }
                            break;
                        case "defaultvalue":
                            if (item is IntegerItemDefinition intItem &&
                                propertyValue.Type == DataType.Number)
                            {
                                intItem.DefaultValue = (int)propertyValue.Number;
                            }
                            break;
                        default:
                            _logger.Warning($"Unknown property '{propertyName}' for item");
                            break;
                    }
                }

                _logger.Msg($"Modified item '{itemId}'");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error modifying item '{itemId}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates an item instance from an item definition
        /// </summary>
        public static ItemInstance CreateItemInstance(string itemId, int quantity)
        {
            try
            {
                var itemDef = ScheduleOne.Registry.GetItem(itemId);
                if (itemDef == null)
                {
                    _logger.Error($"Item '{itemId}' not found");
                    return null;
                }

                var instance = itemDef.GetDefaultInstance();
                if (instance == null)
                {
                    _logger.Error($"Failed to create instance of item '{itemId}'");
                    return null;
                }

                if (quantity > 0)
                {
                    instance.ChangeQuantity(quantity - 1); // -1 because default is already 1
                }

                return instance;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error creating item instance for '{itemId}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Adds an item instance to the player's inventory
        /// </summary>
        public static bool AddItemToPlayerInventory(ItemInstance itemInstance)
        {
            try
            {
                if (itemInstance == null)
                {
                    _logger.Error("Cannot add null item to inventory");
                    return false;
                }

                var playerInventory = PlayerSingleton<PlayerInventory>.Instance;
                if (playerInventory == null)
                {
                    _logger.Error("Player inventory not available");
                    return false;
                }

                // Note: AddItemToInventory returns void, so we need to assume success if no exception occurs
                playerInventory.AddItemToInventory(itemInstance);
                _logger.Msg($"Added {itemInstance.Definition.Name} x{itemInstance.Quantity} to player inventory");

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error adding item to inventory: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Prefab Functions

        /// <summary>
        /// Gets a prefab by ID
        /// </summary>
        public static GameObject GetPrefab(string prefabId)
        {
            try
            {
                return ScheduleOne.Registry.GetPrefab(prefabId);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting prefab '{prefabId}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Checks if a prefab with the given ID exists
        /// </summary>
        public static bool DoesPrefabExist(string prefabId)
        {
            return ScheduleOne.Registry.GetPrefab(prefabId) != null;
        }

        #endregion

        #region Constructable Functions

        /// <summary>
        /// Gets a constructable by ID
        /// </summary>
        public static Constructable GetConstructable(string constructableId)
        {
            try
            {
                return ScheduleOne.Registry.GetConstructable(constructableId);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting constructable '{constructableId}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Checks if a constructable with the given ID exists
        /// </summary>
        public static bool DoesConstructableExist(string constructableId)
        {
            return ScheduleOne.Registry.GetConstructable(constructableId) != null;
        }

        #endregion

        #region Seed Functions

        /// <summary>
        /// Gets a seed by ID
        /// </summary>
        public static Table GetSeed(string seedId)
        {
            try
            {
                var seeds = ScheduleOne.Registry.Instance?.Seeds;
                if (seeds == null)
                    return null;

                var seed = seeds.Find(s => s != null && s.ID == seedId);
                if (seed == null)
                    return null;

                return SeedToTable(seed);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting seed '{seedId}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets all seeds in the registry
        /// </summary>
        public static Table GetAllSeeds()
        {
            var table = new Table(ModCore.Instance._luaEngine);

            try
            {
                var seeds = ScheduleOne.Registry.Instance?.Seeds;
                if (seeds == null)
                    return table;

                int index = 1;
                foreach (var seed in seeds)
                {
                    if (seed != null)
                    {
                        table.Set(index++, DynValue.NewTable(SeedToTable(seed)));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting all seeds: {ex.Message}");
            }

            return table;
        }

        #endregion

        #region Quality Functions

        /// <summary>
        /// Gets a quality level by name
        /// </summary>
        public static int GetQualityLevel(string qualityName)
        {
            if (Enum.TryParse(qualityName, true, out EQuality quality))
                return (int)quality;

            return 0;
        }

        /// <summary>
        /// Gets a quality name by level
        /// </summary>
        public static string GetQualityName(string qualityLevelStr)
        {
            if (int.TryParse(qualityLevelStr, out int qualityLevel) &&
                Enum.IsDefined(typeof(EQuality), qualityLevel))
            {
                return ((EQuality)qualityLevel).ToString();
            }

            return "Unknown";
        }

        /// <summary>
        /// Gets all quality levels as a Lua table
        /// </summary>
        public static Table GetAllQualities()
        {
            var table = new Table(ModCore.Instance._luaEngine);

            foreach (EQuality quality in Enum.GetValues(typeof(EQuality)))
            {
                var qualityTable = new Table(ModCore.Instance._luaEngine);
                qualityTable.Set("name", DynValue.NewString(quality.ToString()));
                qualityTable.Set("level", DynValue.NewNumber((int)quality));

                table.Append(DynValue.NewTable(qualityTable));
            }

            return table;
        }

        #endregion

        #region Helper Functions

        /// <summary>
        /// Converts an ItemDefinition to a Lua table
        /// </summary>
        private static Table ItemToTable(ItemDefinition item)
        {
            if (item == null)
                return null;

            var table = new Table(ModCore.Instance._luaEngine);
            table.Set("id", DynValue.NewString(item.ID));
            table.Set("name", DynValue.NewString(item.Name));
            table.Set("description", DynValue.NewString(item.Description));
            table.Set("category", DynValue.NewString(item.Category.ToString()));
            table.Set("stackLimit", DynValue.NewNumber(item.StackLimit));
            table.Set("availableInDemo", DynValue.NewBoolean(item.AvailableInDemo));
            table.Set("legalStatus", DynValue.NewString(item.legalStatus.ToString()));

            // Convert keywords array to table
            if (item.Keywords != null && item.Keywords.Length > 0)
            {
                var keywordsTable = new Table(ModCore.Instance._luaEngine);
                for (int i = 0; i < item.Keywords.Length; i++)
                {
                    keywordsTable.Set(i + 1, DynValue.NewString(item.Keywords[i]));
                }
                table.Set("keywords", DynValue.NewTable(keywordsTable));
            }

            // Add special properties for different item types
            if (item is QualityItemDefinition qualityItem)
            {
                table.Set("isQualityItem", DynValue.NewBoolean(true));
                table.Set("defaultQuality", DynValue.NewString(qualityItem.DefaultQuality.ToString()));
            }
            else if (item is IntegerItemDefinition intItem)
            {
                table.Set("isIntegerItem", DynValue.NewBoolean(true));
                table.Set("defaultValue", DynValue.NewNumber(intItem.DefaultValue));
            }
            else if (item is StorableItemDefinition storableItem)
            {
                table.Set("isStorableItem", DynValue.NewBoolean(true));
                AddStorableProperties(table, storableItem);
            }

            return table;
        }

        /// <summary>
        /// Converts a SeedDefinition to a Lua table
        /// </summary>
        private static Table SeedToTable(SeedDefinition seed)
        {
            if (seed == null)
                return null;

            var table = new Table(ModCore.Instance._luaEngine);
            table.Set("id", DynValue.NewString(seed.ID));
            table.Set("name", DynValue.NewString(seed.Name));
            // Add more seed properties as needed

            return table;
        }

        /// <summary>
        /// Converts a StorableItemDefinition to include price and required rank information
        /// </summary>
        private static void AddStorableProperties(Table table, StorableItemDefinition item)
        {
            if (item == null)
                return;

            table.Set("basePurchasePrice", DynValue.NewNumber(item.BasePurchasePrice));

            // Handle the FullRank conversion
            if (item.RequiredRank != null)
            {
                // Create a table to represent the FullRank
                var rankTable = new Table(ModCore.Instance._luaEngine);
                rankTable.Set("rank", DynValue.NewString(item.RequiredRank.Rank.ToString()));
                rankTable.Set("tier", DynValue.NewNumber(item.RequiredRank.Tier));
                table.Set("requiredRank", DynValue.NewTable(rankTable));

                // Also provide a simplified numeric representation for easier sorting
                table.Set("requiredRankValue", DynValue.NewNumber((int)item.RequiredRank.Rank * 10 + item.RequiredRank.Tier));
            }

            table.Set("isPurchasable", DynValue.NewBoolean(item.IsPurchasable));
        }

        /// <summary>
        /// Creates an ItemProxy from an ItemDefinition
        /// </summary>
        public static ItemProxy CreateItemProxy(ItemDefinition item)
        {
            return new ItemProxy(item);
        }

        /// <summary>
        /// Creates an ItemInstanceProxy from an ItemInstance
        /// </summary>
        public static ItemInstanceProxy CreateItemInstanceProxy(ItemInstance instance)
        {
            return new ItemInstanceProxy(instance);
        }

        #endregion
    }
}