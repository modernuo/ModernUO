using System;
using Server.Network;

namespace Server.Items
{
    public abstract class FarmableCrop : Item
    {
        private bool m_Picked;

        public FarmableCrop(int itemID) : base(itemID) => Movable = false;

        public FarmableCrop(Serial serial) : base(serial)
        {
        }

        public abstract Item GetCropObject();
        public abstract int GetPickedID();

        public override void OnDoubleClick(Mobile from)
        {
            var map = Map;
            var loc = Location;

            if (Parent != null || Movable || IsLockedDown || IsSecure || map == null || map == Map.Internal)
            {
                return;
            }

            if (!from.InRange(loc, 2) || !from.InLOS(this))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
            else if (!m_Picked)
            {
                OnPicked(from, loc, map);
            }
        }

        public virtual void OnPicked(Mobile from, Point3D loc, Map map)
        {
            ItemID = GetPickedID();

            var spawn = GetCropObject();

            spawn?.MoveToWorld(loc, map);

            m_Picked = true;

            Unlink();

            Timer.DelayCall(TimeSpan.FromMinutes(5.0), Delete);
        }

        public void Unlink()
        {
            if (Spawner != null)
            {
                Spawner.Remove(this);
                Spawner = null;
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(m_Picked);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            m_Picked = version switch
            {
                0 => reader.ReadBool(),
                _ => m_Picked
            };

            if (m_Picked)
            {
                Unlink();
                Delete();
            }
        }
    }
}
