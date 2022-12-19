using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Malefic : DreadSpider
    {
        [Constructible]
        public Malefic()
        {
            IsParagon = true;
            Hue = 0x455;

            SetStr(210, 284);
            SetDex(153, 197);
            SetInt(349, 390);

            SetHits(600, 747);
            SetStam(153, 197);
            SetMana(349, 390);

            SetDamage(15, 22);

            SetDamageType(ResistanceType.Physical, 20);
            SetDamageType(ResistanceType.Poison, 80);

            SetResistance(ResistanceType.Physical, 60, 70);
            SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 40, 49);
            SetResistance(ResistanceType.Poison, 100);
            SetResistance(ResistanceType.Energy, 41, 48);

            SetSkill(SkillName.Wrestling, 96.9, 112.4);
            SetSkill(SkillName.Tactics, 91.3, 105.4);
            SetSkill(SkillName.MagicResist, 79.8, 95.1);
            SetSkill(SkillName.Magery, 103.0, 118.6);
            SetSkill(SkillName.EvalInt, 105.7, 119.6);
            SetSkill(SkillName.Meditation, 0);

            Fame = 21000;
            Karma = -21000;

            /*
            // TODO: uncomment once added
            if (Utility.RandomDouble() < 0.1)
              PackItem( new ParrotItem() );
            */
        }

        public override string CorpseName => "a Malefic corpse";
        public override string DefaultName => "Malefic";

        public override bool GivesMLMinorArtifact => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich, 3);
        }

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.Dismount;
    }
}
