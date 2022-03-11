using Server.Items;

namespace Server.Mobiles
{
    public class WandererOfTheVoid : BaseCreature
    {
        [Constructible]
        public WandererOfTheVoid() : base(AIType.AI_Mage)
        {
            Body = 316;
            BaseSoundID = 377;

            SetStr(111, 200);
            SetDex(101, 125);
            SetInt(301, 390);

            SetHits(351, 400);

            SetDamage(11, 13);

            SetDamageType(ResistanceType.Physical, 0);
            SetDamageType(ResistanceType.Cold, 15);
            SetDamageType(ResistanceType.Energy, 85);

            SetResistance(ResistanceType.Physical, 40, 50);
            SetResistance(ResistanceType.Fire, 15, 25);
            SetResistance(ResistanceType.Cold, 40, 50);
            SetResistance(ResistanceType.Poison, 50, 75);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.EvalInt, 60.1, 70.0);
            SetSkill(SkillName.Magery, 60.1, 70.0);
            SetSkill(SkillName.Meditation, 60.1, 70.0);
            SetSkill(SkillName.MagicResist, 50.1, 75.0);
            SetSkill(SkillName.Tactics, 60.1, 70.0);
            SetSkill(SkillName.Wrestling, 60.1, 70.0);

            Fame = 20000;
            Karma = -20000;

            VirtualArmor = 44;

            var count = Utility.RandomMinMax(2, 3);

            for (var i = 0; i < count; ++i)
            {
                PackItem(new TreasureMap(3, Map.Trammel));
            }
        }

        public WandererOfTheVoid(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a wanderer of the void corpse";
        public override string DefaultName => "a wanderer of the void";

        public override bool BleedImmune => true;
        public override Poison PoisonImmune => Poison.Lethal;
        public override int TreasureMapLevel => Core.AOS ? 4 : 1;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich);
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
