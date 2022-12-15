using ModernUO.Serialization;
using System;
using Server.Engines.Plants;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class YomotsuWarrior : BaseCreature
    {
        [Constructible]
        public YomotsuWarrior() : base(AIType.AI_Melee)
        {
            Body = 245;
            BaseSoundID = 0x452;

            SetStr(486, 530);
            SetDex(151, 165);
            SetInt(17, 31);

            SetHits(486, 530);
            SetMana(17, 31);

            SetDamage(8, 10);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 65, 85);
            SetResistance(ResistanceType.Fire, 30, 50);
            SetResistance(ResistanceType.Cold, 45, 65);
            SetResistance(ResistanceType.Poison, 35, 55);
            SetResistance(ResistanceType.Energy, 25, 50);

            SetSkill(SkillName.Anatomy, 85.1, 95.0);
            SetSkill(SkillName.MagicResist, 82.6, 90.5);
            SetSkill(SkillName.Tactics, 95.1, 105.0);
            SetSkill(SkillName.Wrestling, 97.6, 107.5);

            Fame = 4200;
            Karma = -4200;

            PackItem(new GreenGourd());
            PackItem(new ExecutionersAxe());

            if (Utility.RandomBool())
            {
                PackItem(new LongPants());
            }
            else
            {
                PackItem(new ShortPants());
            }

            switch (Utility.Random(4))
            {
                case 0:
                    PackItem(new Shoes());
                    break;
                case 1:
                    PackItem(new Sandals());
                    break;
                case 2:
                    PackItem(new Boots());
                    break;
                case 3:
                    PackItem(new ThighBoots());
                    break;
            }

            if (Utility.RandomDouble() < .25)
            {
                PackItem(Seed.RandomBonsaiSeed());
            }
        }

        public override string CorpseName => "a yomotsu corpse";
        public override string DefaultName => "a yomotsu warrior";

        public override FoodType FavoriteFood => FoodType.Fish;

        public override int Meat => 1;

        public override bool CanRummageCorpses => true;
        public override int TreasureMapLevel => 3;

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.DoubleStrike;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich, 2);
            AddLoot(LootPack.Gems, 2);
        }

        // TODO: Throwing Dagger

        public override void OnGaveMeleeAttack(Mobile defender, int damage)
        {
            base.OnGaveMeleeAttack(defender, damage);

            if (Utility.RandomDouble() < 0.1)
            {
                /* Maniacal laugh
                 * Cliloc: 1070840
                 * Effect: Type: "3" From: "0x57D4F5B" To: "0x0" ItemId: "0x37B9" ItemIdName: "glow" FromLocation: "(884 715, 10)" ToLocation: "(884 715, 10)" Speed: "10" Duration: "5" FixedDirection: "True" Explode: "False"
                 * Paralyzes for 4 seconds, or until hit
                 */

                defender.FixedEffect(0x37B9, 10, 5);
                defender.SendLocalizedMessage(1070840); // You are frozen as the creature laughs maniacally.

                defender.Paralyze(TimeSpan.FromSeconds(4.0));
            }
        }

        public override int GetIdleSound() => 0x42A;

        public override int GetAttackSound() => 0x435;

        public override int GetHurtSound() => 0x436;

        public override int GetDeathSound() => 0x43A;
    }
}
