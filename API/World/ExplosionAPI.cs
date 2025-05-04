using FishNet;
using MoonSharp.Interpreter;
using ScheduleLua.API.Base;
using ScheduleLua.API.Core;
using ScheduleOne.Combat;
using ScheduleOne.DevUtilities;
using System.Collections;
using UnityEngine;

namespace ScheduleLua.API.World
{
    public class ExplosionAPI : BaseLuaApiModule
    {
        /// <summary>
        /// Registers explosion-related API functions with the Lua engine
        /// </summary>
        public override void RegisterAPI(Script luaEngine)
        {
            if (luaEngine == null)
                throw new ArgumentNullException(nameof(luaEngine));

            luaEngine.Globals["TriggerExplosion"] = (Action<DynValue, float>)((pos, seconds) =>
            {
                if (pos.Type != DataType.Table)
                {
                    LuaUtility.LogError("TriggerExplosion expects a table with x, y, z");
                    return;
                }

                float x = (float)(pos.Table.Get("x").CastToNumber());
                float y = (float)(pos.Table.Get("y").CastToNumber());
                float z = (float)(pos.Table.Get("z").CastToNumber());

                Vector3 position = new Vector3(x, y, z);

                MelonLoader.MelonCoroutines.Start(DelayedExplosion(position, seconds));
            });
        }

        private static IEnumerator DelayedExplosion(Vector3 position, float seconds)
        {
            yield return new WaitForSeconds(seconds);

            if (InstanceFinder.IsServer)
            {
                LuaUtility.Log($"Explosion triggered at {position}");
                NetworkSingleton<CombatManager>.Instance.CreateExplosion(position, ExplosionData.DefaultSmall);
            }
            else
            {
                LuaUtility.LogWarning("Not on server — cannot trigger explosion");
            }
        }
    }
}
