using System.Buffers;
using System.Collections.Generic;
using System.IO;
using Server.Collections;
using Server.Network;

namespace Server.ContextMenus;

public static class ContextMenuSystem
{
    private static readonly Dictionary<Mobile, ContextMenu> _menus = [];

    public static unsafe void Configure()
    {
        IncomingExtendedCommandPackets.RegisterExtended(0x13, true, &ContextMenuRequest);
        IncomingExtendedCommandPackets.RegisterExtended(0x15, true, &ContextMenuResponse);
    }

    public static ContextMenu CreateContextMenu(Mobile from, IEntity target)
    {
        if (target?.Deleted != false)
        {
            return new ContextMenu(from, target, []);
        }

        var list = PooledRefList<ContextMenuEntry>.Create();

        if (target is Mobile mobile)
        {
            if (mobile.CanPaperdollBeOpenedBy(from))
            {
                list.Add(new PaperdollEntry());
            }

            mobile.GetContextMenuEntries(from, ref list);
        }
        else if (target is Item item)
        {
            item.GetContextMenuEntries(from, ref list);
        }

        var entries = list.ToArray();
        list.Dispose();

        return new ContextMenu(from, target, entries);
    }

    public static void ContextMenuResponse(NetState state, SpanReader reader)
    {
        var from = state.Mobile;

        if (from == null || !_menus.Remove(from, out var menu) || from != menu.From)
        {
            return;
        }

        var entity = World.FindEntity((Serial)reader.ReadUInt32());

        if (entity == null || entity != menu.Target || !from.CanSee(entity))
        {
            return;
        }

        Point3D p;

        if (entity is Mobile)
        {
            p = entity.Location;
        }
        else if (entity is Item item)
        {
            p = item.GetWorldLocation();
        }
        else
        {
            return;
        }

        int index = reader.ReadUInt16();

        if (index >= menu.Entries.Length)
        {
            return;
        }

        var e = menu.Entries[index];

        var range = e.Range;

        if (range == -1)
        {
            range = 18;
        }

        if (e.Enabled && from.InRange(p, range))
        {
            e.OnClick(from, entity);
        }
    }

    public static void ContextMenuRequest(NetState state, SpanReader reader)
    {
        var from = state.Mobile;
        var target = World.FindEntity((Serial)reader.ReadUInt32());

        if (from == null || target == null || from.Map != target.Map || !from.CanSee(target))
        {
            return;
        }

        var item = target as Item;

        var checkLocation = item?.GetWorldLocation() ?? target.Location;
        if (!(Utility.InUpdateRange(from.Location, checkLocation) && from.CheckContextMenuDisplay(target)))
        {
            return;
        }

        var c = CreateContextMenu(from, target);

        if (c.Entries.Length <= 0)
        {
            return;
        }

        if (item?.RootParent is Mobile mobile && mobile != from && mobile.AccessLevel >= from.AccessLevel)
        {
            for (var i = 0; i < c.Entries.Length; ++i)
            {
                var entry = c.Entries[i];
                if (!entry.NonLocalUse)
                {
                    entry.Enabled = false;
                }
            }
        }

        _menus[from] = c;
        state.SendDisplayContextMenu(c);
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
