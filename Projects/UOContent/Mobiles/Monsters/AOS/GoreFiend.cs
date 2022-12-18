using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class GoreFiend : BaseCreature
    {
        [Constructible]
        public GoreFiend() : base(AIType.AI_Melee)
        {
            Body = 305;
            BaseSoundID = 224;

            SetStr(161, 185);
            SetDex(41, 65);
            SetInt(46, 70);

            SetHits(97, 111);

            SetDamage(15, 21);

            SetDamageType(ResistanceType.Physical, 85);
            SetDamageType(ResistanceType.Poison, 15);

            SetResistance(ResistanceType.Physical, 35, 45);
            SetResistance(ResistanceType.Fire, 25, 35);
            SetResistance(ResistanceType.Cold, 15, 25);
            SetResistance(ResistanceType.Poison, 5, 15);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.MagicResist, 40.1, 55.0);
            SetSkill(SkillName.Tactics, 45.1, 70.0);
            SetSkill(SkillName.Wrestling, 50.1, 70.0);

            Fame = 1500;
            Karma = -1500;

            VirtualArmor = 24;
        }

        public override string CorpseName => "a gore fiend corpse";
        public override string DefaultName => "a gore fiend";

        public override bool BleedImmune => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
        }

        public override int GetDeathSound() => 1218;
    }
}
