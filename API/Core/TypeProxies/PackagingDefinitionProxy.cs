using MoonSharp.Interpreter;
using ScheduleOne.Product.Packaging;

namespace ScheduleLua.API.Core.TypeProxies
{
    [MoonSharpUserData]
    public class PackagingDefinitionProxy : ItemProxy
    {
        private PackagingDefinition _packaging;

        public int Quantity => _packaging?.Quantity ?? 0;

        public PackagingDefinitionProxy(PackagingDefinition packaging) : base(packaging)
        {
            _packaging = packaging;
        }

        public PackagingDefinition GetInternalInstance()
        {
            return _packaging;
        }

        public override string ToString()
        {
            return $"Packaging: {Name} ({ID}, Quantity: {Quantity})";
        }
    }
}