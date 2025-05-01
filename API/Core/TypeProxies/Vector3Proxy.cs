using MoonSharp.Interpreter;
using ScheduleLua.API.Base;
using UnityEngine;

namespace ScheduleLua.API.Core.TypeProxies
{
    /// <summary>
    /// Proxy class for Vector3 to avoid exposing struct directly
    /// This helps with IL2CPP/AOT compatibility
    /// </summary>
    [MoonSharpUserData]
    public class Vector3Proxy : UnityTypeProxyBase<Vector3>
    {
        public Vector3Proxy(float x, float y, float z)
        {
            _value = new Vector3(x, y, z);
        }

        public Vector3Proxy(Vector3 vector)
        {
            _value = vector;
        }

        public float x { get { return _value.x; } set { _value.x = value; } }
        public float y { get { return _value.y; } set { _value.y = value; } }
        public float z { get { return _value.z; } set { _value.z = value; } }

        public static Vector3Proxy zero => new Vector3Proxy(Vector3.zero);
        public static Vector3Proxy one => new Vector3Proxy(Vector3.one);
        public static Vector3Proxy up => new Vector3Proxy(Vector3.up);
        public static Vector3Proxy down => new Vector3Proxy(Vector3.down);
        public static Vector3Proxy left => new Vector3Proxy(Vector3.left);
        public static Vector3Proxy right => new Vector3Proxy(Vector3.right);
        public static Vector3Proxy forward => new Vector3Proxy(Vector3.forward);
        public static Vector3Proxy back => new Vector3Proxy(Vector3.back);

        public float magnitude => _value.magnitude;
        public float sqrMagnitude => _value.sqrMagnitude;
        public Vector3Proxy normalized => new Vector3Proxy(_value.normalized);

        public static Vector3Proxy operator +(Vector3Proxy a, Vector3Proxy b) =>
            new Vector3Proxy(a._value + b._value);

        public static Vector3Proxy operator -(Vector3Proxy a, Vector3Proxy b) =>
            new Vector3Proxy(a._value - b._value);

        public static Vector3Proxy operator *(Vector3Proxy a, float d) =>
            new Vector3Proxy(a._value * d);

        public static Vector3Proxy operator /(Vector3Proxy a, float d) =>
            new Vector3Proxy(a._value / d);

        public static float Distance(Vector3Proxy a, Vector3Proxy b) =>
            Vector3.Distance(a._value, b._value);

        public static Vector3Proxy Lerp(Vector3Proxy a, Vector3Proxy b, float t) =>
            new Vector3Proxy(Vector3.Lerp(a._value, b._value, t));

        public static implicit operator Vector3(Vector3Proxy proxy) => proxy._value;
        public static implicit operator Vector3Proxy(Vector3 vector) => new Vector3Proxy(vector);

        public override string ToString() => $"({x}, {y}, {z})";
    }
}