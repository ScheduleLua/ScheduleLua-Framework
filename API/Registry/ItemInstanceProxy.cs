using MoonSharp.Interpreter;
using ScheduleOne.ItemFramework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScheduleLua.API.Registry
{
    /// <summary>
    /// A Lua-friendly proxy for ItemInstance to prevent IL2CPP/AOT issues
    /// </summary>
    [MoonSharpUserData]
    public class ItemInstanceProxy
    {
        private ItemInstance _instance;

        public string Name => _instance?.Definition?.Name;
        public string Description => _instance?.Definition?.Description;
        public int Quantity { get => _instance?.Quantity ?? 0; set { if (_instance != null) _instance.ChangeQuantity(value - _instance.Quantity); } }
        public ItemDefinition Definition => _instance?.Definition;

        public ItemInstanceProxy(ItemInstance instance)
        {
            _instance = instance;
        }

        public void ChangeQuantity(int delta)
        {
            _instance?.ChangeQuantity(delta);
        }

        public ItemInstanceProxy Copy(int quantity = -1)
        {
            if (_instance == null)
                return null;

            var copy = _instance.GetCopy(quantity >= 0 ? quantity : _instance.Quantity);
            return new ItemInstanceProxy(copy);
        }

        public bool IsQualityItem()
        {
            return _instance is QualityItemInstance;
        }

        public string GetQuality()
        {
            if (_instance is QualityItemInstance qualityInstance)
            {
                return qualityInstance.Quality.ToString();
            }
            return "None";
        }

        public void SetQuality(string qualityName)
        {
            if (_instance is QualityItemInstance qualityInstance &&
                Enum.TryParse(qualityName, true, out EQuality quality))
            {
                qualityInstance.Quality = quality;
            }
        }

        public bool IsIntegerItem()
        {
            return _instance is IntegerItemInstance;
        }

        public int GetValue()
        {
            if (_instance is IntegerItemInstance intInstance)
            {
                return intInstance.Value;
            }
            return 0;
        }

        public void SetValue(int value)
        {
            if (_instance is IntegerItemInstance intInstance)
            {
                intInstance.Value = value;
            }
        }

        public bool IsCashItem()
        {
            return _instance is CashInstance;
        }

        public float GetBalance()
        {
            if (_instance is CashInstance cashInstance)
            {
                return cashInstance.Balance;
            }
            return 0f;
        }

        public void SetBalance(float balance)
        {
            if (_instance is CashInstance cashInstance)
            {
                cashInstance.SetBalance(balance);
            }
        }

        public void ChangeBalance(float delta)
        {
            if (_instance is CashInstance cashInstance)
            {
                cashInstance.ChangeBalance(delta);
            }
        }

        public override string ToString()
        {
            return $"{Name} x{Quantity}";
        }
    }
}
