using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class MasterMikael : BoneMagi
    {
        [Constructible]
        public MasterMikael()
        {
            IsParagon = true;

            Hue = 0x8FD;

            SetStr(93, 122);
            SetDex(91, 100);
            SetInt(252, 271);

            SetHits(789, 1014);

            SetDamage(11, 19);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 55, 59);
            SetResistance(ResistanceType.Fire, 40, 46);
            SetResistance(ResistanceType.Cold, 72, 80);
            SetResistance(ResistanceType.Poison, 44, 49);
            SetResistance(ResistanceType.Energy, 50, 57);

            SetSkill(SkillName.Wrestling, 80.1, 87.2);
            SetSkill(SkillName.Tactics, 79.0, 90.9);
            SetSkill(SkillName.MagicResist, 90.3, 106.9);
            SetSkill(SkillName.Magery, 103.8, 108.0);
            SetSkill(SkillName.EvalInt, 96.1, 105.3);
            SetSkill(SkillName.Necromancy, 103.8, 108.0);
            SetSkill(SkillName.SpiritSpeak, 96.1, 105.3);

            Fame = 18000;
            Karma = -18000;

            if (Utility.RandomBool())
            {
                PackNecroScroll(Utility.RandomMinMax(5, 9));
            }
            else
            {
                PackScroll(4, 7);
            }

            PackReg(3);
            PackNecroReg(1, 10);
        }

        public override string CorpseName => "a Master Mikael corpse";
        public override string DefaultName => "Master Mikael";

        // TODO: Special move?

        /*
        // TODO: uncomment once added
        public override void OnDeath( Container c )
        {
          base.OnDeath( c );
    
          if (Utility.RandomDouble() < 0.15)
            c.DropItem( new DisintegratingThesisNotes() );
    
          if (Utility.RandomDouble() < 0.1)
            c.DropItem( new ParrotItem() );
        }
        */

        public override bool GivesMLMinorArtifact => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich, 2);
        }
    }
}
