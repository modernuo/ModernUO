/***************************************************************************
 *                                 Gump.cs
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
using System.Collections.Generic;
using System.Text;
using Server.Buffers;
using Server.Collections;
using Server.Network;

namespace Server.Gumps
{
  public class Gump
  {
    private static uint m_NextSerial = 1;

    private static byte[] m_NoMove = StringToBuffer("{ nomove }");
    private static byte[] m_NoClose = StringToBuffer("{ noclose }");
    private static byte[] m_NoDispose = StringToBuffer("{ nodispose }");
    private static byte[] m_NoResize = StringToBuffer("{ noresize }");

    internal int m_TextEntries, m_Switches;
    private int m_X, m_Y;

    public Gump(int x, int y)
    {
      do
      {
        Serial = m_NextSerial++;
      } while (Serial == 0); // standard client apparently doesn't send a gump response packet if serial == 0

      m_X = x;
      m_Y = y;

      TypeID = GetTypeID(GetType());

      Entries = new List<GumpEntry>();
    }

    public int TypeID{ get; }

    public List<GumpEntry> Entries{ get; }

    public uint Serial { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public bool Disposable { get; set; } = true;

    public bool Resizable { get; set; } = true;

    public bool Draggable { get; set; } = true;

    public bool Closable { get; set; } = true;

    public static int GetTypeID(Type type) => type?.FullName?.GetHashCode() ?? -1;

    public void AddPage(int page)
    {
      Add(new GumpPage(page));
    }

    public void AddAlphaRegion(int x, int y, int width, int height)
    {
      Add(new GumpAlphaRegion(x, y, width, height));
    }

    public void AddBackground(int x, int y, int width, int height, int gumpID)
    {
      Add(new GumpBackground(x, y, width, height, gumpID));
    }

    public void AddButton(int x, int y, int normalID, int pressedID, int buttonID,
      GumpButtonType type = GumpButtonType.Reply, int param = 0)
    {
      Add(new GumpButton(x, y, normalID, pressedID, buttonID, type, param));
    }

    public void AddCheck(int x, int y, int inactiveID, int activeID, bool initialState, int switchID)
    {
      Add(new GumpCheck(x, y, inactiveID, activeID, initialState, switchID));
    }

    public void AddGroup(int group)
    {
      Add(new GumpGroup(group));
    }

    public void AddTooltip(int number)
    {
      Add(new GumpTooltip(number));
    }

    public void AddHtml(int x, int y, int width, int height, string text, bool background = false, bool scrollbar = false)
    {
      Add(new GumpHtml(x, y, width, height, text, background, scrollbar));
    }

    public void AddHtmlLocalized(int x, int y, int width, int height, int number, bool background = false, bool scrollbar = false)
    {
      Add(new GumpHtmlLocalized(x, y, width, height, number, background, scrollbar));
    }

    public void AddHtmlLocalized(int x, int y, int width, int height, int number, int color, bool background = false,
      bool scrollbar = false)
    {
      Add(new GumpHtmlLocalized(x, y, width, height, number, color, background, scrollbar));
    }

    public void AddHtmlLocalized(int x, int y, int width, int height, int number, string args, int color,
      bool background = false, bool scrollbar = false)
    {
      Add(new GumpHtmlLocalized(x, y, width, height, number, args, color, background, scrollbar));
    }

    public void AddImage(int x, int y, int gumpID, int hue = 0)
    {
      Add(new GumpImage(x, y, gumpID, hue));
    }

    public void AddImageTiled(int x, int y, int width, int height, int gumpID)
    {
      Add(new GumpImageTiled(x, y, width, height, gumpID));
    }

    public void AddImageTiledButton(int x, int y, int normalID, int pressedID, int buttonID, GumpButtonType type,
      int param, int itemID, int hue, int width, int height, int localizedTooltip = -1)
    {
      Add(new GumpImageTileButton(x, y, normalID, pressedID, buttonID, type, param, itemID, hue, width, height,
        localizedTooltip));
    }

    public void AddItem(int x, int y, int itemID, int hue = 0)
    {
      Add(new GumpItem(x, y, itemID, hue));
    }

    public void AddLabel(int x, int y, int hue, string text)
    {
      Add(new GumpLabel(x, y, hue, text));
    }

    public void AddLabelCropped(int x, int y, int width, int height, int hue, string text)
    {
      Add(new GumpLabelCropped(x, y, width, height, hue, text));
    }

    public void AddRadio(int x, int y, int inactiveID, int activeID, bool initialState, int switchID)
    {
      Add(new GumpRadio(x, y, inactiveID, activeID, initialState, switchID));
    }

    public void AddTextEntry(int x, int y, int width, int height, int hue, int entryID, string initialText, int size = 0)
    {
      Add(new GumpTextEntryLimited(x, y, width, height, hue, entryID, initialText, size));
    }

    public void AddItemProperty(uint serial)
    {
      Add(new GumpItemProperty(serial));
    }

    public void Add(GumpEntry g)
    {
      if (g.Parent != this)
        g.Parent = this;
      else if (!Entries.Contains(g)) Entries.Add(g);
    }

    public void Remove(GumpEntry g)
    {
      if (g == null || !Entries.Contains(g))
        return;

      Entries.Remove(g);
      g.Parent = null;
    }

    public static byte[] StringToBuffer(string str) => Encoding.ASCII.GetBytes(str);

    public void SendTo(NetState state)
    {
      state.AddGump(this);
      ArrayBufferWriter<byte> bufferWriter = new ArrayBufferWriter<byte>(0x10000);
      Compile(state.Unpack, bufferWriter);
      state.Send(bufferWriter.WrittenSpan);
    }

    private void Compile(bool packed, IBufferWriter<byte> bufferWriter)
    {
      // m_Strings.Clear(); // Do we care?
      m_TextEntries = 0;
      m_Switches = 0;

      int writeLength = packed ? 19 : 21;

      SpanWriter headWriter = new SpanWriter(stackalloc byte[writeLength]);
      headWriter.Write((byte)(packed ? 0xDD : 0xB0));
      headWriter.Position += 2; // Dynamic Length

      headWriter.Write(Serial);
      headWriter.Write(TypeID);
      headWriter.Write(X);
      headWriter.Write(Y);

      ArraySet<string> strings = new ArraySet<string>();
      ArrayBufferWriter<byte> layoutBuffer = new ArrayBufferWriter<byte>();
      ArrayBufferWriter<byte> stringsBuffer = new ArrayBufferWriter<byte>();
      SpanWriter sw;

      if (!Draggable)
        layoutBuffer.Write(m_NoMove);

      if (!Closable)
        layoutBuffer.Write(m_NoClose);

      if (!Disposable)
        layoutBuffer.Write(m_NoDispose);

      if (!Resizable)
        layoutBuffer.Write(m_NoResize);

      for (int i = 0; i < Entries.Count; ++i)
        Entries[i].AppendTo(layoutBuffer, strings, ref m_TextEntries, ref m_Switches);

      if (packed)
      {
        layoutBuffer.Write(stackalloc byte[1]); // Null Terminated

        layoutBuffer = WritePacked(layoutBuffer.WrittenSpan);

        // Write the length of the strings
        sw = new SpanWriter(stringsBuffer.GetSpan(4));
        sw.Write(strings.Count);
        stringsBuffer.Advance(4);

        writeLength += layoutBuffer.WrittenCount + 4;
      }
      else
      {
        ushort layoutSize = (ushort)layoutBuffer.WrittenCount;
        headWriter.Position = 19;
        headWriter.Write(layoutSize);

        // Write the length of the strings
        sw = new SpanWriter(stringsBuffer.GetSpan(2));
        sw.Write((ushort)strings.Count);
        stringsBuffer.Advance(2);

        writeLength += layoutSize + 2;
      }

      for (int i = 0; i < strings.Count; ++i)
      {
        string v = strings[i] ?? "";

        int length = (ushort)v.Length;

        sw = new SpanWriter(stringsBuffer.GetSpan(2 + length));
        sw.Write((ushort)length);
        sw.WriteBigUniFixed(v, length);
      }

      if (packed)
        stringsBuffer = WritePacked(stringsBuffer.WrittenSpan);

      writeLength += stringsBuffer.WrittenCount;

      // Write the packet length
      headWriter.Position = 1;
      headWriter.Write((ushort)writeLength);

      bufferWriter.Write(headWriter.Span);
      bufferWriter.Write(layoutBuffer.WrittenSpan);
      bufferWriter.Write(stringsBuffer.WrittenSpan);
    }

    private ArrayBufferWriter<byte> WritePacked(ReadOnlySpan<byte> source)
    {
      ArrayBufferWriter<byte> dest = new ArrayBufferWriter<byte>();

      ulong length = (ulong)source.Length;
      ulong wantLength = Compression.MaxPackSize(length);
      Span<byte> span = dest.GetSpan((int)wantLength);

      ulong packLength = wantLength;

      Compression.Pack(span.Slice(8), ref packLength, source, length, ZLibQuality.Default);
      SpanWriter writer = new SpanWriter(span);
      int packLengthInt = (int)packLength;

      writer.Write(4 + packLengthInt);
      writer.Write(length);
      dest.Advance(8 + packLengthInt);

      return dest;
    }

    public virtual void OnResponse(NetState sender, RelayInfo info)
    {
    }

    public virtual void OnServerClose(NetState owner)
    {
    }
  }
}
