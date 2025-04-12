using MoonSharp.Interpreter;
using System;
using UnityEngine;
using ScheduleOne.Money;
using ScheduleLua.API.Core;

namespace ScheduleLua.API.Economy
{
    /// <summary>
    /// Provides Lua API access to economy-related functionality in Schedule I
    /// </summary>
    public static class EconomyAPI
    {
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

                // Since there's no direct method, modify the field directly
                float currentBalance = MoneyManager.Instance.onlineBalance;
                MoneyManager.Instance.onlineBalance = currentBalance + amount;
                
                // Call MinPass to update UI variables
                MoneyManager.Instance.SendMessage("MinPass", SendMessageOptions.DontRequireReceiver);
                
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
        /// Creates a transaction and adds it to the transaction history
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
    }
}
