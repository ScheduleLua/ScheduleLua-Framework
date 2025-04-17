-- ScheduleLua Error Test Script
-- This script contains intentional errors to test error reporting

local TEST_ERRORS = {}

-- Initialize function called when script is first loaded
function Initialize()
    Log("Error test script initialized!")
    Log("Run the test_errors command to try different errors")
end

-- Test 1: Attempt to call a nil value (undefined function)
TEST_ERRORS.nil_call = function()
    Log("Testing 'attempt to call a nil value' error...")
    
    -- Intentionally call a function that doesn't exist
    nonExistentFunction()
end

-- Test 2: Attempt to index a nil value (accessing field of nil)
TEST_ERRORS.nil_index = function()
    Log("Testing 'attempt to index a nil value' error...")
    
    -- Create a nil variable and try to access a field
    local player = nil
    local health = player.health
end

-- Test 3: Attempt to perform arithmetic on nil
TEST_ERRORS.nil_math = function()
    Log("Testing 'attempt to perform arithmetic on nil' error...")
    
    -- Try to add a number to a nil value
    local score = nil
    local newScore = score + 10
end

-- Test 4: Type mismatch error
TEST_ERRORS.type_mismatch = function()
    Log("Testing type mismatch error...")
    
    -- Try to use a number as a function
    local value = 42
    value()
end

-- Test 5: String concatenation with nil
TEST_ERRORS.bad_concat = function()
    Log("Testing string concatenation with nil error...")
    
    -- Try to concatenate a string with a nil value
    local name = nil
    local greeting = "Hello, " .. name .. "!"
end

-- Test 6: Bad argument type
TEST_ERRORS.arg_type = function()
    Log("Testing bad argument type error...")
    
    -- Pass a string where a number is expected
    local function addNumbers(a, b)
        return a + b
    end
    
    addNumbers(5, "ten")
end

-- Test 7: Deep stack trace
TEST_ERRORS.stack_trace = function()
    Log("Testing stack trace reporting...")
    
    -- Create a chain of function calls to generate a stack trace
    local function level3()
        -- Intentional error
        local x = nil
        return x.value
    end
    
    local function level2()
        return level3()
    end
    
    local function level1()
        return level2()
    end
    
    level1()
end

-- Called when the console is fully loaded and ready
function OnConsoleReady()
    Log("Error test script ready - use the test_errors command to test error handling")
    
    -- Register commands for testing different error types
    RegisterCommand(
        "test_errors",
        "Test error handling",
        "test_errors [error_type]",
        function(args)
            if #args == 0 then
                Log("Available error types: nil_call, nil_index, nil_math, type_mismatch, bad_concat, arg_type, stack_trace")
                return
            end
            
            local errorType = args[1]
            if TEST_ERRORS[errorType] then
                TEST_ERRORS[errorType]()
            else
                Log("Unknown error type: " .. errorType)
                Log("Available error types: nil_call, nil_index, nil_math, type_mismatch, bad_concat, arg_type, stack_trace")
            end
        end
    )
end
