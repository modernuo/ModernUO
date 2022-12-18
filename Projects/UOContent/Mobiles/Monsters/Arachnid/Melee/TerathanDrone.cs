using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class TerathanDrone : BaseCreature
    {
        [Constructible]
        public TerathanDrone() : base(AIType.AI_Melee)
        {
            Body = 71;
            BaseSoundID = 594;

            SetStr(36, 65);
            SetDex(96, 145);
            SetInt(21, 45);

            SetHits(22, 39);
            SetMana(0);

            SetDamage(6, 12);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 20, 25);
            SetResistance(ResistanceType.Fire, 10, 20);
            SetResistance(ResistanceType.Cold, 15, 25);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 15, 25);

            SetSkill(SkillName.Poisoning, 40.1, 60.0);
            SetSkill(SkillName.MagicResist, 30.1, 45.0);
            SetSkill(SkillName.Tactics, 30.1, 50.0);
            SetSkill(SkillName.Wrestling, 40.1, 50.0);

            Fame = 2000;
            Karma = -2000;

            VirtualArmor = 24;

            PackItem(new SpidersSilk(2));
        }

        public override string CorpseName => "a terathan drone corpse";
        public override string DefaultName => "a terathan drone";

        public override int Meat => 4;

        public override OppositionGroup OppositionGroup => OppositionGroup.TerathansAndOphidians;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager);
            // TODO: weapon?
        }
    }
}
