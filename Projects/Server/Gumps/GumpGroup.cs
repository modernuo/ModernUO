/***************************************************************************
 *                                GumpGroup.cs
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

using System.Buffers;
using Server.Buffers;

namespace Server.Gumps
{
  public class GumpGroup : GumpEntry
  {
    private int m_Group;

    public GumpGroup(int group)
    {
      m_Group = group;
    }

    public int Group
    {
      get => m_Group;
      set => Delta(ref m_Group, value);
    }

    public override string Compile() => $"{{ group {m_Group} }}";

    private static byte[] m_LayoutName = Gump.StringToBuffer("{ group ");

    public override void AppendTo(ArrayBufferWriter<byte> buffer, ref int entries, ref int switches)
    {
      SpanWriter writer = new SpanWriter(buffer.GetSpan(20));
      writer.Write(m_LayoutName);
      writer.WriteAscii(m_Group.ToString());
      writer.Write((byte)0x20); // ' '
      writer.Write((byte)0x7D); // '}'
      buffer.Advance(writer.WrittenCount);
    }
  }
}