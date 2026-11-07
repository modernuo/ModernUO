using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ModernUO.CodeGeneratedEvents;
using Server.Accounting;
using Server.Accounting.Security;
using Server.Collections;
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

    private static Dictionary<UInt128, int> _accountCounts;

    private static readonly SearchValues<char> ForbiddenChars = SearchValues.Create("<>:\"/\\|?*");

    public static AccessLevel LockdownLevel { get; set; }

    // Zero by default: behind a proxy without real-IP forwarding every player shares one address.
    internal static int MaxLoginsPerIP { get; set; }

    // Keyed by subscriber so an IPv6 subscriber prefix counts as one address, as an IPv4 address does.
    private static Dictionary<UInt128, int> AccountCounts
    {
        get
        {
            if (_accountCounts == null)
            {
                _accountCounts = new Dictionary<UInt128, int>();

                foreach (Account a in Accounts.GetAccounts())
                {
                    if (a.LoginIPs.Length > 0)
                    {
                        var key = a.LoginIPs[0].ToSubscriberKey();
                        CollectionsMarshal.GetValueRefOrAddDefault(_accountCounts, key, out _)++;
                    }
                }
            }

            return _accountCounts;
        }
    }

    public static int GetAccountCount(IPAddress ip) =>
        ip != null && AccountCounts.TryGetValue(ip.ToSubscriberKey(), out var count) ? count : 0;

    public static void IncrementAccountCount(IPAddress ip)
    {
        if (ip != null)
        {
            CollectionsMarshal.GetValueRefOrAddDefault(AccountCounts, ip.ToSubscriberKey(), out _)++;
        }
    }

    public static void DecrementAccountCount(IPAddress ip)
    {
        if (ip == null)
        {
            return;
        }

        var counts = AccountCounts;
        var key = ip.ToSubscriberKey();

        if (counts.TryGetValue(key, out var count))
        {
            if (count > 1)
            {
                counts[key] = count - 1;
            }
            else
            {
                counts.Remove(key);
            }
        }
    }

    public static void Configure()
    {
        MaxAccountsPerIP = ServerConfiguration.GetOrUpdateSetting("accountHandler.maxAccountsPerIP", 1);
        MaxLoginsPerIP = ServerConfiguration.GetOrUpdateSetting("accountHandler.maxLoginsPerIP", 0);
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

        EventSink.Shutdown += PasswordWorker.Stop;
        EventSink.ServerCrashed += PasswordWorker.OnCrashed;
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
                // Confirmed from the callback: off-loop the write has not landed yet here.
                PasswordWorker.SetPassword(
                    acct,
                    pass,
                    _ => from.SendMessage("The password to your account has changed.")
                );
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

    public static bool CanCreate(IPAddress ip) => GetAccountCount(ip) < MaxAccountsPerIP;

    internal static bool IsAtLoginCap(NetState state, Account acct) => IsAtLoginCap(state, acct, NetState.Instances);

    /// <summary>
    /// Counts distinct other player accounts connected from the same subscriber. The account itself is
    /// excluded because its login-server connection can still be open when it relays to the game server.
    /// </summary>
    internal static bool IsAtLoginCap(NetState state, Account acct, HashSet<NetState> candidates)
    {
        var address = state.Address;

        if (MaxLoginsPerIP <= 0 || acct.AccessLevel > AccessLevel.Player || address == null)
        {
            return false;
        }

        using var seen = PooledRefList<IAccount>.Create(MaxLoginsPerIP);

        foreach (var ns in candidates)
        {
            var other = ns.Account;

            if (ns == state || other == null || other == acct || other.AccessLevel != AccessLevel.Player ||
                !ns.Address.IsSameSubscriber(address) || seen.Contains(other))
            {
                continue;
            }

            seen.Add(other);

            if (seen.Count >= MaxLoginsPerIP)
            {
                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidUsername(ReadOnlySpan<char> username) =>
        username.Length > 0 &&
        // Usernames must not start with a space, end with a space, or end with a period
        !username.StartsWith(' ') && !username.EndsWith(' ') && !username.EndsWith('.') &&
        !username.ContainsAny(ForbiddenChars);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidPassword(ReadOnlySpan<char> password) => password.Length > 0;

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
        else
        {
            HandlePasswordCheck(e, acct, pw);
        }
    }

    /// <summary>
    /// Separate from the caller's else-if chain because two outcomes are not verdicts: the off-loop
    /// path has none yet, and a full queue must reject rather than fall through and verify.
    /// </summary>
    private static void HandlePasswordCheck(AccountLoginEventArgs e, Account acct, string pw)
    {
        switch (DispatchPasswordCheck(e, acct, pw))
        {
            case PasswordCheckDispatch.Deferred:
                {
                    e.Deferred = true;
                    return;
                }
            case PasswordCheckDispatch.Saturated:
                {
                    // Reject rather than verify inline: steering work back onto the loop is what a
                    // flood wants.
                    logger.Warning(
                        "Login: {NetState} Password verification queue full, rejecting '{Username}'",
                        e.State,
                        acct.Username
                    );

                    e.RejectReason = ALRReason.BadComm;
                    return;
                }
        }

        if (!acct.CheckPassword(pw))
        {
            logger.Information("Login: {NetState} Invalid password for '{Username}'", e.State, acct.Username);
            e.RejectReason = ALRReason.BadPass;
            return;
        }

        ApplyVerifiedLogin(e, acct);
    }

    /// <summary>Everything after the password is known good, shared so an off-loop verdict lands
    /// in the same state as an inline one.</summary>
    private static void ApplyVerifiedLogin(AccountLoginEventArgs e, Account acct)
    {
        if (acct.Banned)
        {
            logger.Information("Login: {NetState} Banned account '{Username}'", e.State, acct.Username);
            e.RejectReason = ALRReason.Blocked;
            return;
        }

        if (IsAtLoginCap(e.State, acct))
        {
            logger.Information(
                "Login: {NetState} Account '{Username}' refused, {Count} accounts already logged in from this address",
                e.State,
                acct.Username,
                MaxLoginsPerIP
            );
            e.RejectReason = ALRReason.InUse;
            return;
        }

        logger.Information("Login: {NetState} Valid credentials for '{Username}'", e.State, acct.Username);
        e.State.Account = acct;
        e.Accepted = true;

        acct.LogAccess(e.State);
        LoginAllowlist.RecordLogin(e.State?.Address);
    }

    private enum PasswordCheckDispatch
    {
        /// <summary>Verify on the loop.</summary>
        Inline,

        /// <summary>Handed to the worker; no verdict yet.</summary>
        Deferred,

        /// <summary>The queue is full.</summary>
        Saturated
    }

    /// <summary>
    /// Hands the password check to the worker, whatever algorithm it uses. Every protection is safe
    /// off the loop, so there is no carve-out, and a cheap digest does not need one either:
    /// <c>AccountSecurity.Configure</c> refuses anything below SHA2 as the configured algorithm, so
    /// MD5 and SHA1 only appear as a stored hash awaiting migration. That makes
    /// <c>NeedsPasswordUpgrade</c> true, and the upgrade hash dominates the job.
    /// </summary>
    private static PasswordCheckDispatch DispatchPasswordCheck(AccountLoginEventArgs e, Account acct, string pw)
    {
        if (!PasswordWorker.Enabled)
        {
            return PasswordCheckDispatch.Inline;
        }

        var job = new PasswordJob
        {
            Account = acct,
            State = e.State,
            StoredHash = acct.Password,
            StoredAlgorithm = acct.PasswordAlgorithm,
            VerifyPhrase = acct.GetVerifyPhrase(pw),
            HashPhrase = acct.NeedsPasswordUpgrade() ? acct.GetRehashPhrase(pw) : null,
            TargetAlgorithm = AccountSecurity.CurrentAlgorithm,
            OnComplete = static (j, outcome) =>
                CompleteDeferredAccountLogin(j.State, j.Account, outcome.Verified)
        };

        return PasswordWorker.TryEnqueue(job)
            ? PasswordCheckDispatch.Deferred
            : PasswordCheckDispatch.Saturated;
    }

    /// <summary>Resumes a login whose password check ran on the verification thread.</summary>
    internal static void CompleteDeferredAccountLogin(NetState state, Account acct, bool verified)
    {
        var e = new AccountLoginEventArgs(state, acct.Username, null);

        if (verified)
        {
            ApplyVerifiedLogin(e, acct);
        }
        else
        {
            logger.Information("Login: {NetState} Invalid password for '{Username}'", state, acct.Username);
            e.RejectReason = ALRReason.BadPass;
        }

        IncomingAccountPackets.CompleteAccountLogin(state, e.Accepted, e.RejectReason);
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
        // The auth id was only issued after the account login packet verified this password, so
        // re-deriving the hash costs a second Argon2 verify to answer the same question.
        else if (!e.PreAuthenticated && !acct.CheckPassword(pw))
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
            LoginAllowlist.RecordLogin(e.State?.Address);

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
