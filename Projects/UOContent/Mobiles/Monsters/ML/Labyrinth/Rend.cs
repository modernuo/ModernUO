using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Rend : Reptalon
    {
        [Constructible]
        public Rend()
        {
            IsParagon = true;

            Hue = 0x455;

            SetStr(1261, 1284);
            SetDex(363, 384);
            SetInt(601, 642);

            SetHits(5176, 6100);

            SetDamage(26, 33);

            SetDamageType(ResistanceType.Physical, 100);
            SetDamageType(ResistanceType.Poison, 0);
            SetDamageType(ResistanceType.Energy, 0);

            SetResistance(ResistanceType.Physical, 75, 85);
            SetResistance(ResistanceType.Fire, 81, 94);
            SetResistance(ResistanceType.Cold, 46, 55);
            SetResistance(ResistanceType.Poison, 35, 44);
            SetResistance(ResistanceType.Energy, 45, 52);

            SetSkill(SkillName.Wrestling, 136.3, 150.3);
            SetSkill(SkillName.Tactics, 133.4, 141.4);
            SetSkill(SkillName.MagicResist, 90.9, 110.0);
            SetSkill(SkillName.Anatomy, 66.6, 72.0);

            Fame = 21000;
            Karma = -21000;
        }

        public override string CorpseName => "a Rend corpse";
        public override string DefaultName => "Rend";

        public override bool GivesMLMinorArtifact => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich, 3);
        }

        public override WeaponAbility GetWeaponAbility()
        {
            if (Utility.RandomBool())
            {
                return WeaponAbility.ParalyzingBlow;
            }

            return WeaponAbility.BleedAttack;
        }
    }
}
