using Server.Engines.CannedEvil;
using Server.Network;
using Server.Regions;
using Server.Targeting;

namespace Server.Multis
{
    public abstract class BaseBoatDeed : Item
    {
        public BaseBoatDeed(int id, Point3D offset) : base(0x14F2)
        {
            Weight = 1.0;

            if (!Core.AOS)
            {
                LootType = LootType.Newbied;
            }

            MultiID = id;
            Offset = offset;
        }

        public BaseBoatDeed(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MultiID { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D Offset { get; set; }

        public abstract BaseBoat Boat { get; }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(MultiID);
            writer.Write(Offset);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        MultiID = reader.ReadInt();
                        Offset = reader.ReadPoint3D();

                        break;
                    }
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
            else if (from.AccessLevel < AccessLevel.GameMaster && (from.Map == Map.Ilshenar || from.Map == Map.Malas))
            {
                from.SendLocalizedMessage(1010567, null, 0x25); // You may not place a boat from this location.
            }
            else
            {
                if (Core.SE)
                {
                    from.SendLocalizedMessage(502482); // Where do you wish to place the ship?
                }
                else
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 502482); // Where do you wish to place the ship?
                }

                from.Target = new InternalTarget(this);
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

                if (from.AccessLevel < AccessLevel.GameMaster && (map == Map.Ilshenar || map == Map.Malas))
                {
                    from.SendLocalizedMessage(1043284); // A ship can not be created here.
                    return;
                }

                if (from.Region.IsPartOf<HouseRegion>() || BaseBoat.FindBoatAt(from.Location, from.Map) != null)
                {
                    from.SendLocalizedMessage(
                        1010568,
                        null,
                        0x25
                    ); // You may not place a ship while on another ship or inside a house.
                    return;
                }

                var boat = Boat;

                if (boat == null)
                {
                    return;
                }

                p = new Point3D(p.X - Offset.X, p.Y - Offset.Y, p.Z - Offset.Z);

                if (BaseBoat.IsValidLocation(p, map) && boat.CanFit(p, map, boat.ItemID))
                {
                    Delete();

                    boat.Owner = from;
                    boat.Anchored = true;

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
            private readonly BaseBoatDeed m_Deed;

            public InternalTarget(BaseBoatDeed deed) : base(deed.MultiID, deed.Offset) => m_Deed = deed;

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is IPoint3D ip)
                {
                    if (ip is Item item)
                    {
                        ip = from;
                    }

                    var p = new Point3D(ip);

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
                        m_Deed.OnPlacement(from, p);
                    }
                }
            }
        }
    }
}
