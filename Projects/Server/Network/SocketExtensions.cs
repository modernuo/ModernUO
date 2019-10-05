/***************************************************************************
 *                            SocketExtensions.cs
 *                            -------------------
 *   begin                : August 2, 2019
 *   copyright            : (C) The ModernUO Team
 *   email                : hi@modernuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Buffers;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Server.Network
{
  public static class SocketExtensions
  {
    public static Task<int> ReceiveAsync(this Socket socket, Memory<byte> memory, SocketFlags socketFlags) => SocketTaskExtensions.ReceiveAsync(socket, GetArray(memory), socketFlags);

    public static ArraySegment<byte> GetArray(this Memory<byte> memory) => ((ReadOnlyMemory<byte>)memory).GetArray();

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
