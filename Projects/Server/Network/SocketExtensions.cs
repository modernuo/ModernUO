/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019 - ModernUO Development Team                        *
 * Email: hi@modernuo.com                                                *
 * File: SocketExtensions.cs - Created: 2019/08/02 - Updated: 2019/12/24 *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

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
      if (MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> result))
        return result;

      throw new InvalidOperationException("Buffer backed by array was expected");
    }

    public static ArraySegment<byte> GetArray(this ReadOnlySequence<byte> memory)
    {
      if (SequenceMarshal.TryGetArray(memory, out ArraySegment<byte> result))
        return result;

      throw new InvalidOperationException("Buffer backed by array was expected");
    }
  }
}
