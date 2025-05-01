namespace ScheduleLua.API.Base
{
    public interface IUnityTypeProxy<T>
    {
        T UnderlyingValue { get; }
    }
}