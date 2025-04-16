-- Export Item IDs Script
-- Exports all item IDs from the game registry to a text file

local initialized = false
local exportPath = "item_ids.txt"

-- Called when the script is first loaded
function Initialize()
    Log("=== Item ID Export Script ===")
    Log("Script initialized. Waiting for registry...")
end

-- Called when the registry is ready
function OnRegistryReady()
    Log("Registry is ready! Exporting item IDs...")
    
    if not initialized then
        ExportItemIds()
        initialized = true
    end
end

-- Find all item IDs by trying commonly used items and then looking for similar items
function GetAllItemIdsManually()
    local knownItems = {}
    local itemIds = {}
    
    -- Try accessing some known common items first
    local commonItems = {"cash", "baggie", "jar", "og_kush"}
    for _, id in ipairs(commonItems) do
        local item = GetItemDirect(id)
        if item then
            knownItems[id] = true
            table.insert(itemIds, {
                id = id, 
                name = GetItem(id).name or "Unknown",
                category = GetItem(id).category or "Unknown"
            })
            Log("Found item: " .. id)
        end
    end
    
    -- Try accessing all categories to gather more items
    local categories = {"Consumable", "Equipment", "Ingredient", "Seed", "Tool", "Fertilizer", "Processing", "Miscellaneous"}
    for _, category in ipairs(categories) do
        local categoryItems = GetItemsInCategory(category)
        if categoryItems then
            for _, item in pairs(categoryItems) do
                if item and item.id and not knownItems[item.id] then
                    knownItems[item.id] = true
                    table.insert(itemIds, {
                        id = item.id,
                        name = item.name or "Unknown",
                        category = item.category or category
                    })
                    Log("Found category item: " .. item.id)
                end
            end
        end
    end
    
    -- Additional approach: try sequentially increment numeric IDs
    -- (Uncomment if needed)
    --[[
    for i = 1, 1000 do 
        local testId = "item_" .. i
        if DoesItemExist(testId) then
            if not knownItems[testId] then
                knownItems[testId] = true
                local item = GetItem(testId)
                table.insert(itemIds, {
                    id = testId,
                    name = item.name or "Unknown",
                    category = item.category or "Unknown"
                })
                Log("Found sequential item: " .. testId)
            end
        end
    end
    --]]
    
    Log("Found " .. #itemIds .. " items through manual search")
    return itemIds
end

-- Export all item IDs to a text file
function ExportItemIds()
    -- Try to get all items from the registry first using standard API
    local allItems = GetAllItems()
    local itemsToExport = {}
    local itemCount = 0
    
    -- If we got a valid table with items in it, process them
    if type(allItems) == "table" then
        for _, item in pairs(allItems) do
            if item and item.id then
                itemCount = itemCount + 1
                table.insert(itemsToExport, {
                    id = item.id,
                    name = item.name or "Unknown",
                    category = item.category or "Unknown"
                })
            end
        end
    end
    
    Log("Found " .. itemCount .. " items in registry")
    
    -- If we didn't find any items through the standard API, try manual methods
    if itemCount == 0 then
        Log("Standard API returned 0 items. Trying manual methods...")
        itemsToExport = GetAllItemIdsManually()
        itemCount = #itemsToExport
    end
    
    -- Open file for writing
    local file = io.open(exportPath, "w")
    if not file then
        LogError("Failed to open file for writing: " .. exportPath)
        return
    end
    
    -- Write header
    file:write("# Schedule One Item IDs\n")
    file:write("# Generated on " .. os.date() .. "\n")
    file:write("# Total items: " .. itemCount .. "\n\n")
    
    -- Sort items by ID for easier reading
    table.sort(itemsToExport, function(a, b) return a.id < b.id end)
    
    -- Write item data
    file:write("ID, Name, Category\n")
    file:write("------------------------\n")
    for _, item in ipairs(itemsToExport) do
        file:write(item.id .. ", " .. item.name .. ", " .. item.category .. "\n")
    end
    
    -- Close file
    file:close()
    
    Log("Successfully exported " .. #itemsToExport .. " item IDs to " .. exportPath)
    Log("Export file location: " .. exportPath)
end

-- Called when script is unloaded
function Shutdown()
    Log("Item ID Export Script shutdown")
    return true
end

-- Print initial message
Log("Item ID Export Script loaded!")
