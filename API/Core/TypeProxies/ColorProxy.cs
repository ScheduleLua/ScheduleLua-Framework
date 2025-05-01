using MoonSharp.Interpreter;
using ScheduleLua.API.Base;
using UnityEngine;

namespace ScheduleLua.API.Core.TypeProxies
{
    /// <summary>
    /// Proxy class for Color to avoid exposing struct directly
    /// </summary>
    [MoonSharpUserData]
    public class ColorProxy : UnityTypeProxyBase<Color>
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
}
