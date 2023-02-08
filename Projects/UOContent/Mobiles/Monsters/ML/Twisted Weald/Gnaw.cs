using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Gnaw : DireWolf
    {
        [Constructible]
        public Gnaw()
        {
            IsParagon = true;

            Hue = 0x130;

            SetStr(151, 172);
            SetDex(124, 145);
            SetInt(60, 86);

            SetHits(817, 857);
            SetStam(124, 145);
            SetMana(52, 86);

            SetDamage(16, 22);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 64, 69);
            SetResistance(ResistanceType.Fire, 53, 56);
            SetResistance(ResistanceType.Cold, 22, 27);
            SetResistance(ResistanceType.Poison, 27, 30);
            SetResistance(ResistanceType.Energy, 21, 34);

            SetSkill(SkillName.Wrestling, 106.4, 116.5);
            SetSkill(SkillName.Tactics, 84.1, 103.2);
            SetSkill(SkillName.MagicResist, 96.8, 110.7);

            Fame = 17500;
            Karma = -17500;
        }

        /*
        // TODO: uncomment once added
        public override void OnDeath( Container c )
        {
          base.OnDeath( c );
    
          if (Utility.RandomDouble() < 0.3)
            c.DropItem( new GnawsFang() );
        }
        */

        public override string CorpseName => "a Gnaw corpse";
        public override string DefaultName => "Gnaw";
        public override bool GivesMLMinorArtifact => true;
        public override int Hides => 28;
        public override int Meat => 4;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 2);
        }
    }
}
