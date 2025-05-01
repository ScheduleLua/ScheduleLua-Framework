using System;
using UnityEngine;
using MoonSharp.Interpreter;
using ScheduleOne.NPCs;
using ScheduleLua.API.Core.TypeProxies;

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
}