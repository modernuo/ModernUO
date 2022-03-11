namespace Server.Mobiles
{
    public class GiantIceWorm : BaseCreature
    {
        [Constructible]
        public GiantIceWorm() : base(AIType.AI_Melee)
        {
            Body = 89;
            BaseSoundID = 0xDC;

            SetStr(216, 245);
            SetDex(76, 100);
            SetInt(66, 85);

            SetHits(130, 147);

            SetDamage(7, 17);

            SetDamageType(ResistanceType.Physical, 10);
            SetDamageType(ResistanceType.Cold, 90);

            SetResistance(ResistanceType.Physical, 30, 35);
            SetResistance(ResistanceType.Fire, 0);
            SetResistance(ResistanceType.Cold, 80, 90);
            SetResistance(ResistanceType.Poison, 15, 25);
            SetResistance(ResistanceType.Energy, 10, 20);

            SetSkill(SkillName.Poisoning, 75.1, 95.0);
            SetSkill(SkillName.MagicResist, 45.1, 60.0);
            SetSkill(SkillName.Tactics, 75.1, 80.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            Fame = 4500;
            Karma = -4500;

            VirtualArmor = 40;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 71.1;
        }

        public GiantIceWorm(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a giant ice worm corpse";
        public override bool SubdueBeforeTame => true;
        public override string DefaultName => "a giant ice worm";

        public override Poison PoisonImmune => Poison.Greater;

        public override Poison HitPoison => Poison.Greater;

        public override FoodType FavoriteFood => FoodType.Meat;

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
