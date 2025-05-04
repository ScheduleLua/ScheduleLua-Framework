using MoonSharp.Interpreter;
using ScheduleLua.API.Base;
using ScheduleLua.API.Core;

namespace ScheduleLua.Core.Framework
{
    /// <summary>
    /// Central registry that manages all API modules and their lifecycle.
    /// Handles module registration, initialization, and shutdown based on priority.
    /// </summary>
    public class ApiRegistry
    {
        private readonly List<ILuaApiModule> _modules = new List<ILuaApiModule>();
        private readonly Script _luaEngine;
        private bool _initialized = false;

        /// <summary>
        /// Creates a new API registry for the specified Lua engine
        /// </summary>
        /// <param name="luaEngine">The Lua script engine</param>
        public ApiRegistry(Script luaEngine)
        {
            _luaEngine = luaEngine ?? throw new ArgumentNullException(nameof(luaEngine));
        }

        /// <summary>
        /// Gets all registered modules
        /// </summary>
        public IReadOnlyList<ILuaApiModule> Modules => _modules;

        /// <summary>
        /// Gets whether the registry has been initialized
        /// </summary>
        public bool IsInitialized => _initialized;

        /// <summary>
        /// Registers a new API module
        /// </summary>
        /// <param name="module">The module to register</param>
        /// <returns>True if registered successfully, false if already registered</returns>
        public bool RegisterModule(ILuaApiModule module)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module));

            if (_modules.Any(m => m.Name == module.Name))
            {
                LuaUtility.LogWarning($"Module {module.Name} is already registered.");
                return false;
            }

            _modules.Add(module);

            // If we're already initialized, initialize this module immediately
            if (_initialized)
            {
                try
                {
                    module.Initialize();
                    module.RegisterAPI(_luaEngine);
                    LuaUtility.Log($"Late-initialized module: {module.Name}");
                }
                catch (Exception ex)
                {
                    LuaUtility.LogError($"Failed to late-initialize module {module.Name}: {ex.Message}");
                }
            }

            return true;
        }

        /// <summary>
        /// Initializes all registered modules in priority order (lower priority values initialize first)
        /// </summary>
        public void InitializeAll()
        {
            if (_initialized) return;

            // Sort modules by priority - lower numbers go first
            var modulesToInitialize = _modules.OrderBy(m => m.Priority).ToList();

            // Initialize modules in priority order
            foreach (var module in modulesToInitialize)
            {
                try
                {
                    module.Initialize();
                    module.RegisterAPI(_luaEngine);
                    LuaUtility.Log($"Initialized module: {module.Name}");
                }
                catch (Exception ex)
                {
                    LuaUtility.LogError($"Failed to initialize module {module.Name}: {ex.Message}");
                    LuaUtility.LogError(ex.StackTrace);
                }
            }

            _initialized = true;
        }

        /// <summary>
        /// Shuts down all modules in reverse priority order
        /// </summary>
        public void ShutdownAll()
        {
            if (!_initialized) return;

            // Shutdown in reverse priority order
            foreach (var module in _modules.OrderByDescending(m => m.Priority))
            {
                try
                {
                    module.Shutdown();
                    LuaUtility.Log($"Shut down module: {module.Name}");
                }
                catch (Exception ex)
                {
                    LuaUtility.LogError($"Error shutting down module {module.Name}: {ex.Message}");
                }
            }

            _initialized = false;
        }

        /// <summary>
        /// Gets a module by type
        /// </summary>
        /// <typeparam name="T">The type of module to get</typeparam>
        /// <returns>The module instance, or null if not found</returns>
        public T GetModule<T>() where T : ILuaApiModule
        {
            return _modules.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Gets a module by name
        /// </summary>
        /// <param name="name">The name of the module to get</param>
        /// <returns>The module instance, or null if not found</returns>
        public ILuaApiModule GetModule(string name)
        {
            return _modules.FirstOrDefault(m => m.Name == name);
        }
    }
}