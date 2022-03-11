using Server.Items;

namespace Server.Mobiles
{
    public class FireElemental : BaseCreature
    {
        [Constructible]
        public FireElemental() : base(AIType.AI_Mage)
        {
            Body = 15;
            BaseSoundID = 838;

            SetStr(126, 155);
            SetDex(166, 185);
            SetInt(101, 125);

            SetHits(76, 93);

            SetDamage(7, 9);

            SetDamageType(ResistanceType.Physical, 25);
            SetDamageType(ResistanceType.Fire, 75);

            SetResistance(ResistanceType.Physical, 35, 45);
            SetResistance(ResistanceType.Fire, 60, 80);
            SetResistance(ResistanceType.Cold, 5, 10);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.EvalInt, 60.1, 75.0);
            SetSkill(SkillName.Magery, 60.1, 75.0);
            SetSkill(SkillName.MagicResist, 75.2, 105.0);
            SetSkill(SkillName.Tactics, 80.1, 100.0);
            SetSkill(SkillName.Wrestling, 70.1, 100.0);

            Fame = 4500;
            Karma = -4500;

            VirtualArmor = 40;
            ControlSlots = 4;

            PackItem(new SulfurousAsh(3));

            AddItem(new LightSource());
        }

        public FireElemental(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a fire elemental corpse";
        public override double DispelDifficulty => 117.5;
        public override double DispelFocus => 45.0;

        public override string DefaultName => "a fire elemental";

        public override bool BleedImmune => true;
        public override int TreasureMapLevel => 2;

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

            if (BaseSoundID == 274)
            {
                BaseSoundID = 838;
            }
        }
    }
}
