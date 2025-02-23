using System.Collections.Generic;
using System.Net;

public interface IFirewallManager
{
    void AddIPAddress(IPAddress ip, out ISet<IPAddress> newSet);
    void RemoveIPAddress(IPAddress ip, out ISet<IPAddress> newSet);
    ISet<IPAddress> GetBlockedIPs();
}
