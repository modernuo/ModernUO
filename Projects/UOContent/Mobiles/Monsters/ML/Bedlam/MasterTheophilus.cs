using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class MasterTheophilus : EvilMageLord
    {
        [Constructible]
        public MasterTheophilus()
        {
            IsParagon = true;

            Title = "the necromancer";
            Hue = 0;

            SetStr(137, 187);
            SetDex(253, 301);
            SetInt(393, 444);

            SetHits(663, 876);

            SetDamage(15, 20);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 55, 60);
            SetResistance(ResistanceType.Fire, 50, 58);
            SetResistance(ResistanceType.Cold, 50, 60);
            SetResistance(ResistanceType.Poison, 50, 60);
            SetResistance(ResistanceType.Energy, 50, 60);

            SetSkill(SkillName.Wrestling, 69.9, 105.3);
            SetSkill(SkillName.Tactics, 113.0, 117.9);
            SetSkill(SkillName.MagicResist, 127.0, 132.8);
            SetSkill(SkillName.Magery, 138.1, 143.7);
            SetSkill(SkillName.EvalInt, 125.6, 133.8);
            SetSkill(SkillName.Necromancy, 125.6, 133.8);
            SetSkill(SkillName.SpiritSpeak, 125.6, 133.8);
            SetSkill(SkillName.Meditation, 128.8, 132.9);

            Fame = 18000;
            Karma = -18000;

            AddItem(new Shoes(0x537));
            AddItem(new Robe(0x452));

            for (var i = 0; i < 2; ++i)
            {
                if (Utility.RandomBool())
                {
                    PackNecroScroll(Utility.RandomMinMax(5, 9));
                }
                else
                {
                    PackScroll(4, 7);
                }
            }

            PackReg(7);
            PackReg(7);
            PackReg(8);
        }

        public override string CorpseName => "a Master Theophilus corpse";
        public override string DefaultName => "Master Theophilus";

        public override bool GivesMLMinorArtifact => true;
        public override int TreasureMapLevel => 5;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich, 3);
        }

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.ParalyzingBlow;
    }
}
