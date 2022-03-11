using Server.Items;

namespace Server.Mobiles
{
    public class EarthElemental : BaseCreature
    {
        [Constructible]
        public EarthElemental() : base(AIType.AI_Melee)
        {
            Body = 14;
            BaseSoundID = 268;

            SetStr(126, 155);
            SetDex(66, 85);
            SetInt(71, 92);

            SetHits(76, 93);

            SetDamage(9, 16);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 30, 35);
            SetResistance(ResistanceType.Fire, 10, 20);
            SetResistance(ResistanceType.Cold, 10, 20);
            SetResistance(ResistanceType.Poison, 15, 25);
            SetResistance(ResistanceType.Energy, 15, 25);

            SetSkill(SkillName.MagicResist, 50.1, 95.0);
            SetSkill(SkillName.Tactics, 60.1, 100.0);
            SetSkill(SkillName.Wrestling, 60.1, 100.0);

            Fame = 3500;
            Karma = -3500;

            VirtualArmor = 34;
            ControlSlots = 2;

            PackItem(new FertileDirt(Utility.RandomMinMax(1, 4)));
            PackItem(new MandrakeRoot());

            Item ore = new IronOre(5);
            ore.ItemID = 0x19B7;
            PackItem(ore);
        }

        public EarthElemental(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "an earth elemental corpse";
        public override double DispelDifficulty => 117.5;
        public override double DispelFocus => 45.0;

        public override string DefaultName => "an earth elemental";

        public override bool BleedImmune => true;
        public override int TreasureMapLevel => 1;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
            AddLoot(LootPack.Meager);
            AddLoot(LootPack.Gems);
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
