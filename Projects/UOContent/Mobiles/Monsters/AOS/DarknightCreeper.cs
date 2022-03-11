using Server.Items;

namespace Server.Mobiles
{
    public class DarknightCreeper : BaseCreature
    {
        [Constructible]
        public DarknightCreeper() : base(AIType.AI_Mage)
        {
            Name = NameList.RandomName("darknight creeper");
            Body = 313;
            BaseSoundID = 0xE0;

            SetStr(301, 330);
            SetDex(101, 110);
            SetInt(301, 330);

            SetHits(4000);

            SetDamage(22, 26);

            SetDamageType(ResistanceType.Physical, 85);
            SetDamageType(ResistanceType.Poison, 15);

            SetResistance(ResistanceType.Physical, 60);
            SetResistance(ResistanceType.Fire, 60);
            SetResistance(ResistanceType.Cold, 100);
            SetResistance(ResistanceType.Poison, 90);
            SetResistance(ResistanceType.Energy, 75);

            SetSkill(SkillName.DetectHidden, 80.0);
            SetSkill(SkillName.EvalInt, 118.1, 120.0);
            SetSkill(SkillName.Magery, 112.6, 120.0);
            SetSkill(SkillName.Meditation, 150.0);
            SetSkill(SkillName.Poisoning, 120.0);
            SetSkill(SkillName.MagicResist, 90.1, 90.9);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 90.9);
            SetSkill(SkillName.Necromancy, 120.1, 130.0);
            SetSkill(SkillName.SpiritSpeak, 120.1, 130.0);

            Fame = 22000;
            Karma = -22000;

            VirtualArmor = 34;
        }

        public DarknightCreeper(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a darknight creeper corpse";
        public override bool IgnoreYoungProtection => Core.ML;

        public override bool BardImmune => !Core.SE;
        public override bool Unprovokable => Core.SE;
        public override bool AreaPeaceImmune => Core.SE;
        public override bool BleedImmune => true;
        public override Poison PoisonImmune => Poison.Lethal;
        public override Poison HitPoison => Poison.Lethal;

        public override int TreasureMapLevel => 1;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich, 2);
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            if (!Summoned && !NoKillAwards && DemonKnight.CheckArtifactChance(this))
            {
                DemonKnight.DistributeArtifact(this);
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            if (BaseSoundID == 471)
            {
                BaseSoundID = 0xE0;
            }
        }
    }
}
