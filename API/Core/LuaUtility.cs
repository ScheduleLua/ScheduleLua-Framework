using System;
using UnityEngine;
using MoonSharp.Interpreter;
using ScheduleOne;
using System.Collections.Generic;

namespace ScheduleLua.API.Core
{
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
    /// Provides utility methods and shared functionality for working with MoonSharp Lua
    /// </summary>
    public static class LuaUtility
    {
        static LuaUtility()
        {
            // Register proxy types instead of structs
            UserData.RegisterType<Vector3Proxy>();
            
            // Register other needed types
            UserData.RegisterType<GameObject>();
        }

        /// <summary>
        /// Gets the active MoonSharp Script instance from the Lua environment
        /// </summary>
        public static Script GetLuaScript()
        {
            // Use Core's script instance instead of ScriptRunner
            return ScheduleLua.Core.Instance._luaEngine;
        }

        /// <summary>
        /// Creates a new Lua table
        /// </summary>
        public static Table CreateTable()
        {
            var script = GetLuaScript();
            return new Table(script);
        }

        /// <summary>
        /// Logs a message with the Lua API prefix
        /// </summary>
        public static void Log(string message)
        {
            Debug.Log($"[ScheduleLua] {message}");
        }

        /// <summary>
        /// Logs a warning message with the Lua API prefix
        /// </summary>
        public static void LogWarning(string message)
        {
            Debug.LogWarning($"[ScheduleLua] {message}");
        }

        /// <summary>
        /// Logs an error message with the Lua API prefix
        /// </summary>
        public static void LogError(string message, Exception ex = null)
        {
            string errorMessage = $"[ScheduleLua] {message}";
            if (ex != null)
            {
                errorMessage += $": {ex.Message}";
            }
            Debug.LogError(errorMessage);
        }

        /// <summary>
        /// Converts a Vector3 to a Lua table
        /// </summary>
        public static Table Vector3ToTable(Vector3 vector)
        {
            var table = CreateTable();
            table["x"] = vector.x;
            table["y"] = vector.y;
            table["z"] = vector.z;
            return table;
        }

        /// <summary>
        /// Gets all available map regions as an array of strings
        /// </summary>
        /// <returns>Array of region names</returns>
        public static string[] GetAllMapRegions()
        {
            try
            {
                var regionValues = System.Enum.GetValues(typeof(ScheduleOne.Map.EMapRegion));
                string[] regions = new string[regionValues.Length];
                
                for (int i = 0; i < regionValues.Length; i++)
                {
                    regions[i] = regionValues.GetValue(i).ToString();
                }
                
                return regions;
            }
            catch (Exception ex)
            {
                LogError("Error getting map regions", ex);
                return new string[0];
            }
        }

        /// <summary>
        /// Creates a Lua table from an array of strings
        /// </summary>
        /// <param name="array">The string array to convert</param>
        /// <returns>A Lua table with 1-based indexed strings</returns>
        public static Table StringArrayToTable(string[] array)
        {
            var table = CreateTable();
            
            for (int i = 0; i < array.Length; i++)
            {
                table[i + 1] = array[i];
            }
            
            return table;
        }
    }
} 