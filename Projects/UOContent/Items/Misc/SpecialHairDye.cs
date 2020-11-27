using Server.Gumps;
using Server.Network;

namespace Server.Items
{
    public class SpecialHairDye : Item
    {
        [Constructible]
        public SpecialHairDye() : base(0xE26)
        {
            Weight = 1.0;
            LootType = LootType.Newbied;
        }

        public SpecialHairDye(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "Special Hair Dye";

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

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(GetWorldLocation(), 1))
            {
                from.CloseGump<SpecialHairDyeGump>();
                from.SendGump(new SpecialHairDyeGump(this));
            }
            else
            {
                from.LocalOverheadMessage(MessageType.Regular, 906, 1019045); // I can't reach that.
            }
        }
    }

    public class SpecialHairDyeGump : Gump
    {
        private static readonly SpecialHairDyeEntry[] m_Entries =
        {
            new("*****", 12, 10),
            new("*****", 32, 5),
            new("*****", 38, 8),
            new("*****", 54, 3),
            new("*****", 62, 10),
            new("*****", 81, 2),
            new("*****", 89, 2),
            new("*****", 1153, 2)
        };

        private readonly SpecialHairDye m_SpecialHairDye;

        public SpecialHairDyeGump(SpecialHairDye dye) : base(0, 0)
        {
            m_SpecialHairDye = dye;

            AddPage(0);
            AddBackground(150, 60, 350, 358, 2600);
            AddBackground(170, 104, 110, 270, 5100);
            AddHtmlLocalized(230, 75, 200, 20, 1011013);  // Hair Color Selection Menu
            AddHtmlLocalized(235, 380, 300, 20, 1011014); // Dye my hair this color!
            AddButton(200, 380, 0xFA5, 0xFA7, 1);         // DYE HAIR

            for (var i = 0; i < m_Entries.Length; ++i)
            {
                AddLabel(180, 109 + i * 22, m_Entries[i].HueStart - 1, m_Entries[i].Name);
                AddButton(257, 110 + i * 22, 5224, 5224, 0, GumpButtonType.Page, i + 1);
            }

            for (var i = 0; i < m_Entries.Length; ++i)
            {
                var e = m_Entries[i];

                AddPage(i + 1);

                for (var j = 0; j < e.HueCount; ++j)
                {
                    AddLabel(328 + j / 16 * 80, 102 + j % 16 * 17, e.HueStart + j - 1, "*****");
                    AddRadio(310 + j / 16 * 80, 102 + j % 16 * 17, 210, 211, false, i * 100 + j);
                }
            }
        }

        public override void OnResponse(NetState from, RelayInfo info)
        {
            if (m_SpecialHairDye.Deleted)
            {
                return;
            }

            var m = from.Mobile;
            var switches = info.Switches;

            if (!m_SpecialHairDye.IsChildOf(m.Backpack))
            {
                m.SendLocalizedMessage(1042010); // You must have the objectin your backpack to use it.
                return;
            }

            if (info.ButtonID != 0 && switches.Length > 0)
            {
                if (m.HairItemID == 0)
                {
                    m.SendLocalizedMessage(502623); // You have no hair to dye and cannot use this
                }
                else
                {
                    // To prevent this from being exploited, the hue is abstracted into an internal list

                    var entryIndex = switches[0] / 100;
                    var hueOffset = switches[0] % 100;

                    if (entryIndex >= 0 && entryIndex < m_Entries.Length)
                    {
                        var e = m_Entries[entryIndex];

                        if (hueOffset >= 0 && hueOffset < e.HueCount)
                        {
                            m_SpecialHairDye.Delete();

                            var hue = e.HueStart + hueOffset;

                            m.HairHue = hue;

                            m.SendLocalizedMessage(501199); // You dye your hair
                            m.PlaySound(0x4E);
                        }
                    }
                }
            }
            else
            {
                m.SendLocalizedMessage(501200); // You decide not to dye your hair
            }
        }

        private class SpecialHairDyeEntry
        {
            public SpecialHairDyeEntry(string name, int hueStart, int hueCount)
            {
                Name = name;
                HueStart = hueStart;
                HueCount = hueCount;
            }

            public string Name { get; }

            public int HueStart { get; }

            public int HueCount { get; }
        }
    }
}
