using System;
using System.Collections.Generic;
using MelonLoader;
using ScheduleOne;
using MoonSharp.Interpreter;
using Console = ScheduleOne.Console;
using UnityEngine.SceneManagement;
using ScheduleOne.PlayerScripts;
using ScheduleLua.API.Core;

namespace ScheduleLua.API.Registry
{
    /// <summary>
    /// Provides a system for Lua scripts to register console commands that interface with ScheduleOne's native Console
    /// </summary>
    public static class CommandRegistry
    {
        private static MelonLogger.Instance _logger => ScheduleLua.Core.Instance.LoggerInstance;
        private static Dictionary<string, LuaCommand> _registeredCommands = new Dictionary<string, LuaCommand>();

        // Track which script registered which command
        private static Dictionary<string, LuaScript> _commandOwners = new Dictionary<string, LuaScript>();

        // Track all script instances
        private static Dictionary<string, LuaScript> _scriptInstances = new Dictionary<string, LuaScript>();

        /// <summary>
        /// Registers a script instance for command tracking
        /// </summary>
        public static void RegisterScriptInstance(LuaScript script)
        {
            if (script != null)
            {
                string scriptName = script.Name;
                if (!string.IsNullOrEmpty(scriptName))
                {
                    _scriptInstances[scriptName] = script;
                }
            }
        }

        /// <summary>
        /// Registers the command API with the Lua engine
        /// </summary>
        public static void RegisterCommandAPI(Script luaEngine)
        {
            if (luaEngine == null)
                throw new ArgumentNullException(nameof(luaEngine));

            luaEngine.Globals["RegisterCommand"] = (Action<string, string, string, Closure>)RegisterCommand;
            luaEngine.Globals["UnregisterCommand"] = (Action<string>)UnregisterCommand;
            luaEngine.Globals["UnregisterAllCommands"] = (Action)UnregisterAllCommands;
            luaEngine.Globals["IsCommandRegistered"] = (Func<string, bool>)IsCommandRegistered;
            luaEngine.Globals["GetGameCommands"] = (Func<Table>)GetGameCommands;
        }

        /// <summary>
        /// Register a new console command from Lua
        /// </summary>
        /// <param name="commandName">Command name (without slash)</param>
        /// <param name="description">Command description</param>
        /// <param name="usage">Example usage</param>
        /// <param name="callback">Lua function to call when command is executed</param>
        public static void RegisterCommand(string commandName, string description, string usage, Closure callback)
        {
            if (string.IsNullOrWhiteSpace(commandName))
            {
                LuaUtility.LogError("Cannot register command: Command name is empty");
                return;
            }

            if (callback == null)
            {
                LuaUtility.LogError($"Cannot register command '{commandName}': Callback is null");
                return;
            }

            // Check if the console is ready by checking Core's _consoleReadyTriggered flag
            if (!ScheduleLua.Core.Instance._consoleReadyTriggered)
            {
                LuaUtility.LogError($"Cannot register command '{commandName}' because Console is not yet ready. " +
                             "Please register commands only after the OnConsoleReady event fires to avoid conflicts with native game commands.");
                return;
            }

            // Check if we're in the Menu scene or if the player isn't ready yet
            if (SceneManager.GetActiveScene().name == "Menu")
            {
                LuaUtility.LogError($"Cannot register command '{commandName}' during Menu scene. " +
                             "Please register commands after OnConsoleReady to avoid conflicts.");
                return;
            }

            // Get access to the game's commands dictionary
            Dictionary<string, Console.ConsoleCommand> commandsDict = null;
            if (typeof(Console).GetField("commands", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)?.GetValue(null) is Dictionary<string, Console.ConsoleCommand> dict)
            {
                commandsDict = dict;
            }
            else
            {
                LuaUtility.LogError($"Failed to access Console commands dictionary. Command '{commandName}' may not function properly.");
                return;
            }

            // Check if this is a built-in game command that we should not override
            if (commandsDict.TryGetValue(commandName, out var existingCommand) && !(existingCommand is LuaCommand))
            {
                // This is a built-in game command, not a Lua command - never override it
                _logger.Warning($"Cannot register Lua command '{commandName}' as it would override a built-in game command. " +
                               $"Try using a different name, such as 'lua_{commandName}'.");
                return;
            }

            // Check if command is already registered in our system
            if (_registeredCommands.ContainsKey(commandName))
            {
                _logger.Warning($"Lua command '{commandName}' is already registered. Overwriting...");

                // Remove previous Lua command with the same name
                Console.Commands.RemoveAll(cmd => cmd.CommandWord == commandName && cmd is LuaCommand);
                if (commandsDict.ContainsKey(commandName))
                {
                    commandsDict.Remove(commandName);
                }

                _registeredCommands.Remove(commandName);
                _commandOwners.Remove(commandName);
            }

            // Create a new command
            var luaCommand = new LuaCommand(commandName, description, usage, callback);

            // Register with our system
            _registeredCommands.Add(commandName, luaCommand);

            // Track which script registered this command by finding the currently executing script
            LuaScript currentScript = GetCallingScript(callback);
            if (currentScript != null)
            {
                _commandOwners[commandName] = currentScript;
                currentScript.AddRegisteredCommand(commandName);
            }

            // Register with ScheduleOne's console
            Console.Commands.Add(luaCommand);

            // Update the commands dictionary - only add, never replace game commands
            if (commandsDict.ContainsKey(commandName))
            {
                // This should only happen if it's a Lua command, since we check for game commands above
                commandsDict[commandName] = luaCommand;
            }
            else
            {
                commandsDict.Add(commandName, luaCommand);
            }

            _logger.Msg($"Registered Lua command: {commandName}");
        }

        /// <summary>
        /// Determine which script registered a command based on the closure's owner
        /// </summary>
        private static LuaScript GetCallingScript(Closure callback)
        {
            // Try to identify which script is making this call by accessing internal MoonSharp data
            // This is a bit hacky but provides a good way to determine command ownership
            if (callback == null || callback.OwnerScript == null)
                return null;

            // Look through script instances to find matching script
            foreach (var script in _scriptInstances.Values)
            {
                if (ReferenceEquals(callback.OwnerScript, ScheduleLua.Core.Instance._luaEngine))
                {
                    return script;
                }
            }

            // Fallback: try to find the currently executing script via other methods
            // For simplicity, return the first script found - in a real implementation, 
            // you might need to check call stacks or other indicators
            return _scriptInstances.Count > 0 ? _scriptInstances.Values.GetEnumerator().Current : null;
        }

        /// <summary>
        /// Get all registered game commands as a Lua table
        /// </summary>
        /// <returns>A Lua table containing command names and descriptions</returns>
        public static Table GetGameCommands()
        {
            var table = new Table(ScheduleLua.Core.Instance._luaEngine);

            // Get access to the game's commands dictionary
            if (typeof(Console).GetField("commands", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)?.GetValue(null) is Dictionary<string, Console.ConsoleCommand> commandsDict)
            {
                foreach (var kvp in commandsDict)
                {
                    if (!(kvp.Value is LuaCommand)) // Only include built-in commands, not Lua commands
                    {
                        var cmdTable = new Table(ScheduleLua.Core.Instance._luaEngine);
                        cmdTable["name"] = kvp.Key;
                        cmdTable["description"] = kvp.Value.CommandDescription;
                        cmdTable["usage"] = kvp.Value.ExampleUsage;
                        cmdTable["isLuaCommand"] = false;

                        table[kvp.Key] = cmdTable;
                    }
                }
            }

            return table;
        }

        /// <summary>
        /// Unregister a command by name
        /// </summary>
        /// <param name="commandName">Name of the command to remove</param>
        public static void UnregisterCommand(string commandName)
        {
            if (string.IsNullOrWhiteSpace(commandName))
            {
                LuaUtility.LogError("Cannot unregister command: Command name is empty");
                return;
            }

            if (_registeredCommands.TryGetValue(commandName, out var command))
            {
                // Remove from ScheduleOne's console list
                Console.Commands.RemoveAll(cmd => cmd.CommandWord == commandName && cmd is LuaCommand);

                // IMPORTANT FIX: Also remove from the commands dictionary
                if (typeof(Console).GetField("commands", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)?.GetValue(null) is Dictionary<string, Console.ConsoleCommand> commandsDict)
                {
                    if (commandsDict.ContainsKey(commandName))
                    {
                        commandsDict.Remove(commandName);
                    }
                }

                // Remove the command from the owning script's list if applicable
                if (_commandOwners.TryGetValue(commandName, out var script))
                {
                    // We don't call script.RemoveCommand here, since this method might be called
                    // directly during script reload when we're already handling command cleanup
                    _commandOwners.Remove(commandName);
                }

                // Remove from our registry
                _registeredCommands.Remove(commandName);
                _logger.Msg($"Unregistered Lua command: {commandName}");
            }
            else
            {
                _logger.Warning($"Command '{commandName}' was not registered by Lua and cannot be unregistered");
            }
        }

        /// <summary>
        /// Unregister all commands registered by Lua
        /// </summary>
        public static void UnregisterAllCommands()
        {
            foreach (var commandName in new List<string>(_registeredCommands.Keys))
            {
                UnregisterCommand(commandName);
            }

            _logger.Msg("Unregistered all Lua commands");
        }

        /// <summary>
        /// Check if a command is registered
        /// </summary>
        /// <param name="commandName">Command name to check</param>
        /// <returns>True if the command is registered, false otherwise</returns>
        public static bool IsCommandRegistered(string commandName)
        {
            return _registeredCommands.ContainsKey(commandName);
        }

        /// <summary>
        /// Command implementation that bridges between ScheduleOne's console and Lua functions
        /// </summary>
        public class LuaCommand : ScheduleOne.Console.ConsoleCommand
        {
            private readonly string _commandWord;
            private readonly string _description;
            private readonly string _usage;
            private readonly Closure _callback;

            public override string CommandWord => _commandWord;
            public override string CommandDescription => _description;
            public override string ExampleUsage => _usage;

            public LuaCommand(string commandWord, string description, string usage, Closure callback)
            {
                _commandWord = commandWord;
                _description = description;
                _usage = usage;
                _callback = callback;
            }

            public override void Execute(List<string> args)
            {
                try
                {
                    // Convert C# List to Lua table
                    var argsTable = new Table(null);
                    for (int i = 0; i < args.Count; i++)
                    {
                        argsTable[i + 1] = args[i]; // Lua tables are 1-indexed
                    }

                    // Call the Lua function
                    _callback.Call(argsTable);
                }
                catch (Exception ex)
                {
                    LuaUtility.LogError($"Error executing Lua command '{_commandWord}': {ex.Message}");
                    if (ex is InterpreterException luaEx)
                    {
                        LuaUtility.LogError(luaEx.DecoratedMessage);
                    }
                    else
                    {
                        LuaUtility.LogError(ex.StackTrace);
                    }
                }
            }
        }
    }
}