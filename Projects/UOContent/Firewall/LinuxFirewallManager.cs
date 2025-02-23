using System;
using System.Collections.Generic;
using System.Net;

namespace Server.Firewall;

public class LinuxFirewallManager : IFirewallManager
{
    public void AddIPAddress(IPAddress ip, out ISet<IPAddress> newSet)
    {
        throw new NotImplementedException();
    }

    public void RemoveIPAddress(IPAddress ip, out ISet<IPAddress> newSet)
    {
        throw new NotImplementedException();
    }

    public ISet<IPAddress> GetBlockedIPs() => throw new NotImplementedException();
}
