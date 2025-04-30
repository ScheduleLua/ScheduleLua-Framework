using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using MoonSharp.Interpreter;
using ScheduleOne.Law;
using ScheduleOne.PlayerScripts;
using ScheduleLua.API.Core;
using UnityEngine;
using ScheduleOne.NPCs.Behaviour;
using ScheduleOne.DevUtilities;
using ScheduleLua.API.Base;

namespace ScheduleLua.API.Law
{
    public class LawAPI : BaseLuaApiModule
    {
        /// <summary>
        /// Registers all law-related API functions with the Lua engine
        /// </summary>
        public override void RegisterAPI(Script luaEngine)
        {
            if (luaEngine == null)
                throw new ArgumentNullException(nameof(luaEngine));

            // Law enforcement functions
            luaEngine.Globals["PoliceCallOnSelf"] = (Action)PoliceCallOnSelf;
            luaEngine.Globals["StartFootPatrol"] = (Action)StartFootPatrol;
            luaEngine.Globals["StartVehiclePatrol"] = (Action)StartVehiclePatrol;
            luaEngine.Globals["GetLawIntensity"] = (Func<float>)GetLawIntensity;
            luaEngine.Globals["SetLawIntensity"] = (Action<float>)SetLawIntensity;
        }

        /// <summary>
        /// Calls the police on the player character
        /// </summary>
        public static void PoliceCallOnSelf()
        {
            var player = ScheduleOne.PlayerScripts.Player.Local;
            if (player != null)
            {
                Singleton<LawManager>.Instance.PoliceCalled(player, new Crime());
            }
        }

        /// <summary>
        /// Starts a foot patrol on the first available route
        /// </summary>
        public static void StartFootPatrol()
        {
            var routes = GameObject.FindObjectsOfType<FootPatrolRoute>();
            if (routes == null || routes.Length == 0)
            {
                LuaUtility.LogError("No available foot patrol routes found.");
                return;
            }

            var route = routes.FirstOrDefault();
            if (route == null)
            {
                LuaUtility.LogError("Failed to retrieve a valid foot patrol route.");
                return;
            }

            LawManager.Instance.StartFootpatrol(route, 2);
        }

        /// <summary>
        /// Starts a vehicle patrol on the first available route
        /// </summary>
        public static void StartVehiclePatrol()
        {
            var vehicleRoutes = GameObject.FindObjectsOfType<VehiclePatrolRoute>();
            if (vehicleRoutes == null || vehicleRoutes.Length == 0)
            {
                LuaUtility.LogError("No vehicle patrol routes found.");
                return;
            }

            var vehicleRoute = vehicleRoutes.FirstOrDefault();
            if (vehicleRoute == null)
            {
                LuaUtility.LogError("Failed to retrieve a valid vehicle patrol route.");
                return;
            }

            LawManager.Instance.StartVehiclePatrol(vehicleRoute);
        }

        /// <summary>
        /// Gets the current law enforcement intensity setting
        /// </summary>
        /// <returns>The law intensity value between 0.0 and 1.0</returns>
        public static float GetLawIntensity()
        {
            var settings = LawController.Instance.GetSettings();
            if (settings != null)
            {
                var field = typeof(LawActivitySettings).GetField("intensity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    return (float)field.GetValue(settings);
                }
            }
            return 0f;
        }

        /// <summary>
        /// Sets the law enforcement intensity level
        /// </summary>
        /// <param name="value">The intensity value (0.0 to 1.0)</param>
        public static void SetLawIntensity(float value) => LawController.Instance.SetInternalIntensity(value);
    }
}
