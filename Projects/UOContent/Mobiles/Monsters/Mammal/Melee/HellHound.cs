using Server.Items;

namespace Server.Mobiles
{
    public class HellHound : BaseCreature
    {
        [Constructible]
        public HellHound() : base(AIType.AI_Melee)
        {
            Body = 98;
            BaseSoundID = 229;

            SetStr(102, 150);
            SetDex(81, 105);
            SetInt(36, 60);

            SetHits(66, 125);

            SetDamage(11, 17);

            SetDamageType(ResistanceType.Physical, 20);
            SetDamageType(ResistanceType.Fire, 80);

            SetResistance(ResistanceType.Physical, 25, 35);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Poison, 10, 20);
            SetResistance(ResistanceType.Energy, 10, 20);

            Fame = 3400;
            Karma = -3400;

            VirtualArmor = 30;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 85.5;

            PackItem(new SulfurousAsh(5));
        }

        public HellHound(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a hell hound corpse";
        public override string DefaultName => "a hell hound";
        public override int Meat => 1;
        public override FoodType FavoriteFood => FoodType.Meat;
        public override PackInstinct PackInstinct => PackInstinct.Canine;

        private static MonsterAbility[] _abilities = { MonsterAbilities.FireBreath };
        public override MonsterAbility[] GetMonsterAbilities() => _abilities;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
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
            var version = reader.ReadInt();
        }
    }
}
