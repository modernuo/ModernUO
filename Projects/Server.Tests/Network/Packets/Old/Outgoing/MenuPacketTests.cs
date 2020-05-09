using System;
using System.Collections.Generic;
using System.Linq;
using Server.ContextMenus;
using Server.Menus.ItemLists;
using Server.Menus.Questions;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  internal class ContextMenuItem : Item
  {
    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
      base.GetContextMenuEntries(from, list);

      list.Add(new ContextMenuEntry(500000));
      list.Add(new ContextMenuEntry(500001));
      list.Add(new ContextMenuEntry(500002));
    }

    public ContextMenuItem(Serial serial) : base(serial)
    {
    }
  }

  public class MenuPacketTests : IClassFixture<ServerFixture>
  {
    [Fact]
    public void TestDisplayItemListMenu()
    {
      var menu = new ItemListMenu(
        "Which item would you choose?",
        new[]
        {
          new ItemListEntry("Item 1", 0x01),
          new ItemListEntry("Item 2", 0x100),
          new ItemListEntry("Item 3", 0x1000, 250)
        }
      );

      string question = menu.Question?.Trim() ?? "";
      int questionLength = Math.Min(255, question.Length);

      int length = 11 + questionLength + menu.Entries.Sum(entry => 5 + entry.Name?.Trim().Length ?? 0);

      Span<byte> data = new DisplayItemListMenu(menu).Compile();

      Span<byte> expectedData = stackalloc byte[length];

      int pos = 0;

      expectedData[pos++] = 0x7C; // Packet ID
      ((ushort)length).CopyTo(ref pos, expectedData);
      menu.Serial.CopyTo(ref pos, expectedData);
      pos += 2; // ((ushort)0x00).CopyTo(ref pos, expectedData);
      expectedData[pos++] = (byte)questionLength;
      question.CopyASCIITo(ref pos, expectedData);
      expectedData[pos++] = (byte)menu.Entries.Length;
      for (int i = 0; i < menu.Entries.Length; i++)
      {
        var entry = menu.Entries[i];
        ((ushort)entry.ItemID).CopyTo(ref pos, expectedData);
        ((ushort)entry.Hue).CopyTo(ref pos, expectedData);
        string name = entry.Name?.Trim() ?? "";

        expectedData[pos++] = (byte)name.Length;
        name.CopyASCIITo(ref pos, expectedData);
      }

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestDisplayQuestionMenu()
    {
      var menu = new QuestionMenu(
        "Which option would you choose?",
        new[]
        {
          "Option 1",
          "Option 2",
          "Option 3"
        }
      );

      string question = menu.Question?.Trim() ?? "";
      int questionLength = Math.Min(255, question.Length);

      int length = 11 + questionLength + menu.Answers.Sum(answer => 5 + answer?.Trim().Length ?? 0);

      Span<byte> data = new DisplayQuestionMenu(menu).Compile();

      Span<byte> expectedData = stackalloc byte[length];

      int pos = 0;

      expectedData[pos++] = 0x7C; // Packet ID
      ((ushort)length).CopyTo(ref pos, expectedData);
      menu.Serial.CopyTo(ref pos, expectedData);
      pos += 2; // ((ushort)0x00).CopyTo(ref pos, expectedData);
      expectedData[pos++] = (byte)questionLength;
      question?.CopyASCIITo(ref pos, expectedData);
      expectedData[pos++] = (byte)menu.Answers.Length;
      for (int i = 0; i < menu.Answers.Length; i++)
      {
        pos += 4; // 0x0.CopyTo(ref pos, expectedData);
        var answer = menu.Answers[i]?.Trim() ?? "";
        expectedData[pos++] = (byte)answer.Length;
        answer.CopyASCIITo(ref pos, expectedData);
      }

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestDisplayContextMenu()
    {
      var m = new Mobile(0x1);
      m.DefaultMobileInit();

      var item = new ContextMenuItem(Serial.LastItem + 1);
      var menu = new ContextMenu(m, item);

      Span<byte> data = new DisplayContextMenu(menu).Compile();

      int length = 12 + menu.Entries.Length * 8;

      Span<byte> expectedData = stackalloc byte[length];

      int pos = 0;
      expectedData[pos++] = 0xBF; // Packet ID
      ((ushort)length).CopyTo(ref pos, expectedData); // Length
      ((ushort)0x14).CopyTo(ref pos, expectedData); // Command
      ((ushort)0x02).CopyTo(ref pos, expectedData); // Subcommand
      menu.Target.Serial.CopyTo(ref pos, expectedData);
      var entries = menu.Entries;

      expectedData[pos++] = (byte)entries.Length;

      for (int i = 0; i < entries.Length; i++)
      {
        var entry = entries[i];
        entry.Number.CopyTo(ref pos, expectedData);
        ((ushort)i).CopyTo(ref pos, expectedData);

        var flags = entry.Flags;

        var range = entry.Range;

        if (range == -1)
          range = 18;

        if (!(entry.Enabled && menu.From.InRange(item.GetWorldLocation(), range)))
          flags |= CMEFlags.Disabled;

        ((ushort)flags).CopyTo(ref pos, expectedData);
      }

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestDisplayContextMenuOld()
    {
      var m = new Mobile(0x1);
      m.DefaultMobileInit();

      var item = new ContextMenuItem(Serial.LastItem + 1);
      var menu = new ContextMenu(m, item);

      Span<byte> data = new DisplayContextMenuOld(menu).Compile();

      int length = 12 + menu.Entries.Sum(entry => 6 + ((entry.Color & 0xFFFF) != 0xFFFF ? 2 : 0));

      Span<byte> expectedData = stackalloc byte[length];

      int pos = 0;
      expectedData[pos++] = 0xBF; // Packet ID
      ((ushort)length).CopyTo(ref pos, expectedData); // Length
      ((ushort)0x14).CopyTo(ref pos, expectedData); // Command
      ((ushort)0x01).CopyTo(ref pos, expectedData); // Subcommand
      menu.Target.Serial.CopyTo(ref pos, expectedData);
      var entries = menu.Entries;

      expectedData[pos++] = (byte)entries.Length;

      for (int i = 0; i < entries.Length; i++)
      {
        var entry = entries[i];
        ((ushort)i).CopyTo(ref pos, expectedData);
        ((ushort)(entry.Number - 3000000)).CopyTo(ref pos, expectedData);

        var flags = entry.Flags;

        var color = entry.Color & 0xFFFF;

        if (color != 0xFFFF)
          flags |= CMEFlags.Colored;

        var range = entry.Range;

        if (range == -1)
          range = 18;

        if (!(entry.Enabled && menu.From.InRange(item.GetWorldLocation(), range)))
          flags |= CMEFlags.Disabled;

        ((ushort)flags).CopyTo(ref pos, expectedData);

        if ((flags & CMEFlags.Colored) != 0)
          ((ushort)color).CopyTo(ref pos, expectedData);
      }

      AssertThat.Equal(data, expectedData);
    }
  }
}
