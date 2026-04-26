using System.Collections.Generic;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.ConPVP
{
    public class PreferencesController : Item
    {
        [Constructible]
        public PreferencesController() : base(0x1B7A)
        {
            Visible = false;
            Movable = false;

            Preferences = new Preferences();

            if (Preferences.Instance == null)
            {
                Preferences.Instance = Preferences;
            }
            else
            {
                Delete();
            }
        }

        public PreferencesController(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.Administrator, canModify: true)]
        public Preferences Preferences { get; private set; }

        public override string DefaultName => "preferences controller";

        public override void Delete()
        {
            if (Preferences.Instance != Preferences)
            {
                base.Delete();
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);

            Preferences.Serialize(writer);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        Preferences = new Preferences(reader);
                        Preferences.Instance = Preferences;
                        break;
                    }
            }
        }
    }

    public class Preferences
    {
        private readonly Dictionary<Mobile, PreferencesEntry> m_Table;

        public Preferences() => m_Table = new Dictionary<Mobile, PreferencesEntry>();

        public Preferences(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        var count = reader.ReadEncodedInt();

                        m_Table = new Dictionary<Mobile, PreferencesEntry>(count);

                        for (var i = 0; i < count; ++i)
                        {
                            var entry = new PreferencesEntry(reader, version);

                            if (entry.Mobile != null)
                            {
                                m_Table[entry.Mobile] = entry;
                            }
                        }

                        break;
                    }
            }
        }

        public static Preferences Instance { get; set; }

        public PreferencesEntry Find(Mobile mob)
        {
            if (!m_Table.TryGetValue(mob, out var entry))
            {
                m_Table[mob] = entry = new PreferencesEntry(mob);
            }

            return entry;
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version;

            var count = m_Table.Values.Count;
            writer.WriteEncodedInt(count);

            foreach (var entry in m_Table.Values)
            {
                entry.Serialize(writer);
            }
        }
    }

    public class PreferencesEntry
    {
        public PreferencesEntry(Mobile mob)
        {
            Mobile = mob;
            Disliked = new List<string>();
        }

        public PreferencesEntry(IGenericReader reader, int version)
        {
            switch (version)
            {
                case 0:
                    {
                        Mobile = reader.ReadEntity<Mobile>();

                        var count = reader.ReadEncodedInt();

                        Disliked = new List<string>(count);

                        for (var i = 0; i < count; ++i)
                        {
                            Disliked.Add(reader.ReadString());
                        }

                        break;
                    }
            }
        }

        public Mobile Mobile { get; }

        public List<string> Disliked { get; }

        public void Serialize(IGenericWriter writer)
        {
            writer.Write(Mobile);

            writer.WriteEncodedInt(Disliked.Count);

            for (var i = 0; i < Disliked.Count; ++i)
            {
                writer.Write(Disliked[i]);
            }
        }
    }

    public class PreferencesGump : DynamicGump
    {
        private readonly PreferencesEntry _entry;
        private int _columnX;

        public override bool Singleton => true;

        private PreferencesGump(PreferencesEntry entry) : base(50, 50) => _entry = entry;

        public static void DisplayTo(Mobile from, Preferences prefs)
        {
            if (from?.NetState == null || prefs == null)
            {
                return;
            }

            var entry = prefs.Find(from);

            if (entry == null)
            {
                return;
            }

            from.SendGump(new PreferencesGump(entry));
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            _columnX = 12;

            var arenas = Arena.Arenas;

            builder.AddPage();

            var height = 12 + 20 + arenas.Count * 31 + 24 + 12;

            builder.AddBackground(0, 0, 499 + 40 - 365, height, 0x2436);

            for (var i = 1; i < arenas.Count; i += 2)
            {
                builder.AddImageTiled(12, 32 + i * 31, 475 + 40 - 365, 30, 0x2430);
            }

            builder.AddAlphaRegion(10, 10, 479 + 40 - 365, height - 20);

            AddColumnHeader(ref builder, 35, null);
            AddColumnHeader(ref builder, 115, "Arena");

            builder.AddButton(499 + 40 - 365 - 12 - 63 - 4 - 63, height - 12 - 24, 247, 248, 1);
            builder.AddButton(499 + 40 - 365 - 12 - 63, height - 12 - 24, 241, 242, 2);

            for (var i = 0; i < arenas.Count; ++i)
            {
                var ar = arenas[i];

                var name = ar.Name ?? "(no name)";

                var x = 12;
                var y = 32 + i * 31;

                var color = 0xCCFFCC;

                builder.AddCheckbox(x + 3, y + 1, 9730, 9727, _entry.Disliked.Contains(name), i);
                x += 35;

                AddBorderedText(ref builder, x + 5, y + 5, 115 - 5, name, color, 0);
            }
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (_entry == null)
            {
                return;
            }

            if (info.ButtonID != 1)
            {
                return;
            }

            _entry.Disliked.Clear();

            var arenas = Arena.Arenas;

            for (var i = 0; i < info.Switches.Length; ++i)
            {
                var idx = info.Switches[i];

                if (idx >= 0 && idx < arenas.Count)
                {
                    _entry.Disliked.Add(arenas[idx].Name);
                }
            }
        }

        private static void AddBorderedText(ref DynamicGumpBuilder builder, int x, int y, int width, string text, int color, int borderColor)
        {
            AddColoredText(ref builder, x, y, width, text, color);
        }

        private static void AddColoredText(ref DynamicGumpBuilder builder, int x, int y, int width, string text, int color)
        {
            builder.AddHtml(x, y, width, 20, color == 0 ? text : text.Color(color));
        }

        private void AddColumnHeader(ref DynamicGumpBuilder builder, int width, string name)
        {
            builder.AddBackground(_columnX, 12, width, 20, 0x242C);
            builder.AddImageTiled(_columnX + 2, 14, width - 4, 16, 0x2430);

            if (name != null)
            {
                AddBorderedText(ref builder, _columnX, 13, width, name.Center(), 0xFFFFFF, 0);
            }

            _columnX += width;
        }
    }
}
