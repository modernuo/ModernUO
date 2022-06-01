using System;

namespace Server.Items
{
    public class MiniHouseAddon : BaseAddon
    {
        private MiniHouseType m_Type;

        [Constructible]
        public MiniHouseAddon(MiniHouseType type = MiniHouseType.StoneAndPlaster)
        {
            m_Type = type;

            Construct();
        }

        public MiniHouseAddon(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public MiniHouseType Type
        {
            get => m_Type;
            set
            {
                m_Type = value;
                Construct();
            }
        }

        public override BaseAddonDeed Deed => new MiniHouseDeed(m_Type);

        public void Construct()
        {
            foreach (var c in Components)
            {
                c.Addon = null;
                c.Delete();
            }

            this.Clear(Components);

            var info = MiniHouseInfo.GetInfo(m_Type);

            var size = (int)Math.Sqrt(info.Graphics.Length);
            var num = 0;

            for (var y = 0; y < size; ++y)
            {
                for (var x = 0; x < size; ++x)
                {
                    if (info.Graphics[num] != 0x1) // Veteran Rewards Mod
                    {
                        AddComponent(new AddonComponent(info.Graphics[num++]), size - x - 1, size - y - 1, 0);
                    }
                }
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write((int)m_Type);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Type = (MiniHouseType)reader.ReadInt();
                        break;
                    }
            }
        }
    }

    public class MiniHouseDeed : BaseAddonDeed
    {
        private MiniHouseType m_Type;

        [Constructible]
        public MiniHouseDeed(MiniHouseType type = MiniHouseType.StoneAndPlaster)
        {
            m_Type = type;

            Weight = 1.0;
            LootType = LootType.Blessed;
        }

        public MiniHouseDeed(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public MiniHouseType Type
        {
            get => m_Type;
            set
            {
                m_Type = value;
                InvalidateProperties();
            }
        }

        public override BaseAddon Addon => new MiniHouseAddon(m_Type);
        public override int LabelNumber => 1062096; // a mini house deed

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            list.Add(MiniHouseInfo.GetInfo(m_Type).LabelNumber);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write((int)m_Type);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Type = (MiniHouseType)reader.ReadInt();
                        break;
                    }
            }

            if (Weight == 0.0)
            {
                Weight = 1.0;
            }
        }
    }

    public enum MiniHouseType
    {
        StoneAndPlaster,
        FieldStone,
        SmallBrick,
        Wooden,
        WoodAndPlaster,
        ThatchedRoof,
        Brick,
        TwoStoryWoodAndPlaster,
        TwoStoryStoneAndPlaster,
        Tower,
        SmallStoneKeep,
        Castle,
        LargeHouseWithPatio,
        MarbleHouseWithPatio,
        SmallStoneTower,
        TwoStoryLogCabin,
        TwoStoryVilla,
        SandstoneHouseWithPatio,
        SmallStoneWorkshop,
        SmallMarbleWorkshop,
        MalasMountainPass, // Veteran reward house
        ChurchAtNight      // Veteran reward house
    }

    public class MiniHouseInfo
    {
        private static readonly MiniHouseInfo[] m_Info =
        {
            /* Stone and plaster house           */ new(0x22C4, 1, 1011303),
            /* Field stone house                 */ new(0x22DE, 1, 1011304),
            /* Small brick house                 */ new(0x22DF, 1, 1011305),
            /* Wooden house                      */ new(0x22C9, 1, 1011306),
            /* Wood and plaster house            */ new(0x22E0, 1, 1011307),
            /* Thatched-roof cottage             */ new(0x22E1, 1, 1011308),
            /* Brick house                       */ new(1011309, 0x22CD, 0x22CB, 0x22CC, 0x22CA),
            /* Two-story wood and plaster house  */ new(1011310, 0x2301, 0x2302, 0x2304, 0x2303),
            /* Two-story stone and plaster house */ new(1011311, 0x22FC, 0x22FD, 0x22FF, 0x22FE),
            /* Tower                             */ new(1011312, 0x22F7, 0x22F8, 0x22FA, 0x22F9),
            /* Small stone keep                  */ new(0x22E6, 9, 1011313),
            /* Castle                            */
            new(
                1011314,
                0x22CE,
                0x22D0,
                0x22D2,
                0x22D7,
                0x22CF,
                0x22D1,
                0x22D4,
                0x22D9,
                0x22D3,
                0x22D5,
                0x22D6,
                0x22DB,
                0x22D8,
                0x22DA,
                0x22DC,
                0x22DD
            ),
            /* Large house with patio            */ new(0x22E2, 4, 1011315),
            /* Marble house with patio           */ new(0x22EF, 4, 1011316),
            /* Small stone tower                 */ new(0x22F5, 1, 1011317),
            /* Two-story log cabin               */ new(0x22FB, 1, 1011318),
            /* Two-story villa                   */ new(0x2300, 1, 1011319),
            /* Sandstone house with patio        */ new(0x22F3, 1, 1011320),
            /* Small stone workshop              */ new(0x22F6, 1, 1011321),
            /* Small marble workshop             */ new(0x22F4, 1, 1011322),
            /* Malas Mountain Pass               */ new(1062692, 0x2316, 0x2315, 0x2314, 0x2313),
            /* Church At Night                   */ new(1072215, 0x2318, 0x2317, 0x2319, 0x1)
        };

        public MiniHouseInfo(int start, int count, int labelNumber)
        {
            Graphics = new int[count];

            for (var i = 0; i < count; ++i)
            {
                Graphics[i] = start + i;
            }

            LabelNumber = labelNumber;
        }

        public MiniHouseInfo(int labelNumber, params int[] graphics)
        {
            LabelNumber = labelNumber;
            Graphics = graphics;
        }

        public int[] Graphics { get; }

        public int LabelNumber { get; }

        public static MiniHouseInfo GetInfo(MiniHouseType type)
        {
            var v = (int)type;

            if (v < 0 || v >= m_Info.Length)
            {
                v = 0;
            }

            return m_Info[v];
        }
    }
}
