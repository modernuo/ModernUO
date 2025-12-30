using System.Buffers;
using Server.Network;

namespace Server.Engines.BuffIcons;

public static class BuffIconPackets
{
    public static void SendAddBuffPacket(
        this NetState ns,
        Serial mob,
        BuffIcon iconID,
        int titleCliloc,
        int secondaryCliloc,
        TextDefinition args,
        long seconds
    )
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var hasArgs = args != null;
        var length = hasArgs ? args.ToString()!.Length * 2 + 52 : 46;
        var writer = new SpanWriter(stackalloc byte[length]);
        writer.Write((byte)0xDF); // Packet ID
        writer.Write((ushort)length);
        writer.Write(mob);
        writer.Write((short)iconID);
        writer.Write((short)0x1); // command (0 = remove, 1 = add, 2 = data)
        writer.Write(0);

        writer.Write((short)iconID);
        writer.Write((short)0x1); // command (0 = remove, 1 = add, 2 = data)
        writer.Write(0);

        writer.Write((short)seconds);
        writer.Clear(3);
        writer.Write(titleCliloc);
        writer.Write(secondaryCliloc);

        if (hasArgs)
        {
            writer.Write(0);
            writer.Write((short)0x1);
            writer.Write((ushort)0);
            writer.WriteLE('\t');
            writer.WriteLittleUniNull(args);
            writer.Write((short)0x1);
            writer.Write((ushort)0);
        }
        else
        {
            writer.Clear(10);
        }

        ns.Send(writer.Span);
    }

    public static void SendRemoveBuffPacket(this NetState ns, Serial mob, BuffIcon iconID)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[15]);
        writer.Write((byte)0xDF); // Packet ID
        writer.Write((ushort)15);
        writer.Write(mob);
        writer.Write((short)iconID);
        writer.Write((short)0x0); // command (0 = remove, 1 = add, 2 = data)
        writer.Write(0);

        ns.Send(writer.Span);
    }
}
