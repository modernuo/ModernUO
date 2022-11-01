namespace Server.Mobiles
{
    public class ScaledSwampDragon : BaseMount
    {
        public override string DefaultName => "a swamp dragon";

        [Constructible]
        public ScaledSwampDragon() : base(0x31F, 0x3EBE, AIType.AI_Melee, FightMode.Aggressor)
        {
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

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 93.9;
        }

        public ScaledSwampDragon(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a swamp dragon corpse";

        public override bool AutoDispel => !Controlled;
        public override FoodType FavoriteFood => FoodType.Meat;

        public override bool OverrideBondingReqs() => true;

        public override double GetControlChance(Mobile m, bool useBaseSkill = false) => 1.0;

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
    }
}
