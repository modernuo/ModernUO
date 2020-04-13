using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Server.Network
{
  public class ServerConnectionHandler : ConnectionHandler
  {
    private readonly IMessagePumpService _messagePumpService;
    private readonly ILogger<ServerConnectionHandler> _logger;

    public ServerConnectionHandler(
      IMessagePumpService messagePumpService,
      ILogger<ServerConnectionHandler> logger
    )
    {
      _messagePumpService = messagePumpService;
      _logger = logger;
    }

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
      if (!VerifySocket(connection))
      {
        Release(connection);
        return;
      }

      NetState ns = new NetState(connection);
      TcpServer.Instances.Add(ns);
      _logger.LogInformation($"Client: {ns}: Connected. [{TcpServer.Instances.Count} Online]");

      connection.ConnectionClosed.Register(() => { TcpServer.Instances.Remove(ns); });

      await ProcessIncoming(ns);
    }

    private async Task ProcessIncoming(NetState ns)
    {
      var inPipe = ns.Connection.Transport.Input;

      while (true)
        try
        {
          ReadResult result = await inPipe.ReadAsync();
          if (result.IsCanceled || result.IsCompleted)
            return;

          ReadOnlySequence<byte> seq = result.Buffer;

          if (seq.IsEmpty)
            break;

          int pos = PacketHandlers.ProcessPacket(_messagePumpService, ns, seq);

          if (pos <= 0)
            break;

          inPipe.AdvanceTo(seq.Slice(0, pos).End);
        }
        catch
        {
          // ignored
        }

      inPipe.Complete();
    }

    private static bool VerifySocket(ConnectionContext connection)
    {
      try
      {
        SocketConnectEventArgs args = new SocketConnectEventArgs(connection);

        EventSink.InvokeSocketConnect(args);

        return args.AllowConnection;
      }
      catch (Exception ex)
      {
        NetState.TraceException(ex);
        return false;
      }
    }

    private static void Release(ConnectionContext connection)
    {
      try
      {
        connection.Abort(new ConnectionAbortedException("Failed socket verification."));
      }
      catch (Exception ex)
      {
        NetState.TraceException(ex);
      }

      try
      {
        // TODO: Is this needed?
        connection.DisposeAsync();
      }
      catch (Exception ex)
      {
        NetState.TraceException(ex);
      }
    }
  }
}
