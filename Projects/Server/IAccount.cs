using System;

namespace Server.Accounting;

public static class AccountGold
{
    public static bool Enabled { get; private set; }

    /// <summary>
    ///     This amount specifies the value at which point Gold turns to Platinum.
    ///     By default, when 1,000,000,000 Gold is accumulated, it will transform
    ///     into 1 Platinum.
    ///     !!! WARNING !!!
    ///     The client is designed to perceive the currency threshold at 1,000,000,000
    ///     if you change this, it may cause unexpected results when using secure trading.
    /// </summary>
    public static int CurrencyThreshold = 1000000000;

    /// <summary>
    ///     Enables or Disables automatic conversion of Gold and Checks to Bank Currency
    ///     when they are added to a bank box container.
    /// </summary>
    public static bool ConvertOnBank { get; private set; }

    /// <summary>
    ///     Enables or Disables automatic conversion of Gold and Checks to Bank Currency
    ///     when they are added to a secure trade container.
    /// </summary>
    public static bool ConvertOnTrade { get; private set; }

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetSetting("accountGold.enable", Core.TOL);
        ConvertOnBank = ServerConfiguration.GetSetting("accountGold.convertOnBank", true);
        ConvertOnTrade = ServerConfiguration.GetSetting("accountGold.convertOnTrade", false);
    }
}

public interface IGoldAccount
{
    /// <summary>
    ///     This amount represents the current amount of Gold owned by the player.
    ///     The value does not include the value of Platinum and ranges from
    ///     0 to 999,999,999 by default.
    /// </summary>
    [CommandProperty(AccessLevel.Administrator)]
    int TotalGold { get; }

    /// <summary>
    ///     This amount represents the current amount of Platinum owned by the player.
    ///     The value does not include the value of Gold and ranges from
    ///     0 to 2,147,483,647 by default.
    ///     One Platinum represents the value of CurrencyThreshold in Gold.
    /// </summary>
    [CommandProperty(AccessLevel.Administrator)]
    int TotalPlat { get; }

    /// <summary>
    ///     Attempts to deposit the given amount of Gold into this account.
    ///     If the given amount is greater than the CurrencyThreshold,
    ///     Platinum will be deposited to offset the difference.
    /// </summary>
    /// <param name="amount">Amount to deposit.</param>
    /// <returns>True if successful, false if amount given is less than or equal to zero.</returns>
    bool DepositGold(int amount);

    /// <summary>
    ///     Attempts to deposit the given amount of Platinum into this account.
    /// </summary>
    /// <param name="amount">Amount to deposit.</param>
    /// <returns>True if successful, false if amount given is less than or equal to zero.</returns>
    bool DepositPlat(int amount);

    /// <summary>
    ///     Attempts to withdraw the given amount of Gold from this account.
    ///     If the given amount is greater than the CurrencyThreshold,
    ///     Platinum will be withdrawn to offset the difference.
    /// </summary>
    /// <param name="amount">Amount to withdraw.</param>
    /// <returns>True if successful, false if balance was too low.</returns>
    bool WithdrawGold(int amount);

    /// <summary>
    ///     Attempts to withdraw the given amount of Platinum from this account.
    /// </summary>
    /// <param name="amount">Amount to withdraw.</param>
    /// <returns>True if successful, false if balance was too low.</returns>
    bool WithdrawPlat(int amount);

    /// <summary>
    ///     Returns total gold inclusive of platinum, capped to Int32.
    ///     This is strictly for backwards compatibility
    /// </summary>
    /// <returns>Total gold, capped at Int32.MaxValue</returns>
    long GetTotalGold();
}

public interface IAccount : IGoldAccount, IComparable<IAccount>
{
    string Username { get; set; }
    string Email { get; set; }
    AccessLevel AccessLevel { get; set; }

    int Length { get; }
    int Limit { get; }
    int Count { get; }
    Mobile this[int index] { get; set; }

    void Delete();
    void SetPassword(string password);
    bool CheckPassword(string password);
}
