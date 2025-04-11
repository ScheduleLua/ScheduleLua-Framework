using System;
using UnityEngine;
using MoonSharp.Interpreter;

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
        }
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