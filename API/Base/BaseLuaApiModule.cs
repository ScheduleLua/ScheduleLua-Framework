using MoonSharp.Interpreter;
using ScheduleLua.API.Core;

namespace ScheduleLua.API.Base
{
    /// <summary>
    /// Base implementation of ILuaApiModule that provides common functionality for all API modules.
    /// Specific API modules should inherit from this class rather than implementing ILuaApiModule directly.
    /// </summary>
    public abstract class BaseLuaApiModule : ILuaApiModule
    {
        /// <summary>
        /// Gets the name of this API module (defaults to the class name)
        /// </summary>
        public virtual string Name => GetType().Name;

        /// <summary>
        /// Gets the loading priority of this module (default is 100)
        /// </summary>
        public virtual int Priority => 100;

        /// <summary>
        /// Gets whether this module is deprecated
        /// </summary>
        public virtual bool IsDeprecated => false;

        /// <summary>
        /// Initializes this API module (empty by default)
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Registers all API functions with the Lua engine.
        /// Must be implemented by derived classes.
        /// </summary>
        /// <param name="luaEngine">The Lua script engine</param>
        public abstract void RegisterAPI(Script luaEngine);

        /// <summary>
        /// Shuts down this API module and cleans up any resources (empty by default)
        /// </summary>
        public virtual void Shutdown() { }

        /// <summary>
        /// Logs an informational message prefixed with this module's name
        /// </summary>
        protected void LogInfo(string message) => LuaUtility.Log($"[{Name}] {message}");

        /// <summary>
        /// Logs a warning message prefixed with this module's name
        /// </summary>
        protected void LogWarning(string message) => LuaUtility.LogWarning($"[{Name}] {message}");

        /// <summary>
        /// Logs an error message prefixed with this module's name
        /// </summary>
        protected void LogError(string message) => LuaUtility.LogError($"[{Name}] {message}");
    }
}