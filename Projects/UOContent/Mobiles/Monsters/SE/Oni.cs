using Server.Engines.Plants;

namespace Server.Mobiles
{
    public class Oni : BaseCreature
    {
        [Constructible]
        public Oni() : base(AIType.AI_Mage)
        {
            Body = 241;

            SetStr(801, 910);
            SetDex(151, 300);
            SetInt(171, 195);

            SetHits(401, 530);

            SetDamage(14, 20);

            SetDamageType(ResistanceType.Physical, 70);
            SetDamageType(ResistanceType.Fire, 10);
            SetDamageType(ResistanceType.Energy, 20);

            SetResistance(ResistanceType.Physical, 65, 80);
            SetResistance(ResistanceType.Fire, 50, 70);
            SetResistance(ResistanceType.Cold, 35, 50);
            SetResistance(ResistanceType.Poison, 45, 70);
            SetResistance(ResistanceType.Energy, 45, 65);

            SetSkill(SkillName.EvalInt, 100.1, 125.0);
            SetSkill(SkillName.Magery, 96.1, 106.0);
            SetSkill(SkillName.Anatomy, 85.1, 95.0);
            SetSkill(SkillName.MagicResist, 85.1, 100.0);
            SetSkill(SkillName.Tactics, 86.1, 101.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 12000;
            Karma = -12000;

            if (Utility.RandomDouble() < .33)
            {
                PackItem(Seed.RandomBonsaiSeed());
            }

            // TODO: Brain (0x1CF0) or Skull (0x1AE3) or Body Part (0x1CE3)
        }

        /* TODO: Angry Fire
         * cliloc 1070823
         * Action: 4 4 1 true false 1
         * Damage: 50-85, 60 phys, 20 fire, 20 nrgy according to the guide
         * With 45/49/70 res I got 48
         *  50: 30/10/10 -> 16 + 5 + 3 = 24
         *  85: 51/17/17 -> 28 + 8 + 5 = 41
         */

        public Oni(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "an oni corpse";
        public override string DefaultName => "an oni";

        public override bool CanRummageCorpses => true;
        public override int TreasureMapLevel => 4;

        public override int GetAngerSound() => 0x4E3;

        public override int GetIdleSound() => 0x4E2;

        public override int GetAttackSound() => 0x4E1;

        public override int GetHurtSound() => 0x4E4;

        public override int GetDeathSound() => 0x4E0;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 3);
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
        }
    }
}
