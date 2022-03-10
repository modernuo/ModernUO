using System;

namespace Server.Mobiles
{
    public class Cow : BaseCreature
    {
        [Constructible]
        public Cow() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = Utility.RandomList(0xD8, 0xE7);
            BaseSoundID = 0x78;

            SetStr(30);
            SetDex(15);
            SetInt(5);

            SetHits(18);
            SetMana(0);

            SetDamage(1, 4);

            SetDamage(1, 4);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 5, 15);

            SetSkill(SkillName.MagicResist, 5.5);
            SetSkill(SkillName.Tactics, 5.5);
            SetSkill(SkillName.Wrestling, 5.5);

            Fame = 300;
            Karma = 0;

            VirtualArmor = 10;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 11.1;

            if (Core.AOS && Utility.Random(1000) == 0) // 0.1% chance to have mad cows
            {
                FightMode = FightMode.Closest;
            }
        }

        public Cow(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a cow corpse";

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime MilkedOn { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Milk { get; set; }

        public override string DefaultName => "a cow";

        public override int Meat => 8;
        public override int Hides => 12;
        public override FoodType FavoriteFood => FoodType.FruitsAndVegies | FoodType.GrainsAndHay;

        public override void OnDoubleClick(Mobile from)
        {
            base.OnDoubleClick(from);

            var random = Utility.Random(100);

            if (random < 5)
            {
                Tip();
            }
            else if (random < 20)
            {
                PlaySound(120);
            }
            else if (random < 40)
            {
                PlaySound(121);
            }
        }

        public void Tip()
        {
            PlaySound(121);
            Animate(8, 0, 3, true, false, 0);
        }

        public bool TryMilk(Mobile from)
        {
            if (!from.InLOS(this) || !from.InRange(Location, 2))
            {
                from.SendLocalizedMessage(1080400); // You can not milk the cow from this location.
            }

            if (Controlled && ControlMaster != from)
            {
                from.SendLocalizedMessage(1071182); // The cow nimbly escapes your attempts to milk it.
            }

            if (Milk == 0 && MilkedOn + TimeSpan.FromDays(1) > Core.Now)
            {
                from.SendLocalizedMessage(1080198); // This cow can not be milked now. Please wait for some time.
            }
            else
            {
                if (Milk == 0)
                {
                    Milk = 4;
                }

                MilkedOn = Core.Now;
                Milk--;

                return true;
            }

            return false;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);

            writer.Write(MilkedOn);
            writer.Write(Milk);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version > 0)
            {
                MilkedOn = reader.ReadDateTime();
                Milk = reader.ReadInt();
            }
        }
    }
}
