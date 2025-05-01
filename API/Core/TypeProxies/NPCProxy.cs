using MoonSharp.Interpreter;
using ScheduleLua.API.Base;

namespace ScheduleLua.API.Core.TypeProxies
{
    /// <summary>
    /// Proxy class for NPCs to avoid exposing complex NPC classes directly to Lua
    /// </summary>
    [MoonSharpUserData]
    public class NPCProxy : UnityTypeProxyBase<ScheduleOne.NPCs.NPC>
    {
        private ScheduleOne.NPCs.NPC _npc;

        public NPCProxy(ScheduleOne.NPCs.NPC npc)
        {
            _npc = npc;
        }

        // Basic properties that should be safe to expose
        public string ID => _npc?.ID;
        public string FullName => _npc?.fullName;
        public bool IsConscious => _npc?.IsConscious ?? false;
        public string Region => _npc?.Region.ToString();
        public bool IsMoving => _npc?.Movement?.IsMoving ?? false;

        // Wrapped NPC object for internal use
        public ScheduleOne.NPCs.NPC InternalNPC => _npc;

        // For compatibility with existing code
        public static implicit operator ScheduleOne.NPCs.NPC(NPCProxy proxy) => proxy?._npc;

        public override string ToString() => $"NPC: {FullName ?? "Unknown"} ({ID ?? "Unknown ID"})";
    }
}
