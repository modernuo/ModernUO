using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using Server.Logging;

namespace Server.Firewall;

public static class FirewallManager
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(FirewallManager));

    private static ISet<IPAddress> _blockedIps;
    public static ISet<IPAddress> BlockedIPs => _blockedIps ??= Instance?.GetBlockedIPs();

    private static IFirewallManager _instance;
    private static IFirewallManager Instance
    {
        get
        {
            if (!Enabled)
            {
                return null;
            }

            if (_instance == null)
            {
                _instance ??= CreateFirewallManagerImplementation();
                Enabled = _instance != null;
            }

            return IsAvailable() ? _instance : null;
        }
    }

    private static bool _warnedDebug;
    public static bool Enabled { get; private set; } = true;

    private static bool IsAvailable()
    {
        if (!Enabled)
        {
            return false;
        }

        if (_instance is WindowsFirewallManager && Debugger.IsAttached)
        {
            if (!_warnedDebug)
            {
                logger.Warning("Firewall is disabled while a debugger is attached.");
                _warnedDebug = true;
            }
            return false;
        }

        return true;
    }

    private static IFirewallManager CreateFirewallManagerImplementation()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsFirewallManager();
        }

        // Linux/BSD are not supported.
        // Firewall management requires sudo permissions, which we don't want to require.
        // To implement set up a daemon that listens for requests to block/unblock IPs.
        logger.Warning("Firewall is not supported on this platform.");
        return null;
    }

    public static bool AddIPAddress(IPAddress ip)
    {
        var instance = Instance;
        if (instance == null)
        {
            return false;
        }

        instance.AddIPAddress(ip, out _blockedIps);
        return true;
    }

    public static bool RemoveIPAddress(IPAddress ip)
    {
        var instance = Instance;
        if (instance == null)
        {
            return false;
        }

        instance.RemoveIPAddress(ip, out _blockedIps);
        return true;
    }

    public static bool IsBlocked(IPAddress ip)
    {
        Utility.Intern(ref ip);
        return BlockedIPs.Contains(ip);
    }
}
