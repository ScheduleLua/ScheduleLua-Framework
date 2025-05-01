using MoonSharp.Interpreter;
using ScheduleLua.API.Base;
using ScheduleOne.ItemFramework;

namespace ScheduleLua.API.Core.TypeProxies
{
    /// <summary>
    /// A Lua-friendly proxy for ItemDefinition to prevent IL2CPP/AOT issues
    /// </summary>
    [MoonSharpUserData]
    public class ItemProxy : UnityTypeProxyBase<ItemDefinition>
    {
        private ItemDefinition _item;

        public string ID => _item?.ID;
        public string Name { get => _item?.Name; set { if (_item != null) _item.Name = value; } }
        public string Description { get => _item?.Description; set { if (_item != null) _item.Description = value; } }
        public int StackLimit { get => _item?.StackLimit ?? 0; set { if (_item != null) _item.StackLimit = value; } }
        public EItemCategory Category => _item?.Category ?? default;
        public bool AvailableInDemo { get => _item?.AvailableInDemo ?? false; set { if (_item != null) _item.AvailableInDemo = value; } }
        public ELegalStatus LegalStatus { get => _item?.legalStatus ?? default; set { if (_item != null) _item.legalStatus = value; } }

        public ItemProxy(ItemDefinition item)
        {
            _item = item;
        }

        public string[] GetKeywords()
        {
            return _item?.Keywords ?? new string[0];
        }

        public void SetKeywords(Table keywordsTable)
        {
            if (_item == null || keywordsTable == null)
                return;

            var keywords = new List<string>();
            foreach (var pair in keywordsTable.Pairs)
            {
                if (pair.Value.Type == DataType.String)
                {
                    keywords.Add(pair.Value.String);
                }
            }

            _item.Keywords = keywords.ToArray();
        }

        public ItemInstance CreateInstance(int quantity = 1)
        {
            if (_item == null)
                return null;

            var instance = _item.GetDefaultInstance();
            if (instance != null && quantity > 1)
            {
                instance.ChangeQuantity(quantity - 1); // -1 because default is already 1
            }

            return instance;
        }

        public override string ToString()
        {
            return $"Item[{ID}]: {Name}";
        }
    }
}
