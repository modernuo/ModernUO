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

    public Gump(int x, int y)
    {
      do
      {
        Serial = m_NextSerial++;
      } while (Serial == 0); // standard client apparently doesn't send a gump response packet if serial == 0

      X = x;
      Y = y;

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

      Span<byte> layoutSpan =
        stackalloc byte[Math.Max(layoutBuffer.WrittenCount, GetMaxPackedSize(layoutBuffer.WrittenCount))];
      ReadOnlySpan<byte> layoutReadOnlySpan = layoutSpan;

      SpanWriter sw;

      if (packed)
      {
        layoutBuffer.Write(stackalloc byte[]{0x00}); // Null Terminated

        writeLength += WritePacked(layoutBuffer.WrittenSpan, layoutSpan);

        // Write the length of the strings
        sw = new SpanWriter(stringsBuffer.GetSpan(4));
        sw.Write(strings.Count);
        stringsBuffer.Advance(4);

        writeLength += 4;
      }
      else
      {
        ushort layoutSize = (ushort)layoutBuffer.WrittenCount;
        headWriter.Write(layoutSize);
        layoutReadOnlySpan = layoutBuffer.WrittenSpan;

        // Write the length of the strings
        stringsBuffer.Write(stackalloc byte[]
        {
          (byte)(strings.Count >> 8),
          (byte)strings.Count
        });

        writeLength += layoutSize + 2;
      }

      for (int i = 0; i < strings.Count; ++i)
      {
        string v = strings[i] ?? "";

        int length = 2 + v.Length * 2;
        sw = new SpanWriter(stringsBuffer.GetSpan(length));
        sw.Write((ushort)v.Length);
        sw.WriteBigUni(v);
        stringsBuffer.Advance(length);
      }

      Span<byte> stringsSpan =
        stackalloc byte[Math.Max(stringsBuffer.WrittenCount, GetMaxPackedSize(stringsBuffer.WrittenCount))];
      ReadOnlySpan<byte> stringsReadOnlySpan = stringsSpan;

      if (packed)
        writeLength += WritePacked(stringsBuffer.WrittenSpan.Slice(4), stringsSpan);
      else
      {
        writeLength += stringsBuffer.WrittenCount;
        stringsReadOnlySpan = stringsBuffer.WrittenSpan;
      }

      // Write the packet length
      headWriter.Position = 1;
      headWriter.Write((ushort)writeLength);

      bufferWriter.Write(headWriter.Span);
      bufferWriter.Write(layoutReadOnlySpan);
      bufferWriter.Write(stringsReadOnlySpan);
    }

    private static int GetMaxPackedSize(int sourceLength) => 8 + Compression.MaxPackSize(sourceLength);

    private static int WritePacked(ReadOnlySpan<byte> source, Span<byte> dest)
    {
      int length = source.Length;

      if (length == 0)
      {
        dest.Clear();
        return 4;
      }

      int packLength = dest.Length;

      Compression.Pack(dest.Slice(8), ref packLength, source, ZLibQuality.Default);
      SpanWriter writer = new SpanWriter(dest);

      writer.Write(4 + packLength);
      writer.Write(length);

      return 8 + packLength;
    }

    public virtual void OnResponse(NetState sender, RelayInfo info)
    {
    }

    public virtual void OnServerClose(NetState owner)
    {
    }
  }
}
