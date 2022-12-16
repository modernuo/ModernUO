using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Grobu : BlackBear
    {
        [Constructible]
        public Grobu()
        {
            IsParagon = true;

            Hue = 0x455;

            AI = AIType.AI_Melee;
            FightMode = FightMode.Closest;

            SetStr(192, 210);
            SetDex(132, 150);
            SetInt(50, 52);

            SetHits(1235, 1299);
            SetStam(132, 150);
            SetMana(9);

            SetDamage(15, 18);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 40, 45);
            SetResistance(ResistanceType.Fire, 20, 40);
            SetResistance(ResistanceType.Cold, 32, 35);
            SetResistance(ResistanceType.Poison, 25, 30);
            SetResistance(ResistanceType.Energy, 22, 34);

            SetSkill(SkillName.Wrestling, 96.4, 119.0);
            SetSkill(SkillName.Tactics, 96.2, 116.5);
            SetSkill(SkillName.MagicResist, 66.2, 83.7);

            Fame = 1000;
            Karma = 1000;
        }

        public override string CorpseName => "a Grobu corpse";
        public override string DefaultName => "Grobu";

        public override bool GivesMLMinorArtifact => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 2);
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            c.DropItem(new GrobusFur());
        }
    }
}
