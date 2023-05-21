using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class SirPatrick : SkeletalKnight
    {
        [Constructible]
        public SirPatrick()
        {
            IsParagon = true;

            Hue = 0x47E;

            SetStr(208, 319);
            SetDex(98, 132);
            SetInt(45, 91);

            SetHits(616, 884);

            SetDamage(15, 25);

            SetDamageType(ResistanceType.Physical, 40);
            SetDamageType(ResistanceType.Cold, 60);

            SetResistance(ResistanceType.Physical, 55, 62);
            SetResistance(ResistanceType.Fire, 40, 48);
            SetResistance(ResistanceType.Cold, 71, 80);
            SetResistance(ResistanceType.Poison, 40, 50);
            SetResistance(ResistanceType.Energy, 50, 60);

            SetSkill(SkillName.Wrestling, 126.3, 136.5);
            SetSkill(SkillName.Tactics, 128.5, 143.8);
            SetSkill(SkillName.MagicResist, 102.8, 117.9);
            SetSkill(SkillName.Anatomy, 127.5, 137.2);

            Fame = 18000;
            Karma = -18000;
        }

        public override string CorpseName => "a Sir Patrick corpse";
        public override string DefaultName => "Sir Patrick";

        /*
        // TODO: uncomment once added
        public override void OnDeath( Container c )
        {
          base.OnDeath( c );

          if (Utility.RandomDouble() < 0.15)
            c.DropItem( new DisintegratingThesisNotes() );

          if (Utility.RandomDouble() < 0.05)
            c.DropItem( new AssassinChest() );
        }
        */

        public override bool GivesMLMinorArtifact => true;

        private static MonsterAbility[] _abilities = { new SirPatrickDrainLife() };
        public override MonsterAbility[] GetMonsterAbilities() => _abilities;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich, 2);
        }

        private class SirPatrickDrainLife : DrainLifeAreaAttack
        {
            public override int MinDamage => 14;
            public override int MaxDamage => 30;

            protected override void DoEffectTarget(BaseCreature source, Mobile defender)
            {
                defender.FixedParticles(0x374A, 10, 15, 5013, 0x455, 0, EffectLayer.Waist);
                defender.PlaySound(0x1EA);

                DrainLife(source, defender);
            }
        }
    }
}
