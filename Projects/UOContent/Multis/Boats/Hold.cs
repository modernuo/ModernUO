using Server.Multis;
using Server.Network;

namespace Server.Items
{
    public class Hold : Container
    {
        private BaseBoat m_Boat;

        public Hold(BaseBoat boat) : base(0x3EAE)
        {
            m_Boat = boat;
            Movable = false;
        }

        public Hold(Serial serial) : base(serial)
        {
        }

        public override bool IsDecoContainer => false;

        public void SetFacing(Direction dir)
        {
            ItemID = dir switch
            {
                Direction.East  => 0x3E65,
                Direction.West  => 0x3E93,
                Direction.North => 0x3EAE,
                Direction.South => 0x3EB9,
                _               => ItemID
            };
        }

        public override bool OnDragDrop(Mobile from, Item item)
        {
            if (m_Boat?.Contains(from) != true || m_Boat.IsMoving)
            {
                return false;
            }

            return base.OnDragDrop(from, item);
        }

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            if (m_Boat?.Contains(from) != true || m_Boat.IsMoving)
            {
                return false;
            }

            return base.OnDragDropInto(from, item, p);
        }

        public override bool CheckItemUse(Mobile from, Item item)
        {
            if (item != this && (m_Boat?.Contains(from) != true || m_Boat.IsMoving))
            {
                return false;
            }

            return base.CheckItemUse(from, item);
        }

        public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
        {
            if (m_Boat?.Contains(from) != true || m_Boat.IsMoving)
            {
                return false;
            }

            return base.CheckLift(from, item, ref reject);
        }

        public override void OnAfterDelete()
        {
            m_Boat?.Delete();
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Boat?.Contains(from) != true)
            {
                m_Boat?.TillerMan?.Say(502490); // You must be on the ship to open the hold.
            }
            else if (m_Boat.IsMoving)
            {
                m_Boat.TillerMan?.Say(502491); // I can not open the hold while the ship is moving.
            }
            else
            {
                base.OnDoubleClick(from);
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);

            writer.Write(m_Boat);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Boat = reader.ReadEntity<BaseBoat>();

                        if (m_Boat == null || Parent != null)
                        {
                            Delete();
                        }

                        Movable = false;

                        break;
                    }
            }
        }
    }
}
