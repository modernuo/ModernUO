using Server.Items;

namespace Server.Mobiles
{
    public class CuSidhe : BaseMount
    {
        [Constructible]
        public CuSidhe(string name = null) : base(name, 277, 0x3E91, AIType.AI_Animal, FightMode.Aggressor)
        {
            var chance = Utility.RandomDouble() * 23301;

            if (chance <= 1)
            {
                Hue = 0x489;
            }
            else if (chance < 50)
            {
                Hue = Utility.RandomList(0x657, 0x515, 0x4B1, 0x481, 0x482, 0x455);
            }
            else if (chance < 500)
            {
                Hue = Utility.RandomList(0x97A, 0x978, 0x901, 0x8AC, 0x5A7, 0x527);
            }

            SetStr(1200, 1225);
            SetDex(150, 170);
            SetInt(250, 285);

            SetHits(1010, 1275);

            SetDamage(21, 28);

            SetDamageType(ResistanceType.Physical, 0);
            SetDamageType(ResistanceType.Cold, 50);
            SetDamageType(ResistanceType.Energy, 50);

            SetResistance(ResistanceType.Physical, 50, 65);
            SetResistance(ResistanceType.Fire, 25, 45);
            SetResistance(ResistanceType.Cold, 70, 85);
            SetResistance(ResistanceType.Poison, 30, 50);
            SetResistance(ResistanceType.Energy, 70, 85);

            SetSkill(SkillName.Wrestling, 90.1, 96.8);
            SetSkill(SkillName.Tactics, 90.3, 99.3);
            SetSkill(SkillName.MagicResist, 75.3, 90.0);
            SetSkill(SkillName.Anatomy, 65.5, 69.4);
            SetSkill(SkillName.Healing, 72.2, 98.9);

            Fame = 5000;  // Guessing here
            Karma = 5000; // Guessing here

            Tamable = true;
            ControlSlots = 4;
            MinTameSkill = 101.1;

            if (Utility.RandomDouble() < 0.2)
            {
                PackItem(new TreasureMap(5, Map.Trammel));
            }

            // if (Utility.RandomDouble() < 0.1)
            // PackItem( new ParrotItem() );

            PackGold(500, 800);

            // TODO 0-2 spellweaving scroll
        }

        public CuSidhe(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a cu sidhe corpse";
        public override string DefaultName => "a cu sidhe";

        public override bool CanHeal => true;
        public override bool CanHealOwner => true;
        public override FoodType FavoriteFood => FoodType.FruitsAndVegies;
        public override bool CanAngerOnTame => true;
        public override bool StatLossAfterTame => true;
        public override int Hides => 10;
        public override int Meat => 3;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.AosFilthyRich, 5);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.Race != Race.Elf && from == ControlMaster && from.AccessLevel == AccessLevel.Player)
            {
                var pads = from.FindItemOnLayer(Layer.Shoes);

                if (pads is PadsOfTheCuSidhe)
                {
                    from.SendLocalizedMessage(1071981); // Your boots allow you to mount the Cu Sidhe.
                }
                else
                {
                    from.SendLocalizedMessage(1072203); // Only Elves may use this.
                    return;
                }
            }

            base.OnDoubleClick(from);
        }

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.BleedAttack;

        public override int GetIdleSound() => 0x577;

        public override int GetAttackSound() => 0x576;

        public override int GetAngerSound() => 0x578;

        public override int GetHurtSound() => 0x576;

        public override int GetDeathSound() => 0x579;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version < 1 && Name == "a Cu Sidhe")
            {
                Name = null;
            }
        }
    }
}
