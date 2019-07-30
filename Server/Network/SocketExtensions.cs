using System;
using System.Buffers;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Server.Network
{
  public static class SocketExtensions
  {
    public static Task<int> ReceiveAsync(this Socket socket, Memory<byte> memory, SocketFlags socketFlags)
    {
      return SocketTaskExtensions.ReceiveAsync(socket, GetArray(memory), socketFlags);
    }

    public static ArraySegment<byte> GetArray(this Memory<byte> memory)
    {
      return ((ReadOnlyMemory<byte>)memory).GetArray();
    }

    public static ArraySegment<byte> GetArray(this ReadOnlyMemory<byte> memory)
    {
      if (MemoryMarshal.TryGetArray(memory, out var result))
        return result;

      throw new InvalidOperationException("Buffer backed by array was expected");
    }

    public static ArraySegment<byte> GetArray(this ReadOnlySequence<byte> memory)
    {
      if (SequenceMarshal.TryGetArray(memory, out var result))
        return result;

      throw new InvalidOperationException("Buffer backed by array was expected");
    }
  }
}
