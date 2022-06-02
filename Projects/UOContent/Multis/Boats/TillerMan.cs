using Server.Multis;
using Server.Network;

namespace Server.Items
{
    public class TillerMan : Item
    {
        private BaseBoat m_Boat;

        public TillerMan(BaseBoat boat) : base(0x3E4E)
        {
            m_Boat = boat;
            Movable = false;
        }

        public TillerMan(Serial serial) : base(serial)
        {
        }

        public void SetFacing(Direction dir)
        {
            ItemID = dir switch
            {
                Direction.South => 0x3E4B,
                Direction.North => 0x3E4E,
                Direction.West  => 0x3E50,
                Direction.East  => 0x3E55,
                _               => ItemID
            };
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            list.Add(m_Boat.Status);
        }

        public void Say(int number)
        {
            PublicOverheadMessage(MessageType.Regular, 0x3B2, number);
        }

        public void Say(int number, string args)
        {
            PublicOverheadMessage(MessageType.Regular, 0x3B2, number, args);
        }

        public override void AddNameProperty(IPropertyList list)
        {
            if (m_Boat?.ShipName != null)
            {
                list.Add(1042884, m_Boat.ShipName); // the tiller man of the ~1_SHIP_NAME~
            }
            else
            {
                base.AddNameProperty(list);
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            if (m_Boat?.ShipName != null)
            {
                LabelTo(from, 1042884, m_Boat.ShipName); // the tiller man of the ~1_SHIP_NAME~
            }
            else
            {
                base.OnSingleClick(from);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Boat?.Contains(from) == true)
            {
                m_Boat.BeginRename(from);
            }
            else
            {
                m_Boat?.BeginDryDock(from);
            }
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (dropped is MapItem item && m_Boat?.CanCommand(from) == true && m_Boat.Contains(from))
            {
                m_Boat.AssociateMap(item);
            }

            return false;
        }

        public override void OnAfterDelete()
        {
            m_Boat?.Delete();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

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

                        if (m_Boat == null)
                        {
                            Delete();
                        }

                        break;
                    }
            }
        }
    }
}
