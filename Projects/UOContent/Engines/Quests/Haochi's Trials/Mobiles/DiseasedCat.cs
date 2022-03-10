using Server.Mobiles;

namespace Server.Engines.Quests.Samurai
{
    public class DiseasedCat : BaseCreature
    {
        [Constructible]
        public DiseasedCat() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 0xC9;
            Hue = Utility.RandomAnimalHue();
            BaseSoundID = 0x69;

            SetStr(9);
            SetDex(35);
            SetInt(5);

            SetHits(6);
            SetMana(0);

            SetDamage(1);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 5, 10);

            SetSkill(SkillName.MagicResist, 5.0);
            SetSkill(SkillName.Tactics, 4.0);
            SetSkill(SkillName.Wrestling, 5.0);

            VirtualArmor = 8;
        }

        public DiseasedCat(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a diseased cat";

        public override bool AlwaysMurderer => true;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
