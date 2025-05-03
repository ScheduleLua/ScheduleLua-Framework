using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using ScheduleLua.API.Base;
using ScheduleOne.ItemFramework;
using ScheduleOne.Product;
using ScheduleOne.Properties;
using UnityEngine;

namespace ScheduleLua.API.Core.TypeProxies
{
    [MoonSharpUserData]
    public class ProductDefinitionProxy : UnityTypeProxyBase<ProductDefinition>
    {
        private ProductDefinition _product;

        public string ID => _product?.ID;
        public string Name { get => _product?.Name; set { if (_product != null) _product.Name = value; } }
        public string Description { get => _product?.Description; set { if (_product != null) _product.Description = value; } }
        public EDrugType DrugType => _product?.DrugType ?? default;
        public float BasePrice { get => _product?.BasePrice ?? 0; set { if (_product != null) _product.BasePrice = value; } }
        public float MarketValue { get => _product?.MarketValue ?? 0; set { if (_product != null) _product.MarketValue = value; } }
        public float Price => _product?.Price ?? 0;
        public int EffectsDuration { get => _product?.EffectsDuration ?? 0; set { if (_product != null) _product.EffectsDuration = value; } }
        public float LawIntensityChange { get => _product?.LawIntensityChange ?? 0; set { if (_product != null) _product.LawIntensityChange = value; } }

        public ProductDefinitionProxy(ProductDefinition product)
        {
            _product = product;
        }

        public ProductDefinition GetInternalInstance()
        {
            return _product;
        }

        public float GetAddictiveness()
        {
            return _product?.GetAddictiveness() ?? 0;
        }

        public bool HasProperty(string propertyName)
        {
            if (_product == null || string.IsNullOrEmpty(propertyName))
                return false;

            // Try to parse the property name to EProperty enum
            if (Enum.TryParse<EProperty>(propertyName, out var propertyEnum))
            {
                // Iterate through properties and check for a match by comparing with their ID
                foreach (var prop in _product.Properties)
                {
                    // Convert enum to string and compare with property ID (which is lowercase)
                    if (prop.ID.Equals(propertyName.ToLower(), StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        public ProductItemInstanceProxy CreateInstance(int quantity = 1, string quality = "Normal")
        {
            if (_product == null)
                return null;

            // Parse quality string to enum
            if (Enum.TryParse<EQuality>(quality, out var qualityEnum))
            {
                var instance = new ProductItemInstance(_product, quantity, qualityEnum);
                return new ProductItemInstanceProxy(instance);
            }

            return null;
        }

        public override string ToString()
        {
            return $"ProductDefinition: {Name} ({ID})";
        }
    }
}