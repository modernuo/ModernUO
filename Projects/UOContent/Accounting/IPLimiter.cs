using System.Net;
using Server.Network;

namespace Server.Misc
{
    public static class IPLimiter
    {
        public static readonly IPAddress[] Exemptions =
        {
            IPAddress.Parse( "127.0.0.1" )
        };

        public static bool Enabled { get; private set; }
        public static bool SocketBlock { get; private set; }
        public static int MaxAddresses { get; private set; }

        public static void Configure()
        {
            Enabled = ServerConfiguration.GetOrUpdateSetting("ipLimiter.enable", true);
            SocketBlock = ServerConfiguration.GetOrUpdateSetting("ipLimiter.blockAtConnection", true);
            MaxAddresses = ServerConfiguration.GetOrUpdateSetting("ipLimiter.maxConnectionsPerIP", 10);
        }

        public static bool IsExempt(IPAddress ip)
        {
            for (int i = 0; i < Exemptions.Length; i++)
            {
                if (ip.Equals(Exemptions[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool Verify(IPAddress ourAddress)
        {
            if (!Enabled || IsExempt(ourAddress))
            {
                return true;
            }

            var count = 0;

            foreach (var ns in TcpServer.Instances)
            {
                if (ourAddress.Equals(ns.Address))
                {
                    ++count;

                    if (count >= MaxAddresses)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
