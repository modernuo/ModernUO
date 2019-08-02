/***************************************************************************
 *                                Packet.cs
 *                            -------------------
 *   begin                : August 2, 2019
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
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

using Server.Diagnostics;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Server.Network
{
  public abstract class Packet
  {
    private const int CompressorBufferSize = 0x10000;

    private const int BufferSize = 4096;

    private byte[] m_CompiledBuffer;
    private int m_CompiledLength;
    private int m_Length;
    private State m_State;

    protected PacketWriter m_Stream;

    protected Packet(int packetID)
    {
      PacketID = packetID;

      if (Core.Profiling)
      {
        PacketSendProfile prof = PacketSendProfile.Acquire(GetType());
        prof.Increment();
      }
    }

    protected Packet(int packetID, int length)
    {
      PacketID = packetID;
      m_Length = length;

      m_Stream = PacketWriter.CreateInstance(length); // new PacketWriter( length );
      m_Stream.Write((byte)packetID);

      if (Core.Profiling)
      {
        PacketSendProfile prof = PacketSendProfile.Acquire(GetType());
        prof.Increment();
      }
    }

    public int PacketID { get; }

    public PacketWriter UnderlyingStream => m_Stream;

    public void EnsureCapacity(int length)
    {
      m_Stream = PacketWriter.CreateInstance(length); // new PacketWriter( length );
      m_Stream.Write((byte)PacketID);
      m_Stream.Write((short)0);
    }

    public static Packet SetStatic(Packet p)
    {
      p.SetStatic();
      return p;
    }

    public static Packet Acquire(Packet p)
    {
      p.Acquire();
      return p;
    }

    public static void Release(ref Packet p)
    {
      p?.Release();
      p = null;
    }

    public static void Release(Packet p)
    {
      p?.Release();
    }

    public void SetStatic()
    {
      m_State |= State.Static | State.Acquired;
    }

    public void Acquire()
    {
      m_State |= State.Acquired;
    }

    public void OnSend()
    {
      Core.Set(); // Is this still needed if this is done async?

      if ((m_State & (State.Acquired | State.Static)) == 0)
        Free();
    }

    private void Free()
    {
      if (m_CompiledBuffer == null)
        return;

      if ((m_State & State.Buffered) != 0)
        ArrayPool<byte>.Shared.Return(m_CompiledBuffer);

      m_State &= ~(State.Static | State.Acquired | State.Buffered);

      m_CompiledBuffer = null;
    }

    public void Release()
    {
      if ((m_State & State.Acquired) != 0)
        Free();
    }

    public byte[] Compile(bool compress, out int length)
    {
      lock (this)
      {
        if (m_CompiledBuffer == null)
        {
          if ((m_State & State.Accessed) == 0)
          {
            m_State |= State.Accessed;
          }
          else
          {
            if ((m_State & State.Warned) == 0)
            {
              m_State |= State.Warned;

              try
              {
                using (StreamWriter op = new StreamWriter("net_opt.log", true))
                {
                  op.WriteLine("Redundant compile for packet {0}, use Acquire() and Release()", GetType());
                  op.WriteLine(new StackTrace());
                }
              }
              catch
              {
                // ignored
              }
            }

            m_CompiledBuffer = new byte[0];
            m_CompiledLength = 0;

            length = m_CompiledLength;
            return m_CompiledBuffer;
          }

          InternalCompile(compress);
        }

        length = m_CompiledLength;
        return m_CompiledBuffer;
      }
    }

    private void InternalCompile(bool compress)
    {
      if (m_Length == 0)
      {
        long streamLen = m_Stream.Length;

        m_Stream.Seek(1, SeekOrigin.Begin);
        m_Stream.Write((ushort)streamLen);
      }
      else if (m_Stream.Length != m_Length)
      {
        int diff = (int)m_Stream.Length - m_Length;

        Console.WriteLine("Packet: 0x{0:X2}: Bad packet length! ({1}{2} bytes)", PacketID, diff >= 0 ? "+" : "",
          diff);
      }

      MemoryStream ms = m_Stream.UnderlyingStream;

      m_CompiledBuffer = ms.GetBuffer();
      int length = (int)ms.Length;

      if (compress)
      {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(CompressorBufferSize);

        Compression.Compress(m_CompiledBuffer, 0, length, buffer, ref length);

        if (length <= 0)
        {
          Console.WriteLine("Warning: Compression buffer overflowed on packet 0x{0:X2} ('{1}') (length={2})",
            PacketID, GetType().Name, length);
          using (StreamWriter op = new StreamWriter("compression_overflow.log", true))
          {
            op.WriteLine("{0} Warning: Compression buffer overflowed on packet 0x{1:X2} ('{2}') (length={3})",
              DateTime.UtcNow, PacketID, GetType().Name, length);
            op.WriteLine(new StackTrace());
          }
        }
        else
        {
          m_CompiledLength = length;

          if ((m_State & State.Static) != 0)
          {
            m_CompiledBuffer = new byte[length];
            Buffer.BlockCopy(buffer, 0, m_CompiledBuffer, 0, length);
            ArrayPool<byte>.Shared.Return(buffer);
          }
          else
          {
            m_CompiledBuffer = buffer;
            m_State |= State.Buffered;
          }
        }
      }
      else if (length > 0)
      {
        byte[] old = m_CompiledBuffer;
        m_CompiledLength = length;

        if ((m_State & State.Static) != 0)
          m_CompiledBuffer = new byte[length];
        else
        {
          m_CompiledBuffer = ArrayPool<byte>.Shared.Rent(length);
          m_State |= State.Buffered;
        }

        Buffer.BlockCopy(old, 0, m_CompiledBuffer, 0, length);
      }

      PacketWriter.ReleaseInstance(m_Stream);
      m_Stream = null;
    }

    [Flags]
    private enum State
    {
      Inactive = 0x00,
      Static = 0x01,
      Acquired = 0x02,
      Accessed = 0x04,
      Buffered = 0x08,
      Warned = 0x10
    }
  }
}
