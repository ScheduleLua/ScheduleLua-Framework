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
                
                LuaUtility.Log($"Transaction: {transactionName} - {quantity} x {FormatMoney(unitAmount)} = {FormatMoney(totalAmount)}");
                LuaUtility.Log($"Payment method: {(useOnlineBalance ? "Online Balance" : "Cash")}");
                
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
        /// Sets the ATM deposit limit using Harmony transpiler patches
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
                _atmDepositLimit = amount;
                LuaUtility.Log($"Setting ATM deposit limit to: {_atmDepositLimit}");

                // If we haven't applied Harmony patches yet, do so now
                if (!_atmLimitPatchesApplied)
                {
                    ApplyATMHarmonyPatches();
                }

                // Try to modify any existing ATM instances as a backup
                ModifyExistingATMInstances();

                return true;
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error setting ATM deposit limit: {ex.Message}");
                return false;
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
                    LuaUtility.Log("Created Harmony instance for ATM deposit limit patching");
                }

                // Patch ATMInterface.remainingAllowedDeposit property
                MethodInfo remainingAllowedDepositMethod = typeof(ATMInterface)
                    .GetProperty("remainingAllowedDeposit", BindingFlags.NonPublic | BindingFlags.Static)
                    ?.GetGetMethod(true);

                if (remainingAllowedDepositMethod != null)
                {
                    _harmonyInstance.Patch(
                        remainingAllowedDepositMethod,
                        transpiler: new HarmonyMethod(typeof(EconomyAPI).GetMethod(nameof(TranspileRemainingAllowedDeposit), BindingFlags.Static | BindingFlags.NonPublic))
                    );
                    LuaUtility.Log("Patched ATMInterface.remainingAllowedDeposit");
                }
                else
                {
                    LuaUtility.LogWarning("Could not find ATMInterface.remainingAllowedDeposit method to patch");
                }

                // Patch ATMInterface.Update method
                MethodInfo updateMethod = typeof(ATMInterface).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
                if (updateMethod != null)
                {
                    _harmonyInstance.Patch(
                        updateMethod,
                        transpiler: new HarmonyMethod(typeof(EconomyAPI).GetMethod(nameof(TranspileUpdate), BindingFlags.Static | BindingFlags.NonPublic))
                    );
                    LuaUtility.Log("Patched ATMInterface.Update");
                }
                else
                {
                    LuaUtility.LogWarning("Could not find ATMInterface.Update method to patch");
                }
                
                // Patch ATMInterface.UpdateAvailableAmounts method
                MethodInfo updateAvailableAmountsMethod = typeof(ATMInterface).GetMethod("UpdateAvailableAmounts", BindingFlags.NonPublic | BindingFlags.Instance);
                if (updateAvailableAmountsMethod != null)
                {
                    _harmonyInstance.Patch(
                        updateAvailableAmountsMethod,
                        transpiler: new HarmonyMethod(typeof(EconomyAPI).GetMethod(nameof(TranspileUpdateAvailableAmounts), BindingFlags.Static | BindingFlags.NonPublic))
                    );
                    LuaUtility.Log("Patched ATMInterface.UpdateAvailableAmounts");
                }
                else
                {
                    LuaUtility.LogWarning("Could not find ATMInterface.UpdateAvailableAmounts method to patch");
                }

                _atmLimitPatchesApplied = true;
                LuaUtility.Log("Successfully applied all ATM deposit limit Harmony patches");
            }
            catch (Exception ex)
            {
                LuaUtility.LogError($"Error applying Harmony patches: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Transpiler for the remainingAllowedDeposit property to replace 10000f with our custom limit
        /// </summary>
        private static IEnumerable<CodeInstruction> TranspileRemainingAllowedDeposit(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 10000f)
                {
                    LuaUtility.Log($"Replacing 10000f with {_atmDepositLimit} in remainingAllowedDeposit");
                    yield return new CodeInstruction(OpCodes.Ldc_R4, _atmDepositLimit);
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        /// <summary>
        /// Transpiler for the Update method to replace 10000f with our custom limit
        /// </summary>
        private static IEnumerable<CodeInstruction> TranspileUpdate(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 10000f)
                {
                    LuaUtility.Log($"Replacing 10000f with {_atmDepositLimit} in Update");
                    yield return new CodeInstruction(OpCodes.Ldc_R4, _atmDepositLimit);
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        /// <summary>
        /// Transpiler for the UpdateAvailableAmounts method to replace 10000f with our custom limit
        /// </summary>
        private static IEnumerable<CodeInstruction> TranspileUpdateAvailableAmounts(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 10000f)
                {
                    LuaUtility.Log($"Replacing 10000f with {_atmDepositLimit} in UpdateAvailableAmounts");
                    yield return new CodeInstruction(OpCodes.Ldc_R4, _atmDepositLimit);
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        /// <summary>
        /// Attempts to modify any existing ATM instances' static fields as a backup approach
        /// </summary>
        private static void ModifyExistingATMInstances()
        {
            try
            {
                // Try to set the static field WeeklyDepositSum via reflection
                FieldInfo weeklyDepositSumField = typeof(ATM).GetField("WeeklyDepositSum", BindingFlags.Public | BindingFlags.Static);
                if (weeklyDepositSumField != null)
                {
                    // Don't change the current deposit sum, just read it for logging
                    float currentSum = (float)weeklyDepositSumField.GetValue(null);
                    LuaUtility.Log($"Current WeeklyDepositSum: {currentSum}");
                }

                // Update deposit limit text in all active ATM interfaces
                var atmInterfaces = UnityEngine.Object.FindObjectsOfType<ATMInterface>();
                if (atmInterfaces.Length > 0)
                {
                    foreach (var atmInterface in atmInterfaces)
                    {
                        try
                        {
                            // Find and update the deposit limit text
                            var depositLimitText = atmInterface.GetType()
                                .GetField("depositLimitText", BindingFlags.NonPublic | BindingFlags.Instance)
                                ?.GetValue(atmInterface) as UnityEngine.UI.Text;

                            if (depositLimitText != null)
                            {
                                string currentText = depositLimitText.text;
                                if (currentText.Contains("10000") || currentText.Contains("$10,000"))
                                {
                                    string formattedLimit = MoneyManager.FormatAmount(_atmDepositLimit);
                                    depositLimitText.text = currentText
                                        .Replace("10000", _atmDepositLimit.ToString())
                                        .Replace("$10,000", formattedLimit);
                                    LuaUtility.Log($"Updated ATM UI text to show new limit: {formattedLimit}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LuaUtility.LogWarning($"Error updating ATM interface UI: {ex.Message}");
                        }
                    }
                }
                else
                {
                    LuaUtility.Log("No active ATM interfaces found to update");
                }
            }
            catch (Exception ex)
            {
                LuaUtility.LogWarning($"Error in ModifyExistingATMInstances: {ex.Message}");
            }
        }
    }
}

