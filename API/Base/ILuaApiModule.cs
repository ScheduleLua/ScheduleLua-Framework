using MoonSharp.Interpreter;

namespace ScheduleLua.API.Base
{
    /// <summary>
    /// Interface for all Lua API modules.
    /// Each module encapsulates a specific domain of functionality
    /// exposed to Lua scripts.
    /// </summary>
    public interface ILuaApiModule
    {
        /// <summary>
        /// Gets the name of this API module
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Gets the loading priority of this module (lower numbers load first)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Gets whether this module is deprecated
        /// </summary>
        bool IsDeprecated { get; }

        /// <summary>
        /// Initializes this API module
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Registers all API functions with the Lua engine
        /// </summary>
        /// <param name="luaEngine">The Lua script engine</param>
        void RegisterAPI(Script luaEngine);
        
        /// <summary>
        /// Shuts down this API module and cleans up any resources
        /// </summary>
        void Shutdown();
    }
} 