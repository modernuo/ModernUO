using Server.Items;

namespace Server.Mobiles
{
    public class ShadowWisp : BaseCreature
    {
        [Constructible]
        public ShadowWisp() : base(AIType.AI_Mage, FightMode.Aggressor)
        {
            Body = 165;
            BaseSoundID = 466;

            SetStr(16, 40);
            SetDex(16, 45);
            SetInt(11, 25);

            SetHits(10, 24);

            SetDamage(5, 10);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 15, 20);
            SetResistance(ResistanceType.Fire, 5, 10);
            SetResistance(ResistanceType.Poison, 5, 10);
            SetResistance(ResistanceType.Energy, 15, 20);

            SetSkill(SkillName.EvalInt, 40.0);
            SetSkill(SkillName.Magery, 50.0);
            SetSkill(SkillName.Meditation, 40.0);
            SetSkill(SkillName.MagicResist, 10.0);
            SetSkill(SkillName.Tactics, 0.1, 15.0);
            SetSkill(SkillName.Wrestling, 25.1, 40.0);

            Fame = 500;

            VirtualArmor = 18;

            AddItem(new LightSource());

            PackItem(
                Utility.Random(10) switch
                {
                    0 => new LeftArm(),
                    1 => new RightArm(),
                    2 => new Torso(),
                    3 => new Bone(),
                    4 => new RibCage(),
                    5 => new RibCage(),
                    _ => new BonePile() // 6-9
                }
            );
        }

        public ShadowWisp(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a wisp corpse";
        public override string DefaultName => "a shadow wisp";

        public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;

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
