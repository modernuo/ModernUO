using System.IO;
using System.Net;
using Server.Logging;
using Server.Misc;

namespace Server
{
    public static class AccessRestrictions
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(AccessRestrictions));

        public static void Initialize()
        {
            EventSink.SocketConnect += EventSink_SocketConnect;
        }

        private static void EventSink_SocketConnect(SocketConnectEventArgs e)
        {
            try
            {
                var ip = (e.Connection.RemoteEndPoint as IPEndPoint)?.Address;

                if (Firewall.IsBlocked(ip))
                {
                    logger.Information("Client: {IP}: Firewall blocked connection attempt.", ip);
                    e.AllowConnection = false;
                    return;
                }

                if (IPLimiter.SocketBlock && !IPLimiter.Verify(ip))
                {
                    logger.Warning("Client: {IP}: Past IP limit threshold", ip);

                    using (var op = new StreamWriter("ipLimits.log", true))
                    {
                        op.WriteLine("{0}\tPast IP limit threshold\t{1}", ip, Core.Now);
                    }

                    e.AllowConnection = false;
                }
            }
            catch
            {
                e.AllowConnection = false;
            }
        }
    }
}
