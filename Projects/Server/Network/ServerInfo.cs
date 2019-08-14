using System;
using System.Net;

namespace Server.Network
{
  public sealed class ServerInfo
  {
    public ServerInfo(string name, int fullPercent, TimeZoneInfo tz, IPEndPoint address)
    {
      Name = name;
      FullPercent = fullPercent;
      TimeZone = tz.GetUtcOffset(DateTime.Now).Hours;
      Address = address;
    }

    public string Name { get; set; }

    public int FullPercent { get; set; }

    public int TimeZone { get; set; }

    public IPEndPoint Address { get; set; }
  }
}
