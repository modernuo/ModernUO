using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using Server.Logging;

namespace Server.Firewall;

public static class FirewallManager
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(FirewallManager));

    private static ISet<IPAddress> _blacklistedIps = new HashSet<IPAddress>();
    public static ISet<IPAddress> BlockedIPs => _blacklistedIps;

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
                if (_instance == null)
                {
                    Enabled = false;
                    return null;
                }

                if (IsAvailable())
                {
                    _blacklistedIps = _instance.GetBlockedIPs();
                    return _instance;
                }

                _blacklistedIps = new HashSet<IPAddress>();
                return null;
            }

            return IsAvailable() ? _instance : null;
        }
    }

    private static bool _warnedDebug;
    public static bool Enabled { get; private set; } = true;

    private static bool IsAvailable()
    {
        if (_instance == null)
        {
            if (!_warnedDebug)
            {
                logger.Warning("FirewallManager is not supported on this platform.");
                _warnedDebug = true;
            }

            return false;
        }

        if (_instance is WindowsFirewallManager && Debugger.IsAttached)
        {
            if (!_warnedDebug)
            {
                logger.Warning("FirewallManager is disabled while a debugger is attached.");
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

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new LinuxFirewallManager(); // Placeholder for Linux
        }

        return null;
    }

    public static bool AddIPAddress(IPAddress ip)
    {
        var instance = Instance;
        if (instance == null)
        {
            return false;
        }

        instance.AddIPAddress(ip, out _blacklistedIps);
        return true;
    }

    public static bool RemoveIPAddress(IPAddress ip)
    {
        var instance = Instance;
        if (instance == null)
        {
            return false;
        }

        instance.RemoveIPAddress(ip, out _blacklistedIps);
        return true;
    }

    public static bool IsBlocked(IPAddress ip)
    {
        Utility.Intern(ref ip);
        return _blacklistedIps.Contains(ip);
    }

    public static bool GetBlockedIPs(out ISet<IPAddress> ips)
    {
        var instance = Instance;
        if (instance == null)
        {
            ips = null;
            return false;
        }

        ips = instance.GetBlockedIPs();
        return true;
    }
}
