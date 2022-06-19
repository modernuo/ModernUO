namespace Server.Items
{
    [Flippable(0x236E, 0x2371)]
    public class LightOfTheWinterSolstice : Item
    {
        private static readonly string[] m_StaffNames =
        {
            "Aenima",
            "Alkiser",
            "ASayre",
            "David",
            "Krrios",
            "Mark",
            "Merlin",
            "Merlix", // LordMerlix
            "Phantom",
            "Phenos",
            "psz",
            "Ryan",
            "Quantos",
            "Outkast", // TheOutkastDev
            "V",       // Admin_V
            "Zippy"
        };

        [Constructible]
        public LightOfTheWinterSolstice(string dipper = null) : base(0x236E)
        {
            Dipper = dipper ?? m_StaffNames.RandomElement();

            Weight = 1.0;
            LootType = LootType.Blessed;
            Light = LightType.Circle300;
            Hue = Utility.RandomDyedHue();
        }

        public LightOfTheWinterSolstice(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Dipper { get; set; }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            LabelTo(from, 1070881, Dipper); // Hand Dipped by ~1_name~
            LabelTo(from, 1070880);         // Winter 2004
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1070881, Dipper); // Hand Dipped by ~1_name~
            list.Add(1070880);         // Winter 2004
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(Dipper);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        Dipper = reader.ReadString();
                        break;
                    }
                case 0:
                    {
                        Dipper = m_StaffNames.RandomElement();
                        break;
                    }
            }

            if (Dipper != null)
            {
                Dipper = string.Intern(Dipper);
            }
        }
    }
}
