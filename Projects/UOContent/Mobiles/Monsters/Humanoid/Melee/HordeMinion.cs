using Server.Items;

namespace Server.Mobiles
{
    public class HordeMinion : BaseCreature
    {
        [Constructible]
        public HordeMinion() : base(AIType.AI_Melee)
        {
            Body = 776;
            BaseSoundID = 357;

            SetStr(16, 40);
            SetDex(31, 60);
            SetInt(11, 25);

            SetHits(10, 24);

            SetDamage(5, 10);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 15, 20);
            SetResistance(ResistanceType.Fire, 5, 10);

            SetSkill(SkillName.MagicResist, 10.0);
            SetSkill(SkillName.Tactics, 0.1, 15.0);
            SetSkill(SkillName.Wrestling, 25.1, 40.0);

            Fame = 500;
            Karma = -500;

            VirtualArmor = 18;

            AddItem(new LightSource());

            PackItem(new Bone(3));
            // TODO: Body parts
        }

        public HordeMinion(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a horde minion corpse";
        public override string DefaultName => "a horde minion";

        public override int GetIdleSound() => 338;

        public override int GetAngerSound() => 338;

        public override int GetDeathSound() => 338;

        public override int GetAttackSound() => 406;

        public override int GetHurtSound() => 194;

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
