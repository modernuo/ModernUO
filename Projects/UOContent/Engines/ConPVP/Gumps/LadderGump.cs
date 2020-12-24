using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.ConPVP
{
    public class LadderItem : Item
    {
        [Constructible]
        public LadderItem() : base(0x117F) => Movable = false;

        public LadderItem(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public LadderController Ladder { get; set; }

        public override string DefaultName => "1v1 leaderboard";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);

            writer.Write(Ladder);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        Ladder = reader.ReadEntity<LadderController>();
                        break;
                    }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(GetWorldLocation(), 2))
            {
                var ladder = ConPVP.Ladder.Instance ?? Ladder.Ladder;

                if (ladder != null)
                {
                    from.CloseGump<LadderGump>();
                    from.SendGump(new LadderGump(ladder));
                }
            }
            else
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that
            }
        }
    }

    public class LadderGump : Gump
    {
        private readonly Ladder m_Ladder;

        private readonly List<LadderEntry> m_List;
        private readonly int m_Page;
        private int m_ColumnX = 12;

        public LadderGump(Ladder ladder, int page = 0) : base(50, 50)
        {
            m_Ladder = ladder;
            m_Page = page;

            AddPage(0);

            m_List = new List<LadderEntry>(ladder.Entries);

            var lc = Math.Min(m_List.Count, 150);

            var start = page * 15;
            var end = start + 15;

            if (end > lc)
            {
                end = lc;
            }

            var ct = end - start;

            var height = 12 + 20 + ct * 20 + 23 + 12;

            AddBackground(0, 0, 499, height, 0x2436);

            for (var i = start + 1; i < end; i += 2)
            {
                AddImageTiled(12, 32 + (i - start) * 20, 475, 20, 0x2430);
            }

            AddAlphaRegion(10, 10, 479, height - 20);

            if (page > 0)
            {
                AddButton(446, height - 12 - 2 - 16, 0x15E3, 0x15E7, 1);
            }
            else
            {
                AddImage(446, height - 12 - 2 - 16, 0x2626);
            }

            if ((page + 1) * 15 < lc)
            {
                AddButton(466, height - 12 - 2 - 16, 0x15E1, 0x15E5, 2);
            }
            else
            {
                AddImage(466, height - 12 - 2 - 16, 0x2622);
            }

            AddHtml(
                16,
                height - 12 - 2 - 18,
                400,
                20,
                Color(
                    string.Format("Top {3} of {0:N0} duelists, page {1} of {2}", m_List.Count, page + 1, (lc + 14) / 15, lc),
                    0xFFC000
                )
            );

            AddColumnHeader(75, "Rank");
            AddColumnHeader(115, "Level");
            AddColumnHeader(50, "Guild");
            AddColumnHeader(115, "Name");
            AddColumnHeader(60, "Wins");
            AddColumnHeader(60, "Losses");

            for (var i = start; i < end && i < lc; ++i)
            {
                var entry = m_List[i];

                var y = 32 + (i - start) * 20;
                var x = 12;

                AddBorderedText(x, y, 75, Center(Rank(i + 1)), 0xFFFFFF, 0);
                x += 75;

                /*AddImage( 20, y + 5, 0x2616, 0x96C );
                AddImage( 22, y + 5, 0x2616, 0x96C );
                AddImage( 20, y + 7, 0x2616, 0x96C );
                AddImage( 22, y + 7, 0x2616, 0x96C );

                AddImage( 21, y + 6, 0x2616, 0x454 );*/

                AddImage(x + 3, y + 4, 0x805);

                var xp = entry.Experience;
                var level = Ladder.GetLevel(xp);

                Ladder.GetLevelInfo(level, out var xpBase, out var xpAdvance);

                int width;

                var xpOffset = xp - xpBase;

                if (xpOffset >= xpAdvance)
                {
                    width = 109; // level 50
                }
                else
                {
                    width = (109 * xpOffset + xpAdvance / 2) / (xpAdvance - 1);
                }

                // AddImageTiled( 21, y + 6, width, 8, 0x2617 );
                AddImageTiled(x + 3, y + 4, width, 11, 0x806);
                AddBorderedText(x, y, 115, Center(level.ToString()), 0xFFFFFF, 0);
                x += 115;

                var mob = entry.Mobile;

                if (mob.Guild != null)
                {
                    AddBorderedText(x, y, 50, Center(mob.Guild.Abbreviation), 0xFFFFFF, 0);
                }

                x += 50;

                AddBorderedText(x + 5, y, 115 - 5, mob.Name, 0xFFFFFF, 0);
                x += 115;

                AddBorderedText(x, y, 60, Center(entry.Wins.ToString()), 0xFFFFFF, 0);
                x += 60;

                AddBorderedText(x, y, 60, Center(entry.Losses.ToString()), 0xFFFFFF, 0);
                x += 60;

                // AddBorderedText( 292 + 15, y, 115 - 30, String.Format( "{0} <DIV ALIGN=CENTER>/</DIV> <DIV ALIGN=RIGHT>{1}</DIV>", entry.Wins, entry.Losses ), 0xFFC000, 0 );
            }
        }

        public static string Rank(int num)
        {
            var numStr = num.ToString("N0");

            if (num % 100 > 10 && num % 100 < 20)
            {
                return $"{numStr}th";
            }

            return (num % 10) switch
            {
                1 => $"{numStr}st",
                2 => $"{numStr}nd",
                3 => $"{numStr}rd",
                _ => $"{numStr}th"
            };
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var from = sender.Mobile;

            if (info.ButtonID == 1 && m_Page > 0)
            {
                from.SendGump(new LadderGump(m_Ladder, m_Page - 1));
            }
            else if (info.ButtonID == 2 && (m_Page + 1) * 15 < Math.Min(m_List.Count, 150))
            {
                from.SendGump(new LadderGump(m_Ladder, m_Page + 1));
            }
        }

        public string Center(string text) => $"<CENTER>{text}</CENTER>";

        public string Color(string text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";

        private void AddBorderedText(int x, int y, int width, string text, int color, int borderColor)
        {
            /*AddColoredText( x - 1, y, width, text, borderColor );
            AddColoredText( x + 1, y, width, text, borderColor );
            AddColoredText( x, y - 1, width, text, borderColor );
            AddColoredText( x, y + 1, width, text, borderColor );*/
            /*AddColoredText( x - 1, y - 1, width, text, borderColor );
            AddColoredText( x + 1, y + 1, width, text, borderColor );*/
            AddColoredText(x, y, width, text, color);
        }

        private void AddColoredText(int x, int y, int width, string text, int color)
        {
            if (color == 0)
            {
                AddHtml(x, y, width, 20, text);
            }
            else
            {
                AddHtml(x, y, width, 20, Color(text, color));
            }
        }

        private void AddColumnHeader(int width, string name)
        {
            AddBackground(m_ColumnX, 12, width, 20, 0x242C);
            AddImageTiled(m_ColumnX + 2, 14, width - 4, 16, 0x2430);
            AddBorderedText(m_ColumnX, 13, width, Center(name), 0xFFFFFF, 0);

            m_ColumnX += width;
        }
    }
}
