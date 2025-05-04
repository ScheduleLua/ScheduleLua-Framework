namespace ScheduleLua.API.Base
{
    public abstract class UnityTypeProxyBase<T> : IUnityTypeProxy<T>
    {
        protected T _value;
        public T UnderlyingValue => _value;

        public override string ToString() => _value.ToString();
    }
}