# Registry API for Lua Scripts

This document provides examples of how to use the Registry API from Lua scripts to work with items in Schedule I.

## Item Types Explained

Schedule I has three main types of items that you can work with in Lua:

1. **Regular Items** (ItemDefinition): Basic items with standard properties like name, description, stack limit, etc. Examples include baggie, jar, and other basic items.

2. **Quality Items** (QualityItemDefinition): Items that have a quality level associated with them. Quality can be Perfect, High, Medium, Low, or Poor. Most plant products like ogkush, bluedream, and harvested crops are quality items. The quality affects the item's value and effectiveness.

3. **Integer Items** (IntegerItemDefinition): Items that have a numeric value associated with them. This could represent durability, charges, or other countable properties. Examples include tools with durability or items with a number of uses remaining.

## Registry Ready Event

The Registry system is not immediately available when the game starts, particularly in the main menu, loading screens, or during scene transitions. Using Registry functions before they're available will cause errors. This section shows you how to properly access the Registry at the right time.

### Basic Registry Ready Event Usage

```lua
-- BEST PRACTICE: Initialize your mod with OnRegistryReady
OnRegistryReady(function()
    -- This code will run when the Registry is fully loaded and accessible
    print("Registry is now ready!")
    
    -- It's safe to access items here
    local ogkush = GetItem("ogkush")
    if ogkush then
        print("Found OG Kush: " .. ogkush.name)
        print("Stack limit: " .. ogkush.stackLimit)
    end
    
    -- Initialize your mod here
    InitializeMyModItems()
end)

-- Your initialization function
function InitializeMyModItems()
    -- Create and modify items here
    local customStrain = CreateItem(
        "mymod_whiterhino",
        "White Rhino",
        "A potent strain with high THC content",
        "Consumable",
        50
    )
    
    print("Custom items initialized!")
end
```

### Checking Registry Status Anytime

```lua
-- Check if Registry is ready before attempting operations
function TryModifyItems()
    if IsRegistryReady() then
        -- Safe to use Registry functions
        ModifyItem("jar", {stackLimit = 50})
        return true
    else
        print("Registry not ready yet, will try again later")
        return false
    end
end

-- EXAMPLE: Button click handler that needs Registry access
function OnSettingsButtonClicked()
    if not IsRegistryReady() then
        print("Game is still initializing, please wait...")
        return
    end
    
    -- Safe to access registry
    ApplyItemModifications()
end
```

### Combining with Other Events

```lua
-- Game time events combined with Registry readiness
local dayChangeRegistered = false

-- Set up day change listener when Registry is ready
OnRegistryReady(function()
    if not dayChangeRegistered then
        -- Register for time manager events
        OnDayChanged(function(oldDay, newDay)
            -- This will only run when both Registry is ready AND day changes
            UpdateItemPricesForNewDay(newDay)
        end)
        
        dayChangeRegistered = true
        print("Day change handler registered")
    end
end)

-- Update prices of items when day changes
function UpdateItemPricesForNewDay(day)
    -- Can safely use Registry functions here because OnRegistryReady
    -- guarantees we only get here when Registry is available
    local strainItems = GetItemsInCategory("Consumable")
    for i, item in pairs(strainItems) do
        -- Implement price fluctuation logic based on day
        -- ...
    end
    
    print("Updated prices for day: " .. day)
end
```

### Handling Scene Changes

The Registry might become unavailable during scene transitions. Here's how to handle that:

```lua
-- State tracking
local lastRegistryState = false
local pendingOperations = {}

-- Check Registry state periodically
function CheckRegistryStatus()
    local nowReady = IsRegistryReady()
    
    -- Detect changes in Registry availability
    if nowReady ~= lastRegistryState then
        if nowReady then
            print("Registry is now available")
            -- Process any pending operations
            ProcessPendingOperations()
        else
            print("Registry is no longer available (scene change?)")
        end
        
        lastRegistryState = nowReady
    end
    
    -- Check again after a delay
    Wait(2.0, CheckRegistryStatus)
end

-- Start monitoring Registry status
Wait(1.0, CheckRegistryStatus)

-- Queue operations for when Registry becomes available
function QueueOperation(opName, opFunc)
    if IsRegistryReady() then
        -- Execute immediately if Registry is ready
        print("Executing operation immediately: " .. opName)
        opFunc()
    else
        -- Queue for later execution
        print("Queuing operation: " .. opName)
        table.insert(pendingOperations, {name = opName, func = opFunc})
    end
end

-- Process operations that were queued while Registry was unavailable
function ProcessPendingOperations()
    print("Processing " .. #pendingOperations .. " pending operations")
    
    for i, op in ipairs(pendingOperations) do
        print("Executing: " .. op.name)
        op.func()
    end
    
    -- Clear the queue
    pendingOperations = {}
end

-- Example usage: Queue item creation
function CreateCustomItem()
    QueueOperation("Create custom item", function()
        local newItem = CreateItem(
            "mymod_special_jar", 
            "Special Jar", 
            "A special container for precious items", 
            "Misc", 
            10
        )
        print("Created special jar item")
    end)
end
```

### Best Practices for Registry Access

1. **Always use OnRegistryReady for initialization**:
   ```lua
   OnRegistryReady(function()
       InitializeYourMod()
   end)
   ```

2. **Check IsRegistryReady before any manual Registry operations**:
   ```lua
   function SomeFunction()
       if not IsRegistryReady() then
           print("Registry not available yet")
           return false
       end
       
       -- Safe to use Registry functions
       return true
   end
   ```

3. **Handle Registry becoming unavailable during scene changes**:
   ```lua
   function SafeOperation()
       if not IsRegistryReady() then
           print("Can't perform operation now, Registry unavailable")
           return
       end
       
       -- Safe to proceed
   end
   ```

4. **Use error handling when necessary**:
   ```lua
   function SafeGetItem(itemId)
       if not IsRegistryReady() then
           return nil
       end
       
       local success, result = pcall(function()
           return GetItem(itemId)
       end)
       
       if success then
           return result
       else
           print("Error getting item: " .. result)
           return nil
       end
   end
   ```

## Getting Items

```lua
-- Get an item by ID
local item = GetItem("ogkush")
if item then
    print("Found item: " .. item.name)
    print("Stack limit: " .. item.stackLimit)
end

-- Check if an item exists
if DoesItemExist("jar") then
    print("Jar exists in the registry")
end

-- Get all items in a category
local consumables = GetItemsInCategory("Consumable")
for i, item in pairs(consumables) do
    print(item.name)
end

-- Get all quality levels
local qualities = GetAllQualities()
for i, quality in pairs(qualities) do
    print(quality.name .. " (level " .. quality.level .. ")")
end

-- Get all items
local allItems = GetAllItems()
print("Total items: " .. #allItems)
```

## Creating Items

```lua
-- Create a basic item
local newItem = CreateItem(
    "mymod_baggie_xl",   -- ID
    "XL Baggie",         -- Name
    "A larger baggie for storing more product", -- Description
    "Misc",              -- Category
    50                   -- Stack limit
)

-- Create a quality item
local qualityItem = CreateQualityItem(
    "mymod_purplehaze",
    "Purple Haze",
    "A legendary strain with vibrant purple coloration",
    "Consumable",
    20,
    "Medium"            -- Default quality
)

-- Create an integer item
local intItem = CreateIntegerItem(
    "mymod_trimming_scissors",
    "Durable Trimming Scissors",
    "Extra durable scissors for trimming plants",
    "Tool",
    5,
    100                 -- Default durability value
)
```

## Modifying Items

```lua
-- Modify an existing item's properties
local success = ModifyItem("ogkush", {
    name = "Super OG Kush",
    description = "A supercharged version of OG Kush",
    stackLimit = 40,
    keywords = {"cannabis", "kush", "premium", "super"}
})

-- Modify a quality item
ModifyItem("bluedream", {
    defaultQuality = "High"
})

-- Change legal status
ModifyItem("ogkush_seed", {
    legalStatus = "Illegal"
})
```

## Working with Item Instances

```lua
-- Create an item instance with quantity
local kushInstance = CreateItemInstance("ogkush", 5)

-- Add item to player inventory
if kushInstance then
    local addedToInventory = AddItemToPlayerInventory(kushInstance)
    if addedToInventory then
        print("Added 5 OG Kush to inventory")
    else
        print("Failed to add OG Kush to inventory (might be full)")
    end
end

-- Get a direct reference to work with item (advanced usage)
local jar = GetItemDirect("jar")
local proxy = CreateItemProxy(jar)
if proxy then
    -- Modify the item definition
    proxy.Name = "Super Jar"
    proxy.StackLimit = 30
    
    -- Create an instance from the modified item
    local instance = proxy.CreateInstance(3)
    AddItemToPlayerInventory(instance)
end

-- Working with quality items
local kushInstance = CreateItemInstance("ogkush", 1)
local instanceProxy = CreateItemInstanceProxy(kushInstance)
if instanceProxy and instanceProxy.IsQualityItem() then
    instanceProxy.SetQuality("Perfect")
    AddItemToPlayerInventory(kushInstance)
end

-- Working with integer items
local scissorsInstance = CreateItemInstance("scissors", 1)
local scissorsProxy = CreateItemInstanceProxy(scissorsInstance)
if scissorsProxy and scissorsProxy.IsIntegerItem() then
    scissorsProxy.SetValue(75)  -- Set durability to 75%
    AddItemToPlayerInventory(scissorsInstance)
end

-- Working with cash
local cashInstance = CreateItemInstance("cash", 1)
local cashProxy = CreateItemInstanceProxy(cashInstance)
if cashProxy and cashProxy.IsCashItem() then
    cashProxy.SetBalance(1000)  -- Set money amount to $1000
    AddItemToPlayerInventory(cashInstance)
end
```

## Example: Creating a Custom Item Mod

```lua
-- Register initialization function to run when the Registry is ready
OnRegistryReady(function()
    -- Create a new custom item
    local customStrain = CreateItem(
        "mymod_whiterhino",
        "White Rhino",
        "A potent strain with high THC content",
        "Consumable",
        50
    )
    
    -- Create a high-quality version
    local premiumStrain = CreateQualityItem(
        "mymod_whiterhino_premium",
        "Premium White Rhino",
        "The premium version of White Rhino",
        "Consumable",
        10,
        "Perfect"
    )
    
    -- Add the items to player inventory for testing
    local basicInstance = CreateItemInstance("mymod_whiterhino", 10)
    local premiumInstance = CreateItemInstance("mymod_whiterhino_premium", 2)
    
    AddItemToPlayerInventory(basicInstance)
    AddItemToPlayerInventory(premiumInstance)
    
    print("Custom strain mod initialized!")
end)
```

## Example: Modifying How Items Work

```lua
-- Make all strains stack higher when Registry is ready
OnRegistryReady(function()
    BuffStrainStacks()
    FixBaggieCapacity()
end)

-- Make all strains stack higher
function BuffStrainStacks()
    local allItems = GetAllItems()
    local count = 0
    
    for i, item in pairs(allItems) do
        -- Look for items that might be cannabis strains
        if string.find(item.id, "kush") or 
           string.find(item.id, "dream") or 
           string.find(item.id, "haze") then
            if item.stackLimit < 100 then
                ModifyItem(item.id, {
                    stackLimit = 100
                })
                count = count + 1
            end
        end
    end
    
    print("Modified " .. count .. " strain items to have larger stacks")
end

-- Fix an issue with baggies
function FixBaggieCapacity()
    if DoesItemExist("baggie") then
        ModifyItem("baggie", {
            stackLimit = 30,  -- Increase stack limit
            description = "A small plastic bag for storing product. Now holds more!"
        })
        print("Increased baggie capacity")
    end
end
``` 