using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(2, false)]
    public partial class SwampDragon : BaseMount
    {
        public override string DefaultName => "a swamp dragon";

        [Constructible]
        public SwampDragon() : base(0x31A, 0x3EBD, AIType.AI_Melee, FightMode.Aggressor)
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

        public override string CorpseName => "a swamp dragon corpse";

        [InvalidateProperties]
        [SerializableField(0)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private bool _bardingExceptional;

        [InvalidateProperties]
        [SerializableField(1)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private string _bardingCraftedBy;

        [SerializedCommandProperty(AccessLevel.GameMaster)]
        [InvalidateProperties]
        [SerializableField(3)]
        private int _bardingHP;

        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(2)]
        public bool HasBarding
        {
            get => _hasBarding;
            set
            {
                _hasBarding = value;

                if (_hasBarding)
                {
                    Hue = CraftResources.GetHue(_bardingResource);
                    Body = 0x31F;
                    ItemID = 0x3EBE;
                }
                else
                {
                    Hue = 0x851;
                    Body = 0x31A;
                    ItemID = 0x3EBD;
                }
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(4)]
        public CraftResource BardingResource
        {
            get => _bardingResource;
            set
            {
                _bardingResource = value;

                if (_hasBarding)
                {
                    Hue = CraftResources.GetHue(value);
                }

                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BardingMaxHP => _bardingExceptional ? 2500 : 1000;

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

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (_hasBarding && _bardingExceptional && _bardingCraftedBy != null)
            {
                list.Add(1060853, _bardingCraftedBy); // armor exceptionally crafted by ~1_val~
            }
        }

        private void Deserialize(IGenericReader reader, int version)
        {
            _bardingExceptional = reader.ReadBool();
            _bardingCraftedBy = reader.ReadString();
            _hasBarding = reader.ReadBool();
            _bardingHP = reader.ReadInt();
            _bardingResource = (CraftResource)reader.ReadInt();
        }
    }
}
