using System;
using UnityEngine;
using MoonSharp.Interpreter;
using ScheduleOne.NPCs;
using ScheduleLua.API.NPC;

namespace ScheduleLua.API.Core
{
    /// <summary>
    /// Provides proxy classes for Unity types to ensure compatibility with IL2CPP/AOT platforms
    /// </summary>
    public static class UnityTypeProxies
    {
        /// <summary>
        /// Initialize all Unity type proxies
        /// </summary>
        public static void Initialize()
        {
            // Register all proxy types
            UserData.RegisterType<Vector3Proxy>();
            UserData.RegisterType<QuaternionProxy>();
            UserData.RegisterType<ColorProxy>();
            UserData.RegisterType<RaycastHitProxy>();
            UserData.RegisterType<NPCProxy>();

            // Perform AOT-friendly type pre-registrations
            // This ensures types are known to the IL2CPP compiler
            PreregisterConversions();
        }

        /// <summary>
        /// Registers type conversions to help IL2CPP/AOT compatibility
        /// </summary>
        private static void PreregisterConversions()
        {
            // Pre-register conversions the runtime might need
            // These calls tell the AOT compiler to prepare these conversions
            Script dummy = new Script();

            // Vector3 conversions
            dummy.Globals["v3_conversion_test"] = new Vector3Proxy(1, 2, 3);

            // Color conversions
            dummy.Globals["color_conversion_test"] = new ColorProxy(1, 1, 1, 1);

            // Quaternion conversions  
            dummy.Globals["quat_conversion_test"] = new QuaternionProxy(0, 0, 0, 1);

            // RaycastHit conversions
            dummy.Globals["raycast_conversion_test"] = new RaycastHitProxy();

            // NPC conversions (dummy NPC, will be null in the proxy)
            dummy.Globals["npc_conversion_test"] = new NPCProxy(null);
        }
    }

    /// <summary>
    /// Proxy class for Vector3 to avoid exposing struct directly
    /// This helps with IL2CPP/AOT compatibility
    /// </summary>
    [MoonSharpUserData]
    public class Vector3Proxy
    {
        private Vector3 _vector;

        public Vector3Proxy(float x, float y, float z)
        {
            _vector = new Vector3(x, y, z);
        }

        public Vector3Proxy(Vector3 vector)
        {
            _vector = vector;
        }

        public float x { get { return _vector.x; } set { _vector.x = value; } }
        public float y { get { return _vector.y; } set { _vector.y = value; } }
        public float z { get { return _vector.z; } set { _vector.z = value; } }

        public static Vector3Proxy zero => new Vector3Proxy(Vector3.zero);
        public static Vector3Proxy one => new Vector3Proxy(Vector3.one);
        public static Vector3Proxy up => new Vector3Proxy(Vector3.up);
        public static Vector3Proxy down => new Vector3Proxy(Vector3.down);
        public static Vector3Proxy left => new Vector3Proxy(Vector3.left);
        public static Vector3Proxy right => new Vector3Proxy(Vector3.right);
        public static Vector3Proxy forward => new Vector3Proxy(Vector3.forward);
        public static Vector3Proxy back => new Vector3Proxy(Vector3.back);

        public float magnitude => _vector.magnitude;
        public float sqrMagnitude => _vector.sqrMagnitude;
        public Vector3Proxy normalized => new Vector3Proxy(_vector.normalized);

        public static Vector3Proxy operator +(Vector3Proxy a, Vector3Proxy b) =>
            new Vector3Proxy(a._vector + b._vector);

        public static Vector3Proxy operator -(Vector3Proxy a, Vector3Proxy b) =>
            new Vector3Proxy(a._vector - b._vector);

        public static Vector3Proxy operator *(Vector3Proxy a, float d) =>
            new Vector3Proxy(a._vector * d);

        public static Vector3Proxy operator /(Vector3Proxy a, float d) =>
            new Vector3Proxy(a._vector / d);

        public static float Distance(Vector3Proxy a, Vector3Proxy b) =>
            Vector3.Distance(a._vector, b._vector);

        public static Vector3Proxy Lerp(Vector3Proxy a, Vector3Proxy b, float t) =>
            new Vector3Proxy(Vector3.Lerp(a._vector, b._vector, t));

        public static implicit operator Vector3(Vector3Proxy proxy) => proxy._vector;
        public static implicit operator Vector3Proxy(Vector3 vector) => new Vector3Proxy(vector);

        public override string ToString() => $"({x}, {y}, {z})";
    }

    /// <summary>
    /// Proxy class for Quaternion to avoid exposing struct directly
    /// </summary>
    [MoonSharpUserData]
    public class QuaternionProxy
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

    /// <summary>
    /// Proxy class for Color to avoid exposing struct directly
    /// </summary>
    [MoonSharpUserData]
    public class ColorProxy
    {
        private Color _color;

        public ColorProxy(float r, float g, float b, float a = 1.0f)
        {
            _color = new Color(r, g, b, a);
        }

        public ColorProxy(Color color)
        {
            _color = color;
        }

        public float r { get { return _color.r; } set { _color.r = value; } }
        public float g { get { return _color.g; } set { _color.g = value; } }
        public float b { get { return _color.b; } set { _color.b = value; } }
        public float a { get { return _color.a; } set { _color.a = value; } }

        public static ColorProxy red => new ColorProxy(Color.red);
        public static ColorProxy green => new ColorProxy(Color.green);
        public static ColorProxy blue => new ColorProxy(Color.blue);
        public static ColorProxy white => new ColorProxy(Color.white);
        public static ColorProxy black => new ColorProxy(Color.black);
        public static ColorProxy yellow => new ColorProxy(Color.yellow);
        public static ColorProxy cyan => new ColorProxy(Color.cyan);
        public static ColorProxy magenta => new ColorProxy(Color.magenta);
        public static ColorProxy gray => new ColorProxy(Color.gray);
        public static ColorProxy clear => new ColorProxy(Color.clear);

        public static ColorProxy Lerp(ColorProxy a, ColorProxy b, float t) =>
            new ColorProxy(Color.Lerp(a._color, b._color, t));

        public static implicit operator Color(ColorProxy proxy) => proxy._color;
        public static implicit operator ColorProxy(Color color) => new ColorProxy(color);

        public override string ToString() => $"RGBA({r}, {g}, {b}, {a})";
    }

    /// <summary>
    /// Proxy class for RaycastHit to avoid exposing struct directly
    /// </summary>
    [MoonSharpUserData]
    public class RaycastHitProxy
    {
        private RaycastHit _hit;

        public RaycastHitProxy()
        {
            // Default constructor needed for AOT compatibility
        }

        public RaycastHitProxy(RaycastHit hit)
        {
            _hit = hit;
        }

        public Vector3Proxy point => new Vector3Proxy(_hit.point);
        public Vector3Proxy normal => new Vector3Proxy(_hit.normal);
        public float distance => _hit.distance;
        public string colliderName => _hit.collider?.name ?? "none";
        public string gameObjectName => _hit.transform?.gameObject?.name ?? "none";

        public static implicit operator RaycastHit(RaycastHitProxy proxy) => proxy._hit;
        public static implicit operator RaycastHitProxy(RaycastHit hit) => new RaycastHitProxy(hit);

        public override string ToString() => $"Hit: {gameObjectName} at {point}";
    }
}