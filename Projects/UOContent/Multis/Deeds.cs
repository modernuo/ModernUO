using Server.Regions;
using Server.Targeting;

namespace Server.Multis.Deeds
{
    public class HousePlacementTarget : MultiTarget
    {
        private readonly HouseDeed m_Deed;

        public HousePlacementTarget(HouseDeed deed) : base(deed.MultiID, deed.Offset) => m_Deed = deed;

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

            var reg = Region.Find(p, from.Map);

            if (from.AccessLevel >= AccessLevel.GameMaster || reg.AllowHousing(from, p))
            {
                m_Deed.OnPlacement(from, p);
            }
            else if (reg.IsPartOf<TempNoHousingRegion>())
            {
                // Lord British has decreed a 'no build' period, thus you cannot build this house at this time.
                from.SendLocalizedMessage(501270);
            }
            else if (reg.IsPartOf<TreasureRegion>() || reg.IsPartOf<HouseRegion>())
            {
                // The house could not be created here.  Either something is blocking the house, or the house would not be on valid terrain.
                from.SendLocalizedMessage(1043287);
            }
            else if (reg.IsPartOf<HouseRaffleRegion>())
            {
                from.SendLocalizedMessage(1150493); // You must have a deed for this plot of land in order to build here.
            }
            else
            {
                from.SendLocalizedMessage(501265); // Housing can not be created in this area.
            }
        }
    }

    public abstract class HouseDeed : Item
    {
        public HouseDeed(int id, Point3D offset) : base(0x14F0)
        {
            Weight = 1.0;
            LootType = LootType.Newbied;

            MultiID = id;
            Offset = offset;
        }

        public HouseDeed(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MultiID { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D Offset { get; set; }

        public abstract Rectangle2D[] Area { get; }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(Offset);

            writer.Write(MultiID);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        Offset = reader.ReadPoint3D();

                        goto case 0;
                    }
                case 0:
                    {
                        MultiID = reader.ReadInt();

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
            else if (from.AccessLevel < AccessLevel.GameMaster && BaseHouse.HasAccountHouse(from))
            {
                from.SendLocalizedMessage(501271); // You already own a house, you may not place another!
            }
            else
            {
                /* House placement cancellation could result in a
                 * 60 second delay in the return of your deed.
                 */
                from.SendLocalizedMessage(1010433);

                from.Target = new HousePlacementTarget(this);
            }
        }

        public abstract BaseHouse GetHouse(Mobile owner);

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
            else if (from.AccessLevel < AccessLevel.GameMaster && BaseHouse.HasAccountHouse(from))
            {
                from.SendLocalizedMessage(501271); // You already own a house, you may not place another!
            }
            else
            {
                var center = new Point3D(p.X - Offset.X, p.Y - Offset.Y, p.Z - Offset.Z);
                var res = HousePlacement.Check(from, MultiID, center, out var toMove);

                switch (res)
                {
                    case HousePlacementResult.Valid:
                        {
                            var house = GetHouse(from);
                            house.MoveToWorld(center, from.Map);
                            Delete();

                            for (var i = 0; i < toMove.Count; ++i)
                            {
                                object o = toMove[i];

                                if (o is Mobile mobile)
                                {
                                    mobile.Location = house.BanLocation;
                                }
                                else if (o is Item item)
                                {
                                    item.Location = house.BanLocation;
                                }
                            }

                            break;
                        }
                    case HousePlacementResult.BadItem:
                    case HousePlacementResult.BadLand:
                    case HousePlacementResult.BadStatic:
                    case HousePlacementResult.BadRegionHidden:
                        {
                            // The house could not be created here.  Either something is blocking the house, or the house would not be on valid terrain.
                            from.SendLocalizedMessage(1043287);
                            break;
                        }
                    case HousePlacementResult.NoSurface:
                        {
                            from.SendMessage(
                                "The house could not be created here.  Part of the foundation would not be on any surface."
                            );
                            break;
                        }
                    case HousePlacementResult.BadRegion:
                        {
                            from.SendLocalizedMessage(501265); // Housing cannot be created in this area.
                            break;
                        }
                    case HousePlacementResult.BadRegionTemp:
                        {
                            // Lord British has decreed a 'no build' period, thus you cannot build this house at this time.
                            from.SendLocalizedMessage(501270);
                            break;
                        }
                    case HousePlacementResult.BadRegionRaffle:
                        {
                            // You must have a deed for this plot of land in order to build here.
                            from.SendLocalizedMessage(1150493);
                            break;
                        }
                }
            }
        }
    }

    public class StonePlasterHouseDeed : HouseDeed
    {
        [Constructible]
        public StonePlasterHouseDeed() : base(0x64, new Point3D(0, 4, 0))
        {
        }

        public StonePlasterHouseDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041211;
        public override Rectangle2D[] Area => SmallOldHouse.AreaArray;

        public override BaseHouse GetHouse(Mobile owner) => new SmallOldHouse(owner, 0x64);

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

    public class FieldStoneHouseDeed : HouseDeed
    {
        [Constructible]
        public FieldStoneHouseDeed() : base(0x66, new Point3D(0, 4, 0))
        {
        }

        public FieldStoneHouseDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041212;
        public override Rectangle2D[] Area => SmallOldHouse.AreaArray;

        public override BaseHouse GetHouse(Mobile owner) => new SmallOldHouse(owner, 0x66);

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

    public class SmallBrickHouseDeed : HouseDeed
    {
        [Constructible]
        public SmallBrickHouseDeed() : base(0x68, new Point3D(0, 4, 0))
        {
        }

        public SmallBrickHouseDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041213;
        public override Rectangle2D[] Area => SmallOldHouse.AreaArray;

        public override BaseHouse GetHouse(Mobile owner) => new SmallOldHouse(owner, 0x68);

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

    public class WoodHouseDeed : HouseDeed
    {
        [Constructible]
        public WoodHouseDeed() : base(0x6A, new Point3D(0, 4, 0))
        {
        }

        public WoodHouseDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041214;
        public override Rectangle2D[] Area => SmallOldHouse.AreaArray;

        public override BaseHouse GetHouse(Mobile owner) => new SmallOldHouse(owner, 0x6A);

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

    public class WoodPlasterHouseDeed : HouseDeed
    {
        [Constructible]
        public WoodPlasterHouseDeed() : base(0x6C, new Point3D(0, 4, 0))
        {
        }

        public WoodPlasterHouseDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041215;
        public override Rectangle2D[] Area => SmallOldHouse.AreaArray;

        public override BaseHouse GetHouse(Mobile owner) => new SmallOldHouse(owner, 0x6C);

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

    public class ThatchedRoofCottageDeed : HouseDeed
    {
        [Constructible]
        public ThatchedRoofCottageDeed() : base(0x6E, new Point3D(0, 4, 0))
        {
        }

        public ThatchedRoofCottageDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041216;
        public override Rectangle2D[] Area => SmallOldHouse.AreaArray;

        public override BaseHouse GetHouse(Mobile owner) => new SmallOldHouse(owner, 0x6E);

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

    public class BrickHouseDeed : HouseDeed
    {
        [Constructible]
        public BrickHouseDeed() : base(0x74, new Point3D(-1, 7, 0))
        {
        }

        public BrickHouseDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041219;
        public override Rectangle2D[] Area => GuildHouse.AreaArray;

        public override BaseHouse GetHouse(Mobile owner) => new GuildHouse(owner);

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

    public class TwoStoryWoodPlasterHouseDeed : HouseDeed
    {
        [Constructible]
        public TwoStoryWoodPlasterHouseDeed() : base(0x76, new Point3D(-3, 7, 0))
        {
        }

        public TwoStoryWoodPlasterHouseDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041220;
        public override Rectangle2D[] Area => TwoStoryHouse.AreaArray;

        public override BaseHouse GetHouse(Mobile owner) => new TwoStoryHouse(owner, 0x76);

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

    public class TwoStoryStonePlasterHouseDeed : HouseDeed
    {
        [Constructible]
        public TwoStoryStonePlasterHouseDeed() : base(0x78, new Point3D(-3, 7, 0))
        {
        }

        public TwoStoryStonePlasterHouseDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041221;
        public override Rectangle2D[] Area => TwoStoryHouse.AreaArray;

        public override BaseHouse GetHouse(Mobile owner) => new TwoStoryHouse(owner, 0x78);

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

    public class TowerDeed : HouseDeed
    {
        [Constructible]
        public TowerDeed() : base(0x7A, new Point3D(0, 7, 0))
        {
        }

        public TowerDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041222;
        public override Rectangle2D[] Area => Tower.AreaArray;

        public override BaseHouse GetHouse(Mobile owner) => new Tower(owner);

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

    public class KeepDeed : HouseDeed
    {
        [Constructible]
        public KeepDeed() : base(0x7C, new Point3D(0, 11, 0))
        {
        }

        public KeepDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041223;
        public override Rectangle2D[] Area => Keep.AreaArray;

        public override BaseHouse GetHouse(Mobile owner) => new Keep(owner);

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

    public class CastleDeed : HouseDeed
    {
        [Constructible]
        public CastleDeed() : base(0x7E, new Point3D(0, 16, 0))
        {
        }

        public CastleDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041224;
        public override Rectangle2D[] Area => Castle.AreaArray;

        public override BaseHouse GetHouse(Mobile owner) => new Castle(owner);

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

    public class LargePatioDeed : HouseDeed
    {
        [Constructible]
        public LargePatioDeed() : base(0x8C, new Point3D(-4, 7, 0))
        {
        }

        public LargePatioDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041231;
        public override Rectangle2D[] Area => LargePatioHouse.AreaArray;

        public override BaseHouse GetHouse(Mobile owner) => new LargePatioHouse(owner);

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

    public class LargeMarbleDeed : HouseDeed
    {
        [Constructible]
        public LargeMarbleDeed() : base(0x96, new Point3D(-4, 7, 0))
        {
        }

        public LargeMarbleDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041236;
        public override Rectangle2D[] Area => LargeMarbleHouse.AreaArray;

        public override BaseHouse GetHouse(Mobile owner) => new LargeMarbleHouse(owner);

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

    public class SmallTowerDeed : HouseDeed
    {
        [Constructible]
        public SmallTowerDeed() : base(0x98, new Point3D(3, 4, 0))
        {
        }

        public SmallTowerDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041237;
        public override Rectangle2D[] Area => SmallTower.AreaArray;

        public override BaseHouse GetHouse(Mobile owner) => new SmallTower(owner);

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

    public class LogCabinDeed : HouseDeed
    {
        [Constructible]
        public LogCabinDeed() : base(0x9A, new Point3D(1, 6, 0))
        {
        }

        public LogCabinDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041238;
        public override Rectangle2D[] Area => LogCabin.AreaArray;

        public override BaseHouse GetHouse(Mobile owner) => new LogCabin(owner);

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

    public class SandstonePatioDeed : HouseDeed
    {
        [Constructible]
        public SandstonePatioDeed() : base(0x9C, new Point3D(-1, 4, 0))
        {
        }

        public SandstonePatioDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041239;
        public override Rectangle2D[] Area => SandStonePatio.AreaArray;

        public override BaseHouse GetHouse(Mobile owner) => new SandStonePatio(owner);

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

    public class VillaDeed : HouseDeed
    {
        [Constructible]
        public VillaDeed() : base(0x9E, new Point3D(3, 6, 0))
        {
        }

        public VillaDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041240;
        public override Rectangle2D[] Area => TwoStoryVilla.AreaArray;

        public override BaseHouse GetHouse(Mobile owner) => new TwoStoryVilla(owner);

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

    public class StoneWorkshopDeed : HouseDeed
    {
        [Constructible]
        public StoneWorkshopDeed() : base(0xA0, new Point3D(-1, 4, 0))
        {
        }

        public StoneWorkshopDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041241;
        public override Rectangle2D[] Area => SmallShop.AreaArray2;

        public override BaseHouse GetHouse(Mobile owner) => new SmallShop(owner, 0xA0);

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

    public class MarbleWorkshopDeed : HouseDeed
    {
        [Constructible]
        public MarbleWorkshopDeed() : base(0xA2, new Point3D(-1, 4, 0))
        {
        }

        public MarbleWorkshopDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041242;
        public override Rectangle2D[] Area => SmallShop.AreaArray1;

        public override BaseHouse GetHouse(Mobile owner) => new SmallShop(owner, 0xA2);

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
