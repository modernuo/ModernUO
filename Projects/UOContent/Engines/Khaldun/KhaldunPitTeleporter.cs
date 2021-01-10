using Server.Mobiles;

namespace Server.Items
{
    public class KhaldunPitTeleporter : Item
    {
        [Constructible]
        public KhaldunPitTeleporter() : this(new Point3D(5451, 1374, 0), Map.Felucca)
        {
        }

        [Constructible]
        public KhaldunPitTeleporter(Point3D pointDest, Map mapDest) : base(0x053B)
        {
            Movable = false;
            Hue = 1;

            Active = true;
            PointDest = pointDest;
            MapDest = mapDest;
        }

        public KhaldunPitTeleporter(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D PointDest { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map MapDest { get; set; }

        // the floor of the cavern seems to have collapsed here - a faint light is visible at the bottom of the pit
        public override int LabelNumber => 1016511;

        public override void OnDoubleClick(Mobile m)
        {
            if (!Active)
            {
                return;
            }

            var map = MapDest;

            if (map != null && map != Map.Internal && m.InRange(this, 3))
            {
                BaseCreature.TeleportPets(m, PointDest, MapDest);
                m.MoveToWorld(PointDest, MapDest);
            }
            else
            {
                m.SendLocalizedMessage(1019045); // I can't reach that.
            }
        }

        public override void OnDoubleClickDead(Mobile m)
        {
            OnDoubleClick(m);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(Active);
            writer.Write(PointDest);
            writer.Write(MapDest);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            Active = reader.ReadBool();
            PointDest = reader.ReadPoint3D();
            MapDest = reader.ReadMap();
        }
    }
}
