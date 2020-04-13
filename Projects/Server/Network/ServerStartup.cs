using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Server.Network;

namespace Server.Network
{
  public class ServerStartup
  {
    private readonly IMessagePumpService _messagePumpService;
    public ServerStartup(IMessagePumpService messagePumpService) => _messagePumpService = messagePumpService;

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void Configure(IApplicationBuilder app)
    {
      // Run async?
      Task.Run(() => Core.RunEventLoop(_messagePumpService));
    }
  }
}
