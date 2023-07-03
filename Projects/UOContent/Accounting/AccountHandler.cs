using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Server.Accounting;
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
    private static bool RestrictDeletion = !TestCenter.Enabled;
    private static TimeSpan DeleteDelay = TimeSpan.FromDays(7.0);
    private static bool PasswordCommandEnabled;

    private static CityInfo[] OldHavenStartingCities =
    {
        new("Yew", "The Empath Abbey", 633, 858, 0),
        new("Minoc", "The Barnacle", 2476, 413, 15),
        new("Britain", "Sweet Dreams Inn", 1496, 1628, 10),
        new("Moonglow", "The Scholars Inn", 4408, 1168, 0),
        new("Trinsic", "The Traveler's Inn", 1845, 2745, 0),
        new("Magincia", "The Great Horns Tavern", 3734, 2222, 20),
        new("Jhelom", "The Mercenary Inn", 1374, 3826, 0),
        new("Skara Brae", "The Falconer's Inn", 618, 2234, 0),
        new("Vesper", "The Ironwood Inn", 2771, 976, 0),
        new("Haven", "Buckler's Hideaway", 3667, 2625, 0)
    };

    private static CityInfo[] StartingCities =
    {
        new("New Haven", "New Haven Bank", 1150168, 3667, 2625, 0),
        new("Yew", "The Empath Abbey", 1075072, 633, 858, 0),
        new("Minoc", "The Barnacle", 1075073, 2476, 413, 15),
        new("Britain", "The Wayfarer's Inn", 1075074, 1602, 1591, 20),
        new("Moonglow", "The Scholars Inn", 1075075, 4408, 1168, 0),
        new("Trinsic", "The Traveler's Inn", 1075076, 1845, 2745, 0),
        new("Jhelom", "The Mercenary Inn", 1075078, 1374, 3826, 0),
        new("Skara Brae", "The Falconer's Inn", 1075079, 618, 2234, 0),
        new("Vesper", "The Ironwood Inn", 1075080, 2771, 976, 0)
    };

    private static Dictionary<IPAddress, int> m_IPTable;

    private static char[] m_ForbiddenChars = { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };

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
    }

    public static void Initialize()
    {
        EventSink.DeleteRequest += EventSink_DeleteRequest;
        EventSink.AccountLogin += EventSink_AccountLogin;
        EventSink.GameLogin += EventSink_GameLogin;

        if (PasswordCommandEnabled)
        {
            CommandSystem.Register("Password", AccessLevel.Player, Password_OnCommand);
        }
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

            if (Utility.IPMatchClassC(accessList[0], ipAddress))
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

    private static void EventSink_DeleteRequest(NetState state, int index)
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

                EventSink.InvokePlayerDeleted(m);
                state.SendCharacterListUpdate(acct);
                return;
            }
        }

        state.SendCharacterDeleteResult(res);
        state.SendCharacterListUpdate(acct);
    }

    public static bool CanCreate(IPAddress ip) =>
        !IPTable.TryGetValue(ip, out var result) || result < MaxAccountsPerIP;

    private static bool IsForbiddenChar(char c)
    {
        for (var i = 0; i < m_ForbiddenChars.Length; ++i)
        {
            if (c == m_ForbiddenChars[i])
            {
                return true;
            }
        }

        return false;
    }

    private static Account CreateAccount(NetState state, string un, string pw)
    {
        if (un.Length == 0 || pw.Length == 0)
        {
            return null;
        }

        var isSafe = !(un.StartsWithOrdinal(" ") ||
                       un.EndsWithOrdinal(" ") ||
                       un.EndsWithOrdinal("."));

        for (var i = 0; isSafe && i < un.Length; ++i)
        {
            isSafe = un[i] >= 0x20 && un[i] < 0x7F && !IsForbiddenChar(un[i]);
        }

        for (var i = 0; isSafe && i < pw.Length; ++i)
        {
            isSafe = pw[i] >= 0x20 && pw[i] < 0x7F;
        }

        if (!isSafe)
        {
            return null;
        }

        if (!CanCreate(state.Address))
        {
            logger.Information(
                $"Login: {{NetState}} Account '{{Username}}' not created, ip already has {{AccountCount}} account{(MaxAccountsPerIP == 1 ? "" : "s")}.",
                state,
                un,
                MaxAccountsPerIP
            );

            return null;
        }

        logger.Information("Login: {NetState}: Creating new account '{Username}'", state, un);

        var a = new Account(un, pw);

        return a;
    }

    public static void EventSink_AccountLogin(AccountLoginEventArgs e)
    {
        if (!IPLimiter.SocketBlock && !IPLimiter.Verify(e.State.Address))
        {
            e.Accepted = false;
            e.RejectReason = ALRReason.InUse;

            logger.Information("Login: {NetState}: Past IP limit threshold", e.State);

            using var op = new StreamWriter("ipLimits.log", true);
            op.WriteLine($"{e.State}\tPast IP limit threshold\t{Core.Now}");

            return;
        }

        var un = e.Username;
        var pw = e.Password;

        e.Accepted = false;

        if (Accounts.GetAccount(un) is not Account acct)
        {
            // To prevent someone from making an account of just '' or a bunch of meaningless spaces
            if (AutoAccountCreation && un.Trim().Length > 0)
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

        if (!e.Accepted)
        {
            AccountAttackLimiter.RegisterInvalidAccess(e.State);
        }
    }

    public static void EventSink_GameLogin(GameLoginEventArgs e)
    {
        if (!IPLimiter.SocketBlock && !IPLimiter.Verify(e.State.Address))
        {
            e.Accepted = false;

            logger.Warning("Login: {NetState} Past IP limit threshold", e.State);

            using var op = new StreamWriter("ipLimits.log", true);
            op.WriteLine($"{e.State}\tPast IP limit threshold\t{Core.Now}");

            return;
        }

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
            e.CityInfo = TileMatrix.Pre6000ClientSupport ? OldHavenStartingCities : StartingCities;
        }

        if (!e.Accepted)
        {
            AccountAttackLimiter.RegisterInvalidAccess(e.State);
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
