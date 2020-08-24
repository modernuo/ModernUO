using Server.Items;

namespace Server.Mobiles
{
    public class PatchworkSkeleton : BaseCreature
    {
        [Constructible]
        public PatchworkSkeleton() : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Body = 309;
            BaseSoundID = 0x48D;

            SetStr(96, 120);
            SetDex(71, 95);
            SetInt(16, 40);

            SetHits(58, 72);

            SetDamage(18, 22);

            SetDamageType(ResistanceType.Physical, 85);
            SetDamageType(ResistanceType.Cold, 15);

            SetResistance(ResistanceType.Physical, 55, 65);
            SetResistance(ResistanceType.Fire, 50, 60);
            SetResistance(ResistanceType.Cold, 70, 80);
            SetResistance(ResistanceType.Poison, 100);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.MagicResist, 70.1, 95.0);
            SetSkill(SkillName.Tactics, 55.1, 80.0);
            SetSkill(SkillName.Wrestling, 50.1, 70.0);

            Fame = 500;
            Karma = -500;

            VirtualArmor = 54;
        }

        public PatchworkSkeleton(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a patchwork skeletal corpse";

        public override string DefaultName => "a patchwork skeleton";

        public override bool BleedImmune => true;
        public override Poison PoisonImmune => Poison.Lethal;

        public override int TreasureMapLevel => 1;

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.Dismount;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
