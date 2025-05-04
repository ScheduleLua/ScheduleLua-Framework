using MelonLoader;
using MoonSharp.Interpreter;

namespace ScheduleLua.Core.Framework
{
    /// <summary>
    /// Handles detailed logging of Lua script errors.
    /// </summary>
    public class LuaScriptErrorHandler
    {
        private readonly MelonLogger.Instance _logger;
        private readonly string _scriptFilePath;
        private readonly string _scriptName;

        public LuaScriptErrorHandler(
            MelonLogger.Instance logger,
            string scriptFilePath,
            string scriptName
        )
        {
            _logger = logger;
            _scriptFilePath = scriptFilePath;
            _scriptName = scriptName;
        }

        /// <summary>
        /// Logs detailed error information for Lua script errors including stack traces.
        /// </summary>
        public void LogDetailedError(InterpreterException luaEx, string context)
        {
            string errorMessage = luaEx.DecoratedMessage ?? "Unknown Lua Error";
            string scriptContent = string.Empty;

            try
            {
                // Use the stored file path
                if (File.Exists(_scriptFilePath))
                {
                    scriptContent = File.ReadAllText(_scriptFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(
                    $"Could not read script file '{_scriptFilePath}' to show error context: {ex.Message}"
                );
            }

            // Use the stored script name and logger
            _logger.Error($"{context} in script '{_scriptName}': {errorMessage}");

            // Extract line number if available
            int lineNumber = GetLineNumberFromError(errorMessage);

            // Print problematic code section if available
            LogCodeContext(scriptContent, lineNumber);

            // Print stack trace with function names
            LogStackTrace(luaEx, lineNumber);

            // Provide additional context for common error types
            LogCommonErrorHints(errorMessage);
        }

        private int GetLineNumberFromError(string errorMessage)
        {
            // Example: "test.lua:5: attempt to call a nil value (global 'prnt')"
            // Example: "(string):1: attempt to call global 'missing_func' (a nil value)"
            try
            {
                // Find the first colon, which usually separates file/source from line number
                int firstColon = errorMessage.IndexOf(':');
                if (firstColon > 0)
                {
                    // Find the second colon, which usually separates line number from the message
                    int secondColon = errorMessage.IndexOf(':', firstColon + 1);
                    if (secondColon > firstColon)
                    {
                        string lineStr = errorMessage.Substring(
                            firstColon + 1,
                            secondColon - firstColon - 1
                        );
                        if (int.TryParse(lineStr.Trim(), out int line))
                        {
                            return line;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Could not parse line number from error: {ex.Message}");
            }
            return -1; // Indicate line number not found
        }

        private void LogCodeContext(string scriptContent, int lineNumber)
        {
            if (lineNumber <= 0 || string.IsNullOrEmpty(scriptContent))
                return;

            string[] lines = scriptContent.Split('\n');
            if (lineNumber > lines.Length)
                return;

            _logger.Error($"--- Error near line {lineNumber} in {_scriptName} ---");

            int startLine = Math.Max(0, lineNumber - 3); // Show 2 lines before
            int endLine = Math.Min(lines.Length - 1, lineNumber + 1); // Show line and 1 line after

            for (int i = startLine; i <= endLine; i++)
            {
                // Lua lines are 1-based, array is 0-based
                string linePrefix = (i + 1 == lineNumber) ? ">>>" : "   ";
                _logger.Error($"{linePrefix} {i + 1:D3}: {lines[i].TrimEnd()}");
            }
            _logger.Error("--- End of code context ---");
        }

        private void LogStackTrace(InterpreterException luaEx, int errorLine)
        {
            _logger.Error("Stack trace:");
            if (luaEx.CallStack != null && luaEx.CallStack.Count > 0)
            {
                bool firstFrame = true;
                foreach (var frame in luaEx.CallStack)
                {
                    string funcName = !string.IsNullOrEmpty(frame.Name)
                        ? frame.Name
                        : "<anonymous_function>";
                    string location = frame.Location != null
                        ? $"{Path.GetFileName(frame.Location.SourceIdx.ToString())}:{frame.Location.FromLine}" // Simplify location
                        : "<unknown_location>";
                    string highlight = firstFrame ? "[ERROR] " : "       ";
                    _logger.Error($"  {highlight}at {funcName} ({location})");
                    firstFrame = false;
                }
            }
            else if (errorLine > 0)
            {
                // Fallback if callstack is empty but we have a line number
                _logger.Error($"  [ERROR] at <script_level> ({_scriptName}:{errorLine})");
            }
            else
            {
                _logger.Error("  <Stack trace not available>");
            }
        }

        private void LogCommonErrorHints(string errorMessage)
        {
            // Simplified hints based on common error messages
            if (errorMessage.Contains("attempt to call a nil value"))
            {
                _logger.Error("[Hint] Trying to call something that isn't a function.");
                _logger.Error("       Check for typos in function names or if the variable holds the wrong value.");
                TryExtractAndLogIdentifier(errorMessage, "nil value (global '", "')");
            }
            else if (errorMessage.Contains("attempt to index a nil value"))
            {
                _logger.Error("[Hint] Trying to access a field (e.g., table.field) or method on something that is nil.");
                _logger.Error("       Check if the variable was assigned correctly before use.");
                TryExtractAndLogIdentifier(errorMessage, "nil value (field '", "')");
                TryExtractAndLogIdentifier(errorMessage, "nil value (global '", "')");
            }
            else if (errorMessage.Contains("attempt to perform arithmetic on"))
            {
                _logger.Error("[Hint] Trying to do math (+, -, *, /) on a value that isn't a number (maybe nil or string).");
                _logger.Error("       Ensure variables hold numbers. Use tonumber() to convert strings if needed.");
            }
            else if (errorMessage.Contains("attempt to concatenate"))
            {
                _logger.Error("[Hint] Trying to join strings (..) with a value that isn't a string or number (maybe nil).");
                _logger.Error("       Ensure variables hold strings/numbers. Use tostring() to convert if needed.");
            }
            else if (errorMessage.Contains("bad argument"))
            {
                _logger.Error("[Hint] Called a function with the wrong type of argument (e.g., expected number, got string).");
                _logger.Error("       Check the function's documentation or definition for required argument types.");
            }
            else if (errorMessage.Contains("attempt to call a") && !errorMessage.Contains("nil value"))
            {
                // Catches "attempt to call a string/table/number value"
                _logger.Error("[Hint] Trying to call something that is not a function (e.g., calling a string variable like a function).");
                _logger.Error("       Make sure the variable you are calling actually holds a function.");
            }
            // Add more hints as needed
        }

        private void TryExtractAndLogIdentifier(string errorMessage, string prefix, string suffix)
        {
            int startIndex = errorMessage.IndexOf(prefix);
            if (startIndex != -1)
            {
                startIndex += prefix.Length;
                int endIndex = errorMessage.IndexOf(suffix, startIndex);
                if (endIndex > startIndex)
                {
                    string identifier = errorMessage.Substring(startIndex, endIndex - startIndex);
                    _logger.Error($"       The problematic identifier might be: '{identifier}'");
                }
            }
        }
    }
}
