using System;
using Server.Buffers;
using Server.ContextMenus;
using Server.Menus;
using Server.Menus.ItemLists;
using Server.Menus.Questions;

namespace Server.Network
{
  [Flags]
  public enum CMEFlags
  {
    None = 0x00,
    Disabled = 0x01,
    Arrow = 0x02,
    Highlighted = 0x04,
    Colored = 0x20
  }

  public static partial class Packets
  {
    public static void SendDisplayItemListMenu(NetState ns, ItemListMenu menu)
    {
      // 10 + 128 + (255 * (128 + 5))
      SpanWriter w = new SpanWriter(stackalloc byte[138 + menu.Entries.Length * 133]);
      w.Write((byte)0x7C); // Packet ID
      w.Position += 2; // Dynamic Length

      w.Write(((IMenu)menu).Serial);
      w.Position += 2; // w.Write((short)0);

      string question = menu.Question;

      if (question == null)
        w.Position++; // w.Write((byte)0);
      else
      {
        int questionLength = question.Length;
        w.Write((byte)questionLength);
        w.WriteAsciiFixed(question, questionLength);
      }

      ItemListEntry[] entries = menu.Entries;

      int entriesLength = (byte)entries.Length;

      w.Write((byte)entriesLength);

      for (int i = 0; i < entriesLength; ++i)
      {
        ItemListEntry e = entries[i];

        w.Write((ushort)e.ItemID);
        w.Write((short)e.Hue);

        string name = e.Name;

        if (name == null)
          w.Position++; // w.Write((byte)0);
        else
        {
          int nameLength = name.Length;
          w.Write((byte)nameLength);
          w.WriteAsciiFixed(name, nameLength);
        }
      }

      w.Position = 1;
      w.Write((ushort)w.WrittenCount);

      ns.Send(w.Span);
    }

    public static void SendDisplayQuestionMenu(NetState ns, QuestionMenu menu)
    {
      // 10 + 128 + (255 * (128 + 5))
      SpanWriter w = new SpanWriter(stackalloc byte[138 + menu.Entries.Length * 133]);
      w.Write((byte)0x7C); // Packet ID
      w.Position += 2; // Dynamic Length

      w.Write(((IMenu)menu).Serial);
      w.Position += 2; // w.Write((short)0);

      string question = menu.Question;

      if (question == null)
        w.Position++; // w.Write((byte)0);
      else
      {
        int questionLength = question.Length;
        w.Write((byte)questionLength);
        w.WriteAsciiFixed(question, questionLength);
      }

      string[] answers = menu.Answers;

      int answersLength = (byte)answers.Length;

      w.Write((byte)answersLength);

      for (int i = 0; i < answersLength; ++i)
      {
        w.Position += 4; // w.Write(0);

        string answer = answers[i];

        if (answer == null)
          w.Position++; // w.Write((byte)0);
        else
        {
          int answerLength = answer.Length;
          w.Write((byte)answerLength);
          w.WriteAsciiFixed(answer, answerLength);
        }
      }

      w.Position = 1;
      w.Write((ushort)w.WrittenCount);

      ns.Send(w.Span);
    }

    public static void DisplayContextMenu(NetState ns, ContextMenu menu)
    {
      ContextMenuEntry[] entries = menu.Entries;

      int length = 12 + entries.Length * 8;
      SpanWriter w = new SpanWriter(stackalloc byte[length]);
      w.Write((byte)0xBF); // Packet ID
      w.Write((short)length); // Length

      w.Write((short)0x14);
      w.Write((short)0x02);

      IEntity target = menu.Target as IEntity;

      w.Write(target?.Serial ?? Serial.MinusOne);

      w.Write((byte)entries.Length);

      Point3D p;

      if (target is Mobile)
        p = target.Location;
      else if (target is Item item)
        p = item.GetWorldLocation();
      else
        p = Point3D.Zero;

      for (int i = 0; i < entries.Length; ++i)
      {
        ContextMenuEntry e = entries[i];

        w.Write(e.Number);
        w.Write((short)i);

        int range = e.Range;

        if (range == -1)
          range = 18;

        w.Write((short)(e.Flags | (e.Enabled && menu.From.InRange(p, range) ? CMEFlags.None : CMEFlags.Disabled)));
      }

      ns.Send(w.Span);
    }

    public static void SendDisplayContextMenuOld(NetState ns, ContextMenu menu)
    {
      ContextMenuEntry[] entries = menu.Entries;

      SpanWriter w = new SpanWriter(stackalloc byte[12 + entries.Length * 8]);
      w.Write((byte)0xBF); // Packet ID
      w.Position += 2; // Dynamic Length

      w.Write((short)0x14);
      w.Write((short)0x01); // Old!

      IEntity target = menu.Target as IEntity;

      w.Write(target?.Serial ?? Serial.MinusOne);

      w.Write((byte)entries.Length);

      Point3D p;

      if (target is Mobile)
        p = target.Location;
      else if (target is Item item)
        p = item.GetWorldLocation();
      else
        p = Point3D.Zero;

      for (int i = 0; i < entries.Length; ++i)
      {
        ContextMenuEntry e = entries[i];

        w.Write((short)i);
        w.Write((ushort)(e.Number - 3000000));

        int range = e.Range;

        if (range == -1)
          range = 18;

        CMEFlags flags = e.Flags | (e.Enabled && menu.From.InRange(p, range) ? CMEFlags.None : CMEFlags.Disabled);

        int color = e.Color & 0xFFFF;

        if (color != 0xFFFF)
          flags |= CMEFlags.Colored;

        w.Write((short)flags);

        if ((flags & CMEFlags.Colored) != 0)
          w.Write((short)color);
      }

      w.Position = 1;
      w.Write((ushort)w.WrittenCount);

      ns.Send(w.Span);
    }
  }
}
