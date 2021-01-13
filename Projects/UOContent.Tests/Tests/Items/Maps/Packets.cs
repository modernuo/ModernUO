using Server.Network;

namespace Server.Items
{
    public sealed class MapDetails : Packet
    {
        public MapDetails(MapItem map) : base(0x90, 19)
        {
            Stream.Write(map.Serial);
            Stream.Write((short)0x139D);
            Stream.Write((short)map.Bounds.Start.X);
            Stream.Write((short)map.Bounds.Start.Y);
            Stream.Write((short)map.Bounds.End.X);
            Stream.Write((short)map.Bounds.End.Y);
            Stream.Write((short)map.Width);
            Stream.Write((short)map.Height);
        }
    }

    public sealed class MapDetailsNew : Packet
    {
        public MapDetailsNew(MapItem map) : base(0xF5, 21)
        {
            Stream.Write(map.Serial);
            Stream.Write((short)0x139D);
            Stream.Write((short)map.Bounds.Start.X);
            Stream.Write((short)map.Bounds.Start.Y);
            Stream.Write((short)map.Bounds.End.X);
            Stream.Write((short)map.Bounds.End.Y);
            Stream.Write((short)map.Width);
            Stream.Write((short)map.Height);
            Stream.Write((short)(map.Facet?.MapID ?? 0));
        }
    }

    public class MapCommand : Packet
    {
        public MapCommand(MapItem map, int command, int number, int x, int y) : base(0x56, 11)
        {
            Stream.Write(map.Serial);
            Stream.Write((byte)command);
            Stream.Write((byte)number);
            Stream.Write((short)x);
            Stream.Write((short)y);
        }
    }

    public sealed class MapDisplay : MapCommand
    {
        public MapDisplay(MapItem map) : base(map, 5, 0, 0, 0)
        {
        }
    }

    public sealed class MapAddPin : MapCommand
    {
        public MapAddPin(MapItem map, Point2D point) : base(map, 1, 0, point.X, point.Y)
        {
        }
    }

    public sealed class MapSetEditable : MapCommand
    {
        public MapSetEditable(MapItem map, bool editable) : base(map, 7, editable ? 1 : 0, 0, 0)
        {
        }
    }
}
