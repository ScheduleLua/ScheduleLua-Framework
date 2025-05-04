using MoonSharp.Interpreter;
using ScheduleLua.API.Core;
using ScheduleLua.API.UI.Styles;

namespace ScheduleLua.API.UI
{
    /// <summary>
    /// Central manager for UI subsystem components
    /// </summary>
    public static class UIManager
    {
        // Static instance of the style manager
        private static UIStyleManager _styleManager;

        // Static property to access the style manager
        public static UIStyleManager StyleManager
        {
            get
            {
                if (_styleManager == null)
                {
                    _styleManager = new UIStyleManager();
                    _styleManager.Initialize();
                }
                return _styleManager;
            }
        }

        /// <summary>
        /// Initialize the UI manager
        /// </summary>
        public static void Initialize()
        {
            if (_styleManager == null)
            {
                _styleManager = new UIStyleManager();
                _styleManager.Initialize();
            }

            LuaUtility.Log("UI Manager initialized");
        }

        /// <summary>
        /// Gets the calling environment from a script execution context
        /// </summary>
        public static Table GetCallingEnvironment(ScriptExecutionContext ctx)
        {
            if (ctx == null)
                return null;

            try
            {
                // Get the script instance
                var scriptRuntime = ctx.GetScript();
                if (scriptRuntime != null)
                {
                    // Get the global environment from the script
                    var globals = scriptRuntime.Globals;

                    // Try to get SCRIPT_PATH from globals
                    var scriptPathVal = globals.Get("SCRIPT_PATH");
                    if (scriptPathVal != null && scriptPathVal.Type == DataType.String)
                    {
                        // Create a simple environment with the script path
                        Table simpleEnv = new Table(scriptRuntime);
                        simpleEnv["SCRIPT_PATH"] = scriptPathVal.String;
                        return simpleEnv;
                    }

                    // If we can't get a specific environment, return the globals
                    return globals;
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error getting calling environment: {ex.Message}", ex);
            }

            return null;
        }
    }
}