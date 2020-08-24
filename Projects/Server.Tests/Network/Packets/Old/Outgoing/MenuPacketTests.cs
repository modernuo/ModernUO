using System;
using System.Buffers;
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

            Span<byte> data = new DisplayItemListMenu(menu).Compile();

            string question = menu.Question;
            int questionLength = Math.Min(255, question.Length);
            int entriesCount = 0;
            int length = 11 + questionLength;

            foreach (var entry in menu.Entries)
            {
                length += 5 + entry.Name.Length;
                if (entriesCount == 255)
                    break;

                entriesCount++;
            }

            Span<byte> expectedData = stackalloc byte[length];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x7C); // Packet ID
            expectedData.Write(ref pos, (ushort)length);
            expectedData.Write(ref pos, menu.Serial);
            expectedData.Write(ref pos, (ushort)0x00);
            expectedData.Write(ref pos, (byte)questionLength);
            expectedData.WriteAscii(ref pos, question, 255);
            expectedData.Write(ref pos, (byte)entriesCount);
            for (int i = 0; i < entriesCount; i++)
            {
                var entry = menu.Entries[i];
                expectedData.Write(ref pos, (ushort)entry.ItemID);
                expectedData.Write(ref pos, (ushort)entry.Hue);
                string name = entry.Name?.Trim() ?? "";
                expectedData.Write(ref pos, (byte)Math.Min(255, name.Length));
                expectedData.WriteAscii(ref pos, name, 255);
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

            Span<byte> data = new DisplayQuestionMenu(menu).Compile();

            string question = menu.Question;
            int questionLength = Math.Min(255, question.Length);
            int answersCount = 0;
            int length = 11 + questionLength;

            foreach (var answer in menu.Answers)
            {
                length += 5 + answer.Length;
                if (answersCount == 255)
                    break;

                answersCount++;
            }

            Span<byte> expectedData = stackalloc byte[length];

            int pos = 0;

            expectedData.Write(ref pos, (byte)0x7C); // Packet ID
            expectedData.Write(ref pos, (ushort)length);
            expectedData.Write(ref pos, menu.Serial);
            expectedData.Write(ref pos, (ushort)0x00);
            expectedData.Write(ref pos, (byte)question.Length);
            expectedData.WriteAscii(ref pos, question, 255);
            expectedData.Write(ref pos, (byte)answersCount);
            for (int i = 0; i < answersCount; i++)
            {
                var answer = menu.Answers[i];
#if NO_LOCAL_INIT
        expectedData.Write(ref pos, 0);
#else
                pos += 4;
#endif
                expectedData.Write(ref pos, (byte)Math.Min(255, answer.Length));
                expectedData.WriteAscii(ref pos, answer, 255);
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
            expectedData.Write(ref pos, (byte)0xBF); // Packet ID
            expectedData.Write(ref pos, (ushort)length); // Length
            expectedData.Write(ref pos, (ushort)0x14); // Command
            expectedData.Write(ref pos, (ushort)0x02); // Subcommand
            expectedData.Write(ref pos, menu.Target.Serial);
            var entries = menu.Entries;

            expectedData.Write(ref pos, (byte)entries.Length);

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                expectedData.Write(ref pos, entry.Number);
                expectedData.Write(ref pos, (ushort)i);

                var flags = entry.Flags;

                var range = entry.Range;

                if (range == -1)
                    range = 18;

                if (!(entry.Enabled && menu.From.InRange(item.GetWorldLocation(), range)))
                    flags |= CMEFlags.Disabled;

                expectedData.Write(ref pos, (ushort)flags);
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
            expectedData.Write(ref pos, (byte)0xBF); // Packet ID
            expectedData.Write(ref pos, (ushort)length); // Length
            expectedData.Write(ref pos, (ushort)0x14); // Command
            expectedData.Write(ref pos, (ushort)0x01); // Subcommand
            expectedData.Write(ref pos, menu.Target.Serial);
            var entries = menu.Entries;

            expectedData.Write(ref pos, (byte)entries.Length);

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                expectedData.Write(ref pos, (ushort)i);
                expectedData.Write(ref pos, (ushort)(entry.Number - 3000000));

                var flags = entry.Flags;

                var color = entry.Color & 0xFFFF;

                if (color != 0xFFFF)
                    flags |= CMEFlags.Colored;

                var range = entry.Range;

                if (range == -1)
                    range = 18;

                if (!(entry.Enabled && menu.From.InRange(item.GetWorldLocation(), range)))
                    flags |= CMEFlags.Disabled;

                expectedData.Write(ref pos, (ushort)flags);

                if ((flags & CMEFlags.Colored) != 0)
                    expectedData.Write(ref pos, (ushort)color);
            }

            AssertThat.Equal(data, expectedData);
        }
    }
}
