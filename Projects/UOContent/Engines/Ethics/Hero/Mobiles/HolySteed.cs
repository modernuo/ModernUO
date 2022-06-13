using Server.Ethics;

namespace Server.Mobiles
{
    public class HolySteed : BaseMount
    {
        [Constructible]
        public HolySteed()
            : base("a silver steed", 0x75, 0x3EA8, AIType.AI_Melee, FightMode.Aggressor)
        {
            SetStr(496, 525);
            SetDex(86, 105);
            SetInt(86, 125);

            SetHits(298, 315);

            SetDamage(16, 22);

            SetDamageType(ResistanceType.Physical, 40);
            SetDamageType(ResistanceType.Fire, 40);
            SetDamageType(ResistanceType.Energy, 20);

            SetResistance(ResistanceType.Physical, 55, 65);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Cold, 30, 40);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 20, 30);

            SetSkill(SkillName.MagicResist, 25.1, 30.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 80.5, 92.5);

            Fame = 14000;
            Karma = 14000;

            VirtualArmor = 60;

            Tamable = false;
            ControlSlots = 1;
        }

        public HolySteed(Serial serial)
            : base(serial)
        {
        }

        public override string CorpseName => "a holy corpse";
        public override bool IsDispellable => false;
        public override bool IsBondable => false;

        public override bool HasBreath => true;
        public override bool CanBreath => true;

        public override FoodType FavoriteFood => FoodType.FruitsAndVegies | FoodType.GrainsAndHay;

        public override string ApplyNameSuffix(string suffix)
        {
            if (suffix.Length == 0)
            {
                suffix = Ethic.Hero.Definition.Adjunct.String;
            }
            else
            {
                suffix = $"{suffix} {Ethic.Hero.Definition.Adjunct.String}";
            }

            return base.ApplyNameSuffix(suffix);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Ethic.Find(from) != Ethic.Hero)
            {
                from.SendMessage("You may not ride this steed.");
            }
            else
            {
                base.OnDoubleClick(from);
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
