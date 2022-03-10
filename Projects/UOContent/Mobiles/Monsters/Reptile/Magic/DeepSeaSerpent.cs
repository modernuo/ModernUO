using Server.Items;

namespace Server.Mobiles
{
    public class DeepSeaSerpent : BaseCreature
    {
        [Constructible]
        public DeepSeaSerpent() : base(AIType.AI_Mage)
        {
            Body = 150;
            BaseSoundID = 447;

            Hue = Utility.Random(0x8A0, 5);

            SetStr(251, 425);
            SetDex(87, 135);
            SetInt(87, 155);

            SetHits(151, 255);

            SetDamage(6, 14);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 30, 40);
            SetResistance(ResistanceType.Fire, 70, 80);
            SetResistance(ResistanceType.Cold, 40, 50);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 15, 20);

            SetSkill(SkillName.MagicResist, 60.1, 75.0);
            SetSkill(SkillName.Tactics, 60.1, 70.0);
            SetSkill(SkillName.Wrestling, 60.1, 70.0);

            Fame = 6000;
            Karma = -6000;

            VirtualArmor = 60;
            CanSwim = true;
            CantWalk = true;

            if (Utility.RandomBool())
            {
                PackItem(new SulfurousAsh(4));
            }
            else
            {
                PackItem(new BlackPearl(4));
            }

            // PackItem( new SpecialFishingNet() );
        }

        public DeepSeaSerpent(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a deep sea serpents corpse";
        public override string DefaultName => "a deep sea serpent";

        public override bool HasBreath => true;
        public override int Meat => 1;
        public override int Scales => 8;
        public override ScaleType ScaleType => ScaleType.Blue;

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
