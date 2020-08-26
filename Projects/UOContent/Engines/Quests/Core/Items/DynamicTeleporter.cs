using Server.Mobiles;

namespace Server.Engines.Quests
{
    public abstract class DynamicTeleporter : Item
    {
        public DynamicTeleporter(int itemID = 0x1822, int hue = 0x482) : base(itemID)
        {
            Movable = false;
            Hue = hue;
        }

        public DynamicTeleporter(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1049382; // a magical teleporter

        public virtual int NotWorkingMessage // Nothing Happens.
            => 500309;

        public abstract bool GetDestination(PlayerMobile player, ref Point3D loc, ref Map map);

        public override bool OnMoveOver(Mobile m)
        {
            if (m is PlayerMobile pm)
            {
                var loc = Point3D.Zero;
                Map map = null;

                if (GetDestination(pm, ref loc, ref map))
                {
                    BaseCreature.TeleportPets(pm, loc, map);

                    pm.PlaySound(0x1FE);
                    pm.MoveToWorld(loc, map);

                    return false;
                }

                pm.SendLocalizedMessage(NotWorkingMessage);
            }

            return base.OnMoveOver(m);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
