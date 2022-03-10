using Server.Items;

namespace Server.Mobiles
{
    public class Bogle : BaseCreature
    {
        [Constructible]
        public Bogle() : base(AIType.AI_Mage)
        {
            Body = 153;
            BaseSoundID = 0x482;

            SetStr(76, 100);
            SetDex(76, 95);
            SetInt(36, 60);

            SetHits(46, 60);

            SetDamage(7, 11);

            SetSkill(SkillName.EvalInt, 55.1, 70.0);
            SetSkill(SkillName.Magery, 55.1, 70.0);
            SetSkill(SkillName.MagicResist, 55.1, 70.0);
            SetSkill(SkillName.Tactics, 45.1, 60.0);
            SetSkill(SkillName.Wrestling, 45.1, 55.0);

            Fame = 4000;
            Karma = -4000;

            VirtualArmor = 28;
            PackItem(Loot.RandomWeapon());
            PackItem(new Bone());
        }

        public Bogle(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a ghostly corpse";
        public override string DefaultName => "a bogle";

        public override bool BleedImmune => true;
        public override Poison PoisonImmune => Poison.Lethal;

        public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;

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
