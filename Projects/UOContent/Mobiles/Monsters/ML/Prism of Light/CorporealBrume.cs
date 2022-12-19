using ModernUO.Serialization;
using System;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class CorporealBrume : BaseCreature
    {
        [Constructible]
        public CorporealBrume()
            : base(AIType.AI_Melee)
        {
            Body = 0x104; // TODO: Verify
            BaseSoundID = 0x56B;

            SetStr(400, 450);
            SetDex(100, 150);
            SetInt(50, 60);

            SetHits(1150, 1250);

            SetDamage(21, 25);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 100);
            SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 40, 50);
            SetResistance(ResistanceType.Poison, 50, 60);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.Wrestling, 110.0, 115.0);
            SetSkill(SkillName.Tactics, 110.0, 115.0);
            SetSkill(SkillName.MagicResist, 80.0, 95.0);
            SetSkill(SkillName.Anatomy, 100.0, 110.0);

            Fame = 12000;
            Karma = -12000;
        }

        public override string CorpseName => "a corporeal brume corpse";
        public override string DefaultName => "a corporeal brume";

        // TODO: Verify area attack specifics
        public override bool HasAura => Combatant != null;
        public override TimeSpan AuraInterval => TimeSpan.FromSeconds(20);
        public override int AuraRange => 10;

        public override int AuraBaseDamage => Utility.RandomMinMax(25, 35);
        public override int AuraFireDamage => 0;
        public override int AuraColdDamage => 100;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich);
            AddLoot(LootPack.Rich);
        }

        public override void AuraEffect(Mobile m)
        {
            m.FixedParticles(0x374A, 10, 15, 5038, 1181, 2, EffectLayer.Head);
            m.PlaySound(0x213);
        }
    }
}
