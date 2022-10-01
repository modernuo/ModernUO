namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.Hellcat")]
    public class HellCat : BaseCreature
    {
        [Constructible]
        public HellCat() : base(AIType.AI_Melee)
        {
            Body = 0xC9;
            Hue = Utility.RandomList(0x647, 0x650, 0x659, 0x662, 0x66B, 0x674);
            BaseSoundID = 0x69;

            SetStr(51, 100);
            SetDex(52, 150);
            SetInt(13, 85);

            SetHits(48, 67);

            SetDamage(6, 12);

            SetDamageType(ResistanceType.Physical, 40);
            SetDamageType(ResistanceType.Fire, 60);

            SetResistance(ResistanceType.Physical, 25, 35);
            SetResistance(ResistanceType.Fire, 80, 90);
            SetResistance(ResistanceType.Energy, 15, 20);

            SetSkill(SkillName.MagicResist, 45.1, 60.0);
            SetSkill(SkillName.Tactics, 40.1, 55.0);
            SetSkill(SkillName.Wrestling, 30.1, 40.0);

            Fame = 1000;
            Karma = -1000;

            VirtualArmor = 30;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 71.1;
        }

        public HellCat(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a hell cat corpse";
        public override string DefaultName => "a hell cat";

        public override int Hides => 10;
        public override HideType HideType => HideType.Spined;
        public override FoodType FavoriteFood => FoodType.Meat;
        public override PackInstinct PackInstinct => PackInstinct.Feline;

        private static MonsterAbility[] _abilities = { MonsterAbility.FireBreath };
        public override MonsterAbility[] GetMonsterAbilities() => _abilities;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager);
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
