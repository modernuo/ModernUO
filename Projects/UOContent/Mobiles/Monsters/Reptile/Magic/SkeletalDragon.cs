namespace Server.Mobiles
{
    public class SkeletalDragon : BaseCreature
    {
        [Constructible]
        public SkeletalDragon() : base(AIType.AI_Mage)
        {
            Body = 104;
            BaseSoundID = 0x488;

            SetStr(898, 1030);
            SetDex(68, 200);
            SetInt(488, 620);

            SetHits(558, 599);

            SetDamage(29, 35);

            SetDamageType(ResistanceType.Physical, 75);
            SetDamageType(ResistanceType.Fire, 25);

            SetResistance(ResistanceType.Physical, 75, 80);
            SetResistance(ResistanceType.Fire, 40, 60);
            SetResistance(ResistanceType.Cold, 40, 60);
            SetResistance(ResistanceType.Poison, 70, 80);
            SetResistance(ResistanceType.Energy, 40, 60);

            SetSkill(SkillName.EvalInt, 80.1, 100.0);
            SetSkill(SkillName.Magery, 80.1, 100.0);
            SetSkill(SkillName.MagicResist, 100.3, 130.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 97.6, 100.0);
            SetSkill(SkillName.Necromancy, 120.1, 130.0);
            SetSkill(SkillName.SpiritSpeak, 120.1, 130.0);

            Fame = 22500;
            Karma = -22500;

            VirtualArmor = 80;
        }

        public SkeletalDragon(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a skeletal dragon corpse";
        public override string DefaultName => "a skeletal dragon";

        public override bool ReacquireOnMovement => true;
        public override bool HasBreath => true; // fire breath enabled
        public override int BreathFireDamage => 0;
        public override int BreathColdDamage => 100;
        public override int BreathEffectHue => 0x480;

        public override double BonusPetDamageScalar => Core.SE ? 3.0 : 1.0;
        // TODO: Undead summoning?

        public override bool AutoDispel => true;
        public override Poison PoisonImmune => Poison.Lethal;
        public override bool BleedImmune => true;
        public override int Meat => 19; // where's it hiding these? :)
        public override int Hides => 20;
        public override HideType HideType => HideType.Barbed;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 4);
            AddLoot(LootPack.Gems, 5);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}
