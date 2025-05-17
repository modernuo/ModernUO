using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using ModernUO.CodeGeneratedEvents;
using Server.Accounting;
using Server.Engines.CharacterCreation;
using Server.Engines.Help;
using Server.Logging;
using Server.Network;
using Server.Regions;

namespace Server.Misc;

public static class AccountHandler
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(AccountHandler));

    private static int MaxAccountsPerIP;
    private static bool AutoAccountCreation;
    private static readonly bool RestrictDeletion = !TestCenter.Enabled;
    private static readonly TimeSpan DeleteDelay = TimeSpan.FromDays(7.0);
    private static bool PasswordCommandEnabled;

    private static Dictionary<IPAddress, int> m_IPTable;

    private static readonly SearchValues<char> ForbiddenChars = SearchValues.Create("<>:\"/\\|?*");

    public static AccessLevel LockdownLevel { get; set; }

    public static Dictionary<IPAddress, int> IPTable
    {
        get
        {
            if (m_IPTable == null)
            {
                m_IPTable = new Dictionary<IPAddress, int>();

                foreach (Account a in Accounts.GetAccounts())
                {
                    if (a.LoginIPs.Length > 0)
                    {
                        var ip = a.LoginIPs[0];
                        m_IPTable[ip] = (m_IPTable.TryGetValue(ip, out var value) ? value : 0) + 1;
                    }
                }
            }

            return m_IPTable;
        }
    }

    public static void Configure()
    {
        MaxAccountsPerIP = ServerConfiguration.GetOrUpdateSetting("accountHandler.maxAccountsPerIP", 1);
        AutoAccountCreation = ServerConfiguration.GetOrUpdateSetting("accountHandler.enableAutoAccountCreation", true);
        PasswordCommandEnabled = ServerConfiguration.GetOrUpdateSetting(
            "accountHandler.enablePlayerPasswordCommand",
            false
        );

        if (PasswordCommandEnabled)
        {
            CommandSystem.Register("Password", AccessLevel.Player, Password_OnCommand);
        }
    }

    public static void Initialize()
    {
        EventSink.AccountLogin += EventSink_AccountLogin;
    }

    [Usage("Password <newPassword> <repeatPassword>")]
    [Description(
        "Changes the password of the commanding players account. Requires the same C-class IP address as the account's creator."
    )]
    public static void Password_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (from.Account is not Account acct)
        {
            return;
        }

        var accessList = acct.LoginIPs;

        if (accessList.Length == 0)
        {
            return;
        }

        var ns = from.NetState;

        if (ns == null)
        {
            return;
        }

        if (e.Length == 0)
        {
            from.SendMessage("You must specify the new password.");
            return;
        }

        if (e.Length == 1)
        {
            from.SendMessage("To prevent potential typing mistakes, you must type the password twice. Use the format:");
            from.SendMessage("Password \"(newPassword)\" \"(repeated)\"");
            return;
        }

        var pass = e.GetString(0);
        var pass2 = e.GetString(1);

        if (pass != pass2)
        {
            from.SendMessage("The passwords do not match.");
            return;
        }

        var isSafe = true;

        for (var i = 0; isSafe && i < pass.Length; ++i)
        {
            isSafe = pass[i] >= 0x20 && pass[i] < 0x7F;
        }

        if (!isSafe)
        {
            from.SendMessage("That is not a valid password.");
            return;
        }

        try
        {
            var ipAddress = ns.Address;

            if (accessList[0].MatchClassC(ipAddress))
            {
                acct.SetPassword(pass);
                from.SendMessage("The password to your account has changed.");
            }
            else
            {
                var entry = PageQueue.GetEntry(from);

                if (entry != null)
                {
                    if (entry.Message.StartsWithOrdinal("[Automated: Change Password]"))
                    {
                        from.SendMessage("You already have a password change request in the help system queue.");
                    }
                    else
                    {
                        from.SendMessage("Your IP address does not match that which created this account.");
                    }
                }
                else if (PageQueue.CheckAllowedToPage(from))
                {
                    from.SendMessage(
                        "Your IP address does not match that which created this account.  A page has been entered into the help system on your behalf."
                    );

                    /* The next available Counselor/Game Master will respond as soon as possible.
                     * Please check your Journal for messages every few minutes.
                     */
                    from.SendLocalizedMessage(501234, "", 0x35);

                    PageQueue.Enqueue(
                        new PageEntry(
                            from,
                            $"[Automated: Change Password]<br>Desired password: {pass}<br>Current IP address: {ipAddress}<br>Account IP address: {accessList[0]}",
                            PageType.Account
                        )
                    );
                }
            }
        }
        catch
        {
            // ignored
        }
    }

    public static void DeleteRequest(NetState state, int index)
    {
        if (state.Account is not Account acct)
        {
            state.Disconnect("Attempted to delete a character but the account could not be found.");
            return;
        }

        DeleteResultType res;

        if (index < 0 || index >= acct.Length)
        {
            res = DeleteResultType.BadRequest;
        }
        else
        {
            var m = acct[index];

            if (m == null)
            {
                res = DeleteResultType.CharNotExist;
            }
            else if (m.NetState != null)
            {
                res = DeleteResultType.CharBeingPlayed;
            }
            else if (acct.AccessLevel == AccessLevel.Player && RestrictDeletion && Core.Now < m.Created + DeleteDelay)
            {
                res = DeleteResultType.CharTooYoung;
            }
            // Don't need to check current location, if netstate is null, they're logged out
            else if (
                m.AccessLevel == AccessLevel.Player &&
                Region.Find(m.LogoutLocation, m.LogoutMap).IsPartOf<JailRegion>()
            )
            {
                res = DeleteResultType.BadRequest;
            }
            else
            {
                state.LogInfo($"Deleting character {index} ({m.Serial})");

                acct.Comments.Add(new AccountComment("System", $"Character #{index + 1} {m} deleted by {state}"));

                m.Delete();

                state.SendCharacterListUpdate(acct);
                return;
            }
        }

        state.SendCharacterDeleteResult(res);
        state.SendCharacterListUpdate(acct);
    }

    public static bool CanCreate(IPAddress ip) =>
        !IPTable.TryGetValue(ip, out var result) || result < MaxAccountsPerIP;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidUsername(ReadOnlySpan<char> username) =>
        username.Length > 0 &&
        // Usernames must not start with a space, end with a space, or end with a period
        !username.StartsWith(' ') && !username.EndsWith(' ') && !username.EndsWith('.') &&
        // Usernames must only contain characters [0x20 -> 0x7E], and not contain any forbidden characters
        !username.ContainsAnyExceptInRange((char)0x20, (char)0x7E) &&
        !username.ContainsAny(ForbiddenChars);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidPassword(ReadOnlySpan<char> password) =>
        password.Length > 0 &&
        // Passwords must have characters [0x20 -> 0x7E]
        !password.ContainsAnyExceptInRange((char)0x20, (char)0x7E);

    private static Account CreateAccount(NetState state, string username, string password)
    {
        if (!IsValidUsername(username) || !IsValidPassword(password))
        {
            return null;
        }

        if (!CanCreate(state.Address))
        {
            logger.Information(
                $"Login: {{NetState}} Account '{{Username}}' not created, ip already has {{AccountCount}} account{(MaxAccountsPerIP == 1 ? "" : "s")}.",
                state,
                username,
                MaxAccountsPerIP
            );

            return null;
        }

        logger.Information("Login: {NetState}: Creating new account '{Username}'", state, username);

        return new Account(username, password);
    }

    public static void EventSink_AccountLogin(AccountLoginEventArgs e)
    {
        var un = e.Username;
        var pw = e.Password;

        e.Accepted = false;

        if (Accounts.GetAccount(un) is not Account acct)
        {
            // To prevent someone from making an account of just '' or a bunch of meaningless spaces
            if (AutoAccountCreation && !string.IsNullOrWhiteSpace(un))
            {
                e.State.Account = acct = CreateAccount(e.State, un, pw);
                e.Accepted = acct?.CheckAccess(e.State) ?? false;

                if (!e.Accepted)
                {
                    e.RejectReason = ALRReason.BadComm;
                }
            }
            else
            {
                logger.Information("Login: {NetState} Invalid username '{Username}'", e.State, un);
                e.RejectReason = ALRReason.Invalid;
            }
        }
        else if (!acct.HasAccess(e.State))
        {
            logger.Information("Login: {NetState} Access denied for '{Username}'", e.State, un);
            e.RejectReason = LockdownLevel > AccessLevel.Player ? ALRReason.BadComm : ALRReason.BadPass;
        }
        else if (!acct.CheckPassword(pw))
        {
            logger.Information("Login: {NetState} Invalid password for '{Username}'", e.State, un);
            e.RejectReason = ALRReason.BadPass;
        }
        else if (acct.Banned)
        {
            logger.Information("Login: {NetState} Banned account '{Username}'", e.State, un);
            e.RejectReason = ALRReason.Blocked;
        }
        else
        {
            logger.Information("Login: {NetState} Valid credentials for '{Username}'", e.State, un);
            e.State.Account = acct;
            e.Accepted = true;

            acct.LogAccess(e.State);
        }
    }

    [OnEvent(nameof(GameServer.GameServerLoginEvent))]
    public static void OnGameServerLogin(GameServer.GameLoginEventArgs e)
    {
        var un = e.Username;
        var pw = e.Password;

        if (Accounts.GetAccount(un) is not Account acct)
        {
            e.Accepted = false;
        }
        else if (!acct.HasAccess(e.State))
        {
            logger.Information("Login: {NetState} Access denied for '{Username}'", e.State, un);
            e.Accepted = false;
        }
        else if (!acct.CheckPassword(pw))
        {
            logger.Information("Login: {NetState} Invalid password for '{Username}'", e.State, un);
            e.Accepted = false;
        }
        else if (acct.Banned)
        {
            logger.Information("Login: {NetState} Banned account '{Username}'", e.State, un);
            e.Accepted = false;
        }
        else
        {
            acct.LogAccess(e.State);

            logger.Information("Login: {NetState} Account '{Username}' at character list", e.State, un);
            e.State.Account = acct;
            e.Accepted = true;
            e.CityInfo = CharacterCreation.GetStartingCities();
        }
    }

    public static bool CheckAccount(Mobile mobCheck, Mobile accCheck)
    {
        if (accCheck?.Account is Account a)
        {
            for (var i = 0; i < a.Length; ++i)
            {
                if (a[i] == mobCheck)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
