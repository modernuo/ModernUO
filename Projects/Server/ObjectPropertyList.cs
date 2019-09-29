/***************************************************************************
 *                           ObjectPropertyList.cs
 *                            -------------------
 *   begin                : May 1, 2002
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

using System;
using System.Buffers;
using System.Text;
using Server.Buffers;
using Server.Network;

namespace Server
{
  public sealed class ObjectPropertyList
  {
    // Each of these are localized to "~1_NOTHING~" which allows the string argument to be used
    // TODO: There are more of these, add them.
    private static readonly int[] m_StringNumbers =
    {
      1042971,
      1070722
    };

    private int m_Hash;
    private int m_Strings;

    private ArrayBufferWriter<byte> m_Buffer = new ArrayBufferWriter<byte>();

    public ObjectPropertyList(IEntity e) => Entity = e;

    public IEntity Entity{ get; }

    public int Hash => 0x40000000 + m_Hash;

    public int Header{ get; set; }

    public string HeaderArgs{ get; set; }

    public static bool Enabled{ get; set; }

    public void Send(NetState ns)
    {
      if (ns == null)
        return;

      ReadOnlySpan<byte> body = m_Buffer.WrittenSpan;

      short length = (short)(19 + body.Length);
      SpanWriter writer = new SpanWriter(stackalloc byte[length]);
      writer.Write((byte)0xD6); // Packet ID
      writer.Write(length); // Dynamic Length

      writer.Write((short)1); // Command
      writer.Write(Entity.Serial);
      writer.Position += 2;
      writer.Write(m_Hash);
      writer.Write(body);
      writer.Position += 4;

      ns.Send(writer.Span);
    }

    public void Add(int number)
    {
      if (number == 0)
        return;

      AddHash(number);

      if (Header == 0)
      {
        Header = number;
        HeaderArgs = "";
      }

      SpanWriter writer = new SpanWriter(m_Buffer.GetSpan(6));
      writer.Write(number);
      writer.Write((short)0);

      m_Buffer.Advance(6);
    }

    public void AddHash(int val)
    {
      m_Hash ^= val & 0x3FFFFFF;
      m_Hash ^= (val >> 26) & 0x3F;
    }

    public void Add(int number, string arguments)
    {
      if (number == 0)
        return;

      arguments ??= "";

      if (Header == 0)
      {
        Header = number;
        HeaderArgs = arguments;
      }

      AddHash(number);
      AddHash(arguments.GetHashCode());

      int argLength = Encoding.Unicode.GetByteCount(arguments);
      int length = 6 + argLength;
      SpanWriter writer = new SpanWriter(m_Buffer.GetSpan(length));
      writer.Write(number);
      writer.Write((short)argLength);
      writer.WriteLittleUni(arguments);

      m_Buffer.Advance(length);
    }

    public void Add(int number, string format, object arg0)
    {
      Add(number, string.Format(format, arg0));
    }

    public void Add(int number, string format, object arg0, object arg1)
    {
      Add(number, string.Format(format, arg0, arg1));
    }

    public void Add(int number, string format, object arg0, object arg1, object arg2)
    {
      Add(number, string.Format(format, arg0, arg1, arg2));
    }

    public void Add(int number, string format, params object[] args)
    {
      Add(number, string.Format(format, args));
    }

    private int GetStringNumber() => m_StringNumbers[m_Strings++ % m_StringNumbers.Length];

    public void Add(string text)
    {
      Add(GetStringNumber(), text);
    }

    public void Add(string format, string arg0)
    {
      Add(GetStringNumber(), string.Format(format, arg0));
    }

    public void Add(string format, string arg0, string arg1)
    {
      Add(GetStringNumber(), string.Format(format, arg0, arg1));
    }

    public void Add(string format, string arg0, string arg1, string arg2)
    {
      Add(GetStringNumber(), string.Format(format, arg0, arg1, arg2));
    }

    public void Add(string format, params object[] args)
    {
      Add(GetStringNumber(), string.Format(format, args));
    }

    public void SendOPLInfo(NetState ns)
    {
      SpanWriter writer = new SpanWriter(stackalloc byte[9]);
      writer.Write((byte)0xDC); // Packet ID

      writer.Write(Entity.Serial);
      writer.Write(m_Hash);

      ns.Send(writer.Span);
    }
  }
}
