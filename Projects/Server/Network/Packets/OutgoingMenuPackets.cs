using System;
using System.Buffers;
using System.IO;
using Server.ContextMenus;
using Server.Menus.ItemLists;
using Server.Menus.Questions;

namespace Server.Network;

[Flags]
public enum CMEFlags
{
    None = 0x00,
    Disabled = 0x01,
    Arrow = 0x02,
    Highlighted = 0x04,
    Colored = 0x20
}

public static class OutgoingMenuPackets
{
    public static void SendDisplayItemListMenu(this NetState ns, ItemListMenu menu)
    {
        if (ns == null || menu == null)
        {
            return;
        }

        var question = menu.Question?.Trim();
        var questionLength = question?.Length ?? 0;

        var entries = menu.Entries;
        int entriesLength = (byte)entries.Length;

        var maxLength = 11 + questionLength;
        for (int i = 0; i < entriesLength; i++)
        {
            maxLength += 5 + entries[i].Name?.Length ?? 0; // could be trimmed
        }

        var writer = new SpanWriter(stackalloc byte[maxLength]);
        writer.Write((byte)0x7C); // Packet ID
        writer.Seek(2, SeekOrigin.Current);
        writer.Write(menu.Serial);
        writer.Write((ushort)0);

        writer.Write((byte)questionLength);

        if (question != null)
        {
            writer.WriteAscii(question);
        }

        writer.Write((byte)entriesLength);

        for (var i = 0; i < entriesLength; ++i)
        {
            var e = entries[i];

            writer.Write((ushort)e.ItemID);
            writer.Write((short)e.Hue);

            var name = e.Name?.Trim();

            if (name == null)
            {
                writer.Write((byte)0);
            }
            else
            {
                var nameLength = name.Length;
                writer.Write((byte)nameLength);
                writer.WriteAscii(name);
            }
        }

        writer.WritePacketLength();
        ns.Send(writer.Span);
    }

    public static void SendDisplayQuestionMenu(this NetState ns, QuestionMenu menu)
    {
        if (ns == null || menu == null)
        {
            return;
        }

        var question = menu.Question?.Trim();
        var questionLength = question?.Length ?? 0;

        var answers = menu.Answers;
        int answersLength = (byte)answers.Length;

        var maxLength = 11 + questionLength;
        for (int i = 0; i < answersLength; i++)
        {
            maxLength += 5 + answers[i]?.Length ?? 0; // could be trimmed
        }

        var writer = new SpanWriter(stackalloc byte[maxLength]);
        writer.Write((byte)0x7C); // Packet ID
        writer.Seek(2, SeekOrigin.Current);
        writer.Write(menu.Serial);
        writer.Write((ushort)0);
        writer.Write((byte)questionLength);

        if (question != null)
        {
            writer.WriteAscii(question);
        }

        writer.Write((byte)answersLength);

        for (var i = 0; i < answersLength; ++i)
        {
            writer.Write(0);

            var answer = answers[i]?.Trim();

            if (answer == null)
            {
                writer.Write((byte)0);
            }
            else
            {
                var nameLength = answer.Length;
                writer.Write((byte)nameLength);
                writer.WriteAscii(answer);
            }
        }

        writer.WritePacketLength();
        ns.Send(writer.Span);
    }

    public static void SendDisplayContextMenu(this NetState ns, ContextMenu menu)
    {
        if (ns == null || menu == null)
        {
            return;
        }

        var newCommand = ns.NewHaven && menu.RequiresNewPacket;

        var entries = menu.Entries;
        var entriesLength = (byte)entries.Length;
        var maxLength = 12 + entriesLength * 8;

        var writer = new SpanWriter(stackalloc byte[maxLength]);
        writer.Write((byte)0xBF);                        // Packet ID
        writer.Seek(2, SeekOrigin.Current);              // Length
        writer.Write((short)0x14);                       // Subpacket
        writer.Write((short)(newCommand ? 0x02 : 0x01)); // Command

        var target = menu.Target;
        writer.Write(target.Serial);
        writer.Write(entriesLength);

        var p = target switch
        {
            Mobile _  => target.Location,
            Item item => item.GetWorldLocation(),
            _         => Point3D.Zero
        };

        for (var i = 0; i < entriesLength; ++i)
        {
            var e = entries[i];

            var range = e.Range;

            if (range == -1)
            {
                range = Core.GlobalUpdateRange;
            }

            var flags = e.Flags;
            if (!(e.Enabled && menu.From.InRange(p, range)))
            {
                flags |= CMEFlags.Disabled;
            }

            if (newCommand)
            {
                writer.Write(e.Number);
                writer.Write((short)i);
                writer.Write((short)flags);
            }
            else
            {
                writer.Write((short)i);
                writer.Write((ushort)(e.Number - 3000000));

                var color = e.Color & 0xFFFF;

                if (color != 0xFFFF)
                {
                    flags |= CMEFlags.Colored;
                }

                writer.Write((short)flags);

                if ((flags & CMEFlags.Colored) != 0)
                {
                    writer.Write((short)color);
                }
            }
        }

        writer.WritePacketLength();
        ns.Send(writer.Span);
    }
}
