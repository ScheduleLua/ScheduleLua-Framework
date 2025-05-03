using System.Collections.Generic;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using ScheduleOne.Product;
using ScheduleOne.DevUtilities;
using UnityEngine;
using ScheduleLua.API.Base;

namespace ScheduleLua.API.Core.TypeProxies
{
    [MoonSharpUserData]
    public class ProductManagerProxy : UnityTypeProxyBase<ProductManager>
    {
        private ProductManager _manager => NetworkSingleton<ProductManager>.Instance;

        public bool IsAcceptingOrders => ProductManager.IsAcceptingOrders;
        public bool MethDiscovered => ProductManager.MethDiscovered;
        public bool CocaineDiscovered => ProductManager.CocaineDiscovered;

        public ProductManagerProxy()
        {
            // No need to initialize anything as we use the NetworkSingleton<ProductManager>.Instance
        }

        public Table GetDiscoveredProducts()
        {
            var script = new Script();
            var table = new Table(script);

            int index = 1;
            foreach (var product in ProductManager.DiscoveredProducts)
            {
                table[index++] = new ProductDefinitionProxy(product);
            }

            return table;
        }

        public Table GetListedProducts()
        {
            var script = new Script();
            var table = new Table(script);

            int index = 1;
            foreach (var product in ProductManager.ListedProducts)
            {
                table[index++] = new ProductDefinitionProxy(product);
            }

            return table;
        }

        public Table GetFavouritedProducts()
        {
            var script = new Script();
            var table = new Table(script);

            int index = 1;
            foreach (var product in ProductManager.FavouritedProducts)
            {
                table[index++] = new ProductDefinitionProxy(product);
            }

            return table;
        }

        public void SetProductListed(string productId, bool listed)
        {
            _manager?.SetProductListed(productId, listed);
        }

        public void SetProductFavourited(string productId, bool favourited)
        {
            _manager?.SetProductFavourited(productId, favourited);
        }

        public void DiscoverProduct(string productId)
        {
            _manager?.DiscoverProduct(productId);
        }

        public float GetProductPrice(ProductDefinitionProxy product)
        {
            if (_manager == null || product == null)
                return 0;

            return _manager.GetPrice(product.GetInternalInstance());
        }

        public void SetProductPrice(string productId, float price)
        {
            if (_manager == null)
                return;

            // Clamp price to valid range
            price = Mathf.Clamp(price, ProductManager.MIN_PRICE, ProductManager.MAX_PRICE);
            _manager.SendPrice(productId, price);
        }

        public override string ToString()
        {
            return "ProductManager";
        }
    }
}