using Server.Buffers;
using Server.Network;

namespace Server.Items
{
  public class MapItemPackets
  {
    public static void SendMapDetails(NetState ns, MapItem map)
    {
      // TODO: When was this actually? Guessing >5.0.0
      if (ns.BuffIcon)
        SendMapDetailsNew(ns, map);
      else
        SendMapDetailsOld(ns, map);
    }

    public static void SendMapDetailsOld(NetState ns, MapItem map)
    {
      if (ns == null)
        return;

      SpanWriter writer = new SpanWriter(stackalloc byte[19]);
      writer.Write((byte)0x90); // Packet ID

      writer.Write(map.Serial);
      writer.Write((short)0x139D);
      writer.Write((short)map.Bounds.Start.X);
      writer.Write((short)map.Bounds.Start.Y);
      writer.Write((short)map.Bounds.End.X);
      writer.Write((short)map.Bounds.End.Y);
      writer.Write((short)map.Width);
      writer.Write((short)map.Height);

      ns.Send(writer.Span);
    }

    public static void SendMapDetailsNew(NetState ns, MapItem map)
    {
      if (ns == null)
        return;

      SpanWriter writer = new SpanWriter(stackalloc byte[21]);
      writer.Write((byte)0xF5); // Packet ID

      writer.Write(map.Serial);
      writer.Write((short)0x139D);
      writer.Write((short)map.Bounds.Start.X);
      writer.Write((short)map.Bounds.Start.Y);
      writer.Write((short)map.Bounds.End.X);
      writer.Write((short)map.Bounds.End.Y);
      writer.Write((short)map.Width);
      writer.Write((short)map.Height);
      writer.Write( (short)(map.Facet?.MapID ?? 0) );

      ns.Send(writer.Span);
    }

    public static void SendMapDisplay(NetState ns, MapItem map)
    {
      SendMapCommand(ns, map, 5);
    }

    public static void SendMapAddPin(NetState ns, MapItem map, Point2D p)
    {
      SendMapCommand(ns, map, 1, 0, p.X, p.Y);
    }

    public static void SendMapSetEditable(NetState ns, MapItem map, bool editable)
    {
      SendMapCommand(ns, map, 7, editable ? 1 : 0);
    }

    public static void SendMapCommand(NetState ns, MapItem map, int command, int number = 0, int x = 0, int y = 0)
    {
      if (ns == null)
        return;

      SpanWriter writer = new SpanWriter(stackalloc byte[11]);
      writer.Write((byte)0x56); // Packet ID

      writer.Write(map.Serial);
      writer.Write((byte)command);
      writer.Write((byte)number);
      writer.Write((short)x);
      writer.Write((short)y);

      ns.Send(writer.Span);
    }
  }
}
