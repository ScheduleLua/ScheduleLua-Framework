using MelonLoader;
using MoonSharp.Interpreter;
using ScheduleOne;
using ScheduleOne.ConstructableScripts;
using ScheduleOne.ItemFramework;
using ScheduleOne.Growing;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScheduleLua.API.Registry
{
    /// <summary>
    /// Provides an interface to the ScheduleOne Registry system for Lua scripts
    /// </summary>
    public static class RegistryAPI
    {
        private static MelonLogger.Instance _logger => ScheduleLua.Core.Instance.LoggerInstance;

        /// <summary>
        /// Registers Registry API with the Lua interpreter
        /// </summary>
        public static void RegisterAPI(Script luaEngine)
        {
            if (luaEngine == null)
                throw new ArgumentNullException(nameof(luaEngine));

            // Item Functions
            luaEngine.Globals["GetItem"] = (Func<string, Table>)GetItem;
            luaEngine.Globals["GetItemDirect"] = (Func<string, ItemDefinition>)GetItemDirect;
            luaEngine.Globals["DoesItemExist"] = (Func<string, bool>)DoesItemExist;
            luaEngine.Globals["GetItemCategories"] = (Func<Table>)GetItemCategories;
            luaEngine.Globals["GetItemsInCategory"] = (Func<string, Table>)GetItemsInCategory;
            
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
            
            _logger.Msg("Registry API registered with Lua engine");
        }
        
        /// <summary>
        /// Registers custom types needed for Registry API
        /// </summary>
        private static void RegisterCustomTypes(Script luaEngine)
        {
            // Register item-related enum types for Lua with safe proxies
            UserData.RegisterType<ItemProxy>();
            UserData.RegisterType<EItemCategory>();
            
            // Register conversion function for ItemDefinition -> ItemProxy
            luaEngine.Globals["CreateItemProxy"] = (Func<ItemDefinition, ItemProxy>)CreateItemProxy;
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
            var table = new Table(ScheduleLua.Core.Instance._luaEngine);
            
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
                return new Table(ScheduleLua.Core.Instance._luaEngine);
                
            var table = new Table(ScheduleLua.Core.Instance._luaEngine);
            
            // Note: This is a simplified implementation
            // A comprehensive implementation would need to scan all items in the registry
            // But that would require internal Registry access which may not be available
            
            _logger.Warning("GetItemsInCategory functionality is limited - returning empty table");
            
            return table;
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
            var table = new Table(ScheduleLua.Core.Instance._luaEngine);
            
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
            var table = new Table(ScheduleLua.Core.Instance._luaEngine);
            
            foreach (EQuality quality in Enum.GetValues(typeof(EQuality)))
            {
                var qualityTable = new Table(ScheduleLua.Core.Instance._luaEngine);
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
                
            var table = new Table(ScheduleLua.Core.Instance._luaEngine);
            table.Set("id", DynValue.NewString(item.ID));
            table.Set("name", DynValue.NewString(item.Name));
            table.Set("description", DynValue.NewString(item.Description));
            table.Set("category", DynValue.NewString(item.Category.ToString()));
            table.Set("stackLimit", DynValue.NewNumber(item.StackLimit));
            table.Set("availableInDemo", DynValue.NewBoolean(item.AvailableInDemo));
            
            // Convert keywords array to table
            if (item.Keywords != null && item.Keywords.Length > 0)
            {
                var keywordsTable = new Table(ScheduleLua.Core.Instance._luaEngine);
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
            }
            else if (item is IntegerItemDefinition intItem)
            {
                table.Set("isIntegerItem", DynValue.NewBoolean(true));
            }
            else if (item is StorableItemDefinition storableItem)
            {
                table.Set("isStorableItem", DynValue.NewBoolean(true));
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
                
            var table = new Table(ScheduleLua.Core.Instance._luaEngine);
            table.Set("id", DynValue.NewString(seed.ID));
            table.Set("name", DynValue.NewString(seed.Name));
            // Add more seed properties as needed
            
            return table;
        }
        
        /// <summary>
        /// Creates an ItemProxy from an ItemDefinition
        /// </summary>
        public static ItemProxy CreateItemProxy(ItemDefinition item)
        {
            return new ItemProxy(item);
        }
        
        #endregion
    }
    
    /// <summary>
    /// A Lua-friendly proxy for ItemDefinition to prevent IL2CPP/AOT issues
    /// </summary>
    [MoonSharpUserData]
    public class ItemProxy
    {
        private ItemDefinition _item;
        
        public string ID => _item?.ID;
        public string Name => _item?.Name;
        public string Description => _item?.Description;
        public int StackLimit => _item?.StackLimit ?? 0;
        public EItemCategory Category => _item?.Category ?? default(EItemCategory);
        
        public ItemProxy(ItemDefinition item)
        {
            _item = item;
        }
        
        public override string ToString()
        {
            return $"Item[{ID}]: {Name}";
        }
    }
} 