using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Server.Network
{
  public class TcpServer
  {
    public static List<IPEndPoint> Listeners { get; } = new List<IPEndPoint>();
    // Make this thread safe
    public static List<NetState> Instances { get; } = new List<NetState>();

    public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
      WebHost.CreateDefaultBuilder(args)
        .UseSetting(WebHostDefaults.SuppressStatusMessagesKey, "True")
        .ConfigureServices(services =>
        {
          services.AddSingleton<IMessagePumpService>(new MessagePumpService());
        })
        .UseKestrel(options =>
        {
          foreach (var ipep in Listeners)
          {
            options.Listen(ipep, builder => { builder.UseConnectionHandler<ServerConnectionHandler>(); });
            DisplayListener(ipep);
          }

          options.ListenLocalhost(2593, builder => { builder.UseConnectionHandler<ServerConnectionHandler>(); });

          // Webservices here
        })
        .UseLibuv()
        .UseStartup<ServerStartup>();

    private static void DisplayListener(IPEndPoint ipep)
    {
      if (ipep.Address.Equals(IPAddress.Any) || ipep.Address.Equals(IPAddress.IPv6Any))
      {
        NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface adapter in adapters)
        {
          IPInterfaceProperties properties = adapter.GetIPProperties();
          foreach (UnicastIPAddressInformation unicast in properties.UnicastAddresses)
            if (ipep.AddressFamily == unicast.Address.AddressFamily)
              Console.WriteLine("Listening: {0}:{1}", unicast.Address, ipep.Port);
        }
      }
      else
        Console.WriteLine("Listening: {0}:{1}", ipep.Address, ipep.Port);
    }
  }
}
