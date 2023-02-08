using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class SpectralArmour : BaseCreature
    {
        [Constructible]
        public SpectralArmour() : base(AIType.AI_Melee)
        {
            Body = 637;
            Hue = 0x8026;

            AddItem(new Buckler { Movable = false, Hue = 0x835 });
            AddItem(new ChainCoif { Hue = 0x835 });
            AddItem(new PlateGloves { Hue = 0x835 });

            SetStr(101, 110);
            SetDex(101, 110);
            SetInt(101, 110);

            SetHits(178, 201);
            SetStam(191, 200);

            SetDamage(10, 22);

            SetDamageType(ResistanceType.Physical, 75);
            SetDamageType(ResistanceType.Cold, 25);

            SetResistance(ResistanceType.Physical, 35, 45);
            SetResistance(ResistanceType.Fire, 20, 30);
            SetResistance(ResistanceType.Cold, 30, 40);
            SetResistance(ResistanceType.Poison, 20, 30);
            SetResistance(ResistanceType.Energy, 20, 30);

            SetSkill(SkillName.Wrestling, 75.1, 100.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 90.1, 100);

            VirtualArmor = 40;
            Fame = 7000;
            Karma = -7000;
        }

        public override bool DeleteCorpseOnDeath => true;

        public override string DefaultName => "a spectral armour";

        public override Poison PoisonImmune => Poison.Regular;

        public override int GetIdleSound() => 0x200;

        public override int GetAngerSound() => 0x56;

        public override bool OnBeforeDeath()
        {
            if (!base.OnBeforeDeath())
            {
                return false;
            }

            var gold = new Gold(Utility.RandomMinMax(240, 375));
            gold.MoveToWorld(Location, Map);

            Effects.SendLocationEffect(Location, Map, 0x376A, 10, 1);
            return true;
        }
    }
}
