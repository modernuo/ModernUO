using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Network;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public abstract partial class StValentinesBear : Item
    {
        [InternString]
        [InvalidateProperties]
        [SerializableField(0)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private string _owner;

        [InternString]
        [InvalidateProperties]
        [SerializableField(1)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private string _line1;

        [InternString]
        [InvalidateProperties]
        [SerializableField(2)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private string _line2;

        [InternString]
        [InvalidateProperties]
        [SerializableField(3)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private string _line3;

        public StValentinesBear(int itemid, string name) : base(itemid)
        {
            Owner = name;
            LootType = LootType.Blessed;
        }

        public override string DefaultName => _owner != null ? $"{_owner}'s St. Valentine Bear" : "St. Valentine Bear";

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime EditLimit { get; set; }

        public bool IsSigned => _line1 != null || _line2 != null || _line3 != null;

        public bool CanSign => !IsSigned || Core.Now <= EditLimit;

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (_owner != null)
            {
                list.Add(1150295, _owner); // ~1_NAME~'s St. Valentine Bear
            }
            else
            {
                list.Add(1150294); // St. Valentine Bear
            }

            AddLine(list, 1150301, _line1); // [ ~1_LINE0~ ]
            AddLine(list, 1150302, _line2); // [ ~1_LINE1~ ]
            AddLine(list, 1150303, _line3); // [ ~1_LINE2~ ]
        }

        private static void AddLine(ObjectPropertyList list, int cliloc, string line)
        {
            if (line != null)
            {
                list.Add(cliloc, line);
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            ShowLine(from, 1150301, _line1); // [ ~1_LINE0~ ]
            ShowLine(from, 1150302, _line2); // [ ~1_LINE1~ ]
            ShowLine(from, 1150303, _line3); // [ ~1_LINE2~ ]
        }

        private void ShowLine(Mobile from, int cliloc, string line)
        {
            if (line != null)
            {
                LabelTo(from, cliloc, line);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!CupidsArrow.CheckSeason(from) || !CanSign)
            {
                return;
            }

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1080063); // This must be in your backpack to use it.
                return;
            }

            from.SendGump(new InternalGump(this));
        }

        private class InternalGump : Gump
        {
            private readonly StValentinesBear m_Bear;

            public InternalGump(StValentinesBear bear)
                : base(50, 50)
            {
                m_Bear = bear;

                AddPage(0);
                AddBackground(0, 0, 420, 320, 9300);
                AddHtml(10, 10, 400, 21, "<CENTER>St. Valentine Bear</CENTER>");
                AddHtmlLocalized(
                    10,
                    40,
                    400,
                    75,
                    1150293,
                    0
                ); // Enter up to three lines of personalized greeting for your St. Valentine Bear. You many enter up to 25 characters per line. Once you enter text, you will only be able to correct mistakes for 10 minutes.

                AddHtmlLocalized(10, 129, 400, 21, 1150296, 0); // Line 1:
                AddBackground(10, 150, 400, 24, 9350);
                AddTextEntry(15, 152, 390, 20, 0, 0, "", 25);

                AddHtmlLocalized(10, 179, 400, 21, 1150297, 0); // Line 2:
                AddBackground(10, 200, 400, 24, 9350);
                AddTextEntry(15, 202, 390, 20, 0, 1, "", 25);

                AddHtmlLocalized(10, 229, 400, 21, 1150298, 0); // Line 3:
                AddBackground(10, 250, 400, 24, 9350);
                AddTextEntry(15, 252, 390, 20, 0, 2, "", 25);

                AddButton(15, 285, 242, 241, 0);
                AddButton(335, 285, 247, 248, 1);
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                var from = sender.Mobile;

                if (m_Bear.Deleted || !m_Bear.IsChildOf(from.Backpack) || !m_Bear.CanSign || info.ButtonID != 1)
                {
                    return;
                }

                var line1 = GetLine(info, 0);
                var line2 = GetLine(info, 1);
                var line3 = GetLine(info, 2);

                if (string.IsNullOrEmpty(line1)
                    || string.IsNullOrEmpty(line2)
                    || string.IsNullOrEmpty(line3))
                {
                    from.SendMessage("Lines cannot be left blank.");
                    return;
                }

                if (line1.Length > 25
                    || line2.Length > 25
                    || line3.Length > 25)
                {
                    from.SendMessage("Lines may not exceed 25 characters.");
                    return;
                }

                if (!m_Bear.IsSigned)
                {
                    m_Bear.EditLimit = Core.Now + TimeSpan.FromMinutes(10);
                }

                m_Bear.Line1 = Utility.FixHtml(line1);
                m_Bear.Line2 = Utility.FixHtml(line2);
                m_Bear.Line3 = Utility.FixHtml(line3);

                from.SendMessage("You add the personalized greeting to your St. Valentine Bear.");
            }

            private static string GetLine(RelayInfo info, int idx)
            {
                var tr = info.GetTextEntry(idx);

                return tr?.Text;
            }
        }
    }

    [SerializationGenerator(0, false)]
    [Flippable(0x48E0, 0x48E1)]
    public partial class StValentinesPanda : StValentinesBear
    {
        [Constructible]
        public StValentinesPanda(string name = null) : base(0x48E0, name)
        {
        }
    }

    [SerializationGenerator(0, false)]
    [Flippable(0x48E2, 0x48E3)]
    public partial class StValentinesPolarBear : StValentinesBear
    {
        [Constructible]
        public StValentinesPolarBear(string name = null) : base(0x48E2, name)
        {
        }
    }
}
