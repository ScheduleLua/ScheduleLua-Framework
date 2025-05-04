using MoonSharp.Interpreter;
using ScheduleOne.ItemFramework;
using ScheduleOne.Product;

namespace ScheduleLua.API.Core.TypeProxies
{
    [MoonSharpUserData]
    public class ProductItemInstanceProxy : ItemInstanceProxy
    {
        private ProductItemInstance _productInstance;

        public string PackagingID => _productInstance?.PackagingID;
        public PackagingDefinitionProxy AppliedPackaging => _productInstance?.AppliedPackaging != null
            ? new PackagingDefinitionProxy(_productInstance.AppliedPackaging)
            : null;
        public int Amount => _productInstance?.Amount ?? 0;
        public ProductDefinitionProxy ProductDefinition => _productInstance?.Definition != null
            ? new ProductDefinitionProxy(_productInstance.Definition as ProductDefinition)
            : null;

        public ProductItemInstanceProxy(ProductItemInstance instance) : base(instance)
        {
            _productInstance = instance;
        }

        public void SetPackaging(PackagingDefinitionProxy packagingProxy)
        {
            if (_productInstance != null && packagingProxy != null)
            {
                var packaging = packagingProxy.GetInternalInstance();
                _productInstance.SetPackaging(packaging);
            }
        }

        public void RemovePackaging()
        {
            if (_productInstance != null)
            {
                _productInstance.SetPackaging(null);
            }
        }

        public float GetAddictiveness()
        {
            return _productInstance?.GetAddictiveness() ?? 0;
        }

        public float GetSimilarity(ProductDefinitionProxy otherProduct, string quality)
        {
            if (_productInstance == null || otherProduct == null)
                return 0;

            if (Enum.TryParse<EQuality>(quality, out var qualityEnum))
            {
                return _productInstance.GetSimilarity(otherProduct.GetInternalInstance(), qualityEnum);
            }

            return 0;
        }

        public float GetMonetaryValue()
        {
            return _productInstance?.GetMonetaryValue() ?? 0;
        }

        public override string ToString()
        {
            return $"ProductInstance: {Name} (Qty: {Quantity}, Quality: {GetQuality()}, Packaging: {(_productInstance?.AppliedPackaging != null ? _productInstance.AppliedPackaging.Name : "None")})";
        }
    }
}