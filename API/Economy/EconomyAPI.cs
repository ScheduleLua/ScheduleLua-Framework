using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using ScheduleOne.Money;
using ScheduleLua.API.Core;
using ScheduleOne.UI.ATM;
using HarmonyLib;
using ScheduleOne.UI;
using ScheduleOne.DevUtilities;
using UnityEngine.UI;

namespace ScheduleLua.API.Economy
{
    /// <summary>
    /// Provides Lua API access to economy-related functionality in Schedule I
    /// </summary>
    public static class EconomyAPI
    {
        private static float _atmDepositLimit = 10000f;
        private static bool _atmLimitPatchesApplied = false;
        private static HarmonyLib.Harmony _harmonyInstance = null;

        /// <summary>
        /// Register all economy-related API functions with the Lua engine
        /// </summary>
        public static void RegisterAPI(Script luaEngine)
        {
            if (luaEngine == null)
                throw new ArgumentNullException(nameof(luaEngine));

            // Money management functions
            luaEngine.Globals["GetPlayerCash"] = (Func<float>)GetPlayerCash;
            luaEngine.Globals["GetPlayerOnlineBalance"] = (Func<float>)GetPlayerOnlineBalance;
            luaEngine.Globals["GetLifetimeEarnings"] = (Func<float>)GetLifetimeEarnings;
            luaEngine.Globals["AddPlayerCash"] = (Func<float, bool>)AddPlayerCash;
            luaEngine.Globals["RemovePlayerCash"] = (Func<float, bool>)RemovePlayerCash;
            luaEngine.Globals["AddOnlineBalance"] = (Func<float, bool>)AddOnlineBalance;
            luaEngine.Globals["RemoveOnlineBalance"] = (Func<float, bool>)RemoveOnlineBalance;
            luaEngine.Globals["FormatMoney"] = (Func<float, string>)FormatMoney;
            luaEngine.Globals["GetNetWorth"] = (Func<float>)GetNetWorth;
            luaEngine.Globals["CreateTransaction"] = (Func<string, float, int, bool, bool>)CreateTransaction;
            luaEngine.Globals["CheckIfCanAfford"] = (Func<float, bool>)CheckIfCanAfford;

            // ATM limit functions
            luaEngine.Globals["GetATMDepositLimit"] = (Func<float>)GetATMDepositLimit;
            luaEngine.Globals["SetATMDepositLimit"] = (Func<float, bool>)SetATMDepositLimit;
        }

        /// <summary>
        /// Gets the player's current cash balance
        /// </summary>
        /// <returns>Current cash balance</returns>
        public static float GetPlayerCash()
        {
            try
            {
                if (MoneyManager.Instance == null)
                {
                    LuaUtility.LogWarning("MoneyManager instance not found");
                    return 0f;
                }

                return MoneyManager.Instance.cashBalance;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error getting player cash", ex);
                return 0f;
            }
        }

        /// <summary>
        /// Gets the player's current online balance
        /// </summary>
        /// <returns>Current online balance</returns>
        public static float GetPlayerOnlineBalance()
        {
            try
            {
                if (MoneyManager.Instance == null)
                {
                    LuaUtility.LogWarning("MoneyManager instance not found");
                    return 0f;
                }

                return MoneyManager.Instance.onlineBalance;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error getting online balance", ex);
                return 0f;
            }
        }

        /// <summary>
        /// Gets the player's lifetime earnings
        /// </summary>
        /// <returns>Lifetime earnings</returns>
        public static float GetLifetimeEarnings()
        {
            try
            {
                if (MoneyManager.Instance == null)
                {
                    LuaUtility.LogWarning("MoneyManager instance not found");
                    return 0f;
                }

                return MoneyManager.Instance.lifetimeEarnings;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error getting lifetime earnings", ex);
                return 0f;
            }
        }

        /// <summary>
        /// Adds cash to the player's on-hand balance
        /// </summary>
        /// <param name="amount">Amount to add</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool AddPlayerCash(float amount)
        {
            try
            {
                if (MoneyManager.Instance == null)
                {
                    LuaUtility.LogWarning("MoneyManager instance not found");
                    return false;
                }

                if (amount <= 0)
                {
                    LuaUtility.LogWarning("Amount must be positive");
                    return false;
                }

                MoneyManager.Instance.ChangeCashBalance(amount);
                return true;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error adding player cash", ex);
                return false;
            }
        }

        /// <summary>
        /// Removes cash from the player's on-hand balance
        /// </summary>
        /// <param name="amount">Amount to remove</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool RemovePlayerCash(float amount)
        {
            try
            {
                if (MoneyManager.Instance == null)
                {
                    LuaUtility.LogWarning("MoneyManager instance not found");
                    return false;
                }

                if (amount <= 0)
                {
                    LuaUtility.LogWarning("Amount must be positive");
                    return false;
                }

                if (MoneyManager.Instance.cashBalance < amount)
                {
                    LuaUtility.LogWarning("Not enough cash");
                    return false;
                }

                MoneyManager.Instance.ChangeCashBalance(-amount);
                return true;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error removing player cash", ex);
                return false;
            }
        }

        /// <summary>
        /// Adds to the player's online balance
        /// </summary>
        /// <param name="amount">Amount to add</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool AddOnlineBalance(float amount)
        {
            try
            {
                if (MoneyManager.Instance == null)
                {
                    LuaUtility.LogWarning("MoneyManager instance not found");
                    return false;
                }

                if (amount <= 0)
                {
                    LuaUtility.LogWarning("Amount must be positive");
                    return false;
                }

                float currentBalance = MoneyManager.Instance.onlineBalance;
                MoneyManager.Instance.onlineBalance = currentBalance + amount;

                // Call MinPass and OnlineBalanceDisplay.SetBalance to update UI variables
                MoneyManager.Instance.SendMessage("MinPass", SendMessageOptions.DontRequireReceiver);
                Singleton<HUD>.Instance.OnlineBalanceDisplay.SetBalance(MoneyManager.Instance.onlineBalance);

                return true;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error adding online balance", ex);
                return false;
            }
        }

        /// <summary>
        /// Removes from the player's online balance
        /// </summary>
        /// <param name="amount">Amount to remove</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool RemoveOnlineBalance(float amount)
        {
            try
            {
                if (MoneyManager.Instance == null)
                {
                    LuaUtility.LogWarning("MoneyManager instance not found");
                    return false;
                }

                if (amount <= 0)
                {
                    LuaUtility.LogWarning("Amount must be positive");
                    return false;
                }

                float currentBalance = MoneyManager.Instance.onlineBalance;
                if (currentBalance < amount)
                {
                    LuaUtility.LogWarning("Not enough online balance");
                    return false;
                }

                // Since there's no direct method, modify the field directly
                MoneyManager.Instance.onlineBalance = currentBalance - amount;

                // Call MinPass to update UI variables 
                MoneyManager.Instance.SendMessage("MinPass", SendMessageOptions.DontRequireReceiver);

                return true;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error removing online balance", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets the total net worth (cash + online balance)
        /// </summary>
        /// <returns>Total net worth</returns>
        public static float GetNetWorth()
        {
            try
            {
                if (MoneyManager.Instance == null)
                {
                    LuaUtility.LogWarning("MoneyManager instance not found");
                    return 0f;
                }

                return MoneyManager.Instance.cashBalance + MoneyManager.Instance.onlineBalance;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error getting net worth", ex);
                return 0f;
            }
        }

        /// <summary>
        /// Formats a money amount for display
        /// </summary>
        /// <param name="amount">Amount to format</param>
        /// <returns>Formatted money string</returns>
        public static string FormatMoney(float amount)
        {
            try
            {
                // Use the static method rather than instance method
                return MoneyManager.FormatAmount(amount);
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error formatting money", ex);
                return "$" + amount.ToString("N2");
            }
        }

        /// <summary>
        /// Checks if the player can afford a given amount
        /// </summary>
        /// <param name="amount">Amount to check</param>
        /// <returns>True if the player can afford it, false otherwise</returns>
        public static bool CheckIfCanAfford(float amount)
        {
            try
            {
                if (MoneyManager.Instance == null)
                {
                    LuaUtility.LogWarning("MoneyManager instance not found");
                    return false;
                }

                return MoneyManager.Instance.cashBalance >= amount;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error checking if can afford", ex);
                return false;
            }
        }

        /// <summary>
        /// Creates a transaction, later on this should use the ItemRegistry to give the player an item
        /// </summary>
        /// <param name="transactionName">Name of the transaction</param>
        /// <param name="unitAmount">Unit price</param>
        /// <param name="quantity">Quantity of items</param>
        /// <param name="useOnlineBalance">Whether to charge from online balance (true) or cash (false)</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool CreateTransaction(string transactionName, float unitAmount, int quantity, bool useOnlineBalance)
        {
            try
            {
                if (MoneyManager.Instance == null)
                {
                    LuaUtility.LogWarning("MoneyManager instance not found");
                    return false;
                }

                if (string.IsNullOrEmpty(transactionName))
                {
                    LuaUtility.LogWarning("Transaction name cannot be empty");
                    return false;
                }

                if (quantity <= 0)
                {
                    LuaUtility.LogWarning("Quantity must be positive");
                    return false;
                }

                float totalAmount = unitAmount * quantity;

                // Check if player has enough funds in the selected payment method
                if (useOnlineBalance)
                {
                    if (MoneyManager.Instance.onlineBalance < totalAmount)
                    {
                        LuaUtility.LogWarning("Not enough online balance");
                        return false;
                    }

                    // Use online balance
                    float currentBalance = MoneyManager.Instance.onlineBalance;
                    MoneyManager.Instance.onlineBalance = currentBalance - totalAmount;

                    // Call MinPass to update UI variables
                    MoneyManager.Instance.SendMessage("MinPass", SendMessageOptions.DontRequireReceiver);
                }
                else
                {
                    // Use cash balance
                    if (MoneyManager.Instance.cashBalance < totalAmount)
                    {
                        LuaUtility.LogWarning("Not enough cash");
                        return false;
                    }

                    // Charge from cash balance
                    MoneyManager.Instance.ChangeCashBalance(-totalAmount);
                }

                return true;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError("Error creating transaction", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets the current ATM deposit limit
        /// </summary>
        /// <returns>Current ATM deposit limit</returns>
        public static float GetATMDepositLimit()
        {
            return _atmDepositLimit;
        }

        /// <summary>
        /// Sets the ATM deposit limit using Harmony method patches
        /// </summary>
        /// <param name="amount">New deposit limit amount</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool SetATMDepositLimit(float amount)
        {
            try
            {
                if (amount <= 0)
                {
                    LuaUtility.LogWarning("ATM deposit limit must be positive");
                    return false;
                }

                // Store the new limit
                float previousLimit = _atmDepositLimit;
                _atmDepositLimit = amount;

                // If we haven't applied Harmony patches yet, do so now
                if (!_atmLimitPatchesApplied)
                {
                    ApplyATMHarmonyPatches();
                }
                else
                {
                    // If patches were already applied, we need to refresh them to update any cached values
                    // This is especially important for static values that might have been compiled into IL
                    RefreshHarmonyPatches();
                }

                // Directly update the WeeklyDepositSum without percentage scaling
                UpdateWeeklyDepositSum(previousLimit);

                // Force refresh any open ATM interfaces
                ForceRefreshATMInterfaces();

                // Persist the new limit to ensure it survives scene changes
                PersistDepositLimitValue();

                return true;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error setting ATM deposit limit: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Refreshes Harmony patches when changing limit at runtime to ensure all cached values are updated
        /// </summary>
        private static void RefreshHarmonyPatches()
        {
            try
            {
                // First, unpatch any existing patches to avoid duplicates
                if (_harmonyInstance != null)
                {
                    // Re-patch the direct UI update methods for immediate effect
                    UpdateUIRelatedPatches();
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error refreshing Harmony patches: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates UI-related patches to refresh with new limit value
        /// </summary>
        private static void UpdateUIRelatedPatches()
        {
            try
            {
                // This is for when we change the limit while the game is running
                // We need to make sure UI-specific methods are properly updated

                // Update the Update method in ATMInterface to reflect the new limit
                MethodInfo updateMethod = typeof(ATMInterface).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
                if (updateMethod != null)
                {
                    _harmonyInstance.Patch(
                        updateMethod,
                        postfix: new HarmonyMethod(typeof(EconomyAPI).GetMethod(nameof(Postfix_ForceUpdateATMUI),
                            BindingFlags.Static | BindingFlags.NonPublic))
                    );
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error updating UI patches: {ex.Message}");
            }
        }

        /// <summary>
        /// Ensures the deposit limit value persists across scene changes
        /// </summary>
        private static void PersistDepositLimitValue()
        {
            try
            {
                // We don't have direct access to save functionality, but we can make the limit "sticky"
                // by ensuring direct field updates and any available persistence methods

                // Update the static fields that might cache the limit value
                foreach (var field in typeof(ATMInterface).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    if (field.FieldType == typeof(float) &&
                        (field.Name.Contains("limit") || field.Name.Contains("Limit") || field.Name.Contains("max") || field.Name.Contains("Max")))
                    {
                        try
                        {
                            // Try to update any static fields that might store the limit
                            var currentValue = (float)field.GetValue(null);
                            if (Math.Abs(currentValue - 10000f) < 0.1f)
                            {
                                field.SetValue(null, _atmDepositLimit);
                            }
                        }
                        catch (Exception ex)
                        {
                            LuaUtility.LogWarning($"Could not update field {field.Name}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error persisting deposit limit: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the WeeklyDepositSum value while maintaining the actual deposited amount
        /// </summary>
        private static void UpdateWeeklyDepositSum(float previousLimit)
        {
            try
            {
                // Get the current deposit sum
                float currentSum = ATM.WeeklyDepositSum;

                // Only adjust the sum if we're lowering the limit and the current sum exceeds it
                if (currentSum > _atmDepositLimit)
                {
                    float newSum = _atmDepositLimit;
                    ATM.WeeklyDepositSum = newSum;
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error updating WeeklyDepositSum: {ex.Message}");
            }
        }

        /// <summary>
        /// Force refreshes all active ATM interfaces to use the new limit
        /// </summary>
        private static void ForceRefreshATMInterfaces()
        {
            try
            {
                var atmInterfaces = UnityEngine.Object.FindObjectsOfType<ATMInterface>();
                if (atmInterfaces == null || atmInterfaces.Length == 0)
                {
                    return;
                }

                foreach (var atmInterface in atmInterfaces)
                {
                    try
                    {
                        // Get the depositLimitText field
                        FieldInfo depositLimitTextField = atmInterface.GetType().GetField("depositLimitText",
                            BindingFlags.NonPublic | BindingFlags.Instance);

                        if (depositLimitTextField != null)
                        {
                            Text depositLimitText = depositLimitTextField.GetValue(atmInterface) as Text;
                            if (depositLimitText != null)
                            {
                                // Update the text to show the new limit
                                string formattedText = MoneyManager.FormatAmount(ATM.WeeklyDepositSum) + " / " +
                                    MoneyManager.FormatAmount(_atmDepositLimit);

                                depositLimitText.text = formattedText;

                                // Update the color
                                depositLimitText.color = (ATM.WeeklyDepositSum >= _atmDepositLimit)
                                    ? new Color32(255, 75, 75, 255)
                                    : Color.white;
                            }
                        }

                        // Force reset and reapply the entire interface to ensure all text elements are updated
                        // Try setting isOpen to false then back to true
                        PropertyInfo isOpenProp = atmInterface.GetType().GetProperty("isOpen",
                            BindingFlags.Public | BindingFlags.Instance);

                        if (isOpenProp != null && isOpenProp.CanRead)
                        {
                            bool isCurrentlyOpen = (bool)isOpenProp.GetValue(atmInterface);

                            if (isCurrentlyOpen)
                            {
                                // If it's open, try calling internal refresh methods
                                MethodInfo updateMethod = atmInterface.GetType().GetMethod("Update",
                                    BindingFlags.NonPublic | BindingFlags.Instance);

                                if (updateMethod != null)
                                {
                                    // Force run a complete update cycle
                                    updateMethod.Invoke(atmInterface, null);
                                }
                            }
                        }

                        // Update deposit button states
                        FieldInfo depositButtonField = atmInterface.GetType().GetField("menu_DepositButton",
                            BindingFlags.NonPublic | BindingFlags.Instance);

                        if (depositButtonField != null)
                        {
                            Button depositButton = depositButtonField.GetValue(atmInterface) as Button;
                            if (depositButton != null)
                            {
                                // Update button interactable state based on new limit
                                depositButton.interactable = ATM.WeeklyDepositSum < _atmDepositLimit;
                            }
                        }

                        // Try to update any amount buttons if on the amount selection screen
                        var amountButtons = atmInterface.GetType().GetField("amountButtons",
                            BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(atmInterface) as List<Button>;

                        if (amountButtons != null)
                        {
                            MethodInfo updateAvailableAmountsMethod = atmInterface.GetType().GetMethod("UpdateAvailableAmounts",
                                BindingFlags.NonPublic | BindingFlags.Instance);

                            if (updateAvailableAmountsMethod != null)
                            {
                                // Force calling the UpdateAvailableAmounts method to refresh button states
                                updateAvailableAmountsMethod.Invoke(atmInterface, null);
                            }

                            // Also force the label text update
                            Text amountLabelText = atmInterface.GetType().GetField("amountLabelText",
                                BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(atmInterface) as Text;

                            if (amountLabelText != null)
                            {
                                // Force refresh any labels in the amount selection screen
                                MethodInfo defaultAmountSelectionMethod = atmInterface.GetType().GetMethod("DefaultAmountSelection",
                                    BindingFlags.NonPublic | BindingFlags.Instance);

                                if (defaultAmountSelectionMethod != null)
                                {
                                    defaultAmountSelectionMethod.Invoke(atmInterface, null);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LuaUtility.LogError($"Error refreshing ATM interface: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in ForceRefreshATMInterfaces: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies Harmony patches to replace the hardcoded 10000 value in ATM and ATMInterface classes
        /// </summary>
        private static void ApplyATMHarmonyPatches()
        {
            try
            {
                // Create a Harmony instance if we don't have one yet
                if (_harmonyInstance == null)
                {
                    _harmonyInstance = new HarmonyLib.Harmony("com.schedulelua.atm.depositlimit");
                }

                // ** Patch 1: Patch the remainingAllowedDeposit property **
                try
                {
                    PropertyInfo remainingAllowedDepositProperty = typeof(ATMInterface).GetProperty("remainingAllowedDeposit",
                        BindingFlags.NonPublic | BindingFlags.Static);

                    if (remainingAllowedDepositProperty != null)
                    {
                        MethodInfo remainingAllowedDepositGetter = remainingAllowedDepositProperty.GetGetMethod(true);
                        if (remainingAllowedDepositGetter != null)
                        {
                            _harmonyInstance.Patch(
                                remainingAllowedDepositGetter,
                                prefix: new HarmonyMethod(typeof(EconomyAPI).GetMethod(nameof(Prefix_RemainingAllowedDeposit),
                                    BindingFlags.Static | BindingFlags.NonPublic))
                            );
                        }
                        else
                        {
                            LuaUtility.LogWarning("Could not find getter for remainingAllowedDeposit property");
                        }
                    }
                    else
                    {
                        LuaUtility.LogWarning("Could not find remainingAllowedDeposit property");
                    }
                }
                catch (Exception ex)
                {
                    LuaUtility.LogError($"Error patching remainingAllowedDeposit: {ex.Message}");
                }

                // ** Patch 2: Patch all methods in ATMInterface that use the 10000 limit **
                // This covers Update, UpdateAvailableAmounts and other methods
                try
                {
                    var methods = typeof(ATMInterface).GetMethods(BindingFlags.Instance | BindingFlags.Static |
                        BindingFlags.Public | BindingFlags.NonPublic);

                    foreach (var method in methods)
                    {
                        if (method.DeclaringType == typeof(ATMInterface))
                        {
                            try
                            {
                                _harmonyInstance.Patch(
                                    method,
                                    transpiler: new HarmonyMethod(typeof(EconomyAPI).GetMethod(nameof(Transpile_ReplaceATMLimit),
                                        BindingFlags.Static | BindingFlags.NonPublic))
                                );
                            }
                            catch (Exception ex)
                            {
                                LuaUtility.LogWarning($"Error patching method {method.Name}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LuaUtility.LogError($"Error patching ATMInterface methods: {ex.Message}");
                }

                // ** Patch 3: Add specific postfixes for critical methods **

                // Patch the Update method with a direct postfix to force UI updates
                try
                {
                    MethodInfo updateMethod = typeof(ATMInterface).GetMethod("Update",
                        BindingFlags.NonPublic | BindingFlags.Instance);

                    if (updateMethod != null)
                    {
                        _harmonyInstance.Patch(
                            updateMethod,
                            postfix: new HarmonyMethod(typeof(EconomyAPI).GetMethod(nameof(Postfix_ForceUpdateATMUI),
                                BindingFlags.Static | BindingFlags.NonPublic))
                        );
                    }
                }
                catch (Exception ex)
                {
                    LuaUtility.LogError($"Error adding UI update postfix: {ex.Message}");
                }

                // Patch the ProcessTransaction method with postfix
                try
                {
                    MethodInfo processTransactionMethod = typeof(ATMInterface).GetMethod("ProcessTransaction",
                        BindingFlags.Instance | BindingFlags.NonPublic);

                    if (processTransactionMethod != null)
                    {
                        _harmonyInstance.Patch(
                            processTransactionMethod,
                            postfix: new HarmonyMethod(typeof(EconomyAPI).GetMethod(nameof(Postfix_ProcessTransaction),
                                BindingFlags.Static | BindingFlags.NonPublic))
                        );
                    }
                    else
                    {
                        LuaUtility.LogWarning("Could not find ProcessTransaction method");
                    }
                }
                catch (Exception ex)
                {
                    LuaUtility.LogError($"Error patching ProcessTransaction: {ex.Message}");
                }

                // ** Patch 4: Override the GetAmountFromIndex method **
                try
                {
                    MethodInfo getAmountFromIndexMethod = typeof(ATMInterface).GetMethod("GetAmountFromIndex",
                        BindingFlags.Static | BindingFlags.Public);

                    if (getAmountFromIndexMethod != null)
                    {
                        _harmonyInstance.Patch(
                            getAmountFromIndexMethod,
                            transpiler: new HarmonyMethod(typeof(EconomyAPI).GetMethod(nameof(Transpile_ReplaceATMLimit),
                                BindingFlags.Static | BindingFlags.NonPublic))
                        );
                    }
                }
                catch (Exception ex)
                {
                    LuaUtility.LogError($"Error patching GetAmountFromIndex: {ex.Message}");
                }

                // ** Patch 5: Also patch the ATM class itself since it may contain hardcoded 10000 values **
                try
                {
                    var atmMethods = typeof(ATM).GetMethods(BindingFlags.Instance | BindingFlags.Static |
                        BindingFlags.Public | BindingFlags.NonPublic);

                    foreach (var method in atmMethods)
                    {
                        if (method.DeclaringType == typeof(ATM))
                        {
                            try
                            {
                                _harmonyInstance.Patch(
                                    method,
                                    transpiler: new HarmonyMethod(typeof(EconomyAPI).GetMethod(nameof(Transpile_ReplaceATMLimit),
                                        BindingFlags.Static | BindingFlags.NonPublic))
                                );
                            }
                            catch (Exception ex)
                            {
                                LuaUtility.LogWarning($"Error patching ATM method {method.Name}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LuaUtility.LogError($"Error patching ATM class methods: {ex.Message}");
                }

                _atmLimitPatchesApplied = true;
                // LuaUtility.Log("Successfully applied all ATM deposit limit patches");
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error applying Harmony patches: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Prefix for the remainingAllowedDeposit property to override its value
        /// </summary>
        private static bool Prefix_RemainingAllowedDeposit(ref float __result)
        {
            __result = _atmDepositLimit - ATM.WeeklyDepositSum;
            // Skip original method
            return false;
        }

        /// <summary>
        /// Generic transpiler that replaces all instances of 10000f with our custom limit
        /// </summary>
        private static IEnumerable<CodeInstruction> Transpile_ReplaceATMLimit(IEnumerable<CodeInstruction> instructions)
        {
            var instructionsList = new List<CodeInstruction>(instructions);

            for (int i = 0; i < instructionsList.Count; i++)
            {
                if (instructionsList[i].opcode == OpCodes.Ldc_R4 &&
                    Math.Abs((float)instructionsList[i].operand - 10000f) < 0.01f)
                {
                    instructionsList[i] = new CodeInstruction(OpCodes.Ldc_R4, _atmDepositLimit);
                }
            }

            return instructionsList;
        }

        /// <summary>
        /// Postfix that forces UI text updates in the ATM interface
        /// </summary>
        private static void Postfix_ForceUpdateATMUI(ATMInterface __instance)
        {
            try
            {
                if (__instance == null)
                    return;

                // Get the isOpen property to check if the ATM is currently in use
                PropertyInfo isOpenProp = __instance.GetType().GetProperty("isOpen",
                    BindingFlags.Public | BindingFlags.Instance);

                bool isOpen = false;
                if (isOpenProp != null && isOpenProp.CanRead)
                {
                    isOpen = (bool)isOpenProp.GetValue(__instance);
                }

                if (!isOpen)
                    return;

                // Find and update the deposit limit text
                FieldInfo depositLimitTextField = __instance.GetType().GetField("depositLimitText",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (depositLimitTextField != null)
                {
                    Text depositLimitText = depositLimitTextField.GetValue(__instance) as Text;
                    if (depositLimitText != null)
                    {
                        // Force set the text using our custom limit
                        depositLimitText.text = MoneyManager.FormatAmount(ATM.WeeklyDepositSum) + " / " +
                            MoneyManager.FormatAmount(_atmDepositLimit);

                        // Update color based on whether limit is reached
                        depositLimitText.color = (ATM.WeeklyDepositSum >= _atmDepositLimit)
                            ? new Color32(255, 75, 75, 255)
                            : Color.white;
                    }
                }

                // Update deposit button state
                FieldInfo menuDepositButtonField = __instance.GetType().GetField("menu_DepositButton",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (menuDepositButtonField != null)
                {
                    Button menuDepositButton = menuDepositButtonField.GetValue(__instance) as Button;
                    if (menuDepositButton != null)
                    {
                        // Update button state based on our limit
                        menuDepositButton.interactable = ATM.WeeklyDepositSum < _atmDepositLimit;
                    }
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in Postfix_ForceUpdateATMUI: {ex.Message}");
            }
        }

        /// <summary>
        /// Postfix for the ProcessTransaction method to monitor and adjust WeeklyDepositSum
        /// </summary>
        private static void Postfix_ProcessTransaction(ATMInterface __instance, float amount, bool depositing)
        {
            try
            {
                if (depositing)
                {

                    // If depositing went over our limit, cap it
                    if (ATM.WeeklyDepositSum > _atmDepositLimit)
                    {
                        float oldSum = ATM.WeeklyDepositSum;
                        ATM.WeeklyDepositSum = _atmDepositLimit;
                    }

                    // Force refresh the UI
                    ForceRefreshATMInterfaces();
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error in Postfix_ProcessTransaction: {ex.Message}");
            }
        }
    }
}


