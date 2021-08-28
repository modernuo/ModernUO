using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Mobiles
{
        [TypeAlias(
            "Server.Mobiles.BrownHorse",
            "Server.Mobiles.DirtyHorse",
            "Server.Mobiles.GrayHorse",
            "Server.Mobiles.TanHorse"
        )]
        public class Horse : BaseMount
        {
            private static readonly int[] m_IDs =
            {
            0xC8, 0x3E9F,
            0xE2, 0x3EA0,
            0xE4, 0x3EA1,
            0xCC, 0x3EA2
        };
        private bool m_BardingExceptional;
        private Mobile m_BardingCrafter;
        private int m_BardingHP;
        private bool m_HasBarding;
        private CraftResource m_BardingResource;
        public Body bodyVal;
        public int idVal;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile BardingCrafter
        {
            get { return m_BardingCrafter; }
            set { m_BardingCrafter = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool BardingExceptional
        {
            get { return m_BardingExceptional; }
            set { m_BardingExceptional = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BardingHP
        {
            get { return m_BardingHP; }
            set { m_BardingHP = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasBarding
        {
            get { return m_HasBarding; }
            set
            {
                m_HasBarding = value;

                if (m_HasBarding)
                {
                    Hue = CraftResources.GetHue(m_BardingResource);
                    BodyValue = 284;
                    ItemID = 0x3E92;
                }
                else
                {
                    Hue = 0;
                    BodyValue = bodyVal;
                    ItemID = idVal;
                }

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource BardingResource
        {
            get { return m_BardingResource; }
            set
            {
                m_BardingResource = value;

                if (m_HasBarding)
                    Hue = CraftResources.GetHue(value);

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BardingMaxHP
        {
            get { return m_BardingExceptional ? 2500 : 1000; }
        }

        [Constructible]
            public Horse(string name = "a horse") : base(
                name,
                0xE2,
                0x3EA0,
                AIType.AI_Animal,
                FightMode.Aggressor,
                10,
                1,
                0.2,
                0.4
            )
            {
                var random = Utility.Random(4);

                Body = m_IDs[random * 2];
                ItemID = m_IDs[random * 2 + 1];
                bodyVal = this.Body;
                idVal = this.ItemID;

                BaseSoundID = 0xA8;

                SetStr(22, 98);
                SetDex(56, 75);
                SetInt(6, 10);

                SetHits(28, 45);
                SetMana(0);

                SetDamage(3, 4);

                SetDamageType(ResistanceType.Physical, 100);

                SetResistance(ResistanceType.Physical, 15, 20);

                SetSkill(SkillName.MagicResist, 25.1, 30.0);
                SetSkill(SkillName.Tactics, 29.3, 44.0);
                SetSkill(SkillName.Wrestling, 29.3, 44.0);

                Fame = 300;
                Karma = 300;

                Tamable = true;
                ControlSlots = 1;
                MinTameSkill = 29.1;
            }

            public Horse(Serial serial) : base(serial)
            {
            }

            public override void GetProperties(ObjectPropertyList list)
            {
                base.GetProperties(list);

                if (m_HasBarding && m_BardingExceptional && m_BardingCrafter != null)
                    list.Add(1060853, m_BardingCrafter.Name); // armor exceptionally crafted by ~1_val~
            }

            public override void Serialize(IGenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write((int)1); // version

                writer.Write((bool)m_BardingExceptional);
                writer.Write((Mobile)m_BardingCrafter);
                writer.Write((bool)m_HasBarding);
                writer.Write((int)m_BardingHP);
                writer.Write((int)m_BardingResource);
            }

            public override void Deserialize(IGenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();

                switch (version)
                {
                    case 1:
                        {
                            m_BardingExceptional = reader.ReadBool();
                            m_BardingCrafter = reader.ReadEntity<Mobile>();
                            m_HasBarding = reader.ReadBool();
                            m_BardingHP = reader.ReadInt();
                            m_BardingResource = (CraftResource)reader.ReadInt();
                            break;
                        }
                }

                if (BaseSoundID == -1)
                    BaseSoundID = 0x16A;
            }
        }
    
}
