using System;
using Server.Mobiles;
using Server.Multis;
using Server.Targeting;

namespace Server.Items
{
    [Flippable(0x1EBA, 0x1EBB)]
    public class TaxidermyKit : Item
    {
        private static readonly TrophyInfo[] m_Table =
        {
            new(typeof(BrownBear), 0x1E60, 1041093, 1041107),
            new(typeof(GreatHart), 0x1E61, 1041095, 1041109),
            new(typeof(BigFish), 0x1E62, 1041096, 1041110),
            new(typeof(Gorilla), 0x1E63, 1041091, 1041105),
            new(typeof(Orc), 0x1E64, 1041090, 1041104),
            new(typeof(PolarBear), 0x1E65, 1041094, 1041108),
            new(typeof(Troll), 0x1E66, 1041092, 1041106)
        };

        [Constructible]
        public TaxidermyKit() : base(0x1EBA) => Weight = 1.0;

        public TaxidermyKit(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041279; // a taxidermy kit

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

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else if (from.Skills.Carpentry.Base < 90.0)
            {
                from.SendLocalizedMessage(1042594); // You do not understand how to use this.
            }
            else
            {
                from.SendLocalizedMessage(1042595); // Target the corpse to make a trophy out of.
                from.Target = new CorpseTarget(this);
            }
        }

        public class TrophyInfo
        {
            public TrophyInfo(Type type, int id, int deedNum, int addonNum)
            {
                CreatureType = type;
                NorthID = id;
                DeedNumber = deedNum;
                AddonNumber = addonNum;
            }

            public Type CreatureType { get; }

            public int NorthID { get; }

            public int DeedNumber { get; }

            public int AddonNumber { get; }
        }

        private class CorpseTarget : Target
        {
            private readonly TaxidermyKit m_Kit;

            public CorpseTarget(TaxidermyKit kit) : base(3, false, TargetFlags.None) => m_Kit = kit;

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Kit.Deleted)
                {
                    return;
                }

                var corpse = targeted as Corpse;

                if (!(corpse != null || targeted is BigFish))
                {
                    from.SendLocalizedMessage(1042600); // That is not a corpse!
                }
                else if (corpse?.VisitedByTaxidermist == true)
                {
                    from.SendLocalizedMessage(1042596); // That corpse seems to have been visited by a taxidermist already.
                }
                else if (!m_Kit.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                }
                else if (from.Skills.Carpentry.Base < 90.0)
                {
                    from.SendLocalizedMessage(1042603); // You would not understand how to use the kit.
                }
                else
                {
                    var obj = corpse?.Owner ?? targeted;

                    foreach (var t in m_Table)
                    {
                        if (t.CreatureType != obj.GetType())
                        {
                            continue;
                        }

                        var pack = from.Backpack;

                        if (pack?.ConsumeTotal(typeof(Board), 10) == true)
                        {
                            from.SendLocalizedMessage(
                                1042278
                            );                                  // You review the corpse and find it worthy of a trophy.
                            from.SendLocalizedMessage(1042602); // You use your kit up making the trophy.

                            Mobile hunter = null;
                            var weight = 0;

                            if (targeted is BigFish fish)
                            {
                                hunter = fish.Fisher;
                                weight = (int)fish.Weight;

                                fish.Consume();
                            }

                            from.AddToBackpack(new TrophyDeed(t, hunter, weight));

                            if (corpse != null)
                            {
                                corpse.VisitedByTaxidermist = true;
                            }

                            m_Kit.Delete();
                            return;
                        }

                        from.SendLocalizedMessage(1042598); // You do not have enough boards.
                        return;
                    }

                    from.SendLocalizedMessage(1042599); // That does not look like something you want hanging on a wall.
                }
            }
        }
    }

    public class TrophyAddon : Item, IAddon
    {
        private int m_AddonNumber;
        private int m_AnimalWeight;

        private Mobile m_Hunter;

        public TrophyAddon(
            Mobile from, int itemID, int westID, int northID, int deedNumber,
            int addonNumber, Mobile hunter = null, int animalWeight = 0
        ) : base(itemID)
        {
            WestID = westID;
            NorthID = northID;
            DeedNumber = deedNumber;
            m_AddonNumber = addonNumber;

            m_Hunter = hunter;
            m_AnimalWeight = animalWeight;

            Movable = false;

            MoveToWorld(from.Location, from.Map);
        }

        public TrophyAddon(Serial serial) : base(serial)
        {
        }

        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        [CommandProperty(AccessLevel.GameMaster)]
        public int WestID { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int NorthID { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DeedNumber { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int AddonNumber
        {
            get => m_AddonNumber;
            set
            {
                m_AddonNumber = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Hunter
        {
            get => m_Hunter;
            set
            {
                m_Hunter = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int AnimalWeight
        {
            get => m_AnimalWeight;
            set
            {
                m_AnimalWeight = value;
                InvalidateProperties();
            }
        }

        public override int LabelNumber => m_AddonNumber;

        public bool CouldFit(IPoint3D p, Map map)
        {
            if (!map.CanFit(p.X, p.Y, p.Z, ItemData.Height))
            {
                return false;
            }

            if (ItemID == NorthID)
            {
                return BaseAddon.IsWall(p.X, p.Y - 1, p.Z, map); // North wall
            }

            return BaseAddon.IsWall(p.X - 1, p.Y, p.Z, map);     // West wall
        }

        public Item Deed => new TrophyDeed(WestID, NorthID, DeedNumber, m_AddonNumber, m_Hunter, m_AnimalWeight);

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (m_AnimalWeight >= 20)
            {
                if (m_Hunter != null)
                {
                    list.Add(1070857, m_Hunter.Name); // Caught by ~1_fisherman~
                }

                list.Add(1070858, m_AnimalWeight); // ~1_weight~ stones
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(m_Hunter);
            writer.Write(m_AnimalWeight);

            writer.Write(WestID);
            writer.Write(NorthID);
            writer.Write(DeedNumber);
            writer.Write(m_AddonNumber);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Hunter = reader.ReadEntity<Mobile>();
                        m_AnimalWeight = reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        WestID = reader.ReadInt();
                        NorthID = reader.ReadInt();
                        DeedNumber = reader.ReadInt();
                        m_AddonNumber = reader.ReadInt();
                        break;
                    }
            }

            Timer.StartTimer(FixMovingCrate);
        }

        private void FixMovingCrate()
        {
            if (Deleted)
            {
                return;
            }

            if (Movable || IsLockedDown)
            {
                var deed = Deed;

                if (Parent is Item item)
                {
                    item.AddItem(deed);
                    deed.Location = Location;
                }
                else
                {
                    deed.MoveToWorld(Location, Map);
                }

                Delete();
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            var house = BaseHouse.FindHouseAt(this);

            if (house?.IsCoOwner(from) == true)
            {
                if (from.InRange(GetWorldLocation(), 1))
                {
                    from.AddToBackpack(Deed);
                    Delete();
                }
                else
                {
                    from.SendLocalizedMessage(500295); // You are too far away to do that.
                }
            }
        }
    }

    [Flippable(0x14F0, 0x14EF)]
    public class TrophyDeed : Item
    {
        private int m_AnimalWeight;
        private int m_DeedNumber;

        private Mobile m_Hunter;

        public TrophyDeed(
            int westID, int northID, int deedNumber, int addonNumber,
            Mobile hunter = null, int animalWeight = 0
        ) : base(0x14F0)
        {
            WestID = westID;
            NorthID = northID;
            m_DeedNumber = deedNumber;
            AddonNumber = addonNumber;
            m_Hunter = hunter;
            m_AnimalWeight = animalWeight;
        }

        public TrophyDeed(TaxidermyKit.TrophyInfo info, Mobile hunter, int animalWeight)
            : this(info.NorthID + 7, info.NorthID, info.DeedNumber, info.AddonNumber, hunter, animalWeight)
        {
        }

        public TrophyDeed(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int WestID { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int NorthID { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DeedNumber
        {
            get => m_DeedNumber;
            set
            {
                m_DeedNumber = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int AddonNumber { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Hunter
        {
            get => m_Hunter;
            set
            {
                m_Hunter = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int AnimalWeight
        {
            get => m_AnimalWeight;
            set
            {
                m_AnimalWeight = value;
                InvalidateProperties();
            }
        }

        public override int LabelNumber => m_DeedNumber;

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (m_AnimalWeight >= 20)
            {
                if (m_Hunter != null)
                {
                    list.Add(1070857, m_Hunter.Name); // Caught by ~1_fisherman~
                }

                list.Add(1070858, m_AnimalWeight); // ~1_weight~ stones
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(m_Hunter);
            writer.Write(m_AnimalWeight);

            writer.Write(WestID);
            writer.Write(NorthID);
            writer.Write(m_DeedNumber);
            writer.Write(AddonNumber);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Hunter = reader.ReadEntity<Mobile>();
                        m_AnimalWeight = reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        WestID = reader.ReadInt();
                        NorthID = reader.ReadInt();
                        m_DeedNumber = reader.ReadInt();
                        AddonNumber = reader.ReadInt();
                        break;
                    }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                var house = BaseHouse.FindHouseAt(from);

                if (house?.IsCoOwner(from) == true)
                {
                    var northWall = BaseAddon.IsWall(from.X, from.Y - 1, from.Z, from.Map);
                    var westWall = BaseAddon.IsWall(from.X - 1, from.Y, from.Z, from.Map);

                    if (northWall && westWall)
                    {
                        switch (from.Direction & Direction.Mask)
                        {
                            case Direction.North:
                            case Direction.South:
                                westWall = false;
                                break;

                            case Direction.East:
                            case Direction.West:
                                northWall = false;
                                break;

                            default:
                                from.SendMessage("Turn to face the wall on which to hang this trophy.");
                                return;
                        }
                    }

                    var itemID = 0;

                    if (northWall)
                    {
                        itemID = NorthID;
                    }
                    else if (westWall)
                    {
                        itemID = WestID;
                    }
                    else
                    {
                        from.SendLocalizedMessage(1042626); // The trophy must be placed next to a wall.
                    }

                    if (itemID > 0)
                    {
                        house.Addons.Add(
                            new TrophyAddon(
                                from,
                                itemID,
                                WestID,
                                NorthID,
                                m_DeedNumber,
                                AddonNumber,
                                m_Hunter,
                                m_AnimalWeight
                            )
                        );
                        Delete();
                    }
                }
                else
                {
                    from.SendLocalizedMessage(502092); // You must be in your house to do this.
                }
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }
    }
}
