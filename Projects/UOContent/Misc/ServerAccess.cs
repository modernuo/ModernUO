using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Server.Accounting;
using Server.Json;
using Server.Logging;
using Server.Network;

namespace Server.Misc;

public static class ServerAccess
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ServerAccess));
    private const string _serverAccessConfigurationPath = "Configuration/server-access.json";
    public static ServerAccessConfiguration ServerAccessConfiguration { get; private set; }

    public static void SaveConfiguration()
    {
        var path = Path.Join(Core.BaseDirectory, _serverAccessConfigurationPath);

        JsonConfig.Serialize(path, ServerAccessConfiguration ??= new ServerAccessConfiguration());
    }

    public static void AddProtectedAccount(Account acct, bool save = false)
    {
        var username = acct.Username.ToLower();
        ServerAccessConfiguration.ProtectedAccounts.Add(username);
        logger.Information("Protected account added: {0}", username);

        if (save)
        {
            SaveConfiguration();
        }
    }

    public static void RemoveProtectedAccount(Account acct, bool save = false)
    {
        var username = acct.Username.ToLower();
        ServerAccessConfiguration.ProtectedAccounts.Remove(username);
        logger.Information("Protected account removed: {0}", username);

        if (save)
        {
            SaveConfiguration();
        }
    }

    public static void Configure()
    {
        var path = Path.Join(Core.BaseDirectory, _serverAccessConfigurationPath);

        if (!File.Exists(path))
        {
            SaveConfiguration();
            return;
        }

        ServerAccessConfiguration = JsonConfig.Deserialize<ServerAccessConfiguration>(path);
        if (ServerAccessConfiguration.ProtectedAccounts.Count > 0)
        {
            var protectedAccounts = string.Join(", ", ServerAccessConfiguration.ProtectedAccounts);
            logger.Information("Protected accounts registered: {0}", protectedAccounts);
        }
    }

    public static void Initialize()
    {
        EventSink.AccountLogin += EventSink_ResetProtectedAccount;
    }

    public static void EventSink_ResetProtectedAccount(AccountLoginEventArgs e)
    {
        var username = e.Username.ToLower();
        if (!ServerAccessConfiguration.ProtectedAccounts.Contains(username))
        {
            return;
        }

        var acct = Accounts.GetAccount(username);
        if (acct == null || !acct.Banned && acct.AccessLevel >= AccessLevel.Owner || !acct.CheckPassword(e.Password))
        {
            return;
        }

        acct.Banned = false;
        acct.AccessLevel = AccessLevel.Owner;

        logger.Warning("Protected account \"{0}\" has been reset.", username);

        if (e.RejectReason is ALRReason.Blocked or ALRReason.BadPass or ALRReason.BadComm)
        {
            e.Accepted = true;
        }
    }
}

public record ServerAccessConfiguration
{
    [JsonPropertyName("protectedAccounts")]
    public HashSet<string> ProtectedAccounts { get; set; } = new();
}
