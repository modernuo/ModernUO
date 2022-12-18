using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class StoneHarpy : BaseCreature
    {
        [Constructible]
        public StoneHarpy() : base(AIType.AI_Melee)
        {
            Body = 73;
            BaseSoundID = 402;

            SetStr(296, 320);
            SetDex(86, 110);
            SetInt(51, 75);

            SetHits(178, 192);
            SetMana(0);

            SetDamage(8, 16);

            SetDamageType(ResistanceType.Physical, 75);
            SetDamageType(ResistanceType.Poison, 25);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 20, 30);
            SetResistance(ResistanceType.Cold, 10, 20);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.MagicResist, 50.1, 65.0);
            SetSkill(SkillName.Tactics, 70.1, 100.0);
            SetSkill(SkillName.Wrestling, 70.1, 100.0);

            Fame = 4500;
            Karma = -4500;

            VirtualArmor = 50;
        }

        public override string CorpseName => "a stone harpy corpse";
        public override string DefaultName => "a stone harpy";

        public override int Meat => 1;
        public override int Feathers => 50;
        public override bool CanFly => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average, 2);
            AddLoot(LootPack.Gems, 2);
        }

        public override int GetAttackSound() => 916;

        public override int GetAngerSound() => 916;

        public override int GetDeathSound() => 917;

        public override int GetHurtSound() => 919;

        public override int GetIdleSound() => 918;
    }
}
