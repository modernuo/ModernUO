using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Pyre : Phoenix
    {
        [Constructible]
        public Pyre()
        {
            IsParagon = true;

            Hue = 0x489;

            FightMode = FightMode.Closest;

            SetStr(605, 611);
            SetDex(391, 519);
            SetInt(669, 818);

            SetHits(1783, 1939);

            SetDamage(30);

            SetDamageType(ResistanceType.Physical, 50);
            SetDamageType(ResistanceType.Fire, 50);

            SetResistance(ResistanceType.Physical, 65);
            SetResistance(ResistanceType.Fire, 72, 75);
            SetResistance(ResistanceType.Poison, 36, 41);
            SetResistance(ResistanceType.Energy, 50, 51);

            SetSkill(SkillName.Wrestling, 121.9, 130.6);
            SetSkill(SkillName.Tactics, 114.4, 117.4);
            SetSkill(SkillName.MagicResist, 147.7, 153.0);
            SetSkill(SkillName.Poisoning, 122.8, 124.0);
            SetSkill(SkillName.Magery, 121.8, 127.8);
            SetSkill(SkillName.EvalInt, 103.6, 117.0);
            SetSkill(SkillName.Meditation, 100.0, 110.0);

            Fame = 21000;
            Karma = -21000;
        }

        public override string CorpseName => "a Pyre corpse";
        public override string DefaultName => "Pyre";

        public override bool GivesMLMinorArtifact => true;
        public override int TreasureMapLevel => 5;
        public override bool HasAura => true;

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
