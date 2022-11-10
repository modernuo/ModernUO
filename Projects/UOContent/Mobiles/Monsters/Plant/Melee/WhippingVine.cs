using Server.Items;

namespace Server.Mobiles
{
    public class WhippingVine : BaseCreature
    {
        [Constructible]
        public WhippingVine() : base(AIType.AI_Melee)
        {
            Body = 8;
            Hue = 0x851;
            BaseSoundID = 352;

            SetStr(251, 300);
            SetDex(76, 100);
            SetInt(26, 40);

            SetMana(0);

            SetDamage(7, 25);

            SetDamageType(ResistanceType.Physical, 70);
            SetDamageType(ResistanceType.Poison, 30);

            SetResistance(ResistanceType.Physical, 75, 85);
            SetResistance(ResistanceType.Fire, 15, 25);
            SetResistance(ResistanceType.Cold, 15, 25);
            SetResistance(ResistanceType.Poison, 75, 85);
            SetResistance(ResistanceType.Energy, 35, 45);

            SetSkill(SkillName.MagicResist, 70.0);
            SetSkill(SkillName.Tactics, 70.0);
            SetSkill(SkillName.Wrestling, 70.0);

            Fame = 1000;
            Karma = -1000;

            VirtualArmor = 45;

            PackReg(3);
            PackItem(new FertileDirt(Utility.RandomMinMax(1, 10)));

            if (Utility.RandomDouble() < 0.2)
            {
                PackItem(new ExecutionersCap());
            }

            PackItem(new Vines());
        }

        public WhippingVine(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a whipping vine corpse";
        public override string DefaultName => "a whipping vine";

        public override bool BardImmune => !Core.AOS;
        public override Poison PoisonImmune => Poison.Lethal;

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
