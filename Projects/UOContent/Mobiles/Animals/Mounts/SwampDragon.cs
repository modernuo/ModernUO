using Server.Items;

namespace Server.Mobiles
{
    public class SwampDragon : BaseMount
    {
        private Mobile m_BardingCrafter;
        private bool m_BardingExceptional;
        private int m_BardingHP;
        private CraftResource m_BardingResource;
        private bool m_HasBarding;

        [Constructible]
        public SwampDragon(string name = "a swamp dragon") : base(
            name,
            0x31A,
            0x3EBD,
            AIType.AI_Melee,
            FightMode.Aggressor,
            10,
            1,
            0.2,
            0.4
        )
        {
            BaseSoundID = 0x16A;

            SetStr(201, 300);
            SetDex(66, 85);
            SetInt(61, 100);

            SetHits(121, 180);

            SetDamage(3, 4);

            SetDamageType(ResistanceType.Physical, 75);
            SetDamageType(ResistanceType.Poison, 25);

            SetResistance(ResistanceType.Physical, 35, 40);
            SetResistance(ResistanceType.Fire, 20, 30);
            SetResistance(ResistanceType.Cold, 20, 40);
            SetResistance(ResistanceType.Poison, 20, 30);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.Anatomy, 45.1, 55.0);
            SetSkill(SkillName.MagicResist, 45.1, 55.0);
            SetSkill(SkillName.Tactics, 45.1, 55.0);
            SetSkill(SkillName.Wrestling, 45.1, 55.0);

            Fame = 2000;
            Karma = -2000;

            Hue = 0x851;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 93.9;
        }

        public SwampDragon(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a swamp dragon corpse";

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile BardingCrafter
        {
            get => m_BardingCrafter;
            set
            {
                m_BardingCrafter = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool BardingExceptional
        {
            get => m_BardingExceptional;
            set
            {
                m_BardingExceptional = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BardingHP
        {
            get => m_BardingHP;
            set
            {
                m_BardingHP = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasBarding
        {
            get => m_HasBarding;
            set
            {
                m_HasBarding = value;

                if (m_HasBarding)
                {
                    Hue = CraftResources.GetHue(m_BardingResource);
                    BodyValue = 0x31F;
                    ItemID = 0x3EBE;
                }
                else
                {
                    Hue = 0x851;
                    BodyValue = 0x31A;
                    ItemID = 0x3EBD;
                }

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource BardingResource
        {
            get => m_BardingResource;
            set
            {
                m_BardingResource = value;

                if (m_HasBarding)
                {
                    Hue = CraftResources.GetHue(value);
                }

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BardingMaxHP => m_BardingExceptional ? 2500 : 1000;

        public override bool ReacquireOnMovement => true;
        public override bool AutoDispel => !Controlled;
        public override FoodType FavoriteFood => FoodType.Meat;
        public override int Meat => 19;
        public override int Hides => 20;
        public override int Scales => 5;
        public override ScaleType ScaleType => ScaleType.Green;
        public override bool CanAngerOnTame => true;

        public override bool OverrideBondingReqs() => true;

        public override int GetIdleSound() => 0x2CE;

        public override int GetDeathSound() => 0x2CC;

        public override int GetHurtSound() => 0x2D1;

        public override int GetAttackSound() => 0x2C8;

        public override double GetControlChance(Mobile m, bool useBaseSkill = false) => 1.0;

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_HasBarding && m_BardingExceptional && m_BardingCrafter != null)
            {
                list.Add(1060853, m_BardingCrafter.Name); // armor exceptionally crafted by ~1_val~
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(m_BardingExceptional);
            writer.Write(m_BardingCrafter);
            writer.Write(m_HasBarding);
            writer.Write(m_BardingHP);
            writer.Write((int)m_BardingResource);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

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

            if (Hue == 0 && !m_HasBarding)
            {
                Hue = 0x851;
            }

            if (BaseSoundID == -1)
            {
                BaseSoundID = 0x16A;
            }
        }
    }
}
