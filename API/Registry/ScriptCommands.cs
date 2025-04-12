using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MelonLoader;
using ScheduleOne;
using MoonSharp.Interpreter;
using Console = ScheduleOne.Console;
using UnityEngine.SceneManagement;

namespace ScheduleLua.API.Registry
{
    /// <summary>
    /// Provides backend console commands for managing Lua scripts
    /// </summary>
    public static class ScriptCommands
    {
        private static MelonLogger.Instance _logger => ScheduleLua.Core.Instance.LoggerInstance;
        private static bool _commandsRegistered = false;

        /// <summary>
        /// Registers the backend console commands for the Lua API
        /// </summary>
        public static void RegisterBackendCommands()
        {
            // Check if we're in the right scene and the console is ready
            if (!ScheduleLua.Core.Instance._consoleReadyTriggered || SceneManager.GetActiveScene().name != "Main")
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
            public override string CommandDescription => "Reloads Lua scripts. Specify a script name to reload a specific script.";
            public override string ExampleUsage => "luareload [scriptname]";

            public override void Execute(List<string> args)
            {
                try
                {
                    var core = ScheduleLua.Core.Instance;
                    if (core == null)
                    {
                        Console.Log("Lua system is not initialized.");
                        return;
                    }

                    if (args.Count == 0)
                    {
                        // Reload all scripts
                        int reloadedCount = 0;
                        foreach (var script in core._loadedScripts.Values.ToList())
                        {
                            if (script.Reload())
                            {
                                reloadedCount++;
                            }
                        }
                        Console.Log($"Reloaded {reloadedCount} Lua scripts.");
                    }
                    else
                    {
                        // Reload a specific script
                        string scriptName = args[0];
                        bool found = false;

                        foreach (var script in core._loadedScripts.Values.ToList())
                        {
                            if (script.Name.Equals(scriptName, StringComparison.OrdinalIgnoreCase))
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

                        if (!found)
                        {
                            Console.Log($"Script not found: {scriptName}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Log($"Error reloading scripts: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Command implementation for luaenable
        /// </summary>
        private class LuaEnableCommand : Console.ConsoleCommand
        {
            public override string CommandWord => "luaenable";
            public override string CommandDescription => "Enables a specific Lua script by reloading and initializing it.";
            public override string ExampleUsage => "luaenable <scriptname>";

            public override void Execute(List<string> args)
            {
                try
                {
                    var core = ScheduleLua.Core.Instance;
                    if (core == null)
                    {
                        Console.Log("Lua system is not initialized.");
                        return;
                    }

                    if (args.Count == 0)
                    {
                        Console.Log("Please specify a script name to enable.");
                        return;
                    }

                    string scriptName = args[0];
                    bool found = false;

                    foreach (var script in core._loadedScripts.Values.ToList())
                    {
                        if (script.Name.Equals(scriptName, StringComparison.OrdinalIgnoreCase))
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

                    if (!found)
                    {
                        Console.Log($"Script not found: {scriptName}");
                    }
                }
                catch (Exception ex)
                {
                    Console.Log($"Error enabling script: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Command implementation for luadisable
        /// </summary>
        private class LuaDisableCommand : Console.ConsoleCommand
        {
            public override string CommandWord => "luadisable";
            public override string CommandDescription => "Disables a specific Lua script until it is manually enabled again.";
            public override string ExampleUsage => "luadisable <scriptname>";

            public override void Execute(List<string> args)
            {
                try
                {
                    var core = ScheduleLua.Core.Instance;
                    if (core == null)
                    {
                        Console.Log("Lua system is not initialized.");
                        return;
                    }

                    if (args.Count == 0)
                    {
                        Console.Log("Please specify a script name to disable.");
                        return;
                    }

                    string scriptName = args[0];
                    bool found = false;

                    foreach (var kvp in core._loadedScripts.ToList())
                    {
                        var script = kvp.Value;
                        if (script.Name.Equals(scriptName, StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;

                            // We can't fully unload a script, but we can remove it from the collection
                            // temporarily to stop it from receiving events and updates
                            core._loadedScripts.Remove(kvp.Key);
                            Console.Log($"Disabled script: {script.Name}");
                            break;
                        }
                    }

                    if (!found)
                    {
                        Console.Log($"Script not found: {scriptName}");
                    }
                }
                catch (Exception ex)
                {
                    Console.Log($"Error disabling script: {ex.Message}");
                }
            }
        }
    }
}