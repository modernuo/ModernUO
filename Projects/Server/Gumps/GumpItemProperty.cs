/***************************************************************************
 *                            GumpItemProperty.cs
 *                            -------------------
 *   begin                : May 26, 2013
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
using Server.Collections;

namespace Server.Gumps
{
  public class GumpItemProperty : GumpEntry
  {
    public GumpItemProperty(uint serial) => Serial = serial;

    public uint Serial { get; set; }

    public override string Compile(ArraySet<string> strings) => $"{{ itemproperty {Serial} }}";

    private static readonly byte[] m_LayoutName = Gump.StringToBuffer("{ itemproperty ");

    public override void AppendTo(ArrayBufferWriter<byte> buffer, ArraySet<string> strings, ref int entries, ref int switches)
    {
      SpanWriter writer = new SpanWriter(buffer.GetSpan(27));
      writer.Write(m_LayoutName);
      writer.WriteAscii(Serial.ToString());
      writer.Write((byte)0x20); // ' '
      writer.Write((byte)0x7D); // '}'

      buffer.Advance(writer.WrittenCount);
    }
  }
}
