using MelonLoader;
using UnityEngine.SceneManagement;
using Console = ScheduleOne.Console;

namespace ScheduleLua.API.Registry
{
    /// <summary>
    /// Provides backend console commands for managing Lua scripts
    /// </summary>
    public static class ScriptCommands
    {
        private static MelonLogger.Instance _logger => ModCore.Instance.LoggerInstance;
        private static bool _commandsRegistered = false;

        /// <summary>
        /// Registers the backend console commands for the Lua API
        /// </summary>
        public static void RegisterBackendCommands()
        {
            // Check if we're in the right scene and the console is ready
            if (!ModCore.Instance._consoleReadyTriggered || SceneManager.GetActiveScene().name != "Main")
            {
                return;
            }

            // Don't register commands multiple times
            if (_commandsRegistered)
            {
                return;
            }

            try
            {
                // Access the Console commands dictionary
                Dictionary<string, Console.ConsoleCommand> commandsDict = null;
                if (typeof(Console).GetField("commands", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)?.GetValue(null) is Dictionary<string, Console.ConsoleCommand> dict)
                {
                    commandsDict = dict;
                }
                else
                {
                    _logger.Error("Failed to access Console commands dictionary. Backend commands may not function properly.");
                    return;
                }

                RegisterLuaReloadCommand(commandsDict);

                // Breaks the lua script/engine
                // RegisterLuaEnableCommand(commandsDict);

                // Breaks the lua script/engine
                // RegisterLuaDisableCommand(commandsDict);

                _commandsRegistered = true;
                _logger.Msg("Registered backend Lua script management commands");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error registering backend commands: {ex.Message}");
            }
        }

        private static void RegisterLuaReloadCommand(Dictionary<string, Console.ConsoleCommand> commandsDict)
        {
            var command = new LuaReloadCommand();
            Console.Commands.Add(command);

            if (commandsDict.ContainsKey(command.CommandWord))
            {
                commandsDict[command.CommandWord] = command;
            }
            else
            {
                commandsDict.Add(command.CommandWord, command);
            }

            _logger.Msg($"Registered command: {command.CommandWord}");
        }

        private static void RegisterLuaEnableCommand(Dictionary<string, Console.ConsoleCommand> commandsDict)
        {
            var command = new LuaEnableCommand();
            Console.Commands.Add(command);

            if (commandsDict.ContainsKey(command.CommandWord))
            {
                commandsDict[command.CommandWord] = command;
            }
            else
            {
                commandsDict.Add(command.CommandWord, command);
            }

            _logger.Msg($"Registered command: {command.CommandWord}");
        }

        private static void RegisterLuaDisableCommand(Dictionary<string, Console.ConsoleCommand> commandsDict)
        {
            var command = new LuaDisableCommand();
            Console.Commands.Add(command);

            if (commandsDict.ContainsKey(command.CommandWord))
            {
                commandsDict[command.CommandWord] = command;
            }
            else
            {
                commandsDict.Add(command.CommandWord, command);
            }

            _logger.Msg($"Registered command: {command.CommandWord}");
        }

        /// <summary>
        /// Command implementation for luareload
        /// </summary>
        private class LuaReloadCommand : Console.ConsoleCommand
        {
            public override string CommandWord => "luareload";
            public override string CommandDescription => "Reloads Lua scripts and mods. Specify a script or mod name to reload a specific one.";
            public override string ExampleUsage => "luareload [scriptname|modname]";

            public override void Execute(List<string> args)
            {
                try
                {
                    var core = ModCore.Instance;
                    if (core == null)
                    {
                        Console.Log("Lua system is not initialized.");
                        return;
                    }

                    // If no args given, reload all scripts and mods
                    if (args.Count == 0)
                    {
                        // Reload all individual scripts
                        int reloadedScriptCount = 0;
                        foreach (var script in core._loadedScripts.Values.ToList())
                        {
                            if (script.Reload())
                            {
                                reloadedScriptCount++;
                            }
                        }

                        // Reload all mods
                        int reloadedModCount = 0;
                        if (core._modManager != null)
                        {
                            foreach (var mod in core._modManager.LoadedMods.Values.ToList())
                            {
                                if (mod.Reload())
                                {
                                    reloadedModCount++;
                                }
                            }
                        }

                        Console.Log($"Reloaded {reloadedScriptCount} individual Lua scripts and {reloadedModCount} mods.");
                    }
                    else
                    {
                        // Reload a specific script or mod
                        string name = args[0];
                        bool found = false;

                        // First check if it's a mod name
                        if (core._modManager != null)
                        {
                            var mod = core._modManager.GetMod(name);
                            if (mod != null)
                            {
                                found = true;
                                if (mod.Reload())
                                {
                                    Console.Log($"Reloaded mod: {mod.Manifest.Name}");
                                }
                                else
                                {
                                    Console.Log($"Failed to reload mod: {mod.Manifest.Name}");
                                }
                            }
                        }

                        // If not a mod, check if it's an individual script
                        if (!found)
                        {
                            foreach (var script in core._loadedScripts.Values.ToList())
                            {
                                if (script.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    found = true;
                                    if (script.Reload())
                                    {
                                        Console.Log($"Reloaded script: {script.Name}");
                                    }
                                    else
                                    {
                                        Console.Log($"Failed to reload script: {script.Name}");
                                    }
                                    break;
                                }
                            }
                        }

                        if (!found)
                        {
                            Console.Log($"Script or mod not found: {name}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Log($"Error reloading: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Command implementation for luaenable
        /// </summary>
        private class LuaEnableCommand : Console.ConsoleCommand
        {
            public override string CommandWord => "luaenable";
            public override string CommandDescription => "Enables a specific Lua script or mod by reloading and initializing it.";
            public override string ExampleUsage => "luaenable <scriptname|modname>";

            public override void Execute(List<string> args)
            {
                try
                {
                    var core = ModCore.Instance;
                    if (core == null)
                    {
                        Console.Log("Lua system is not initialized.");
                        return;
                    }

                    if (args.Count == 0)
                    {
                        Console.Log("Please specify a script or mod name to enable.");
                        return;
                    }

                    string name = args[0];
                    bool found = false;

                    // First check if it's a mod name
                    if (core._modManager != null)
                    {
                        var mod = core._modManager.GetMod(name);
                        if (mod != null)
                        {
                            found = true;
                            if (mod.Reload() && mod.Initialize())
                            {
                                Console.Log($"Enabled mod: {mod.Manifest.Name}");
                            }
                            else
                            {
                                Console.Log($"Failed to enable mod: {mod.Manifest.Name}");
                            }
                        }
                    }

                    // If not a mod, check if it's an individual script
                    if (!found)
                    {
                        foreach (var script in core._loadedScripts.Values.ToList())
                        {
                            if (script.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                            {
                                found = true;
                                // Reload and initialize the script
                                if (script.Reload() && script.Initialize())
                                {
                                    Console.Log($"Enabled script: {script.Name}");
                                }
                                else
                                {
                                    Console.Log($"Failed to enable script: {script.Name}");
                                }
                                break;
                            }
                        }
                    }

                    if (!found)
                    {
                        Console.Log($"Script or mod not found: {name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.Log($"Error enabling: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Command implementation for luadisable
        /// </summary>
        private class LuaDisableCommand : Console.ConsoleCommand
        {
            public override string CommandWord => "luadisable";
            public override string CommandDescription => "Disables a specific Lua script or mod until it is manually enabled again.";
            public override string ExampleUsage => "luadisable <scriptname|modname>";

            public override void Execute(List<string> args)
            {
                try
                {
                    var core = ModCore.Instance;
                    if (core == null)
                    {
                        Console.Log("Lua system is not initialized.");
                        return;
                    }

                    if (args.Count == 0)
                    {
                        Console.Log("Please specify a script or mod name to disable.");
                        return;
                    }

                    string name = args[0];
                    bool found = false;

                    // First check if it's a mod name
                    if (core._modManager != null)
                    {
                        var mod = core._modManager.GetMod(name);
                        if (mod != null)
                        {
                            found = true;
                            // For mods, we can't fully disable them but we can notify the user
                            Console.Log($"Note: Mods cannot be fully disabled at runtime. Mod '{mod.Manifest.Name}' will not receive further updates until the game is restarted or the mod is enabled again.");
                            // We could implement a _disabledMods HashSet in ModManager to prevent updates
                        }
                    }

                    // If not a mod, check if it's an individual script
                    if (!found)
                    {
                        foreach (var kvp in core._loadedScripts.ToList())
                        {
                            var script = kvp.Value;
                            if (script.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                            {
                                found = true;

                                // We can't fully unload a script, but we can remove it from the collection
                                // temporarily to stop it from receiving events and updates
                                core._loadedScripts.Remove(kvp.Key);
                                Console.Log($"Disabled script: {script.Name}");
                                break;
                            }
                        }
                    }

                    if (!found)
                    {
                        Console.Log($"Script or mod not found: {name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.Log($"Error disabling: {ex.Message}");
                }
            }
        }
    }
}