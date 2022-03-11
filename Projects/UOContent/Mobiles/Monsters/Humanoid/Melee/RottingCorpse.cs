namespace Server.Mobiles
{
    public class RottingCorpse : BaseCreature
    {
        [Constructible]
        public RottingCorpse() : base(AIType.AI_Melee)
        {
            Body = 155;
            BaseSoundID = 471;

            SetStr(301, 350);
            SetDex(75);
            SetInt(151, 200);

            SetHits(1200);
            SetStam(150);
            SetMana(0);

            SetDamage(8, 10);

            SetDamageType(ResistanceType.Physical, 0);
            SetDamageType(ResistanceType.Cold, 50);
            SetDamageType(ResistanceType.Poison, 50);

            SetResistance(ResistanceType.Physical, 35, 45);
            SetResistance(ResistanceType.Fire, 20, 30);
            SetResistance(ResistanceType.Cold, 50, 70);
            SetResistance(ResistanceType.Poison, 40, 50);
            SetResistance(ResistanceType.Energy, 20, 30);

            SetSkill(SkillName.Poisoning, 120.0);
            SetSkill(SkillName.MagicResist, 250.0);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 6000;
            Karma = -6000;

            VirtualArmor = 40;
        }

        public RottingCorpse(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a rotting corpse";
        public override string DefaultName => "a rotting corpse";

        public override bool BleedImmune => true;
        public override Poison PoisonImmune => Poison.Lethal;
        public override Poison HitPoison => Poison.Lethal;
        public override int TreasureMapLevel => 5;

        public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 2);
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
