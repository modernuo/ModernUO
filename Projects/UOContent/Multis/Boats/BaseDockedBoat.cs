using Server.Engines.CannedEvil;
using Server.Regions;
using Server.Targeting;

namespace Server.Multis
{
    public abstract class BaseDockedBoat : Item
    {
        private string m_ShipName;

        public BaseDockedBoat(int id, Point3D offset, BaseBoat boat) : base(0x14F4)
        {
            Weight = 1.0;
            LootType = LootType.Blessed;

            MultiID = id;
            Offset = offset;

            m_ShipName = boat.ShipName;
        }

        public BaseDockedBoat(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MultiID { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D Offset { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string ShipName
        {
            get => m_ShipName;
            set
            {
                m_ShipName = value;
                InvalidateProperties();
            }
        }

        public abstract BaseBoat Boat { get; }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(MultiID);
            writer.Write(Offset);
            writer.Write(m_ShipName);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                case 0:
                    {
                        MultiID = reader.ReadInt();
                        Offset = reader.ReadPoint3D();
                        m_ShipName = reader.ReadString();

                        if (version == 0)
                        {
                            reader.ReadUInt();
                        }

                        break;
                    }
            }

            if (LootType == LootType.Newbied)
            {
                LootType = LootType.Blessed;
            }

            if (Weight == 0.0)
            {
                Weight = 1.0;
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                from.SendLocalizedMessage(502482); // Where do you wish to place the ship?

                from.Target = new InternalTarget(this);
            }
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (m_ShipName != null)
            {
                list.Add(m_ShipName);
            }
            else
            {
                base.AddNameProperty(list);
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            if (m_ShipName != null)
            {
                LabelTo(from, m_ShipName);
            }
            else
            {
                base.OnSingleClick(from);
            }
        }

        public void OnPlacement(Mobile from, Point3D p)
        {
            if (Deleted)
            {
                return;
            }

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                var map = from.Map;

                if (map == null)
                {
                    return;
                }

                var boat = Boat;

                if (boat == null)
                {
                    return;
                }

                p = new Point3D(p.X - Offset.X, p.Y - Offset.Y, p.Z - Offset.Z);

                if (BaseBoat.IsValidLocation(p, map) && boat.CanFit(p, map, boat.ItemID) && map != Map.Ilshenar &&
                    map != Map.Malas)
                {
                    Delete();

                    boat.Owner = from;
                    boat.Anchored = true;
                    boat.ShipName = m_ShipName;

                    var keyValue = boat.CreateKeys(from);

                    if (boat.PPlank != null)
                    {
                        boat.PPlank.KeyValue = keyValue;
                    }

                    if (boat.SPlank != null)
                    {
                        boat.SPlank.KeyValue = keyValue;
                    }

                    boat.MoveToWorld(p, map);
                }
                else
                {
                    boat.Delete();
                    from.SendLocalizedMessage(1043284); // A ship can not be created here.
                }
            }
        }

        private class InternalTarget : MultiTarget
        {
            private readonly BaseDockedBoat m_Model;

            public InternalTarget(BaseDockedBoat model) : base(model.MultiID, model.Offset) => m_Model = model;

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is not IPoint3D ip)
                {
                    return;
                }

                Point3D p = ip switch
                {
                    Item item => item.GetWorldTop(),
                    Mobile m  => m.Location,
                    _         => new Point3D(ip)
                };

                var region = Region.Find(p, from.Map);

                if (region.IsPartOf<DungeonRegion>())
                {
                    from.SendLocalizedMessage(502488); // You can not place a ship inside a dungeon.
                }
                else if (region.IsPartOf<HouseRegion>() || region.IsPartOf<ChampionSpawnRegion>())
                {
                    from.SendLocalizedMessage(1042549); // A boat may not be placed in this area.
                }
                else
                {
                    m_Model.OnPlacement(from, p);
                }
            }
        }
    }
}
