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
using System.Linq;
using System.Runtime.InteropServices;
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

    public void Invalidate()
    {
      //if ( m_Strings.Count > 0 )
      //	m_Strings.Clear();
    }

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

    public void AddTextEntry( int x, int y, int width, int height, int hue, int entryID, string initialText )
    {
      Add( new GumpTextEntry( x, y, width, height, hue, entryID, initialText ) );
    }

    public void AddTextEntry(int x, int y, int width, int height, int hue, int entryID, string initialText, int size)
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
      m_TextEntries = 0;
      m_Switches = 0;
      if (state.Unpack)
        CompilePacked(bufferWriter);
      else
        CompileFast(bufferWriter);
      state.Send(bufferWriter.WrittenSpan);
    }

    private void WriteLayout(ArrayBufferWriter<byte> layoutBuffer, ArraySet<string> strings)
    {
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
    }

    private static void WriteStrings(IBufferWriter<byte> stringsBuffer, IList<string> strings)
    {
      int stringLength = 2 * strings.Count + strings.Sum(t => t.Length * 2);
      SpanWriter rawStringsWriter = new SpanWriter(stringsBuffer.GetSpan(stringLength));

      for (int i = 0; i < strings.Count; ++i)
      {
        string v = strings[i] ?? "";

        rawStringsWriter.Write((ushort)v.Length);
        rawStringsWriter.WriteBigUni(v);
      }

      stringsBuffer.Advance(stringLength);
    }

    public static byte[] StringToBuffer(string str) => Encoding.ASCII.GetBytes(str);

    private void CompileFast(IBufferWriter<byte> bufferWriter)
    {
      SpanWriter writer = new SpanWriter(bufferWriter.GetSpan(21));
      writer.Write((byte)0xB0); // Packed ID
      writer.Position += 2; // Dynamic Length

      writer.Write(Serial);
      writer.Write(TypeID);
      writer.Write(X);
      writer.Write(Y);

      ArraySet<string> strings = new ArraySet<string>();
      ArrayBufferWriter<byte> layoutBuffer = new ArrayBufferWriter<byte>();
      WriteLayout(layoutBuffer, strings);

      writer.Write((ushort)layoutBuffer.WrittenCount);

      ArrayBufferWriter<byte> stringsBuffer = new ArrayBufferWriter<byte>();
      Span<byte> span = stringsBuffer.GetSpan(2);
      span[0] = (byte)(strings.Count >> 8);
      span[1] = (byte)strings.Count;
      bufferWriter.Advance(2);
      WriteStrings(stringsBuffer, strings);

      writer.Position = 1;
      writer.Write((ushort)(23 + layoutBuffer.WrittenCount + stringsBuffer.WrittenCount));

      // Write the header
      bufferWriter.Advance(21);

      // Write the layout
      bufferWriter.Write(layoutBuffer.WrittenSpan);

      // Write strings
      bufferWriter.Write(stringsBuffer.WrittenSpan);
    }

    private static int GetMaxPackedSize(int sourceLength) => 8 + (int)Compression.MaxPackSize((ulong)sourceLength);

    private static int WritePacked(ReadOnlySpan<byte> source, Span<byte> dest)
    {
      int length = source.Length;

      if (length == 0)
      {
        dest.Clear();
        return 4;
      }

      ulong packLength = (ulong)dest.Length - 8;

      ZLibError ce = Compression.Pack(dest.Slice(8), ref packLength, source, ZLibQuality.Default);
      if (ce != ZLibError.Okay) Console.WriteLine("ZLib error: {0} (#{1})", ce, (int)ce);
      SpanWriter writer = new SpanWriter(dest);
      int packLengthInt = (int)packLength;
      writer.Write(4 + packLengthInt);
      writer.Write(length);

      return 8 + packLengthInt;
    }

    public virtual void OnResponse(NetState sender, RelayInfo info)
    {
    }

    public virtual void OnServerClose(NetState owner)
    {
    }
  }
}
