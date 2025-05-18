using System;
using Server.Network;
using Server.Spells;
using Server.Spells.Seventh;

namespace Server.Gumps
{
    public class PolymorphEntry
    {
        public static readonly PolymorphEntry Chicken = new(8401, 0xD0, 1015236, 15, 10);
        public static readonly PolymorphEntry Dog = new(8405, 0xD9, 1015237, 17, 10);
        public static readonly PolymorphEntry Wolf = new(8426, 0xE1, 1015238, 18, 10);
        public static readonly PolymorphEntry Panther = new(8473, 0xD6, 1015239, 20, 14);
        public static readonly PolymorphEntry Gorilla = new(8437, 0x1D, 1015240, 23, 10);
        public static readonly PolymorphEntry BlackBear = new(8399, 0xD3, 1015241, 22, 10);
        public static readonly PolymorphEntry GrizzlyBear = new(8411, 0xD4, 1015242, 22, 12);
        public static readonly PolymorphEntry PolarBear = new(8417, 0xD5, 1015243, 26, 10);
        public static readonly PolymorphEntry HumanMale = new(8397, 0x190, 1015244, 29, 8);
        public static readonly PolymorphEntry HumanFemale = new(8398, 0x191, 1015254, 29, 10);
        public static readonly PolymorphEntry Slime = new(8424, 0x33, 1015246, 5, 10);
        public static readonly PolymorphEntry Orc = new(8416, 0x11, 1015247, 29, 10);
        public static readonly PolymorphEntry LizardMan = new(8414, 0x21, 1015248, 26, 10);
        public static readonly PolymorphEntry Gargoyle = new(8409, 0x04, 1015249, 22, 10);
        public static readonly PolymorphEntry Ogre = new(8415, 0x01, 1015250, 24, 9);
        public static readonly PolymorphEntry Troll = new(8425, 0x36, 1015251, 25, 9);
        public static readonly PolymorphEntry Ettin = new(8408, 0x02, 1015252, 25, 8);
        public static readonly PolymorphEntry Daemon = new(8403, 0x09, 1015253, 25, 8);

        private PolymorphEntry(int art, int body, int locNum, int x, int y)
        {
            ArtID = art;
            BodyID = body;
            LocNumber = locNum;
            X = x;
            Y = y;
        }

        public int ArtID { get; }

        public int BodyID { get; }

        public int LocNumber { get; }

        public int X { get; }

        public int Y { get; }
    }

    public class PolymorphGump : StaticGump<PolymorphGump>
    {
        private static readonly PolymorphCategory[] Categories =
        {
            new(
                1015235, // Animals
                PolymorphEntry.Chicken,
                PolymorphEntry.Dog,
                PolymorphEntry.Wolf,
                PolymorphEntry.Panther,
                PolymorphEntry.Gorilla,
                PolymorphEntry.BlackBear,
                PolymorphEntry.GrizzlyBear,
                PolymorphEntry.PolarBear,
                PolymorphEntry.HumanMale
            ),

            new(
                1015245, // Monsters
                PolymorphEntry.Slime,
                PolymorphEntry.Orc,
                PolymorphEntry.LizardMan,
                PolymorphEntry.Gargoyle,
                PolymorphEntry.Ogre,
                PolymorphEntry.Troll,
                PolymorphEntry.Ettin,
                PolymorphEntry.Daemon,
                PolymorphEntry.HumanFemale
            )
        };

        private readonly Item _scroll;

        public PolymorphGump(Item scroll) : base(50, 50) => _scroll = scroll;

        protected override void BuildLayout(ref StaticGumpBuilder builder)
        {
            int x, y;
            builder.AddPage();
            builder.AddBackground(0, 0, 585, 393, 5054);
            builder.AddBackground(195, 36, 387, 275, 3000);
            builder.AddHtmlLocalized(0, 0, 510, 18, 1015234); // <center>Polymorph Selection Menu</center>
            builder.AddHtmlLocalized(60, 355, 150, 18, 1011036); // OKAY
            builder.AddButton(25, 355, 4005, 4007, 1, GumpButtonType.Reply, 1);
            builder.AddHtmlLocalized(320, 355, 150, 18, 1011012); // CANCEL
            builder.AddButton(285, 355, 4005, 4007, 0, GumpButtonType.Reply, 2);

            y = 35;
            for (var i = 0; i < Categories.Length; i++)
            {
                var cat = Categories[i];
                builder.AddHtmlLocalized(5, y, 150, 25, cat.LocNumber, true);
                builder.AddButton(155, y, 4005, 4007, 0, GumpButtonType.Page, i + 1);
                y += 25;
            }

            for (var i = 0; i < Categories.Length; i++)
            {
                var cat = Categories[i];
                builder.AddPage(i + 1);

                for (var c = 0; c < cat.Entries.Length; c++)
                {
                    var entry = cat.Entries[c];
                    var yOffset = Math.DivRem(c, 3, out var xOffset);
                    x = 198 + xOffset * 129;
                    y = 38 + yOffset * 67;

                    builder.AddHtmlLocalized(x, y, 100, 18, entry.LocNumber);
                    builder.AddItem(x + 20, y + 25, entry.ArtID);
                    builder.AddRadio(x, y + 20, 210, 211, false, (c << 8) + i);
                }
            }
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (info.ButtonID != 1 || info.Switches.Length <= 0)
            {
                return;
            }

            var ent = Math.DivRem(info.Switches[0], 256, out var cat);

            if (cat < 0 || cat >= Categories.Length)
            {
                return;
            }

            var entries = Categories[cat];

            if (ent >= 0 && ent < entries.Entries.Length)
            {
                Spell spell = new PolymorphSpell(state.Mobile, _scroll, entries.Entries[ent].BodyID);
                spell.Cast();
            }
        }

        private class PolymorphCategory
        {
            public PolymorphCategory(int num, params PolymorphEntry[] entries)
            {
                LocNumber = num;
                Entries = entries;
            }

            public PolymorphEntry[] Entries { get; }

            public int LocNumber { get; }
        }
    }

    public class NewPolymorphGump : StaticGump<NewPolymorphGump>
    {
        private static readonly PolymorphEntry[] _entries =
        {
            PolymorphEntry.Chicken,
            PolymorphEntry.Dog,
            PolymorphEntry.Wolf,
            PolymorphEntry.Panther,
            PolymorphEntry.Gorilla,
            PolymorphEntry.BlackBear,
            PolymorphEntry.GrizzlyBear,
            PolymorphEntry.PolarBear,
            PolymorphEntry.HumanMale,
            PolymorphEntry.HumanFemale,
            PolymorphEntry.Slime,
            PolymorphEntry.Orc,
            PolymorphEntry.LizardMan,
            PolymorphEntry.Gargoyle,
            PolymorphEntry.Ogre,
            PolymorphEntry.Troll,
            PolymorphEntry.Ettin,
            PolymorphEntry.Daemon
        };

        private readonly Item _scroll;

        public NewPolymorphGump(Item scroll) : base(0, 0) => _scroll = scroll;

        protected override void BuildLayout(ref StaticGumpBuilder builder)
        {
            builder.AddPage();

            builder.AddBackground(0, 0, 520, 404, 0x13BE);
            builder.AddImageTiled(10, 10, 500, 20, 0xA40);
            builder.AddImageTiled(10, 40, 500, 324, 0xA40);
            builder.AddImageTiled(10, 374, 500, 20, 0xA40);
            builder.AddAlphaRegion(10, 10, 500, 384);

            builder.AddHtmlLocalized(14, 12, 500, 20, 1015234, 0x7FFF); // <center>Polymorph Selection Menu</center>

            builder.AddButton(10, 374, 0xFB1, 0xFB2, 0);
            builder.AddHtmlLocalized(45, 376, 450, 20, 1060051, 0x7FFF); // CANCEL

            for (var i = 0; i < _entries.Length; i++)
            {
                var entry = _entries[i];

                var page = Math.DivRem(i, 10, out var pos) + 1;

                if (pos == 0)
                {
                    if (page > 1)
                    {
                        builder.AddButton(400, 374, 0xFA5, 0xFA7, 0, GumpButtonType.Page, page);
                        builder.AddHtmlLocalized(440, 376, 60, 20, 1043353, 0x7FFF); // Next
                    }

                    builder.AddPage(page);

                    if (page > 1)
                    {
                        builder.AddButton(300, 374, 0xFAE, 0xFB0, 0, GumpButtonType.Page, 1);
                        builder.AddHtmlLocalized(340, 376, 60, 20, 1011393, 0x7FFF); // Back
                    }
                }

                var yOffset = Math.DivRem(pos, 2, out var xOffset);
                var x = xOffset * 250 + 14;
                var y = yOffset * 64 + 44;

                builder.AddImageTiledButton(x, y, 0x918, 0x919, i + 1, GumpButtonType.Reply, 0, entry.ArtID, 0x0, entry.X, entry.Y);
                builder.AddHtmlLocalized(x + 84, y, 250, 60, entry.LocNumber, 0x7FFF);
            }
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            var idx = info.ButtonID - 1;

            if (idx < 0 || idx >= _entries.Length)
            {
                return;
            }

            Spell spell = new PolymorphSpell(sender.Mobile, _scroll, _entries[idx].BodyID);
            spell.Cast();
        }
    }
}
