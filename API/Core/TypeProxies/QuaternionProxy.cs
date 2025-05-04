using MoonSharp.Interpreter;
using ScheduleLua.API.Base;
using UnityEngine;

namespace ScheduleLua.API.Core.TypeProxies
{
    /// <summary>
    /// Proxy class for Quaternion to avoid exposing struct directly
    /// </summary>
    [MoonSharpUserData]
    public class QuaternionProxy : UnityTypeProxyBase<Quaternion>
    {
        private Quaternion _quat;

        public QuaternionProxy(float x, float y, float z, float w)
        {
            _quat = new Quaternion(x, y, z, w);
        }

        public QuaternionProxy(Quaternion quat)
        {
            _quat = quat;
        }

        public float x { get { return _quat.x; } set { _quat.x = value; } }
        public float y { get { return _quat.y; } set { _quat.y = value; } }
        public float z { get { return _quat.z; } set { _quat.z = value; } }
        public float w { get { return _quat.w; } set { _quat.w = value; } }

        public static QuaternionProxy identity => new QuaternionProxy(Quaternion.identity);

        public static QuaternionProxy Euler(float x, float y, float z) =>
            new QuaternionProxy(Quaternion.Euler(x, y, z));

        public static QuaternionProxy LookRotation(Vector3Proxy forward, Vector3Proxy upwards = null) =>
            new QuaternionProxy(Quaternion.LookRotation(forward, upwards ?? Vector3Proxy.up));

        public static implicit operator Quaternion(QuaternionProxy proxy) => proxy._quat;
        public static implicit operator QuaternionProxy(Quaternion quat) => new QuaternionProxy(quat);

        public override string ToString() => $"({x}, {y}, {z}, {w})";
    }
}
