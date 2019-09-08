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
using System.Collections.Generic;
using System.Text;
using Server.Buffers;
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
    private List<string> m_Strings;

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
      m_Strings = new List<string>();
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
      {
        g.Parent = this;
      }
      else if (!Entries.Contains(g))
      {
        Entries.Add(g);
      }
    }

    public void Remove(GumpEntry g)
    {
      if (g == null || !Entries.Contains(g))
        return;

      Entries.Remove(g);
      g.Parent = null;
    }

    public int Intern(string value)
    {
      int indexOf = m_Strings.IndexOf(value);

      if (indexOf >= 0) return indexOf;

      m_Strings.Add(value);
      return m_Strings.Count - 1;
    }

    public static byte[] StringToBuffer(string str) => Encoding.ASCII.GetBytes(str);

    public void SendTo(NetState state)
    {
      state.AddGump(this);
      Span<byte> buffer = stackalloc byte[Packets.MaxPacketSize];
      state.Send(buffer.Slice(0, Compile(state?.Unpack == true, buffer)));
    }

    private int Compile(bool packed, Span<byte> buffer)
    {
      SpanWriter compiledWriter = new SpanWriter(buffer);
      compiledWriter.Write((byte)(packed ? 0xDD : 0xB0));
      compiledWriter.Position += 2; // Dynamic Length

      compiledWriter.Write(Serial);
      compiledWriter.Write(TypeID);
      compiledWriter.Write(X);
      compiledWriter.Write(Y);

      if (!packed)
        compiledWriter.Position += 2; // Old has Layout Length

      SpanWriter writer = packed ? new SpanWriter(stackalloc byte[0x8000]) : compiledWriter;

      if (!Draggable)
        writer.Write(m_NoMove);

      if (!Closable)
        writer.Write(m_NoClose);

      if (!Disposable)
        writer.Write(m_NoDispose);

      if (!Resizable)
        writer.Write(m_NoResize);

      for (int i = 0; i < Entries.Count; ++i)
        Entries[i].AppendTo(writer, ref m_TextEntries, ref m_Switches);

      if (packed)
      {
        writer.Position++; // Null terminated
        WritePacked(writer.Span, compiledWriter);
      }
      else
      {
        int pos = writer.Position;
        writer.Position = 19;
        writer.Write((ushort)(pos - 21)); // Length of the layout
      }

      compiledWriter.Write((ushort)m_Strings.Count);

      writer = packed ? new SpanWriter(stackalloc byte[0x8000]) : compiledWriter;

      for (int i = 0; i < m_Strings.Count; ++i)
      {
        string v = m_Strings[i] ?? "";

        int length = (ushort)v.Length;

        writer.Write((ushort)length);
        writer.WriteBigUniFixed(v, length);
      }

      if (packed)
        WritePacked(writer.Span, compiledWriter);

      int bytesWritten = compiledWriter.Position;
      compiledWriter.Position = 1;
      compiledWriter.Write((ushort)bytesWritten);

      return bytesWritten;
    }

    private void WritePacked(ReadOnlySpan<byte> source, SpanWriter dest)
    {
      int length = source.Length;
      dest.Position += 4;

      if (length == 0)
        return;

      dest.Write(length);

      int packLength = 0;

      Compression.Pack(dest.Span.Slice(dest.Position), ref packLength, source, length, ZLibQuality.Default);

      dest.Position -= 8;
      dest.Write(packLength);
      dest.Position += 4 + packLength;
    }

    public virtual void OnResponse(NetState sender, RelayInfo info)
    {
    }

    public virtual void OnServerClose(NetState owner)
    {
    }
  }
}
