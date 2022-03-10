namespace Server.Mobiles
{
    public class AirElemental : BaseCreature
    {
        [Constructible]
        public AirElemental() : base(AIType.AI_Mage)
        {
            Body = 13;
            Hue = 0x4001;
            BaseSoundID = 655;

            SetStr(126, 155);
            SetDex(166, 185);
            SetInt(101, 125);

            SetHits(76, 93);

            SetDamage(8, 10);

            SetDamageType(ResistanceType.Physical, 20);
            SetDamageType(ResistanceType.Cold, 40);
            SetDamageType(ResistanceType.Energy, 40);

            SetResistance(ResistanceType.Physical, 35, 45);
            SetResistance(ResistanceType.Fire, 15, 25);
            SetResistance(ResistanceType.Cold, 10, 20);
            SetResistance(ResistanceType.Poison, 10, 20);
            SetResistance(ResistanceType.Energy, 25, 35);

            SetSkill(SkillName.EvalInt, 60.1, 75.0);
            SetSkill(SkillName.Magery, 60.1, 75.0);
            SetSkill(SkillName.MagicResist, 60.1, 75.0);
            SetSkill(SkillName.Tactics, 60.1, 80.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            Fame = 4500;
            Karma = -4500;

            VirtualArmor = 40;
            ControlSlots = 2;
        }

        public AirElemental(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "an air elemental corpse";
        public override double DispelDifficulty => 117.5;
        public override double DispelFocus => 45.0;

        public override string DefaultName => "an air elemental";

        public override bool BleedImmune => true;
        public override int TreasureMapLevel => 2;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
            AddLoot(LootPack.Meager);
            AddLoot(LootPack.LowScrolls);
            AddLoot(LootPack.MedScrolls);
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

            if (BaseSoundID == 263)
            {
                BaseSoundID = 655;
            }
        }
    }
}
